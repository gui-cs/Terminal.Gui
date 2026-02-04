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
//  Space key - If focused item is Active, move focus to and Acivate next. Else, Activate current. Do NOT Accept.
//  Enter key - Activate and Accept the focused item.
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - If item is not active, make Active. Do NOT Accept.

/// <summary>
///     Provides a user interface for displaying and selecting a single item from a list of options.
///     Each option is represented by a checkbox, but only one can be selected at a time.
///     <see cref="OptionSelector{TEnum}"/> provides a type-safe version where a <see langword="enum"/> can be
///     provided.
/// </summary>
public class OptionSelector : SelectorBase, IDesignable
{
    /// <inheritdoc/>
    public OptionSelector () =>

        // By default, for OptionSelector, Value is set to 0. It can be set to null if a developer
        // really wants that.
        base.Value = 0;

    /// <inheritdoc/>
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        if (base.OnHandlingHotKey (args) is true)
        {
            return true;
        }

        if (!CanFocus)
        {
            if (RaiseActivating (args.Context) is true)
            {
                return true;
            }
        }
        else if (!HasFocus && Value is null)
        {
            if (RaiseActivating (args.Context) is true)
            {
                return true;
            }

            SetFocus ();
            Value = Values? [0];

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        if (base.OnActivating (args) is true)
        {
            return true;
        }

        if (!CanFocus || args.Context?.Source is not CheckBox checkBox)
        {
            Cycle ();

            return false;
        }

        if (args.Context?.Binding is KeyBinding && (int)checkBox.Data! == Value)
        {
            // Caused by keypress. If the checkbox is already checked, we cycle to the next one.
            Cycle ();
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
        }

        return false;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc />
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (previousFocusedView == this)
        {
            // Losing focus, restore cursor to value
            SetCursor ();
        }
        else if (focusedView == this)
        {
            // Gaining focus, restore cursor to value
            SetCursor ();
        }
        base.OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);

        return;

        void SetCursor ()
        {
            if (Values is { } && Value.HasValue && Cursor != Value)
            {
                Cursor = Value.Value;
            }
        }
    }

    private void OnCheckboxOnActivating (object? sender, CommandEventArgs args)
    {
        if (sender is not CheckBox checkbox)
        {
            return;
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.Value == CheckState.Checked) <= 1);

        if (args.Context?.Binding is MouseBinding && checkbox.Value == CheckState.Checked)
        {
            // If user clicks with mouse and item is already checked, do nothing
            args.Handled = true;

            return;
        }

        if (args.Context?.Binding is KeyBinding && args.Context.Command == Command.HotKey && checkbox.Value == CheckState.Checked)
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

        // Selecting doesn't normally propagate, so we do it here
        if (InvokeCommand (Command.Activate, args.Context) is true)
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
        args.Handled = false; // Do not set to false; let Accepting propagate
    }

    private void Cycle ()
    {
        int valueIndex = Values.IndexOf (v => v == Value);

        Value = valueIndex == Values?.Count () - 1 ? Values! [0] : Values! [valueIndex + 1];

        if (HasFocus)
        {
            valueIndex = Values.IndexOf (v => v == Value);
            SubViews.OfType<CheckBox> ().ToArray () [valueIndex].SetFocus ();
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.Value == CheckState.Checked) <= 1);
    }

    /// <summary>
    ///     Updates the checked state of all checkbox subviews so that only the checkbox corresponding
    ///     to the current <see cref="SelectorBase.Value"/> is checked. Throws <see cref="InvalidOperationException"/>
    ///     if a checkbox's Data property is not set.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public override void UpdateChecked ()
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            var value = (int)(cb.Data ?? throw new InvalidOperationException ("CheckBox.Data must be set"));

            cb.Value = value == Value ? CheckState.Checked : CheckState.UnChecked;
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.Value == CheckState.Checked) <= 1);
    }

    /// <summary>
    ///     Gets or sets the <see cref="SelectorBase.Labels"/> index for the cursor. The cursor may or may not be the selected
    ///     RadioItem.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Maps to either the X or Y position within <see cref="View.Viewport"/> depending on <see cref="Orientation"/>.
    ///     </para>
    /// </remarks>
    public new int Cursor
    {
        get
        {
            if (!CanFocus)
            {
                return 0;
            }

            if (HasFocus)
            {
                return SubViews.OfType<CheckBox> ().ToArray ().IndexOf (Focused);
            }

            return field;
        }
        set
        {
            if (!CanFocus)
            {
                return;
            }

            field = value;

            CheckBox [] checkBoxes = SubViews.OfType<CheckBox> ().ToArray ();

            if (value < 0 || value >= checkBoxes.Length)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"Cursor index is out of range");
            }

            if (HasFocus)
            {
                checkBoxes [value].SetFocus ();
            }
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
