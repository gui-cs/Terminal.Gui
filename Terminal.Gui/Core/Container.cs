using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	public class Frame : View {

		public Frame ()
		{
			IgnoreBorderPropertyOnRedraw = true;
		}

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

		public override void Redraw (Rect bounds)
		{
			
			//OnDrawContent (bounds);
			//OnDrawSubViews (bounds);
			//OnDrawContentComplete (bounds);

			if (ColorScheme != null) {
				Driver.SetAttribute (ColorScheme.Normal);
			}

			Thickness.Draw (Frame, (string)Data);

			//OnDrawContent (bounds); 

			//if (Text != null) {
			//	Thickness?.Draw (Frame, $"{Text} {DiagnosticsLabel?.Text}");
			//}
			if (BorderStyle != BorderStyle.None) {
				var lc = new LineCanvas ();
				lc.AddLine (Frame.Location, Frame.Width - 1, Orientation.Horizontal, BorderStyle);
				lc.AddLine (Frame.Location, Frame.Height - 1, Orientation.Vertical, BorderStyle);

				lc.AddLine (new Point (Frame.X, Frame.Y + Frame.Height - 1), Frame.Width - 1, Orientation.Horizontal, BorderStyle);
				lc.AddLine (new Point (Frame.X + Frame.Width - 1, Frame.Y), Frame.Height - 1, Orientation.Vertical, BorderStyle);
				foreach (var p in lc.GenerateImage (Frame)) {
					Driver.Move (p.Key.X, p.Key.Y);
					Driver.AddRune (p.Value);
				}

				if (!ustring.IsNullOrEmpty (Title)) {
					Driver.DrawWindowTitle (Frame, Title, 0, 0, 0, 0);
				}
			}
		}

		//public Label DiagnosticsLabel { get; set; }
		// TODO: v2 = This is teporary; need to also enable (or not) simple way of setting 
		// other border properties
		// TOOD: v2 - Missing 3D effect
		public BorderStyle BorderStyle { get; set; } = BorderStyle.None;

		public Thickness Thickness { get; set; }

		// TODO: v2 - This is confusing. It is a read-only property and actually only returns a size, so 
		// should not be a Rect. However, it may make sense to keep it a Rect and support negative Location
		// for scrolling. Still noodling this.
		/// <summary>
		/// Gets the rectangle that describes the inner area of the frame. The Location is always 0, 0.
		/// </summary>
		public new Rect Bounds {
			get {
				if (Thickness != null) {
					new Rect (Point.Empty, Frame.Size);
				}
				// Return the frame-relative bounds 
				return Thickness.GetInnerRect (new Rect (Point.Empty, Frame.Size));
			}
			set {
				throw new InvalidOperationException ("It makes no sense to explicitly set Bounds.");
			}
		}

		//public override void OnDrawContent (Rect viewport)
		//{
		//	// do nothing
		//}

		//public override void Redraw (Rect bounds)
		//{

		//	if (ColorScheme != null) {
		//		Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
		//	}

		//	//if (Text != null) {
		//	//	Thickness?.Draw (Frame, $"{Text} {DiagnosticsLabel?.Text}");
		//	//}
		//	if (BorderStyle != BorderStyle.None) {
		//		var lc = new LineCanvas ();
		//		lc.AddLine (Frame.Location, Frame.Width - 1, Orientation.Horizontal, BorderStyle);
		//		lc.AddLine (Frame.Location, Frame.Height - 1, Orientation.Vertical, BorderStyle);

		//		lc.AddLine (new Point (Frame.X, Frame.Y + Frame.Height - 1), Frame.Width - 1, Orientation.Horizontal, BorderStyle);
		//		lc.AddLine (new Point (Frame.X + Frame.Width - 1, Frame.Y), Frame.Height - 1, Orientation.Vertical, BorderStyle);
		//		foreach (var p in lc.GenerateImage (Frame)) {
		//			Driver.Move (p.Key.X, p.Key.Y);
		//			Driver.AddRune (p.Value);
		//		}

		//		if (!ustring.IsNullOrEmpty (Title)) {
		//			Driver.DrawWindowTitle (Frame, Title, 0, 0, 0, 0);
		//		}
		//	}

		//	base.Redraw (bounds);
		//}
	}

}
