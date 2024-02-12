﻿namespace Terminal.Gui;

public partial class View
{
    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.</summary>
    public virtual bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if want mouse position reports; otherwise, <see langword="false"/>.</value>
    public virtual bool WantMousePositionReports { get; set; }

    /// <summary>Event fired when a mouse event is generated.</summary>
    public event EventHandler<MouseEventEventArgs> MouseClick;

    /// <summary>Event fired when the view receives the mouse event for the first time.</summary>
    public event EventHandler<MouseEventEventArgs> MouseEnter;

    /// <summary>Event fired when the view receives a mouse event for the last time.</summary>
    public event EventHandler<MouseEventEventArgs> MouseLeave;

    /// <inheritdoc/>
    public override bool OnMouseEnter (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);
        MouseEnter?.Invoke (this, args);

        return args.Handled || base.OnMouseEnter (mouseEvent);
    }

    /// <summary>Method invoked when a mouse event is generated</summary>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    public virtual bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);

        if (MouseEvent (mouseEvent))
        {
            return true;
        }

        if (mouseEvent.Flags == MouseFlags.Button1Clicked)
        {
            if (CanFocus && !HasFocus && SuperView != null)
            {
                SuperView.SetFocus (this);
                SetNeedsDisplay ();
            }

            return OnMouseClick (args);
        }

        if (mouseEvent.Flags == MouseFlags.Button2Clicked)
        {
            return OnMouseClick (args);
        }

        if (mouseEvent.Flags == MouseFlags.Button3Clicked)
        {
            return OnMouseClick (args);
        }

        if (mouseEvent.Flags == MouseFlags.Button4Clicked)
        {
            return OnMouseClick (args);
        }

        return false;
    }

    /// <inheritdoc/>
    public override bool OnMouseLeave (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);
        MouseLeave?.Invoke (this, args);

        return args.Handled || base.OnMouseLeave (mouseEvent);
    }

    /// <summary>Invokes the MouseClick event.</summary>
    protected bool OnMouseClick (MouseEventEventArgs args)
    {
        if (!Enabled)
        {
            return true;
        }

        MouseClick?.Invoke (this, args);

        return args.Handled;
    }
}
