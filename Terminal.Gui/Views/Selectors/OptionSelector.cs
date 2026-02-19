using System.Diagnostics;

namespace Terminal.Gui.Views;

// DoubleClick - Focus, Activate, and Accept the item under the mouse (CanFocus or not)
// Click - Focus, Activate, and do NOT Accept the item under the mouse (CanFocus or not).
// CanFocus - Not Focused:
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Focus item. If item is not active, make Active. Do NOT Accept.
// !CanFocus - Not Focused:
//  HotKey - Do NOT Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Do NOT Focus item. If item is not active, make Active. Do NOT Accept.
// Focused:
//  Space key - If focused item is Active, move focus to and Activate next. Else, Activate current. Do NOT Accept.
//  Enter key - Activate and Accept the focused item.
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - If item is not active, make Active. Do NOT Accept. If item is active, do nothing.

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

    // ──── Command Coordination ────

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
        if (ctx.Source?.TryGetTarget (out View? source) == true && source is CheckBox)
        {
            return source;
        }

        return Focused;
    }

    /// <summary>
    ///     Consumes: OptionSelector owns selection state, not the individual CheckBoxes.
    /// </summary>
    protected override bool ConsumeDispatch => true;

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");
        base.OnActivated (ctx);

        // Apply value change for all activation paths.
        ApplyActivation (ctx);
    }

    /// <summary>
    ///     Applies the value change based on the activation source.
    /// </summary>
    private void ApplyActivation (ICommandContext? ctx)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        // Determine the target CheckBox based on routing:
        // - BubblingUp: use ctx.Source (the CheckBox that bubbled)
        // - DispatchingDown (from Shortcut): use Focused (the CheckBox that has focus)
        // - Direct/programmatic: Cycle() (advance to next option)
        CheckBox? checkBox = null;

        if (ctx?.Source?.TryGetTarget (out View? sourceView) == true && sourceView is CheckBox cb)
        {
            checkBox = cb;
        }
        else if (ctx?.Routing == CommandRouting.DispatchingDown && Focused is CheckBox focusedCb)
        {
            checkBox = focusedCb;
        }

        if (checkBox is null)
        {
            Cycle ();

            return;
        }

        if (ctx.Binding is KeyBinding keyBinding && (int)checkBox.Data! == Value && keyBinding.Key is { } && keyBinding.Key == Key.Space)
        {
            // Caused by space. If the checkbox is already checked, we cycle to the next one.
            Cycle ();
        }
        else
        {
            if (Value == (int)checkBox.Data!)
            {
                return;
            }

            Value = (int)checkBox.Data!;
        }
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
    }

    private void Cycle ()
    {
        Logging.Debug ($"{this.ToIdentifyingString ()}");

        int valueIndex = Values.IndexOf (v => v == Value);

        Value = valueIndex == Values?.Count - 1 ? Values! [0] : Values! [valueIndex + 1];

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
            Logging.Debug ($"{this.ToIdentifyingString ()} {cb.ToIdentifyingString ()}.Value = {value}");

            cb.Value = value == Value ? CheckState.Checked : CheckState.UnChecked;
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.Value == CheckState.Checked) <= 1);
    }

    /// <summary>
    ///     Gets or sets the <see cref="SelectorBase.Labels"/> index for the focused item. The active item may or may not be
    ///     the selected
    ///     RadioItem.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Maps to either the X or Y position within <see cref="View.Viewport"/> depending on <see cref="Orientation"/>.
    ///     </para>
    /// </remarks>
    public int FocusedItem
    {
        get
        {
            if (!CanFocus)
            {
                return 0;
            }

            return HasFocus ? SubViews.OfType<CheckBox> ().ToArray ().IndexOf (Focused) : field;
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
                throw new ArgumentOutOfRangeException (nameof (value), @"FocusedItem index is out of range");
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
