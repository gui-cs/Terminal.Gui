
namespace Terminal.Gui.Views;

/// <summary>Renders a continuous line with grid line ticks and labels</summary>
public abstract class Axis
{
    /// <summary>Default value for <see cref="ShowLabelsEvery"/></summary>
    private const uint DefaultShowLabelsEvery = 5;

    /// <summary>
    ///     Allows you to control what label text is rendered for a given <see cref="Increment"/> when
    ///     <see cref="ShowLabelsEvery"/> is above 0
    /// </summary>
    public LabelGetterDelegate LabelGetter;

    /// <summary>Populates base properties and sets the read only <see cref="Orientation"/></summary>
    /// <param name="orientation"></param>
    protected Axis (Orientation orientation)
    {
        Orientation = orientation;
        LabelGetter = DefaultLabelGetter;
    }

    /// <summary>Number of units of graph space between ticks on axis. 0 for no ticks</summary>
    /// <value></value>
    public float Increment { get; set; } = 1;

    /// <summary>The minimum axis point to show.  Defaults to null (no minimum)</summary>
    public float? Minimum { get; set; }

    /// <summary>Direction of the axis</summary>
    /// <value></value>
    public Orientation Orientation { get; }

    /// <summary>The number of <see cref="Increment"/> before an label is added. 0 = never show labels</summary>
    public uint ShowLabelsEvery { get; set; } = DefaultShowLabelsEvery;

    /// <summary>
    ///     Displayed below/to left of labels (see <see cref="Orientation"/>). If text is not visible, check
    ///     <see cref="GraphView.MarginBottom"/> / <see cref="GraphView.MarginLeft"/>
    /// </summary>
    public string Text { get; set; }

    /// <summary>True to render axis.  Defaults to true</summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    ///     Draws a custom label <paramref name="text"/> at <paramref name="screenPosition"/> units along the axis (X or Y
    ///     depending on <see cref="Orientation"/>)
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="screenPosition"></param>
    /// <param name="text"></param>
    public abstract void DrawAxisLabel (GraphView graph, int screenPosition, string text);

    /// <summary>Draws labels and axis <see cref="Increment"/> ticks</summary>
    /// <param name="graph"></param>
    public abstract void DrawAxisLabels (GraphView graph);

    /// <summary>Draws the solid line of the axis</summary>
    /// <param name="graph"></param>
    public abstract void DrawAxisLine (GraphView graph);

    /// <summary>Resets all configurable properties of the axis to default values</summary>
    public virtual void Reset ()
    {
        Increment = 1;
        ShowLabelsEvery = DefaultShowLabelsEvery;
        Visible = true;
        Text = "";
        LabelGetter = DefaultLabelGetter;
        Minimum = null;
    }

    /// <summary>Draws a single cell of the solid line of the axis</summary>
    /// <param name="graph"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    protected abstract void DrawAxisLine (GraphView graph, int x, int y);

    private string DefaultLabelGetter (AxisIncrementToRender toRender) { return toRender.Value.ToString ("N0"); }
}

/// <summary>The horizontal (x-axis) of a <see cref="GraphView"/></summary>
public class HorizontalAxis : Axis
{
    /// <summary>
    ///     Creates a new instance of axis with an <see cref="Orientation"/> of <see cref="Orientation.Horizontal"/>
    /// </summary>
    public HorizontalAxis () : base (Orientation.Horizontal) { }

