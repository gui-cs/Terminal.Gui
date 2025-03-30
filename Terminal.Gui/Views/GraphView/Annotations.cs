namespace Terminal.Gui;

/// <summary>
///     <para>Describes an overlay element that is rendered either before or after a series.</para>
///     <para>
///         Annotations can be positioned either in screen space (e.g. a legend) or in graph space (e.g. a line showing
///         high point)
///     </para>
///     <para>Unlike <see cref="ISeries"/>, annotations are allowed to draw into graph margins</para>
/// </summary>
public interface IAnnotation
{
    /// <summary>
    ///     True if annotation should be drawn before <see cref="ISeries"/>.  This allows Series and later annotations to
    ///     potentially draw over the top of this annotation.
    /// </summary>
    bool BeforeSeries { get; }

    /// <summary>
    ///     Called once after series have been rendered (or before if <see cref="BeforeSeries"/> is true). Use
    ///     <see cref="View.Driver"/> to draw and <see cref="View.Viewport"/> to avoid drawing outside of graph
    /// </summary>
    /// <param name="graph"></param>
    void Render (GraphView graph);
}

/// <summary>Displays text at a given position (in screen space or graph space)</summary>
public class TextAnnotation : IAnnotation
{
    /// <summary>
    ///     The location in graph space to draw the <see cref="Text"/>.  This annotation will only show if the point is in
    ///     the current viewable area of the graph presented in the <see cref="GraphView"/>
    /// </summary>
    public PointF GraphPosition { get; set; }

    /// <summary>
    ///     The location on screen to draw the <see cref="Text"/> regardless of scroll/zoom settings.  This overrides
    ///     <see cref="GraphPosition"/> if specified.
    /// </summary>
    public Point? ScreenPosition { get; set; }

    /// <summary>Text to display on the graph</summary>
    public string Text { get; set; }

    /// <summary>True to add text before plotting series.  Defaults to false</summary>
    public bool BeforeSeries { get; set; }

    /// <summary>Draws the annotation</summary>
    /// <param name="graph"></param>
    public void Render (GraphView graph)
    {
        if (ScreenPosition.HasValue)
        {
            DrawText (graph, ScreenPosition.Value.X, ScreenPosition.Value.Y);

            return;
        }

        Point screenPos = graph.GraphSpaceToScreen (GraphPosition);
        DrawText (graph, screenPos.X, screenPos.Y);
    }

    /// <summary>
    ///     Draws the <see cref="Text"/> at the given coordinates with truncation to avoid spilling over
    ///     <see name="View.Viewport"/> of the <paramref name="graph"/>
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="x">Screen x position to start drawing string</param>
    /// <param name="y">Screen y position to start drawing string</param>
    protected void DrawText (GraphView graph, int x, int y)
    {
        // the draw point is out of control bounds
        if (!graph.Viewport.Contains (new Point (x, y)))
        {
            return;
        }

        // There is no text to draw
        if (string.IsNullOrWhiteSpace (Text))
        {
            return;
        }

        graph.Move (x, y);

        int availableWidth = graph.Viewport.Width - x;

        if (availableWidth <= 0)
        {
            return;
        }

        if (Text.Length < availableWidth)
        {
            View.Driver.AddStr (Text);
        }
        else
        {
            View.Driver.AddStr (Text.Substring (0, availableWidth));
        }
    }
}

/// <summary>A box containing symbol definitions e.g. meanings for colors in a graph. The 'Key' to the graph</summary>
public class LegendAnnotation : View, IAnnotation
{
    /// <summary>Ordered collection of entries that are rendered in the legend.</summary>
    private readonly List<Tuple<GraphCellToRender, string>> _entries = new ();

    /// <summary>Creates a new empty legend at the empty screen coordinates.</summary>
    public LegendAnnotation () : this (Rectangle.Empty) { }

    /// <summary>Creates a new empty legend at the given screen coordinates.</summary>
    /// <param name="legendBounds">
    ///     Defines the area available for the legend to render in (within the graph).  This is in
    ///     screen units (i.e. not graph space)
    /// </param>
    public LegendAnnotation (Rectangle legendBounds)
    {
        X = legendBounds.X;
        Y = legendBounds.Y;
        Width = legendBounds.Width;
        Height = legendBounds.Height;
        BorderStyle = LineStyle.Single;
    }

