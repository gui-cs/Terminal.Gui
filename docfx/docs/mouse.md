# Mouse API

## See Also

* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Command Deep Dive](command.md)
* [Keyboard Deep Dive](keyboard.md)
* [Lexicon & Taxonomy](lexicon.md)

## Tenets for Terminal.Gui Mouse Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Keyboard Required; Mouse Optional** - Terminal users expect full functionality without having to pick up the mouse. At the same time they love being able to use the mouse when it makes sense to do so. We strive to ensure anything that can be done with the keyboard is also possible with the mouse. We avoid features that are only useable with the mouse.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to mouse input in a way that is consistent with the platform. For example, on Windows, right-click typically shows context menus, double-click activates items, and the mouse wheel scrolls content. On other platforms, Terminal.Gui respects the platform's conventions for mouse interactions.

## Mouse APIs

*Terminal.Gui* provides the following APIs for handling mouse input:

* **MouseEventArgs** - @Terminal.Gui.Input.MouseEventArgs provides a platform-independent abstraction for common mouse operations. It is used for processing mouse input and raising mouse events.

* **Mouse Bindings** - Mouse Bindings provide a declarative method for handling mouse input in View implementations. The View calls @Terminal.Gui.ViewBase.View.AddCommand to declare it supports a particular command and then uses @Terminal.Gui.Input.MouseBindings to indicate which mouse events will invoke the command. 

* **Mouse Events** - The Mouse Bindings API is rich enough to support the majority of use-cases. However, in some cases subscribing directly to mouse events is needed (e.g. drag & drop). Use @Terminal.Gui.ViewBase.View.MouseEvent and related events in these cases.

* **Mouse State** - @Terminal.Gui.ViewBase.View.MouseState provides an abstraction for the current state of the mouse, enabling views to do interesting things like change their appearance based on the mouse state.

Each of these APIs are described more fully below.

## Mouse Bindings

Mouse Bindings is the preferred way of handling mouse input in View implementations. The View calls @Terminal.Gui.ViewBase.View.AddCommand to declare it supports a particular command and then uses @Terminal.Gui.Input.MouseBindings to indicate which mouse events will invoke the command. For example, if a View wants to respond to the user using the mouse wheel to scroll up, it would do this:

```cs
public class MyView : View
{
    public MyView()
    {
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        MouseBindings.Add (MouseFlags.WheelUp, Command.ScrollUp);
        
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        MouseBindings.Add (MouseFlags.WheelDown, Command.ScrollDown);
        
        // Mouse clicks invoke Command.Select by default
        // Override to customize click behavior
        AddCommand (Command.Select, () => {
            SelectItem();
            return true;
        });
    }
}
```

The @Terminal.Gui.Input.Command enum lists generic operations that are implemented by views. 

### Common Mouse Bindings

Here are some common mouse binding patterns used throughout Terminal.Gui:

* **Click Events**: `MouseFlags.Button1Clicked` for primary selection/activation - maps to `Command.Select` by default
* **Double-Click Events**: `MouseFlags.Button1DoubleClicked` for default actions (like opening/accepting)
* **Right-Click Events**: `MouseFlags.Button3Clicked` for context menus
* **Scroll Events**: `MouseFlags.WheelUp` and `MouseFlags.WheelDown` for scrolling content
* **Drag Events**: `MouseFlags.Button1Pressed` combined with mouse move tracking for drag operations

### Default Mouse Bindings

By default, all views have the following mouse bindings configured:

```cs
MouseBindings.Add (MouseFlags.Button1Clicked, Command.Select);
MouseBindings.Add (MouseFlags.Button2Clicked, Command.Select);
MouseBindings.Add (MouseFlags.Button3Clicked, Command.Select);
MouseBindings.Add (MouseFlags.Button4Clicked, Command.Select);
MouseBindings.Add (MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl, Command.Select);
```

When a mouse click occurs, the `Command.Select` is invoked, which raises the `Selecting` event. Views can override `OnSelecting` or subscribe to the `Selecting` event to handle clicks:

