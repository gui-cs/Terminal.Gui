# Drawing (Text, Lines, and Color)

Terminal.Gui provides a set of APIs for formatting text, line drawing, and character-based graphing. 

## Drawing Taxonomy & Lexicon

[!INCLUDE [Drawing Lexicon](~/includes/drawing-lexicon.md)]

# View Drawing API

Terminal.Gui apps draw using the @Terminal.Gui.View.Move(System.Int32,System.Int32) and @Terminal.Gui.View.AddRune(System.Text.Rune) APIs. Move selects the column and row of the cell and AddRune places the specified glyph in that cell using the @Terminal.Gui.Attribute that was most recently set via @Terminal.Gui.View.SetAttribute(Terminal.Gui.Attribute). The driver caches all changed Cells and efficiently outputs them to the terminal each iteration of the Application. In other words, Terminal.Gui uses deferred rendering. 

## Drawing Lifecycle

**Drawing occurs during Application MainLoop iterations**, not immediately when draw-related methods are called. This deferred rendering approach provides better performance and ensures visual consistency.

### MainLoop Iteration Process

Each iteration of the @Terminal.Gui.Application MainLoop (throttled to a maximum rate) performs these steps in order:

1. **Layout** - Views that need layout are measured and positioned (@Terminal.Gui.View.LayoutSubviews is called)
2. **Draw** - Views that need drawing update the driver's back buffer (@Terminal.Gui.View.Draw is called)
3. **Write** - The driver writes changed portions of the back buffer to the actual terminal
4. **Cursor** - The driver ensures the cursor is positioned correctly with appropriate visibility

### When Drawing Actually Occurs

- **Normal Operation**: Drawing happens automatically during MainLoop iterations when @Terminal.Gui.View.NeedsDraw or @Terminal.Gui.View.SubViewNeedsDraw is set
- **Forced Update**: @Terminal.Gui.Application.LayoutAndDraw can be called to immediately trigger layout and drawing outside of the normal iteration cycle
- **Testing**: Tests can call @Terminal.Gui.View.Draw directly to update the back buffer, then call @Terminal.Gui.IDriver.Refresh to output to the terminal

**Important**: Calling `View.Draw()` does not immediately update the terminal screen. It only updates the driver's back buffer. The actual terminal output occurs when the driver's `Refresh()` method is called, which happens automatically during MainLoop iterations.

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
3) Calling @Terminal.Gui.TextFormatter.Draw(Terminal.Gui.IDriver, System.Drawing.Rectangle,Terminal.Gui.Attribute,Terminal.Gui.Attribute,System.Drawing.Rectangle).

## Line drawing

1) Add the lines via @Terminal.Gui.LineCanvas
2) Either render the line canvas via @Terminal.Gui.LineCanvas.GetMap or let the @Terminal.Gui.View do so automatically (which enables automatic line joining across Views).

## When Drawing Occurs

The @Terminal.Gui.Application MainLoop will iterate over all Views in the view hierarchy performing the following steps:

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

Most of the steps above can be overridden by developers using the standard [Terminal.Gui Cancellable Work Pattern](cancellable-work-pattern.md). For example, the base @Terminal.Gui.View always clears the viewport. To override this, a subclass can override @Terminal.Gui.View.OnClearingViewport to simply return `true`. Or, a user of `View` can subscribe to the @Terminal.Gui.View.ClearingViewport event and set the `Cancel` argument to `true`.

Then, after the above steps have completed, the Mainloop will iterate through all views in the view hierarchy again, this time calling Draw on any @Terminal.Gui.View.Margin objects, using the cached Clip region mentioned above. This enables Margin to be transparent.

### Declaring that drawing is needed

If a View need to redraw because something changed within it's Content Area it can call @Terminal.Gui.View.SetNeedsDraw. If a View needs to be redrawn because something has changed the size of the Viewport, it can call @Terminal.Gui.View.SetNeedsLayout.

**Note**: Calling `SetNeedsDraw()` does not immediately cause drawing to occur. It marks the view as needing to be redrawn, which will happen in the next MainLoop iteration. To force immediate drawing (typically only needed in tests), call @Terminal.Gui.Application.LayoutAndDraw.

## Clipping

Clipping enables better performance and features like transparent margins by ensuring regions of the terminal that need to be drawn actually get drawn by the driver. Terminal.Gui supports non-rectangular clip regions with @Terminal.Gui.Region. The driver.Clip is the application managed clip region and is managed by @Terminal.Gui.Application. Developers cannot change this directly, but can use @Terminal.Gui.View.SetClipToScreen, @Terminal.Gui.View.SetClip(Terminal.Gui.Region), @Terminal.Gui.View.SetClipToFrame, etc...


## Cell

The @Terminal.Gui.Cell class represents a single cell on the screen. It contains a character and an attribute. The character is of type `Rune` and the attribute is of type @Terminal.Gui.Attribute.

`Cell` is not exposed directly to the developer. Instead, the driver classes manage the `Cell` array that represents the screen.

To draw a `Cell` to the screen, use @Terminal.Gui.View.Move(System.Int32,System.Int32) to specify the row and column coordinates and then use the @Terminal.Gui.View.AddRune(System.Int32,System.Int32,System.Text.Rune) method to draw a single glyph.  

