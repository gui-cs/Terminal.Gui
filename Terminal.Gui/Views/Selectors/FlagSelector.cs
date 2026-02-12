using System.Diagnostics;

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
        //// Per spec: HotKey restores focus when not focused but does NOT change Active.
        //// When already focused, HotKey is a no-op.
        //// Replace DefaultHotKeyHandler so the HandlingHotKey event fires
        //// but Activate is NOT invoked (no flag toggle).
        //AddCommand (Command.HotKey,
        //            ctx =>
        //            {
        //                if (RaiseHandlingHotKey (ctx) is true)
        //                {
        //                    return true;
        //                }

        //                if (CanFocus && !HasFocus)
        //                {
        //                    SetFocus ();
        //                }

        //                // Do NOT invoke Activate - HotKey should not toggle flags.
        //                return true;
        //            });
    }


    /// <inheritdoc />
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        if (ctx?.Source?.TryGetTarget (out View? sourceView) is not true || sourceView is not CheckBox checkBox)
        {
            return;
        }

        if (ctx.Binding is KeyBinding { Key: { } } keyBinding && keyBinding.Key == Key.Space)
        {

        }


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
        //checkbox.Activating += OnCheckboxOnActivating;
    }

    //private void OnCheckboxOnActivating (object? sender, CommandEventArgs args)
    //{
    //    if (sender is not CheckBox checkbox)
    //    {
    //        return;
    //    }
    //    Logging.Debug ($"{this.ToIdentifyingString ()} ({args.Context})");

    //    // Bubble up
    //    // TODO: This should not be needed. Figure out why SelectorBase bubble up is not handling this properly.
    //    InvokeCommand (Command.Activate, args.Context);

    //    args.Handled = true;
    //}

    private void OnCheckboxOnValueChanging (object? sender, ValueChangingEventArgs<CheckState> args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }

        if (checkbox.Value == CheckState.Checked && (int)checkbox.Data! == 0 && Value == 0)
        {
            // None flag was already checked; prevent changing again
            args.Handled = true;
        }
    }

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
        // Uncheck ONLY the None checkbox (Data == 0)
        _updatingChecked = true;

        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => (int)sv.Data! == 0))
        {
            cb.Value = CheckState.UnChecked;
        }
        _updatingChecked = false;
    }

    private void UncheckAll ()
    {
        // Uncheck all NON-None checkboxes (Data != 0)
        _updatingChecked = true;

        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => (int)(sv.Data ?? null!) != 0))
        {
            cb.Value = CheckState.UnChecked;
        }
        _updatingChecked = false;
    }

    private bool _updatingChecked;

    /// <inheritdoc/>
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
    protected override void OnCreatingSubViews ()
    {
        // FlagSelector supports a "None" check box; add it
        if (Styles.HasFlag (SelectorStyles.ShowNoneFlag) && Values is { } && !Values.Contains (0))
        {
            Add (CreateCheckBox ("None", 0));
        }
    }

    /// <inheritdoc/>
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
