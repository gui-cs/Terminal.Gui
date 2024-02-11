﻿using System.Collections.ObjectModel;

namespace Terminal.Gui;
#nullable enable
/// <summary>Describes a series of data that can be rendered into a <see cref="GraphView"/>></summary>
public interface ISeries
{
    /// <summary>
    ///     Draws the <paramref name="graphBounds"/> section of a series into the <paramref name="graph"/> view
    ///     <paramref name="drawBounds"/>
    /// </summary>
    /// <param name="graph">Graph series is to be drawn onto</param>
    /// <param name="drawBounds">Visible area of the graph in Console Screen units (excluding margins)</param>
    /// <param name="graphBounds">Visible area of the graph in Graph space units</param>
    void DrawSeries (GraphView graph, Rect drawBounds, RectangleF graphBounds);
}

/// <summary>Series composed of any number of discrete data points</summary>
public class ScatterSeries : ISeries
{
    /// <summary>
    ///     The color and character that will be rendered in the console when there are point(s) in the corresponding
    ///     graph space. Defaults to uncolored 'dot'
    /// </summary>
    public GraphCellToRender Fill { get; set; } = new (Glyphs.Dot);

    /// <summary>Collection of each discrete point in the series</summary>
    /// <returns></returns>
    public List<PointF> Points { get; set; } = new ();

    /// <summary>Draws all points directly onto the graph</summary>
    public void DrawSeries (GraphView graph, Rect drawBounds, RectangleF graphBounds)
    {
        if (Fill.Color.HasValue)
        {
            Application.Driver.SetAttribute (Fill.Color.Value);
        }

        foreach (PointF p in Points.Where (p => graphBounds.Contains (p)))
        {
            Point screenPoint = graph.GraphSpaceToScreen (p);
            graph.AddRune (screenPoint.X, screenPoint.Y, Fill.Rune);
        }
    }
}

/// <summary>Collection of <see cref="BarSeries"/> in which bars are clustered by category</summary>
public class MultiBarSeries : ISeries
{
    /// <summary>Creates a new series of clustered bars.</summary>
    /// <param name="numberOfBarsPerCategory">Each category has this many bars</param>
    /// <param name="barsEvery">How far appart to put each category (in graph space)</param>
    /// <param name="spacing">
    ///     How much spacing between bars in a category (should be less than <paramref name="barsEvery"/>/
    ///     <paramref name="numberOfBarsPerCategory"/>)
    /// </param>
    /// <param name="colors">
    ///     Array of colors that define bar color in each category.  Length must match
    ///     <paramref name="numberOfBarsPerCategory"/>
    /// </param>
    public MultiBarSeries (int numberOfBarsPerCategory, float barsEvery, float spacing, Attribute []? colors = null)
    {
        subSeries = new BarSeries [numberOfBarsPerCategory];

        if (colors != null && colors.Length != numberOfBarsPerCategory)
        {
            throw new ArgumentException (
                                         "Number of colors must match the number of bars",
                                         nameof (numberOfBarsPerCategory)
                                        );
        }

        for (var i = 0; i < numberOfBarsPerCategory; i++)
        {
            subSeries [i] = new BarSeries ();
            subSeries [i].BarEvery = barsEvery;
            subSeries [i].Offset = i * spacing;

            // Only draw labels for the first bar in each category
            subSeries [i].DrawLabels = i == 0;

            if (colors != null)
            {
                subSeries [i].OverrideBarColor = colors [i];
            }
        }

        Spacing = spacing;
    }

    private readonly BarSeries [] subSeries;

    /// <summary>
    ///     The number of units of graph space between bars.  Should be less than <see cref="BarSeries.BarEvery"/>
    /// </summary>
    public float Spacing { get; }

    /// <summary>
    ///     Sub collections.  Each series contains the bars for a different category.  Thus SubSeries[0].Bars[0] is the
    ///     first bar on the axis and SubSeries[1].Bars[0] is the second etc
    /// </summary>
    public IReadOnlyCollection<BarSeries> SubSeries => new ReadOnlyCollection<BarSeries> (subSeries);

    /// <summary>Draws all <see cref="SubSeries"/></summary>
    /// <param name="graph"></param>
    /// <param name="drawBounds"></param>
    /// <param name="graphBounds"></param>
    public void DrawSeries (GraphView graph, Rect drawBounds, RectangleF graphBounds)
    {
        foreach (BarSeries bar in subSeries)
        {
            bar.DrawSeries (graph, drawBounds, graphBounds);
        }
    }

    /// <summary>Adds a new cluster of bars</summary>
    /// <param name="label"></param>
    /// <param name="fill"></param>
    /// <param name="values">Values for each bar in category, must match the number of bars per category</param>
    public void AddBars (string label, Rune fill, params float [] values)
    {
        if (values.Length != subSeries.Length)
        {
            throw new ArgumentException (
                                         "Number of values must match the number of bars per category",
                                         nameof (values)
                                        );
        }

        for (var i = 0; i < values.Length; i++)
        {
            subSeries [i]
                .Bars.Add (
                           new BarSeriesBar (
                                             label,
                                             new GraphCellToRender (fill),
                                             values [i]
                                            )
                          );
        }
    }
}