```cs
public class MyView : View
{
    public MyView()
    {
        // Option 1: Subscribe to Selecting event
        Selecting += (s, e) =>
        {
            if (e.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs })
            {
                // Access mouse position and flags
                HandleSelection(mouseArgs.Position, mouseArgs.Flags);
                e.Handled = true;
            }
        };
    }
    
    // Option 2: Override OnSelecting
    protected override bool OnSelecting(CommandEventArgs args)
    {
        if (args.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs })
        {
            // Custom selection logic with mouse position
            if (mouseArgs.Position.Y == 0)
            {
                HandleHeaderClick();
                return true;
            }
        }
        return base.OnSelecting(args);
    }
}
```

## Mouse Events

At the core of *Terminal.Gui*'s mouse API is the @Terminal.Gui.Input.MouseEventArgs class. The @Terminal.Gui.Input.MouseEventArgs class provides a platform-independent abstraction for common mouse events. Every mouse event can be fully described in a @Terminal.Gui.Input.MouseEventArgs instance, and most of the mouse-related APIs are simply helper functions for decoding a @Terminal.Gui.Input.MouseEventArgs.

When the user does something with the mouse, the driver maps the platform-specific mouse event into a `MouseEventArgs` and calls `IApplication.Mouse.RaiseMouseEvent`. Then, `IApplication.Mouse.RaiseMouseEvent` determines which `View` the event should go to. The `View.OnMouseEvent` method can be overridden or the `View.MouseEvent` event can be subscribed to, to handle the low-level mouse event. If the low-level event is not handled by a view, `IApplication` will then call the appropriate high-level helper APIs.

### Mouse Event Processing Flow

Mouse events are processed through the following workflow using the [Cancellable Work Pattern](cancellable-work-pattern.md):

1. **Driver Level**: The driver captures platform-specific mouse events and converts them to `MouseEventArgs`
2. **Application Level**: `IApplication.Mouse.RaiseMouseEvent` determines the target view and routes the event
3. **View Level**: The target view processes the event through `View.NewMouseEvent()`:
   1. **Pre-condition validation** - Checks if view is enabled, visible, and wants the event type
   2. **Low-level MouseEvent** - Raises `OnMouseEvent()` and `MouseEvent` event
   3. **Mouse grab handling** - If `HighlightStates` or `WantContinuousButtonPressed` are set:
      - Automatically grabs mouse on button press
      - Handles press/release/click lifecycle
      - Sets focus if view is focusable
      - Updates `MouseState` (Pressed, PressedOutside)
   4. **Command invocation** - For click events, invokes commands via `MouseBindings` (default: `Command.Select` ? `Selecting` event)
   5. **Mouse wheel handling** - Raises `OnMouseWheel()` and `MouseWheel` event

### Handling Mouse Events Directly

For scenarios requiring direct mouse event handling (such as custom drag-and-drop operations), subscribe to the `MouseEvent` or override `OnMouseEvent`:

```cs
public class CustomView : View
{
    public CustomView()
    {
        MouseEvent += OnMouseEventHandler;
    }
    
    private void OnMouseEventHandler(object sender, MouseEventArgs e)
    {
        if (e.Flags.HasFlag(MouseFlags.Button1Pressed))
        {
            // Handle drag start
            e.Handled = true;
        }
    }
    
    // Alternative: Override the virtual method
    protected override bool OnMouseEvent(MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag(MouseFlags.Button1Pressed))
        {
            // Handle drag start
            return true; // Event was handled
        }
        return base.OnMouseEvent(mouseEvent);
    }
}
```

### Handling Mouse Clicks

The recommended pattern for handling mouse clicks is to use the `Selecting` event or override `OnSelecting`. This integrates with the command system and provides access to mouse event details through the command context:

```cs
public class ClickableView : View
{
    public ClickableView()
    {
        Selecting += OnSelecting;
    }
    
    private void OnSelecting(object sender, CommandEventArgs e)
    {
        // Extract mouse event information from command context
        if (e.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs })
        {
            // Access mouse position (viewport-relative)
            Point clickPosition = mouseArgs.Position;
            
            // Check which button was clicked
            if (mouseArgs.Flags.HasFlag(MouseFlags.Button1Clicked))
            {
                HandleLeftClick(clickPosition);
            }
            else if (mouseArgs.Flags.HasFlag(MouseFlags.Button3Clicked))
            {
                ShowContextMenu(clickPosition);
            }
            
            e.Handled = true;
        }
    }
}
```

For views that need different behavior for different mouse buttons, configure custom mouse bindings:

