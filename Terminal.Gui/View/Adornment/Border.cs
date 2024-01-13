﻿using System;
using System.Linq;

namespace Terminal.Gui;

/// <summary>
/// Implements Border for <see cref="View"/>. 
/// </summary>
/// <remarks>
/// <para>
/// Renders a border around the view including the <see cref="View.Title"/>. If <see cref="Thickness"/> is non-zero the border
/// will be drawn. 
/// </para>
/// <para>
/// See the <see cref="Adornment"/> class. 
/// </para>
/// </remarks>
public class Border : Adornment {
	/// <summary>
	/// Sets the style of the border by changing the <see cref="Thickness"/>. This is a helper API for
	/// setting the <see cref="Thickness"/> to <c>(1,1,1,1)</c> and setting the line style of the
	/// views that comprise the border. If set to <see cref="LineStyle.None"/> no border will be drawn.
	/// </summary>
	public new LineStyle BorderStyle { get; set; } = LineStyle.None;

	/// <inheritdoc />
	public override void OnDrawContent (Rect contentArea)
	{
		base.OnDrawContent (contentArea);

		if (Thickness == Thickness.Empty) {
			return;
		}

		//Driver.SetAttribute (Colors.Error.Normal);
		var screenBounds = BoundsToScreen (Frame);

		//OnDrawSubviews (bounds); 

		// TODO: v2 - this will eventually be two controls: "BorderView" and "Label" (for the title)

		// The border frame (and title) are drawn at the outermost edge of border; 
		// For Border
		// ...thickness extends outward (border/title is always as far in as possible)
		var borderBounds = new Rect (
			screenBounds.X + Math.Max (0, Thickness.Left - 1),
			screenBounds.Y + Math.Max (0, Thickness.Top - 1),
			Math.Max (0, screenBounds.Width - Math.Max (0, Math.Max (0, Thickness.Left - 1) + Math.Max (0, Thickness.Right - 1))),
			Math.Max (0, screenBounds.Height - Math.Max (0, Math.Max (0, Thickness.Top - 1) + Math.Max (0, Thickness.Bottom - 1))));

		var topTitleLineY = borderBounds.Y;
		var titleY = borderBounds.Y;
		var titleBarsLength = 0; // the little vertical thingies
		var maxTitleWidth = Math.Min (Parent.Title.GetColumns (), Math.Min (screenBounds.Width - 4, borderBounds.Width - 4));
		var sideLineLength = borderBounds.Height;
		var canDrawBorder = borderBounds.Width > 0 && borderBounds.Height > 0;

		if (!string.IsNullOrEmpty (Parent?.Title)) {
			if (Thickness.Top == 2) {
				topTitleLineY = borderBounds.Y - 1;
				titleY = topTitleLineY + 1;
				titleBarsLength = 2;
			}

			// ┌────┐
			//┌┘View└
			//│
			if (Thickness.Top == 3) {
				topTitleLineY = borderBounds.Y - (Thickness.Top - 1);
				titleY = topTitleLineY + 1;
				titleBarsLength = 3;
				sideLineLength++;
			}

			// ┌────┐
			//┌┘View└
			//│
			if (Thickness.Top > 3) {
				topTitleLineY = borderBounds.Y - 2;
				titleY = topTitleLineY + 1;
				titleBarsLength = 3;
				sideLineLength++;
			}

		}

		if (canDrawBorder && Thickness.Top > 0 && maxTitleWidth > 0 && !string.IsNullOrEmpty (Parent?.Title)) {
			var prevAttr = Driver.GetAttribute ();
			if (ColorScheme != null) {
				Driver.SetAttribute (HasFocus ? GetHotNormalColor () : GetNormalColor ());
			} else {
				Driver.SetAttribute (Parent.HasFocus ? Parent.GetHotNormalColor () : Parent.GetNormalColor ());
			}
			DrawTitle (new Rect (borderBounds.X, titleY, maxTitleWidth, 1), Parent?.Title);
			Driver.SetAttribute (prevAttr);
		}

		if (canDrawBorder && BorderStyle != LineStyle.None) {
			var lc = Parent?.LineCanvas;

			var drawTop = Thickness.Top > 0 && Frame.Width > 1 && Frame.Height > 1;
			var drawLeft = Thickness.Left > 0 && (Frame.Height > 1 || Thickness.Top == 0);
			var drawBottom = Thickness.Bottom > 0 && Frame.Width > 1;
			var drawRight = Thickness.Right > 0 && (Frame.Height > 1 || Thickness.Top == 0);

			var prevAttr = Driver.GetAttribute ();
			if (ColorScheme != null) {
				Driver.SetAttribute (GetNormalColor ());
			} else {
				Driver.SetAttribute (Parent.GetNormalColor ());
			}

			if (drawTop) {
				// ╔╡Title╞═════╗
				// ╔╡╞═════╗
				if (borderBounds.Width < 4 || string.IsNullOrEmpty (Parent?.Title)) {
					// ╔╡╞╗ should be ╔══╗
					lc.AddLine (new Point (borderBounds.Location.X, titleY), borderBounds.Width, Orientation.Horizontal, BorderStyle, Driver.GetAttribute ());
				} else {

					// ┌────┐
					//┌┘View└
					//│
					if (Thickness.Top == 2) {
						lc.AddLine (new Point (borderBounds.X + 1, topTitleLineY), Math.Min (borderBounds.Width - 2, maxTitleWidth + 2), Orientation.Horizontal, BorderStyle, Driver.GetAttribute ());
					}
					// ┌────┐
					//┌┘View└
					//│
					if (borderBounds.Width >= 4 && Thickness.Top > 2) {
						lc.AddLine (new Point (borderBounds.X + 1, topTitleLineY), Math.Min (borderBounds.Width - 2, maxTitleWidth + 2), Orientation.Horizontal, BorderStyle, Driver.GetAttribute ());
						lc.AddLine (new Point (borderBounds.X + 1, topTitleLineY + 2), Math.Min (borderBounds.Width - 2, maxTitleWidth + 2), Orientation.Horizontal, BorderStyle, Driver.GetAttribute ());
					}

					// ╔╡Title╞═════╗
					// Add a short horiz line for ╔╡
					lc.AddLine (new Point (borderBounds.Location.X, titleY), 2, Orientation.Horizontal, BorderStyle, Driver.GetAttribute ());
					// Add a vert line for ╔╡
					lc.AddLine (new Point (borderBounds.X + 1, topTitleLineY), titleBarsLength, Orientation.Vertical, LineStyle.Single, Driver.GetAttribute ());
					// Add a vert line for ╞
					lc.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, topTitleLineY), titleBarsLength, Orientation.Vertical, LineStyle.Single, Driver.GetAttribute ());
					// Add the right hand line for ╞═════╗
					lc.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, titleY), borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2), Orientation.Horizontal, BorderStyle, Driver.GetAttribute ());
				}
			}
			if (drawLeft) {
				lc.AddLine (new Point (borderBounds.Location.X, titleY), sideLineLength, Orientation.Vertical, BorderStyle, Driver.GetAttribute ());
			}
			if (drawBottom) {
				lc.AddLine (new Point (borderBounds.X, borderBounds.Y + borderBounds.Height - 1), borderBounds.Width, Orientation.Horizontal, BorderStyle, Driver.GetAttribute ());
			}
			if (drawRight) {
				lc.AddLine (new Point (borderBounds.X + borderBounds.Width - 1, titleY), sideLineLength, Orientation.Vertical, BorderStyle, Driver.GetAttribute ());
			}
			Driver.SetAttribute (prevAttr);

			// TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
			if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler) == ConsoleDriver.DiagnosticFlags.FrameRuler) {
				// Top
				var hruler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };
				if (drawTop) {
					hruler.Draw (new Point (screenBounds.X, screenBounds.Y));
				}

				// Redraw title 
				if (drawTop && maxTitleWidth > 0 && !string.IsNullOrEmpty (Parent?.Title)) {
					prevAttr = Driver.GetAttribute ();
					if (ColorScheme != null) {
						Driver.SetAttribute (HasFocus ? GetHotNormalColor () : GetNormalColor ());
					} else {
						Driver.SetAttribute (Parent.HasFocus ? Parent.GetHotNormalColor () : Parent.GetNormalColor ());
					}
					DrawTitle (new Rect (borderBounds.X, titleY, Parent.Title.GetColumns (), 1), Parent?.Title);
					Driver.SetAttribute (prevAttr);
				}

				//Left
				var vruler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };
				if (drawLeft) {
					vruler.Draw (new Point (screenBounds.X, screenBounds.Y + 1), 1);
				}

				// Bottom
				if (drawBottom) {
					hruler.Draw (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
				}

				// Right
				if (drawRight) {
					vruler.Draw (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
				}

			}
		}

		//base.OnDrawContent (contentArea);
	}

	/// <summary>
	/// Draws the title for a Window-style view.
	/// </summary>
	/// <param name="region">Screen relative region where the title will be drawn.</param>
	/// <param name="title">The title.</param>
	public void DrawTitle (Rect region, string title)
	{
		var width = region.Width;
		if (!string.IsNullOrEmpty (title)) {
			Driver.Move (region.X + 2, region.Y);
			//Driver.AddRune (' ');
			var str = title.EnumerateRunes ().Sum (r => Math.Max (r.GetColumns (), 1)) >= width
				? TextFormatter.Format (title, width, false, false) [0] : title;
			Driver.AddStr (str);
		}
	}

	/// <summary>
	/// Draws a frame in the current view, clipped by the boundary of this view
	/// </summary>
	/// <param name="region">View-relative region for the frame to be drawn.</param>
	/// <param name="clear">If set to <see langword="true"/> it clear the region.</param>
	[Obsolete ("This method is obsolete in v2. Use use LineCanvas or Frame instead.", false)]
	public void DrawFrame (Rect region, bool clear)
	{
		var savedClip = ClipToBounds ();
		var screenBounds = BoundsToScreen (region);

		if (clear) {
			Driver.FillRect (region);
		}

		var lc = new LineCanvas ();
		var drawTop = region.Width > 1 && region.Height > 1;
		var drawLeft = region.Width > 1 && region.Height > 1;
		var drawBottom = region.Width > 1 && region.Height > 1;
		var drawRight = region.Width > 1 && region.Height > 1;

		if (drawTop) {
			lc.AddLine (screenBounds.Location, screenBounds.Width, Orientation.Horizontal, BorderStyle);
		}
		if (drawLeft) {
			lc.AddLine (screenBounds.Location, screenBounds.Height, Orientation.Vertical, BorderStyle);
		}
		if (drawBottom) {
			lc.AddLine (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1), screenBounds.Width, Orientation.Horizontal, BorderStyle);
		}
		if (drawRight) {
			lc.AddLine (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y), screenBounds.Height, Orientation.Vertical, BorderStyle);
		}
		foreach (var p in lc.GetMap ()) {
			Driver.Move (p.Key.X, p.Key.Y);
			Driver.AddRune (p.Value);
		}
		lc.Clear ();

		// TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
		if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler) == ConsoleDriver.DiagnosticFlags.FrameRuler) {
			// Top
			var hruler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };
			if (drawTop) {
				hruler.Draw (new Point (screenBounds.X, screenBounds.Y));
			}

			//Left
			var vruler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };
			if (drawLeft) {
				vruler.Draw (new Point (screenBounds.X, screenBounds.Y + 1), 1);
			}

			// Bottom
			if (drawBottom) {
				hruler.Draw (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
			}

			// Right
			if (drawRight) {
				vruler.Draw (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
			}
		}

		Driver.Clip = savedClip;
	}
}
