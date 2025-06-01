# Drawing (Text, Lines, and Color)

Terminal.Gui provides a set of APIs for formatting text, line drawing, and character-based graphing. 

## Drawing Taxonomy & Lexicon

[!INCLUDE [Drawing Lexicon](~/includes/drawing-lexicon.md)]

# View Drawing API

Terminal.Gui apps draw using the @Terminal.Gui.ViewBase.View.Move(System.Int32,System.Int32) and @Terminal.Gui.ViewBase.View.AddRune(System.Text.Rune) APIs. Move selects the column and row of the cell and AddRune places the specified glyph in that cell using the @Terminal.Gui.Drawing.Attribute that was most recently set via @Terminal.Gui.ViewBase.View.SetAttribute(Terminal.Gui.Drawing.Attribute). The @Terminal.Gui.Drivers.ConsoleDriver caches all changed Cells and efficiently outputs them to the terminal each iteration of the Application. In other words, Terminal.Gui uses deferred rendering. 

## Coordinate System for Drawing

The @Terminal.Gui.ViewBase.View draw APIs all take coordinates specified in *Viewport-Relative* coordinates. That is, `0, 0` is the top-left cell visible to the user.

See [Layout](layout.md) for more details of the Terminal.Gui coordinate system.

## Outputting unformatted text

1) Moving the draw cursor using @Terminal.Gui.ViewBase.View.Move(System.Int32,System.Int32).
2) Setting the attributes using @Terminal.Gui.ViewBase.View.SetAttribute(Terminal.Gui.Drawing.Attribute).
3) Outputting glyphs by calling @Terminal.Gui.ViewBase.View.AddRune(System.Text.Rune) or @Terminal.Gui.ViewBase.View.AddStr(System.String) .

## Outputting formatted text

1) Adding the text to a @Terminal.Gui.Text.TextFormatter object.
2) Setting formatting options, such as @Terminal.Gui.Text.TextFormatter.Alignment.
3) Calling @Terminal.Gui.Text.TextFormatter.Draw(System.Drawing.Rectangle,Terminal.Gui.Drawing.Attribute,Terminal.Gui.Drawing.Attribute,System.Drawing.Rectangle,Terminal.Gui.Drivers.IConsoleDriver).

## Line drawing

1) Add the lines via @Terminal.Gui.Drawing.LineCanvas
2) Either render the line canvas via @Terminal.Gui.Drawing.LineCanvas.GetMap or let the @Terminal.Gui.ViewBase.View do so automatically (which enables automatic line joining across Views).

## When Drawing Occurs

The @Terminal.Gui.App.Application MainLoop will iterate over all Views in the view hierarchy performing the following steps:

0) Determines if @Terminal.Gui.ViewBase.View.NeedsDraw or @Terminal.Gui.ViewBase.View.SubViewNeedsDraw are set. If neither is set, processing stops.
1) Sets the clip to the view's Frame.
2) Draws the @Terminal.Gui.ViewBase.View.Border and @Terminal.Gui.ViewBase.View.Padding (but NOT the Margin).
3) Sets the clip to the view's Viewport.
4) Sets the Normal color scheme.
5) Calls Draw on any @Terminal.Gui.ViewBase.View.SubViews.
6) Draws @Terminal.Gui.ViewBase.View.Text.
7) Draws any non-text content (the base View does nothing.)
8) Sets the clip back to the view's Frame.
9) Draws @Terminal.Gui.ViewBase.View.LineCanvas (which may have been added to by any of the steps above).
10) Draws the @Terminal.Gui.ViewBase.View.Border and @Terminal.Gui.ViewBase.View.Padding SubViews (just the subviews). (but NOT the Margin).
11) The Clip at this point excludes all SubViews NOT INCLUDING their Margins. This clip is cached so @Terminal.Gui.ViewBase.View.Margin can be rendered later.
12) DrawComplete is raised.
13) The current View's Frame NOT INCLUDING the Margin is excluded from the current Clip region.

Most of the steps above can be overridden by developers using the standard [Terminal.Gui Cancellable Work Pattern](cancellable-work-pattern.md). For example, the base @Terminal.Gui.ViewBase.View always clears the viewport. To override this, a subclass can override @Terminal.Gui.ViewBase.View.OnClearingViewport to simply return `true`. Or, a user of `View` can subscribe to the @Terminal.Gui.ViewBase.View.ClearingViewport event and set the `Cancel` argument to `true`.

Then, after the above steps have completed, the Mainloop will iterate through all views in the view hierarchy again, this time calling Draw on any @Terminal.Gui.ViewBase.View.Margin objects, using the cached Clip region mentioned above. This enables Margin to be transparent.

### Declaring that drawing is needed

If a View need to redraw because something changed within it's Content Area it can call @Terminal.Gui.ViewBase.View.SetNeedsDraw. If a View needs to be redrawn because something has changed the size of the Viewport, it can call @Terminal.Gui.ViewBase.View.SetNeedsLayout.

## Clipping

