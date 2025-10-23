# Proposal: Refactor Application.RaiseMouseEvent for Concurrency Support

## Problem Statement

`Application.RaiseMouseEvent` currently has multiple static dependencies that prevent true concurrent/parallelizable unit testing.

My previous tests in `ArrangementTests.cs` called `NewMouseEvent` directly on views, which bypassed `Application.RaiseMouseEvent`. While this tested the view-level mouse handling, it did NOT test the full application-level mouse event routing that includes:
- Application-level mouse event dispatching
- Popover dismissal logic  
- View hierarchy checking against `Application.Top`
- Mouse enter/leave event tracking across the view hierarchy
- Application-level `MouseEvent` event raising

The current `AllArrangementTests_AreParallelizable` test proves nothing about true concurrency because it doesn't use `Application.RaiseMouseEvent` at all.

### Static Dependencies in Application.Mouse.cs

1. **`LastMousePosition`** - Static field that stores the last mouse position (line 11)
2. **`IsMouseDisabled`** - Static configuration property (line 20)
3. **`Application.Top`** - Static reference to the top-level view (line 104)
4. **`Application.Popover`** - Static reference to popover management (lines 79-82, 104)
5. **`Application.Navigation`** - Static reference (used indirectly via `View.GetViewsUnderLocation`)
6. **`MouseEvent`** - Static event for application-level mouse handling (line 193)
7. **`CachedViewsUnderMouse`** - Static list tracking views under mouse for enter/leave events (line 239)

These static dependencies mean that:
- Multiple tests cannot run `Application.RaiseMouseEvent` concurrently without interfering with each other
- Tests must use `Application.Init` which sets up global state
- Tests cannot be truly parallelizable when testing full mouse event routing

## Current Architecture

The current architecture already has a good foundation with `IApplication` and `ApplicationImpl`:

```csharp
public interface IApplication
{
    IMouseGrabHandler MouseGrabHandler { get; set; }
    ITimedEvents? TimedEvents { get; }
    // ... other methods
}

public class ApplicationImpl : IApplication
{
    public IMouseGrabHandler MouseGrabHandler { get; set; } = new MouseGrabHandler();
    // ... instance-based implementation
}
```

**Key Pattern**: `Application.MouseGrabHandler` is implemented as:
```csharp
public static IMouseGrabHandler MouseGrabHandler
{
    get => ApplicationImpl.Instance.MouseGrabHandler;
    set => ApplicationImpl.Instance.MouseGrabHandler = value;
}
```

This delegates to the instance, allowing different instances in different test contexts.

## Proposed Solution

Follow the same pattern established by `MouseGrabHandler` to refactor mouse event handling.

### Step 1: Create IMouseEventHandler Interface

Create `Terminal.Gui/App/Mouse/IMouseEventHandler.cs`:

```csharp
#nullable enable
namespace Terminal.Gui.App;

/// <summary>
/// Handles mouse event processing and state for an Application instance.
/// </summary>
public interface IMouseEventHandler
{
    /// <summary>
    /// Gets or sets the last recorded mouse position.
    /// </summary>
    Point? LastMousePosition { get; set; }
    
    /// <summary>
    /// Gets or sets whether mouse input is disabled.
    /// </summary>
    bool IsMouseDisabled { get; set; }
    
    /// <summary>
    /// Raised when a mouse event occurs at the application level.
    /// </summary>
    event EventHandler<MouseEventArgs>? MouseEvent;
    
    /// <summary>
    /// Gets the list of views currently under the mouse (for enter/leave tracking).
    /// </summary>
    List<View?> CachedViewsUnderMouse { get; }
    
    /// <summary>
    /// Processes a mouse event and routes it to the appropriate views.
    /// </summary>
    /// <param name="mouseEvent">The mouse event with screen-relative coordinates.</param>
    /// <param name="top">The top-level view for the application context.</param>
    /// <param name="popover">The popover manager for the application context.</param>
    /// <param name="mouseGrabHandler">The mouse grab handler for the application context.</param>
    void RaiseMouseEvent (
        MouseEventArgs mouseEvent, 
        View? top, 
        IPopover? popover,
        IMouseGrabHandler mouseGrabHandler);
}
```

### Step 2: Implement MouseEventHandler  

Create `Terminal.Gui/App/Mouse/MouseEventHandler.cs`:

