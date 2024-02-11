namespace Terminal.Gui;

/// <summary>
///     The Label <see cref="View"/> displays a string at a given position and supports multiple lines separated by newline characters. Multi-line Labels support word wrap.
/// </summary>
/// <remarks>
///     The <see cref="Label"/> view is functionality identical to <see cref="View"/> and is included for API backwards compatibility.
/// </remarks>
public class Label : View
{
    /// <inheritdoc/>
    public Label ()
    {
        Height = 1;
        AutoSize = true;

        // Things this view knows how to do
        AddCommand (
                    Command.Default,
                    () =>
                    {
                        // BUGBUG: This is a hack, but it does work.
                        bool can = CanFocus;
                        CanFocus = true;
                        SetFocus ();
                        SuperView.FocusNext ();
                        CanFocus = can;

                        return true;
                    }
                   );
        AddCommand (Command.Accept, () => AcceptKey ());

        // Default key bindings for this view
        KeyBindings.Add (KeyCode.Space, Command.Accept);
    }

    /// <summary>
    ///     The event fired when the user clicks the primary mouse button within the Bounds of this <see cref="View"/> or if the user presses the action key while this view is focused. (TODO: IsDefault)
    /// </summary>
    /// <remarks>
    ///     Client code can hook up to this event, it is raised when the button is activated either with the mouse or the keyboard.
    /// </remarks>
    public event EventHandler Clicked;

    /// <summary>Virtual method to invoke the <see cref="Clicked"/> event.</summary>
    public virtual void OnClicked () { Clicked?.Invoke (this, EventArgs.Empty); }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <summary>Method invoked when a mouse event is generated</summary>
    /// <param name="mouseEvent"></param>
    /// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
    public override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        var args = new MouseEventEventArgs (mouseEvent);

        if (OnMouseClick (args))
        {
            return true;
        }

        if (MouseEvent (mouseEvent))
        {
            return true;
        }

        if (mouseEvent.Flags == MouseFlags.Button1Clicked)
        {
            if (!HasFocus && SuperView != null)
            {
                if (!SuperView.HasFocus)
                {
                    SuperView.SetFocus ();
                }

                SetFocus ();
                SetNeedsDisplay ();
            }

            OnClicked ();

            return true;
        }

        return false;
    }

    private bool AcceptKey ()
    {
        if (!HasFocus)
        {
            SetFocus ();
        }

        OnClicked ();

        return true;
    }
}
