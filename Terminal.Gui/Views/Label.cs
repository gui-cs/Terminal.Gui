namespace Terminal.Gui;

/// <summary>
///     The Label <see cref="View"/> displays text that describes the View next in the <see cref="View.Subviews"/>. When
///     Label
///     recieves a <see cref="Command.HotKey"/> command it will pass it to the next <see cref="View"/> in
///     <see cref="View.Subviews"/>.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Label.Title"/> and <see cref="Label.Text"/> are the same property. When <see cref="Label.Title"/> is
///         set
///         <see cref="Label.Text"/> is also set. When <see cref="Label.Text"/> is set <see cref="Label.Title"/> is also
///         set.
///     </para>
///     <para>
///         If <see cref="Label.CanFocus"/> is <see langword="false"/> and the use clicks on the Label,
///         the <see cref="Command.HotKey"/> will be invoked on the next <see cref="View"/> in
///         <see cref="View.Subviews"/>."
///     </para>
/// </remarks>
public class Label : View
{
    /// <inheritdoc/>
    public Label ()
    {
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        // Things this view knows how to do
        AddCommand (Command.HotKey, InvokeHotKeyOnNext);

        TitleChanged += Label_TitleChanged;
        MouseClick += Label_MouseClick;
    }

    private void Label_MouseClick (object sender, MouseEventEventArgs e)
    {
        if (!CanFocus)
        {
            e.Handled = InvokeCommand (Command.HotKey) == true;
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
        int me = SuperView?.Subviews.IndexOf (this) ?? -1;

        if (me != -1 && me < SuperView?.Subviews.Count - 1)
        {
            SuperView?.Subviews [me + 1].InvokeCommand (Command.HotKey, context.Key, context.KeyBinding);
        }

        return true;
    }
}
