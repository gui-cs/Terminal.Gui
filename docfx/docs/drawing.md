# Drawing (Text, Lines, and Color)

Terminal.Gui provides a set of APIs for formatting text, line drawing, and character-based graphing. 

## Drawing Lexicon and Taxonomy

| Term | Meaning |
|:-----|:--------|
| **Attribute** | Defines the concrete visual styling for a visual element, including Foreground color, Background color, and TextStyle. |
| **BackgroundColor** | A property of `Attribute` that describes the color of background text. |
| **Color** | Base terminal color (part of the color palette; supports TrueColor and named values like White, Black, Cyan, etc.). |
| **Cell** | A single character and its attributes which occupies a particular row and column in the terminal. Not exposed directly to the developer, but used internally by drivers. See @Terminal.Gui.Cell |
| **ForegroundColor** | A property of `Attribute` that describes the color of foreground text. |
| **Scheme** | A Scheme is a mapping from `VisualRole`s (e.g. `VisualRole.Focus`) to `Attribute`s, defining how a `View` should look based on its purpose (e.g. Menu or Dialog). |
| **Style** | A property of `Attribute` that captures additional font-like hints such as bold, italic, underline, beyond color. |
| **Theme** | A single named instance containing specific appearance settings (e.g., "Default", "Dark"). |
| **Themes** | A collection of named Theme definitions, each of which bundles visual and layout settings. |
| **VisualRole** | The semantic role/purpose of a visual element inside a View (e.g., Normal, Focus, HotFocus, Active, Disabled, ReadOnly). |

# View Drawing API

Terminal.Gui apps draw using the @Terminal.Gui.View.Move(System.Int32,System.Int32) and @Terminal.Gui.View.AddRune(System.Text.Rune) APIs. Move selects the column and row of the cell and AddRune places the specified glyph in that cell using the @Terminal.Gui.Attribute that was most recently set via @Terminal.Gui.View.SetAttribute(Terminal.Gui.Attribute). The @Terminal.Gui.ConsoleDriver caches all changed Cells and efficiently outputs them to the terminal each iteration of the Application. In other words, Terminal.Gui uses deferred rendering. 

## Coordinate System for Drawing

The @Terminal.Gui.View draw APIs all take coordinates specified in *Viewport-Relative* coordinates. That is, `0, 0` is the top-left cell visible to the user.

See [Layout](layout.md) for more details of the Terminal.Gui coordinate system.

## Outputting unformatted text

1) Moving the draw cursor using @Terminal.Gui.View.Move(System.Int32,System.Int32).
2) Setting the attributes using @Terminal.Gui.View.SetAttribute(Terminal.Gui.Attribute).
3) Outputting glyphs by calling @Terminal.Gui.View.AddRune(System.Text.Rune) or @Terminal.Gui.View.AddStr(System.String) .

## Outputting formatted text

1) Adding the text to a @Terminal.Gui.TextFormatter object.
2) Setting formatting options, such as @Terminal.Gui.TextFormatter.Alignment.
3) Calling @Terminal.Gui.TextFormatter.Draw(System.Drawing.Rectangle,Terminal.Gui.Attribute,Terminal.Gui.Attribute,System.Drawing.Rectangle,Terminal.Gui.IConsoleDriver).

## Line drawing

1) Add the lines via @Terminal.Gui.LineCanvas.AddLine(System.Drawing.Point,System.Int32,Terminal.Gui.Orientation,Terminal.Gui.LineStyle,System.Nullable{Terminal.Gui.Attribute}).
2) Either render the line canvas via @Terminal.Gui.LineCanvas.GetMap or let the @Terminal.Gui.View do so automatically (which enables automatic line joining across Views).

## When Drawing Occurs

The @Terminal.Gui.Application MainLoop will iterate over all Views in the view hierarchy, starting with @Terminal.Gui.Application.Toplevels. The @Terminal.Gui.View.Draw method will be called which, in turn:

0) Determines if @Terminal.Gui.View.NeedsDraw or @Terminal.Gui.View.SubViewNeedsDraw are set. If neither is set, processing stops.
1) Sets the clip to the view's Frame.
2) Draws the @Terminal.Gui.View.Border and @Terminal.Gui.View.Padding (but NOT the Margin).
3) Sets the clip to the view's Viewport.
4) Sets the Normal color scheme.
5) Calls Draw on any @Terminal.Gui.View.SubViews.
6) Draws @Terminal.Gui.View.Text.
7) Draws any non-text content (the base View does nothing.)
8) Sets the clip back to the view's Frame.
9) Draws @Terminal.Gui.View.LineCanvas (which may have been added to by any of the steps above).
10) Draws the @Terminal.Gui.View.Border and @Terminal.Gui.View.Padding SubViews (just the subviews). (but NOT the Margin).
11) The Clip at this point excludes all SubViews NOT INCLUDING their Margins. This clip is cached so @Terminal.Gui.View.Margin can be rendered later.
12) DrawComplete is raised.
13) The current View's Frame NOT INCLUDING the Margin is excluded from the current Clip region.

