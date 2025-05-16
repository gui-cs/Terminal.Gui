using System;
using System.ComponentModel;

namespace Terminal.Gui
{
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

            var args = new CancelEventArgs<T> (currentValue, ref newValue);
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
            var changedArgs = new EventArgs<T> (currentValue, newValue);
            onChanged?.Invoke (changedArgs);
            changedEvent?.Invoke (null, changedArgs);

            return true;
        }
    }

    public class CancelEventArgs<T> : CancelEventArgs
    {
        public T CurrentValue { get; }
        public T NewValue { get; set; }
        public CancelEventArgs (T currentValue, ref T newValue)
        {
            CurrentValue = currentValue;
            NewValue = newValue;
        }
    }

    public class EventArgs<T> : EventArgs
    {
        public T OldValue { get; }
        public T NewValue { get; }
        public EventArgs (T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}