#nullable enable
using System.Collections.Immutable;
using System.Diagnostics;

namespace Terminal.Gui.Views;

// DoubleClick - Focus, Select, and Accept the item under the mouse.
// Click - Focus, Select, and do NOT Accept the item under the mouse.
// CanFocus - Not Focused:
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Focus item. If item is not active, make Active. Do NOT Accept.
// !CanFocus - Not Focused:
//  HotKey - Do NOT Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Do NOT Focus item. If item is not active, make Active. Do NOT Accept.
// Focused:
//  Space key - If focused item is Active, move focus to and Acivate next. Else, Select current. Do NOT Accept.
//  Enter key - Select and Accept the focused item.
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - If item is not active, make Active. Do NOT Accept.

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
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        if (base.OnHandlingHotKey (args) is true)
        {
            return true;
        }
        if (!CanFocus)
        {
            if (RaiseSelecting (args.Context) is true)
            {
                return true;
            }
//            Cycle ();
  //          return true;
        }
        else if (!HasFocus && Value is null)
        {
            if (RaiseSelecting (args.Context) is true)
            {
                return true;
            }
            SetFocus ();
            Value = Values? [0];
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override bool OnSelecting (CommandEventArgs args)
    {
        if (base.OnSelecting (args) is true)
        {
            return true;
        }

        if (!CanFocus || args.Context?.Source is not CheckBox checkBox)
        {
            Cycle ();

            return false;
        }

        if (args.Context is CommandContext<KeyBinding> { } && (int)checkBox.Data! == Value)
        {
            // Caused by keypress. If the checkbox is already checked, we cycle to the next one.
            Cycle ();

            return false;
        }
        else
        {
            if (Value == (int)checkBox.Data!)
            {
                return true;
            }

            Value = (int)checkBox.Data!;

            // if (HasFocus)
            {
                UpdateChecked ();
            }

            return false;
        }

        return false;
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

        checkbox.Selecting += OnCheckboxOnSelecting;
        checkbox.Accepting += OnCheckboxOnAccepting;
    }


    private void OnCheckboxOnSelecting (object? sender, CommandEventArgs args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.CheckedState == CheckState.Checked) <= 1);

        if (args.Context is CommandContext<MouseBinding> { } && checkbox.CheckedState == CheckState.Checked)
        {
            // If user clicks with mouse and item is already checked, do nothing
            args.Handled = true;
            return;
        }

        if (args.Context is CommandContext<KeyBinding> binding && binding.Command == Command.HotKey && checkbox.CheckedState == CheckState.Checked)
        {
            // If user uses an item hotkey and the item is already checked, do nothing
            args.Handled = true;
            return;
        }

        if (checkbox.CanFocus)
        {
            // For Select, if the view is focusable and SetFocus succeeds, by defition,
            // the event is handled. So return what SetFocus returns.
            checkbox.SetFocus ();
        }

        // Selecting doesn't normally propogate, so we do it here
        if (InvokeCommand (Command.Select, args.Context) is true)
        {
            // Do not return here; we want to toggle the checkbox state
            args.Handled = true;

            return;
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
        int valueIndex = Values.IndexOf (v => v == Value);
        Value = valueIndex == Values?.Count () - 1
            ? Values! [0]
            : Values! [valueIndex + 1];

        if (HasFocus)
        {
            valueIndex = Values.IndexOf (v => v == Value);
            SubViews.OfType<CheckBox> ().ToArray () [valueIndex].SetFocus ();
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.CheckedState == CheckState.Checked) <= 1);
    }


    /// <summary>
    ///     Updates the checked state of all checkbox subviews so that only the checkbox corresponding
    ///     to the current <see cref="Value"/> is checked. Throws <see cref="InvalidOperationException"/>
    ///     if a checkbox's Data property is not set.
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

    /// <summary>
    ///     Gets or sets the list of labels for each value.
    /// </summary>
    public string [] RadioLabels
    {
        get => Labels?.ToArray () ?? [];
        set => Labels = value;
    }

    /// <summary>Gets or sets the selected radio label index.</summary>
    /// <value>The index. -1 if no item is selected.</value>
    public int SelectedItem
    {
        get
        {
            if (Value is null)
            {
                return -1;
            }

            return Value.Value;
        }
        set => Value = value == -1 ? null : value;
    }

    /// <inheritdoc />
    protected override void OnValueChanged (int? value, int? previousValue)
    {
        int newValue = -1;
        int prevValue = -1;

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.CheckedState == CheckState.Checked) <= 1);

        if (value is { })
        {
            newValue = value.Value;
        }

        if (previousValue is { })
        {
            prevValue = previousValue.Value;
        }

        OnSelectedItemChanged (newValue, prevValue);
        SelectedItemChanged?.Invoke (this, new (newValue, prevValue));
    }

    /// <summary>Called whenever the current selected item changes. Invokes the <see cref="SelectedItemChanged"/> event.</summary>
    /// <param name="selectedItem"></param>
    /// <param name="previousSelectedItem"></param>
    protected virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem) { }
    /// <summary>Raised when the selected radio label has changed.</summary>
    public event EventHandler<SelectedItemChangedArgs>? SelectedItemChanged;

    /// <summary>
    ///     Gets or sets the <see cref="RadioLabels"/> index for the cursor. The cursor may or may not be the selected
    ///     RadioItem.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Maps to either the X or Y position within <see cref="View.Viewport"/> depending on <see cref="Orientation"/>.
    ///     </para>
    /// </remarks>
    public int Cursor
    {
        get => !CanFocus ? 0 : SubViews.OfType<CheckBox> ().ToArray ().IndexOf (Focused);
        set
        {
            if (!CanFocus)
            {
                return;
            }

            CheckBox [] checkBoxes = SubViews.OfType<CheckBox> ().ToArray ();

            if (value < 0 || value >= checkBoxes.Length)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"Cursor index is out of range");
            }

            checkBoxes [value].SetFocus ();
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        AssignHotKeys = true;
        Labels = ["Option 1", "Option 2", "Third Option", "Option Quattro"];

        return true;
    }
}
