# Drawing (Text, Lines, and Color)

Terminal.Gui provides a set of APIs for formatting text, line drawing, and character-based graphing. The fundamental concept is a `Cell` which ocupises a particular row and column in the terminal. A Cell includes the character (glyph) that should be rendred by the terminal, and attributes that indicate how the glphy should be rendered (e.g. the foreground and background color).

Color is supported on all platforms, including Windows, Mac, and Linux. The default colors are 24-bit RGB colors, but the library will gracefully degrade to 16-colors if the terminal does not support 24-bit color, and black and white if the terminal does not support 16-colors.

## View Drawing API

A `View` will typically draw text when the [OnDrawContent](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnDrawContent_) is called (or the `DrawContent` event is received). 

Outputing text directly involves:

a) Moving the draw cursor using the `Move` API.
b) Setting the attributes using `SetAttribute`.
c) Outputting glyphs by calling `AddRune` or `AddStr`

Outputting formatted text involves:

a) Adding the text to a `TextFormatter` object.
b) Setting formatting options, such as `TextFormatter.TextAlignment`.
c) calling `TextFormatter.Draw` 

Line drawing is accomplished using the `LineCanvas` API:

a) Add the lines via `LineCanvas.Add`.
b) Either render the line canvas via `LineCanvas.Draw` or let the `View` do so automatically (which enables automatic line joining across Views).

## Coordinate System for Drawing

The `View` draw APIs, including the `OnDrawContent` method, the `DrawContent` event, and the `View.Move` method, all take coordinates specified in *Viewport-Relative* coordinates. That is, `0, 0` is the top-left cell visible to the user.

See [Layout](layout.html) for more details of the Terminal.Gui coordinate system.

## Cell

The `Cell` class represents a single cell on the screen. It contains a character and an attribute. The character is of type `Rune` and the attribute is of type `Attribute`. 

Normally `Cell` is not exposed directly to the developer. Instead, the `ConsoleDriver` classes manage the `Cell` array that represents the screen.

To draw a `Cell` to the screen, first use `View.Move` to specify the row and column coordinates and then use the `View.AddRune` method to draw a single glyph. To draw a string, use `View.AddStr`. 

## Unicode

Terminal.Gui supports the full range of Unicode/wide characters. This includes emoji, CJK characters, and other wide characters. For Unicode characters that require more than one cell, `AddRune` and the `ConsoleDriver` automatically manage the cells. Extension methods to `Rune` are provided to determine if a `Rune` is a wide character and to get the width of a `Rune`.

See the Character Map sample app in the [UI Catalog](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#ui-catalog) for examples of Unicode characters.

## Attribute 

The `Attribute` class represents the formatting attributes of a `Cell`. It exposes properties for the foreground and background colors. The foreground and background colors are of type `Color`. In the future, it will expose properties for bold, underline, and other formatting attributes.

## Color

The `Color` class represents a color. It provides automatic mapping between the legacy 4-bit (16-color) system and 24-bit colors. It contains properties for the red, green, and blue components of the color. The red, green, and blue components are of type `byte`. The `Color` class also contains a static property for each of the 16 ANSI colors.

## Color Schemes

Terminal.Gui supports named collection of colors called `ColorScheme`s. Three built-in color schemes are provided: "Default", "Dark", and "Light". Additional color schemes can be defined via [Configuration Manager](). 

Color schemes support defining colors for various states of a view. The following states are supported:

* Normal - The color of normal text.
* HotNormal - The color of text indicating a [Hotkey]().
* Focus - The color of text that indicates the view has focus.
* HotFocus - The color of text indicating a hot key, when the view has focus.
* Disabled - The state of a view when it is disabled.

Change the colors of a view by setting the `View.ColorScheme` property.

## Text Formatting

Terminal.Gui supports text formatting using the [TextFormatter]() class. The `TextFormatter` class provides methods for formatting text using the following formatting options:

* Horizontal Alignment - Left, Center, Right
* Vertical Alignment - Top, Middle, Bottom
* Word Wrap - Enabled or Disabled
* Formatting Hot Keys

## Glyphs

Terminal.Gui supports rendering glyphs using the `Glyph` class. The `Glyph` class represents a single glyph. It contains a character and an attribute. The character is of type `Rune` and the attribute is of type `Attribute`. A set of static properties are provided for the standard glyphs used for standard views (e.g. the default indicator for [Button](~/api/Terminal.Gui.Button.yml)) and line drawing (e.g. [LineCanvas](~/api/Terminal.Gui.LineCanvas.yml)).

## Line Drawing

Terminal.Gui supports drawing lines and shapes using box-drawing glyphs. The `LineCanvas` class provides *auto join*, a smart TUI drawing system that automatically selects the correct line/box drawing glyphs for intersections making drawing complex shapes easy. See [Line Canvas](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#line-canvas) for details. The `Snake` and `Line Drawing` Scenarios in the [UI Catalog](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#ui-catalog) sample app are both examples of the power of the `LineCanvas`.

## Thickness

Describes the thickness of a frame around a rectangle. The thickness is specified for each side of the rectangle using a `Thickness` object. The `Thickness` object contains properties for the left, top, right, and bottom thickness. The `Frame` class uses `Thickness` to support drawing the frame around a view. The `View` class contains three `Frame`-dervied properties: 

* `Margin` - The space between the view and its peers (other views at the same level in the view hierarchy).
* `Border` - The space between the view and its Padding. This is where the frame, title, and other "Adornments" are drawn.
* `Padding` - The space between the view and its content. This is where the text, images, and other content is drawn. The inner rectangle of `Padding` is the `Bounds` of a view. 

See [View](~/api/Terminal.Gui.View.yml) for details.

