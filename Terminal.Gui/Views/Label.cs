namespace Terminal.Gui;

/// <summary>
///     The Label <see cref="View"/> displays text that describes the View next in the <see cref="View.Subviews"/>. When
///     Label
///     recieves a <see cref="Command.HotKey"/> command it will pass it to the next <see cref="View"/> in
///     <see cref="View.Subviews"/>.
/// </summary>
/// <remarks>
///     <para>
///         Title and Text are the same property. When Title is set Text s also set. When Text is set Title is also set.
///     </para>
///     <para>
///         If <see cref="View.CanFocus"/> is <see langword="false"/> and the use clicks on the Label,
///         the <see cref="Command.HotKey"/> will be invoked on the next <see cref="View"/> in
///         <see cref="View.Subviews"/>."
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
        AddCommand (Command.HotKey, InvokeHotKeyOnNext);

        TitleChanged += Label_TitleChanged;
        MouseClick += Label_MouseClick;
    }

    // TODO: base raises Select, but we want to raise HotKey. This can be simplified?
    private void Label_MouseClick (object sender, MouseEventArgs e)
    {
        if (!CanFocus)
        {
            e.Handled = InvokeCommand (Command.HotKey, ctx: new (Command.HotKey, key: null, data: this)) == true;
        }
    }

    private void Label_TitleChanged (object sender, EventArgs<string> e)
    {
        base.Text = e.CurrentValue;
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

    private bool? InvokeHotKeyOnNext (CommandContext context)
    {
        if (RaiseHandlingHotKey () == true)
        {
            return true;
        }

        if (CanFocus)
        {
            SetFocus ();

            return true;
        }

        int me = SuperView?.Subviews.IndexOf (this) ?? -1;

        if (me != -1 && me < SuperView?.Subviews.Count - 1)
        {
            return SuperView?.Subviews [me + 1].InvokeCommand (Command.HotKey, context.Key, context.KeyBinding) == true;
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
