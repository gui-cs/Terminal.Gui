using NStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
		public AxisView AxisX {get;} 

		/// <summary>
		/// Vertical axis
		/// </summary>
		/// <value></value>
		public AxisView AxisY {get;} 

		/// <summary>
		/// Collection of data series that are rendered in the graph
		/// </summary>
		/// <returns></returns>
		public List<ISeries> Series {get;} = new List<ISeries>();

		/// <summary>
		/// The graph space position of the bottom left of the control.
		/// Changing this scrolls the viewport around in the graph
		/// </summary>
		/// <value></value>
		public PointF ScrollOffset {get;set;} = new PointF(0,0);

		/// <summary>
		/// Translates console width/height into graph space. Defaults
		/// to 1 row/col of console space being 1 unit of graph space. 
		/// </summary>
		/// <returns></returns>
		public PointF CellSize {get;set;} = new PointF(1,1);

		/// <summary>
		/// Creates a new graph with a 1 to 1 graph space with absolute layout
		/// </summary>
		public GraphView()
		{
			CanFocus = true;

			AxisX = new HorizontalAxis(this);
		 	AxisY = new VerticalAxis(this);

			Add(AxisX);
			Add(AxisY);
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

			RefreshViewport();
						
			for(int x=0;x<Bounds.Width;x++){
				for(int y=0;y<Bounds.Height;y++){

					var space = ScreenToGraphSpace(x,y);

					foreach(var s in Series){
						var rune = s.GetCellValueIfAny(space);
						
						if(rune.HasValue){
							Move(x,y);
							Driver.AddRune(rune.Value);
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
		public RectangleF ScreenToGraphSpace (int col, int row)
		{
			return new RectangleF (
				ScrollOffset.X + (col * CellSize.X),
				ScrollOffset.Y + ((Bounds.Height - row) * CellSize.Y),
				CellSize.X, CellSize.Y);
		}

		/// <summary>
		/// Calculates the screen location for a given point in graph space.
		/// Bear in mind these This may not be off screen
		/// </summary>
		/// <param name="location">Point within the graph</param>
		/// <returns>Screen position (Row / Column) which would be used to render the <paramref name="location"/></returns>
		public Point GraphSpaceToScreen (PointF location)
		{
			return new Point (
				
				(int)((location.X - ScrollOffset.X) / CellSize.X),
				 // screen coordinates are top down while graph coordinates are bottom up
				 Bounds.Height - (int)((location.Y - ScrollOffset.Y) / CellSize.Y) 
				);
		}

		/// <summary>
		/// Refreshes the render area of the graph. Call after changing the
		/// scroll area, axis increments etc
		/// </summary>
		public void RefreshViewport(){

			// find the origin of the graph in screen space (this allows for 'crosshair' style
			// graphs where positive and negative numbers visible
			var origin = GraphSpaceToScreen (new PointF (0, 0));

			// position X axis
			AxisX.X = 0;

			// Float the X axis so that it accurately represents the origin of the graph
			// but anchor it to top/bottom if the origin is offscreen
			AxisX.Y = Math.Min(Math.Max(0, origin.Y), Bounds.Height-AxisX.Thickness);
			AxisX.Height = AxisX.Thickness;
			AxisX.Width = Bounds.Width;

			// position Y axis

			// Float the Y axis so that it accurately represents the origin of the graph
			// but anchor it to left/right if the origin is offscreen
			AxisY.X = Math.Min (Math.Max (0, origin.X - AxisX.Thickness), Bounds.Width);
			AxisY.Y = 0;
			AxisY.Height = Bounds.Height;
			AxisY.Width = AxisY.Thickness;
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
		private void Scroll (float offsetX, float offsetY)
		{
			ScrollOffset = new PointF (
				ScrollOffset.X + offsetX,
				ScrollOffset.Y + offsetY);

			RefreshViewport ();
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
		Rune? GetCellValueIfAny(RectangleF graphSpace);
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
		public List<PointF> Points {get;set;} = new List<PointF>();

		/// <summary>
		/// Returns a point symbol if the <paramref name="graphSpace"/> contains 
		/// any of the <see cref="Points"/>
		/// </summary>
		/// <param name="graphSpace"></param>
		/// <returns></returns>
		public Rune? GetCellValueIfAny (RectangleF graphSpace)
		{
			if(Points.Any(p=>graphSpace.Contains(p))){
				return 'x';
			}

			return null;
		}
	}

	/// <summary>
	/// Renders a continuous line with grid line ticks and labels
	/// </summary>
	public abstract class AxisView : View
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
		public float Increment {get;set;} = 1;

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
		/// Parent <see cref="GraphView"/> in which this axis is displayed
		/// </summary>
		/// <value></value>
		public GraphView Graph {get;}

		/// <summary>
		/// The amount of screen space required to render the line and labels.
		/// This is a measure perpendicular to the <see cref="Orientation"/>
		/// (i.e. it is height of a Horizontal axis and width of a Vertical axis)
		/// </summary>
		public abstract int Thickness { get; }

		protected AxisView (Orientation orientation)
		{
			Orientation = orientation;
		}
		protected AxisView(GraphView graph,Orientation orientation):this(orientation)
		{
			Graph = graph;
		}
	}

	/// <summary>
	/// The horizontal (x axis) of a <see cref="GraphView"/>
	/// </summary>
	public class HorizontalAxis : AxisView {
		public HorizontalAxis ():base(Orientation.Horizontal)
		{

			LabelGetter = DefaultLabelGetter;
		}
		public HorizontalAxis (GraphView graph) : base (graph,Orientation.Horizontal)
		{
			LabelGetter = DefaultLabelGetter;
		}

		/// <summary>
		/// If no labels then thickness of axis is 1 otherwise 2 (text runs along
		/// the bottom).
		/// </summary>
		public override int Thickness => ShowLabelsEvery == 0 ? 1 : 2;

		private string DefaultLabelGetter (AxisIncrementToRender toRender)
		{
			return ((int)(toRender.GraphSpace.X + toRender.GraphSpace.Width / 2)).ToString ();
		}


		/// <summary>
		/// Draws the horizontal x axis including labels and increment ticks
		/// </summary>
		/// <param name="bounds"></param>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);

			// Cannot render orphan axes
			if (Graph == null) {
				return;
			}

			AxisIncrementToRender toRender = null;
			int labels = 0;

			for (int i = 0; i < Bounds.Width; i++) {

				Move (i, 0);

				// what bit of the graph is supposed to go here?
				var graphSpace = Graph.ScreenToGraphSpace (i,0);

				// if we are overdue rendering a label
				if (toRender == null || graphSpace.X > toRender.GraphSpace.X + Increment) {

					toRender = new AxisIncrementToRender (Orientation, new Point (i, 0), graphSpace);

					// draw the tick on the axis
					Driver.AddRune (Driver.TopTee);

					// and the label (if we are due one)
					if (ShowLabelsEvery != 0) {

						// if this increment also needs a label
						if (labels++ % ShowLabelsEvery == 0) {
							Move (i, 1);
							Driver.AddStr (LabelGetter (toRender));
						};
					}
				}
				else
				{
					Driver.AddRune (Driver.HLine);
				}
			}
		}
	}

	/// <summary>
	/// The vertical (i.e. Y axis) of a <see cref="GraphView"/>
	/// </summary>
	public class VerticalAxis : AxisView {

		/// <summary>
		/// Returns 1 + widest visible label
		/// </summary>
		public override int Thickness => GetThickness();

		private int GetThickness()
		{
			return GetThickness(GetLabels());
		}

		private int GetThickness (IEnumerable<AxisIncrementToRender> labels)
		{
			// Thickness is 1 (line) + the widest label
			var l = labels.ToArray();
			if(l.Length == 0){
				return 1;
			}

			return  l.Max(s=>s.Text.Length) + 1;
		}

		public VerticalAxis () :base(Orientation.Vertical)
		{

			LabelGetter = DefaultLabelGetter;
		}
		public VerticalAxis (GraphView graph) : base (graph, Orientation.Vertical)
		{
			LabelGetter = DefaultLabelGetter;
		}

		private string DefaultLabelGetter (AxisIncrementToRender toRender)
		{
			return ((int)(toRender.GraphSpace.Y + toRender.GraphSpace.Height / 2)).ToString ();
		}

		/// <summary>
		/// Draws the vertical y axis including labels and increment ticks
		/// </summary>
		/// <param name="bounds"></param>
		public override void Redraw (Rect bounds)
		{
			// Cannot render orphan axes
			if (Graph == null) {
				return;
			}

			Driver.SetAttribute (ColorScheme.Normal);

			var labels = GetLabels();
			var width = GetThickness(labels);
			
			// Draw solid line
			// draw axis bottom up
			for (int i = Bounds.Height; i > 0; i--) {
				Move (width-1, i);

				var label = labels.FirstOrDefault(l=>l.ScreenLocation.Y == i);

				if(label != null){
					// draw the tick on the axis
					Driver.AddRune (Driver.RightTee);

					// and the label text
					if(!string.IsNullOrWhiteSpace(label.Text)){
						Move (0, i);
						Driver.AddStr (label.Text);
					}
				} else {
					Driver.AddRune (Driver.VLine);
				}
			}

		}

		private IEnumerable<AxisIncrementToRender> GetLabels ()
		{
			
			AxisIncrementToRender toRender = null;
			int labels = 0;

			for (int i = Bounds.Height; i > 0; i--) {

				// what bit of the graph is supposed to go here?
				var graphSpace = Graph.ScreenToGraphSpace (0,i);

				// if we are overdue rendering a label
				if (toRender == null || graphSpace.Y > toRender.GraphSpace.Y + Increment) {

					toRender = new AxisIncrementToRender (Orientation, new Point (0, i), graphSpace);

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
		/// Location in the <see cref="AxisView"/> that the axis increment appears
		/// </summary>
		public Point ScreenLocation { get;  }

		/// <summary>
		/// The volume of graph that is represented by this screen coordingate
		/// </summary>
		public RectangleF GraphSpace { get; }

		/// <summary>
		/// The text (if any) that should be displayed at this axis increment
		/// </summary>
		/// <value></value>
		public string Text { get; internal set; } = "";

		/// <summary>
		/// Describe a new section of an axis that requires an axis increment
		/// symbol and/or label
		/// </summary>
		/// <param name="orientation"></param>
		/// <param name="screen"></param>
		/// <param name="graphSpace"></param>
		public AxisIncrementToRender (Orientation orientation,Point screen, RectangleF graphSpace)
		{
			Orientation = orientation;
			ScreenLocation = screen;
			GraphSpace = graphSpace;
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