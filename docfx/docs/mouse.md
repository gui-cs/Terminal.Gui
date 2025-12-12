# Mouse Deep Dive

## Table of Contents

- [Tenets for Terminal.Gui Mouse Handling](#tenets-for-terminalgui-mouse-handling-unless-you-know-better-ones)
- [Mouse Behavior - End User's Perspective](#mouse-behavior---end-users-perspective)
- [Mouse APIs](#mouse-apis)
- [Mouse Bindings](#mouse-bindings)
  - [Common Mouse Bindings](#common-mouse-bindings)
  - [Default Mouse Bindings](#default-mouse-bindings)
- [Mouse Events](#mouse-events)
  - [Mouse Event Processing Flow](#mouse-event-processing-flow)
  - [Handling Mouse Events Directly](#handling-mouse-events-directly)
  - [Handling Mouse Clicks](#handling-mouse-clicks)
- [Mouse State and Mouse Grab](#mouse-state-and-mouse-grab)
  - [Mouse State](#mouse-state)
  - [Mouse Grab](#mouse-grab)
    - [Continuous Button Press](#continuous-button-press)
    - [Mouse Grab Lifecycle](#mouse-grab-lifecycle)
- [Mouse Button and Movement Concepts](#mouse-button-and-movement-concepts)
- [Global Mouse Handling](#global-mouse-handling)
- [Mouse Enter/Leave Events](#mouse-enterleave-events)
- [Mouse Coordinate Systems](#mouse-coordinate-systems)
- [Best Practices](#best-practices)
- [Limitations and Considerations](#limitations-and-considerations)
- [How Drivers Work](#how-drivers-work)
  - [Complete Mouse Event Pipeline](#complete-mouse-event-pipeline) 🔥 **START HERE for pipeline understanding**
  - [Input Processing Architecture](#input-processing-architecture)
  - [Platform-Specific Input Processors](#platform-specific-input-processors)
  - [Mouse Event Generation](#mouse-event-generation)
  - [ANSI Mouse Parsing](#ansi-mouse-parsing)
  - [Event Flow](#event-flow)
  - [Recommended Pipeline Improvements](#recommended-pipeline-improvements)

> **Quick Reference:** See [Mouse Pipeline Summary](mouse-pipeline-summary.md) for a condensed overview of the complete pipeline from ANSI input to command execution.

## Tenets for Terminal.Gui Mouse Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Keyboard Required; Mouse Optional** - Terminal users expect full functionality without having to pick up the mouse. At the same time they love being able to use the mouse when it makes sense to do so. We strive to ensure anything that can be done with the keyboard is also possible with the mouse. We avoid features that are only useable with the mouse.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to mouse input in a way that is consistent with the platform. For example, on Windows, right-click typically shows context menus, double-click activates items, and the mouse wheel scrolls content. On other platforms, Terminal.Gui respects the platform's conventions for mouse interactions.

## Mouse Behavior - End User's Perspective

### Button

| Scenario                                      | Visual pressed state                                      | `Command.Accept` invocations                                                                 | `Command.Activate` invocations | Rationale & Rule |
|-----------------------------------------------|-----------------------------------------------------------|-----------------------------------------------------------------------------------------------|---------------------------------|------------------|
| Simple single click (press + release inside)  | Pressed on **LeftButtonPressed** → stays until **LeftButtonReleased** (anywhere) | **Exactly 1** on release inside the button                                           | Never                           | Universal UI contract |
| Hold mouse (MouseHoldRepeat = **false** – default) | Pressed immediately → stays until release                 | **Exactly 1** on release inside                                                       | Never                           | Normal push-button |
| Hold mouse (MouseHoldRepeat = **true**) | Same visual behavior                                      | Starts repeating after ~300 ms, then ~30–60 ms **only while cursor remains inside**    | Never                           | Scrollbar arrow / spin button behavior |
| Drag outside while holding → release outside  | Visual pressed cleared on any **LeftButtonReleased**        | **None** (canceled)                                                                   | Never                           | Standard click cancellation |
| **Double-click** (MouseHoldRepeat = **false**) | Normal press → release → press → release cycle          | **Exactly 2** (one per release inside)                                               | Never                           | Required – users double-click buttons constantly |
| **Double-click** (MouseHoldRepeat = **true**) | Same visual cycle                                         | **Exactly 2** (repeating only applies to continuous hold, not discrete clicks)       | Never                           | Repeating ≠ double-click |
| Triple-click or faster multi-click            | Same rule                                                 | One `Accept` per release inside → 3 Accepts on triple-click                          | Never                           | No coalescing for normal buttons |

This behavior matches Qt QPushButton, GTK Button, Win32 BUTTON, WPF Button, NSButton, Flutter ElevatedButton, Android Button, etc. – all established since the 1990s.

### ListView

| Scenario                                      | Visual selection state                                    | `Command.Accept` invocations                                                                 | `Command.Activate` invocations | Rationale & Rule |
|-----------------------------------------------|-----------------------------------------------------------|----------------------------------------------------------------------------------------------|--------------------------------|------------------|
| Simple single click (press + release inside)  | Item selected immediately on **LeftButtonClicked**          | Never                                                                                        | **Exactly 1** on click         | Selection happens on click |
| Hold mouse on item                            | Selection changes immediately on click                    | Never                                                                                        | **Exactly 1** on initial click | No continuous action |
| Click different items rapidly                 | Selection updates with each click                         | Never                                                                                        | **Exactly 1** per click        | Each click selects new item |
| Drag outside while holding → release outside  | Selection remains on last clicked item                    | Never                                                                                        | Never                          | Drag doesn't change selection |
| **Double-click** on item                      | Item selected on first click → stays selected            | **Exactly 1** on second click (opens/enters item)                                           | **Exactly 1** on first click (selects) | Standard file browser behavior |
| Triple-click on item                          | Item selected → remains selected through all clicks      | **Exactly 1** on second click only                                                          | **Exactly 1** on first click, **Exactly 1** on third click | Only first double-click fires Accept |
| Click on empty space (no item)                | Deselect current selection                                | Never                                                                                        | Never                          | Click on background clears selection |
| **Enter key** when item selected              | No change (item already selected)                         | **Exactly 1** (opens/enters selected item)                                                  | Never                          | Keyboard equivalent of double-click |


## Mouse APIs

*Terminal.Gui* provides the following APIs for handling mouse input:

* **Mouse** - @Terminal.Gui.Input.Mouse provides a platform-independent abstraction for common mouse operations. It is used for processing mouse input and raising mouse events.

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
        
        // Mouse clicks invoke Command.Activate by default
        // Override to customize click behavior
        AddCommand (Command.Activate, () => {
            SelectItem();
            return true;
        });
    }
}
```

The @Terminal.Gui.Input.Command enum lists generic operations that are implemented by views. 

### Common Mouse Bindings

Here are some common mouse binding patterns used throughout Terminal.Gui:

* **Click Events**: `MouseFlags.LeftButtonClicked` for primary selection/activation - maps to `Command.Activate` by default
* **Double-Click Events**: `MouseFlags.LeftButtonDoubleClicked` for default actions (like opening/accepting)
* **Right-Click Events**: `MouseFlags.RightButtonClicked` for context menus
* **Scroll Events**: `MouseFlags.WheelUp` and `MouseFlags.WheelDown` for scrolling content
* **Drag Events**: `MouseFlags.LeftButtonPressed` combined with mouse move tracking for drag operations

### Default Mouse Bindings

By default, all views have the following mouse bindings configured:

```cs
MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);
MouseBindings.Add (MouseFlags.MiddleButtonClicked, Command.Activate);
MouseBindings.Add (MouseFlags.RightButtonClicked, Command.Activate);
MouseBindings.Add (MouseFlags.Button4Clicked, Command.Activate);
MouseBindings.Add (MouseFlags.LeftButtonClicked | MouseFlags.ButtonCtrl, Command.Activate);
```

When a mouse click occurs, the `Command.Activate` is invoked, which raises the `Activating` event. Views can override `OnActivating` or subscribe to the `Activating` event to handle clicks:

```cs
public class MyView : View
{
    public MyView()
    {
        // Option 1: Subscribe to Activating event
        Activating += (s, e) =>
        {
            if (e.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs })
            {
                // Access mouse position and flags
                HandleSelection(mouseArgs.Position, mouseArgs.Flags);
                e.Handled = true;
            }
        };
    }
    
    // Option 2: Override OnActivating
    protected override bool OnActivating(CommandEventArgs args)
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
        return base.OnActivating(args);
    }
}
```

## Mouse Events

At the core of *Terminal.Gui*'s mouse API is the @Terminal.Gui.Input.Mouse class. The @Terminal.Gui.Input.Mouse class provides a platform-independent abstraction for common mouse events. Every mouse event can be fully described in a @Terminal.Gui.Input.Mouse instance, and most of the mouse-related APIs are simply helper functions for decoding a @Terminal.Gui.Input.Mouse.

When the user does something with the mouse, the driver maps the platform-specific mouse event into a `Mouse` and calls `IApplication.Mouse.RaiseMouseEvent`. Then, `IApplication.Mouse.RaiseMouseEvent` determines which `View` the event should go to. The `View.OnMouseEvent` method can be overridden or the `View.MouseEvent` event can be subscribed to, to handle the low-level mouse event. If the low-level event is not handled by a view, `IApplication` will then call the appropriate high-level helper APIs.

### Mouse Event Processing Flow

Mouse events are processed through the following workflow using the [Cancellable Work Pattern](cancellable-work-pattern.md):

1. **Driver Level**: The driver captures platform-specific mouse events and converts them to `Mouse`
2. **Application Level**: `IApplication.Mouse.RaiseMouseEvent` determines the target view and routes the event
3. **View Level**: The target view processes the event through `View.NewMouseEvent()`:
   1. **Pre-condition validation** - Checks if view is enabled, visible, and wants the event type
   2. **Low-level MouseEvent** - Raises `OnMouseEvent()` and `MouseEvent` event
   3. **Mouse grab handling** - If `MouseHighlightStates` or `MouseHoldRepeat` are set:
      - Automatically grabs mouse on button press
      - Handles press/release/click lifecycle
      - Sets focus if view is focusable
      - Updates `MouseState` (Pressed, PressedOutside)
   4. **Command invocation** - For click events, invokes commands via `MouseBindings` (default: `Command.Activate` → `Activating` event)
   5. **Mouse wheel handling** - Invokes commands bound to mouse wheel flags via `MouseBindings`

### Handling Mouse Events Directly

For scenarios requiring direct mouse event handling (such as custom drag-and-drop operations), subscribe to the `MouseEvent` or override `OnMouseEvent`:

```cs
public class CustomView : View
{
    public CustomView()
    {
        MouseEvent += OnMouseEventHandler;
    }
    
    private void OnMouseEventHandler(object sender, Mouse e)
    {
        if (e.Flags.HasFlag(MouseFlags.LeftButtonPressed))
        {
            // Handle drag start
            e.Handled = true;
        }
    }
    
    // Alternative: Override the virtual method
    protected override bool OnMouseEvent(Mouse mouse)
    {
        if (mouse.Flags.HasFlag(MouseFlags.LeftButtonPressed))
        {
            // Handle drag start
            return true;
        }
        return base.OnMouseEvent(mouse);
    }
}
```

### Handling Mouse Clicks

The recommended pattern for handling mouse clicks is to use the `Activating` event or override `OnActivating`. This integrates with the command system and provides access to mouse event details through the command context:

```cs
public class ClickableView : View
{
    public ClickableView()
    {
        Activating += OnActivating;
    }
    
    private void OnActivating(object sender, CommandEventArgs e)
    {
        // Extract mouse event information from command context
        if (e.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs })
        {
            // Access mouse position (viewport-relative)
            Point clickPosition = mouseArgs.Position;
            
            // Check which button was clicked
            if (mouseArgs.Flags.HasFlag(MouseFlags.LeftButtonClicked))
            {
                HandleLeftClick(clickPosition);
            }
            else if (mouseArgs.Flags.HasFlag(MouseFlags.RightButtonClicked))
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
        MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.Add(MouseFlags.RightButtonClicked, Command.ContextMenu);
        
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
* **PressedOutside** - Mouse was pressed inside but moved outside the view (when not using `MouseHoldRepeat`)

It works in conjunction with the @Terminal.Gui.ViewBase.View.MouseHighlightStates which is a list of mouse states that will cause a view to become highlighted.

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
view.MouseHighlightStates = MouseState.In | MouseState.Pressed;
```

### Mouse Grab

Views with `MouseHighlightStates` or `MouseHoldRepeat` enabled automatically **grab the mouse** when a button is pressed. This means:

1. **Automatic Grab**: The view receives all mouse events until the button is released, even if the mouse moves outside the view's `Viewport`
2. **Focus Management**: If the view is focusable (`CanFocus = true`), it automatically receives focus on the first button press
3. **State Tracking**: The view's `MouseState` is updated to reflect press/release/outside states
4. **Automatic Ungrab**: The mouse is released when:
   - The button is released (via `WhenGrabbedHandleClicked()`)
   - The view is removed from its parent hierarchy (via `View.OnRemoved()`)
   - The application ends (via `App.End()`)

#### Continuous Button Press

When `MouseHoldRepeat` is set to `true`, the view receives repeated click events while the button is held down:

```cs
view.MouseHoldRepeat = true;

view.Activating += (s, e) =>
{
    // This will be called repeatedly while the button is held down
    // Useful for scroll buttons, increment/decrement buttons, etc.
    DoRepeatAction();
    e.Handled = true;
};
```

**Note**: With `MouseHoldRepeat`, the `MouseState.PressedOutside` flag has no effect - the view continues to receive events and maintains the pressed state even when the mouse moves outside.

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
        (unless MouseHoldRepeat is true)
    
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
    if (e.Flags.HasFlag(MouseFlags.RightButtonClicked))
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
    protected override bool OnMouseEvent(Mouse mouse)
    {
        if (mouse.Flags.HasFlag(MouseFlags.RightButtonClicked))
        {
            // Access application mouse functionality through View.App
            App?.Mouse?.RaiseMouseEvent(mouse);
            return true;
        }
        return base.OnMouseEvent(mouse);
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

* **Screen Coordinates** - Relative to the entire terminal screen (0,0 is top-left of terminal) - available via `Mouse.ScreenPosition`
* **View Coordinates** - Relative to the view's viewport (0,0 is top-left of view's viewport) - available via `Mouse.Position` (nullable)

The `Mouse` provides both coordinate systems:
* `Mouse.ScreenPosition` - Screen coordinates (absolute position on screen)
* `Mouse.Position` - Viewport-relative coordinates (position within the view's content area)

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
* **Use the `Activating` event** to handle mouse clicks - it's raised by the default `Command.Activate` binding for all mouse buttons
* **Access mouse details via CommandContext** when you need position or flags in `Activating` handlers:
  ```cs
  view.Activating += (s, e) =>
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
* **Use `MouseHighlightStates`** to enable automatic mouse grab and visual feedback - views will automatically grab the mouse and update their appearance
* **Use `MouseHoldRepeat`** for repeating actions (scroll buttons, increment/decrement) - the view will receive repeated events while the button is held
* **Respect platform conventions** - use right-click for context menus, double-click for default actions
* **Provide keyboard alternatives** - ensure all mouse functionality has keyboard equivalents
* **Test with different terminals** - mouse support varies between terminal applications
* **Mouse grab is automatic** - you don't need to manually call `GrabMouse()`/`UngrabMouse()` when using `MouseHighlightStates` or `MouseHoldRepeat`

## Limitations and Considerations

* Not all terminal applications support mouse input - always provide keyboard alternatives
* Mouse wheel support may vary between platforms and terminals
* Some terminals may not support all mouse buttons or modifier keys
* Mouse coordinates are limited to character cell boundaries - sub-character precision is not available
* Performance can be impacted by excessive mouse move event handling - use mouse enter/leave events when appropriate rather than tracking all mouse moves

## How Drivers Work

The **Driver Level** is the first stage of mouse event processing, where platform-specific mouse events are captured and converted into a standardized `Mouse` instance that the rest of Terminal.Gui can process uniformly.

### Complete Mouse Event Pipeline

This section documents the complete flow from raw terminal input to View command execution.

```mermaid
sequenceDiagram
    participant Terminal as Terminal/Console
    participant Driver as ConsoleDriver
    participant AnsiParser as AnsiMouseParser
    participant Interpreter as MouseInterpreter
    participant InputProcessor as InputProcessorImpl
    participant AppMouse as IMouse (MouseImpl)
    participant View as View
    participant Commands as Command System

    Note over Terminal: User clicks mouse
    Terminal->>Driver: ANSI escape sequence<br/>ESC[<0;10;5M (press)<br/>ESC[<0;10;5m (release)
    
    Driver->>AnsiParser: ProcessMouseInput(ansiString)
    Note over AnsiParser: Parses button code, x, y, terminator<br/>Converts to 0-based coords<br/>Maps to MouseFlags
    AnsiParser->>Driver: Mouse { Flags=LeftButtonPressed, ScreenPosition=(9,4) }
    
    Driver->>InputProcessor: Queue mouse event
    InputProcessor->>Interpreter: Process(Mouse)
    Note over Interpreter: Tracks press/release pairs<br/>Generates clicked events<br/>Detects double/triple clicks<br/>Tracks timing & position
    
    Interpreter->>InputProcessor: Mouse { Flags=LeftButtonClicked, ... }
    
    InputProcessor->>AppMouse: RaiseMouseEvent(Mouse)
    Note over AppMouse: 1. Find deepest view under mouse<br/>2. Check for popover dismissal<br/>3. Handle mouse grab<br/>4. Convert to view coordinates<br/>5. Raise MouseEnter/Leave
    
    AppMouse->>View: NewMouseEvent(Mouse { Position=viewportRelative })
    
    Note over View: View Processing Pipeline:
    View->>View: 1. Pre-conditions (enabled, visible)
    View->>View: 2. RaiseMouseEvent → MouseEvent
    View->>View: 3. Mouse grab handling<br/>(if MouseHighlightStates or WantContinuous)
    View->>View: 4. Convert flags<br/>(Pressed→Clicked if needed)
    View->>Commands: 5. InvokeCommandsBoundToMouse
    Note over Commands: Default: LeftButtonClicked → Command.Activate
    Commands->>View: RaiseActivating/Accepting
    View->>View: OnActivating/OnAccepting
```

### Stage 1: Terminal Input (ANSI Escape Sequences)

**Input Format:** SGR Extended Mouse Mode (`ESC[<button;x;yM/m`)

**Example User Action:** Single click at column 10, row 5

```
Press:   ESC[<0;10;5M    (button=0, x=10, y=5, terminator='M')
Release: ESC[<0;10;5m    (button=0, x=10, y=5, terminator='m')
```

**Key Points:**
- Coordinates are **1-based** in ANSI (top-left = 1,1)
- `M` terminator = press, `m` terminator = release
- Button codes: 0=left, 1=middle, 2=right, 64/65=wheel up/down
- Modifiers encoded in button code (8=Alt, 16=Ctrl, 4=Shift)
- Motion tracking: button codes 32-34 with `PositionReport` flag

### Stage 2: ANSI Parsing (AnsiMouseParser)

**Location:** `Terminal.Gui/Drivers/AnsiHandling/AnsiMouseParser.cs`

**Responsibilities:**
1. Parse ANSI escape sequence using regex: `\u001b\[<(\d+);(\d+);(\d+)(M|m)`
2. Extract button code, x, y, terminator
3. Convert coordinates to **0-based** (subtract 1 from both x and y)
4. Map button code + terminator to `MouseFlags`:
   - Button codes 0-2 → `LeftButton/2/3` + `Pressed/Released` based on terminator
   - Button codes 64-65 → `WheeledUp/Down`
   - Button codes 68-69 → `WheeledLeft/Right`
   - Button codes 32-34 → Drag with `PositionReport` flag
   - Button codes 35-63 → Motion with `PositionReport`
5. Extract modifiers from button code (Alt=8, Ctrl=16, Shift=4 bit flags)
6. Create `Mouse` instance with `ScreenPosition` and `Flags`

**Output:** `Mouse { Timestamp=now, ScreenPosition=(9,4), Flags=LeftButtonPressed }`

**Code Location:** `AnsiMouseParser.ProcessMouseInput(string input)`

### Stage 3: Click Synthesis (MouseInterpreter)

**Location:** `Terminal.Gui/Drivers/MouseInterpreter.cs`

**Responsibilities:**
1. **Track press/release pairs** to generate click events
2. **Detect multi-clicks** (double, triple) based on:
   - Time between clicks (default 500ms threshold)
   - Position proximity (same location)
   - Same button
3. **Emit synthetic events:**
   - When release follows press → emit `LeftButtonClicked`
   - When second click within threshold → emit `LeftButtonDoubleClicked`
   - When third click within threshold → emit `LeftButtonTripleClicked`
4. **Maintain state** across events:
   - Last click time, position, button
   - Click count for current sequence

**Key Behavior:**
- **Immediate emission:** Press and Release events pass through immediately
- **Deferred clicks:** ~~Click events are deferred until threshold expires~~ **CURRENT BUG** - see [#4471](https://github.com/gui-cs/Terminal.Gui/issues/4471)
- **Multi-click detection:** Tracks timing and position to synthesize double/triple clicks

**Output:** Stream of `Mouse` events including synthesized clicks

**Code Locations:**
- `MouseInterpreter.Process(Mouse mouse)`
- `MouseButtonClickTracker` - tracks individual button state

### Stage 4: Application-Level Routing (MouseImpl)

**Location:** `Terminal.Gui/App/Mouse/MouseImpl.cs`

**Entry Point:** `IMouse.RaiseMouseEvent(Mouse mouse)`

**Responsibilities:**

#### 4.1: Find Target View
```csharp
List<View?> viewsUnderMouse = App.TopRunnableView.GetViewsUnderLocation(
    mouse.ScreenPosition, 
    ViewportSettingsFlags.TransparentMouse
);
View? deepestView = viewsUnderMouse?.LastOrDefault();
```

#### 4.2: Check for Popover Dismissal
```csharp
if (mouse.IsPressed && 
    App.Popover?.GetActivePopover() is {} popover &&
    !View.IsInHierarchy(popover, deepestView, includeAdornments: true))
{
    ApplicationPopover.HideWithQuitCommand(popover);
    RaiseMouseEvent(mouse); // Recurse to handle event below popover
    return;
}
```

#### 4.3: Handle Mouse Grab
```csharp
if (MouseGrabView is {})
{
    // Convert to grab view's viewport coordinates
    Point viewportLoc = MouseGrabView.ScreenToViewport(mouse.ScreenPosition);
    Mouse grabEvent = new() { 
        Position = viewportLoc, 
        ScreenPosition = mouse.ScreenPosition,
        View = MouseGrabView 
    };
    MouseGrabView.NewMouseEvent(grabEvent);
    return;
}
```

#### 4.4: Convert to View Coordinates
```csharp
Point viewportLocation = deepestView.ScreenToViewport(mouse.ScreenPosition);
Mouse viewMouseEvent = new() {
    Timestamp = mouse.Timestamp,
    Position = viewportLocation,      // Viewport-relative!
    Flags = mouse.Flags,
    ScreenPosition = mouse.ScreenPosition,
    View = deepestView
};
```

#### 4.5: Raise MouseEnter/Leave
```csharp
RaiseMouseEnterLeaveEvents(mouse.ScreenPosition, viewsUnderMouse);
```

#### 4.6: Send to View
```csharp
deepestView.NewMouseEvent(viewMouseEvent);
// If not handled, propagate to SuperView
```

**Key State Managed:**
- `MouseGrabView` - View that has grabbed mouse input
- `CachedViewsUnderMouse` - For Enter/Leave tracking
- `LastMousePosition` - For reference by other components

### Stage 5: View-Level Processing (View.NewMouseEvent)

**Location:** `Terminal.Gui/ViewBase/View.Mouse.cs`

**Entry Point:** `View.NewMouseEvent(Mouse mouse)`

**Processing Pipeline:**

#### 5.1: Pre-condition Validation
```csharp
if (!Enabled) return false;                    // Disabled views don't eat events
if (!CanBeVisible(this)) return false;          // Invisible views ignored
if (!MousePositionTracking  &&                // Filter unwanted motion
    mouse.Flags == MouseFlags.PositionReport) 
    return false;
```

#### 5.2: Raise Low-Level MouseEvent
```csharp
if (RaiseMouseEvent(mouse) || mouse.Handled)
{
    return true;  // View handled it via OnMouseEvent or MouseEvent subscriber
}
```

**This is where views can handle mouse events directly** before command processing.

#### 5.3: Mouse Grab Handling
**Conditions:** `MouseHighlightStates != None` OR `MouseHoldRepeat == true`

##### 5.3a: Pressed Event
```csharp
WhenGrabbedHandlePressed(mouse):
    if (App.Mouse.MouseGrabView != this)
        App.Mouse.GrabMouse(this);
        if (!HasFocus && CanFocus) SetFocus();
        mouse.Handled = true;  // Don't raise command on first press
        
    if (mouse.Position in Viewport)
        MouseState |= MouseState.Pressed;
        MouseState &= ~MouseState.PressedOutside;
    else
        if (!MouseHoldRepeat)
            MouseState |= MouseState.PressedOutside;
```

##### 5.3b: Released Event
```csharp
WhenGrabbedHandleReleased(mouse):
    MouseState &= ~MouseState.Pressed;
    MouseState &= ~MouseState.PressedOutside;
    
    if (!MouseHoldRepeat && MouseState.HasFlag(MouseState.In))
        // Convert Released → Clicked for command invocation
        mouse.Flags = LeftButtonReleased → LeftButtonClicked;
```

##### 5.3c: Clicked Event
```csharp
WhenGrabbedHandleClicked(mouse):
    if (App.Mouse.MouseGrabView == this && mouse.IsSingleClicked)
        App.Mouse.UngrabMouse();
        // Return true if mouse outside viewport (cancel click)
        return !Viewport.Contains(mouse.Position);
```

#### 5.4: Convert Flags for Command Binding
```csharp
// MouseBindings bind to Clicked events, but driver sends Pressed
// Convert Pressed → Clicked for binding lookup
ConvertPressedToClicked(mouse):
    LeftButtonPressed  → LeftButtonClicked
    MiddleButtonPressed  → MiddleButtonClicked
    RightButtonPressed  → RightButtonClicked
    Button4Pressed  → Button4Clicked
```

#### 5.5: Invoke Commands via MouseBindings
```csharp
RaiseCommandsBoundToButtonClickedFlags(mouse):
    ConvertPressedToClicked(mouse);
    InvokeCommandsBoundToMouse(mouse);

InvokeCommandsBoundToMouse(mouse):
    if (MouseBindings.TryGet(mouse.Flags, out binding))
        binding.MouseEventArgs = mouse;
        InvokeCommands(binding.Commands, binding);
```

**Default Bindings** (from `SetupMouse()`):
```csharp
MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Activate);
MouseBindings.Add(MouseFlags.MiddleButtonClicked, Command.Activate);
MouseBindings.Add(MouseFlags.RightButtonClicked, Command.Context);
MouseBindings.Add(MouseFlags.Button4Clicked, Command.Activate);
MouseBindings.Add(MouseFlags.LeftButtonClicked | MouseFlags.ButtonCtrl, Command.Context);
```

#### 5.6: Command Execution
See [Command Deep Dive](command.md) for details on command execution flow.

**Example: LeftButtonClicked → Command.Activate:**
```csharp
InvokeCommand(Command.Activate, context):
    RaiseActivating(context):
        OnActivating(args) || args.Cancel  // Subclass override
        Activating?.Invoke(this, args)     // Event subscribers
        if (!args.Cancel && CanFocus) SetFocus();
```

### Stage 6: Continuous Button Press (Optional)

**Location:** `Terminal.Gui/ViewBase/MouseHeldDown.cs`

**Enabled When:** `View.MouseHoldRepeat == true`

**Behavior:**
1. On button press → Start timer (500ms initial delay)
2. Timer ticks → Raise `MouseIsHeldDownTick` event (50ms interval, 0.5 acceleration)
3. View handles tick → Invoke commands again
4. On button release → Stop timer

**Use Cases:**
- Scrollbar arrows (scroll while held)
- Spin buttons (increment while held)
- Any UI that should repeat action while mouse button held

**Code Flow:**
```csharp
NewMouseEvent(mouse):
    if (MouseHoldRepeat)
        if (mouse.IsPressed)
            MouseHeldDown.Start(mouse);
            MouseHeldDown.MouseIsHeldDownTick += (s, e) => 
                RaiseCommandsBoundToButtonClickedFlags(e.NewValue);
        else
            MouseHeldDown.Stop();
```

### Key Design Decisions & Current Limitations

#### Coordinates Through the Pipeline
1. **ANSI**: 1-based (1,1 = top-left)
2. **AnsiMouseParser**: Converts to 0-based screen coordinates
3. **MouseImpl**: Screen coordinates (0,0 = top-left of terminal)
4. **View**: Viewport-relative coordinates (0,0 = top-left of view's Viewport)

#### Mouse Grab Semantics
- **Automatic**: Views with `MouseHighlightStates` or `MouseHoldRepeat` auto-grab on press
- **Manual**: Views can call `App.Mouse.GrabMouse(this)` explicitly
- **Ungrab**: Automatic on clicked, or manual via `App.Mouse.UngrabMouse()`
- **Grabbed view receives ALL events** until ungrabbed, even if mouse outside viewport

#### Pressed vs. Clicked Conversion
**Problem:** Drivers emit `Pressed` and `Released`, but MouseBindings expect `Clicked`

**Current Solution:** `ConvertPressedToClicked()` in `View.NewMouseEvent()`
- Converts `LeftButtonPressed` → `LeftButtonClicked` before binding lookup
- **Only for grabbed views** or when mouse is released inside viewport

**Limitation:** This is confusing and error-prone. See recommendations below.

#### Click Synthesis Timing
**Current Bug:** MouseInterpreter defers click events by 500ms to detect double-clicks
- This causes 500ms delay for single clicks - **UNACCEPTABLE UX**
- See [Issue #4471](https://github.com/gui-cs/Terminal.Gui/issues/4471)

**OS Behavior:** Clicks are emitted immediately; applications handle timing
- Single click → Immediate feedback
- Double click → Application sees second click and acts differently

### Input Processing Architecture

Terminal.Gui uses a layered input processing architecture:

1. **Platform Input Capture** - Platform-specific APIs capture raw input events
2. **InputProcessorImpl** - Base class that coordinates input processing using specialized parsers
3. **AnsiResponseParser** - Parses ANSI escape sequences from the input stream
4. **MouseInterpreter** - Generates synthetic click events from press/release pairs
5. **AnsiMouseParser** - Parses ANSI mouse escape sequences into `Mouse` events

### Platform-Specific Input Processors

Different platforms use specialized input processors that inherit from `InputProcessorImpl<TInputRecord>`:

* **WindowsInputProcessor** - Processes `WindowsConsole.InputRecord` structures from the Windows Console API (`ReadConsoleInput()`). Converts Windows mouse events directly to `Mouse` instances.
* **ANSI-based Processors** - For Unix/Linux and other ANSI-compatible terminals, input is processed through ANSI escape sequence parsing.

### Mouse Event Generation

Mouse events are generated through a two-stage process:

1. **Raw Event Capture**: Platform APIs capture basic press/release/movement events
2. **Click Synthesis**: The `MouseInterpreter` analyzes press/release timing and position to generate single, double, and triple click events

### ANSI Mouse Parsing

For terminals that use ANSI escape sequences for mouse input (most modern terminals), the `AnsiMouseParser` handles:

- **SGR Extended Mode** (`ESC[&lt;button;x;yM/m`) - Standard format for mouse events
- **Button States** - Press/release detection with button codes 0-3 for left/middle/right/fourth buttons
- **Modifiers** - Alt/Ctrl/Shift detection through extended button codes
- **Wheel Events** - Button codes 64-65 for vertical scrolling, 68-69 for horizontal
- **Motion Events** - Button codes 32-63 for drag operations and mouse movement

### Event Flow

```
Platform API → InputProcessorImpl → AnsiResponseParser → MouseInterpreter → Application
     ↓              ↓                        ↓                    ↓
Raw Events    ANSI Parsing           Mouse Parsing      Click Synthesis
```

This architecture ensures consistent mouse behavior across all supported platforms while maintaining platform-specific optimizations where available.

### Recommended Pipeline Improvements

Based on the pipeline analysis above, here are recommended changes (backwards compatibility not required):

#### 1. **Fix Click Synthesis Timing** (Critical - UX Issue)
**Problem:** MouseInterpreter defers clicks by 500ms to detect double-clicks

**Solution:** Emit clicks immediately, like OSes do
```csharp
// MouseInterpreter should emit:
Press   → LeftButtonPressed   (immediate)
Release → LeftButtonReleased  (immediate)
        → LeftButtonClicked   (immediate after release)

// If second click within threshold:
Press   → LeftButtonPressed
Release → LeftButtonReleased
        → LeftButtonDoubleClicked  (NOT LeftButtonClicked!)
```

**Impact:** 
- Single clicks feel instant (no 500ms delay)
- Applications track timing themselves (see ListView example in mouse.md)
- Matches OS behavior

#### 2. **Simplify Pressed/Clicked Conversion**
**Problem:** Confusing logic to convert `Pressed` → `Clicked` in multiple places

**Option A: Driver emits Clicked** (Recommended)
```csharp
// MouseInterpreter already tracks press/release pairs
// Just emit Clicked instead of maintaining separate flags
Press → LeftButtonPressed (immediate, for drag/grab detection)
Release → LeftButtonClicked (immediate, for command binding)
```

**Option B: MouseBindings accept Pressed**
```csharp
// Change default bindings to use Pressed instead of Clicked
MouseBindings.Add(MouseFlags.LeftButtonPressed, Command.Activate);
// Remove ConvertPressedToClicked logic
```

**Recommendation:** Option A - matches user mental model ("clicked" = press + release)

#### 3. **Clarify Mouse Grab Lifecycle**
**Problem:** Grab logic split across MouseImpl and View makes it hard to understand

**Solution:** Document the state machine clearly
```csharp
// View.NewMouseEvent should have clear sections:
// 1. Pre-conditions
// 2. Low-level event (MouseEvent)
// 3. GRAB HANDLING (if MouseHighlightStates or WantContinuous):
//    a. On Pressed: Grab, set focus, update MouseState
//    b. On Released: Convert to Clicked, update MouseState  
//    c. On Clicked: Ungrab
// 4. Command invocation (for Clicked/Wheel)
```

**Add to documentation:**
- When grab happens (automatically vs manual)
- What grabbed view receives (all events, converted coordinates)
- When ungrab happens (clicked vs manual)
- How MouseHoldRepeat affects grab

#### 4. **Unify Coordinate Conversion**
**Problem:** Coordinate conversion happens in multiple places

**Solution:** Centralize in MouseImpl
```csharp
// MouseImpl.RaiseMouseEvent already does:
Point viewportLocation = view.ScreenToViewport(mouse.ScreenPosition);

// Make this THE ONLY place coordinates are converted
// Document: "mouse.Position is ALWAYS viewport-relative when it reaches View"
```

#### 5. **Separate Press/Release from Click in MouseFlags**
**Problem:** `LeftButtonPressed` and `LeftButtonClicked` are both present, causing confusion

**Proposed Flag Reorganization:**
```csharp
// Raw events (from driver, immediate):
LeftButtonPressed   // Button went down
LeftButtonReleased  // Button came up

// Synthetic events (from MouseInterpreter, after release):
LeftButtonClicked        // Press + Release in same location
LeftButtonDoubleClicked  // Second click within threshold
LeftButtonTripleClicked  // Third click within threshold

// Current state:
LeftButtonDown      // Button is currently down (for drag detection)

// Remove: LeftButtonPressed used for both "event" and "state" - confusing!
```

**Benefits:**
- Clear separation of "what happened" vs "current state"
- Easier to understand when to use each flag
- Matches OS event models

#### 6. **Document the "Why" of Each Stage**
Add to each stage in pipeline docs:
- **Purpose:** What problem does this stage solve?
- **Input:** What does it receive?
- **Output:** What does it emit?
- **State:** What state does it maintain?
- **Decisions:** What choices does it make?

**Example for MouseInterpreter:**
```markdown
**Purpose:** Synthesize high-level click events from low-level press/release pairs

**Input:** Raw Press/Release events from driver

**Output:** 
- Pass through: Pressed, Released, Motion, Wheel (immediate)
- Synthesized: Clicked, DoubleClicked, TripleClicked (immediate after release)

**State:** Tracks last click time, position, button for multi-click detection

**Decisions:**
- Is this release part of a click? (same position as press)
- Is this click part of a multi-click? (timing + position + button match)
```

#### 7. **Add Pipeline Trace Logging**
```csharp
// At each stage, log the transformation:
Logging.Trace($"[AnsiParser] {ansiString} → {mouse.Flags} at {mouse.ScreenPosition}");
Logging.Trace($"[MouseInterpreter] {inputFlags} → {outputFlags}");
Logging.Trace($"[MouseImpl] Screen {screenPos} → View {viewPos} on {view.Id}");
Logging.Trace($"[View] {mouse.Flags} → Command.{command}");
```

**Benefits:**
- Easy debugging of "why didn't my click work?"
- Understand pipeline transformations
- Validate coordinate conversions

#### 8. **Align with Command System Design**
**From command.md:** 
- `Command.Activate` = Interaction/Selection (single click on ListView)
- `Command.Accept` = Confirmation/Action (double-click or Enter)

**Pipeline should support:**
```csharp
// ListView example from mouse.md tables:
First click:   LeftButtonClicked → Command.Activate (select item)
Second click:  LeftButtonClicked → Command.Activate (still!)
               BUT ListView tracks timing and invokes Command.Accept itself

// Default MouseBindings should be:
MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Activate);
MouseBindings.Add(MouseFlags.LeftButtonDoubleClicked, Command.Accept);  // NEW!

// Then Button handles:
Command.Accept → Button action (matches single click)
Command.Activate → Set focus (do nothing else)

// ListView handles:
Command.Activate → Select item (first click) OR invoke Accept (second click)
Command.Accept → Open item (from DoubleClicked or Enter key)
```

**Benefits:**
- Consistent with documented behavior in mouse.md
- Applications don't need custom timing logic
- Framework provides DoubleClicked flag for "accept" actions

#### Summary of Pipeline Changes

| Stage | Current Behavior | Recommended Change | Impact |
|-------|-----------------|-------------------|---------|
| **MouseInterpreter** | Defers clicks 500ms | Emit clicks immediately | **Critical** - fixes UX bug |
| **MouseInterpreter** | Emits `Clicked` 500ms after `Released` | Emit `Clicked` immediately after `Released` | Simplifies timing |
| **View.NewMouseEvent** | Converts `Pressed`→`Clicked` in multiple places | Driver emits `Clicked`, no conversion needed | Clearer code |
| **MouseImpl** | Coordinate conversion | Already correct, just document | Better clarity |
| **MouseBindings** | Only `LeftButtonClicked` → `Activate` | Add `LeftButtonDoubleClicked` → `Accept` | Matches command.md design |
| **Documentation** | Scattered | Centralized pipeline doc (this section) | Developer productivity |



## See Also

* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Command Deep Dive](command.md)
* [Keyboard Deep Dive](keyboard.md)
* [Lexicon & Taxonomy](lexicon.md)