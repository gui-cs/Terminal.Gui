using System;
using System.ComponentModel;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace Terminal.Gui
{
    public static class CWPWorkflowHelper
    {
        /// <summary>
        /// Executes a single-phase CWP workflow with a virtual method, event, and default action.
        /// </summary>
        /// <typeparam name="TArgs">Type of event arguments, must inherit from CancelEventArgs.</typeparam>
        /// <param name="onMethod">Virtual method, returns true to cancel.</param>
        /// <param name="eventHandler">Event to raise.</param>
        /// <param name="args">Event arguments with Cancel/Handled property.</param>
        /// <param name="defaultAction">Default action to execute if not handled.</param>
        /// <returns>True if handled, false if not, null if no event subscribers.</returns>
        public static bool? Execute<TArgs> (
            Func<TArgs, bool> onMethod,
            EventHandler<TArgs>? eventHandler,
            TArgs args,
            Action? defaultAction = null)
            where TArgs : CancelEventArgs
        {
            if (onMethod (args) || args.Handled)
            {
                return true;
            }

            eventHandler?.Invoke (null, args);
            if (args.Handled)
            {
                return true;
            }

            if (defaultAction != null)
            {
                defaultAction ();
                return true;
            }

            return eventHandler == null ? null : false;
        }

        /// <summary>
        /// Executes a CWP workflow with a result, suitable for methods like GetScheme.
        /// </summary>
        /// <typeparam name="TArgs">Type of event arguments.</typeparam>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="onMethod">Virtual method, returns true to cancel.</param>
        /// <param name="eventHandler">Event to raise.</param>
        /// <param name="args">Event arguments with result property.</param>
        /// <param name="defaultAction">Default action returning a result.</param>
        /// <returns>Result or default if handled/cancelled.</returns>
        public static TResult ExecuteWithResult<TArgs, TResult> (
            Func<TArgs, bool> onMethod,
            EventHandler<TArgs>? eventHandler,
            TArgs args,
            Func<TResult> defaultAction)
            where TArgs : CancelEventArgs<TResult>
        {
            if (onMethod (args) || args.Handled)
            {
                return args.Result ?? default!;
            }

            eventHandler?.Invoke (null, args);
            if (args.Handled)
            {
                return args.Result ?? default!;
            }

            return defaultAction ();
        }
    }

    public class CancelEventArgs<TResult> : CancelEventArgs
    {
        public TResult? Result { get; set; }
        public CancelEventArgs (TResult? result) => Result = result;
    }
}