```cs
public class MultiButtonView : View
{
    public MultiButtonView()
    {
        // Clear default bindings
        MouseBindings.Clear();
        
        // Map different buttons to different commands
        MouseBindings.Add(MouseFlags.Button1Clicked, Command.Select);
        MouseBindings.Add(MouseFlags.Button3Clicked, Command.ContextMenu);
        
        AddCommand(Command.ContextMenu, HandleContextMenu);
    }
    
    private bool HandleContextMenu()
    {
        // Show context menu
        return true;
    }
}
```

## Mouse State and Mouse Grab

### Mouse State

The @Terminal.Gui.ViewBase.View.MouseState property provides an abstraction for the current state of the mouse, enabling views to do interesting things like change their appearance based on the mouse state.

Mouse states include:
* **None** - No mouse interaction with the view
* **In** - Mouse is positioned over the view (inside the viewport)
* **Pressed** - Mouse button is pressed down while over the view
* **PressedOutside** - Mouse was pressed inside but moved outside the view (when not using `WantContinuousButtonPressed`)

It works in conjunction with the @Terminal.Gui.ViewBase.View.HighlightStates which is a list of mouse states that will cause a view to become highlighted.

Subscribe to the @Terminal.Gui.ViewBase.View.MouseStateChanged event to be notified when the mouse state changes:

```cs
view.MouseStateChanged += (sender, e) => 
{
    switch (e.Value)
    {
        case MouseState.In:
            // Change appearance when mouse hovers
            break;
        case MouseState.Pressed:
            // Change appearance when pressed
            break;
        case MouseState.PressedOutside:
            // Mouse was pressed inside but moved outside
            break;
    }
};
```

Configure which states should cause highlighting:

```cs
// Highlight when mouse is over the view or when pressed
view.HighlightStates = MouseState.In | MouseState.Pressed;
```

### Mouse Grab

Views with `HighlightStates` or `WantContinuousButtonPressed` enabled automatically **grab the mouse** when a button is pressed. This means:

1. **Automatic Grab**: The view receives all mouse events until the button is released, even if the mouse moves outside the view's `Viewport`
2. **Focus Management**: If the view is focusable (`CanFocus = true`), it automatically receives focus on the first button press
3. **State Tracking**: The view's `MouseState` is updated to reflect press/release/outside states
4. **Automatic Ungrab**: The mouse is released when:
   - The button is released (via `WhenGrabbedHandleClicked()`)
   - The view is removed from its parent hierarchy (via `View.OnRemoved()`)
   - The application ends (via `App.End()`)

#### Continuous Button Press

When `WantContinuousButtonPressed` is set to `true`, the view receives repeated click events while the button is held down:

```cs
view.WantContinuousButtonPressed = true;

view.Selecting += (s, e) =>
{
    // This will be called repeatedly while the button is held down
    // Useful for scroll buttons, increment/decrement buttons, etc.
    DoRepeatAction();
    e.Handled = true;
};
```

**Note**: With `WantContinuousButtonPressed`, the `MouseState.PressedOutside` flag has no effect - the view continues to receive events and maintains the pressed state even when the mouse moves outside.

#### Mouse Grab Lifecycle

```
Button Press (inside view)
    ?
Mouse Grabbed Automatically
    ?? View receives focus (if CanFocus)
    ?? MouseState |= MouseState.Pressed
    ?? All mouse events route to this view
    
Mouse Move (while grabbed)
    ?? Inside Viewport: MouseState remains Pressed
    ?? Outside Viewport: MouseState |= MouseState.PressedOutside
        (unless WantContinuousButtonPressed is true)
    
Button Release
    ?
Mouse Ungrabbed Automatically
    ?? MouseState &= ~MouseState.Pressed
    ?? MouseState &= ~MouseState.PressedOutside
    ?? Click event raised (if still in bounds)
```

## Mouse Button and Movement Concepts

* **Down** - Indicates the user pushed a mouse button down.
* **Pressed** - Indicates the mouse button is down; for example if the mouse was pressed down and remains down for a period of time.
* **Released** - Indicates the user released a mouse button.
* **Clicked** - Indicates the user pressed then released the mouse button while over a particular View. 
* **Double-Clicked** - Indicates the user clicked twice in rapid succession.
* **Triple-Clicked** - Indicates the user clicked three times in rapid succession.
* **Moved** - Indicates the mouse moved to a new location since the last mouse event.
* **Wheel** - Indicates the mouse wheel was scrolled up or down.

