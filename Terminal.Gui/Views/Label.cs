using System.Reflection.Metadata.Ecma335;

namespace Terminal.Gui;

/// <summary>
///     The Label <see cref="View"/> displays a string at a given position and supports multiple lines separated by
///     newline characters. Multi-line Labels support word wrap.
/// </summary>
/// <remarks>
///     The <see cref="Label"/> view is functionality identical to <see cref="View"/> and is included for API
///     backwards compatibility.
/// </remarks>
public class Label : View
{
    /// <inheritdoc/>
    public Label ()
    {
        Height = 1;
        AutoSize = true;

        // Things this view knows how to do
        AddCommand (Command.Default, FocusNext);
        AddCommand (Command.Accept, AcceptKey);

        // Default key bindings for this view
        KeyBindings.Add (Key.Space, Command.Accept);

        TitleChanged += Label_TitleChanged;
        //TextChanged += Label_TextChanged;
    }

    private void Label_TitleChanged (object sender, StringEventArgs e)
    {
        base.Text = e.New;
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc />
    public override string Text
    {
        get => base.Title;
        set => base.Text = base.Title = value;
    }

    /// <inheritdoc />
    public override Rune HotKeySpecifier
    {
        get => base.HotKeySpecifier;
        set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value;
    }

    private new bool? FocusNext ()
    {
        var me = SuperView?.Subviews.IndexOf (this) ?? -1;
        if (me != -1 && me < SuperView?.Subviews.Count - 1)
        {
            SuperView?.Subviews [me + 1].SetFocus ();
        }

        return true;
    }

    /// <summary>
    ///     The event fired when the user clicks the primary mouse button within the Bounds of this <see cref="View"/> or
    ///     if the user presses the action key while this view is focused. (TODO: IsDefault)
    /// </summary>
    /// <remarks>
    ///     Client code can hook up to this event, it is raised when the button is activated either with the mouse or the
    ///     keyboard.
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
            if (!CanFocus)
            {
                FocusNext ();
            }

            if (!HasFocus && SuperView is { })
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

    private bool? AcceptKey ()
    {
        if (!HasFocus)
        {
            SetFocus ();
        }

        OnClicked ();

        return true;
    }
}
