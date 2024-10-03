﻿#nullable enable
using System.Reflection.Metadata;

namespace Terminal.Gui;

/// <summary>Shows a check box that can be cycled between two or three states.</summary>
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

        // Select (Space key and single-click) - Advance state and raise Select event
        AddCommand (Command.Select, AdvanceCheckState);

        // Accept (Enter key and double-click) - Raise Accept event - DO NOT advance state
        AddCommand (Command.Accept, () =>  RaiseAcceptEvent());

        // Hotkey - Advance state and raise Select event - DO NOT raise Accept
        AddCommand (Command.HotKey, AdvanceCheckState);

        TitleChanged += Checkbox_TitleChanged;

        HighlightStyle = DefaultHighlightStyle;
        MouseClick += CheckBox_MouseClick;
    }

    private void CheckBox_MouseClick (object? sender, MouseEventEventArgs e)
    {
#if CHECKBOX_SUPPORTS_DOUBLE_CLICK_ACCEPT
        CheckState savedCheckState = CheckedState;
#endif

        if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            AdvanceCheckState ();
            ;
 //           e.Handled = AdvanceCheckState () == true;
        }

#if CHECKBOX_SUPPORTS_DOUBLE_CLICK_ACCEPT
        if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1DoubleClicked))
        {
            if (RaiseAcceptEvent () == true)
            {
                e.Handled = false;
                _checkedState = savedCheckState;
            }

            // TODO: This needs to be made consistent with how Button.IsDefault works
            if (SuperView is { })
            {
                // TODO: This should pass context
                SuperView.InvokeCommand (Command.Accept);
            }
        }
#endif
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
        set => ChangeCheckedState (value);
    }

    /// <summary>
    ///     INTERNAL Sets CheckedState.
    /// </summary>
    /// <param name="value"></param>
    /// <returns><see langword="true"/> if state change was canceled, <see langword="false"/> if the state changed, and <see langword="null"/> if the state was not changed for some other reason.</returns>
    private bool? ChangeCheckedState (CheckState value)
    {
        if (_checkedState == value || (value is CheckState.None && !AllowCheckStateNone))
        {
            return null;
        }

        CancelEventArgs<CheckState> e = new (in _checkedState, ref value);

        if (OnCheckedStateChanging (e))
        {
            return true;
        }

        CheckedStateChanging?.Invoke (this, e);
        if (e.Cancel)
        {
            return e.Cancel;
        }

        _checkedState = value;
        UpdateTextFormatterText ();
        OnResizeNeeded ();

        EventArgs<CheckState> args = new (in _checkedState);
        OnCheckedStateChanged (args);

        CheckedStateChanged?.Invoke (this, args);

        if (RaiseSelectEvent () == true)
        {
            return true;
        }

        return false;
    }

    /// <summary>Called when the <see cref="CheckBox"/> state is changing.</summary>
    /// <remarks>
    /// <para>
    ///    The state cahnge can be cancelled by setting the args.Cancel to <see langword="true"/>.
    /// </para>
    /// </remarks>
    protected virtual bool OnCheckedStateChanging (CancelEventArgs<CheckState> args) { return false;}

    /// <summary>Raised when the <see cref="CheckBox"/> state is changing.</summary>
    /// <remarks>
    /// <para>
    ///    This event can be cancelled. If cancelled, the <see cref="CheckBox"/> will not change its state.
    /// </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs<CheckState>>? CheckedStateChanging;

    /// <summary>Called when the <see cref="CheckBox"/> state has changed.</summary>
    protected virtual void OnCheckedStateChanged (EventArgs<CheckState> args) { }

    /// <summary>Raised when the <see cref="CheckBox"/> state has changed.</summary>
    public event EventHandler<EventArgs<CheckState>>? CheckedStateChanged;

    /// <summary>
    ///     Advances <see cref="CheckedState"/> to the next value. Invokes the cancelable <see cref="CheckedStateChanging"/> event.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Cycles through the states <see cref="CheckState.None"/>, <see cref="CheckState.Checked"/>, and <see cref="CheckState.UnChecked"/>.
    /// </para>
    /// <para>
    ///     If the <see cref="CheckedStateChanging"/> event is not canceled, the <see cref="CheckedState"/> will be updated and the <see cref="Command.Accept"/> event will be raised.
    /// </para>
    /// </remarks>
    /// <returns><see langword="true"/> if state change was canceled, <see langword="false"/> if the state changed, and <see langword="null"/> if the state was not changed for some other reason.</returns>
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

        bool? cancelled = ChangeCheckedState (e.NewValue);

        return cancelled;
    }

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
