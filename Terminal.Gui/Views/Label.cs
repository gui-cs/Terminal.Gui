namespace Terminal.Gui.Views;

/// <summary>
///     Displays text that describes the View next in the <see cref="View.SubViews"/>. When
///     the user presses a hotkey that matches the <see cref="View.HotKey"/> of the Label, the next <see cref="View"/> in
///     <see cref="View.SubViews"/> will be activated.
/// </summary>
/// <remarks>
///     <para>
///         Title and Text are the same property. When Title is set Text s also set. When Text is set Title is also set.
///     </para>
///     <para>
///         If <see cref="View.CanFocus"/> is <see langword="false"/> and the use clicks on the Label,
///         the <see cref="Command.HotKey"/> will be invoked on the next <see cref="View"/> in
///         <see cref="View.SubViews"/>.
///     </para>
/// </remarks>
public class Label : View, IDesignable
{
    /// <inheritdoc/>
    public Label ()
    {
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        // On HoKey, pass it to the next view
        AddCommand (Command.HotKey, InvokeHotKeyOnNextPeer);

        TitleChanged += Label_TitleChanged;
        MouseClick += Label_MouseClick;
    }

    private void Label_MouseClick (object sender, MouseEventArgs e)
    {
        if (!CanFocus)
        {
            e.Handled = InvokeCommand<KeyBinding> (Command.HotKey, new ([Command.HotKey], this, this)) == true;
        }
    }

    private void Label_TitleChanged (object sender, EventArgs<string> e)
    {
        base.Text = e.Value;
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc/>
    public override string Text
    {
        get => Title;
        set => base.Text = Title = value;
    }

    /// <inheritdoc/>
    public override Rune HotKeySpecifier
    {
        get => base.HotKeySpecifier;
        set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value;
    }

    private bool? InvokeHotKeyOnNextPeer (ICommandContext commandContext)
    {
        if (RaiseHandlingHotKey () == true)
        {
            return true;
        }

        if (CanFocus)
        {
            SetFocus ();

            // Always return true on hotkey, even if SetFocus fails because
            // hotkeys are always handled by the View (unless RaiseHandlingHotKey cancels).
            // This is the same behavior as the base (View).
            return true;
        }

        if (HotKey.IsValid)
        {
            // If the Label has a hotkey, we need to find the next view in the subview list
            int me = SuperView?.SubViews.IndexOf (this) ?? -1;

            if (me != -1 && me < SuperView?.SubViews.Count - 1)
            {
                return SuperView?.SubViews.ElementAt (me + 1).InvokeCommand (Command.HotKey) == true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Text = "_Label";

        return true;
    }
}
