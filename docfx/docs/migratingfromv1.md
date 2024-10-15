# Migrating From v1 To v2

This document provides an overview of the changes between Terminal.Gui v1 and v2. It is intended to help developers migrate their applications from v1 to v2.

For detailed breaking change documentation check out this Discussion: https://github.com/gui-cs/Terminal.Gui/discussions/2448

## View Constructors -> Initializers

In v1, @Terminal.Gui.View and most sub-classes had multiple constructors that took a variety of parameters. In v2, the constructors have been replaced with initializers. This change was made to simplify the API and make it easier to use. In addition, the v1 constructors drove a false (and needlessly complex) distinction between "Absolute" and "Computed" layout. In v2, the layout system is much simpler and more intuitive.

### How to Fix

Replace the constructor calls with initializer calls.

```diff
- var myView = new View (new Rect (10, 10, 40, 10));
+ var myView = new View { X = 10, Y = 10, Width = 40, Height = 10 };
```

## TrueColor Support - 24-bit Color is the default

Terminal.Gui v2 now supports 24-bit color by default. This means that the colors you use in your application will be more accurate and vibrant. If you are using custom colors in your application, you may need to update them to use the new 24-bit color format.

The @Terminal.Gui.Attribute class has been simplified. Color names now match the ANSI standard ('Brown' is now called 'Yellow')

### How to Fix

Static class `Attribute.Make` has been removed. Use constructor instead

