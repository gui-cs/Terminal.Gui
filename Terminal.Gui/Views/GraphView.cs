using NStack;
using System;
using System.Collections.Generic;
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
		/// Creates a new graph with a 1 to 1 graph space with absolute layout
		/// </summary>
		public GraphView()
		{
			CanFocus = true;

			AxisX = new HorizontalAxis();
		 	AxisY = new VerticalAxis();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (ColorScheme.Normal);

			Move (0, 0);

			// clear all old content
			for (int i = 0; i < Bounds.Height; i++) {
				Move (0, i);
				Driver.AddStr (new string (' ', Bounds.Width));
			}

			base.Redraw(bounds);

			// If there is no data do not display a graph
			if (!Series.Any ()) {
				return;
			}
				

			AxisX.DrawAxisLine (Driver, this, Bounds);
			AxisY.DrawAxisLine (Driver, this, Bounds);

			AxisX.DrawAxisLabels (Driver, this, Bounds);
			AxisY.DrawAxisLabels (Driver, this, Bounds);

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
				case Key.CursorRight:
					Scroll (CellSize.X, 0);
					return true;
				case Key.CursorDown:
					Scroll (0, -CellSize.Y);
					return true;
				case Key.CursorUp:
					Scroll (0,CellSize.Y);
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
	/// Series of bars positioned at regular intervals
	/// </summary>
	public class BarSeries : ISeries {

		/// <summary>
		/// Ordered collection of graph bars to position along axis
		/// </summary>
		public List<Bar> Bars {get;set;}

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
					return bar.Fill;
				}
			}
			else {
				// and the bar is at least this high / wide
				if (bar.Value >= toBeat && toBeat > 0) {

					return bar.Fill;
				}

			}
			return null;
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
			public string Text { get; }

			/// <summary>
			/// The color and character that will be rendered in the console
			/// when the bar extends over it
			/// </summary>
			public GraphCellToRender Fill { get; }

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
		}
	}

	/// <summary>
	/// Renders a continuous line with grid line ticks and labels
	/// </summary>
	public abstract class Axis
	{
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
		public uint ShowLabelsEvery { get; set; } = 5;
				
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
			graph.Move (0, 0);
			driver.SetAttribute (graph.ColorScheme.Normal);

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
			driver.SetAttribute (graph.ColorScheme.Normal);

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
			driver.SetAttribute (graph.ColorScheme.Normal);

			var x = GetAxisXPosition (graph, bounds);
			var labels = GetLabels(graph,bounds);
			var labelThickness = GetLabelThickness (labels);
				
			foreach(var label in labels) {

				graph.Move (label.ScreenLocation.X, label.ScreenLocation.Y);

				// draw the tick on the axis
				driver.AddRune (driver.RightTee);

				// and the label text
				if (!string.IsNullOrWhiteSpace (label.Text)) {
					graph.Move (x - labelThickness, label.ScreenLocation.Y);
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
			return (int)((UInt32)X ^
			(((UInt32)Y << 13) | ((UInt32)Y >> 19)) ^
			(((UInt32)Width << 26) | ((UInt32)Width >> 6)) ^
			(((UInt32)Height << 7) | ((UInt32)Height >> 25)));
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
	/// Determines what should be displayed at a given label
	/// </summary>
	/// <param name="toRender">The axis increment to which the label is attached</param>
	/// <returns></returns>
	public delegate string LabelGetterDelegate (AxisIncrementToRender toRender);


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
}