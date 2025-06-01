#nullable enable
using System;
using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
/// 
/// </summary>
public static class CWPPropertyHelper
{
    /// <summary>
    /// Executes a CWP workflow for a property change, with pre- and post-change events.
    /// </summary>
    /// <typeparam name="T">Type of the property value.</typeparam>
    /// <param name="currentValue">Current property value.</param>
    /// <param name="newValue">Proposed new value.</param>
    /// <param name="onChanging">Virtual method for pre-change, returns true to cancel.</param>
    /// <param name="changingEvent">Pre-change event.</param>
    /// <param name="onChanged">Virtual method for post-change.</param>
    /// <param name="changedEvent">Post-change event.</param>
    /// <returns>True if the property was changed, false if cancelled.</returns>
    public static bool ChangeProperty<T> (
        T currentValue,
        ref T newValue,
        Func<CancelEventArgs<T>, bool> onChanging,
        EventHandler<CancelEventArgs<T>>? changingEvent,
        Action<EventArgs<T>>? onChanged,
        EventHandler<EventArgs<T>>? changedEvent)
    {
        if (EqualityComparer<T>.Default.Equals (currentValue, newValue))
        {
            return false;
        }

        CancelEventArgs<T> args = new CancelEventArgs<T> (in currentValue, ref newValue);
        if (onChanging (args) || args.Cancel)
        {
            return false;
        }

        changingEvent?.Invoke (null, args);
        if (args.Cancel)
        {
            return false;
        }

        newValue = args.NewValue;
        EventArgs<T> changedArgs = new EventArgs<T> (newValue);
        onChanged?.Invoke (changedArgs);
        changedEvent?.Invoke (null, changedArgs);

        return true;
    }
}
