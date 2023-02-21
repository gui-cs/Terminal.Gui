using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	public class Container : View {
		public Container ()
		{
			IgnoreBorderPropertyOnRedraw = true;
		}

		public virtual void OnDrawSubViews (Rect clipRect)
		{
			if (Subviews == null) {
				return;
			}

			foreach (var view in Subviews) {
				if (!view.NeedDisplay.IsEmpty || view.ChildNeedsDisplay || view.LayoutNeeded) {
					if (true) {//)  && (view.Frame.IntersectsWith (boundsAdjustedForBorder) || boundsAdjustedForBorder.X < 0 || bounds.Y < 0)) {
						if (view.LayoutNeeded) {
							view.LayoutSubviews ();
						}

						// Draw the subview
						// Use the view's bounds (view-relative; Location will always be (0,0)
						if (view.Visible && view.Frame.Width > 0 && view.Frame.Height > 0) {
							var rect = view.Bounds;
							//view.OnDrawContent (rect);
							view.Redraw (rect);
							//view.OnDrawContentComplete (rect);
						}
					}
					view.NeedDisplay = Rect.Empty;
					view.ChildNeedsDisplay = false;
				}
			}

		}

		public override void OnDrawContent (Rect viewport)
		{
			if (!ustring.IsNullOrEmpty (TextFormatter.Text)) {
				Clear (viewport);
				SetChildNeedsDisplay ();
				// Draw any Text
				if (TextFormatter != null) {
					TextFormatter.NeedsFormat = true;
				}
				Rect containerBounds = GetContainerBounds ();
				TextFormatter?.Draw (ViewToScreen (viewport), HasFocus ? ColorScheme.Focus : GetNormalColor (),
				    HasFocus ? ColorScheme.HotFocus : Enabled ? ColorScheme.HotNormal : ColorScheme.Disabled,
				    containerBounds);
			}
			//base.OnDrawContent (viewport);
		}

		public override void OnDrawContentComplete (Rect viewport)
		{
			//base.OnDrawContentComplete (viewport);
		}

		public override void Redraw (Rect bounds)
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (ColorScheme != null) {
				Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			}

			OnDrawContent (bounds);
			OnDrawSubViews (bounds);
			OnDrawContentComplete (bounds);
		}

	}

	public class Frame : Container {
		public Label DiagnosticsLabel { get; set; }
		public BorderStyle BorderStyle { get; set; } = BorderStyle.None;

		public Frame () => IgnoreBorderPropertyOnRedraw = true;
		
		public Thickness Thickness { get; set; }

		public new Rect Bounds {
			get {
				if (Thickness != null) {
					new Rect (Point.Empty, Frame.Size);
				}
				var frameRelativeBounds = Thickness.GetInnerRect (new Rect (Point.Empty, Frame.Size));
				return frameRelativeBounds;
			}
			set {
				throw new InvalidOperationException ("It makes no sense to explicitly set Bounds.");
				//Frame = new Rect (Frame.Location, value.Size
				//	+ new Size (Margin.Thickness.Right, Margin.Thickness.Bottom)
				//	+ new Size (BorderFrame.Thickness.Right, BorderFrame.Thickness.Bottom)
				//	+ new Size (BorderFrame.Thickness.Right, BorderFrame.Thickness.Bottom));
			}
		}

		public override void OnDrawContent (Rect viewport)
		{
			// do nothing
		}

		public override void Redraw (Rect bounds)
		{
			if (ColorScheme != null) {
				Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			}

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

			base.Redraw (bounds);
		}
	}

}
