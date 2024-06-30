#nullable enable
namespace Terminal.Gui;

/// <summary>The <see cref="CheckBox"/> <see cref="View"/> shows an on/off toggle that the user can set</summary>
public class CheckBox : View
{
    private readonly Rune _charChecked;
    private readonly Rune _charNullChecked;
    private readonly Rune _charUnChecked;
    private bool _allowNullChecked;
    private bool? _checked = false;

    /// <summary>
    ///     Initializes a new instance of <see cref="CheckBox"/>.
    /// </summary>
    public CheckBox ()
    {
        _charNullChecked = Glyphs.NullChecked;
        _charChecked = Glyphs.Checked;
        _charUnChecked = Glyphs.UnChecked;

        Width = Dim.Auto (DimAutoStyle.Text);
        Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1);

        CanFocus = true;

        // Things this view knows how to do
        AddCommand (Command.Accept, OnToggled);
        AddCommand (Command.HotKey, OnToggled);

        // Default keybindings for this view
        KeyBindings.Add (Key.Space, Command.Accept);

        TitleChanged += Checkbox_TitleChanged;

        HighlightStyle = Gui.HighlightStyle.PressedOutside | Gui.HighlightStyle.Pressed;
        MouseClick += CheckBox_MouseClick;
    }

    private void CheckBox_MouseClick (object? sender, MouseEventEventArgs e)
    {
        e.Handled = OnToggled () == true;
    }

    private void Checkbox_TitleChanged (object? sender, EventArgs<string> e)
    {
        base.Text = e.CurrentValue;
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

    /// <summary>
    ///     If <see langword="true"/> allows <see cref="Checked"/> to be null, true, or false. If <see langword="false"/>
    ///     only allows <see cref="Checked"/> to be true or false.
    /// </summary>
    public bool AllowNullChecked
    {
        get => _allowNullChecked;
        set
        {
            _allowNullChecked = value;
            Checked ??= false;
        }
    }

    /// <summary>
    ///     The state of the <see cref="CheckBox"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///        If <see langword="null"/> and <see cref="AllowNullChecked"/> is <see langword="true"/>, the <see cref="CheckBox"/>
    ///        will display the <c>ConfigurationManager.Glyphs.NullChecked</c> character (☒).
    ///     </para>
    ///     <para>
    ///        If <see langword="false"/>, the <see cref="CheckBox"/>
    ///        will display the <c>ConfigurationManager.Glyphs.UnChecked</c> character (☐).
    ///     </para>
    ///     <para>
    ///        If <see langword="false"/>, the <see cref="CheckBox"/>
    ///        will display the <c>ConfigurationManager.Glyphs.Checked</c> character (☑).
    ///     </para>
    /// </remarks>
    public bool? Checked
    {
        get => _checked;
        set
        {
            if (value is null && !AllowNullChecked)
            {
                return;
            }

            _checked = value;
            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <summary>Called when the <see cref="Checked"/> property changes. Invokes the <see cref="Toggled"/> event.</summary>
    /// <remarks>
    /// </remarks>
    /// <returns>If <see langword="true"/> the <see cref="Toggled"/> event was canceled.</returns>
    public bool? OnToggled ()
    {
        bool ? oldValue = Checked;
        CancelEventArgs<bool?> e = new (ref _checked, ref oldValue);

        if (AllowNullChecked)
        {
            switch (Checked)
            {
                case null:
                    e.NewValue = true;

                    break;
                case true:
                    e.NewValue = false;

                    break;
                case false:
                    e.NewValue = null;

                    break;
            }
        }
        else
        {
            e.NewValue = !Checked;
        }

        Toggled?.Invoke (this, e);
        if (e.Cancel)
        {
            return e.Cancel;
        }

        // By default, Command.Accept calls OnAccept, so we need to call it here to ensure that the event is fired.
        if (OnAccept () == true)
        {
            return true;
        }

        Checked = e.NewValue;

        return true;
    }

    /// <summary>Toggled event, raised when the <see cref="CheckBox"/> is toggled.</summary>
    /// <remarks>
    /// <para>
    ///    This event can be cancelled. If cancelled, the <see cref="CheckBox"/> will not change its state.
    /// </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs<bool?>>? Toggled;

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        switch (TextAlignment)
        {
            case Alignment.Start:
            case Alignment.Center:
            case Alignment.Fill:
                TextFormatter.Text = $"{GetCheckedState ()} {Text}";

                break;
            case Alignment.End:
                TextFormatter.Text = $"{Text} {GetCheckedState ()}";

                break;
        }
    }

    private Rune GetCheckedState ()
    {
        return Checked switch
        {
            true => _charChecked,
            false => _charUnChecked,
            var _ => _charNullChecked
        };
    }
}