Clipping enables better performance and features like transparent margins by ensuring regions of the terminal that need to be drawn actually get drawn by the @Terminal.Gui.Drivers.ConsoleDriver. Terminal.Gui supports non-rectangular clip regions with @Terminal.Gui.Drawing.Region. @Terminal.Gui.Drivers.ConsoleDriver.Clip is the application managed clip region and is managed by @Terminal.Gui.App.Application. Developers cannot change this directly, but can use @Terminal.Gui.ViewBase.View.SetClipToScreen, @Terminal.Gui.ViewBase.View.SetClip(Terminal.Gui.Drawing.Region), @Terminal.Gui.ViewBase.View.SetClipToFrame, etc...


## Cell

The @Terminal.Gui.Drawing.Cell class represents a single cell on the screen. It contains a character and an attribute. The character is of type `Rune` and the attribute is of type @Terminal.Gui.Drawing.Attribute.

`Cell` is not exposed directly to the developer. Instead, the @Terminal.Gui.Drivers.ConsoleDriver classes manage the `Cell` array that represents the screen.

To draw a `Cell` to the screen, use @Terminal.Gui.ViewBase.View.Move(System.Int32,System.Int32) to specify the row and column coordinates and then use the @Terminal.Gui.ViewBase.View.AddRune(System.Int32,System.Int32,System.Text.Rune) method to draw a single glyph.  

// ... existing code ...

## Attribute 

The @Terminal.Gui.Drawing.Attribute class represents the formatting attributes of a `Cell`. It exposes properties for the foreground and background colors as well as the text style. The foreground and background colors are of type @Terminal.Gui.Drawing.Color. Bold, underline, and other formatting attributes are supported via the @Terminal.Gui.Drawing.Attribute.Style property.

Use @Terminal.Gui.ViewBase.View.SetAttribute(Terminal.Gui.Drawing.Attribute) to indicate which Attribute subsequent @Terminal.Gui.ViewBase.View.AddRune(System.Text.Rune) and @Terminal.Gui.ViewBase.View.AddStr(System.String) calls will use:

```cs
// This is for illustration only. Developers typically use SetAttributeForRole instead.
SetAttribute (new Attribute (Color.Red, Color.Black, Style.Underline));
AddStr ("Red on Black Underlined.");
```

In the above example a hard-coded Attribute is set. Normally, developers will use @Terminal.Gui.ViewBase.View.SetAttributeForRole(Terminal.Gui.Drawing.VisualRole) to have the system use the Attributes associated with a `VisualRole` (see below).

```cs
// Modify the View's Scheme such that Focus is Red on Black Underlined
SetScheme (new Scheme (Scheme)
    {
        Focus = new Attribute (Color.Red, Color.Black, Style.Underline)
    });
    
SetAttributeForRole (VisualRole.Focus);
AddStr ("Red on Black Underlined.");
```

// ... existing code ...

## VisualRole

Represents the semantic visual role of a visual element rendered by a View (e.g., Normal text, Focused item, Active selection).

@Terminal.Gui.Drawing.VisualRole provides a set of predefined VisualRoles:

[!code-csharp[VisualRole.cs](../../Terminal.Gui/Drawing/VisualRole.cs)]

## Schemes

[!INCLUDE [Scheme Overview](~/includes/scheme-overview.md)]

See [Scheme Deep Dive](scheme.md) for more details.

## Text Formatting

Terminal.Gui supports text formatting using @Terminal.Gui.Text.TextFormatter. @Terminal.Gui.Text.TextFormatter provides methods for formatting text using the following formatting options:

* Horizontal Alignment - Left, Center, Right
* Vertical Alignment - Top, Middle, Bottom
* Word Wrap - Enabled or Disabled
* Formatting Hot Keys

## Glyphs

The @Terminal.Gui.Drawing.Glyphs class defines the common set of glyphs used to draw checkboxes, lines, borders, etc... The default glyphs can be changed per-ThemeScope via @Terminal.Gui.Configuration.ConfigurationManager. 

## Line Drawing

Terminal.Gui supports drawing lines and shapes using box-drawing glyphs. The @Terminal.Gui.Drawing.LineCanvas class provides *auto join*, a smart TUI drawing system that automatically selects the correct line/box drawing glyphs for intersections making drawing complex shapes easy. See @Terminal.Gui.Drawing.LineCanvas.

## Thickness

Describes the thickness of a frame around a rectangle. The thickness is specified for each side of the rectangle using a @Terminal.Gui.Drawing.Thickness object. The Thickness class contains properties for the left, top, right, and bottom thickness. The @Terminal.Gui.ViewBase.Adornment class uses @Terminal.Gui.Drawing.Thickness to support drawing the frame around a view. 

See [View Deep Dive](View.md) for details.

## Diagnostics

The @Terminal.Gui.ViewBase.ViewDiagnosticFlags.DrawIndicator flag can be set on @Terminal.Gui.ViewBase.View.Diagnostics to cause an animated glyph to appear in the `Border` of each View. The glyph will animate each time that View's `Draw` method is called where either @Terminal.Gui.ViewBase.View.NeedsDraw or @Terminal.Gui.ViewBase.View.SubViewNeedsDraw is set. 