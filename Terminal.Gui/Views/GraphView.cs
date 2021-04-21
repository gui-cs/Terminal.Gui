﻿using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	/// <summary>
	/// Control for rendering graphs (bar, scatter etc)
	/// </summary>
	public class GraphView : View {

		/// <summary>
		/// Horizontal axis
		/// </summary>
		/// <value></value>
		public HorizontalAxis AxisX { get; set; }

		/// <summary>
		/// Vertical axis
		/// </summary>
		/// <value></value>
		public VerticalAxis AxisY { get; set; }

		/// <summary>
		/// Collection of data series that are rendered in the graph
		/// </summary>
		public List<ISeries> Series { get; } = new List<ISeries> ();


		/// <summary>
		/// Elements drawn into graph after series have been drawn e.g. Legends etc
		/// </summary>
		public List<IAnnotation> Annotations { get; } = new List<IAnnotation> ();

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
		public PointF ScrollOffset { get; set; } = new PointF (0, 0);

		/// <summary>
		/// Translates console width/height into graph space. Defaults
		/// to 1 row/col of console space being 1 unit of graph space. 
		/// </summary>
		/// <returns></returns>
		public PointF CellSize { get; set; } = new PointF (1, 1);

		/// <summary>
		/// The color of the background of the graph and axis/labels
		/// </summary>
		public Attribute? GraphColor { get; set; }

		/// <summary>
		/// Creates a new graph with a 1 to 1 graph space with absolute layout
		/// </summary>
		public GraphView ()
		{
			CanFocus = true;

			AxisX = new HorizontalAxis ();
			AxisY = new VerticalAxis ();
		}

		/// <summary>
		/// Clears all settings configured on the graph and resets all properties
		/// to default values (<see cref="CellSize"/>, <see cref="ScrollOffset"/> etc) 
		/// </summary>
		public void Reset ()
		{
			ScrollOffset = new PointF (0, 0);
			CellSize = new PointF (1, 1);
			AxisX.Reset ();
			AxisY.Reset ();
			Series.Clear ();
			Annotations.Clear ();
			GraphColor = null;
			SetNeedsDisplay ();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if(CellSize.X == 0 || CellSize.Y == 0) {
				throw new Exception ($"{nameof(CellSize)} cannot be 0");
			}

			SetDriverColorToGraphColor (); 

			Move (0, 0);

			// clear all old content
			for (int i = 0; i < Bounds.Height; i++) {
				Move (0, i);
				Driver.AddStr (new string (' ', Bounds.Width));
			}

			// If there is no data do not display a graph
			if (!Series.Any () && !Annotations.Any ()) {
				return;
			}

			// Draw 'before' annotations
			foreach (var a in Annotations.Where (a => a.BeforeSeries)) {
				a.Render (this);
			}

			SetDriverColorToGraphColor ();

			AxisY.DrawAxisLine (this);
			AxisX.DrawAxisLine (this);

			AxisY.DrawAxisLabels (this);
			AxisX.DrawAxisLabels (this);

			// Draw a cross where the two axis cross
			var axisIntersection = new Point(AxisY.GetAxisXPosition(this),AxisX.GetAxisYPosition(this));

			if (AxisX.Visible && AxisY.Visible) {
				Move (axisIntersection.X, axisIntersection.Y);
				AddRune (axisIntersection.X, axisIntersection.Y, '\u253C');
			}

			SetDriverColorToGraphColor ();

			// The drawable area of the graph (anything that isn't in the margins)
			Rect drawBounds = new Rect((int)MarginLeft,0, Bounds.Width - ((int)MarginLeft), Bounds.Height - (int)MarginBottom);
			RectangleF graphSpace = ScreenToGraphSpace (drawBounds);

			foreach (var s in Series) {

				s.DrawSeries (this, drawBounds, graphSpace);

				// If a series changes the graph color reset it
				SetDriverColorToGraphColor ();
			}

			SetDriverColorToGraphColor ();

			// Draw 'after' annotations
			foreach (var a in Annotations.Where (a => !a.BeforeSeries)) {
				a.Render (this);
			}

		}

		/// <summary>
		/// Sets the color attribute of <see cref="Application.Driver"/> to the <see cref="GraphColor"/>
		/// (if defined) or <see cref="ColorScheme"/> otherwise.
		/// </summary>
		public void SetDriverColorToGraphColor ()
		{
			Driver.SetAttribute (GraphColor ?? ColorScheme.Normal);
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
				ScrollOffset.X + ((col - MarginLeft) * CellSize.X),
				ScrollOffset.Y + ((Bounds.Height - (row + MarginBottom + 1)) * CellSize.Y),
				CellSize.X, CellSize.Y);
		}


		/// <summary>
		/// Returns the section of the graph that is represented by the screen area
		/// </summary>
		/// <param name="screenArea"></param>
		/// <returns></returns>
		public RectangleF ScreenToGraphSpace (Rect screenArea)
		{
			// get position of the bottom left
			var pos = ScreenToGraphSpace (screenArea.Left, screenArea.Bottom-1);

			return new RectangleF (pos.X, pos.Y, screenArea.Width * CellSize.X, screenArea.Height * CellSize.Y);
		}
		/// <summary>
		/// Calculates the screen location for a given point in graph space.
		/// Bear in mind these be off screen
		/// </summary>
		/// <param name="location">Point in graph space that may or may not be represented in the
		/// visible area of graph currently presented.  E.g. 0,0 for origin</param>
		/// <returns>Screen position (Column/Row) which would be used to render the graph <paramref name="location"/>.
		/// Note that this can be outside the current client area of the control</returns>
		public Point GraphSpaceToScreen (PointF location)
		{
			return new Point (

				(int)((location.X - ScrollOffset.X) / CellSize.X) + (int)MarginLeft,
				 // screen coordinates are top down while graph coordinates are bottom up
				 (Bounds.Height - 1) - (int)MarginBottom - (int)((location.Y - ScrollOffset.Y) / CellSize.Y)
				);
		}



		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			//&& Focused == tabsBar

			if (HasFocus && CanFocus) {
				switch (keyEvent.Key) {

				case Key.CursorLeft:
					Scroll (-CellSize.X, 0);
					return true;
				case Key.CursorLeft | Key.CtrlMask:
					Scroll (-CellSize.X * 5, 0);
					return true;
				case Key.CursorRight:
					Scroll (CellSize.X, 0);
					return true;
				case Key.CursorRight | Key.CtrlMask:
					Scroll (CellSize.X * 5, 0);
					return true;
				case Key.CursorDown:
					Scroll (0, -CellSize.Y);
					return true;
				case Key.CursorDown | Key.CtrlMask:
					Scroll (0, -CellSize.Y * 5);
					return true;
				case Key.CursorUp:
					Scroll (0, CellSize.Y);
					return true;
				case Key.CursorUp | Key.CtrlMask:
					Scroll (0, CellSize.Y * 5);
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

			SetNeedsDisplay ();
		}


		#region Bresenham's line algorithm
		// https://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23

		int ipart (decimal x) { return (int)x; }


		decimal fpart (decimal x)
		{
			if (x < 0) return (1 - (x - Math.Floor (x)));
			return (x - Math.Floor (x));
		}

		/// <summary>
		/// Draws a line between two points in screen space.  Can be diagonals.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="symbol">The symbol to use for the line</param>
		public void DrawLine (Point start, Point end, Rune symbol)
		{
			if (Equals (start, end)) {
				return;
			}

			int x0 = start.X;
			int y0 = start.Y;
			int x1 = end.X;
			int y1 = end.Y;

			int dx = Math.Abs (x1 - x0), sx = x0 < x1 ? 1 : -1;
			int dy = Math.Abs (y1 - y0), sy = y0 < y1 ? 1 : -1;
			int err = (dx > dy ? dx : -dy) / 2, e2;

			while (true) {

				AddRune (x0, y0, symbol);

				if (x0 == x1 && y0 == y1) break;
				e2 = err;
				if (e2 > -dx) { err -= dy; x0 += sx; }
				if (e2 < dy) { err += dx; y0 += sy; }
			}
		}

		#endregion
	}
}