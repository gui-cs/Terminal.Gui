# Migrating From v1 To v2

This document provides an overview of the changes between Terminal.Gui v1 and v2. It is intended to help developers migrate their applications from v1 to v2.

For detailed breaking change documentation check out this Discussion: https://github.com/gui-cs/Terminal.Gui/discussions/2448

## TrueColor Support - 24-bit Color is the default

Terminal.Gui v2 now supports 24-bit color by default. This means that the colors you use in your application will be more accurate and vibrant. If you are using custom colors in your application, you may need to update them to use the new 24-bit color format.

Some methods to do with `Attribute` class have been simplified.  Color names now match the ANSII standard ('Brown' is now called 'Yellow')

### How to Fix

Static class `Attribute.Make` has been removed. Use constructor instead

```diff
- var c = Attribute.Make(Color.BrightMagenta, Color.Blue);
+ var c = new Attribute(Color.BrightMagenta, Color.Blue)
```

```diff
- var c = Color.Brown;
+ var c = Color.Yellow;
```


## Low-Level Type Changes

* `Rect` -> `Rectangle`
* `Point` -> `Point`
* `Size` -> `Size`

### How to Fix

* Replace `Rect` with `Rectangle`


## `NStack.string` has been removed. Use `System.Rune` instead. 

See [Unicode](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#unicode) for details.

### How to Fix

Replace `using` statements with the `System.Text` namespace

```diff
- using NStack;
+ using System.Text;
```

Anywhere you have an implicit cast from `char` to `Rune`, replace with a constructor call 

```diff
- myView.AddRune(col, row, '▄');
+ myView.AddRune(col, row, new Rune('▄'));
```

When measuring the screen space taken up by a `Rune` use `GetColumns()`

```diff
- Rune.ColumnWidth(rune);
+ rune.GetColumns();
```
When measuring the screen space taken up by a `string` you can use the extension method `GetColumns()`

```diff 
- myString.Sum(c=>Rune.ColumnWidth(c));
+ myString.GetColumns();
```


## `View Life Cycle Management

In v1, `View` was derived from `Responder` which supported `IDisposable`. In v2, `Responder` has been removed and `View` is the base-class supporting `IDisposable`. 

In v1, `Application.Init` automatically created a toplevel view and set `Applicaton.Top`. In v2, `Application.Init` no longer automatically creates a toplevel or sets `Applicaton.Top`; app developers must explicitly create the toplevel view and pass it to `Appliation.Run` (or use `Application.Run<myTopLevel>`). Developers are responsible for calling `Dispose` on any toplevel they create before exiting. 

### How to Fix

* Replace `Responder` with `View`
* Update any code that assumed `Application.Init` automatically created a toplevel view and set `Applicaton.Top`.
* Update any code that assumed `Application.Init` automatically disposed of the toplevel view when the application exited.

## Layout Improvements

In v2, the layout system has been improved to make it easier to create complex user interfaces. If you are using custom layouts in your application, you may need to update them to use the new layout system.

* `View.Frame` now represents the position and size of the view in the superview's coordinate system. The `Frame` property is of type `Rectangle`.
* `View.Bounds` has been replaced by `View.Viewport`. The `Viewport` property represents the visible area of the view in its own coordinate system. The `Viewport` property is of type `Rectangle`.
* `View.ContentSize` represents the size of the view's content. The `ContentSize` property is of type `Size`. This replaces `ScrollView` and `ScrollBarView` in v1. See more below.

### How to Fix

### `Bounds` -> `Viewport`

* Rename `Bounds` to `Viewport`. The `Location` property of `Bounds` can now have non-zero values.
* Update any code that assumed `Bounds.Location` was always `Point.Empty`.
* Update any code that used `Bounds` to refer to the size of the view's content. Use `ContentSize` instead.
* Update any code that assumed `Bounds.Size` was the same as `Frame.Size`. `Frame.Size` defines the size of the view in the superview's coordinate system, while `Viewport.Size` defines the visible area of the view in its own coordinate system.
* Use `View.GetAdornmentsThickness` to get the total thickness of the view's border, margin, and padding.
* Not assume a View can draw outside of 'Viewport'. Use the 'Margin', 'Border', and 'Padding' Adornments to do things outside of `Viewport`. View subclasses should not implement their own concept of padding or margins but leverage these `Adornments` instead. 
* Mouse and draw events now provide coordinates relative to the `Viewport` not the `Frame`.

## `View.AutoSize` has been removed. Use `Dim.Auto` for width or height instead.

In v1, `View.AutoSize` was used to size a view to its `Text`. In v2, `View.AutoSize` has been removed. Use `Dim.Auto` for width or height instead.

### How to Fix

* Replace `View.AutoSize = true` with `View.Width = Dim.Auto` or `View.Height = Dim.Auto` as needed.

## Adornments

In v2, the `Border`, `Margin`, and `Padding` properties have been added to all views. This simplifies view development and enables a sophisticated look and feel. If you are using custom borders, margins, or padding in your application, you may need to update them to use the new properties.

* `View.Border` is now of type `Adornment`. `View.BorderStyle` is provided as a convenience property to set the border style (`myView.BorderStyle = LineStyle.Double`).

### How to Fix

## Built-in Scrolling

In v1, scrolling was enabled by using `ScrollView` or `ScrollBarView`. In v2, the base `View` class supports scrolling inherently. The area of a view visible to the user at a given moment was previously a rectangle called `Bounds`. `Bounds.Location` was always `Point.Empty`. In v2 the visible area is a rectangle called `Viewport` which is a protal into the Views content, which can be bigger (or smaller) than the area visible to the user. Causing a view to scroll is as simple as changing `View.Viewport.Location`. The View's content described by `View.ContentSize`. See [Layout](layout.md) for details.

### How to Fix

* Replace `ScrollView` with `View` and use the `Viewport` and `ContentSize` properties to control scrolling.
* Update any code that assumed `Bounds.Location` was always `Point.Empty`.
* Update any code that used `Bounds` to refer to the size of the view's content. Use `ContentSize` instead.
* Update any code that assumed `Bounds.Size` was the same as `Frame.Size`. `Frame.Size` defines the size of the view in the superview's coordinate system, while `Viewport.Size` defines the visible area of the view in its own coordinate system.

## Updated Keyboard API

The API for handling keyboard input is significantly improved. See [Keyboard API](keyboard.md).

* The `Key` class replaces the `KeyEvent` struct and provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level `KeyCode` enum when possible. See [Key](~/api/Terminal.Gui.Key.yml) for more details.
* The preferred way to handle single keystrokes is to use **Key Bindings**. Key Bindings map a key press to a [Command](~/api/Terminal.Gui.Command.yml). A view can declare which commands it supports, and provide a lambda that implements the functionality of the command, using `View.AddCommand()`. Use the `View.Keybindings` to configure the key bindings.

### How to Fix

* Replace `KeyEvent` with `Key`
* Use `View.AddCommand` to define commands your view supports.
* Use `View.Keybindings` to configure key bindings to `Command`s.
* It should be very uncommon for v2 code to override `OnKeyPressed` etc... 

## Updated Mouse API

The API for mouse input is now internally consistent and easiser to use.

* The `MouseEvent` class replaces `MouseEventEventArgs`.
* More granular APIs are provided to ease handling specific mouse actions. See [Mouse API](mouse.md).
* Views can use the `View.Highlight` event to have the view be visibly highlighted on various mouse events.
* Views can set `View.WantContinousButtonPresses = true` to ahve their `Command.Accept` command be invoked repeatedly as the user holds a mouse button down on the view.
* Mouse and draw events now provide coordinates relative to the `Viewport` not the `Screen`.

### How to Fix

* Replace `MouseEventEventArgs` with `MouseEvent`
* Use the `View.Highlight` event to have the view be visibly highlighted on various mouse events.
* Set `View.WantContinousButtonPresses = true` to have the `Command.Accept` command be invoked repeatedly as the user holds a mouse button down on the view.
* Update any code that assumed mouse events provided coordinates relative to the `Screen`.

## Cursor and Focus

The cursor and focus system has been redesigned in v2 to be more consistent and easier to use. If you are using custom cursor or focus logic in your application, you may need to update it to use the new system.

### How to Fix

* Use `Application.MostFocusedView` to get the most focused view in the application.
* Use `View.CursorPosition` to set the cursor position in a view. Set `View.CursorPosition` to `null` to hide the cursor.
* Set `View.CursorVisibility` to the cursor style you want to use.
* Remove any overrides of `OnEnter` and `OnLeave` that explicity change the cursor.


## Events now use `object sender, EventArgs args` signature

Previously events in Terminal.Gui used a mixture of `Action` (no arguments), `Action<string>` (or other raw datatype) and `Action<EventArgs>`.  Now all events use the `EventHandler<EventArgs>` [standard .net design pattern](https://learn.microsoft.com/en-us/dotnet/csharp/event-pattern#event-delegate-signatures).

For example `event Action<long> TimeoutAdded` has become `event EventHandler<TimeoutEventArgs> TimeoutAdded`

This change was made for the following reasons:

- Event parameters are now individually named and documented (with xmldoc)
- Future additions to event parameters can be made without being breaking changes (i.e. adding new properties to the EventArgs class)

For example:

```csharp

public class TimeoutEventArgs : EventArgs {

	/// <summary>
	/// Gets the <see cref="DateTime.Ticks"/> in UTC time when the 
	/// <see cref="Timeout"/> will next execute after.
	/// </summary>
	public long Ticks { get; }

[...]
}
```

## How To Fix
If you previously had a lamda expression, you can simply add the extra arguments:

```diff
- btnLogin.Clicked += () => { /*do something*/ };
+ btnLogin.Clicked += (s,e) => { /*do something*/ };
```

If you have used a named method instead of a lamda you will need to update the signature e.g.

```diff
- private void MyButton_Clicked ()
+ private void MyButton_Clicked (object sender, EventArgs e)
```

## `ReDraw` is now `Draw`

### How to Fix

* Replace `ReDraw` with `Draw`
* Mouse and draw events now provide coordinates relative to the `Viewport` not the `Frame`.

## No more nested classes

All public classes that were previously [nested classes](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/nested-types) are now in the root namespace as their own classes.

### How To Fix
Replace references to to nested types with the new standalone version

```diff
- var myTab = new TabView.Tab();
+ var myTab = new Tab();
```