Most of the steps above can be overridden by developers using the standard [Terminal.Gui Cancellable Work Pattern](cancellable_work_pattern.md). For example, the base @Terminal.Gui.View always clears the viewport. To override this, a subclass can override @Terminal.Gui.View.OnClearingViewport to simply return `true`. Or, a user of `View` can subscribe to the @Terminal.Gui.View.ClearingViewport event and set the `Cancel` argument to `true`.

Then, after the above steps have completed, the Mainloop will iterate through all views in the view hierarchy again, this time calling Draw on any @Terminal.Gui.View.Margin objects, using the cached Clip region mentioned above. This enables Margin to be transparent.

### Declaring that drawing is needed

If a View need to redraw because something changed within it's Content Area it can call @Terminal.Gui.View.SetNeedsDraw. If a View needs to be redrawn because something has changed the size of the Viewport, it can call @Terminal.Gui.View.SetNeedsLayout.

## Clipping

Clipping enables better performance and features like transparent margins by ensuring regions of the terminal that need to be drawn actually get drawn by the @Terminal.Gui.ConsoleDriver. Terminal.Gui supports non-rectangular clip regions with @Terminal.Gui.Region. @Terminal.Gui.ConsoleDriver.Clip is the application managed clip region and is managed by @Terminal.Gui.Application. Developers cannot change this directly, but can use @Terminal.Gui.View.ClipToScreen, @Terminal.Gui.View.SetClip(Region), @Terminal.Gui.View.ClipToFrame, and @Terminal.Gui.ClipToViewPort.


## Cell

The @Terminal.Gui.Cell class represents a single cell on the screen. It contains a character and an attribute. The character is of type `Rune` and the attribute is of type @Terminal.Gui.Attribute.

`Cell` is not exposed directly to the developer. Instead, the @Terminal.Gui.ConsoleDriver.yml) classes manage the `Cell` array that represents the screen.

To draw a `Cell` to the screen, use Terminal.Gui.View.Move(System.Int32,System.Int32) to specify the row and column coordinates and then use the @Terminal.Gui.View.AddRune(System.Int32,System.Int32,System.Text.Rune) method to draw a single glyph.  

## Unicode

Terminal.Gui supports the full range of Unicode/wide characters. This includes emoji, CJK characters, and other wide characters. For Unicode characters that require more than one cell, `AddRune` and the `ConsoleDriver` automatically manage the cells. Extension methods to `Rune` are provided to determine if a `Rune` is a wide character and to get the width of a `Rune`.

See the Character Map sample app in the [UI Catalog](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#ui-catalog) for examples of Unicode characters.

## Attribute 

The @Terminal.Gui.Attribute class represents the formatting attributes of a `Cell`. It exposes properties for the foreground and background colors as well as the text style. The foreground and background colors are of type @Terminal.Gui.Color. Bold, underline, and other formatting attributes are supported via the @Terminal.Gui.Attribute.Style property.

Use @Terminal.Gui.View.SetAttribute to indicate which Attribute subsequent @Terminal.Gui.View.AddRune and @Terminal.Gui.View.AddStr calls will use:

```cs
// This is for illustration only. Developers typically use SetAttributeForRole instead.
SetAttribute (new Attribute (Color.Red, Color.Black, Style.Underline));
AddStr ("Red on Black Underlined.");
```

In the above example a hard-coded Attribute is set. Normally, developers will use @Terminal.Gui.View.SetAttributeForRole(VisualRole) to have the system use the Attributes associated with a `VisualRole` (see below).

```cs
// Modify the View's Scheme such that Focus is Red on Black Underlined
SetScheme (new Scheme (Scheme)
    {
        Focus = new Attribute (Color.Red, Color.Black, Style.Underline)
    });
    
SetAttributeForRole (VisualRole.Focus);
AddStr ("Red on Black Underlined.");
```

## Color

Color is supported on all platforms, including Windows, Mac, and Linux. The default colors are 24-bit RGB colors, but the library will gracefully degrade to 16-colors if the terminal does not support 24-bit color, and black and white if the terminal does not support 16-colors.

The `Color` class represents a color. It provides automatic mapping between the legacy 4-bit (16-color) system and 24-bit colors. It contains properties for the red, green, and blue components of the color. The `StandardColor` enum provides a set of predefined colors.

```cs
Attribute attribute = new Attribute(StandardColor.Goldenrod, StandardColor.Wheat Style.None);
```

## VisualRole

Represents the semantic visual role of a visual element rendered by a View (e.g., Normal text, Focused item, Active selection).

@Terminal.Gui.VisualRole provides a set of predefined VisualRoles:

[!code-csharp[VisualRole.cs](../../Terminal.Gui/Drawing/VisualRole.cs)]

## Schemes

[!code-md[Scheme Overview](scheme.md#Scheme-Overview)]

See [Scheme Deep Dive](scheme.md) for more details.

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