```diff
- var c = Attribute.Make(Color.BrightMagenta, Color.Blue);
+ var c = new Attribute(Color.BrightMagenta, Color.Blue);
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

In v1, @Terminal.Gui.View was derived from `Responder` which supported `IDisposable`. In v2, `Responder` has been removed and @Terminal.Gui.View is the base-class supporting `IDisposable`. 

In v1, @Terminal.Gui./Terminal.Gui.Application.Init) automatically created a toplevel view and set [Application.Top](~/api/Terminal.Gui.Application.Top. In v2, @Terminal.Gui.Application.Init no longer automatically creates a toplevel or sets @Terminal.Gui.Application.Top; app developers must explicitly create the toplevel view and pass it to @Terminal.Gui.Application.Run (or use `Application.Run<myTopLevel>`). Developers are responsible for calling `Dispose` on any toplevel they create before exiting. 

### How to Fix

* Replace `Responder` with @Terminal.Gui.View
* Update any code that assumes `Application.Init` automatically created a toplevel view and set `Application.Top`.
* Update any code that assumes `Application.Init` automatically disposed of the toplevel view when the application exited.

## @Terminal.Gui.Pos and @Terminal.Gui.Dim types now adhere to standard C# idioms

* In v1, the @Terminal.Gui.Pos and @Terminal.Gui.Dim types (e.g. @Terminal.Gui.Pos.PosView) were nested classes and marked @Terminal.Gui.internal. In v2, they are no longer nested, and have appropriate public APIs. 
* Nullabilty is enabled.
* Methods & properties follow standards.
* The static method that creates a @Terminal.Gui.PosAbsolute, `Pos.At`, was renamed to @Terminal.Gui.Pos.Absolute for consistency.
* The static method that crates as @Terminal.Gui.DimAbsoulte, `Dim.Sized`, was renamed to @Terminal.Gui.Dim.Absolute for consistency.

### How to Fix

* Search and replace `Pos.Pos` -> `Pos`.
* Search and replace `Dim.Dim` -> `Dim`.
* Search and replace `Pos.At` -> `Pos.Absolute`
* Search and replace `Dim.Sized` -> `Dim.Absolute`
* Search and replace `Dim.Anchor` -> `Dim.GetAnchor`
* Search and replace `Pos.Anchor` -> `Pos.GetAnchor`

## Layout Improvements

In v2, the layout system has been improved to make it easier to create complex user interfaces. If you are using custom layouts in your application, you may need to update them to use the new layout system.

* The distinction between `Absolute Layout` and `Computed Layout` has been removed, as has the `LayoutStyle` enum. v1 drew a false distinction between these styles. 
* @Terminal.Gui.View.Frame now represents the position and size of the view in the superview's coordinate system. The `Frame` property is of type `Rectangle`.
* @Terminal.Gui.View.Bounds has been replaced by @Terminal.Gui.View.Viewport. The `Viewport` property represents the visible area of the view in its own coordinate system. The `Viewport` property is of type `Rectangle`.
* @Terminal.Gui.View.GetContentSize represents the size of the view's content. This replaces `ScrollView` and `ScrollBarView` in v1. See more below.

### How to Fix

### `Bounds` -> `Viewport`

* Remove all references ot `LayoutStyle`.
* Rename `Bounds` to `Viewport`. The `Location` property of `Bounds` can now have non-zero values.
* Update any code that assumed `Bounds.Location` was always `Point.Empty`.
* Update any code that used `Bounds` to refer to the size of the view's content. Use `GetContentSize()` instead.
* Update any code that assumed `Bounds.Size` was the same as `Frame.Size`. `Frame.Size` defines the size of the view in the superview's coordinate system, while `Viewport.Size` defines the visible area of the view in its own coordinate system.
* Use @Terminal.Gui.View.GetAdornmentsThickness to get the total thickness of the view's border, margin, and padding.
* Not assume a View can draw outside of 'Viewport'. Use the 'Margin', 'Border', and 'Padding' Adornments to do things outside of `Viewport`. View subclasses should not implement their own concept of padding or margins but leverage these `Adornments` instead. 
* Mouse and draw events now provide coordinates relative to the `Viewport` not the `Frame`.

## `View.AutoSize` has been removed. Use @Terminal.Gui.Dim.Auto for width or height instead.

In v1, `View.AutoSize` was used to size a view to its `Text`. In v2, `View.AutoSize` has been removed. Use @Terminal.Gui.Dim.Auto for width or height instead.

### How to Fix

* Replace `View.AutoSize = true` with `View.Width = Dim.Auto` or `View.Height = Dim.Auto` as needed. See the [DimAuto Deep Dive](dimauto.md) for more information.

## Adornments

In v2, the `Border`, `Margin`, and `Padding` properties have been added to all views. This simplifies view development and enables a sophisticated look and feel. If you are using custom borders, margins, or padding in your application, you may need to update them to use the new properties.

* `View.Border` is now of type @Terminal.Gui.Adornment. @Terminal.Gui.View.BorderStyle is provided as a convenience property to set the border style (`myView.BorderStyle = LineStyle.Double`).

### How to Fix

## Built-in Scrolling

In v1, scrolling was enabled by using `ScrollView` or `ScrollBarView`. In v2, the base @Terminal.Gui.View class supports scrolling inherently. The area of a view visible to the user at a given moment was previously a rectangle called `Bounds`. `Bounds.Location` was always `Point.Empty`. In v2 the visible area is a rectangle called `Viewport` which is a protal into the Views content, which can be bigger (or smaller) than the area visible to the user. Causing a view to scroll is as simple as changing `View.Viewport.Location`. The View's content is described by @Terminal.Gui.View.GetContentSize. See [Layout](layout.md) for details.

### How to Fix

* Replace `ScrollView` with @Terminal.Gui.View and use `Viewport` and @Terminal.Gui.View.GetContentSize to control scrolling.
* Update any code that assumed `Bounds.Location` was always `Point.Empty`.
* Update any code that used `Bounds` to refer to the size of the view's content. Use @Terminal.Gui.View.GetContentSize instead.
* Update any code that assumed `Bounds.Size` was the same as `Frame.Size`. `Frame.Size` defines the size of the view in the superview's coordinate system, while `Viewport.Size` defines the visible area of the view in its own coordinate system.

## Updated Keyboard API

The API for handling keyboard input is significantly improved. See [Keyboard API](keyboard.md).

* The @Terminal.Gui.Key class replaces the `KeyEvent` struct and provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level @Terminal.Gui.KeyCode enum when possible. See @Terminal.Gui.Key for more details.
* The preferred way to enable Application-wide or View-heirarchy-dependent keystrokes is to use the @Terminal.Gui.Shortcut View or the built-in View's that utilize it, such as the @Terminal.Gui.Bar-based views.
* The preferred way to handle single keystrokes is to use **Key Bindings**. Key Bindings map a key press to a @Terminal.Gui.Command. A view can declare which commands it supports, and provide a lambda that implements the functionality of the command, using `View.AddCommand()`. Use the @Terminal.Gui.View.Keybindings to configure the key bindings.
* For better consistency and user experience, the default key for closing an app or `Toplevel` is now `Esc` (it was previously `Ctrl+Q`).
* The `Application.RootKeyEvent` method has been replaced with `Application.KeyDown`

### How to Fix

* Replace `KeyEvent` with `Key`
* Use @Terminal.Gui.View.AddCommand to define commands your view supports.
* Use @Terminal.Gui.View.Keybindings to configure key bindings to `Command`s.
* It should be very uncommon for v2 code to override `OnKeyPressed` etc... 
* Anywhere `Ctrl+Q` was hard-coded as the "quit key", replace with `Application.QuitKey`.
* See *Navigation* below for more information on v2's navigation keys.
* Replace `Application.RootKeyEvent` with `Application.KeyDown`.  If the reason for subscribing to RootKeyEvent was to enable an application-wide action based on a key-press, consider using Application.KeyBindings instead.

```diff
- Application.RootKeyEvent(KeyEvent arg)
+ Application.KeyDown(object? sender, Key e)
```

## **@"Terminal.Gui.Command" has been expanded and simplified

In v1, the `Command` enum had duplicate entries and inconsistent naming. In v2 it has been both expanded and simplified.

### How To Fix

* Update any references to old `Command` values with the updated versions.

## Updated Mouse API

The API for mouse input is now internally consistent and easier to use.

* The @Terminal.Gui.MouseEventArgs class replaces `MouseEventEventArgs`.
* More granular APIs are provided to ease handling specific mouse actions. See [Mouse API](mouse.md).
* Views can use the @Terminal.Gui.View.Highlight event to have the view be visibly highlighted on various mouse events.
* Views can set `View.WantContinousButtonPresses = true` to have their @Terminal.Gui.Command.Accept command be invoked repeatedly as the user holds a mouse button down on the view.
* Mouse and draw events now provide coordinates relative to the `Viewport` not the `Screen`.
* The `Application.RootMouseEvent` method has been replaced with `Application.MouseEvent`

### How to Fix

* Replace `MouseEventEventArgs` with `MouseEvent`
* Use the @Terminal.Gui.View.Highlight event to have the view be visibly highlighted on various mouse events.
* Set `View.WantContinousButtonPresses = true` to have the @Terminal.Gui.Command.Accept command be invoked repeatedly as the user holds a mouse button down on the view.
* Update any code that assumed mouse events provided coordinates relative to the `Screen`.
* Replace `Application.RootMouseEvent` with `Application.MouseEvent`.  

```diff
- Application.RootMouseEvent(KeyEvent arg)
+ Application.MouseEvent(object? sender, MouseEventArgs mouseEvent)
```

## Navigation - `Cursor`, `Focus`, `TabStop` etc... 

The cursor and focus system has been redesigned in v2 to be more consistent and easier to use. If you are using custom cursor or focus logic in your application, you may need to update it to use the new system.

### Cursor

In v1, whether the cursor (the flashing caret) was visible or not was controlled by `View.CursorVisibility` which was an enum extracted from Ncruses/Terminfo. It only works in some cases on Linux, and only partially with `WindowsDriver`. The position of the cursor was the same as `ConsoleDriver.Row`/`Col` and determined by the last call to `ConsoleDriver.Move`. `View.PositionCursor()` could be overridden by views to cause `Application` to call `ConsoleDriver.Move` on behalf of the app and to manage setting `CursorVisibility`. This API was confusing and bug-prone.

In v2, the API is (NOT YET IMPLEMENTED) simplified. A view simply reports the style of cursor it wants and the Viewport-relative location:

* `public Point? CursorPosition`
    - If `null` the cursor is not visible
    - If `{}` the cursor is visible at the `Point`.
* `public event EventHandler<LocationChangedEventArgs>? CursorPositionChanged`
* `public int? CursorStyle`
	- If `null` the default cursor style is used.
	- If `{}` specifies the style of cursor. See [cursor.md](cursor.md) for more.
* `Application` now has APIs for querying available cursor styles.
* The details in `ConsoleDriver` are no longer available to applications.	

#### How to Fix (Cursor API)

* Use @Terminal.Gui.View.CursorPosition to set the cursor position in a view. Set @Terminal.Gui.View.CursorPosition to `null` to hide the cursor.
* Set @Terminal.Gui.View.CursorVisibility to the cursor style you want to use.
* Remove any overrides of `OnEnter` and `OnLeave` that explicitly change the cursor.

### Focus

See [navigation.md](navigation.md) for more details.
See also [Keyboard](keyboard.md) where HotKey is covered more deeply...

* In v1, `View.CanFocus` was `true` by default. In v2, it is `false`. Any `View` subclass that wants to be focusable must set `CanFocus = true`.
* In v1 it was not possible to remove focus from a view. `HasFocus` as a get-only property. In v2, `view.HasFocus` can be set as well. Setting to `true` is equivalent to calling `view.SetFocus`. Setting to `false` is equivalent to calling `view.SuperView.AdvanceFocus` (which might not actually cause `view` to stop having focus). 
* In v1, calling `super.Add (view)` where `view.CanFocus == true` caused all views up the hierarchy (all SuperViews) to get `CanFocus` set to `true` as well. In v2, developers need to explicitly set `CanFocus` for any view in the view-hierarchy where focus is desired. This simplifies the implementation and removes confusing automatic behavior. 
* In v1, if `view.CanFocus == true`, `Add` would automatically set `TabStop`. In v2, the automatic setting of `TabStop` in `Add` is retained because it is not overly complex to do so and is a nice convenience for developers to not have to set both `Tabstop` and `CanFocus`. Note v2 does NOT automatically change `CanFocus` if `TabStop` is changed.
* `view.TabStop` now describes the behavior of a view in the focus chain. the `TabBehavior` enum includes `NoStop` (the view may be focusable, but not via next/prev keyboard nav), `TabStop` (the view may be focusable, and `NextTabStop`/`PrevTabStop` keyboard nav will stop), `TabGroup` (the view may be focusable, and `NextTabGroup`/`PrevTabGroup` keyboard nav will stop). 
* In v1, the `View.Focused` property was a cache of which view in `SubViews/TabIndexes` had `HasFocus == true`. There was a lot of logic for keeping this property in sync. In v2, `View.Focused` is a get-only, computed property. 
* In v1, the `View.MostFocused` property recursed down the subview-hierarchy on each get. In addition, because only one View in an application can be the "most focused", it doesn't make sense for this property to be on every View. In v2, this API is removed. Use `Application.Navigation.GetFocused()` instead.
* The v1 APIs `View.EnsureFocus`/`FocusNext`/`FocusPrev`/`FocusFirst`/`FocusLast` are replaced in v2 with these APIs that accomplish the same thing, more simply.
  - `public bool AdvanceFocus (NavigationDirection direction, TabBehavior? behavior)`  
  - `public bool FocusDeepest (NavigationDirection direction, TabBehavior? behavior)` 
* In v1, the `View.OnEnter/Enter` and `View.OnLeave/Leave` virtual methods/events could be used to notify that a view had gained or lost focus, but had confusing semantics around what it mean to override (requiring calling `base`) and bug-ridden behavior on what the return values signified. The "Enter" and "Leave" terminology was confusing. In v2, `View.OnHasFocusChanging/HasFocusChanging` and `View.OnHasFocusChanged/HasFocusChanged` replace `View.OnEnter/Enter` and `View.OnLeave/Leave`. These virtual methods/events follow standard Terminal.Gui event patterns. The `View.OnHasFocusChanging/HasFocusChanging` event supports being cancelled.
* In v1, the concept of `Mdi` views included a large amount of complex code (in `Toplevel` and `Application`) for dealing with navigation across overlapped Views. This has all been radically simplified in v2. Any View can work in an "overlapped" or "tiled" way. See [navigation.md](navigation.md) for more details.
* The `View.TabIndex` and `View.TabIndexes` have been removed. Change the order of the views in `View.Subviews` to change the navigation order (using, for example `View.MoveSubviewTowardsStart()`).

### How to Fix (Focus API)

* Set @Terminal.Gui.View.CanFocus to `true` for any View sub-class that wants to be focusable.
* Use @Terminal.Gui.Application.Navigation.GetFocused to get the most focused view in the application.
* Use @Terminal.Gui.Application.Navigation.AdvanceFocus to cause focus to change.

### Keyboard Navigation

In v2, `HotKey`s can be used to navigate across the entire application view-hierarchy. They work independently of `Focus`. This enables a user to navigate across a complex UI of nested subviews if needed (even in overlapped scenarios). An example use-case is the `AllViewsTester` scenario.

In v2, unlike v1, multiple Views in an application (even within the same SuperView) can have the same `HotKey`. Each press of the `HotKey` will invoke the next `HotKey` across the View hierarchy (NOT IMPLEMENTED YET)*

In v1, the keys used for navigation were both hard-coded and configurable, but in an inconsistent way. `Tab` and `Shift+Tab` worked consistently for navigating between Subviews, but were not configurable. `Ctrl+Tab` and `Ctrl+Shift+Tab` navigated across `Overlapped` views and had configurable "alternate" versions (`Ctrl+PageDown` and `Ctrl+PageUp`).

In v2, this is made consistent and configurable:

- `Application.NextTabStopKey` (`Key.Tab`) - Navigates to the next subview that is a `TabStop` (see below). If there is no next, the first subview that is a `TabStop` will gain focus.
- `Application.PrevTabStopKey` (`Key.Tab.WithShift`) - Opposite of `Application.NextTabStopKey`.
- `Key.CursorRight` - Operates identically to `Application.NextTabStopKey`.
- `Key.CursorDown` - Operates identically to `Application.NextTabStopKey`.
- `Key.CursorLeft` - Operates identically to `Application.PrevTabStopKey`.
- `Key.CursorUp` - Operates identically to `Application.PrevTabStopKey`.
- `Application.NextTabGroupKey` (`Key.F6`) - Navigates to the next view in the view-hierarchy that is a `TabGroup` (see below). If there is no next, the first view which is a `TabGroup`` will gain focus.
- `Application.PrevTabGroupKey` (`Key.F6.WithShift`) - Opposite of `Application.NextTabGroupKey`.