## Attribute 

The @Terminal.Gui.Attribute class represents the formatting attributes of a `Cell`. It exposes properties for the foreground and background colors as well as the text style. The foreground and background colors are of type @Terminal.Gui.Color. Bold, underline, and other formatting attributes are supported via the @Terminal.Gui.Attribute.Style property.

Use @Terminal.Gui.View.SetAttribute(Terminal.Gui.Attribute) to indicate which Attribute subsequent @Terminal.Gui.View.AddRune(System.Text.Rune) and @Terminal.Gui.View.AddStr(System.String) calls will use:

```cs
// This is for illustration only. Developers typically use SetAttributeForRole instead.
SetAttribute (new Attribute (Color.Red, Color.Black, Style.Underline));
AddStr ("Red on Black Underlined.");
```

In the above example a hard-coded Attribute is set. Normally, developers will use @Terminal.Gui.View.SetAttributeForRole(Terminal.Gui.VisualRole) to have the system use the Attributes associated with a `VisualRole` (see below).

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

Terminal.Gui supports 24-bit true color (16.7 million colors) via the @Terminal.Gui.Color struct. The @Terminal.Gui.Color struct represents colors in ARGB32 format, with separate bytes for Alpha (transparency), Red, Green, and Blue components.

### Standard Colors (W3C+)

Terminal.Gui provides comprehensive support for W3C standard color names plus additional common terminal colors via the @Terminal.Gui.StandardColor enum. This includes all standard W3C colors (like `AliceBlue`, `Red`, `Tomato`, etc.) as well as classic terminal colors (like `AmberPhosphor`, `GreenPhosphor`).

Colors can be created from standard color names:

```cs
var color1 = new Color(StandardColor.CornflowerBlue);
var color2 = new Color(StandardColor.Tomato);
var color3 = new Color("Red");  // Case-insensitive color name parsing
```

Standard colors can also be parsed from strings:

```cs
if (Color.TryParse("CornflowerBlue", out Color color))
{
    // Use the color
}
```

### Alpha Channel and Transparency

While @Terminal.Gui.Color supports an alpha channel for transparency (values 0-255), **terminal rendering does not currently support alpha blending**. The alpha channel is primarily used to:

- Indicate whether a color should be rendered at all (alpha = 0 means fully transparent/don't render)
- Support future transparency features
- Enable terminal background pass-through (see [#2381](https://github.com/gui-cs/Terminal.Gui/issues/2381) and [#4229](https://github.com/gui-cs/Terminal.Gui/issues/4229))

**Important**: When matching colors to standard color names, the alpha channel is **ignored**. This means `Color(255, 0, 0, 255)` (opaque red) and `Color(255, 0, 0, 128)` (semi-transparent red) will both be recognized as "Red". This design decision supports the vision of enabling transparent backgrounds while still being able to identify colors semantically.

```cs
var opaqueRed = new Color(255, 0, 0, 255);
var transparentRed = new Color(255, 0, 0, 0);

// Both will resolve to "Red"
ColorStrings.GetColorName(opaqueRed);      // Returns "Red"
ColorStrings.GetColorName(transparentRed); // Returns "Red"
```

### Legacy 16-Color Support

For backwards compatibility and terminals with limited color support, Terminal.Gui maintains the legacy 16-color system via @Terminal.Gui.ColorName16. When true color is not available or when `Application.Force16Colors` is set, Terminal.Gui will map true colors to the nearest 16-color equivalent.

## VisualRole

Represents the semantic visual role of a visual element rendered by a View (e.g., Normal text, Focused item, Active selection).

@Terminal.Gui.VisualRole provides a set of predefined VisualRoles:

[!code-csharp[VisualRole.cs](../../Terminal.Gui/Drawing/VisualRole.cs)]

## Schemes

[!INCLUDE [Scheme Overview](~/includes/scheme-overview.md)]

See [Scheme Deep Dive](scheme.md) for more details.

## Text Formatting

Terminal.Gui supports text formatting using @Terminal.Gui.TextFormatter. @Terminal.Gui.TextFormatter provides methods for formatting text using the following formatting options:

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

The @Terminal.Gui.ViewDiagnosticFlags.DrawIndicator flag can be set on @Terminal.Gui.View.Diagnostics to cause an animated glyph to appear in the `Border` of each View. The glyph will animate each time that View's `Draw` method is called where either @Terminal.Gui.View.NeedsDraw or @Terminal.Gui.View.SubViewNeedsDraw is set.

## Accessing Application Drawing Context

Views can access application-level drawing functionality through `View.App`:

```csharp
public class CustomView : View
{
    protected override bool OnDrawingContent()
    {
        // Access driver capabilities through View.App
        if (App?.Driver?.SupportsTrueColor == true)
        {
            // Use true color features
            SetAttribute(new Attribute(Color.FromRgb(255, 0, 0), Color.FromRgb(0, 0, 255)));
        }
        else
        {
            // Fallback to 16-color mode
            SetAttributeForRole(VisualRole.Normal);
        }
        
        AddStr("Custom drawing with application context");
        return true;
    }
}