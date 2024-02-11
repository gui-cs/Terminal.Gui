﻿namespace Terminal.Gui;

/// <summary>Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.</summary>
public class Line : View
{
    /// <summary>Constructs a Line object.</summary>
    public Line () { }

    /// <summary>
    ///     The direction of the line.  If you change this you will need to manually update the Width/Height of the control to cover a relevant area based on the new direction.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <inheritdoc/>
    public override bool OnDrawAdornments ()
    {
        Rect screenBounds = BoundsToScreen (Bounds);
        LineCanvas lc;

        lc = SuperView?.LineCanvas;

        lc.AddLine (
                    screenBounds.Location,
                    Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height,
                    Orientation,
                    BorderStyle
                   );

        return true;
    }

    //public override void OnDrawContentComplete (Rect contentArea)
    //{
    //	var screenBounds = ViewToScreen (Frame);

    //}

    /// <inheritdoc/>
    public override void OnDrawContent (Rect contentArea) { OnDrawAdornments (); }
}
