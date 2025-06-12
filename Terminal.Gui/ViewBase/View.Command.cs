#nullable enable

namespace Terminal.Gui.ViewBase;

public partial class View // Command APIs
{
    private readonly Dictionary<Command, CommandImplementation> _commandImplementations = new ();

    #region Default Implementation

    /// <summary>
    ///     Helper to configure all things Command related for a View. Called from the View constructor.
    /// </summary>
    private void SetupCommands ()
    {
        // NotBound - Invoked if no handler is bound
        AddCommand (Command.NotBound, RaiseCommandNotBound);

        // Enter - Raise Accepted
        AddCommand (Command.Accept, RaiseAccepting);

        // HotKey - SetFocus and raise HandlingHotKey
        AddCommand (
                    Command.HotKey,
                    () =>
                    {
                        if (RaiseHandlingHotKey () is true)
                        {
                            return true;
                        }

                        SetFocus ();

                        // Always return true on hotkey, even if SetFocus fails because 
                        // hotkeys are always handled by the View (unless RaiseHandlingHotKey cancels).
                        return true;
                    });

        // Space or single-click - Raise Selecting
        AddCommand (
                    Command.Select,
                    ctx =>
                    {
                        if (RaiseSelecting (ctx) is true)
                        {
                            return true;
                        }

                        if (CanFocus)
                        {
                            // For Select, if the view is focusable and SetFocus succeeds, by defition,
                            // the event is handled. So return what SetFocus returns.
                            return SetFocus ();
                        }

                        return false;
                    });
    }

