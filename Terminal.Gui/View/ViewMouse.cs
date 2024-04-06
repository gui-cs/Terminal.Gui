using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
/// Describes the highlight style of a view.
/// </summary>
[Flags]
public enum HighlightStyle
{
    /// <summary>
    /// No highlight.
    /// </summary>
    None = 0,

    /// <summary>
    /// The mouse is hovering over the view.
    /// </summary>
    Hover = 1,

    /// <summary>
    /// The mouse is pressed within the <see cref="View.Bounds"/>.
    /// </summary>
    Pressed = 2,

    /// <summary>
    /// The mouse is pressed but moved outside the <see cref="View.Bounds"/>.
    /// </summary>
    PressedOutside = 4
}

/// <summary>
/// Event arguments for the <see cref="View.Highlight"/> event.
/// </summary>
public class HighlightEventArgs : CancelEventArgs
{
    public HighlightEventArgs (HighlightStyle style)
    {
        HighlightStyle = style;
    }

    /// <summary>
    /// The highlight style.
    /// </summary>
    public HighlightStyle HighlightStyle { get; }
}

public partial class View
{
    /// <summary>
    ///     Gets or sets whether the <see cref="View"/> will be highlighted visually while the mouse button is
    ///     pressed.
    /// </summary>
    public HighlightStyle HighlightStyle { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants continuous button pressed events.</summary>
    public virtual bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if mouse position reports are wanted; otherwise, <see langword="false"/>.</value>
    public virtual bool WantMousePositionReports { get; set; }

    /// <summary>
    ///     Called by <see cref="Application.OnMouseEvent"/> when the mouse enters <see cref="Bounds"/>. The view will
    ///     then receive mouse events until <see cref="NewMouseLeaveEvent"/> is called indicating the mouse has left
    ///     the view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be both enabled and visible to receive mouse events.
    ///     </para>
    ///     <para>
    ///         This method calls <see cref="OnMouseEnter"/> to fire the event.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/> if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool? NewMouseEnterEvent (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        return OnMouseEnter (mouseEvent);
    }

    /// <summary>
    ///     Called by <see cref="NewMouseEvent"/> when the mouse enters <see cref="Bounds"/>. The view will
    ///     then receive mouse events until <see cref="OnMouseLeave"/> is called indicating the mouse has left
    ///     the view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Override this method or subscribe to <see cref="MouseEnter"/> to change the default enter behavior.
    /// </para>
    /// <para>
    ///     The coordinates are relative to <see cref="View.Bounds"/>.
    /// </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool OnMouseEnter (MouseEvent mouseEvent)
    {

        var args = new MouseEventEventArgs (mouseEvent);
        MouseEnter?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Event fired when the mouse moves into the View's <see cref="Bounds"/>.</summary>
    public event EventHandler<MouseEventEventArgs> MouseEnter;


    /// <summary>
    ///     Called by <see cref="Application.OnMouseEvent"/> when the mouse leaves <see cref="Bounds"/>. The view will
    ///     then no longer receive mouse events.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be both enabled and visible to receive mouse events.
    ///     </para>
    ///     <para>
    ///         This method calls <see cref="OnMouseLeave"/> to fire the event.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/> if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool? NewMouseLeaveEvent (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        return OnMouseLeave (mouseEvent);
    }
    /// <summary>
    ///     Called by <see cref="NewMouseEvent"/> when a mouse leaves <see cref="Bounds"/>. The view will
    ///     no longer receive mouse events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Override this method or subscribe to <see cref="MouseEnter"/> to change the default leave behavior.
    /// </para>
    /// <para>
    ///     The coordinates are relative to <see cref="View.Bounds"/>.
    /// </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool OnMouseLeave (MouseEvent mouseEvent)
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

