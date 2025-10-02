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
        
        AddCommand (Command.Select, () => SelectItem());
        MouseBindings.Add (MouseFlags.Button1Clicked, Command.Select);
    }
}
```

The @Terminal.Gui.Input.Command enum lists generic operations that are implemented by views. 

### Common Mouse Bindings

Here are some common mouse binding patterns used throughout Terminal.Gui:

* **Click Events**: `MouseFlags.Button1Clicked` for primary selection/activation
* **Double-Click Events**: `MouseFlags.Button1DoubleClicked` for default actions (like opening/accepting)
* **Right-Click Events**: `MouseFlags.Button3Clicked` for context menus
* **Scroll Events**: `MouseFlags.WheelUp` and `MouseFlags.WheelDown` for scrolling content
* **Drag Events**: `MouseFlags.Button1Pressed` combined with mouse move tracking for drag operations

## Mouse Events

At the core of *Terminal.Gui*'s mouse API is the @Terminal.Gui.Input.MouseEventArgs class. The @Terminal.Gui.Input.MouseEventArgs class provides a platform-independent abstraction for common mouse events. Every mouse event can be fully described in a @Terminal.Gui.Input.MouseEventArgs instance, and most of the mouse-related APIs are simply helper functions for decoding a @Terminal.Gui.Input.MouseEventArgs.

When the user does something with the mouse, the `ConsoleDriver` maps the platform-specific mouse event into a `MouseEventArgs` and calls `Application.RaiseMouseEvent`. Then, `Application.RaiseMouseEvent` determines which `View` the event should go to. The `View.OnMouseEvent` method can be overridden or the `View.MouseEvent` event can be subscribed to, to handle the low-level mouse event. If the low-level event is not handled by a view, `Application` will then call the appropriate high-level helper APIs. For example, if the user double-clicks the mouse, `View.OnMouseClick` will be called/`View.MouseClick` will be raised with the event arguments indicating which mouse button was double-clicked. 

### Mouse Event Processing Flow

Mouse events are processed through the following workflow using the [Cancellable Work Pattern](cancellable-work-pattern.md):

1. **Driver Level**: The ConsoleDriver captures platform-specific mouse events and converts them to `MouseEventArgs`
2. **Application Level**: `Application.RaiseMouseEvent` determines the target view and routes the event
3. **View Level**: The target view processes the event through:
   - `OnMouseEvent` (virtual method that can be overridden)
   - `MouseEvent` event (for event subscribers)
   - Mouse bindings (if the event wasn't handled)
   - High-level events like `OnMouseClick`, `MouseEnter`, `MouseLeave`

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

## Mouse State

The @Terminal.Gui.ViewBase.View.MouseState property provides an abstraction for the current state of the mouse, enabling views to do interesting things like change their appearance based on the mouse state.

Mouse states include:
* **Normal** - Default state when mouse is not interacting with the view
* **Over** - Mouse is positioned over the view
* **Pressed** - Mouse button is pressed down while over the view
* **Clicked** - Mouse was clicked on the view

It works in conjunction with the @Terminal.Gui.ViewBase.View.HighlightStates which is a list of mouse states that will cause a view to become highlighted.

Subscribe to the @Terminal.Gui.ViewBase.View.MouseStateChanged event to be notified when the mouse state changes:

```cs
view.MouseStateChanged += (sender, e) => 
{
    switch (e.NewState)
    {
        case MouseState.Over:
            // Change appearance when mouse hovers
            break;
        case MouseState.Pressed:
            // Change appearance when pressed
            break;
    }
};
```

## Mouse Button and Movement Concepts

* **Down** - Indicates the user pushed a mouse button down.
* **Pressed** - Indicates the mouse button is down; for example if the mouse was pressed down and remains down for a period of time.
* **Released** - Indicates the user released a mouse button.
* **Clicked** - Indicates the user pressed then released the mouse button while over a particular View. 
* **Double-Clicked** - Indicates the user clicked twice in rapid succession.
* **Moved** - Indicates the mouse moved to a new location since the last mouse event.
* **Wheel** - Indicates the mouse wheel was scrolled up or down.

## Global Mouse Handling

The @Terminal.Gui.App.Application.MouseEvent event can be used if an application wishes to receive all mouse events before they are processed by individual views:

```cs
Application.MouseEvent += (sender, e) => 
{
    // Handle application-wide mouse events
    if (e.Flags.HasFlag(MouseFlags.Button3Clicked))
    {
        ShowGlobalContextMenu(e.Position);
        e.Handled = true;
    }
};
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

* **Screen Coordinates** - Relative to the entire terminal screen (0,0 is top-left of terminal)
* **View Coordinates** - Relative to the view's content area (0,0 is top-left of view's viewport)

The `MouseEventArgs` provides both coordinate systems:
* `MouseEventArgs.Position` - Screen coordinates
* `MouseEventArgs.ViewPosition` - View-relative coordinates (when available)

## Best Practices

* **Use Mouse Bindings** when possible for simple mouse interactions - they integrate well with the Command system
* **Handle Mouse Events directly** for complex interactions like drag-and-drop or custom gestures  
* **Respect platform conventions** - use right-click for context menus, double-click for default actions
* **Provide keyboard alternatives** - ensure all mouse functionality has keyboard equivalents
* **Test with different terminals** - mouse support varies between terminal applications
* **Use Mouse State** to provide visual feedback when users hover or interact with views

## Limitations and Considerations

* Not all terminal applications support mouse input - always provide keyboard alternatives
* Mouse wheel support may vary between platforms and terminals
* Some terminals may not support all mouse buttons or modifier keys
* Mouse coordinates are limited to character cell boundaries - sub-character precision is not available
* Performance can be impacted by excessive mouse move event handling - use mouse enter/leave events when appropriate rather than tracking all mouse moves