    /// <summary>
    ///     Called when a command that has not been bound is invoked.
    /// </summary>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseCommandNotBound (ICommandContext? ctx)
    {
        CommandEventArgs args = new () { Context = ctx };

        // For robustness' sake, even if the virtual method returns true, if the args 
        // indicate the event should be cancelled, we honor that.
        if (OnCommandNotBound (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        CommandNotBound?.Invoke (this, args);

        return CommandNotBound is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when a command that has not been bound is invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> and return <see langword="true"/> to indicate the event was
    ///     handled and processing should stop.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnCommandNotBound (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when a command that has not been bound is invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    public event EventHandler<CommandEventArgs>? CommandNotBound;

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked.
    ///     Calls <see cref="OnAccepting"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.Accept"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Accepting"/> event should be raised after the state of the View has changed (after
    ///         <see cref="Selecting"/> is raised).
    ///     </para>
    ///     <para>
    ///         If the Accepting event is not handled, <see cref="Command.Accept"/> will be invoked on the SuperView, enabling
    ///         default Accept behavior.
    ///     </para>
    ///     <para>
    ///         If a peer-View raises the Accepting event and the event is not cancelled, the <see cref="Command.Accept"/> will
    ///         be invoked on the
    ///         first Button in the SuperView that has <see cref="Button.IsDefault"/> set to <see langword="true"/>.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseAccepting (ICommandContext? ctx)
    {
        Logging.Debug ($"{Title} ({ctx?.Source?.Title})");
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        Logging.Debug ($"{Title} ({ctx?.Source?.Title}) - Calling OnAccepting...");
        args.Handled = OnAccepting (args) || args.Handled;

        if (!args.Handled && Accepting is { })
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            Logging.Debug ($"{Title} ({ctx?.Source?.Title}) - Raising Accepting...");
            Accepting?.Invoke (this, args);
        }

        // Accept is a special case where if the event is not canceled, the event is
        //  - Invoked on any peer-View with IsDefault == true
        //  - bubbled up the SuperView hierarchy.
        if (!args.Handled)
        {
            // If there's an IsDefault peer view in SubViews, try it
            View? isDefaultView = SuperView?.InternalSubViews.FirstOrDefault (v => v is Button { IsDefault: true });

            if (isDefaultView != this && isDefaultView is Button { IsDefault: true } button)
            {
                // TODO: It's a bit of a hack that this uses KeyBinding. There should be an InvokeCommmand that 
                // TODO: is generic?

                Logging.Debug ($"{Title} ({ctx?.Source?.Title}) - InvokeCommand on Default View ({isDefaultView.Title})");
                bool? handled = isDefaultView.InvokeCommand (Command.Accept, ctx);

                if (handled == true)
                {
                    return true;
                }
            }

            if (SuperView is { })
            {
                Logging.Debug ($"{Title} ({ctx?.Source?.Title}) - Invoking Accept on SuperView ({SuperView.Title}/{SuperView.Id})...");

                return SuperView?.InvokeCommand (Command.Accept, ctx);
            }
        }

        return args.Handled;
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> and return <see langword="true"/> to indicate the event was
    ///     handled and processing should stop.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="View.RaiseAccepting"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnAccepting (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the user is accepting the state of the View and the <see cref="Command.Accept"/> has
    ///     been invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="View.RaiseAccepting"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepting;

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Select"/>) causing the View to change state.
    ///     Calls <see cref="OnSelecting"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.Select"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Selecting"/> event should be raised after the state of the View has been changed and before see
    ///     <see cref="Accepting"/>.
    /// </remarks>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseSelecting (ICommandContext? ctx)
    {
        //Logging.Debug ($"{Title} ({ctx?.Source?.Title})");
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnSelecting (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        Selecting?.Invoke (this, args);

        return Selecting is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Select"/>) causing the View to change state.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> and return <see langword="true"/> to indicate the event was
    ///     handled and processing should stop.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnSelecting (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the user has performed an action (e.g. <see cref="Command.Select"/>) causing the View
    ///     to change state.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    public event EventHandler<CommandEventArgs>? Selecting;

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>s. Calls
    ///     <see cref="OnHandlingHotKey"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.HotKey"/> handler calls this method.
    /// </summary>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseHandlingHotKey ()
    {
        CommandEventArgs args = new () { Context = new CommandContext<KeyBinding> { Command = Command.HotKey } };
        //Logging.Debug ($"{Title} ({args.Context?.Source?.Title})");

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnHandlingHotKey (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        HandlingHotKey?.Invoke (this, args);

        return HandlingHotKey is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnHandlingHotKey (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the View is handling the user pressing the View's <see cref="HotKey"/>. Set
    ///     CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should stop.
    /// </summary>
    public event EventHandler<CommandEventArgs>? HandlingHotKey;

    #endregion Default Implementation

    /// <summary>
    ///     Function signature commands.
    /// </summary>
    /// <param name="ctx">Provides context about the circumstances of invoking the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    public delegate bool? CommandImplementation (ICommandContext? ctx);

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///         AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="impl"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that require <see cref="ICommandContext"/>.
    ///     </para>
    ///     <para>
    ///         See the Commands Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.Gui/docs/command.html"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="impl">The delegate.</param>
    protected void AddCommand (Command command, CommandImplementation impl) { _commandImplementations [command] = impl; }

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///         AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="impl"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that do not require context.
    ///         If the command requires context, use
    ///         <see cref="AddCommand(Command,CommandImplementation)"/>
    ///     </para>
    ///     <para>
    ///         See the Commands Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.Gui/docs/command.html"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="impl">The delegate.</param>
    protected void AddCommand (Command command, Func<bool?> impl) { _commandImplementations [command] = ctx => impl (); }

    /// <summary>Returns all commands that are supported by this <see cref="View"/>.</summary>
    /// <returns></returns>
    public IEnumerable<Command> GetSupportedCommands () { return _commandImplementations.Keys; }

    /// <summary>
    ///     Invokes the specified commands.
    /// </summary>
    /// <param name="commands">The set of commands to invoke.</param>
    /// <param name="binding">The binding that caused the invocation, if any. This will be passed as context with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommands<TBindingType> (Command [] commands, TBindingType binding)
    {
        bool? toReturn = null;

        foreach (Command command in commands)
        {
            if (!_commandImplementations.ContainsKey (command))
            {
                Logging.Warning (@$"{command} is not supported by this View ({GetType ().Name}). Binding: {binding}.");
            }

            // each command has its own return value
            bool? thisReturn = InvokeCommand (command, binding);

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

    /// <summary>
    ///     Invokes the specified command.
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="binding">The binding that caused the invocation, if any. This will be passed as context with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommand<TBindingType> (Command command, TBindingType binding)
    {
        if (!_commandImplementations.TryGetValue (command, out CommandImplementation? implementation))
        {
            _commandImplementations.TryGetValue (Command.NotBound, out implementation);
        }

        return implementation! (
                                new CommandContext<TBindingType>
                                {
                                    Command = command,
                                    Source = this,
                                    Binding = binding
                                });
    }

    /// <summary>
    ///     Invokes the specified command.
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="ctx">The context to pass with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommand (Command command, ICommandContext? ctx)
    {
        if (!_commandImplementations.TryGetValue (command, out CommandImplementation? implementation))
        {
            _commandImplementations.TryGetValue (Command.NotBound, out implementation);
        }

        return implementation! (ctx);
    }

    /// <summary>
    ///     Invokes the specified command without context.
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommand (Command command)
    {
        if (!_commandImplementations.TryGetValue (command, out CommandImplementation? implementation))
        {
            _commandImplementations.TryGetValue (Command.NotBound, out implementation);
        }

        return implementation! (
                                new CommandContext<object>
                                {
                                    Command = command,
                                    Source = this,
                                    Binding = null
                                });
    }
}
