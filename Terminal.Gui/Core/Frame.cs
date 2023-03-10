using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	/// <summary>
	/// Frames are a special form of <see cref="View"/> that act as adornments; they appear outside of the <see cref="View.Bounds"/>
	/// enabling borders, menus, etc... 
	/// </summary>
	public class Frame : View {

		/// <summary>
		/// Frames are a special form of <see cref="View"/> that act as adornments; they appear outside of the <see cref="View.Bounds"/>
		/// enabling borders, menus, etc... 
		/// </summary>
		public Frame ()
		{
		}

		/// <summary>
		/// The Parent of this Frame. 
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
			// Frames are children of a View, not SubViews. Thus ViewToScreen will not work.
			// To get the screen-relative coordinates of a Frame, we need to know who
			// the Parent is

			// Computes the real row, col relative to the screen.
			var inner = Parent?.Bounds ?? Bounds;
			rrow = row - inner.Y;
			rcol = col - inner.X;

			Parent?.ViewToScreen (rcol, rrow, out rcol, out rrow, clipped);

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="clipRect"></param>
		public virtual void OnDrawSubViews (Rect clipRect)
		{
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
		
		/// <inheritdoc/>
		public override void OnDrawContent (Rect viewport)
		{
			if (!ustring.IsNullOrEmpty (TextFormatter.Text)) {
				Clear (viewport);
				SetChildNeedsDisplay ();
				// Draw any Text
				if (TextFormatter != null) {
					TextFormatter.NeedsFormat = true;
					Rect containerBounds = GetContainerBounds ();
					TextFormatter?.Draw (ViewToScreen (viewport), HasFocus ? ColorScheme.Focus : GetNormalColor (),
					    HasFocus ? ColorScheme.HotFocus : Enabled ? ColorScheme.HotNormal : ColorScheme.Disabled,
					    containerBounds);
				}
			}
		}

		/// <summary>
		/// Redraws the Frames that comprise the <see cref="Frame"/>.
		/// </summary>
		/// <param name="clipRect"></param>
		public override void Redraw (Rect clipRect)
		{

			//OnDrawContent (bounds);
			//OnDrawSubViews (bounds);
			//OnDrawContentComplete (bounds);

			if (ColorScheme != null) {
				Driver.SetAttribute (ColorScheme.Normal);
			}

			var screenBounds = ViewToScreen (Frame);
			Thickness.Draw (screenBounds, (string)Data);

			//OnDrawContent (bounds); 

			if (BorderStyle != BorderStyle.None) {
				var lc = new LineCanvas ();
				lc.AddLine (screenBounds.Location, Frame.Width - 1, Orientation.Horizontal, BorderStyle);
				lc.AddLine (screenBounds.Location, Frame.Height - 1, Orientation.Vertical, BorderStyle);

				lc.AddLine (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1), screenBounds.Width - 1, Orientation.Horizontal, BorderStyle);
				lc.AddLine (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y), screenBounds.Height - 1, Orientation.Vertical, BorderStyle);
				foreach (var p in lc.GenerateImage (screenBounds)) {
					Driver.Move (p.Key.X, p.Key.Y);
					Driver.AddRune (p.Value);
				}

				if (!ustring.IsNullOrEmpty (Parent?.Title)) {
					Driver.DrawWindowTitle (screenBounds, Parent?.Title, 0, 0, 0, 0);
				}
			}
		}

		//public Label DiagnosticsLabel { get; set; }
		// TODO: v2 = This is teporary; need to also enable (or not) simple way of setting 
		// other border properties
		// TOOD: v2 - Missing 3D effect
		/// <summary>
		/// 
		/// </summary>
		public BorderStyle BorderStyle { get; set; } = BorderStyle.None;

		/// <summary>
		/// Defines the rectangle that the <see cref="Frame"/> will use to draw its content. 
		/// </summary>
		public Thickness Thickness { get; set; }

		// TODO: v2 - This is confusing. It is a read-only property and actually only returns a size, so 
		// should not be a Rect. However, it may make sense to keep it a Rect and support negative Location
		// for scrolling. Still noodling this.
		/// <summary>
		/// Gets the rectangle that describes the inner area of the frame. The Location is always 0, 0.
		/// </summary>
		public override Rect Bounds {
			get {
				if (Thickness == null) {
					return new Rect (Point.Empty, Frame.Size);
				}
				// Return the frame-relative bounds 
				return Thickness.GetInnerRect (new Rect (Point.Empty, Frame.Size));
			}
			set {
				throw new InvalidOperationException ("It makes no sense to explicitly set Bounds.");
			}
		}
	}
}
