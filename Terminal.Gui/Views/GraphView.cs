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

		AxisView AxisX {get;} = new AxisView(Orientation.Horizontal);
		AxisView AxisY {get;} = new AxisView(Orientation.Vertical);

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
		public PointF Zoom {get;set;} = new PointF(1,1);

		public GraphView()
		{
			Add(AxisX);
			Add(AxisY);
			RefreshViewport();

		}
		

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			RefreshViewport();

			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);

			AxisX.Redraw (AxisX.Bounds);
			AxisY.Redraw (AxisY.Bounds);
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

		public AxisView()
		{

		}
		public AxisView(Orientation orientation)
		{
			Orientation = orientation;
		}

		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			//Driver.SetAttribute (ColorScheme.Normal);

			if(Orientation == Orientation.Horizontal){
				for(int i=0;i<bounds.Width;i++){
					Move (i,0);
					Driver.AddRune(Driver.TopTee);
				}
			}
			else{

				for(int i=0;i<bounds.Height;i++){
					Move (0, i);
					Driver.AddRune(Driver.RightTee);
				}
			}
		}

	}

	public enum Orientation
	{
		Horizontal,
		Vertical
	}
}