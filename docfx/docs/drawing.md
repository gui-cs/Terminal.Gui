# Drawing (Text, Lines, and Color)

Terminal.Gui provides a set of APIs for formatting text, line drawing, and character-based graphing. The fundamental concept is a @Terminal.Gui.Cell which occupies a particular row and column in the terminal. A Cell includes the character (glyph) that should be rendred by the terminal, and attributes that indicate how the glyph should be rendered (e.g. the foreground and background color).

Color is supported on all platforms, including Windows, Mac, and Linux. The default colors are 24-bit RGB colors, but the library will gracefully degrade to 16-colors if the terminal does not support 24-bit color, and black and white if the terminal does not support 16-colors.

## View Drawing API

Terminal.Gui apps draw using the @Terminal.Gui.View.Move(System.Int32,System.Int32) and @Terminal.Gui.View.AddRune(System.Text.Rune) APIs. Move selects the column and row of the cell and AddRune places the specified glyph in that cell using the @Terminal.Gui.Attribute that was most recently set via @Terminal.Gui.View.SetAttribute(Terminal.Gui.Attribute). The @Terminal.Gui.ConsoleDriver caches all changed Cells and efficiently outputs them to the terminal each iteration of the Application. In other words, Terminal.Gui uses deferred rendering. 

Outputting unformatted text involves:

1) Moving the draw cursor using @Terminal.Gui.View.Move(System.Int32,System.Int32).
2) Setting the attributes using @Terminal.Gui.View.SetAttribute(Terminal.Gui.Attribute).
3) Outputting glyphs by calling @Terminal.Gui.View.AddRune(System.Text.Rune) or @Terminal.Gui.View.AddStr(System.String) .

Outputting formatted text involves:

1) Adding the text to a @Terminal.Gui.TextFormatter object.
2) Setting formatting options, such as @Terminal.Gui.TextFormatter.Alignment.
3) Calling @Terminal.Gui.TextFormatter.Draw(System.Drawing.Rectangle,Terminal.Gui.Attribute,Terminal.Gui.Attribute,System.Drawing.Rectangle,Terminal.Gui.IConsoleDriver).

Line drawing is accomplished using the @Terminal.Gui.LineCanvas API:

1) Add the lines via @Terminal.Gui.LineCanvas.AddLine(System.Drawing.Point,System.Int32,Terminal.Gui.Orientation,Terminal.Gui.LineStyle,System.Nullable{Terminal.Gui.Attribute}).
2) Either render the line canvas via @Terminal.Gui.LineCanvas.GetMap or let the @Terminal.Gui.View do so automatically (which enables automatic line joining across Views).

### Drawing occurs each MainLoop Iteration

The @Terminal.Gui.Application MainLoop will iterate over all Views in the view hierarchy, starting with @Terminal.Gui.Application.Toplevels. The @Terminal.Gui.View.Draw method will be called which, in turn:

0) Determines if @Terminal.Gui.View.NeedsDraw or @Terminal.Gui.View.SubviewNeedsDraw are set. If neither is set, processing stops.
1) Sets the clip to the view's Frame.
2) Draws the @Terminal.Gui.View.Border and @Terminal.Gui.View.Padding (but NOT the Margin).
3) Sets the clip to the view's Viewport.
4) Sets the Normal color scheme.
5) Calls Draw on any @Terminal.Gui.View.Subviews.
6) Draws @Terminal.Gui.View.Text.
7) Draws any non-text content (the base View does nothing.)
8) Sets the clip back to the view's Frame.
9) Draws @Terminal.Gui.View.LineCanvas (which may have been added to by any of the steps above).
10) Draws the @Terminal.Gui.View.Border and @Terminal.Gui.View.Padding Subviews (just the subviews). (but NOT the Margin).
11) The Clip at this point excludes all Subviews NOT INCLUDING their Margins. This clip is cached so @Terminal.Gui.View.Margin can be rendered later.
12) DrawComplete is raised.
13) The current View's Frame NOT INCLUDING the Margin is excluded from the current Clip region.

Most of the steps above can be overridden by developers using the standard [Terminal.Gui cancellable event pattern](events.md). For example, the base @Terminal.Gui.View always clears the viewport. To override this, a subclass can override @Terminal.Gui.View.OnClearingViewport to simply return `true`. Or, a user of `View` can subscribe to the @Terminal.Gui.View.ClearingViewport event and set the `Cancel` argument to `true`.

Then, after the above steps have completed, the Mainloop will iterate through all views in the view hierarchy again, this time calling Draw on any @Terminal.Gui.View.Margin objects, using the cached Clip region mentioned above. This enables Margin to be transparent.


### Declaring that drawing is needed

If a View need to redraw because something changed within it's Content Area it can call @Terminal.Gui.View.SetNeedsDraw. If a View needs to be redrawn because something has changed the size of the Viewport, it can call @Terminal.Gui.View.SetNeedsLayout.

