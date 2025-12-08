#nullable enable
using System.Numerics;
using JetBrains.Annotations;
using SixLabors.Fonts;

namespace UICatalog.Scenarios;

/// <summary>
///     A <see cref="View"/> that renders text in large block letters using <see cref="LineCanvas"/>.
///     The text is rendered using TrueType fonts converted to vector outlines and approximated as box-drawing characters.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="BigText"/> uses SixLabors.Fonts to load TrueType fonts and extract glyph outlines as vector paths.
///         These paths are then approximated as horizontal and vertical lines that can be rendered using LineCanvas.
///         The characters are rendered at a larger scale than normal text, making them suitable for titles, headers, or
///         emphasis.
///     </para>
///     <para>
///         The view automatically sizes itself based on the text content and the configured <see cref="GlyphHeight"/>.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var bigText = new BigText
/// {
///     Text = "Hello!",
///     GlyphHeight = 8,
///     Style = LineStyle.Double,
///     Font = "Arial"
/// };
/// </code>
/// </example>
public class BigText : View
{
    private int _glyphHeight = 8;
    private LineStyle _style = LineStyle.Single;
    private Font? _font;
    private string _fontFamily = "Arial";
    private static readonly FontCollection _fontCollection = new ();

    /// <summary>
    ///     Initializes a new instance of the <see cref="BigText"/> class.
    /// </summary>
    public BigText ()
    {
        CanFocus = false;
        Height = Dim.Auto (DimAutoStyle.Content);
        Width = Dim.Auto (DimAutoStyle.Content);

        // Try to load system fonts
        TryLoadSystemFonts ();
        UpdateFont ();
    }

    private static bool _systemFontsLoaded;

    private static void TryLoadSystemFonts ()
    {
        if (_systemFontsLoaded)
        {
            return;
        }

        try
        {
            // Try to add system fonts
            if (OperatingSystem.IsWindows ())
            {
                string fontsPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Fonts));

                if (Directory.Exists (fontsPath))
                {
                    foreach (string fontFile in Directory.GetFiles (fontsPath, "*.ttf"))
                    {
                        try
                        {
                            _fontCollection.Add (fontFile);
                        }
                        catch
                        {
                            // Ignore individual font loading errors
                        }
                    }
                }
            }
            else if (OperatingSystem.IsLinux ())
            {
                string [] fontPaths =
                {
                    "/usr/share/fonts/truetype",
                    "/usr/local/share/fonts",
                    Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".fonts")
                };

