# Terminal.Gui v2

This document provides an overview of the new features and improvements in Terminal.Gui v2.

For information on how to port code from v1 to v2, see the [v1 To v2 Migration Guide](migratingfromv1.md).

## Modern Look & Feel 

Apps built with Terminal.Gui now feel modern thanks to these improvements:

* *TrueColor support* - 24-bit color support for Windows, Mac, and Linux. Legacy 16-color systems are still supported, automatically. See [TrueColor](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#truecolor) for details.
* *Enhanced Borders and Padding* - Terminal.Gui now supports a `Border`, `Margin`, and `Padding` property on all views. This simplifies View development and enables a sophisticated look and feel. See [Adornments](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#adornments) for details.
* *User Configurable Color Themes* - See [Color Themes](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#color-themes) for details.
* *Enhanced Unicode/Wide Character support* - Terminal.Gui now supports the full range of Unicode/wide characters. See [Unicode](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#unicode) for details.
* [LineCanvas](~/api/Terminal.Gui.LineCanvas.yml) - Terminal.Gui now supports a line canvas enabling high-performance drawing of lines and shapes using box-drawing glyphs. `LineCanvas` provides *auto join*, a smart TUI drawing system that automatically selects the correct line/box drawing glyphs for intersections making drawing complex shapes easy. See [Line Canvas](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#line-canvas) for details.

## Simplified API

The entire library has been reviewed and simplified. As a result, the API is more consistent and uses modern .NET API standards (e.g. for events). This refactoring resulted in the removal of thousands of lines of code, better unit tests, and higher performance than v1.

## [View](~/api/Terminal.Gui.View.yml) Improvements
* *Improved!* View Lifetime Management is Now Deterministic - In v1 the rules for lifetime management of `View` objects was unclear and led to non-dterministic behavior and hard to diagnose bugs. This was particularly acute in the behavior of `Application.Run`. In v2, the rules are clear and the code and unit test infrastructure tries to enforce them. See [Migrating From v1 To v2](migratingfromv1.md) for more details.
* *New!* Adornments - Adornments are a special form of View that appear outside the `Viewport`: @Terminal.Gui.View.Margin, @Terminal.Gui.View.Border, and @Terminal.Gui.View.Padding.
* *New!* Built-in Scrolling/Virtual Content Area - In v1, to have a view a user could scroll required either a bespoke scrolling implementation, inheriting from `ScrollView`, or managing the complexity of `ScrollBarView` directly. In v2, the base-View class supports scrolling inherently. The area of a view visible to the user at a given moment was previously a rectangle called `Bounds`. `Bounds.Location` was always `Point.Empty`. In v2 the visible area is a rectangle called `Viewport` which is a protal into the Views content, which can be bigger (or smaller) than the area visible to the user. Causing a view to scroll is as simple as changing `View.Viewport.Location`. The View's content described by `View.GetContentSize()`. See [Layout](layout.md) for details.
* *New!* @Terminal.Gui.DimAuto - Automatically sizes the view to fit the view's Text, SubViews, or ContentArea.
* *Improved!* @Terminal.Gui.PosAnchorEnd - New to v2 is `Pos.AnchorEnd ()` (with no parameters) which allows a view to be anchored to the right or bottom of the SuperView. 
* *New!* @Terminal.Gui.PosAlign - Aligns a set of views horizontally or vertically (left, right, center, etc...).
* *New!* @Terminal.Gui.View.Arrangement enables tiled and overlapped view arrangement and moving/resizing Views with the keyboard and mouse. See [Arrangement](arrangement.md).
* *Improved!* Keyboard [Navigation](navigation.md) has been revamped to be more reliability and ensure TUI apps built with Terminal.Gui are accessible. 

## New and Improved Built-in Views

* *[DatePicker](~/api/Terminal.Gui.DatePicker.yml)* - NEW! 
* *ScrollView* - Replaced by built-in scrolling.
* *@"Terminal.Gui.ScrollBar"* - Replaces *ScrollBarView* with a much simpler view.
* *[Slider](~/api/Terminal.Gui.Slider.yml)* - NEW!
* *[Shortcut](~/api/Terminal.Gui.Shortcut.yml)* - NEW! An opinionated (visually & API) View for displaying a command, helptext, key.
* *[Bar](~/api/Terminal.Gui.Bar.yml)* - NEW! Building-block View for containing Shortcuts. Opinionated relative to Orientation but minimially so. The basis for the new StatusBar, MenuBar, and Menu views.
* *[StatusBar](~/api/Terminal.Gui.StatusBar.yml)* - New implementation based on `Bar`
* *[MenuBar](~/api/Terminal.Gui.MenuBar.yml)* - COMING SOON! New implementation based on `Bar`
* *[ContextMenu](~/api/Terminal.Gui.ContextMenu.yml)* - COMING SOON! New implementation based on `Bar`
* *[FileDialog](~/api/Terminal.Gui.FileDialog.yml)* - The new, modern file dialog includes icons (in TUI!) for files/folders, search, and a `TreeView`. 
* *@"Terminal.Gui.ColorPicker"* - Fully supports TrueColor with the ability to choose a color using HSV, RGB, or HSL as well as W3C standard color names.

## Configuration Manager

Terminal.Gui now supports a configuration manager enabling library and app settings to be persisted and loaded from the file system. See [Configuration Manager](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#configuration-manager) for details.

## Updated Keyboard API

The API for handling keyboard input is significantly improved. See [Keyboard API](keyboard.md).

* The `Key` class replaces the `KeyEvent` struct and provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level `KeyCode` enum when possible. See [Key](~/api/Terminal.Gui.Key.yml) for more details.
* The preferred way to handle single keystrokes is to use **Key Bindings**. Key Bindings map a key press to a [Command](~/api/Terminal.Gui.Command.yml). A view can declare which commands it supports, and provide a lambda that implements the functionality of the command, using `View.AddCommand()`. Use the `View.Keybindings` to configure the key bindings.
* For better consistency and user experience, the default key for closing an app or `Toplevel` is now `Esc` (it was previously `Ctrl+Q`).

## Updated Mouse API

The API for mouse input is now internally consistent and easiser to use.

* The `MouseEvent` class replaces `MouseEventEventArgs`.
* More granular APIs are provided to ease handling specific mouse actions. See [Mouse API](mouse.md).
* Views can use the `View.Highlight` event to have the view be visibly highlighted on various mouse events.
* Views can set `View.WantContinousButtonPresses = true` to ahve their `Command.Accept` command be invoked repeatedly as the user holds a mouse button down on the view.
