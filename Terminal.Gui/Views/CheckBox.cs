#nullable enable
namespace Terminal.Gui;

/// <summary>Shows a check box that can be cycled between three states.</summary>
public class CheckBox : View
{
    /// <summary>
    /// Gets or sets the default Highlight Style.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static HighlightStyle DefaultHighlightStyle { get; set; } = HighlightStyle.PressedOutside | HighlightStyle.Pressed | HighlightStyle.Hover;

    /// <summary>
    ///     Initializes a new instance of <see cref="CheckBox"/>.
    /// </summary>
    public CheckBox ()
    {
        Width = Dim.Auto (DimAutoStyle.Text);
        Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1);

        CanFocus = true;

        // Things this view knows how to do

        AddCommand (Command.Accept, OnAccept);
        AddCommand (Command.HotKey, () =>
                                    {
                                        //AdvanceCheckState ();

                                        return OnAccept ();
                                    });
        AddCommand (Command.Select, () =>
                                    {
                                        OnSelect (); 
                                        AdvanceCheckState ();

                                        return false;
                                    });

        // Default keybindings for this view
        KeyBindings.Add (Key.Space, Command.Select);
        KeyBindings.Add (Key.Enter, Command.Accept);

        TitleChanged += Checkbox_TitleChanged;

        HighlightStyle = DefaultHighlightStyle;
        MouseClick += CheckBox_MouseClick;
    }

    private void CheckBox_MouseClick (object? sender, MouseEventEventArgs e)
    {
        //e.Handled = AdvanceCheckState () == true;

        //if (CanFocus)
        {
            e.Handled = InvokeCommand (Command.Select) == true;
        }
        //else
        //{
        //    e.Handled = InvokeCommand (Command.Accept) == true;
        //}

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

    private bool _allowNone = false;

    /// <summary>
    ///     If <see langword="true"/> allows <see cref="CheckedState"/> to be <see cref="CheckState.None"/>. The default is <see langword="false"/>.
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

            if (CheckedState == CheckState.None)
            {
                CheckedState = CheckState.UnChecked;
            }
        }
    }

    private CheckState _checkedState = CheckState.UnChecked;

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
    public CheckState CheckedState
    {
        get => _checkedState;
        set
        {
            if (_checkedState == value || (value is CheckState.None && !AllowCheckStateNone))
            {
                return;
            }

            _checkedState = value;
            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <summary>
    ///     Advances <see cref="CheckedState"/> to the next value. Invokes the cancelable <see cref="CheckedStateChanging"/> event.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <returns>If <see langword="true"/> the <see cref="CheckedStateChanging"/> event was canceled.</returns>
    /// <remarks>
    /// <para>
    ///     Cycles through the states <see cref="CheckState.None"/>, <see cref="CheckState.Checked"/>, and <see cref="CheckState.UnChecked"/>.
    /// </para>
    /// <para>
    ///     If the <see cref="CheckedStateChanging"/> event is not canceled, the <see cref="CheckedState"/> will be updated and the <see cref="Command.Accept"/> event will be raised.
    /// </para>
    /// </remarks>
    public bool? AdvanceCheckState ()
    {
        CheckState oldValue = CheckedState;
        CancelEventArgs<CheckState> e = new (in _checkedState, ref oldValue);

        switch (CheckedState)
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

        CheckedStateChanging?.Invoke (this, e);
        if (e.Cancel)
        {
            return e.Cancel;
        }

        SetFocus ();

        CheckedState = e.NewValue;

        return true;
    }

    /// <summary>Raised when the <see cref="CheckBox"/> state is changing.</summary>
    /// <remarks>
    /// <para>
    ///    This event can be cancelled. If cancelled, the <see cref="CheckBox"/> will not change its state.
    /// </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs<CheckState>>? CheckedStateChanging;

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        base.UpdateTextFormatterText ();
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
        return CheckedState switch
        {
            CheckState.Checked => Glyphs.CheckStateChecked,
            CheckState.UnChecked => Glyphs.CheckStateUnChecked,
            CheckState.None => Glyphs.CheckStateNone,
            _ => throw new ArgumentOutOfRangeException ()
        };
    }
}
