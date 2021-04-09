using NStack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Control for rendering graphs (bar, scatter etc)
	/// </summary>
	public class GraphView : View {

		/// <summary>
		/// Horizontal axis
		/// </summary>
		/// <value></value>
		public Axis AxisX {get;} 

		/// <summary>
		/// Vertical axis
		/// </summary>
		/// <value></value>
		public Axis AxisY {get;} 

		/// <summary>
		/// Collection of data series that are rendered in the graph
		/// </summary>
		/// <returns></returns>
		public List<ISeries> Series {get;} = new List<ISeries>();

		/// <summary>
		/// Amount of space to leave on left of control.  Graph content (<see cref="Series"/>)
		/// will not be rendered in margins but axis labels may be
		/// </summary>
		public uint MarginLeft { get; set; }

		/// <summary>
		/// Amount of space to leave on bottom of control.  Graph content (<see cref="Series"/>)
		/// will not be rendered in margins but axis labels may be
		/// </summary>
		public uint MarginBottom { get; set; }

		/// <summary>
		/// The graph space position of the bottom left of the control.
		/// Changing this scrolls the viewport around in the graph
		/// </summary>
		/// <value></value>
		public PointD ScrollOffset {get;set;} = new PointD(0,0);

		/// <summary>
		/// Translates console width/height into graph space. Defaults
		/// to 1 row/col of console space being 1 unit of graph space. 
		/// </summary>
		/// <returns></returns>
		public PointD CellSize {get;set;} = new PointD(1,1);

		/// <summary>
		/// The color of the background of the graph and axis/labels
		/// </summary>
		public Attribute? GraphColor { get; set; }

		/// <summary>
		/// Creates a new graph with a 1 to 1 graph space with absolute layout
		/// </summary>
		public GraphView()
		{
			CanFocus = true;

			AxisX = new HorizontalAxis();
		 	AxisY = new VerticalAxis();
		}

		/// <summary>
		/// Clears all settings configured on the graph and resets all properties
		/// to default values (<see cref="CellSize"/>, <see cref="ScrollOffset"/> etc) 
		/// </summary>
		public void Reset()
		{
			ScrollOffset = new PointD(0,0);
			CellSize = new PointD(1,1);
			AxisX.Reset();
		 	AxisY.Reset();
			Series.Clear();
			GraphColor = null;
			SetNeedsDisplay();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (GraphColor ??ColorScheme.Normal);

			Move (0, 0);

			// clear all old content
			for (int i = 0; i < Bounds.Height; i++) {
				Move (0, i);
				Driver.AddStr (new string (' ', Bounds.Width));
			}

			// If there is no data do not display a graph
			if (!Series.Any ()) {
				return;
			}

			

			for (int x= (int)MarginLeft;x<Bounds.Width;x++){
				for(int y=0;y<Bounds.Height - (int)MarginBottom;y++){

					var space = ScreenToGraphSpace(x,y);

					foreach(var s in Series){
						var render = s.GetCellValueIfAny(space);
						
						if(render != null){
							Move(x,y);

							var old = Driver.GetAttribute ();
							if (render.Color.HasValue) {
								Driver.SetAttribute (render.Color.Value);
							}

							Driver.AddRune(render.Rune);

							if (render.Color.HasValue) {
								Driver.SetAttribute (old);
							}
						}
					}
				}
			}

			AxisY.DrawAxisLine (Driver, this, Bounds);
			AxisX.DrawAxisLine (Driver, this, Bounds);
			
			AxisY.DrawAxisLabels (Driver, this, Bounds);
			AxisX.DrawAxisLabels (Driver, this, Bounds);

			// Draw origin with plus
			var origin = GraphSpaceToScreen(new PointD(0,0));


			if(origin.X >= MarginLeft && origin.X<Bounds.Width){
				if(origin.Y >= 0 && origin.Y<=Bounds.Height - MarginBottom){

					if(AxisX.Visible && AxisY.Visible){
						Move(origin.X,origin.Y);
						AddRune(origin.X,origin.Y,'\u253C');
					}
				}
			}

		}

		/// <summary>
		/// Returns the section of the graph that is represented by the given
		/// screen position
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		public RectangleD ScreenToGraphSpace (int col, int row)
		{
			return new RectangleD (
				ScrollOffset.X + ((col - MarginLeft) * CellSize.X),
				ScrollOffset.Y + ((Bounds.Height - (row + MarginBottom+1)) * CellSize.Y),
				CellSize.X, CellSize.Y);
		}

		/// <summary>
		/// Calculates the screen location for a given point in graph space.
		/// Bear in mind these be off screen
		/// </summary>
		/// <param name="location">Point in graph space that may or may not be represented in the
		/// visible area of graph currently presented.  E.g. 0,0 for origin</param>
		/// <returns>Screen position (Column/Row) which would be used to render the graph <paramref name="location"/>.
		/// Note that this can be outside the current client area of the control</returns>
		public Point GraphSpaceToScreen (PointD location)
		{
			return new Point (

				(int)((location.X - ScrollOffset.X) / CellSize.X) + (int)MarginLeft,
				 // screen coordinates are top down while graph coordinates are bottom up
				 (Bounds.Height-1) - (int)MarginBottom - (int)((location.Y - ScrollOffset.Y) / CellSize.Y) 
				);
		}


		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			//&& Focused == tabsBar

			if (HasFocus && CanFocus ) {
				switch (keyEvent.Key) {

				case Key.CursorLeft:
					Scroll (-CellSize.X, 0);
					return true;
				case Key.CursorLeft | Key.CtrlMask:
					Scroll (-CellSize.X*5, 0);
					return true;
				case Key.CursorRight:
					Scroll (CellSize.X, 0);
					return true;
				case Key.CursorRight | Key.CtrlMask:
					Scroll (CellSize.X*5, 0);
					return true;
				case Key.CursorDown:
					Scroll (0, -CellSize.Y);
					return true;
				case Key.CursorDown | Key.CtrlMask:
					Scroll (0, -CellSize.Y*5);
					return true;
				case Key.CursorUp:
					Scroll (0,CellSize.Y);
					return true;
				case Key.CursorUp | Key.CtrlMask:
					Scroll (0,CellSize.Y*5);
					return true;
				}
			}

			return base.ProcessKey (keyEvent);
		}

		/// <summary>
		/// Scrolls the view by a given number of units in graph space.
		/// See <see cref="CellSize"/> to translate this into rows/cols
		/// </summary>
		/// <param name="offsetX"></param>
		/// <param name="offsetY"></param>
		private void Scroll (decimal offsetX, decimal offsetY)
		{
			ScrollOffset = new PointD (
				ScrollOffset.X + offsetX,
				ScrollOffset.Y + offsetY);

			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Describes a series of data that can be rendered into a <see cref="GraphView"/>>
	/// </summary>
	public interface ISeries 
	{
		/// <summary>
		/// Return the rune that should be drawn on the screen (if any)
		/// for the current position in the control
		/// </summary>
		/// <param name="graphSpace">Projection of the screen location into the chart graph space</param>
		GraphCellToRender GetCellValueIfAny (RectangleD graphSpace);
	}

	/// <summary>
	/// Describes how to render a single row/column of a <see cref="GraphView"/> based
	/// on the value(s) in <see cref="ISeries"/> at that location
	/// </summary>
	public class GraphCellToRender {

		/// <summary>
		/// The character to render in the console
		/// </summary>
		public Rune Rune { get; set; }

		/// <summary>
		/// Optional color to render the <see cref="Rune"/> with
		/// </summary>
		public Attribute? Color { get; set; }

		/// <summary>
		/// Creates instance and sets <see cref="Rune"/> with default graph coloring
		/// </summary>
		/// <param name="rune"></param>
		public GraphCellToRender (Rune rune)
		{
			Rune = rune;
		}
		/// <summary>
		/// Creates instance and sets <see cref="Rune"/> with custom graph coloring
		/// </summary>
		/// <param name="rune"></param>
		/// <param name="color"></param>
		public GraphCellToRender (Rune rune, Attribute color):this(rune)
		{
			Color = color;
		}
		/// <summary>
		/// Creates instance and sets <see cref="Rune"/> and <see cref="Color"/> (or default if null)
		/// </summary>
		public GraphCellToRender (Rune rune, Attribute? color) : this (rune)
		{
			Color = color;
		}
	}

	/// <summary>
	/// Series composed of any number of discrete data points 
	/// </summary>
	public class ScatterSeries : ISeries
	{
		/// <summary>
		/// Collection of each discrete point in the series
		/// </summary>
		/// <returns></returns>
		public List<PointD> Points {get;set;} = new List<PointD>();

		/// <summary>
		/// Returns a point symbol if the <paramref name="graphSpace"/> contains 
		/// any of the <see cref="Points"/>
		/// </summary>
		/// <param name="graphSpace"></param>
		/// <returns></returns>
		public GraphCellToRender GetCellValueIfAny (RectangleD graphSpace)
		{
			if(Points.Any(p=>graphSpace.Contains(p))){
				return new GraphCellToRender('x');
			}

			return null;
		}
	}


	/// <summary>
	/// Collection of <see cref="BarSeries"/> in which bars are clustered by category
	/// </summary>
	public class MultiBarSeries :ISeries{

		BarSeries [] subSeries;

		/// <summary>
		/// Sub collections.  Each series contains the bars for a different category.  Thus 
		/// SubSeries[0].Bars[0] is the first bar on the axis and SubSeries[1].Bars[0] is the
		/// second etc
		/// </summary>
		public IReadOnlyCollection<BarSeries> SubSeries { get => new ReadOnlyCollection<BarSeries> (subSeries); }

		/// <summary>
		/// The number of units of graph space between bars.  Should be 
		/// less than <see cref="BarSeries.BarEvery"/>
		/// </summary>
		public decimal Spacing { get; }

		/// <summary>
		/// Creates a new series of clustered bars.
		/// </summary>
		/// <param name="numberOfBarsPerCategory">Each category has this many bars</param>
		/// <param name="barsEvery">How far appart to put each category (in graph space)</param>
		/// <param name="spacing">How much spacing between bars in a category (should be less than <paramref name="barsEvery"/>/<paramref name="numberOfBarsPerCategory"/>)</param>
		/// <param name="colors">Array of colors that define bar colour in each category.  Length must match <paramref name="numberOfBarsPerCategory"/></param>
		public MultiBarSeries (int numberOfBarsPerCategory, decimal barsEvery, decimal spacing, Attribute[] colors = null)
		{
			subSeries = new BarSeries [numberOfBarsPerCategory];

			if(colors != null && colors.Length != numberOfBarsPerCategory){
				throw new ArgumentException("Number of colours must match the number of bars",nameof(numberOfBarsPerCategory));
			}


			for(int i = 0; i < numberOfBarsPerCategory; i++) {
				subSeries [i] = new BarSeries ();
				subSeries [i].BarEvery = barsEvery;
				subSeries [i].Offset = i * spacing;

				if(colors != null) {
					subSeries [i].OverrideBarColor = colors [i];
				}
			}
			Spacing = spacing;
		}

		/// <summary>
		/// Adds a new cluster of bars
		/// </summary>
		/// <param name="label"></param>
		/// <param name="fill"></param>
		/// <param name="values">Values for each bar in category, must match the number of bars per category</param>
		public void AddBars (string label,Rune fill, params decimal[] values)
		{
			if(values.Length != subSeries.Length) {
				throw new ArgumentException ("Number of values must match the number of bars per category", nameof (values));
			}
			
			for (int i = 0; i < values.Length; i++) {
				subSeries [i].Bars.Add(new BarSeries.Bar(null,
					new GraphCellToRender(fill),values[i]));
			}
		}

		/// <summary>
		/// Iterates over each <see cref="SubSeries"/> bar and returns the first
		/// that wants to render in the given graph space
		/// </summary>
		/// <param name="graphSpace"></param>
		/// <returns></returns>
		public GraphCellToRender GetCellValueIfAny (RectangleD graphSpace)
		{
			foreach(var bar in subSeries) {

				var cell = bar.GetCellValueIfAny (graphSpace);

				if(cell != null) {
					return cell;
				}
			}

			return null;
		}
	}

	/// <summary>
	/// Series of bars positioned at regular intervals
	/// </summary>
	public class BarSeries : ISeries {

		/// <summary>
		/// Ordered collection of graph bars to position along axis
		/// </summary>
		public List<Bar> Bars { get; set; } = new List<Bar> ();

		/// <summary>
		/// Determines the spacing of bars along the axis. Defaults to 1 i.e. 
		/// every 1 unit of graph space a bar is rendered.  Note that you should
		/// also consider <see cref="GraphView.CellSize"/> when changing this.
		/// </summary>
		public decimal BarEvery { get; set; } = 1;

		/// <summary>
		/// Direction bars protrude from the corresponding axis.
		/// Defaults to vertical
		/// </summary>
		public Orientation Orientation { get; set; } = Orientation.Vertical;

		/// <summary>
		/// The number of units of graph space along the axis before rendering the first bar
		/// (and subsequent bars - see <see cref="BarEvery"/>).  Defaults to 0
		/// </summary>
		public decimal Offset { get; internal set; } = 0;

		/// <summary>
		/// Overrides the <see cref="Bar.Fill"/> and <see cref="Bar.ColorGetter"/>
		/// with a fixed color
		/// </summary>
		public Attribute? OverrideBarColor { get; internal set; }

		/// <summary>
		/// Returns the <see cref="Bar.Fill"/> of the first bar that extends over
		/// the <paramref name="graphSpace"/> specified
		/// </summary>
		/// <param name="graphSpace"></param>
		/// <returns></returns>
		public GraphCellToRender GetCellValueIfAny (RectangleD graphSpace)
		{
			Bar bar = LocationToBar (graphSpace);

			//if no bar should be rendered at this position
			if(bar == null) {
				return null;
			}
			
			var toBeat = Orientation == Orientation.Vertical ? graphSpace.Top : graphSpace.Right;
			
			// for negative bars
			if(bar.Value < 0) {

				// fill above (up to 0)
				if (bar.Value <= toBeat && toBeat < 0) {
					return ApplyColor(bar.GetFinalFill(graphSpace));
				}
			}
			else {
				// and the bar is at least this high / wide
				if (bar.Value >= toBeat && toBeat > 0) {

					return ApplyColor(bar.GetFinalFill (graphSpace));
				}

			}
			return null;
		}

		/// <summary>
		/// Applies any color overriding
		/// </summary>
		/// <param name="graphCellToRender"></param>
		/// <returns></returns>
		protected virtual GraphCellToRender ApplyColor (GraphCellToRender graphCellToRender)
		{
			if(OverrideBarColor.HasValue) {
				graphCellToRender.Color = OverrideBarColor;
			}

			return graphCellToRender;
		}

		/// <summary>
		/// Translates a position in the graph to the Bar (if any) that
		/// should be rendered there (assuming the bar was long enough).
		/// This depends on the <see cref="Orientation"/>
		/// </summary>
		/// <param name="graphSpace"></param>
		/// <returns></returns>
		private Bar LocationToBar (RectangleD graphSpace)
		{
			// Position bars on x axis Bar1 at: 
			// Vertical Bars: x=1, Bar2 at x=2 etc
			// Horizontal Bars: y=1, Bar2 at y=2 etc
			for (int i = 0; i < Bars.Count; i++) {

				decimal barPosition = (i + 1) * BarEvery;
				barPosition += Offset;

				// the x/y position that the cell would have to be between for the bar to be rendered
				var low = Orientation == Orientation.Vertical ? graphSpace.X : graphSpace.Y;
				var high = Orientation == Orientation.Vertical ? graphSpace.Right : graphSpace.Bottom;

				// if a bar contained in this cell's X/Y axis of data space
				if (barPosition >=  low && barPosition < high) {
					return Bars [i];
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the name of the bar (if any) that is rendered at this
		/// point in the x axis
		/// </summary>
		/// <param name="axisPoint"></param>
		/// <returns></returns>
		public string GetLabelText (AxisIncrementToRender axisPoint)
		{
			return LocationToBar (axisPoint.GraphSpace)?.Text;
		}

		/// <summary>
		/// A single bar in a <see cref="BarSeries"/>
		/// </summary>
		public class Bar {

			/// <summary>
			/// Optional text that describes the bar.  This can be added as a label on the axis by setting
			/// <see cref="Axis.LabelGetter"/> = <see cref="BarSeries.GetLabelText(AxisIncrementToRender)"/>
			/// </summary>
			public string Text { get; set;}

			/// <summary>
			/// The color and character that will be rendered in the console
			/// when the bar extends over it
			/// </summary>
			public GraphCellToRender Fill { get; set;}

			/// <summary>
			/// Allows you to control the color of the bar at a given graph location.  This
			/// overrides the <see cref="Fill"/>
			/// </summary>
			public GraphAttributeGetterDelegate ColorGetter { get; set; }

			/// <summary>
			/// The value in graph space X/Y (depending on <see cref="Orientation"/>) to which the bar extends.
			/// </summary>
			public decimal Value { get; }

			/// <summary>
			/// Creates a new instance of a single bar rendered in the given <paramref name="fill"/> that extends
			/// out <paramref name="value"/> graph space units in the default <see cref="Orientation"/>
			/// </summary>
			/// <param name="text"></param>
			/// <param name="fill"></param>
			/// <param name="value"></param>
			public Bar (string text, GraphCellToRender fill, decimal value)
			{
				Text = text;
				Fill = fill;
				Value = value;
			}

			/// <summary>
			/// Returns <see cref="Fill"/> with optional overriding color (see <see cref="ColorGetter"/>)
			/// </summary>
			/// <returns></returns>
			internal GraphCellToRender GetFinalFill (RectangleD graphSpace)
			{
				var customColor = ColorGetter?.Invoke (graphSpace);

				if(customColor == null) {
					return Fill;
				}

				return new GraphCellToRender (Fill.Rune, customColor);
			}
		}
	}

	/// <summary>
	/// Series of points linked with a line
	/// </summary>
	public class LineSeries : ISeries, IComparer<PointD> {

		List<PointD> points = new List<PointD>();

		/// <summary>
		/// Ordered collection of points in the series
		/// </summary>
		public ReadOnlyCollection<PointD> Points { get => points.AsReadOnly (); }
		
		
		/// <summary>
		/// Color for the line that connects points
		/// </summary>
		public Attribute? LineColor { get; set; }

		/// <summary>
		/// Color for the points
		/// </summary>
		public Attribute? PointColor { get; set; }

		/// <summary>
		/// The symbol to show on screen at each point.  Deafults to 'x'
		/// </summary>
		public Rune PointSymbol { get; set; } = 'x';

		/// <summary>
		/// Adds <paramref name="toAdd"/> to <see cref="Points"/> in order of X
		/// </summary>
		/// <param name="toAdd"></param>
		public void Add(PointD toAdd)
		{
			points.Add (toAdd);
			points.Sort(this);
		}

		/// <summary>
		/// Adds <paramref name="toAdd"/> to <see cref="Points"/> in order of X
		/// </summary>
		/// <param name="toAdd"></param>
		public void AddRange (IEnumerable<PointD> toAdd)
		{
			points.AddRange (toAdd);
			points.Sort (this);
		}

		/// <summary>
		/// Compares points on the X axis 
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public int Compare (PointD a, PointD b)
		{
			if (a.X == b.X) {
				return 0;
			}

			if (a.X > b.X) {
				return 1;
			}
			
			return  -1;
		}

		/// <summary>
		/// Returns a symbol if the line between any two consecutive points crosses the graph space
		/// </summary>
		/// <param name="graphSpace"></param>
		/// <returns></returns>
		public GraphCellToRender GetCellValueIfAny (RectangleD graphSpace)
		{
			foreach (var line in PointsToLines (graphSpace)) {
				
				// we are on a plot point
				if (graphSpace.Contains(line.Start) || graphSpace.Contains (line.End)) {
					return new GraphCellToRender (PointSymbol, PointColor);
				}

				var clip = CohenSutherland.CohenSutherlandLineClip (graphSpace, line.Start, line.End);

				// the line passes through the current cell of the graph 
				if (clip != null) {
					return new GraphCellToRender ('.',LineColor);
				}
			}

			return null;
		}

		/// <summary>
		/// Returns all lines that could potentially pass through <paramref name="graphSpace"/>
		/// </summary>
		/// <param name="graphSpace"></param>
		/// <returns></returns>
		private IEnumerable<LineD> PointsToLines (RectangleD graphSpace)
		{						
			var first = points.LastOrDefault (p => p.X < graphSpace.X);
			var then = points.Where(p => p.X >= graphSpace.X && p.X < graphSpace.Right);
			var last = points.FirstOrDefault(p => p.X > graphSpace.X);

			var consider = new List<PointD> ();
			consider.Add (first);
			consider.AddRange (then);
			consider.Add (last);


			for (int i=0;i< consider.Count - 1; i++) {
				
				var line = new LineD (consider [i], consider [i + 1]);
				if (line.Start != null && line.End != null) {
					yield return line;
				}
					
			}
		}
	}
	/// <summary>
	/// Renders a continuous line with grid line ticks and labels
	/// </summary>
	public abstract class Axis
	{
		/// <summary>
		/// Default value for <see cref="ShowLabelsEvery"/>
		/// </summary>
		const uint DefaultShowLabelsEvery = 5;

		/// <summary>
		/// Direction of the axis
		/// </summary>
		/// <value></value>
		public Orientation Orientation {get;}
				
		/// <summary>
		/// Number of units of graph space between ticks on axis
		/// </summary>
		/// <value></value>
		public decimal Increment {get;set;} = 1;

		/// <summary>
		/// The number of <see cref="Increment"/> before an label is added.
		/// 0 = never show labels
		/// </summary>
		public uint ShowLabelsEvery { get; set; } = DefaultShowLabelsEvery;

		/// <summary>
		/// True to render axis.  Defaults to true
		/// </summary>
		public bool Visible { get; set; } = true;

		/// <summary>
		/// Allows you to control what label text is rendered for a given <see cref="Increment"/>
		/// when <see cref="ShowLabelsEvery"/> is above 0
		/// </summary>
		public LabelGetterDelegate LabelGetter;

		/// <summary>
		/// Displayed below/to left of labels (see <see cref="Orientation"/>).
		/// If text is not visible, check <see cref="GraphView.MarginBottom"/> / <see cref="GraphView.MarginLeft"/>
		/// </summary>
		public string Text;

		/// <summary>
		/// Populates base properties and sets the read only <see cref="Orientation"/>
		/// </summary>
		/// <param name="orientation"></param>
		protected Axis (Orientation orientation)
		{
			Orientation = orientation;
		}

		/// <summary>
		/// Draws the solid line of the axis
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="graph"></param>
		/// <param name="bounds"></param>
		public abstract void DrawAxisLine (ConsoleDriver driver, GraphView graph, Rect bounds);

		/// <summary>
		/// Draws labels and axis <see cref="Increment"/> ticks
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="graph"></param>
		/// <param name="bounds"></param>

		public abstract void DrawAxisLabels (ConsoleDriver driver, GraphView graph, Rect bounds);

		/// <summary>
		/// Resets all configurable properties of the axis to default values
		/// </summary>
		public virtual void Reset ()
		{
			Increment = 1;
			ShowLabelsEvery = DefaultShowLabelsEvery;
			Visible = true;
			Text = "";
		}
	}

	/// <summary>
	/// The horizontal (x axis) of a <see cref="GraphView"/>
	/// </summary>
	public class HorizontalAxis : Axis {

		/// <summary>
		/// Creates a new instance of axis with an <see cref="Orientation"/> of <see cref="Orientation.Horizontal"/>
		/// </summary>
		public HorizontalAxis ():base(Orientation.Horizontal)
		{
			LabelGetter = DefaultLabelGetter;
		}

		/// <inheritdoc/>
		public override void Reset ()
		{
			base.Reset ();

			LabelGetter = DefaultLabelGetter;
		}

		private string DefaultLabelGetter (AxisIncrementToRender toRender)
		{
			return toRender.GraphSpace.X.ToString ("N0");
		}

		/// <summary>
		/// Draws the horizontal axis line
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="graph"></param>
		/// <param name="bounds"></param>
		public override void DrawAxisLine (ConsoleDriver driver, GraphView graph, Rect bounds)
		{
			if (!Visible) {
				return;
			}

			graph.Move (0, 0);

			var y = GetAxisYPosition (graph, bounds);

			for (int i = 0; i < bounds.Width; i++) {

				graph.Move (i, y);
				driver.AddRune (driver.HLine);
			}
		}

		/// <summary>
		/// Draws the horizontal x axis labels and <see cref="Axis.Increment"/> ticks
		/// </summary>
		public override void DrawAxisLabels (ConsoleDriver driver,GraphView graph,Rect bounds)
		{
			if (!Visible) {
				return;
			}

			var labels = GetLabels (graph, bounds);

			foreach (var label in labels) {

				graph.Move (label.ScreenLocation.X, label.ScreenLocation.Y);

				// draw the tick on the axis
				driver.AddRune (driver.TopTee);

				// and the label text
				if (!string.IsNullOrWhiteSpace (label.Text)) {

					// center the label but don't draw it outside bounds of the graph
					int drawAtX = Math.Max (0, label.ScreenLocation.X - (label.Text.Length / 2));
					string toRender = label.Text;

					// this is how much space is left
					int xSpaceAvailable = graph.Bounds.Width - drawAtX;

					// There is no space for the label at all!
					if (xSpaceAvailable <= 0) {
						continue;
					}

					// if we are close to right side of graph, don't overspill
					if (toRender.Length > xSpaceAvailable) {
						toRender = toRender.Substring (0, xSpaceAvailable);
					}

					graph.Move (drawAtX,label.ScreenLocation.Y + 1);
					driver.AddStr (toRender);
				}
			}

			// if there is a title
			if (!string.IsNullOrWhiteSpace (Text)) {
				
				string toRender = Text;

				// if label is too long
				if (toRender.Length > graph.Bounds.Width) {
					toRender = toRender.Substring (0, graph.Bounds.Width);
				}

				graph.Move (graph.Bounds.Width/2 - (toRender.Length / 2), graph.Bounds.Height -1);
				driver.AddStr (toRender);
			}
		}

		private IEnumerable<AxisIncrementToRender> GetLabels (GraphView graph, Rect bounds)
		{
			// if no labels
			if(Increment == 0) {
				yield break;
			}

			int labels = 0;
			int y = GetAxisYPosition (graph, bounds);
			
			for (int i = 0; i < bounds.Width; i++) {

				// what bit of the graph is supposed to go here?
				var graphSpace = graph.ScreenToGraphSpace (i,y);

				if(Math.Abs(graphSpace.X) % Increment < graph.CellSize.X)
				{
					var toRender =  new AxisIncrementToRender (Orientation, new Point (i, y), graphSpace);

					if (ShowLabelsEvery != 0) {

						// if this increment also needs a label
						if (labels++ % ShowLabelsEvery == 0) {
							toRender.Text = LabelGetter (toRender);
						};
					}

					yield return toRender;
				}
			}
		}
		/// <summary>
		/// Returns the Y screen position of the origin (typically 0,0) of graph space.
		/// Return value is bounded by the screen i.e. the axis is always rendered even
		/// if the origin is offscreen.
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="bounds"></param>
		private int GetAxisYPosition(GraphView graph, Rect bounds)
		{
			// find the origin of the graph in screen space (this allows for 'crosshair' style
			// graphs where positive and negative numbers visible
			var origin = graph.GraphSpaceToScreen (new PointD (0, 0));

			// decimal the X axis so that it accurately represents the origin of the graph
			// but anchor it to top/bottom if the origin is offscreen
			return Math.Min (Math.Max (0, origin.Y), bounds.Height - ((int)graph.MarginBottom+1));
		}
	}

	/// <summary>
	/// The vertical (i.e. Y axis) of a <see cref="GraphView"/>
	/// </summary>
	public class VerticalAxis : Axis {

		private int GetLabelThickness (IEnumerable<AxisIncrementToRender> labels)
		{
			var l = labels.ToArray();
			if(l.Length == 0){
				return 1;
			}

			return  l.Max(s=>s.Text.Length);
		}

		/// <summary>
		/// Creates a new <see cref="Orientation.Vertical"/> axis
		/// </summary>
		public VerticalAxis () :base(Orientation.Vertical)
		{
			LabelGetter = DefaultLabelGetter;
		}

		/// <inheritdoc/>
		public override void Reset ()
		{
			base.Reset ();

			LabelGetter = DefaultLabelGetter;
		}
		private string DefaultLabelGetter (AxisIncrementToRender toRender)
		{
			return toRender.GraphSpace.Y.ToString ("N0");
		}

		/// <summary>
		/// Draws the vertical axis line
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="graph"></param>
		/// <param name="bounds"></param>
		public override void DrawAxisLine (ConsoleDriver driver, GraphView graph, Rect bounds)
		{
			if (!Visible) {
				return;
			}

			var x = GetAxisXPosition (graph, bounds);

			// Draw solid line
			for (int i = 0; i < bounds.Height; i++) {
				graph.Move (x, i);
				driver.AddRune (driver.VLine);
			}
		}


		/// <summary>
		/// Draws axis <see cref="Axis.Increment"/> markers and labels
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="graph"></param>
		/// <param name="bounds"></param>
		public override void DrawAxisLabels (ConsoleDriver driver, GraphView graph, Rect bounds)
		{
			if (!Visible) {
				return;
			}

			var x = GetAxisXPosition (graph, bounds);
			var labels = GetLabels(graph,bounds);
			var labelThickness = GetLabelThickness (labels);
				
			foreach(var label in labels) {

				graph.Move (label.ScreenLocation.X, label.ScreenLocation.Y);

				// draw the tick on the axis
				driver.AddRune (driver.RightTee);

				// and the label text
				if (!string.IsNullOrWhiteSpace (label.Text)) {
					graph.Move (Math.Max(0,x - labelThickness), label.ScreenLocation.Y);
					driver.AddStr (label.Text);
				}
			}

			// if there is a title
			if (!string.IsNullOrWhiteSpace (Text)) {

				string toRender = Text;

				// if label is too long
				if (toRender.Length > graph.Bounds.Height) {
					toRender = toRender.Substring (0, graph.Bounds.Height);
				}

				// Draw it 1 letter at a time vertically down row 0 of the control
				int startDrawingAtY = graph.Bounds.Height / 2 - (toRender.Length / 2);

				for(int i= 0; i< toRender.Length; i++) {

					graph.Move (0, startDrawingAtY+i);
					driver.AddRune(toRender[i]);
				}

			}
		}

		private IEnumerable<AxisIncrementToRender> GetLabels (GraphView graph,Rect bounds)
		{
			// if no labels
			if (Increment == 0) {
				yield break;
			}

			int labels = 0;

			int x = GetAxisXPosition (graph, bounds);

			for (int i = 0; i < bounds.Height; i++) {

				// what bit of the graph is supposed to go here?
				var graphSpace = graph.ScreenToGraphSpace (x,i);

				if(Math.Abs(graphSpace.Y) % Increment < graph.CellSize.Y)
				{
					var toRender =  new AxisIncrementToRender (Orientation, new Point (x,i), graphSpace);

					// and the label (if we are due one)
					if (ShowLabelsEvery != 0) {

						// if this increment also needs a label
						if (labels++ % ShowLabelsEvery == 0) {
							toRender.Text = LabelGetter(toRender);
						};

						yield return toRender;
					}

				}
				
			}
		}

		/// <summary>
		/// Returns the X screen position of the origin (typically 0,0) of graph space.
		/// Return value is bounded by the screen i.e. the axis is always rendered even
		/// if the origin is offscreen.
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="bounds"></param>
		private int GetAxisXPosition (GraphView graph, Rect bounds)
		{
			// find the origin of the graph in screen space (this allows for 'crosshair' style
			// graphs where positive and negative numbers visible
			var origin = graph.GraphSpaceToScreen (new PointD (0, 0));

			// decimal the Y axis so that it accurately represents the origin of the graph
			// but anchor it to left/right if the origin is offscreen
			return Math.Min (Math.Max ((int)graph.MarginLeft, origin.X), bounds.Width);
		}
	}


	/// <summary>
	/// A location on an axis of a <see cref="GraphView"/> that may
	/// or may not have a label associated with it
	/// </summary>
	public class AxisIncrementToRender {

		/// <summary>
		/// Direction of the parent axis
		/// </summary>
		public Orientation Orientation { get; }

		/// <summary>
		/// Location in the <see cref="Axis"/> that the axis increment appears
		/// </summary>
		public Point ScreenLocation { get;  }

		/// <summary>
		/// The volume of graph that is represented by this screen coordinate
		/// </summary>
		public RectangleD GraphSpace { get; }

		private string _text = "";

		/// <summary>
		/// The text (if any) that should be displayed at this axis increment
		/// </summary>
		/// <value></value>
		public string Text {
			get => _text;
			internal set { _text = value ?? ""; }
		}

		/// <summary>
		/// Describe a new section of an axis that requires an axis increment
		/// symbol and/or label
		/// </summary>
		/// <param name="orientation"></param>
		/// <param name="screen"></param>
		/// <param name="graphSpace"></param>
		public AxisIncrementToRender (Orientation orientation,Point screen, RectangleD graphSpace)
		{
			Orientation = orientation;
			ScreenLocation = screen;
			GraphSpace = graphSpace;
		}
	}

	/// <summary>
	/// Rectangle class based on the exact floating point primative 'decimal'
	/// </summary>
	public class RectangleD {

		/// <summary>
		/// A rectangle with 0 size positioned at the origin (0,0)
		/// </summary>
		public static readonly RectangleD Empty = new RectangleD (0,0,0,0);

		/// <summary>
		/// X coordinate of the Upper Left of this rectangle
		/// </summary>
		public decimal X { get; private set; }
		/// <summary>
		/// Y coordinate of the Upper Left of this rectangle
		/// </summary>
		public decimal Y { get; private set; }
		/// <summary>
		/// Width of the rectangle
		/// </summary>
		public decimal Width { get; private set; }

		/// <summary>
		/// Height of the rectangle
		/// </summary>
		public decimal Height { get; private set; }

		/// <summary>
		/// Creates a new rectangle of the given size and position
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public RectangleD (decimal x, decimal y, decimal width, decimal height)
		{
			this.X = x;
			this.Y = y;
			this.Width= width;
			this.Height = height;
		}

		/// <summary>
		/// X Coordinate of the left edge of rectangle
		/// </summary>
		public decimal Left {
			get {
				return X;
			}
		}

		/// <summary>
		/// Y Coordinate of the top edge of rectangle
		/// </summary>
		public decimal Top {
			get {
				return Y;
			}
		}

		/// <summary>
		/// X Coordinate of the right edge of rectangle
		/// </summary>
		public decimal Right {
			get {
				return X + Width;
			}
		}

		/// <summary>
		/// Y Coordinate of the bottom edge of rectangle.  Note <see cref="Bottom"/> is 
		/// larger than <see cref="Top"/> because rectangles are measured from upper left
		/// </summary>
		public decimal Bottom {
			get {
				return Y + Height;
			}
		}
		
		/// <summary>
		/// Returns true if the Width or Height are 0 or negative
		/// </summary>
		public bool IsEmpty {
			get {
				return (Width <= 0) || (Height <= 0);
			}
		}

		/// <summary>
		/// True if both rectangles cover exactly the same space
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals (object obj)
		{
			if (!(obj is RectangleD))
				return false;
			RectangleD comp = (RectangleD)obj;
			return (comp.X == this.X) &&
			       (comp.Y == this.Y) &&
			       (comp.Width == this.Width) &&
			       (comp.Height == this.Height);
		}
		/// <summary>
		/// True if both rectangles cover exactly the same space
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator == (RectangleD left, RectangleD right)
		{
			return (left.X == right.X
				 && left.Y == right.Y
				 && left.Width == right.Width
				 && left.Height == right.Height);
		}

		/// <summary>
		/// True if any dimension or corner of rectangles differ
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator != (RectangleD left, RectangleD right)
		{
			return !(left == right);
		}

		/// <summary>
		/// True if the given point is inside the rectangle.  Note that this is
		/// inclusive of <see cref="X"/> and <see cref="Y"/> but exclusive of
		/// <see cref="Right"/> and <see cref="Bottom"/>
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool Contains (decimal x, decimal y)
		{
			return this.X <= x &&
			x < this.X + this.Width &&
			this.Y <= y &&
			y < this.Y + this.Height;
		}

		/// <summary>
		/// True if the given point is inside the rectangle.  Note that this is
		/// inclusive of <see cref="X"/> and <see cref="Y"/> but exclusive of
		/// <see cref="Right"/> and <see cref="Bottom"/>
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		public bool Contains (PointD pt)
		{
			return Contains (pt.X, pt.Y);
		}
		
		/// <summary>
		/// Generates a hashcode from the X,Y Width and Height elements
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode ()
		{
			unchecked{
				return (int)((int)X * Y * Width * Height);
			}

		}
		/// <summary>
		/// Increases the size of the rectangle by the provided factors
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Inflate (decimal x, decimal y)
		{
			this.X -= x;
			this.Y -= y;
			this.Width += 2 * x;
			this.Height += 2 * y;
		}

		/// <summary>
		/// Modifies the Rectangle to include only the section that overlaps
		/// with <paramref name="rect"/>
		/// </summary>
		/// <param name="rect"></param>
		public void Intersect (RectangleD rect)
		{
			RectangleD result = RectangleD.Intersect (rect, this);
			this.X = result.X;
			this.Y = result.Y;
			this.Width = result.Width;
			this.Height = result.Height;
		}
		/// <summary>
		/// Returns the overlapping section of <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static RectangleD Intersect (RectangleD a, RectangleD b)
		{
			decimal x1 = Math.Max (a.X, b.X);
			decimal x2 = Math.Min (a.X + a.Width, b.X + b.Width);
			decimal y1 = Math.Max (a.Y, b.Y);
			decimal y2 = Math.Min (a.Y + a.Height, b.Y + b.Height);
			if (x2 >= x1
			    && y2 >= y1) {
				return new RectangleD (x1, y1, x2 - x1, y2 - y1);
			}
			return RectangleD.Empty;
		}
		/// <summary>
		/// Returns true if the provided <paramref name="rect"/> overlaps with
		/// this rectangle
		/// </summary>
		/// <param name="rect"></param>
		/// <returns></returns>
		public bool IntersectsWith (RectangleD rect)
		{
			return (rect.X < this.X + this.Width) &&
			       (this.X < (rect.X + rect.Width)) &&
			       (rect.Y < this.Y + this.Height) &&
			       (this.Y < rect.Y + rect.Height);
		}

		/// <summary>
		/// Returns user friendly description of current rectangle size/location
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return "{X=" + X.ToString (CultureInfo.CurrentCulture) + ",Y=" + Y.ToString (CultureInfo.CurrentCulture) +
			",Width=" + Width.ToString (CultureInfo.CurrentCulture) +
			",Height=" + Height.ToString (CultureInfo.CurrentCulture) + "}";
		}
	}

	/// <summary>
	/// Describes a point in X and Y space based on the exact floating
	/// point primative 'decimal'
	/// </summary>
	public class PointD {

		/// <summary>
		/// X coordinate of the point
		/// </summary>
		public decimal X;

		/// <summary>
		/// Y coordinate of the point
		/// </summary>
		public decimal Y;

		/// <summary>
		/// Creates a new point at the given coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public PointD (decimal x, decimal y)
		{
			this.X = x;
			this.Y = y;
		}
	}
	/// <summary>
	/// Describes two points in graph space and a line between them
	/// </summary>
	public class LineD {
		/// <summary>
		/// The start of the line
		/// </summary>
		public PointD Start { get; }

		/// <summary>
		/// The end point of the line
		/// </summary>
		public PointD End { get; }

		/// <summary>
		/// Creates a new point at the given coordinates
		/// </summary>
		public LineD(PointD start, PointD end)
		{
			this.Start = start;
			this.End = end;
		}

		/// <summary>
		/// Returns the gradient of the line.  If vertical then <see cref="decimal.MaxValue"/> is returned
		/// </summary>
		/// <returns></returns>
		public decimal GetGradient ()
		{
			decimal changeInHeight = End.Y - Start.Y;
			decimal changeInWidth = End.X - Start.X;

			if(changeInWidth == 0) {
				return decimal.MaxValue;
			}

			return changeInHeight / changeInWidth;
		}
	}

	/// <summary>
	/// Determines what should be displayed at a given label
	/// </summary>
	/// <param name="toRender">The axis increment to which the label is attached</param>
	/// <returns></returns>
	public delegate string LabelGetterDelegate (AxisIncrementToRender toRender);

	/// <summary>
	/// Determines what color (if non default) should be used to render a graph element at the given location
	/// </summary>
	/// <param name="graphLocation">The section of graph space which this screen cell represents</param>
	/// <returns></returns>
	public delegate Attribute? GraphAttributeGetterDelegate (RectangleD graphLocation);

	/// <summary>
	/// Direction of an element (horizontal or vertical)
	/// </summary>
	public enum Orientation
	{
		
		/// <summary>
		/// Left to right 
		/// </summary>
		Horizontal,

		/// <summary>
		/// Bottom to top
		/// </summary>
		Vertical
	}

	/// <summary>
	/// This is the Cohen–Sutherland algorithm for checking if a line intersects a rectangle
	/// https://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm
	/// </summary>
	public static class CohenSutherland {

		/// <summary>
		/// Bitfields used to partition the space into 9 regiond
		/// </summary>
		private const byte INSIDE = 0; // 0000
		private const byte LEFT = 1;   // 0001
		private const byte RIGHT = 2;  // 0010
		private const byte ABOVE = 4; // 0100
		private const byte BELOW = 8;    // 1000

		/// <summary>
		/// Compute the bit code for a point (x, y) using the clip rectangle
		/// bounded diagonally by <paramref name="rect"/>
		/// </summary>
		/// <returns></returns>
		private static byte ComputeOutCode (RectangleD rect, decimal x, decimal y)
		{
			// initialised as being inside of clip window
			byte code = INSIDE;

			if (x < rect.Left)           // to the left of clip window
				code |= LEFT;
			else if (x > rect.Right)     // to the right of clip window
				code |= RIGHT;
			if (y < rect.Top)         // below the clip window
				code |= ABOVE;
			else if (y > rect.Bottom)       // above the clip window
				code |= BELOW;

			return code;
		}

		/// <summary>
		/// Cohen–Sutherland clipping algorithm clips a line from
		/// P0 = (x0, y0) to P1 = (x1, y1) against a rectangle with
		/// diagonal from <paramref name="rect"/>
		/// </summary>
		/// <returns>a list of two points in the resulting clipped line, or zero</returns>
		public static LineD CohenSutherlandLineClip (RectangleD rect,
				       PointD p0, PointD p1)
		{
			decimal x0 = p0.X;
			decimal y0 = p0.Y;
			decimal x1 = p1.X;
			decimal y1 = p1.Y;

			// compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
			byte outcode0 = CohenSutherland.ComputeOutCode (rect, x0, y0);
			byte outcode1 = CohenSutherland.ComputeOutCode (rect, x1, y1);
			bool accept = false;

			while (true) {
				// Bitwise OR is 0. Trivially accept and get out of loop
				if ((outcode0 | outcode1) == 0) {
					accept = true;
					break;
				}
				// Bitwise AND is not 0. Trivially reject and get out of loop
				else if ((outcode0 & outcode1) != 0) {
					break;
				} else {
					// failed both tests, so calculate the line segment to clip
					// from an outside point to an intersection with clip edge
					decimal x, y;

					// At least one endpoint is outside the clip rectangle; pick it.
					byte outcodeOut = (outcode0 != 0) ? outcode0 : outcode1;

					// Now find the intersection point;
					// use formulas y = y0 + slope * (x - x0), x = x0 + (1 / slope) * (y - y0)
					if ((outcodeOut & BELOW) != 0) {   // point is above the clip rectangle
						x = x0 + (x1 - x0) * (rect.Bottom - y0) / (y1 - y0);
						y = rect.Bottom;
					} else if ((outcodeOut & ABOVE) != 0) { // point is below the clip rectangle
						x = x0 + (x1 - x0) * (rect.Top - y0) / (y1 - y0);
						y = rect.Top;
					} else if ((outcodeOut & RIGHT) != 0) {  // point is to the right of clip rectangle
						y = y0 + (y1 - y0) * (rect.Right - x0) / (x1 - x0);
						x = rect.Right;
					} else if ((outcodeOut & LEFT) != 0) {   // point is to the left of clip rectangle
						y = y0 + (y1 - y0) * (rect.Left - x0) / (x1 - x0);
						x = rect.Left;
					} else {
						return null;
					}

					// Now we move outside point to intersection point to clip
					// and get ready for next pass.
					if (outcodeOut == outcode0) {
						x0 = x;
						y0 = y;
						outcode0 = CohenSutherland.ComputeOutCode (rect, x0, y0);
					} else {
						x1 = x;
						y1 = y;
						outcode1 = CohenSutherland.ComputeOutCode (rect, x1, y1);
					}
				}
			}

			// return the clipped line
			return (accept) ?
			    new LineD (new PointD(x0,y0),new PointD(x1, y1)) : null;

		}
	}
}