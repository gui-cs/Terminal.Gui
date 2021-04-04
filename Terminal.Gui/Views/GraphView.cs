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
		/// The data space position of the bottom left of the control.
		/// Changing this scrolls the viewport around in the graph
		/// </summary>
		/// <value></value>
		public PointF Origin {get;set;} = new PointF(0,0);

		/// <summary>
		/// Translates console width/height into data space. Defaults
		/// to 1 row/col of console space being 1 unit of data space.		/// 
		/// </summary>
		/// <returns></returns>
		public PointF CellSize {get;set;} = new PointF(1,1);

		public GraphView()
		{

			AxisX = new AxisView(this,Orientation.Horizontal);
		 	AxisY = new AxisView(this,Orientation.Vertical);

			Add(AxisX);
			Add(AxisY);
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw(bounds);

			RefreshViewport();

			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);

			for(int x=0;x<Bounds.Width;x++){
				for(int y=0;y<Bounds.Width;y++){

					var space = ViewToDataSpace(x,y);

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
		/// Projects the given screen location (in client area) into the 
		/// data space (coordinate system of the <see cref="Series"/>)
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		public RectangleF ViewToDataSpace (int col, int row)
		{
			// TODO: fix this to respect Zoom etc
			return new RectangleF(col * CellSize.X, (Bounds.Height - row)*CellSize.Y ,CellSize.X,CellSize.Y);
		}

		/// <summary>
		/// Refreshes the render area of the graph. Call after changing the
		/// scroll area, axis increments etc
		/// </summary>
		public void RefreshViewport(){

			// TODO: Consider Origin

			// position X axis
			AxisX.X = 0;
			AxisX.Y = Bounds.Height -1;
			AxisX.Height = 1;
			AxisX.Width = Bounds.Width;

			// position Y axis
			AxisY.X = 0;
			AxisY.Y = 0;
			AxisY.Height = Bounds.Height;
			AxisY.Width = 1;
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			//&& Focused == tabsBar

			if (HasFocus && CanFocus ) {
				switch (keyEvent.Key) {

				case Key.CursorLeft:
					return true;
				case Key.CursorRight:
					return true;
				}
			}

			return base.ProcessKey (keyEvent);
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
		/// <param name="dataSpace">Projection of the screen location into the chart data space</param>
		Rune? GetCellValueIfAny(RectangleF dataSpace);
	}

	public class ScatterSeries : ISeries
	{
		/// <summary>
		/// Collection of each discrete point in the series
		/// </summary>
		/// <returns></returns>
		public List<PointF> Points {get;set;} = new List<PointF>();

		public Rune? GetCellValueIfAny (RectangleF dataSpace)
		{
			if(Points.Any(p=>dataSpace.Contains(p))){
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
		/// Number of units of data space between ticks on axis
		/// </summary>
		/// <value></value>
		public float Increment {get;set;} = 1;

		/// <summary>
		/// Parent <see cref="GraphView"/> in which this axis is displayed
		/// </summary>
		/// <value></value>
		public GraphView Graph {get;}

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

					var rect = Graph.ViewToDataSpace (i, 0);
					
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

					var rect = Graph.ViewToDataSpace (0, i);

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