        return args.Handled;
    }

    /// <summary>Event fired when the mouse leaves the View's <see cref="Bounds"/>.</summary>
    public event EventHandler<MouseEventEventArgs> MouseLeave;

    /// <summary>
    ///     Processes a <see cref="MouseEvent"/>. This method is called by <see cref="Application.OnMouseEvent"/> when a mouse
    ///     event occurs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be both enabled and visible to receive mouse events.
    ///     </para>
    ///     <para>
    ///         This method calls <see cref="OnMouseEvent"/> to process the event. If the event is not handled, and one of the
    ///         mouse buttons was clicked, it calls <see cref="OnMouseClick"/> to process the click.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetHighlight"/> and <see cref="DisableHighlight"/> for more information.
    ///     </para>
    ///     <para>
    ///         If <see cref="WantContinuousButtonPressed"/> is <see langword="true"/>, the <see cref="OnMouseClick"/> event
    ///         will be invoked repeatedly while the button is pressed.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/> if the event was handled, <see langword="false"/> otherwise.</returns>
    public bool? NewMouseEvent (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            // A disabled view should not eat mouse events
            return false;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        if (OnMouseEvent (mouseEvent))
        {
            // Technically mouseEvent.Handled should already be true if implementers of OnMouseEvent
            // follow the rules. But we'll update it just in case.
            return mouseEvent.Handled = true;
        }

        if (HighlightStyle != Gui.HighlightStyle.None || WantContinuousButtonPressed)
        {
            if (HandlePressed (mouseEvent))
            {
                return mouseEvent.Handled;
            }

            if (HandleReleased (mouseEvent))
            {
                return mouseEvent.Handled;
            }

            if (HandleClicked (mouseEvent))
            {
                return mouseEvent.Handled;
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button1TripleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2TripleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3TripleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4TripleClicked)
           )
        {
            // If it's a click, and we didn't handle it, then we'll call OnMouseClick
            // We get here if the view did not handle the mouse event via OnMouseEvent/MouseEvent and
            // it did not handle the press/release/clicked events via HandlePress/HandleRelease/HandleClicked
            return OnMouseClick (new (mouseEvent));
        }

        return false;
    }

    /// <summary>
    ///     For cases where the view is grabbed and the mouse is clicked, this method handles the released event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Marked internal just to support unit tests
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    private bool HandlePressed (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Pressed))
        {
            // The first time we get pressed event, grab the mouse and set focus
            if (Application.MouseGrabView != this)
            {
                Application.GrabMouse (this);

                if (CanFocus)
                {
                    // Set the focus, but don't invoke Accept
                    SetFocus ();
                }
            }

            if (Bounds.Contains (mouseEvent.X, mouseEvent.Y))
            {
                SetHighlight (HighlightStyle.HasFlag(HighlightStyle.Pressed) ? HighlightStyle.Pressed : HighlightStyle.None);
            }
            else
            {
                SetHighlight (HighlightStyle.HasFlag (HighlightStyle.PressedOutside) ? HighlightStyle.PressedOutside : HighlightStyle.None);
            }

            if (WantContinuousButtonPressed && Application.MouseGrabView == this)
            {
                // If this is not the first pressed event, click
                return OnMouseClick (new (mouseEvent));
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    /// <summary>
    ///     For cases where the view is grabbed and the mouse is clicked, this method handles the released event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool HandleReleased (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Released))
        {
            if (Application.MouseGrabView == this)
            {
                SetHighlight (HighlightStyle.None);
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    /// <summary>
    ///     For cases where the view is grabbed and the mouse is clicked, this method handles the click event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool HandleClicked (MouseEvent mouseEvent)
    {
        if (Application.MouseGrabView == this
            && (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked)))
        {
            // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
            Application.UngrabMouse ();
            SetHighlight (HighlightStyle.None);

            // If mouse is still in bounds, click
            if (!WantContinuousButtonPressed && Bounds.Contains (mouseEvent.X, mouseEvent.Y))
            {
                return OnMouseClick (new (mouseEvent));
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    [CanBeNull]
    private ColorScheme _savedHighlightColorScheme;

    /// <summary>
    ///     Enables the highlight for the view when the mouse is pressed. Called from OnMouseEvent.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set <see cref="HighlightStyle"/> to have the view highlighted based on the mouse.
    ///     </para>
    ///     <para>
    ///         Calls <see cref="OnHighlight"/> which fires the <see cref="Highlight"/> event.
    ///     </para>
    ///     <para>
    ///         Marked internal just to support unit tests
    ///     </para>
    /// </remarks>
    internal void SetHighlight (HighlightStyle style)
    {
        // Enable override via virtual method and/or event
        if (OnHighlight (style) == true)
        {
            return;
        }

        if (style.HasFlag (HighlightStyle.Pressed) || style.HasFlag (HighlightStyle.PressedOutside))
        {

            if (_savedHighlightColorScheme is null && ColorScheme is { })
            {
                _savedHighlightColorScheme ??= ColorScheme;

                if (CanFocus)
                {
                    // TODO: Make the inverted color configurable
                    var cs = new ColorScheme (ColorScheme)
                    {
                        // For Buttons etc...
                        Focus = new (ColorScheme.Normal.Foreground, ColorScheme.Focus.Background),

                        // For Adornments
                        Normal = new (ColorScheme.Focus.Foreground, ColorScheme.Normal.Background)
                    };
                    ColorScheme = cs;
                }
                else
                {
                    var cs = new ColorScheme (ColorScheme)
                    {
                        // For Buttons etc... that can't focus (like up/down).
                        Normal = new (ColorScheme.Focus.Background, ColorScheme.Normal.Foreground)
                    };
                    ColorScheme = cs;
                }
            }
        }
        else
        {
            // Unhighlight
            if (_savedHighlightColorScheme is { })
            {
                ColorScheme = _savedHighlightColorScheme;
                _savedHighlightColorScheme = null;
            }
        }
    }

    /// <summary>
    ///     Fired when the view is highlighted. Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/>
    ///     to implement a custom highlight scheme or prevent the view from being highlighted.
    /// </summary>
    public event EventHandler<HighlightEventArgs> Highlight;

    /// <summary>
    ///     Called when the view is to be highlighted.
    /// </summary>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool? OnHighlight (HighlightStyle highlight)
    {
        HighlightEventArgs args = new (highlight);
        Highlight?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>Called when a mouse event occurs within the view's <see cref="Bounds"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Bounds"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool OnMouseEvent (MouseEvent mouseEvent)
    {
        var args = new MouseEventEventArgs (mouseEvent);

        MouseEvent?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Event fired when a mouse event occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Bounds"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs> MouseEvent;

    /// <summary>Invokes the MouseClick event.</summary>
    /// <remarks>
    ///     <para>
    ///         Called when the mouse is either clicked or double-clicked. Check
    ///         <see cref="MouseEvent.Flags"/> to see which button was clicked.
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected bool OnMouseClick (MouseEventEventArgs args)
    {
        if (!Enabled)
        {
            // QUESTION: Is this right? Should a disabled view eat mouse clicks?
            args.Handled = true;

            return true;
        }

        MouseClick?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        if (!HasFocus && CanFocus)
        {
            args.Handled = true;
            SetFocus ();
        }

        return args.Handled;
    }

    /// <summary>Event fired when a mouse click occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         Fired when the mouse is either clicked or double-clicked. Check
    ///         <see cref="MouseEvent.Flags"/> to see which button was clicked.
    ///     </para>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Bounds"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs> MouseClick;
}
