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

		public AxisView AxisX {get;} 
		public AxisView AxisY {get;} 

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

		public GraphView()
		{
			CanFocus = true;

			AxisX = new AxisView(this,Orientation.Horizontal);
		 	AxisY = new AxisView(this,Orientation.Vertical);

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
				for(int y=0;y<Bounds.Width;y++){

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
			AxisX.Y = Math.Min(Math.Max(0, origin.Y), Bounds.Height);
			AxisX.Height = AxisX.Thickness;
			AxisX.Width = Bounds.Width;

			// position Y axis

			// Float the Y axis so that it accurately represents the origin of the graph
			// but anchor it to left/right if the origin is offscreen
			AxisY.X = Math.Min (Math.Max (0, origin.X), Bounds.Width - AxisX.Thickness);
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

	public class ScatterSeries : ISeries
	{
		/// <summary>
		/// Collection of each discrete point in the series
		/// </summary>
		/// <returns></returns>
		public List<PointF> Points {get;set;} = new List<PointF>();

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
	public class AxisView : View
	{
		/// <summary>
		/// Direction of the axis
		/// </summary>
		/// <value></value>
		public Orientation Orientation {get;set;}

		/// <summary>
		/// True to render labels next to the 
		/// </summary>
		/// <value></value>
		public bool ShowLabels {get;set;}

		/// <summary>
		/// Number of units of graph space between ticks on axis
		/// </summary>
		/// <value></value>
		public float Increment {get;set;} = 1;

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
		public int Thickness { get; } = 1;

		public AxisView()
		{

		}
		public AxisView(GraphView graph,Orientation orientation)
		{
			Orientation = orientation;
			Graph = graph;
		}

		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);

			// Cannot render orphan axes
			if(Graph == null){
				return;
			}

			float nextTick = 0;

			Rune tickSymbol = Orientation == Orientation.Horizontal ? 
								Driver.TopTee :
								Driver.RightTee ;

			Rune nonTickSymbol = Orientation == Orientation.Horizontal ? 
								Driver.HLine :
								Driver.VLine ;

			if(Orientation == Orientation.Horizontal){
				for(int i=0;i<bounds.Width;i++){
					Move (i,0);

					var rect = Graph.ScreenToGraphSpace (i, 0);
					
					if(rect.X>= nextTick && nextTick <= rect.Right)
					{
						Driver.AddRune(tickSymbol);

						// find the next tick spot that is outside of the current cell
						while (nextTick <= rect.Right) {
							nextTick += Increment;
						}
					}
					else
					{
						Driver.AddRune(nonTickSymbol);
					}
				}
			}
			else{
				// draw axis bottom up
				for(int i= bounds.Height; i>0;i--){
					Move (0, i);

					var rect = Graph.ScreenToGraphSpace (0, i);

					if (rect.Y >= nextTick && nextTick <= rect.Bottom) {
						Driver.AddRune (tickSymbol);

						// find the next tick spot that is outside of the current cell
						while(nextTick <= rect.Bottom) {
							nextTick += Increment;
						}
					} else
					{
						Driver.AddRune(nonTickSymbol);
					}
				}
			}
		}

	}

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