## Global Mouse Handling

The @Terminal.Gui.App.Application.MouseEvent event can be used if an application wishes to receive all mouse events before they are processed by individual views:

```csharp
App.Mouse.MouseEvent += (sender, e) => 
{
    // Handle application-wide mouse events
    if (e.Flags.HasFlag(MouseFlags.Button3Clicked))
    {
        ShowGlobalContextMenu(e.Position);
        e.Handled = true;
    }
};
```

For view-specific mouse handling that needs access to application context, use `View.App`:

```csharp
public class MyView : View
{
    protected override bool OnMouseEvent(MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked))
        {
            // Access application mouse functionality through View.App
            App?.Mouse?.RaiseMouseEvent(mouseEvent);
            return true;
        }
        return base.OnMouseEvent(mouseEvent);
    }
}
```

## Mouse Enter/Leave Events

The @Terminal.Gui.ViewBase.View.MouseEnter and @Terminal.Gui.ViewBase.View.MouseLeave events enable a View to take action when the mouse enters or exits the view boundary. Internally, this is used to enable @Terminal.Gui.ViewBase.View.Highlight functionality:

```cs
view.MouseEnter += (sender, e) => 
{
    // Mouse entered the view
    UpdateTooltip("Hovering over button");
};

view.MouseLeave += (sender, e) => 
{
    // Mouse left the view  
    HideTooltip();
};
```

## Mouse Coordinate Systems

Mouse coordinates in Terminal.Gui are provided in multiple coordinate systems:

* **Screen Coordinates** - Relative to the entire terminal screen (0,0 is top-left of terminal) - available via `MouseEventArgs.ScreenPosition`
* **View Coordinates** - Relative to the view's viewport (0,0 is top-left of view's viewport) - available via `MouseEventArgs.Position`

The `MouseEventArgs` provides both coordinate systems:
* `MouseEventArgs.ScreenPosition` - Screen coordinates (absolute position on screen)
* `MouseEventArgs.Position` - Viewport-relative coordinates (position within the view's content area)

When handling mouse events in views, use `Position` for viewport-relative coordinates:

```cs
view.MouseEvent += (s, e) =>
{
    // e.Position is viewport-relative
    if (e.Position.X < 10 && e.Position.Y < 5)
    {
        // Click in top-left corner of viewport
    }
};
```

## Best Practices

* **Use Mouse Bindings and Commands** for simple mouse interactions - they integrate well with the Command system and work alongside keyboard bindings
* **Use the `Selecting` event** to handle mouse clicks - it's raised by the default `Command.Select` binding for all mouse buttons
* **Access mouse details via CommandContext** when you need position or flags in `Selecting` handlers:
  ```cs
  view.Selecting += (s, e) =>
  {
      if (e.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs })
      {
          Point position = mouseArgs.Position;
          MouseFlags flags = mouseArgs.Flags;
          // Handle with position and flags
      }
  };
  ```
* **Handle Mouse Events directly** only for complex interactions like drag-and-drop or custom gestures (override `OnMouseEvent` or subscribe to `MouseEvent`)
* **Use `HighlightStates`** to enable automatic mouse grab and visual feedback - views will automatically grab the mouse and update their appearance
* **Use `WantContinuousButtonPressed`** for repeating actions (scroll buttons, increment/decrement) - the view will receive repeated events while the button is held
* **Respect platform conventions** - use right-click for context menus, double-click for default actions
* **Provide keyboard alternatives** - ensure all mouse functionality has keyboard equivalents
* **Test with different terminals** - mouse support varies between terminal applications
* **Mouse grab is automatic** - you don't need to manually call `GrabMouse()`/`UngrabMouse()` when using `HighlightStates` or `WantContinuousButtonPressed`

## Limitations and Considerations

* Not all terminal applications support mouse input - always provide keyboard alternatives
* Mouse wheel support may vary between platforms and terminals
* Some terminals may not support all mouse buttons or modifier keys
* Mouse coordinates are limited to character cell boundaries - sub-character precision is not available
* Performance can be impacted by excessive mouse move event handling - use mouse enter/leave events when appropriate rather than tracking all mouse moves