/// <summary>Series of bars positioned at regular intervals</summary>
public class BarSeries : ISeries
{
    /// <summary>
    ///     Determines the spacing of bars along the axis. Defaults to 1 i.e. every 1 unit of graph space a bar is
    ///     rendered. Note that you should also consider <see cref="GraphView.CellSize"/> when changing this.
    /// </summary>
    public float BarEvery { get; set; } = 1;

    /// <summary>Ordered collection of graph bars to position along axis</summary>
    public List<BarSeriesBar> Bars { get; set; } = new ();

    /// <summary>True to draw <see cref="BarSeriesBar.Text"/> along the axis under the bar.  Defaults to true.</summary>
    public bool DrawLabels { get; set; } = true;

    /// <summary>
    ///     The number of units of graph space along the axis before rendering the first bar (and subsequent bars - see
    ///     <see cref="BarEvery"/>).  Defaults to 0
    /// </summary>
    public float Offset { get; set; }

    /// <summary>Direction bars protrude from the corresponding axis. Defaults to vertical</summary>
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Overrides the <see cref="BarSeriesBar.Fill"/> with a fixed color</summary>
    public Attribute? OverrideBarColor { get; set; }

    /// <summary>Draws bars that are currently in the <paramref name="drawBounds"/></summary>
    /// <param name="graph"></param>
    /// <param name="drawBounds">Screen area of the graph excluding margins</param>
    /// <param name="graphBounds">Graph space area that should be drawn into <paramref name="drawBounds"/></param>
    public virtual void DrawSeries (GraphView graph, Rect drawBounds, RectangleF graphBounds)
    {
        for (var i = 0; i < Bars.Count; i++)
        {
            float xStart = Orientation == Orientation.Horizontal ? 0 : Offset + (i + 1) * BarEvery;
            float yStart = Orientation == Orientation.Horizontal ? Offset + (i + 1) * BarEvery : 0;

            float endX = Orientation == Orientation.Horizontal ? Bars [i].Value : xStart;
            float endY = Orientation == Orientation.Horizontal ? yStart : Bars [i].Value;

            // translate to screen positions
            Point screenStart = graph.GraphSpaceToScreen (new PointF (xStart, yStart));
            Point screenEnd = graph.GraphSpaceToScreen (new PointF (endX, endY));

            // Start the bar from wherever the axis is
            if (Orientation == Orientation.Horizontal)
            {
                screenStart.X = graph.AxisY.GetAxisXPosition (graph);

                // dont draw bar off the right of the control
                screenEnd.X = Math.Min (graph.Bounds.Width - 1, screenEnd.X);

                // if bar is off the screen
                if (screenStart.Y < 0 || screenStart.Y > drawBounds.Height - graph.MarginBottom)
                {
                    continue;
                }
            }
            else
            {
                // Start the axis
                screenStart.Y = graph.AxisX.GetAxisYPosition (graph);

                // dont draw bar up above top of control
                screenEnd.Y = Math.Max (0, screenEnd.Y);

                // if bar is off the screen
                if (screenStart.X < graph.MarginLeft || screenStart.X > graph.MarginLeft + drawBounds.Width - 1)
                {
                    continue;
                }
            }

            // draw the bar unless it has no height
            if (Bars [i].Value != 0)
            {
                DrawBarLine (graph, screenStart, screenEnd, Bars [i]);
            }

            // If we are drawing labels and the bar has one
            if (DrawLabels && !string.IsNullOrWhiteSpace (Bars [i].Text))
            {
                // Add the label to the relevant axis
                if (Orientation == Orientation.Horizontal)
                {
                    graph.AxisY.DrawAxisLabel (graph, screenStart.Y, Bars [i].Text);
                }
                else if (Orientation == Orientation.Vertical)
                {
                    graph.AxisX.DrawAxisLabel (graph, screenStart.X, Bars [i].Text);
                }
            }
        }
    }

    /// <summary>Applies any color overriding</summary>
    /// <param name="graphCellToRender"></param>
    /// <returns></returns>
    protected virtual GraphCellToRender AdjustColor (GraphCellToRender graphCellToRender)
    {
        if (OverrideBarColor.HasValue)
        {
            graphCellToRender.Color = OverrideBarColor;
        }

        return graphCellToRender;
    }

    /// <summary>Override to do custom drawing of the bar e.g. to apply varying color or changing the fill symbol mid bar.</summary>
    /// <param name="graph"></param>
    /// <param name="start">Screen position of the start of the bar</param>
    /// <param name="end">Screen position of the end of the bar</param>
    /// <param name="beingDrawn">The Bar that occupies this space and is being drawn</param>
    protected virtual void DrawBarLine (GraphView graph, Point start, Point end, BarSeriesBar beingDrawn)
    {
        GraphCellToRender adjusted = AdjustColor (beingDrawn.Fill);

        if (adjusted.Color.HasValue)
        {
            Application.Driver.SetAttribute (adjusted.Color.Value);
        }

        graph.DrawLine (start, end, adjusted.Rune);

        graph.SetDriverColorToGraphColor ();
    }
}
