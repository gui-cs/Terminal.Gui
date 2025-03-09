# Mouse API

## Tenets for Terminal.Gui Mouse Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Keyboard Required; Mouse Optional** - Terminal users expect full functionality without having to pick up the mouse. At the same time they love being able to use the mouse when it makes sense to do so. We strive to ensure anything that can be done with the keyboard is also possible with the mouse. We avoid features that are only useable with the mouse.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to mouse input in a way that is consistent with the platform. For example, on Windows ???

## Mouse APIs

*Terminal.Gui* provides the following APIs for handling mouse input:

* **MouseEventArgs** - @Terminal.Gui.MouseEventArgs provides a platform-independent abstraction for common mouse operations. It is used for processing mouse input and raising mouse events.
* **Mouse Bindings** - Mouse Bindings provide a declarative method for handling mouse input in View implementations. The View calls Terminal.Gui.View.AddCommand(Terminal.Gui.Command,System.Func{System.Nullable{System.Boolean}}) to declare it supports a particular command and then uses @Terminal.Gui.MouseBindings to indicate which mouse events will invoke the command. 
* **Mouse Events** - The Mouse Bindings API is rich enough to support the  majority of use-cases. However, in some cases subscribing directly to key events is needed (e.g. drag & drop). Use @Terminal.Gui.View.MouseEvent and related events in these cases.

Each of these APIs are described more fully below.

## Mouse Bindings

Mouse Bindings is the preferred way of handling mouse input in View implementations. The View calls Terminal.Gui.View.AddCommand(Terminal.Gui.Command,System.Func{System.Nullable{System.Boolean}}) to declare it supports a particular command and then uses @Terminal.Gui.MouseBindings to indicate which mouse events will invoke the command. For example, if a View wants to respond to the user using the mouse wheel to scroll up, it would do this:

```cs
public MyView : View
{
  AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
  MouseBindings.Add (MouseFlags.Button1DoubleClick, Command.ScrollUp);
}
```

The [Command](~/api/Terminal.Gui.Command.yml) enum lists generic operations that are implemented by views. 

## Mouse Events

At the core of *Terminal.Gui*'s mouse API is the @Terminal.Gui.MouseEventArgs class. The @Terminal.Gui.MouseEventArgs class provides a platform-independent abstraction for common mouse events. Every mouse event can be fully described in a @Terminal.Gui.MouseEventArgs instance, and most of the mouse-related APIs are simply helper functions for decoding a @Terminal.Gui.MouseEventArgs.

When the user does something with the mouse, the `ConsoleDriver` maps the platform-specific mouse event into a `MouseEventArgs` and calls `Application.RaiseMouseEvent`. Then, `Application.RaiseMouseEvent` determines which `View` the event should go to. The `View.OnMouseEvent` method can be overridden or the `View.MouseEvent` event can be subscribed to, to handle the low-level mouse event. If the low-level event is not handled by a view, `Application` will then call the appropriate high-level helper APIs. For example, if the user double-clicks the mouse, `View.OnMouseClick` will be called/`View.MouseClick` will be raised with the event arguments indicating which mouse button was double-clicked. 

## Mouse Button and Movement Concepts

* **Down** - Indicates the user pushed a mouse button down.
* **Pressed** - Indicates the mouse button is down; for example if the mouse was pressed down and remains down for a period of time.
* **Released** - Indicates the user released a mouse button.
* **Clicked** - Indicates the user pressed then released the mouse button while over a particular View. 
* **Moved** - Indicates the mouse moved to a new location since the last mouse event.

## **Global Mouse Handling**

The @Terminal.Gui.Application.MouseEvent event can be used if an application wishes to receive all mouse events.

## Mouse Enter/Leave Events

The @Terminal.Gui.View.MouseEnter and @Terminal.Gui.View.MouseLeave events enable a View to take action when the mouse is over the view. Internally, this is used to enable @Terminal.Gui.View.Highlight.

