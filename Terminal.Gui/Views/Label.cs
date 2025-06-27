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

        // On HotKey, pass it to the next view
        AddCommand (Command.HotKey, RaiseHotKeyOnNextPeer);

        // On click, invoke the HotKey command on the next view
        MouseBindings.ReplaceCommands (MouseFlags.Button1Clicked, Command.HotKey);

        TitleChanged += Label_TitleChanged;
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

    private bool? RaiseHotKeyOnNextPeer (ICommandContext commandContext)
    {
        if (RaiseHandlingHotKey (commandContext) == true)
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
            // If the Label has a hotkey, we need to find the next peer-view and pass the
            // command on to it.
            int me = SuperView?.SubViews.IndexOf (this) ?? -1;

            if (me != -1 && me < SuperView?.SubViews.Count - 1)
            {
                View? nextPeer = SuperView?.SubViews.ElementAt (me + 1);
                if (nextPeer is null)
                {
                    return false;
                }

                // Swap out the key to the HotKey of the target view
                CommandContext<KeyBinding>? keyCommandContext = new CommandContext<KeyBinding> ()
                {
                    Command = Command.HotKey,
                    Binding = new ([Command.HotKey], nextPeer, data: null),
                };
                return nextPeer.InvokeCommand (Command.HotKey, keyCommandContext) == true;
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
