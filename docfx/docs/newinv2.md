# Terminal.Gui v2

This document provides an overview of the new features and improvements in Terminal.Gui v2.

For information on how to port code from v1 to v2, see the [v1 To v2 Migration Guide](migratingfromv1.md).

## Modern Look & Feel 

Apps built with Terminal.Gui now feel modern thanks to these improvements:

* *TrueColor support* - 24-bit color support for Windows, Mac, and Linux. Legacy 16-color systems are still supported, automatically. See [TrueColor](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#truecolor) for details.
* *Enhanced Borders and Padding* - Terminal.Gui now supports a `Border`, `Margin`, and `Padding` property on all views. This simplifies View development and enables a sophisticated look and feel. See [Adornments](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#adornments) for details.
* *User Configurable Color Themes* - See [Color Themes](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#color-themes) for details.
* *Enhanced Unicode/Wide Character support *- Terminal.Gui now supports the full range of Unicode/wide characters. See [Unicode](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#unicode) for details.
* *Line Canvas* - Terminal.Gui now supports a line canvas enabling high-performance drawing of lines and shapes using box-drawing glyphs. `LineCanvas` provides *auto join*, a smart TUI drawing system that automatically selects the correct line/box drawing glyphs for intersections making drawing complex shapes easy. See [Line Canvas](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#line-canvas) for details.

## Simplified API

The entire library has been reviewed and simplified. As a result, the API is more consistent and uses modern .NET API standards (e.g. for events). This refactoring resulted in the removal of thousands of lines of code, better unit tests, and higher performance than v1. See [Simplified API](overview.md#simplified-api) for details.

## View Improvements

* *Life Cycle Management* - 
* In v1, `View` was derived from `Responder` which supported `IDisposable`. In v2, `Responder` has been removed and `View` is the base-class supporting `IDisposable`. 
* `Application.Init` no longer automatically creates a toplevel or sets `Applicaton.Top`; app developers must explicitly create the toplevel view and pass it to `Appliation.Run` (or use `Application.Run<myTopLevel>`). Developers are responsible for calling `Dispose` on any toplevel they create before exiting. 
* *Adornments* - 
* *Built-in Scrolling/Virtual Content Area* - In v1, to have a view a user could scroll required either a bespoke scrolling implementation, inheriting from `ScrollView`, or managing the complexity of `ScrollBarView` directly. In v2, the base-View class supports scrolling inherently. The area of a view visible to the user at a given moment was previously a rectangle called `Bounds`. `Bounds.Location` was always `Point.Empty`. In v2 the visible area is a rectangle called `Viewport` which is a protal into the Views content, which can be bigger (or smaller) than the area visible to the user. Causing a view to scroll is as simple as changing `View.Viewport.Location`. The View's content described by `View.ContentSize`. See [Layout](layout.md) for details.
* *Computed Layout Improvements* - 
* *`Pos.AnchorEnd ()`* - New to v2 is `Pos.AnchorEnd ()` (with no parameters) which allows a view to be anchored to the right or bottom of the Superview. 
* *`Dim.Auto`* - Views can now be sized to their content (either Text or Subveiws) using `Dim.Auto` for width or height. This replaces `View.AutoSize` in v1.
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