`F6` was chosen to match [Windows](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-accelerators#common-keyboard-accelerators)

These keys are all registered as `KeyBindingScope.Application` key bindings by `Application`. Because application-scoped key bindings have the lowest priority, Views can override the behaviors of these keys (e.g. `TextView` overrides `Key.Tab` by default, enabling the user to enter `\t` into text). The `AllViews_AtLeastOneNavKey_Leaves` unit test ensures all built-in Views have at least one of the above keys that can advance. 

### How to Fix (Keyboard Navigation)

...

## Button.Clicked Event Renamed

The `Button.Clicked` event has been renamed `Button.Accepting`

## How to Fix

Rename all instances of `Button.Clicked` to `Button.Accepting`.  Note the signature change to mouse events below.

```diff
- btnLogin.Clicked 
+ btnLogin.Accepting
```

Alternatively, if you want to have key events as well as mouse events to fire an event, use `Button.Accepting`.

## Events now use `object sender, EventArgs args` signature

Previously events in Terminal.Gui used a mixture of `Action` (no arguments), `Action<string>` (or other raw datatype) and `Action<EventArgs>`. Now all events use the `EventHandler<EventArgs>` [standard .net design pattern](https://learn.microsoft.com/en-us/dotnet/csharp/event-pattern#event-delegate-signatures).

For example, `event Action`<long> TimeoutAdded` has become `event EventHandler<TimeoutEventArgs> TimeoutAdded`

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
If you previously had a lambda expression, you can simply add the extra arguments:

```diff
- btnLogin.Clicked += () => { /*do something*/ };
+ btnLogin.Accepting += (s,e) => { /*do something*/ };
```
Note that the event name has also changed as noted above.

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
Replace references to nested types with the new standalone version

```diff
- var myTab = new TabView.Tab();
+ var myTab = new Tab();
```

## View and Text Alignment Changes

In v1, both `TextAlignment` and `VerticalTextAlignment` enums were used to align text in views. In v2, these enums have been replaced with the @Terminal.Gui.Alignment enum. The @Terminal.Gui.View.TextAlignment property controls horizontal text alignment and the @Terminal.Gui.View.VerticalTextAlignment property controls vertical text alignment.

v2 now supports @Terminal.Gui.Pos.Align which enables views to be easily aligned within their Superview. 

The @Terminal.Gui.Aligner class makes it easy to align elements (text, Views, etc...) within a container. 

### How to Fix

* Replace `VerticalAlignment.Middle` is now @Terminal.Gui.Alignment.Center. 

## `StatusBar`- `StatusItem` is replaced by `Shortcut`

@Terminal.Gui.StatusBar has been upgraded to utilize @Terminal.Gui.Shortcut.

### How to Fix

```diff
-  var statusBar = new StatusBar (
-                                       new StatusItem []
-                                       {
-                                           new (
-                                                Application.QuitKey,
-                                                $"{Application.QuitKey} to Quit",
-                                                () => Quit ()
-                                               )
-                                       }
-                                      );
+ var statusBar = new StatusBar (new Shortcut [] { new (Application.QuitKey, "Quit", Quit) });
```

## `CheckBox` - API renamed and simplified

In v1 `CheckBox` used `bool?` to represent the 3 states. To support consistent behavior for the `Accept` event, `CheckBox` was refactored to use the new `CheckState` enum instead of `bool?`.

Additionally, the `Toggle` event was renamed `CheckStateChanging` and made cancelable. The `Toggle` method was renamed to `AdvanceCheckState`.

### How to Fix

```diff
-var cb = new CheckBox ("_Checkbox", true); {
-				X = Pos.Right (label) + 1,
-				Y = Pos.Top (label) + 2
-			};
-			cb.Toggled += (e) => {
-			};
-           cb.Toggle ();
+
+var cb = new CheckBox ()
+{ 
+	Title = "_Checkbox",
+	CheckState = CheckState.Checked
+}
+cb.CheckStateChanging += (s, e) =>
+{	
+	e.Cancel = preventChange;
+}
+preventChange = false;
+cb.AdvanceCheckState ();
```

## `MainLoop` is no longer accessible from `Application`

In v1, you could add timeouts via `Application.MainLoop.AddTimeout` among other things.  In v2, the `MainLoop` object is internal to `Application` and methods previously accessed via `MainLoop` can now be accessed directly via `Application`

### How to Fix

```diff
- Application.MainLoop.AddTimeout (TimeSpan time, Func<MainLoop, bool> callback)
+ Application.AddTimeout (TimeSpan time, Func<bool> callback)
```

## `SendSubviewXXX` renamed and corrected

In v1, the `View` methods to move Subviews within the Subviews list were poorly named and actually operated in reverse of what their names suggested.

In v2, these methods have been named correctly.

- `SendSubViewToBack` -> `MoveSubviewToStart` - Moves the specified subview to the start of the list.
- `SendSubViewBackward` -> `MoveSubviewTowardsStart` - Moves the specified subview one position towards the start of the list.
- `SendSubViewToFront` -> `MoveSubviewToEnd` - Moves the specified subview to the end of the list.
- `SendSubViewForward` -> `MoveSubviewTowardsEnd` - Moves the specified subview one position towards the end of the list.

## `Mdi` Replaced by `ViewArrangement.Overlapped`

In v1, it apps with multiple overlapping views could be created using a set of APIs spread across `Application` (e.g. `Application.MdiTop`) and `Toplevel` (e.g. `IsMdiContainer`). This functionality has been replaced in v2 with @Terminal.Gui.View.Arrangement. Specifically, overlapped views with @Terminal.Gui.View.Arrangement having the @Terminal.Gui.ViewArrangement.Overlapped flag set will be arranged in an overlapped fashion using the order in their SuperView's subview list as the Z-order. 

Setting the @Terminal.Gui.ViewArrangement.Movable flag will enable the overlapped views to be movable with the mouse or keyboard (`Ctrl+F5` to activate).

Setting the @Terminal.Gui.ViewArrangement.Sizable flag will enable the overlapped views to be resized with the mouse or keyboard (`Ctrl+F5` to activate).

In v1, only Views derived from `Toplevel` could be overlapped. In v2, any view can be.

v1 conflated the concepts of 

## Others...

* `View` and all subclasses support `IDisposable` and must be disposed (by calling `view.Dispose ()`) by whatever code owns the instance when the instance is longer needed. 

* To simplify programming, any `View` added as a Subview another `View` will have it's lifecycle owned by the Superview; when a `View` is disposed, it will call `Dispose` on all the items in the `Subviews` property. Note this behavior is the same as it was in v1, just clarified.

* In v1, `Application.End` called `Dispose ()` on @Terminal.Gui.Application.Top (via `Runstate.Toplevel`). This was incorrect as it meant that after `Application.Run` returned, `Application.Top` had been disposed, and any code that wanted to interrogate the results of `Run` by accessing `Application.Top` only worked by accident. This is because GC had not actually happened; if it had the application would have crashed. In v2 `Application.End` does NOT call `Dispose`, and it is the caller to `Application.Run` who is responsible for disposing the `Toplevel` that was either passed to `Application.Run (View)` or created by `Application.Run<T> ()`.

* Any code that creates a `Toplevel`, either by using `top = new()` or by calling either `top = Application.Run ()` or `top = ApplicationRun<T>()` must call `top.Dispose` when complete. The exception to this is if `top` is passed to `myView.Add(top)` making it a subview of `myView`. This is because the semantics of `Add` are that the `myView` takes over responsibility for the subviews lifetimes. Of course, if someone calls `myView.Remove(top)` to remove said subview, they then re-take responsbility for `top`'s lifetime and they must call `top.Dispose`.