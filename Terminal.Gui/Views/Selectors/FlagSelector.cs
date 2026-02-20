namespace Terminal.Gui.Views;

// DoubleClick - Focus, Activate, and Accept the item under the mouse (CanFocus or not)
// Click - Focus, Activate, and do NOT Accept the item under the mouse (CanFocus or not).
// Not Focused:
//  HotKey - Restore Focus. Do NOT change Active.
//  Item HotKey - Focus item. Activate (Toggle) item. Do NOT Accept.
// Focused:
//  Space key - Activate (Toggle) focused item. Do NOT Accept.
//  Enter key - Activate (Toggle) and Accept the focused item.
//  HotKey - No-op.
//  Item HotKey - Focus item, Activate (Toggle), and do NOT Accept.

/// <summary>
///     Provides a user interface for displaying and selecting non-mutually-exclusive flags from a provided dictionary.
///     <see cref="FlagSelector{TFlagsEnum}"/> provides a type-safe version where a `[Flags]` <see langword="enum"/> can be
///     provided.
/// </summary>
public class FlagSelector : SelectorBase, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagSelector"/> class.
    /// </summary>
    public FlagSelector ()
    {
        KeyBindings.Remove (Key.Space);
        KeyBindings.Remove (Key.Enter);
    }

    /// <summary>
    ///     Returns the dispatch target for composite command handling.
    ///     Only dispatches for Activate commands — Accept should bubble normally.
    /// </summary>
    protected override View? GetDispatchTarget (ICommandContext? ctx)
    {
        // Only dispatch Activate, not Accept. Accept should bubble to Menu/MenuBar normally.
        if (ctx?.Command != Command.Activate)
        {
            return null;
        }

        // When a CheckBox's activation bubbles up, the source IS the CheckBox.
        if (ctx.Source?.TryGetTarget (out View? source) == true && source is CheckBox cb)
        {
            return source;
        }

        // Suppress dispatch when the HotKey flag is set (HotKey → SetFocus only, no toggle).
        if (_suppressHotKeyActivate)
        {
            _suppressHotKeyActivate = false;

            return null;
        }

        return Focused;
    }

    /// <summary>
    ///     Consumes: FlagSelector owns toggle semantics.
    /// </summary>
    protected override bool ConsumeDispatch => true;

    // Set by OnHandlingHotKey to suppress the Activate that DefaultHotKeyHandler
    // fires after RaiseHandlingHotKey. Checked and cleared in GetDispatchTarget.
    private bool _suppressHotKeyActivate;

    /// <inheritdoc/>
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        if (base.OnHandlingHotKey (args))
        {
            return true;
        }

        // When focused, HotKey is a no-op
        if (HasFocus)
        {
            return true;
        }

        // Not focused: restore focus, suppress the Activate dispatch that DefaultHotKeyHandler will invoke.
        _suppressHotKeyActivate = true;

        if (CanFocus)
        {
            SetFocus ();
        }

        // Return false so HandlingHotKey event fires (required by AllViews contract)
        return false;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Toggle only when the source is a CheckBox (IsBubblingUp path where consume prevented
        // CheckBox.AdvanceCheckState). For programmatic invocations, BubbleDown already activated
        // the focused CheckBox and AdvanceCheckState ran, so no additional toggle is needed.
        if (ctx?.Source?.TryGetTarget (out View? source) != true || source is not CheckBox checkBox)
        {
            return;
        }

        if (ctx.Binding is { } && ctx.Binding.Commands.Contains (Command.Accept))
        {
            // If the binding was via Accept, don't change the state
            return;
        }

        checkBox.Value = checkBox.Value == CheckState.Checked
                             ? CheckState.UnChecked
                             : CheckState.Checked;

        // CheckboxOnValueChanged handler updates FlagSelector.Value bitmask
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is not CheckBox checkbox)
        {
            return;
        }

        checkbox.RadioStyle = false;

        checkbox.ValueChanging += OnCheckboxOnValueChanging;
        checkbox.ValueChanged += CheckboxOnValueChanged;
    }

    private void OnCheckboxOnValueChanging (object? sender, ValueChangingEventArgs<CheckState> args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }

        //Logging.Debug ($"{this.ToIdentifyingString ()} ({args.CurrentValue}->{args.NewValue})");

        if (checkbox.Value == CheckState.Checked && (int)checkbox.Data! == 0 && Value == 0)
        {
            // None flag was already checked; prevent changing again
            args.Handled = true;
        }
    }

    private void CheckboxOnValueChanged (object? sender, ValueChangedEventArgs<CheckState> args)
    {
        if (sender is not CheckBox checkBox)
        {
            return;
        }

        //Logging.Debug ($"{this.ToIdentifyingString ()} ({args.OldValue}->{args.NewValue})");

        int newValue = Value ?? 0;

        if (checkBox.Value == CheckState.Checked)
        {
            if ((int)checkBox.Data! == 0)
            {
                newValue = 0;
            }
            else
            {
                newValue |= (int)checkBox.Data!;
            }
        }
        else
        {
            newValue &= ~(int)checkBox.Data!;
        }

        Value = newValue;
    }

    private bool _updatingChecked;

    /// <summary>
    ///     Gets or sets the value of the selected flags.
    /// </summary>
    public override int? Value
    {
        get;
        set
        {
            if (_updatingChecked || field == value)
            {
                return;
            }

            int? previousValue = field;

            // Raise ValueChanging (cancellable) - use base class implementation
            if (RaiseValueChanging (previousValue, value))
            {
                return;
            }

            field = value;

            if (field is null)
            {
                UncheckNone ();
                UncheckAll ();
            }
            else
            {
                UpdateChecked ();
            }

            RaiseValueChanged (previousValue, field);
        }
    }

    private void UncheckNone ()
    {
        _updatingChecked = true;

        // Uncheck ONLY the None checkbox (Data == 0)

        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => (int)sv.Data! == 0))
        {
            cb.Value = CheckState.UnChecked;
        }
        _updatingChecked = false;
    }

    private void UncheckAll ()
    {
        _updatingChecked = true;

        // Uncheck all NON-None checkboxes (Data != 0)

        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => (int)(sv.Data ?? null!) != 0))
        {
            cb.Value = CheckState.UnChecked;
        }
        _updatingChecked = false;
    }

    /// <inheritdoc/>
    public override void UpdateChecked ()
    {
        _updatingChecked = true;

        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            var flag = (int)(cb.Data ?? throw new InvalidOperationException ("CheckBox.Data must be set"));

            // If this flag is set in Value, check the checkbox. Otherwise, uncheck it.
            if (flag == 0)
            {
                cb.Value = Value != 0 ? CheckState.UnChecked : CheckState.Checked;
            }
            else
            {
                cb.Value = (Value & flag) == flag ? CheckState.Checked : CheckState.UnChecked;
            }
        }

        _updatingChecked = false;
    }

    /// <inheritdoc/>
    public override void CreateSubViews ()
    {
        base.CreateSubViews ();

        var changed = false;

        // FlagSelector supports a "None" check box; add it
        if (Styles.HasFlag (SelectorStyles.ShowNoneFlag) && Values is { } && !Values.Contains (0))
        {
            Add (CreateCheckBox ("None", 0));
            changed = true;
        }

        // If the values include 0 and ShowNoneFlag is not specified, remove the zero-value check box
        if (!Styles.HasFlag (SelectorStyles.ShowNoneFlag))
        {
            CheckBox? noneCheckBox = SubViews.OfType<CheckBox> ().FirstOrDefault (cb => (int)cb.Data! == 0);

            if (noneCheckBox is { })
            {
                Remove (noneCheckBox);
                noneCheckBox.Dispose ();
                changed = true;
            }
        }

        if (changed)
        {
            SetLayout ();
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Styles = SelectorStyles.All;
        AssignHotKeys = true;
        SetValuesAndLabels<SelectorStyles> ();

        Labels = Enum.GetValues<SelectorStyles> ()
                     .Select (l => l switch
                                   {
                                       SelectorStyles.None => "No Style",
                                       SelectorStyles.ShowNoneFlag => "Show None Value Style",
                                       SelectorStyles.ShowValue => "Show Value Editor Style",
                                       SelectorStyles.All => "All Styles",
                                       _ => l.ToString ()
                                   })
                     .ToList ();

        return true;
    }
}
