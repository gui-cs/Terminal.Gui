using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	public class Container : View
	{
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
					if (view.Frame.IntersectsWith (clipRect)) {// && (view.Frame.IntersectsWith (boundsAdjustedForBorder) || boundsAdjustedForBorder.X < 0 || bounds.Y < 0)) {
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

		public Frame ()
		{
			IgnoreBorderPropertyOnRedraw = true;

			DiagnosticsLabel = new Label () {
				AutoSize = false,
				X = 0,
				Y = Pos.AnchorEnd (1),
				Width = Dim.Fill (),
				TextAlignment = TextAlignment.Centered

			};
			Add (DiagnosticsLabel);
			SetNeedsLayout ();
		}

		public Thickness Thickness { get; set; }

		public override void OnDrawContent (Rect viewport)
		{
			// do nothing
		}

		public override void Redraw (Rect bounds)
		{
			if (ColorScheme != null) {
				Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			}

			Thickness.Draw (Frame, $"{Text} {DiagnosticsLabel.Text}");
			if (BorderStyle != BorderStyle.None) {
				var lc = new LineCanvas ();
				lc.AddLine (Frame.Location, Frame.Width - 1, Orientation.Horizontal, BorderStyle);
				lc.AddLine (Frame.Location, Frame.Height - 1, Orientation.Vertical, BorderStyle);

				lc.AddLine (new Point (Frame.X, Frame.Y + Frame.Height - 1), Frame.Width - 1, Orientation.Horizontal, BorderStyle);
				lc.AddLine (new Point (Frame.X + Frame.Width - 1, Frame.Y), Frame.Height - 1, Orientation.Vertical, BorderStyle);
				lc.Draw (this, Frame);
				Driver.DrawWindowTitle (Frame, $"{Text} {Thickness}", 0, 0, 0, 0);
			}

			base.Redraw (bounds);
		}
	}

	public class View2 : Container {
		public Frame Margin { get; set; }
		public new Frame Border { get; set; }
		public Frame Padding{ get; set; }

		public View2 ()
		{
			IgnoreBorderPropertyOnRedraw = true;
			Margin = new Frame () {
				Text = "Margin",
				Thickness = new Thickness (15, 2, 15, 4),
				ColorScheme = Colors.ColorSchemes ["Error"]
			};
			//Margin.DiagnosticsLabel.Text = "Margin";

			Border = new Frame () {
				Text = "Border",
				BorderStyle = BorderStyle.Single,
				Thickness = new Thickness (2),
				ColorScheme = Colors.ColorSchemes ["Dialog"]
			};

			Padding = new Frame () {
				Text = "Padding",
				Thickness = new Thickness (3),
				ColorScheme = Colors.ColorSchemes ["Toplevel"]
			};
			SetNeedsLayout ();
		}

		public override void LayoutSubviews ()
		{
			Margin.X = Frame.Location.X;
			Margin.Y = Frame.Location.Y;
			Margin.Width = Frame.Size.Width;
			Margin.Height = Frame.Size.Height;
			Margin.SetNeedsLayout ();
			Margin.LayoutSubviews ();
			Margin.SetNeedsDisplay ();

			var border = Margin.Thickness.GetInnerRect (Frame);
			Border.X = border.Location.X;
			Border.Y = border.Location.Y;
			Border.Width = border.Size.Width;
			Border.Height = border.Size.Height;
			Border.SetNeedsLayout ();
			Border.LayoutSubviews ();
			Border.SetNeedsDisplay ();

			var padding = Border.Thickness.GetInnerRect (border);
			Padding.X = padding.Location.X;
			Padding.Y = padding.Location.Y;
			Padding.Width = padding.Size.Width;
			Padding.Height = padding.Size.Height;
			Padding.SetNeedsLayout ();
			Padding.LayoutSubviews ();
			Padding.SetNeedsDisplay ();

			Bounds = Padding.Thickness.GetInnerRect (padding);

			base.LayoutSubviews ();
		}

		public virtual void OnDrawFrames (Rect frame)
		{
			Margin.Redraw (Margin.Bounds);
			Border.Redraw (Border.Bounds);
			Padding.Redraw (Border.Bounds);

			var border = Margin.Thickness.GetInnerRect (frame);
			var padding = Border.Thickness.GetInnerRect (border);
			var content = Padding.Thickness.GetInnerRect (padding);

			// Draw the diagnostics label on the bottom of the content
			var tf = new TextFormatter () {
				Text = "Content",
				Alignment = TextAlignment.Centered,
				VerticalAlignment = VerticalTextAlignment.Bottom
			};
			tf.Draw (content, ColorScheme.Normal, ColorScheme.Normal);
		}

		public override void Redraw (Rect bounds)
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (ColorScheme != null) {
				Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			}

			OnDrawFrames (Frame);
			base.Redraw (bounds);
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			Margin?.Dispose ();
			Margin = null;
			Border?.Dispose ();
			Border = null;
			Padding?.Dispose ();
			Padding = null;
		}
	}
}