    /// <summary>Returns false i.e. Legends render after series</summary>
    public bool BeforeSeries => false;

    // BUGBUG: Legend annotations are subviews. But for some reason the are rendered directly in OnDrawContent 
    // BUGBUG: instead of just being normal subviews. They get rendered as blank rects and thus we disable subview drawing.
    /// <inheritdoc />
    protected override bool OnDrawingText () { return true; }

    // BUGBUG: Legend annotations are subviews. But for some reason the are rendered directly in OnDrawContent 
    // BUGBUG: instead of just being normal subviews. They get rendered as blank rects and thus we disable subview drawing.
    /// <inheritdoc />
    protected override bool OnClearingViewport () { return true; }


    /// <summary>Draws the Legend and all entries into the area within <see cref="View.Viewport"/></summary>
    /// <param name="graph"></param>
    public void Render (GraphView graph)
    {
        if (!IsInitialized)
        {
            ColorScheme = new ColorScheme { Normal = Application.Driver?.GetAttribute () ?? Attribute.Default};
            graph.Add (this);
        }

        if (BorderStyle != LineStyle.None)
        {
            DrawAdornments ();
            RenderLineCanvas ();
        }

        var linesDrawn = 0;

        foreach (Tuple<GraphCellToRender, string> entry in _entries)
        {
            if (entry.Item1.Color.HasValue)
            {
                SetAttribute (entry.Item1.Color.Value);
            }
            else
            {
                graph.SetDriverColorToGraphColor ();
            }

            // add the symbol
            AddRune (0, linesDrawn, entry.Item1.Rune);

            // switch to normal coloring (for the text)
            graph.SetDriverColorToGraphColor ();

            // add the text
            Move (1, linesDrawn);

            string str = TextFormatter.ClipOrPad (entry.Item2, Viewport.Width - 1);
            Application.Driver?.AddStr (str);

            linesDrawn++;

            // Legend has run out of space
            if (linesDrawn >= Viewport.Height)
            {
                break;
            }
        }
    }

    /// <summary>Adds an entry into the legend.  Duplicate entries are permissible</summary>
    /// <param name="graphCellToRender">The symbol appearing on the graph that should appear in the legend</param>
    /// <param name="text">
    ///     Text to render on this line of the legend.  Will be truncated if outside of Legend
    ///     <see cref="View.Viewport"/>
    /// </param>
    public void AddEntry (GraphCellToRender graphCellToRender, string text) { _entries.Add (Tuple.Create (graphCellToRender, text)); }
}

/// <summary>Sequence of lines to connect points e.g. of a <see cref="ScatterSeries"/></summary>
public class PathAnnotation : IAnnotation
{
    /// <summary>Color for the line that connects points</summary>
    public Attribute? LineColor { get; set; }

    /// <summary>The symbol that gets drawn along the line, defaults to '.'</summary>
    public Rune LineRune { get; set; } = new ('.');

    /// <summary>Points that should be connected.  Lines will be drawn between points in the order they appear in the list</summary>
    public List<PointF> Points { get; set; } = new ();

    /// <summary>True to add line before plotting series.  Defaults to false</summary>
    public bool BeforeSeries { get; set; }

    /// <summary>Draws lines connecting each of the <see cref="Points"/></summary>
    /// <param name="graph"></param>
    public void Render (GraphView graph)
    {
        graph.SetAttribute (LineColor ?? graph.ColorScheme.Normal);

        foreach (LineF line in PointsToLines ())
        {
            Point start = graph.GraphSpaceToScreen (line.Start);
            Point end = graph.GraphSpaceToScreen (line.End);
            graph.DrawLine (start, end, LineRune);
        }
    }

    /// <summary>Generates lines joining <see cref="Points"/></summary>
    /// <returns></returns>
    private IEnumerable<LineF> PointsToLines ()
    {
        for (var i = 0; i < Points.Count - 1; i++)
        {
            yield return new LineF (Points [i], Points [i + 1]);
        }
    }
}