    /// <summary>
    ///     Draws the given <paramref name="text"/> on the axis at x <paramref name="screenPosition"/>. For the screen y
    ///     position use <see cref="GetAxisYPosition(GraphView)"/>
    /// </summary>
    /// <param name="graph">Graph being drawn onto</param>
    /// <param name="screenPosition">Number of screen columns along the axis to take before rendering</param>
    /// <param name="text">Text to render under the axis tick</param>
    public override void DrawAxisLabel (GraphView graph, int screenPosition, string text)
    {
        IConsoleDriver driver = Application.Driver;
        int y = GetAxisYPosition (graph);

        graph.Move (screenPosition, y);

        // draw the tick on the axis
        Application.Driver?.AddRune (Glyphs.TopTee);

        // and the label text
        if (!string.IsNullOrWhiteSpace (text))
        {
            // center the label but don't draw it outside bounds of the graph
            int drawAtX = Math.Max (0, screenPosition - text.Length / 2);
            string toRender = text;

            // this is how much space is left
            int xSpaceAvailable = graph.Viewport.Width - drawAtX;

            // There is no space for the label at all!
            if (xSpaceAvailable <= 0)
            {
                return;
            }

            // if we are close to right side of graph, don't overspill
            if (toRender.Length > xSpaceAvailable)
            {
                toRender = toRender.Substring (0, xSpaceAvailable);
            }

            graph.Move (drawAtX, Math.Min (y + 1, graph.Viewport.Height - 1));
            driver.AddStr (toRender);
        }
    }

    /// <summary>Draws the horizontal x-axis labels and <see cref="Axis.Increment"/> ticks</summary>
    public override void DrawAxisLabels (GraphView graph)
    {
        if (!Visible || Increment == 0)
        {
            return;
        }

        Rectangle viewport = graph.Viewport;

        IEnumerable<AxisIncrementToRender> labels = GetLabels (graph, viewport);

        foreach (AxisIncrementToRender label in labels)
        {
            DrawAxisLabel (graph, label.ScreenLocation, label.Text);
        }

        // if there is a title
        if (!string.IsNullOrWhiteSpace (Text))
        {
            string toRender = Text;

            // if label is too long
            if (toRender.Length > graph.Viewport.Width)
            {
                toRender = toRender.Substring (0, graph.Viewport.Width);
            }

            graph.Move (graph.Viewport.Width / 2 - toRender.Length / 2, graph.Viewport.Height - 1);
            Application.Driver?.AddStr (toRender);
        }
    }

    /// <summary>Draws the horizontal axis line</summary>
    /// <param name="graph"></param>
    public override void DrawAxisLine (GraphView graph)
    {
        if (!Visible)
        {
            return;
        }

        Rectangle bounds = graph.Viewport;

        graph.Move (0, 0);

        int y = GetAxisYPosition (graph);

        // start the x-axis at left of screen (either 0 or margin)
        var xStart = (int)graph.MarginLeft;

        // but if the x-axis has a minimum (minimum is in graph space units)
        if (Minimum.HasValue)
        {
            // start at the screen location of the minimum
            int minimumScreenX = graph.GraphSpaceToScreen (new PointF (Minimum.Value, y)).X;

            // unless that is off the screen to the left
            xStart = Math.Max (xStart, minimumScreenX);
        }

        for (int i = xStart; i < bounds.Width; i++)
        {
            DrawAxisLine (graph, i, y);
        }
    }

    /// <summary>
    ///     Returns the Y screen position of the origin (typically 0,0) of graph space. Return value is bounded by the
    ///     screen i.e. the axis is always rendered even if the origin is offscreen.
    /// </summary>
    /// <param name="graph"></param>
    public int GetAxisYPosition (GraphView graph)
    {
        // find the origin of the graph in screen space (this allows for 'crosshair' style
        // graphs where positive and negative numbers visible
        Point origin = graph.GraphSpaceToScreen (new PointF (0, 0));

        // float the X axis so that it accurately represents the origin of the graph
        // but anchor it to top/bottom if the origin is offscreen
        return Math.Min (Math.Max (0, origin.Y), graph.Viewport.Height - ((int)graph.MarginBottom + 1));
    }

    /// <summary>Draws a horizontal axis line at the given <paramref name="x"/>, <paramref name="y"/> screen coordinates</summary>
    /// <param name="graph"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    protected override void DrawAxisLine (GraphView graph, int x, int y)
    {
        graph.Move (x, y);
        Application.Driver?.AddRune (Glyphs.HLine);
    }

