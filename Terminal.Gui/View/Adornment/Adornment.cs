﻿using System;

namespace Terminal.Gui;

// TODO: v2 - Missing 3D effect - 3D effects will be drawn by a mechanism separate from Adornments
// TODO: v2 - If a Adornment has focus, navigation keys (e.g Command.NextView) should cycle through SubViews of the Adornments
// QUESTION: How does a user navigate out of an Adornment to another Adornment, or back into the Parent's SubViews?

/// <summary>
/// Adornments are a special form of <see cref="View"/> that appear outside of the <see cref="View.Bounds"/>
/// enabling borders, menus, etc...See <see cref="Border"/>.
/// </summary>
public class Adornment : View {
	Thickness _thickness = Thickness.Empty;

	/// <summary>
	/// The Parent of this Adornment (the View this Adornment surrounds).
	/// </summary>
	public View Parent { get; set; }

	/// <summary>
	/// Adornments cannot be used as sub-views, so this method always throws an <see cref="InvalidOperationException"/>.
	/// TODO: Are we sure?
	/// </summary>
	public override View SuperView {
		get => null;
		set => throw new NotImplementedException ();
	}

	/// <summary>
	/// Adornments only render to their Parent or Parent's SuperView's LineCanvas,
	/// so this always throws an <see cref="InvalidOperationException"/>.
	/// </summary>
	public override bool SuperViewRendersLineCanvas {
		get => false; // throw new NotImplementedException ();
		set => throw new NotImplementedException ();
	}

	/// <summary>
	/// Defines the rectangle that the <see cref="Adornment"/> will use to draw its content.
	/// </summary>
	public Thickness Thickness {
		get => _thickness;
		set {
			var prev = _thickness;
			_thickness = value;
			if (prev != _thickness) {

				Parent?.LayoutAdornments ();
				OnThicknessChanged (prev);
			}

		}
	}

	/// <summary>
	/// Gets the rectangle that describes the inner area of the Adornment. The Location is always (0,0).
	/// </summary>
	public override Rect Bounds {
		get => Thickness?.GetInside (new Rect (Point.Empty, Frame.Size)) ?? new Rect (Point.Empty, Frame.Size);
		set => throw new InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
	}

	internal override void CreateAdornments ()
	{
		/* Do nothing - Adornments do not have Adornments */
	}

	internal override void LayoutAdornments ()
	{
		/* Do nothing - Adornments do not have Adornments */
	}

	/// <inheritdoc/>
	public override void BoundsToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
	{
		// Adornments are *Children* of a View, not SubViews. Thus View.BoundsToScreen will not work.
		// To get the screen-relative coordinates of a Adornment, we need to know who
		// the Parent is
		var parentFrame = Parent?.Frame ?? Frame;
		rrow = row + parentFrame.Y;
		rcol = col + parentFrame.X;

		// We now have rcol/rrow in coordinates relative to our View's SuperView. If our View's SuperView has
		// a SuperView, keep going...
		Parent?.SuperView?.BoundsToScreen (rcol, rrow, out rcol, out rrow, clipped);
	}

	/// <inheritdoc/>
	public override Rect FrameToScreen ()
	{
		// Adornments are *Children* of a View, not SubViews. Thus View.FrameToScreen will not work.
		// To get the screen-relative coordinates of a Adornment, we need to know who
		// the Parent is
		var ret = Parent?.Frame ?? Frame;
		ret.Size = Frame.Size;

		ret.Location = Parent?.FrameToScreen ().Location ?? ret.Location;

		// We now have coordinates relative to our View. If our View's SuperView has
		// a SuperView, keep going...
		return ret;
	}

	/// <summary>
	/// Does nothing for Adornment
	/// </summary>
	/// <returns></returns>
	public override bool OnDrawAdornments () => false;

	/// <summary>
	/// Does nothing for Adornment
	/// </summary>
	/// <returns></returns>
	public override bool OnRenderLineCanvas () => false;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="clipRect"></param>
	public virtual void OnDrawSubViews (Rect clipRect)
	{
		// TODO: Enable subviews of Adornments (adornments).
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
	/// Redraws the Adornments that comprise the <see cref="Adornment"/>.
	/// </summary>
	public override void OnDrawContent (Rect contentArea)
	{
		if (Thickness == Thickness.Empty) {
			return;
		}

		var screenBounds = BoundsToScreen (Frame);

		Attribute normalAttr = Parent.GetNormalColor ();
		if (ColorScheme != null) {
			// If a color scheme was set, use it instead
			normalAttr = GetNormalColor ();
		} 

		// This just draws/clears the thickness, not the insides.
		Driver.SetAttribute (normalAttr);
		Thickness.Draw (screenBounds, (string)(Data != null ? Data : string.Empty));

		if (!string.IsNullOrEmpty (TextFormatter.Text)) {
			if (TextFormatter != null) {
				TextFormatter.Size = Frame.Size;
				TextFormatter.NeedsFormat = true;
			}
		}

		TextFormatter?.Draw (screenBounds, normalAttr, normalAttr, Rect.Empty, false);
		//base.OnDrawContent (contentArea);
	}

	/// <summary>
	/// Called whenever the <see cref="Thickness"/> property changes.
	/// </summary>
	public virtual void OnThicknessChanged (Thickness previousThickness) => ThicknessChanged?.Invoke (this, new ThicknessEventArgs { Thickness = Thickness, PreviousThickness = previousThickness });

	/// <summary>
	/// Fired whenever the <see cref="Thickness"/> property changes.
	/// </summary>
	public event EventHandler<ThicknessEventArgs> ThicknessChanged;
}