                foreach (string fontPath in fontPaths)
                {
                    if (Directory.Exists (fontPath))
                    {
                        foreach (string fontFile in Directory.GetFiles (fontPath, "*.ttf", SearchOption.AllDirectories))
                        {
                            try
                            {
                                _fontCollection.Add (fontFile);
                            }
                            catch
                            {
                                // Ignore individual font loading errors
                            }
                        }
                    }
                }
            }
            else if (OperatingSystem.IsMacOS ())
            {
                string [] fontPaths =
                {
                    "/System/Library/Fonts",
                    "/Library/Fonts",
                    Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "Library/Fonts")
                };

                foreach (string fontPath in fontPaths)
                {
                    if (Directory.Exists (fontPath))
                    {
                        foreach (string fontFile in Directory.GetFiles (fontPath, "*.ttf", SearchOption.AllDirectories))
                        {
                            try
                            {
                                _fontCollection.Add (fontFile);
                            }
                            catch
                            {
                                // Ignore individual font loading errors
                            }
                        }
                    }
                }
            }

            _systemFontsLoaded = true;
        }
        catch
        {
            // If we can't load system fonts, we'll just use the built-in SixLabors fonts
        }
    }

    /// <summary>
    ///     Gets or sets the font family name to use for rendering. Default is "Arial".
    ///     If the font is not found, falls back to the default SixLabors font.
    /// </summary>
    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (_fontFamily != value)
            {
                _fontFamily = value;
                UpdateFont ();
                UpdateContentSize ();
                SetNeedsLayout ();
                SetNeedsDraw ();
            }
        }
    }

    private void UpdateFont ()
    {
        try
        {
            FontFamily family;

            if (_fontCollection.TryGet (_fontFamily, out family!))
            {
                _font = family.CreateFont (_glyphHeight, FontStyle.Regular);
            }
            else
            {
                // Fallback to system default
                family = SystemFonts.Get (_fontFamily);
                _font = family.CreateFont (_glyphHeight, FontStyle.Regular);
            }
        }
        catch
        {
            // If font not found, use system default
            try
            {
                FontFamily family = SystemFonts.Families.First ();
                _font = family.CreateFont (_glyphHeight, FontStyle.Regular);
            }
            catch
            {
                // Last resort - null font will cause fallback rendering
                _font = null;
            }
        }
    }

    /// <summary>
    ///     Gets or sets the height of each glyph in terminal cells. Default is 8.
    /// </summary>
    /// <remarks>
    ///     This controls how tall the rendered characters will be. Larger values create
    ///     taller letters but require more vertical space.
    /// </remarks>
    public int GlyphHeight
    {
        get => _glyphHeight;
        set
        {
            if (_glyphHeight != value)
            {
                _glyphHeight = value;
                UpdateFont ();
                UpdateContentSize ();
                SetNeedsLayout ();
                SetNeedsDraw ();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="LineStyle"/> used to draw the text. Default is <see cref="LineStyle.Single"/>.
    /// </summary>
    public LineStyle Style
    {
        get => _style;
        set
        {
            if (_style != value)
            {
                _style = value;
                SetNeedsDraw ();
            }
        }
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (string.IsNullOrEmpty (Text))
        {
            return true;
        }

        var lineCanvas = new LineCanvas ();
        DrawText (lineCanvas, Text, 0, 0, _font, Style, GetAttributeForRole (VisualRole.Normal));

        // Get the cell map and render it
        Dictionary<Point, Cell?> cellMap = lineCanvas.GetCellMap ();

        foreach (KeyValuePair<Point, Cell?> kvp in cellMap)
        {
            if (kvp.Value.HasValue)
            {
                Cell cell = kvp.Value.Value;

                // Check if position is within viewport
                if (kvp.Key.X < 0 || kvp.Key.X >= Viewport.Width || kvp.Key.Y < 0 || kvp.Key.Y >= Viewport.Height)
                {
                    continue;
                }

                // Move to the viewport position and add the grapheme string
                Move (kvp.Key.X, kvp.Key.Y);
                AddStr (cell.Grapheme);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            UpdateContentSize ();
            SetNeedsLayout ();
        }
    }

    private void UpdateContentSize ()
    {
        // Calculate the size needed for the text
        if (!string.IsNullOrEmpty (Text))
        {
            Size size = MeasureText (Text, _font);
            SetContentSize (size);
        }
        else
        {
            SetContentSize (new Size (0, 0));
        }
    }

    /// <summary>
    ///     Draws text using LineCanvas at the specified position.
    /// </summary>
    /// <param name="canvas">The LineCanvas to draw on.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="x">Starting X position.</param>
    /// <param name="y">Starting Y position.</param>
    /// <param name="font">The font to use for rendering.</param>
    /// <param name="style">Line style to use.</param>
    /// <param name="attribute">Optional attribute for the lines.</param>
    private static void DrawText (LineCanvas canvas, string text, int x, int y, Font? font, LineStyle style, Attribute? attribute)
    {
        if (font is null || string.IsNullOrEmpty (text))
        {
            return;
        }

        var glyphRenderer = new LineCanvasGlyphRenderer (canvas, style, attribute);
        var renderer = new TextRenderer (glyphRenderer);

        var options = new TextOptions (font)
        {
            Origin = new (x, y),
            Dpi = 96
        };

        renderer.RenderText (text, options);
    }

    /// <summary>
    ///     Measures the size required to render the given text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use for measuring.</param>
    /// <returns>The size in terminal cells.</returns>
    private static Size MeasureText (string text, Font? font)
    {
        if (string.IsNullOrEmpty (text) || font is null)
        {
            return Size.Empty;
        }

        try
        {
            FontRectangle bounds = TextMeasurer.MeasureBounds (text, new (font));

            // Convert font units to terminal cells
            // We scale based on the font size
            var width = (int)Math.Ceiling (bounds.Width / font.Size * font.Size);
            var height = (int)Math.Ceiling (bounds.Height / font.Size * font.Size);

            return new (width, height);
        }
        catch
        {
            // Fallback if measurement fails
            return new ((int)(text.Length * (font?.Size ?? 8)), (int)(font?.Size ?? 8));
        }
    }

    /// <summary>
    ///     A custom glyph renderer that converts font glyph outlines to LineCanvas lines.
    /// </summary>
    private class LineCanvasGlyphRenderer : IGlyphRenderer
    {
        private readonly LineCanvas _canvas;
        private readonly LineStyle _style;
        private readonly Attribute? _attribute;
        private Vector2 _currentPoint;
        private readonly List<Vector2> _currentPath = new ();
        private readonly int _samplesPerSegment = 5; // Number of line segments to approximate curves

        public LineCanvasGlyphRenderer (LineCanvas canvas, LineStyle style, Attribute? attribute)
        {
            _canvas = canvas;
            _style = style;
            _attribute = attribute;
        }

        public bool BeginGlyph (in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            _currentPath.Clear ();

            return true;
        }

        public void BeginFigure () { _currentPath.Clear (); }

        public void MoveTo (Vector2 point)
        {
            _currentPoint = point;

            if (_currentPath.Count > 0)
            {
                // Start a new path segment
                ProcessPath ();
                _currentPath.Clear ();
            }

            _currentPath.Add (point);
        }

        public void LineTo (Vector2 point)
        {
            _currentPath.Add (point);
            _currentPoint = point;
        }

        public void QuadraticBezierTo (Vector2 control, Vector2 end)
        {
            // Approximate quadratic Bezier with line segments
            Vector2 start = _currentPoint;

            for (var i = 1; i <= _samplesPerSegment; i++)
            {
                float t = i / (float)_samplesPerSegment;
                float t1 = 1 - t;

                // Quadratic Bezier formula: B(t) = (1-t)²P? + 2(1-t)tP? + t²P?
                Vector2 point = t1 * t1 * start + 2 * t1 * t * control + t * t * end;
                _currentPath.Add (point);
            }

            _currentPoint = end;
        }

        public void CubicBezierTo (Vector2 control1, Vector2 control2, Vector2 end)
        {
            // Approximate cubic Bezier with line segments
            Vector2 start = _currentPoint;

            for (var i = 1; i <= _samplesPerSegment; i++)
            {
                float t = i / (float)_samplesPerSegment;
                float t1 = 1 - t;

                // Cubic Bezier formula: B(t) = (1-t)³P? + 3(1-t)²tP? + 3(1-t)t²P? + t³P?
                Vector2 point = t1 * t1 * t1 * start
                                + 3 * t1 * t1 * t * control1
                                + 3 * t1 * t * t * control2
                                + t * t * t * end;
                _currentPath.Add (point);
            }

            _currentPoint = end;
        }

        public void EndFigure () { ProcessPath (); }

        public void EndGlyph ()
        {
            // Glyph rendering complete
        }

        private void ProcessPath ()
        {
            if (_currentPath.Count < 2)
            {
                return;
            }

            // Convert the path to horizontal and vertical line segments
            for (var i = 0; i < _currentPath.Count - 1; i++)
            {
                Vector2 start = _currentPath [i];
                Vector2 end = _currentPath [i + 1];

                // Convert to terminal cells (rounding to nearest integer)
                var x1 = (int)Math.Round (start.X);
                var y1 = (int)Math.Round (start.Y);
                var x2 = (int)Math.Round (end.X);
                var y2 = (int)Math.Round (end.Y);

                // Determine if this is more horizontal or vertical
                int dx = Math.Abs (x2 - x1);
                int dy = Math.Abs (y2 - y1);

                if (dx > dy && dx > 0)
                {
                    // More horizontal - draw horizontal line
                    int length = Math.Abs (x2 - x1);
                    int startX = Math.Min (x1, x2);
                    _canvas.AddLine (new (startX, y1), length, Orientation.Horizontal, _style, _attribute);
                }
                else if (dy > 0)
                {
                    // More vertical - draw vertical line
                    int length = Math.Abs (y2 - y1);
                    int startY = Math.Min (y1, y2);
                    _canvas.AddLine (new (x1, startY), length, Orientation.Vertical, _style, _attribute);
                }
            }
        }

        public void SetColor (GlyphColor color)
        {
            // Not used for terminal rendering
        }

        public void EndText ()
        {
            // All text rendering complete
        }

        public void BeginText (in FontRectangle bounds)
        {
            // Starting text rendering
        }

        public TextDecorations EnabledDecorations ()
        {
            // We don't support text decorations like underline, strikethrough, etc.
            return TextDecorations.None;
        }

        public void SetDecoration (TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        {
            // Not needed for terminal rendering - we don't support decorations
        }
    }

    /// <summary>
    ///     Gets the width of a glyph at the specified height.
    /// </summary>
    private static int GetGlyphWidth (char c, int height)
    {
        // Normalize to lowercase for consistent sizing
        char normalized = char.ToLowerInvariant (c);

        return normalized switch
        {
            ' ' => height / 2,
            'i' or 'l' or '!' or '|' => Math.Max (2, height / 4),
            't' or 'f' or 'j' => Math.Max (3, height / 3),
            'm' or 'w' => Math.Max (5, height * 5 / 8),
            _ => Math.Max (4, height / 2)
        };
    }
}
