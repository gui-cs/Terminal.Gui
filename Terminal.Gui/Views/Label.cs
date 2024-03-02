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
        AddCommand (Command.HotKey, FocusNext);

        // Default key bindings for this view
        KeyBindings.Add (Key.Space, Command.Accept);

        TitleChanged += Label_TitleChanged;
        MouseClick += Label_MouseClick;
    }

    private void Label_MouseClick (object sender, MouseEventEventArgs e)
    {
        e.Handled = InvokeCommand (Command.Accept) == true;
    }

    private void Label_TitleChanged (object sender, StateEventArgs<string> e)
    {
        base.Text = e.NewValue;
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

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);
        return base.OnEnter (view);
    }
}
