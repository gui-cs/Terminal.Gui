# Terminal.Gui v2

This document provides an overview of the new features and improvements in Terminal.Gui v2.

For information on how to port code from v1 to v2, see the [v1 To v2 Migration Guide](migratingfromv1.md).

## Modern Look & Feel 

Apps built with Terminal.Gui now feel modern thanks to these improvements:

* *TrueColor support* - 24-bit color support for Windows, Mac, and Linux. Legacy 16-color systems are still supported, automatically. See [TrueColor](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#truecolor) for details.
* *Enhanced Borders and Padding* - Terminal.Gui now supports a `Border`, `Margin`, and `Padding` property on all views. This simplifies View development and enables a sophisticated look and feel. See [Adornments](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#adornments) for details.
* *User Configurable Color Themes* - See [Color Themes](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#color-themes) for details.
* *Enhanced Unicode/Wide Character support* - Terminal.Gui now supports the full range of Unicode/wide characters. See [Unicode](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#unicode) for details.
* *Line Canvas* - Terminal.Gui now supports a line canvas enabling high-performance drawing of lines and shapes using box-drawing glyphs. `LineCanvas` provides *auto join*, a smart TUI drawing system that automatically selects the correct line/box drawing glyphs for intersections making drawing complex shapes easy. See [Line Canvas](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#line-canvas) for details.

## Simplified API

The entire library has been reviewed and simplified. As a result, the API is more consistent and uses modern .NET API standards (e.g. for events). This refactoring resulted in the removal of thousands of lines of code, better unit tests, and higher performance than v1. See [Simplified API](overview.md#simplified-api) for details.

## `View` Improvements
* *View Lifetime Management is Now Deterministic* - In v1 the rules ofr lifetime management of `View` objects was unclear and led to non-dterministic behavior and hard to diagnose bugs. This was particularly acute in the behavior of `Application.Run`. In v2, the rules are clear and the code and unit test infrastructure tries to enforce them. 
  * `View` and all subclasses support `IDisposable` and must be disposed (by calling `view.Dispose ()`) by whatever code owns the instance when the instance is longer needed. 
  * To simplify programming, any `View` added as a Subview another `View` will have it's lifecycle owned by the Superview; when a `View` is disposed, it will call `Dispose` on all the items in the `Subviews` property. Note this behavior is the same as it was in v1, just clarified.
  * In v1, `Application.End` called `Dispose ()` on `Application.Top` (via `Runstate.Toplevel`). This was incorrect as it meant that after `Application.Run` returned, `Application.Top` had been disposed, and any code that wanted to interogate the results of `Run` by accessing `Application.Top` only worked by accident. This is because GC had not actually happened; if it had the application would have crashed. In v2 `Application.End` does NOT call `Dispose`, and it is the caller to `Application.Run` who is responsible for disposing the `Toplevel` that was either passed to `Application.Run (View)` or created by `Application.Run<T> ()`.
	* Any code that creates a `Toplevel`, either by using `top = new()` or by calling either `top = Application.Run ()` or `top = ApplicationRun<T>()` must call `top.Dispose` when complete.
  	*  The exception to this is if `top` is passed to `myView.Add(top)` making it a subview of `myView`. This is because the semantics of `Add` are that the `myView` takes over responsibility for the subviews lifetimes. Of course, if someone calls `myView.Remove(top)` to remove said subview, they then re-take responsbility for `top`'s lifetime and they must call `top.Dispose`.
* New! *Adornments* -  Adornments are a special form of View that appear outside the `Viewport`: `Margin`, `Border`, and `Padding`.
* New! *Built-in Scrolling/Virtual Content Area* - In v1, to have a view a user could scroll required either a bespoke scrolling implementation, inheriting from `ScrollView`, or managing the complexity of `ScrollBarView` directly. In v2, the base-View class supports scrolling inherently. The area of a view visible to the user at a given moment was previously a rectangle called `Bounds`. `Bounds.Location` was always `Point.Empty`. In v2 the visible area is a rectangle called `Viewport` which is a protal into the Views content, which can be bigger (or smaller) than the area visible to the user. Causing a view to scroll is as simple as changing `View.Viewport.Location`. The View's content described by `View.GetContentSize()`. See [Layout](layout.md) for details.
* New! *`Dim.Auto`* - Automatically sizes the view to fitthe view's Text, SubViews, or ContentArea.
* Improved! *`Pos.AnchorEnd ()`* - New to v2 is `Pos.AnchorEnd ()` (with no parameters) which allows a view to be anchored to the right or bottom of the Superview. 
* New! *`Pos.Align ()`* - Aligns a set of views horizontally or vertically (left, rigth, center, etc...).
* ...	

## New and Improved Built-in Views

* *DatePicker* - NEW!
* *ScrollView* - Replaced by built-in scrolling.
* *ScrollBar* - Replaces *ScrollBarView* with a much simpler view.
* *Slider* - NEW!
* *Bars* - NEW!
* *StatusBar* - New implementation based on `Bar`
* *MenuBar* - New implementation based on `Bar`
* *ContextMenu* - New implementation based on `Bar`
* *File Dialog* - The new, modern file dialog includes icons (in TUI!) for files/folders, search, and a `TreeView``. See [FileDialog](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#filedialog) for details.

## Configuration Manager

Terminal.Gui now supports a configuration manager enabling library and app settings to be persisted and loaded from the file system. See [Configuration Manager](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#configuration-manager) for details.

## Updated Keyboard API

The API for handling keyboard input is significantly improved. See [Keyboard API](keyboard.md).

* The `Key` class replaces the `KeyEvent` struct and provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level `KeyCode` enum when possible. See [Key](~/api/Terminal.Gui.Key.yml) for more details.
* The preferred way to handle single keystrokes is to use **Key Bindings**. Key Bindings map a key press to a [Command](~/api/Terminal.Gui.Command.yml). A view can declare which commands it supports, and provide a lambda that implements the functionality of the command, using `View.AddCommand()`. Use the `View.Keybindings` to configure the key bindings.

## Updated Mouse API

The API for mouse input is now internally consistent and easiser to use.

* The `MouseEvent` class replaces `MouseEventEventArgs`.
* More granular APIs are provided to ease handling specific mouse actions. See [Mouse API](mouse.md).
* Views can use the `View.Highlight` event to have the view be visibly highlighted on various mouse events.
* Views can set `View.WantContinousButtonPresses = true` to ahve their `Command.Accept` command be invoked repeatedly as the user holds a mouse button down on the view.