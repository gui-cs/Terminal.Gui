# Mouse API

## Tenets for Terminal.Gui Mouse Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Keyboard Required; Mouse Optional** - Terminal users expect full functionality without having to pick up the mouse. At the same time they love being able to use the mouse when it makes sense to do so. We strive to ensure anything that can be done with the keyboard is also possible with the mouse. We avoid features that are only useable with the mouse.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to mouse input in a way that is consistent with the platform. For example, on Windows ???

## Mouse APIs

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