    private IEnumerable<AxisIncrementToRender> GetLabels (GraphView graph, Rectangle viewport)
    {
        // if no labels
        if (Increment == 0)
        {
            yield break;
        }

        var labels = 0;
        int y = GetAxisYPosition (graph);

        RectangleF start = graph.ScreenToGraphSpace ((int)graph.MarginLeft, y);
        RectangleF end = graph.ScreenToGraphSpace (viewport.Width, y);

        // don't draw labels below the minimum
        if (Minimum.HasValue)
        {
            start.X = Math.Max (start.X, Minimum.Value);
        }

        RectangleF current = start;

        while (current.X < end.X)
        {
            int screenX = graph.GraphSpaceToScreen (new PointF (current.X, current.Y)).X;

            // The increment we will render (normally a top T unicode symbol)
            var toRender = new AxisIncrementToRender (Orientation, screenX, current.X);

            // Not every increment has to have a label
            if (ShowLabelsEvery != 0)
            {
                // if this increment also needs a label
                if (labels++ % ShowLabelsEvery == 0)
                {
                    toRender.Text = LabelGetter (toRender);
                }

                ;
            }

            // Label or no label definitely render it
            yield return toRender;

            current.X += Increment;
        }
    }
}

/// <summary>The vertical (i.e. Y axis) of a <see cref="GraphView"/></summary>
public class VerticalAxis : Axis
{
    /// <summary>Creates a new <see cref="Orientation.Vertical"/> axis</summary>
    public VerticalAxis () : base (Orientation.Vertical) { }

    /// <summary>
    ///     Draws the given <paramref name="text"/> on the axis at y <paramref name="screenPosition"/>. For the screen x
    ///     position use <see cref="GetAxisXPosition(GraphView)"/>
    /// </summary>
    /// <param name="graph">Graph being drawn onto</param>
    /// <param name="screenPosition">Number of rows from the top of the screen (i.e. down the axis) before rendering</param>
    /// <param name="text">
    ///     Text to render to the left of the axis tick.  Ensure to set <see cref="GraphView.MarginLeft"/> or
    ///     <see cref="GraphView.ScrollOffset"/> sufficient that it is visible
    /// </param>
    public override void DrawAxisLabel (GraphView graph, int screenPosition, string text)
    {
        int x = GetAxisXPosition (graph);
        int labelThickness = text.Length;

        graph.Move (x, screenPosition);

        // draw the tick on the axis
        Application.Driver?.AddRune (Glyphs.RightTee);

        // and the label text
        if (!string.IsNullOrWhiteSpace (text))
        {
            graph.Move (Math.Max (0, x - labelThickness), screenPosition);
            Application.Driver?.AddStr (text);
        }
    }

    /// <summary>Draws axis <see cref="Axis.Increment"/> markers and labels</summary>
    /// <param name="graph"></param>
    public override void DrawAxisLabels (GraphView graph)
    {
        if (!Visible || Increment == 0)
        {
            return;
        }

        Rectangle bounds = graph.Viewport;
        IEnumerable<AxisIncrementToRender> labels = GetLabels (graph, bounds);

        foreach (AxisIncrementToRender label in labels)
        {
            DrawAxisLabel (graph, label.ScreenLocation, label.Text);
        }

        // if there is a title
        if (!string.IsNullOrWhiteSpace (Text))
        {
            string toRender = Text;

            // if label is too long
            if (toRender.Length > graph.Viewport.Height)
            {
                toRender = toRender.Substring (0, graph.Viewport.Height);
            }

            // Draw it 1 letter at a time vertically down row 0 of the control
            int startDrawingAtY = graph.Viewport.Height / 2 - toRender.Length / 2;

            for (var i = 0; i < toRender.Length; i++)
            {
                graph.Move (0, startDrawingAtY + i);
                Application.Driver?.AddRune ((Rune)toRender [i]);
            }
        }
    }

    /// <summary>Draws the vertical axis line</summary>
    /// <param name="graph"></param>
    public override void DrawAxisLine (GraphView graph)
    {
        if (!Visible)
        {
            return;
        }

        Rectangle bounds = graph.Viewport;

        int x = GetAxisXPosition (graph);

        int yEnd = GetAxisYEnd (graph);

        // don't draw down further than the control bounds
        yEnd = Math.Min (yEnd, bounds.Height - (int)graph.MarginBottom);

        // Draw solid line
        for (var i = 0; i < yEnd; i++)
        {
            DrawAxisLine (graph, x, i);
        }
    }