```csharp
#nullable enable
namespace Terminal.Gui.App;

/// <summary>
/// Default implementation of IMouseEventHandler.
/// </summary>
public class MouseEventHandler : IMouseEventHandler
{
    public Point? LastMousePosition { get; set; }
    public bool IsMouseDisabled { get; set; }
    public event EventHandler<MouseEventArgs>? MouseEvent;
    public List<View?> CachedViewsUnderMouse { get; } = new();
    
    public void RaiseMouseEvent (
        MouseEventArgs mouseEvent, 
        View? top, 
        IPopover? popover,
        IMouseGrabHandler mouseGrabHandler)
    {
        // Move implementation from Application.RaiseMouseEvent
        // Replace all static references with parameters
        if (Application.Initialized)
        {
            LastMousePosition = mouseEvent.ScreenPosition;
        }
        
        if (IsMouseDisabled)
        {
            return;
        }
        
        mouseEvent.Position = mouseEvent.ScreenPosition;
        
        List<View?> currentViewsUnderMouse = View.GetViewsUnderLocation (
            mouseEvent.ScreenPosition, 
            ViewportSettingsFlags.TransparentMouse);
        
        View? deepestViewUnderMouse = currentViewsUnderMouse.LastOrDefault();
        
        if (deepestViewUnderMouse is { })
        {
            mouseEvent.View = deepestViewUnderMouse;
        }
        
        MouseEvent?.Invoke (null, mouseEvent);
        
        if (mouseEvent.Handled)
        {
            return;
        }
        
        // Dismiss popover - use parameter instead of static
        if (mouseEvent.IsPressed
            && popover?.GetActivePopover() as View is { Visible: true } visiblePopover
            && View.IsInHierarchy (visiblePopover, deepestViewUnderMouse, includeAdornments: true) is false)
        {
            ApplicationPopover.HideWithQuitCommand (visiblePopover);
            RaiseMouseEvent (mouseEvent, top, popover, mouseGrabHandler);
            return;
        }
        
        if (HandleMouseGrab (deepestViewUnderMouse, mouseEvent, mouseGrabHandler))
        {
            return;
        }
        
        if (deepestViewUnderMouse is null)
        {
            return;
        }
        
        // Use parameter instead of static Application.Top
        if (!View.IsInHierarchy (top, deepestViewUnderMouse, true) 
            && !View.IsInHierarchy (popover?.GetActivePopover() as View, deepestViewUnderMouse, true))
        {
            return;
        }
        
        // Rest of implementation continues...
        // (Route mouse event to views, handle enter/leave, etc.)
    }
    
    private bool HandleMouseGrab (
        View? deepestViewUnderMouse, 
        MouseEventArgs mouseEvent, 
        IMouseGrabHandler mouseGrabHandler)
    {
        // Move implementation from Application.HandleMouseGrab
        // Replace static MouseGrabHandler with parameter
        if (mouseGrabHandler.MouseGrabView is { })
        {
            Point frameLoc = mouseGrabHandler.MouseGrabView.ScreenToViewport (mouseEvent.ScreenPosition);
            
            var viewRelativeMouseEvent = new MouseEventArgs
            {
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse ?? mouseGrabHandler.MouseGrabView
            };
            
            if (mouseGrabHandler.MouseGrabView?.NewMouseEvent (viewRelativeMouseEvent) is true)
            {
                return true;
            }
            
            if (mouseGrabHandler.MouseGrabView is null && deepestViewUnderMouse is Adornment)
            {
                return true;
            }
        }
        
        return false;
    }
}
```

### Step 3: Add to IApplication Interface

Update `Terminal.Gui/App/IApplication.cs`:

```csharp
public interface IApplication
{
    IMouseGrabHandler MouseGrabHandler { get; set; }
    IMouseEventHandler MouseEventHandler { get; set; }  // NEW
    ITimedEvents? TimedEvents { get; }
    // ... other methods
}
```

### Step 4: Update ApplicationImpl

Update `Terminal.Gui/App/ApplicationImpl.cs`:

```csharp
public class ApplicationImpl : IApplication
{
    public IMouseGrabHandler MouseGrabHandler { get; set; } = new MouseGrabHandler();
    public IMouseEventHandler MouseEventHandler { get; set; } = new MouseEventHandler();  // NEW
    // ... rest of implementation
}
```

### Step 5: Update Application Static Class

Update `Terminal.Gui/App/Application.Mouse.cs`:

```csharp
public static partial class Application
{
    // Add static gateway to instance
    public static IMouseEventHandler MouseEventHandler
    {
        get => ApplicationImpl.Instance.MouseEventHandler;
        set => ApplicationImpl.Instance.MouseEventHandler = value;
    }
    
    // Update existing static properties to delegate to instance
    internal static Point? LastMousePosition
    {
        get => MouseEventHandler.LastMousePosition;
        set => MouseEventHandler.LastMousePosition = value;
    }
    
    public static Point? GetLastMousePosition() => MouseEventHandler.LastMousePosition;
    
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool IsMouseDisabled
    {
        get => MouseEventHandler.IsMouseDisabled;
        set => MouseEventHandler.IsMouseDisabled = value;
    }
    
    // Update RaiseMouseEvent to delegate to instance
    internal static void RaiseMouseEvent (MouseEventArgs mouseEvent)
    {
        MouseEventHandler.RaiseMouseEvent (mouseEvent, Top, Popover, MouseGrabHandler);
    }
    
    // Update static event to delegate to instance
    public static event EventHandler<MouseEventArgs>? MouseEvent
    {
        add => MouseEventHandler.MouseEvent += value;
        remove => MouseEventHandler.MouseEvent -= value;
    }
    
    // Update internal methods to use instance
    internal static bool HandleMouseGrab (View? deepestViewUnderMouse, MouseEventArgs mouseEvent)
    {
        return MouseEventHandler.HandleMouseGrab (deepestViewUnderMouse, mouseEvent, MouseGrabHandler);
    }
    
    internal static List<View?> CachedViewsUnderMouse => MouseEventHandler.CachedViewsUnderMouse;
}
```

## Benefits

1. **True Concurrency**: Each test can create its own `ApplicationImpl` instance with isolated mouse state
2. **No Static Pollution**: Tests don't interfere with each other's mouse event tracking
3. **Backward Compatible**: Existing code using static `Application` methods continues to work
4. **Testability**: Tests can inject mock `IMouseEventHandler` implementations
5. **Follows Existing Pattern**: Matches the pattern established by `MouseGrabHandler` and `TimedEvents`

## Example: Truly Concurrent Test

```csharp
[Fact]
public void RaiseMouseEvent_WorksConcurrently ()
{
    // Create isolated application instance for this test
    var appImpl = new ApplicationImpl();
    
    // Create isolated mouse handler for this test
    var mouseHandler = new MouseEventHandler();
    appImpl.MouseEventHandler = mouseHandler;
    
    // Create view hierarchy for this test
    var top = new Toplevel { Width = 80, Height = 25 };
    var movableView = new View 
    { 
        Arrangement = ViewArrangement.Movable,
        BorderStyle = LineStyle.Single,
        X = 10, Y = 10, Width = 20, Height = 10
    };
    top.Add (movableView);
    
    // Simulate mouse press using FULL application-level routing
    var pressEvent = new MouseEventArgs 
    { 
        ScreenPosition = new (11, 10),
        Flags = MouseFlags.Button1Pressed 
    };
    
    // Use instance method - completely isolated from other tests!
    mouseHandler.RaiseMouseEvent (pressEvent, top, null, appImpl.MouseGrabHandler);
    
    // Verify application-level routing worked
    Assert.Equal (new Point (11, 10), mouseHandler.LastMousePosition);
    Assert.NotNull (pressEvent.View);
    
    // This test runs completely isolated from other tests!
    // No interference with Application.Top, Application.Popover, etc.
}
```

## Migration Strategy

### Phase 1: Create Interfaces and Implementation (This PR)
- [ ] Create `IMouseEventHandler` interface
- [ ] Create `MouseEventHandler` implementation
- [ ] Add property to `IApplication`
- [ ] Update `ApplicationImpl`
- [ ] Update `Application` static class to delegate
- [ ] All existing code continues to work unchanged

### Phase 2: Update Tests (Future PR)
- [ ] Add concurrent tests using instance methods
- [ ] Gradually migrate existing tests
- [ ] No requirement to update all tests immediately

### Phase 3: Documentation (Future PR)
- [ ] Update architecture docs
- [ ] Add examples of concurrent testing
- [ ] Document migration path

## Open Questions

1. **Application.Top and Application.Popover**: Should these also be moved to instance-based?
   - **Recommendation**: Yes, but as separate refactorings. For now, pass as parameters.
   
2. **View.GetViewsUnderLocation**: Uses `Application.Navigation` statically.
   - **Recommendation**: Address in future refactoring focused on navigation.

3. **Backward Compatibility**: Keep static API forever?
   - **Recommendation**: Yes - static API is convenient and follows C# conventions.

## Estimated Effort

- Interface creation: 1 hour
- MouseEventHandler implementation: 2-3 hours
- Application updates: 1-2 hours
- Testing: 2-3 hours
- **Total: 6-9 hours**

## Related Work

This proposal aligns with the ongoing v2 architecture migration:
- ✅ `IMouseGrabHandler` / `MouseGrabHandler` (completed)
- ✅ `ITimedEvents` / `TimedEvents` (completed)
- 🔄 `IMouseEventHandler` / `MouseEventHandler` (this proposal)
- 📝 `IKeyboardEventHandler` (future)
- 📝 `IViewHierarchyManager` (future - for Top, Popover, Navigation)