### Clipping

> [!IMPORTANT]
> Clipping is still under development and the API is subject to change.


Clipping enables better performance and features like transparent margins by ensuring regions of the terminal that need to be drawn actually get drawn by the @Terminal.Gui.ConsoleDriver. Terminal.Gui supports non-rectangular clip regions with @Terminal.Gui.Region. @Terminal.Gui.ConsoleDriver.Clip is the application managed clip region and is managed by @Terminal.Gui.Application. Developers cannot change this directly, but can use @Terminal.Gui.View.ClipToScreen, @Terminal.Gui.View.SetClip(Region), @Terminal.Gui.View.ClipToFrame, and @Terminal.Gui.ClipToViewPort.

## Coordinate System for Drawing

The @Terminal.Gui.View draw APIs all take coordinates specified in *Viewport-Relative* coordinates. That is, `0, 0` is the top-left cell visible to the user.

See [Layout](layout.md) for more details of the Terminal.Gui coordinate system.

## Cell

The @Terminal.Gui.Cell class represents a single cell on the screen. It contains a character and an attribute. The character is of type `Rune` and the attribute is of type @Terminal.Gui.Attribute.

`Cell` is not exposed directly to the developer. Instead, the @Terminal.Gui.ConsoleDriver.yml) classes manage the `Cell` array that represents the screen.

To draw a `Cell` to the screen, use Terminal.Gui.View.Move(System.Int32,System.Int32) to specify the row and column coordinates and then use the @Terminal.Gui.View.AddRune(System.Int32,System.Int32,System.Text.Rune) method to draw a single glyph.  

## Unicode

Terminal.Gui supports the full range of Unicode/wide characters. This includes emoji, CJK characters, and other wide characters. For Unicode characters that require more than one cell, `AddRune` and the `ConsoleDriver` automatically manage the cells. Extension methods to `Rune` are provided to determine if a `Rune` is a wide character and to get the width of a `Rune`.

See the Character Map sample app in the [UI Catalog](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#ui-catalog) for examples of Unicode characters.

## Attribute 

The @Terminal.Gui.Attribute class represents the formatting attributes of a `Cell`. It exposes properties for the foreground and background colors. The foreground and background colors are of type @Terminal.Gui.Color. In the future, it will expose properties for bold, underline, and other formatting attributes.

## Color

The `Color` class represents a color. It provides automatic mapping between the legacy 4-bit (16-color) system and 24-bit colors. It contains properties for the red, green, and blue components of the color. The `Color` class also contains a static property for each of the 16 ANSI colors.

## Color Schemes

Terminal.Gui supports named collections of colors called @Terminal.Gui.ColorScheme. Three built-in color schemes are provided: "Default", "Dark", and "Light". Additional color schemes can be defined via [Configuration Manager](config.md). 

Color schemes support defining colors for various states of a View. The following states are supported:

* Normal - The color of normal text.
* HotNormal - The color of text indicating a @Terminal.Gui.View.Hotkey.
* Focus - The color of text that indicates the view has focus.
* HotFocus - The color of text indicating a hot key, when the view has focus.
* Disabled - The state of a view when it is disabled.

Change the colors of a view by setting the @Terminal.Gui.View.ColorScheme property.

## Text Formatting

Terminal.Gui supports text formatting using @Terminal.Gui.View.TextFormatter. @Terminal.Gui.TextFormatter provides methods for formatting text using the following formatting options:

* Horizontal Alignment - Left, Center, Right
* Vertical Alignment - Top, Middle, Bottom
* Word Wrap - Enabled or Disabled
* Formatting Hot Keys

## Glyphs

The @Terminal.Gui.Glyphs class defines the common set of glyphs used to draw checkboxes, lines, borders, etc... The default glyphs can be changed per-ThemeScope via @Terminal.Gui.ConfigurationManager. 

## Line Drawing

Terminal.Gui supports drawing lines and shapes using box-drawing glyphs. The @Terminal.Gui.LineCanvas class provides *auto join*, a smart TUI drawing system that automatically selects the correct line/box drawing glyphs for intersections making drawing complex shapes easy. See @Terminal.Gui.LineCanvas.

## Thickness

Describes the thickness of a frame around a rectangle. The thickness is specified for each side of the rectangle using a @Terminal.Gui.Thickness object. The Thickness class contains properties for the left, top, right, and bottom thickness. The @Terminal.Gui.Adornment class uses @Terminal.Gui.Thickness to support drawing the frame around a view. 

See [View Deep Dive](View.md) for details.

## Diagnostics

The @Terminal.Gui.ViewDiagnostics.DisplayIndicator flag can be set on @Terminal.Gui.View.Diagnostics to cause an animated glyph to appear in the `Border` of each View. The glyph will animate each time that View's `Draw` method is called where either @Terminal.Gui.View.NeedsDraw or @Terminal.Gui.View.SubViewNeedsDraw is set. 