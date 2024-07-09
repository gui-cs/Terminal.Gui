#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents the state of a <see cref="CheckBox"/>.
/// </summary>
public enum CheckState
{
    None,
    Checked,
    UnChecked
}

/// <summary>Shows a check box that can be toggled.</summary>
public class CheckBox : View
{
    private bool _allowNone;
    private CheckState _checked = CheckState.UnChecked;

    /// <summary>
    ///     Initializes a new instance of <see cref="CheckBox"/>.
    /// </summary>
    public CheckBox ()
    {
        Width = Dim.Auto (DimAutoStyle.Text);
        Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1);

        CanFocus = true;

        // Things this view knows how to do
        AddCommand (Command.Accept, OnToggle);
        AddCommand (Command.HotKey, OnToggle);

        // Default keybindings for this view
        KeyBindings.Add (Key.Space, Command.Accept);

        TitleChanged += Checkbox_TitleChanged;

        HighlightStyle = Gui.HighlightStyle.PressedOutside | Gui.HighlightStyle.Pressed;
        MouseClick += CheckBox_MouseClick;
    }

    private void CheckBox_MouseClick (object? sender, MouseEventEventArgs e)
    {
        e.Handled = OnToggle () == true;
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
    ///     If <see langword="true"/> allows <see cref="State"/> to be <see cref="CheckState.None"/>.
    /// </summary>
    public bool AllowCheckStateNone
    {
        get => _allowNone;
        set
        {
            if (_allowNone == value)
            {
                return;
            }
            _allowNone = value;

            if (State == CheckState.None)
            {
                State = CheckState.UnChecked;
            }
        }
    }

    /// <summary>
    ///     The state of the <see cref="CheckBox"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///        If <see cref="AllowCheckStateNone"/> is <see langword="true"/> and <see cref="CheckState.None"/>, the <see cref="CheckBox"/>
    ///        will display the <c>ConfigurationManager.Glyphs.CheckStateNone</c> character (☒).
    ///     </para>
    ///     <para>
    ///        If <see cref="CheckState.UnChecked"/>, the <see cref="CheckBox"/>
    ///        will display the <c>ConfigurationManager.Glyphs.CheckStateUnChecked</c> character (☐).
    ///     </para>
    ///     <para>
    ///        If <see cref="CheckState.Checked"/>, the <see cref="CheckBox"/>
    ///        will display the <c>ConfigurationManager.Glyphs.CheckStateChecked</c> character (☑).
    ///     </para>
    /// </remarks>
    public CheckState State
    {
        get => _checked;
        set
        {
            if (_checked == value || (value is CheckState.None && !AllowCheckStateNone))
            {
                return;
            }

            _checked = value;
            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <summary>Called when the <see cref="State"/> property changes. Invokes the cancelable <see cref="Toggle"/> event.</summary>
    /// <remarks>
    /// </remarks>
    /// <returns>If <see langword="true"/> the <see cref="Toggle"/> event was canceled.</returns>
    /// <remarks>
    ///     Toggling cycles through the states <see cref="CheckState.None"/>, <see cref="CheckState.Checked"/>, and <see cref="CheckState.UnChecked"/>.
    /// </remarks>
    public bool? OnToggle ()
    {
        CheckState oldValue = State;
        CancelEventArgs<CheckState> e = new (ref _checked, ref oldValue);

        switch (State)
        {
            case CheckState.None:
                e.NewValue = CheckState.Checked;

                break;
            case CheckState.Checked:
                e.NewValue = CheckState.UnChecked;

                break;
            case CheckState.UnChecked:
                if (AllowCheckStateNone)
                {
                    e.NewValue = CheckState.None;
                }
                else
                {
                    e.NewValue = CheckState.Checked;
                }

                break;
        }

        Toggle?.Invoke (this, e);
        if (e.Cancel)
        {
            return e.Cancel;
        }

        // By default, Command.Accept calls OnAccept, so we need to call it here to ensure that the event is fired.
        if (OnAccept () == true)
        {
            return true;
        }

        State = e.NewValue;

        return true;
    }

    /// <summary>Toggle event, raised when the <see cref="CheckBox"/> is toggled.</summary>
    /// <remarks>
    /// <para>
    ///    This event can be cancelled. If cancelled, the <see cref="CheckBox"/> will not change its state.
    /// </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs<CheckState>>? Toggle;

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        switch (TextAlignment)
        {
            case Alignment.Start:
            case Alignment.Center:
            case Alignment.Fill:
                TextFormatter.Text = $"{GetCheckedGlyph ()} {Text}";

                break;
            case Alignment.End:
                TextFormatter.Text = $"{Text} {GetCheckedGlyph ()}";

                break;
        }
    }

    private Rune GetCheckedGlyph ()
    {
        return State switch
        {
            CheckState.Checked => Glyphs.CheckStateChecked,
            CheckState.UnChecked => Glyphs.CheckStateUnChecked,
            CheckState.None => Glyphs.CheckStateNone,
            _ => throw new ArgumentOutOfRangeException ()
        };
    }
}
