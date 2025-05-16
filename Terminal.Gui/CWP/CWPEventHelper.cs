using System;

namespace Terminal.Gui
{
    public static class CWPEventHelper
    {
        /// <summary>
        /// Executes an event-driven CWP workflow with only an event.
        /// </summary>
        /// <typeparam name="TArgs">Type of event arguments, must have Handled property.</typeparam>
        /// <param name="eventHandler">Event to raise.</param>
        /// <param name="args">Event arguments.</param>
        /// <returns>True if handled, false otherwise.</returns>
        public static bool Execute<TArgs> (
            EventHandler<TArgs>? eventHandler,
            TArgs args)
            where TArgs : CancelEventArgs
        {
            if (eventHandler == null)
            {
                return false;
            }

            eventHandler.Invoke (null, args);
            return args.Handled;
        }
    }
}
