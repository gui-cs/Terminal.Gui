namespace Terminal.Gui.Views;

// DoubleClick - Focus, Select (Toggle), and Accept the item under the mouse.
// Click - Focus, Select (Toggle), and do NOT Accept the item under the mouse.
// Not Focused:
//  HotKey - Restore Focus. Do NOT change Active.
//  Item HotKey - Focus item. Select (Toggle) item. Do NOT Accept.
// Focused:
//  Space key - Select (Toggle) focused item. Do NOT Accept.
//  Enter key - Select (Toggle) and Accept the focused item.
//  HotKey - No-op.
//  Item HotKey - Focus item, Select (Toggle), and do NOT Accept.

/// <summary>
///     Provides a user interface for displaying and selecting non-mutually-exclusive flags from a provided dictionary.
///     <see cref="FlagSelector{TFlagsEnum}"/> provides a type-safe version where a `[Flags]` <see langword="enum"/> can be
///     provided.
/// </summary>
public class FlagSelector : SelectorBase, IDesignable
{
    /// <inheritdoc />
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);
        if (view is not CheckBox checkbox)
        {
            return;
        }

        checkbox.RadioStyle = false;

        checkbox.CheckedStateChanging += OnCheckboxOnCheckedStateChanging;
        checkbox.CheckedStateChanged += OnCheckboxOnCheckedStateChanged;
        checkbox.Accepting += Activating;
        checkbox.Accepting += OnCheckboxOnAccepting;
    }

    private void OnCheckboxOnCheckedStateChanging (object? sender, ResultEventArgs<CheckState> args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }

        if (checkbox.CheckedState == CheckState.Checked && (int)checkbox.Data! == 0 && Value == 0)
        {
            args.Handled = true;
        }
    }

    private void OnCheckboxOnCheckedStateChanged (object? sender, EventArgs<CheckState> args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }

        int newValue = Value ?? 0;

        if (checkbox.CheckedState == CheckState.Checked)
        {
            if ((int)checkbox.Data! == default!)
            {
                newValue = 0;
            }
            else
            {
                newValue |= (int)checkbox.Data!;
            }
        }
        else
        {
            newValue &= ~(int)checkbox.Data!;
        }

        Value = newValue;
    }

    private void Activating (object? sender, CommandEventArgs args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }

        if (checkbox.CanFocus)
        {
            // For Select, if the view is focusable and SetFocus succeeds, by defition,
            // the event is handled. So return what SetFocus returns.
            checkbox.SetFocus ();
        }

        // Activating doesn't normally propogate, so we do it here
        if (InvokeCommand (Command.Activate, args.Context) is true)
        {
            // Do not return here; we want to toggle the checkbox state
            args.Handled = true;

            //return;
        }
    }

    private void OnCheckboxOnAccepting (object? sender, CommandEventArgs args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }
        Value = (int)checkbox.Data!;
        args.Handled = false; // Do not set to false; let Accepting propagate
    }

    private int? _value;

    /// <summary>
    /// Gets or sets the value of the selected flags.
    /// </summary>
    public override int? Value
    {
        get => _value;
        set
        {
            if (_updatingChecked || _value == value)
            {
                return;
            }

            int? previousValue = _value;
            _value = value;

            if (_value is null)
            {
                UncheckNone ();
                UncheckAll ();
            }
            else
            {
                UpdateChecked ();
            }

            RaiseValueChanged (previousValue);
        }
    }

    private void UncheckNone ()
    {
        // Uncheck ONLY the None checkbox (Data == 0)
        _updatingChecked = true;
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => (int)sv.Data! == 0))
        {
            cb.CheckedState = CheckState.UnChecked;
        }
        _updatingChecked = false;
    }

    private void UncheckAll ()
    {
        // Uncheck all NON-None checkboxes (Data != 0)
        _updatingChecked = true;
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => (int)(sv.Data ?? default!) != default!))
        {
            cb.CheckedState = CheckState.UnChecked;
        }
        _updatingChecked = false;
    }

    private bool _updatingChecked = false;

    /// <inheritdoc />
    public override void UpdateChecked ()
    {
        if (_updatingChecked)
        {
            return;
        }
        _updatingChecked = true;
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            var flag = (int)(cb.Data ?? throw new InvalidOperationException ("CheckBox.Data must be set"));

            // If this flag is set in Value, check the checkbox. Otherwise, uncheck it.
            if (flag == 0)
            {
                cb.CheckedState = (Value != 0) ? CheckState.UnChecked : CheckState.Checked;
            }
            else
            {
                cb.CheckedState = (Value & flag) == flag ? CheckState.Checked : CheckState.UnChecked;
            }
        }

        _updatingChecked = false;
    }

    /// <inheritdoc />
    protected override void OnCreatingSubViews ()
    {
        // FlagSelector supports a "None" check box; add it
        if (Styles.HasFlag (SelectorStyles.ShowNoneFlag) && Values is { } && !Values.Contains (0))
        {
            Add (CreateCheckBox ("None", 0));
        }
    }

    /// <inheritdoc />
    protected override void OnCreatedSubViews ()
    {
        // If the values include 0, and ShowNoneFlag is not specified, remove the "None" check box
        if (!Styles.HasFlag (SelectorStyles.ShowNoneFlag))
        {
            CheckBox? noneCheckBox = SubViews.OfType<CheckBox> ().FirstOrDefault (cb => (int)cb.Data! == 0);

            if (noneCheckBox is { })
            {
                Remove (noneCheckBox);
                noneCheckBox.Dispose ();
            }
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Styles = SelectorStyles.All;
        AssignHotKeys = true;
        SetValuesAndLabels<SelectorStyles> ();
        Labels = Enum.GetValues<SelectorStyles> ()
                     .Select (
                              l => l switch
                                   {
                                       SelectorStyles.None => "No Style",
                                       SelectorStyles.ShowNoneFlag => "Show None Value Style",
                                       SelectorStyles.ShowValue => "Show Value Editor Style",
                                       SelectorStyles.All => "All Styles",
                                       _ => l.ToString ()
                                   }).ToList ();

        return true;
    }
}
