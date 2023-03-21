using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	// TODO: v2 - Missing 3D effect - 3D effects will be drawn by a mechanism separate from Frames
	// TODO: v2 - If a Frame has focus, navigation keys (e.g Command.NextView) should cycle through SubViews of the Frame
	// QUESTION: How does a user navigate out of a Frame to another Frame, or back into the Parent's SubViews?

	/// <summary>
	/// Frames are a special form of <see cref="View"/> that act as adornments; they appear outside of the <see cref="View.Bounds"/>
	/// enabling borders, menus, etc... 
	/// </summary>
	public class Frame : View {
		private Thickness _thickness = Thickness.Empty;

		internal override void CreateFrames () { /* Do nothing - Frames do not have Frames */ }

		/// <summary>
		/// The Parent of this Frame (the View this Frame surrounds).
		/// </summary>
		public View Parent { get; set; }

		/// <summary>
		/// Frames cannot be used as sub-views, so this method always throws an <see cref="InvalidOperationException"/>.
		/// TODO: Are we sure?
		/// </summary>
		public override View SuperView {
			get {
				return null;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		/// <inheritdoc/>
		public override void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
		{
			// Frames are *Children* of a View, not SubViews. Thus View.ViewToScreen will not work.
			// To get the screen-relative coordinates of a Frame, we need to know who
			// the Parent is
			var parentFrame = Parent?.Frame ?? Frame;
			rrow = row + parentFrame.Y;
			rcol = col + parentFrame.X;

			// We now have rcol/rrow in coordinates relative to our SuperView. If our SuperView has
			// a SuperView, keep going...
			Parent?.SuperView?.ViewToScreen (rcol, rrow, out rcol, out rrow, clipped);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="clipRect"></param>
		public virtual void OnDrawSubViews (Rect clipRect)
		{
			// TODO: Enable subviews of Frames (adornments).
			//	if (Subviews == null) {
			//		return;
			//	}

			//	foreach (var view in Subviews) {
			//		// BUGBUG: v2 - shouldn't this be !view.LayoutNeeded? Why draw if layout is going to happen and we'll just draw again?
			//		if (view.LayoutNeeded) {
			//			view.LayoutSubviews ();
			//		}
			//		if ((view.Visible && !view.NeedDisplay.IsEmpty && view.Frame.Width > 0 && view.Frame.Height > 0) || view.ChildNeedsDisplay) {
			//			view.Redraw (view.Bounds);

			//			view.NeedDisplay = Rect.Empty;
			//			// BUGBUG - v2 why does this need to be set to false?
			//			// Shouldn't it be set when the subviews draw?
			//			view.ChildNeedsDisplay = false;
			//		}
			//	}

		}

		/// <summary>
		/// Redraws the Frames that comprise the <see cref="Frame"/>.
		/// </summary>
		/// <param name="clipRect"></param>
		public override void Redraw (Rect clipRect)
		{
			if (Thickness == Thickness.Empty) return;

			if (ColorScheme != null) {
				Driver.SetAttribute (ColorScheme.Normal);
			} else {
				Driver.SetAttribute (Parent.GetNormalColor ());
			}

			var prevClip = SetClip (Frame);

			var screenBounds = ViewToScreen (Frame);
			Thickness.Draw (screenBounds, (string)(Data != null ? Data : string.Empty));

			//OnDrawSubviews (bounds); 

			// TODO: v2 - this will eventually be two controls: "BorderView" and "Label" (for the title)

			if (Id == "BorderFrame" && Thickness.Top > 0 && !ustring.IsNullOrEmpty (Parent?.Title)) {

				Driver.SetAttribute (Parent.HasFocus ? Parent.GetHotNormalColor () : Parent.GetNormalColor ());
				Driver.DrawWindowTitle (screenBounds, Parent?.Title, 0, 0, 0, 0);
			}

			if (Id == "BorderFrame" && BorderStyle != BorderStyle.None) {
				var lc = new LineCanvas ();
				if (Thickness.Top > 0) {
					// ╔╡ Title ╞═════╗
					// ╔╡  ╞═════╗
					if (Frame.Width < 6 || ustring.IsNullOrEmpty (Parent?.Title)) {
						// ╔╡╞╗ should be ╔══╗
						lc.AddLine (screenBounds.Location, Frame.Width - 1, Orientation.Horizontal, BorderStyle);
					} else {
						var titleWidth = Math.Min (Parent.Title.ConsoleWidth, Frame.Width - 6);
						// ╔╡ Title ╞═════╗
						// Add a short horiz line for ╔╡
						lc.AddLine (screenBounds.Location, 1, Orientation.Horizontal, BorderStyle);
						// Add a short vert line for ╔╡
						lc.AddLine (new Point (screenBounds.X + 1, screenBounds.Location.Y), 0, Orientation.Vertical, BorderStyle.Single);
						// Add a short vert line for ╞
						lc.AddLine (new Point (screenBounds.X + 1 + (titleWidth + 1), screenBounds.Location.Y), 0, Orientation.Vertical, BorderStyle.Single);
						// Add the right hand line for ╞═════╗
						lc.AddLine (new Point (screenBounds.X + 1 + (titleWidth + 1), screenBounds.Location.Y), Frame.Width - (titleWidth + 3), Orientation.Horizontal, BorderStyle);
					}
				}
				if (Thickness.Left > 0) {
					lc.AddLine (screenBounds.Location, Frame.Height - 1, Orientation.Vertical, BorderStyle);
				}
				if (Thickness.Bottom > 0) {
					lc.AddLine (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1), screenBounds.Width - 1, Orientation.Horizontal, BorderStyle);
				}
				if (Thickness.Right > 0) {
					lc.AddLine (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y), screenBounds.Height - 1, Orientation.Vertical, BorderStyle);
				}
				foreach (var p in lc.GenerateImage (screenBounds)) {
					Driver.Move (p.Key.X, p.Key.Y);
					Driver.AddRune (p.Value);
				}
			}


			Driver.Clip = prevClip;
		}

		// TODO: v2 - Frame.BorderStyle is temporary - Eventually the border will be drawn by a "BorderView" that is a subview of the Frame.
		/// <summary>
		/// 
		/// </summary>
		public BorderStyle BorderStyle { get; set; } = BorderStyle.None;

		/// <summary>
		/// Defines the rectangle that the <see cref="Frame"/> will use to draw its content. 
		/// </summary>
		public Thickness Thickness {
			get { return _thickness; }
			set {
				var prev = _thickness;
				_thickness = value;
				if (prev != _thickness) {
					OnThicknessChanged ();
				}

			}
		}

		/// <summary>
		/// Called whenever the <see cref="Thickness"/> property changes.
		/// </summary>
		public virtual void OnThicknessChanged ()
		{
			ThicknessChanged?.Invoke (this, new ThicknessEventArgs () { Thickness = Thickness });
		}

		/// <summary>
		/// Fired whenever the <see cref="Thickness"/> property changes.
		/// </summary>
		public event EventHandler<ThicknessEventArgs> ThicknessChanged;

		/// <summary>
		/// Gets the rectangle that describes the inner area of the frame. The Location is always (0,0).
		/// </summary>
		public override Rect Bounds {
			get {
				return Thickness?.GetInnerRect (new Rect (Point.Empty, Frame.Size)) ?? new Rect (Point.Empty, Frame.Size);
			}
			set {
				throw new InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
			}
		}
	}
}