    /// <summary>
    ///     Returns the X screen position of the origin (typically 0,0) of graph space. Return value is bounded by the
    ///     screen i.e. the axis is always rendered even if the origin is offscreen.
    /// </summary>
    /// <param name="graph"></param>
    public int GetAxisXPosition (GraphView graph)
    {
        // find the origin of the graph in screen space (this allows for 'crosshair' style
        // graphs where positive and negative numbers visible
        Point origin = graph.GraphSpaceToScreen (new PointF (0, 0));

        // float the Y axis so that it accurately represents the origin of the graph
        // but anchor it to left/right if the origin is offscreen
        return Math.Min (Math.Max ((int)graph.MarginLeft, origin.X), graph.Viewport.Width - 1);
    }

    /// <summary>Draws a vertical axis line at the given <paramref name="x"/>, <paramref name="y"/> screen coordinates</summary>
    /// <param name="graph"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    protected override void DrawAxisLine (GraphView graph, int x, int y)
    {
        graph.Move (x, y);
        Application.Driver?.AddRune (Glyphs.VLine);
    }

    private int GetAxisYEnd (GraphView graph)
    {
        // draw down the screen (0 is top of screen)
        // end at the bottom of the screen

        //unless there is a minimum
        if (Minimum.HasValue)
        {
            return graph.GraphSpaceToScreen (new PointF (0, Minimum.Value)).Y;
        }

        return graph.Viewport.Height;
    }

    private IEnumerable<AxisIncrementToRender> GetLabels (GraphView graph, Rectangle bounds)
    {
        // if no labels
        if (Increment == 0)
        {
            yield break;
        }

        var labels = 0;
        int x = GetAxisXPosition (graph);

        // remember screen space is top down so the lowest graph
        // space value is at the bottom of the screen
        RectangleF start = graph.ScreenToGraphSpace (x, bounds.Height - (1 + (int)graph.MarginBottom));
        RectangleF end = graph.ScreenToGraphSpace (x, 0);

        // don't draw labels below the minimum
        if (Minimum.HasValue)
        {
            start.Y = Math.Max (start.Y, Minimum.Value);
        }

        RectangleF current = start;

        while (current.Y < end.Y)
        {
            int screenY = graph.GraphSpaceToScreen (new PointF (current.X, current.Y)).Y;

            // Create the axis symbol
            var toRender = new AxisIncrementToRender (Orientation, screenY, current.Y);

            // and the label (if we are due one)
            if (ShowLabelsEvery != 0)
            {
                // if this increment also needs a label
                if (labels++ % ShowLabelsEvery == 0)
                {
                    toRender.Text = LabelGetter (toRender);
                }

                ;
            }

            // draw the axis symbol (and label if it has one)
            yield return toRender;

            current.Y += Increment;
        }
    }
}

/// <summary>A location on an axis of a <see cref="GraphView"/> that may or may not have a label associated with it</summary>
public class AxisIncrementToRender
{
    private string _text = "";

    /// <summary>Describe a new section of an axis that requires an axis increment symbol and/or label</summary>
    /// <param name="orientation"></param>
    /// <param name="screen"></param>
    /// <param name="value"></param>
    public AxisIncrementToRender (Orientation orientation, int screen, float value)
    {
        Orientation = orientation;
        ScreenLocation = screen;
        Value = value;
    }

    /// <summary>Direction of the parent axis</summary>
    public Orientation Orientation { get; }

    /// <summary>The screen location (X or Y depending on <see cref="Orientation"/>) that the increment will be rendered at</summary>
    public int ScreenLocation { get; }

    /// <summary>The value at this position on the axis in graph space</summary>
    public float Value { get; }

    /// <summary>The text (if any) that should be displayed at this axis increment</summary>
    /// <value></value>
    internal string Text
    {
        get => _text;
        set => _text = value ?? "";
    }
}

/// <summary>Delegate for custom formatting of axis labels.  Determines what should be displayed at a given label</summary>
/// <param name="toRender">The axis increment to which the label is attached</param>
/// <returns></returns>
public delegate string LabelGetterDelegate (AxisIncrementToRender toRender);
