#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View // Command APIs
{
    #region Default Implementation

    /// <summary>
    ///     Helper to configure all things Command related for a View. Called from the View constructor.
    /// </summary>
    private void SetupCommands ()
    {
        // Enter - Raise Accepted
        AddCommand (Command.Accept, RaiseAccepting);

        // HotKey - SetFocus and raise HandlingHotKey
        AddCommand (Command.HotKey,
                    () =>
                    {
                        if (RaiseHandlingHotKey () is true)
                        {
                            return true;
                        }

                        SetFocus ();

                        return true;
                    });

        // Space or single-click - Raise Selecting
        AddCommand (Command.Select, (ctx) =>
                                    {
                                        if (RaiseSelecting (ctx) is true)
                                        {
                                            return true;
                                        }

                                        if (CanFocus)
                                        {
                                            SetFocus ();

                                            return true;
                                        }

                                        return false;
                                    });
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked. Calls <see cref="OnAccepting"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.Accept"/> handler calls this method.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     The <see cref="Accepting"/> event should raised after the state of the View has changed (after <see cref="Selecting"/> is raised).
    /// </para>
    /// <para>
    ///    If the Accepting event is not handled, <see cref="Command.Accept"/> will be invoked on the SuperView, enabling default Accept behavior.
    /// </para>
    /// <para>
    ///    If a peer-View raises the Accepting event and the event is not cancelled, the <see cref="Command.Accept"/> will be invoked on the
    ///    first Button in the SuperView that has <see cref="Button.IsDefault"/> set to <see langword="true"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    ///     If <see langword="true"/> the event was canceled. If <see langword="false"/> the event was raised but not canceled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseAccepting (CommandContext ctx)
    {
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        args.Cancel = OnAccepting (args) || args.Cancel;

        if (!args.Cancel)
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            Accepting?.Invoke (this, args);
        }

        // Accept is a special case where if the event is not canceled, the event is
        //  - Invoked on any peer-View with IsDefault == true
        //  - bubbled up the SuperView hierarchy.
        if (!args.Cancel)
        {
            // If there's an IsDefault peer view in Subviews, try it
            var isDefaultView = SuperView?.Subviews.FirstOrDefault (v => v is Button { IsDefault: true });

            if (isDefaultView != this && isDefaultView is Button { IsDefault: true } button)
            {
                bool? handled = isDefaultView.InvokeCommand (Command.Accept, ctx: new (Command.Accept, null, null, this));
                if (handled == true)
                {
                    return true;
                }
            }

            return SuperView?.InvokeCommand (Command.Accept, ctx: new (Command.Accept, null, null, this)) == true;
        }

        return Accepting is null ? null : args.Cancel;
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked. Set <see cref="CommandEventArgs.Cancel"/> to
    ///     <see langword="true"/> and return <see langword="true"/> to stop processing.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="View.RaiseAccepting"/> for more information.
    /// </para>
    /// </remarks>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnAccepting (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked. Set
    ///     <see cref="CommandEventArgs.Cancel"/> to cancel the event.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="View.RaiseAccepting"/> for more information.
    /// </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepting;

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Select"/>) causing the View to change state. Calls <see cref="OnSelecting"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.Select"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Selecting"/> event should raised after the state of the View has been changed and before see <see cref="Accepting"/>.
    /// </remarks>
    /// <returns>
    ///     If <see langword="true"/> the event was canceled. If <see langword="false"/> the event was raised but not canceled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseSelecting (CommandContext ctx)
    {
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnSelecting (args) || args.Cancel)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        Selecting?.Invoke (this, args);

        return Selecting is null ? null : args.Cancel;
    }

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Select"/>) causing the View to change state.
    ///     Set <see cref="CommandEventArgs.Cancel"/> to
    ///     <see langword="true"/> and return <see langword="true"/> to cancel the state change. The default implementation does nothing.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnSelecting (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the user has performed an action (e.g. <see cref="Command.Select"/>) causing the View to change state.
    ///     Set <see cref="CommandEventArgs.Cancel"/> to
    ///     <see langword="true"/> to cancel the state change.
    /// </summary>
    public event EventHandler<CommandEventArgs>? Selecting;

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>s. Calls <see cref="OnHandlingHotKey"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.HotKey"/> handler calls this method.
    /// </summary>
    /// <returns>
    ///     If <see langword="true"/> the event was handled. If <see langword="false"/> the event was raised but not handled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseHandlingHotKey ()
    {
        CommandEventArgs args = new ();

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnHandlingHotKey (args) || args.Cancel)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        HandlingHotKey?.Invoke (this, args);

        return HandlingHotKey is null ? null : args.Cancel;
    }

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>. Set <see cref="CommandEventArgs.Cancel"/> to
    ///     <see langword="true"/> to stop processing.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnHandlingHotKey (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the View is handling the user pressing the View's <see cref="HotKey"/>. Set
    ///     <see cref="CommandEventArgs.Cancel"/>
    ///     to cancel the event.
    /// </summary>
    public event EventHandler<CommandEventArgs>? HandlingHotKey;

    #endregion Default Implementation

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///         AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that require <see cref="CommandContext"/>. Use
    ///         <see cref="AddCommand(Command,Func{System.Nullable{bool}})"/>
    ///         in cases where the command does not require a <see cref="CommandContext"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    protected void AddCommand (Command command, Func<CommandContext, bool?> f) { CommandImplementations [command] = f; }

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///         AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that do not require a <see cref="CommandContext"/>.
    ///         If the command requires context, use
    ///         <see cref="AddCommand(Command,Func{CommandContext,System.Nullable{bool}})"/>
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    protected void AddCommand (Command command, Func<bool?> f) { CommandImplementations [command] = ctx => f (); }

    /// <summary>Returns all commands that are supported by this <see cref="View"/>.</summary>
    /// <returns></returns>
    public IEnumerable<Command> GetSupportedCommands () { return CommandImplementations.Keys; }

    /// <summary>
    ///     Invokes the specified commands.
    /// </summary>
    /// <param name="commands"></param>
    /// <param name="key">The key that caused the commands to be invoked, if any.</param>
    /// <param name="keyBinding"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was found.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled)
    ///     <see langword="false"/> if the command was invoked and the command was not handled.
    /// </returns>
    public bool? InvokeCommands (Command [] commands, Key? key = null, KeyBinding? keyBinding = null)
    {
        bool? toReturn = null;

        foreach (Command command in commands)
        {
            if (!CommandImplementations.ContainsKey (command))
            {
                throw new NotSupportedException (@$"{command} is not supported by ({GetType ().Name}).");
            }

            // each command has its own return value
            bool? thisReturn = InvokeCommand (command, key, keyBinding);

            // if we haven't got anything yet, the current command result should be used
            toReturn ??= thisReturn;

            // if ever see a true then that's what we will return
            if (thisReturn ?? false)
            {
                toReturn = true;
            }
        }

        return toReturn;
    }

    /// <summary>Invokes the specified command.</summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="key">The key that caused the command to be invoked, if any.</param>
    /// <param name="keyBinding"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was found. <see langword="true"/> if the command was invoked, and it
    ///     handled (or cancelled) the command. <see langword="false"/> if the command was invoked, and it did not handle (or cancel) the command.
    /// </returns>
    public bool? InvokeCommand (Command command, Key? key = null, KeyBinding? keyBinding = null)
    {
        if (CommandImplementations.TryGetValue (command, out Func<CommandContext, bool?>? implementation))
        {
            var context = new CommandContext (command, key, keyBinding); // Create the context here
            return implementation (context);
        }

        return null;
    }

    /// <summary>
    /// Invokes the specified command.
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="ctx">Context to pass with the invocation.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found. <see langword="true"/> if the command was invoked, and it
    ///     handled (or cancelled) the command. <see langword="false"/> if the command was invoked, and it did not handle (or cancel) the command.
    /// </returns>
    public bool? InvokeCommand (Command command, CommandContext ctx)
    {
        if (CommandImplementations.TryGetValue (command, out Func<CommandContext, bool?>? implementation))
        {
            return implementation (ctx);
        }

        return null;
    }
}
