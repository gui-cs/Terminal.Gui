#nullable enable
using System.Collections.Immutable;
using System.Diagnostics;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a user interface for displaying and selecting a single item from a list of options.
///     Each option is represented by a checkbox, but only one can be selected at a time.
/// </summary>
public class OptionSelector : SelectorBase, IDesignable
{
    /// <inheritdoc />
    public OptionSelector ()
    {
        // By default, for OptionSelector, Value is set to 0. It can be set to null if a developer
        // really wants that.
        base.Value = 0;
    }

    /// <inheritdoc />
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);
        if (view is not CheckBox checkbox)
        {
            return;
        }

        checkbox.RadioStyle = true;

        checkbox.Activating += OnCheckboxOnActivating;
        checkbox.Accepting += OnCheckboxOnAccepting;
    }


    private void OnCheckboxOnActivating (object? sender, CommandEventArgs args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }
        // Activating doesn't normally propogate, so we do it here
        if (RaiseActivating (args.Context) is true)
        {
            // Do not return here; we want to toggle the checkbox state
        }

        if (args.Context is CommandContext<KeyBinding> { } && (int)checkbox.Data! == Value)
        {
            // Caused by keypress. If the checkbox is already checked, we cycle to the next one.
            Cycle ();
        }
        else
        {
            Value = (int)checkbox.Data!;

            if (HasFocus)
            {
                UpdateChecked ();
            }
        }
        args.Handled = true;
    }

    private void OnCheckboxOnAccepting (object? sender, CommandEventArgs args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }
        Value = (int)checkbox.Data!;
    }

    private void Cycle ()
    {
        if (Value == Labels?.Count () - 1)
        {
            Value = 0;
        }
        else
        {
            Value++;
        }

        if (HasFocus)
        {
            SubViews.OfType<CheckBox> ().ToArray () [Value!.Value].SetFocus ();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public override void UpdateChecked ()
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            int value = (int)(cb.Data ?? throw new InvalidOperationException ("CheckBox.Data must be set"));

            cb.CheckedState = value == Value ? CheckState.Checked : CheckState.UnChecked;
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.CheckedState == CheckState.Checked) <= 1);
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        AssignHotKeys = true;
        Labels = ["Option 1", "Option 2", "Third Option", "Option Quattro"];

        return true;
    }
}
