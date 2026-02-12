namespace Terminal.Gui.ViewBase;

public partial class View // Command APIs
{
    private readonly Dictionary<Command, CommandImplementation> _commandImplementations = new ();

    /// <summary>
    ///     Helper to configure all things Command related for a View. Called from the View constructor.
    /// </summary>
    private void SetupCommands ()
    {
        // Space or single-click - Raise Activating
        AddCommand (Command.Activate, DefaultActivateHandler);

        // Enter - Raise Accepted
        AddCommand (Command.Accept, DefaultAcceptHandler);

        // HotKey - SetFocus and raise HandlingHotKey
        AddCommand (Command.HotKey, DefaultHotKeyHandler);

        // NotBound - Invoked if no handler is bound
        AddCommand (Command.NotBound, DefaultCommandNotBoundHandler);
    }

    #region Command Management

    /// <summary>
    ///     Function signature for command invocations.
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
    ///         See the Commands Deep Dive for more information: <see href="../docs/command.md"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="impl">The delegate.</param>
    protected void AddCommand (Command command, CommandImplementation impl) => _commandImplementations [command] = impl;

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
    ///         See the Commands Deep Dive for more information: <see href="../docs/command.md"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="impl">The delegate.</param>
    protected void AddCommand (Command command, Func<bool?> impl) => _commandImplementations [command] = _ => impl ();

    /// <summary>Returns all commands that are supported by this <see cref="View"/>.</summary>
    /// <returns></returns>
    public IEnumerable<Command> GetSupportedCommands () => _commandImplementations.Keys;

    #endregion Command Management

    #region Invoke

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
    public bool? InvokeCommands (Command [] commands, ICommandBinding? binding)
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
    ///     Invokes the specified command given a binding. The binding is used as context for the command invocation and can be
    ///     used by command handlers to make decisions based on the source of the command.
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
    public bool? InvokeCommand (Command command, ICommandBinding? binding) =>
        InvokeCommand (command, new CommandContext { Command = command, Source = new WeakReference<View> (this), Binding = binding });

    /// <summary>
    ///     Invokes the specified command given a context. This is the most general form of InvokeCommand and allows the caller
    ///     to specify arbitrary context.
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
    ///     Invokes the specified command with a default <see cref="CommandContext"/> where <see cref="CommandContext.Source"/>
    ///     is a weak reference to `this`. The binding in the context will be set to null since this method is for invocations
    ///     that are
    ///     not caused by a binding (e.g. bubbling, default button invocation, etc.).
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommand (Command command) =>
        InvokeCommand (command,
                       new CommandContext
                       {
                           Command = command,
                           Source = new WeakReference<View> (this),

                           // By definition, this invocation has no binding
                           Binding = null
                       });

    #endregion Invoke

    #region Default Event Handlers

    internal bool? DefaultCommandNotBoundHandler (ICommandContext? ctx) => RaiseCommandNotBound (ctx);

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
    protected virtual bool OnCommandNotBound (CommandEventArgs args) => false;

    /// <summary>
    ///     Cancelable event raised when a command that has not been bound is invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    public event EventHandler<CommandEventArgs>? CommandNotBound;

    #region Accept

    internal bool? DefaultAcceptHandler (ICommandContext? ctx)
    {
        if (RaiseAccepting (ctx) is true)
        {
            return true;
        }

        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx?.Source}) - Calling RaiseAccepted");
        RaiseAccepted (ctx);

        return false;
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked.
    ///     Calls <see cref="OnAccepting"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.Accept"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Accepting"/> event should be raised after the state of the View has changed (after
    ///         <see cref="Activating"/> is raised).
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
        //Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx?.Source?.Title})");
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        //Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx?.Source?.Title}) - Calling OnAccepting...");
        args.Handled = OnAccepting (args) || args.Handled;

        if (!args.Handled && Accepting is { })
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            //Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx?.Source?.Title}) - Raising Accepting...");
            Accepting?.Invoke (this, args);
        }

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle Activate bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleToSuperView (ctx, args.Handled) is true;
        }

        // Do not return null as the event was raised.
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
    protected virtual bool OnAccepting (CommandEventArgs args) => false;

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
    ///     Raises the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating the View has been accepted.
    ///     This is called after <see cref="Accepting"/> has been raised and not cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="Accepting"/>, this event cannot be cancelled. It is raised after the View has been accepted.
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context.</param>
    /// <seealso cref="RaiseAccepting"/>
    protected void RaiseAccepted (ICommandContext? ctx)
    {
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);
    }

    // BUGBUG: Accepted should not use CommandEventArgs since it cannot be cancelled. Use EventArgs<ICommandContext?>?

    /// <summary>
    ///     Called when the View has been accepted. This is called after <see cref="Accepting"/> has been raised and not
    ///     cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="OnAccepting"/>, this method is called after the View has been accepted and cannot cancel the
    ///         operation.
    ///     </para>
    /// </remarks>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnAccepted (CommandEventArgs args) { }

    /// <summary>
    ///     Event raised when the View has been accepted. This is raised after <see cref="Accepting"/> has been raised and not
    ///     cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="Accepting"/>, this event cannot be cancelled. It is raised after the View has been accepted.
    ///     </para>
    ///     <para>
    ///         See <see cref="RaiseAccepted"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepted;

    #endregion Accept

    #region Activate

    internal bool? DefaultActivateHandler (ICommandContext? ctx)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        if (RaiseActivating (ctx) is true)
        {
            return true;
        }

        if (CanFocus)
        {
            // Set focus if not handled yet. Setting focus does NOT mean the event is handled, so we return.
            SetFocus ();
        }

        RaiseActivated (ctx);

        return false;
    }

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the View to change state
    ///     or preparing it for interaction.
    ///     Calls <see cref="OnActivating"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.Activate"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Activating"/> event should be raised after the state of the View has been changed and before see
    ///     <see cref="Accepting"/>.
    /// </remarks>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseActivating (ICommandContext? ctx)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnActivating (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Invoking Activating event");
        Activating?.Invoke (this, args);

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle Activate bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleToSuperView (ctx, args.Handled) is true;
        }

        return args.Handled;
    }

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the View to change state
    ///     or preparing it for interaction.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> and return <see langword="true"/> to indicate the event was
    ///     handled and processing should stop.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnActivating (CommandEventArgs args) => false;

    /// <summary>
    ///     Cancelable event raised when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the
    ///     View
    ///     to change state or preparing it for interaction.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    public event EventHandler<CommandEventArgs>? Activating;

    /// <summary>
    ///     Raises the <see cref="OnActivated"/>/<see cref="Activated"/> event indicating the View has been activated.
    ///     This is called after <see cref="Activated"/> has been raised and not cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="Activating"/>, this event cannot be cancelled. It is raised after the View has activated.
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context.</param>
    /// <seealso cref="RaiseActivating"/>
    protected void RaiseActivated (ICommandContext? ctx)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");
        OnActivated (ctx);
        Activated?.Invoke (this, new EventArgs<ICommandContext?> (ctx));
    }

    /// <summary>
    ///     Called when the View has been activated. This is called after <see cref="Accepting"/> has been raised and not
    ///     cancelled.
    /// </summary>
    /// <param name="ctx">The event arguments.</param>
    protected virtual void OnActivated (ICommandContext? ctx) { }

    /// <summary>
    ///     Event raised when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the
    ///     View to change state or preparing it for interaction.
    /// </summary>
    public event EventHandler<EventArgs<ICommandContext?>>? Activated;

    #endregion Activate

    #region HotKey

    internal bool? DefaultHotKeyHandler (ICommandContext? ctx)
    {
        if (RaiseHandlingHotKey (ctx) is true)
        {
            return true;
        }

        if (CanFocus)
        {
            // Set focus if not handled yet. Setting focus does NOT mean the event is handled, so we return.
            SetFocus ();
        }

        RaiseHotKeyCommand (ctx);

        // Pass the original binding so downstream handlers (e.g. Shortcut.OnActivating)
        // can distinguish a user-initiated HotKey activation from a programmatic one.
        InvokeCommand (Command.Activate, ctx?.Binding);

        return false;
    }

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>s. Calls
    ///     <see cref="OnHandlingHotKey"/> which can be cancelled. If not cancelled raises <see cref="HandlingHotKey"/> event.
    ///     The default <see cref="Command.HotKey"/> handler calls this method.
    /// </summary>
    /// <param name="ctx">The context to pass with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseHandlingHotKey (ICommandContext? ctx)
    {
        CommandEventArgs args = new () { Context = ctx };

        if (OnHandlingHotKey (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        HandlingHotKey?.Invoke (this, args);

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleToSuperView (ctx, args.Handled) is true;
        }

        return args.Handled;
    }

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnHandlingHotKey (CommandEventArgs args) => false;

    /// <summary>
    ///     Raises the <see cref="OnHotKeyCommand"/>/<see cref="HotKeyCommand"/> event indicating the View is handling the user
    ///     pressing the View's <see cref="HotKey"/>.
    ///     The default <see cref="Command.HotKey"/> handler calls this method.
    /// </summary>
    /// <param name="ctx"></param>
    protected void RaiseHotKeyCommand (ICommandContext? ctx)
    {
        OnHotKeyCommand (ctx);
        HotKeyCommand?.Invoke (this, new EventArgs<ICommandContext?> (ctx));
    }

    /// <summary>
    ///     Cancelable event raised when the View is handling the user pressing the View's <see cref="HotKey"/>. Set
    ///     CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should stop.
    /// </summary>
    public event EventHandler<CommandEventArgs>? HandlingHotKey;

    /// <summary>
    ///     Called when the View's <see cref="HotKey"/> is pressed, if the event was not handled by
    ///     <see cref="OnHandlingHotKey"/> or the <see cref="HandlingHotKey"/> event.
    /// </summary>
    /// <param name="ctx"></param>
    protected virtual void OnHotKeyCommand (ICommandContext? ctx) { }

    /// <summary>
    ///     Event raised when the View's <see cref="HotKey"/> is pressed, if the event was not handled by
    ///     <see cref="OnHandlingHotKey"/> or the <see cref="HandlingHotKey"/> event.
    /// </summary>
    public event EventHandler<EventArgs<ICommandContext?>>? HotKeyCommand;

    #endregion HotKey

    #endregion Default Event Handlers

    #region Command Propagation

    /// <summary>
    ///     Gets or sets the default accept view for this View. The default accept view will have <see cref="Command.Accept"/>
    ///     invoked on it
    ///     anytime a peer View raises <see cref="Command.Accept"/> and the event is not handled.
    /// </summary>
    public View? DefaultAcceptView
    {
        get
        {
            if (field is null)
            {
                return GetSubViews (includePadding: true).FirstOrDefault (v => v is Button { IsDefault: true });
            }

            return field;
        }
        set => field = value;
    }

    /// <summary>
    ///     Gets or sets the list of commands that should bubble up to this View from unhandled SubViews.
    ///     When a SubView raises a command that is not handled, and the command is in the SuperView's
    ///     <see cref="CommandsToBubbleUp"/> list, the command will be invoked on the SuperView.
    /// </summary>
    /// <remarks>
    ///     e.g. to enable <see cref="Command.Activate"/> bubbling for hierarchical views:
    ///     <code>
    ///         menuBar.CommandsToBubbleUp = [Command.Activate];
    ///     </code>
    /// </remarks>
    public IReadOnlyList<Command> CommandsToBubbleUp { get; set; } = [];

    /// <summary>
    ///     Dispatches a command downward to a SubView with bubbling suppressed. Creates a new
    ///     <see cref="CommandContext"/> with <see cref="ICommandContext.IsBubblingDown"/> set to <see langword="true"/>,
    ///     which causes <see cref="TryBubbleToSuperView"/> to skip bubbling on the target, preventing re-entry.
    /// </summary>
    /// <param name="target">The SubView to dispatch the command to.</param>
    /// <param name="ctx">The original command context, used to determine the command and source.</param>
    /// <returns>
    ///     The result of invoking the command on the target.
    /// </returns>
    protected bool? BubbleDown (View target, ICommandContext? ctx)
    {
        CommandContext downCtx = new (ctx?.Command ?? Command.NotBound, ctx?.Source, null) { IsBubblingDown = true };

        return target.InvokeCommand (downCtx.Command, downCtx);
    }

    /// <summary>
    ///     Bubbles a command to the SuperView if the command is in SuperView's <see cref="CommandsToBubbleUp"/> list.
    ///     Handles the special case of invoking <see cref="Command.Accept"/> on a peer IsDefault button.
    /// </summary>
    /// <param name="ctx">The command context to pass along.</param>
    /// <param name="handled">Whether the command was already handled by this View.</param>
    /// <returns>
    ///     <see langword="true"/> if the command was handled (either locally or by bubbling).
    ///     <see langword="false"/> if the command was not handled.
    /// </returns>
    protected bool? TryBubbleToSuperView (ICommandContext? ctx, bool handled)
    {
        if (handled)
        {
            return true;
        }

        if (ctx?.IsBubblingDown == true)
        {
            return handled;
        }

        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        // Special case: Command.Accept checks for IsDefault peer button first
        if (ctx?.Command == Command.Accept)
        {
            View? isDefaultView = SuperView?.DefaultAcceptView;

            if (isDefaultView is { } && isDefaultView != this)
            {
                ctx?.Source = new WeakReference<View> (isDefaultView);
                bool? buttonHandled = isDefaultView.InvokeCommand (Command.Accept, ctx);

                if (buttonHandled == true)
                {
                    return true;
                }
            }
        }

        // Check if SuperView wants this command bubbled up to it
        if (SuperView?.CommandsToBubbleUp.Contains (ctx!.Command) == true)
        {
            //ICommandContext context = new CommandContext (ctx.Command, ctx.Source, null);
            return SuperView.InvokeCommand (ctx.Command, ctx);
        }

        if (SuperView is Padding padding)
        {
            // Check if Padding's Parent wants this command bubbled up to it
            if (padding.Parent?.CommandsToBubbleUp.Contains (ctx!.Command) == true)
            {
                return padding.Parent.InvokeCommand (ctx!.Command, ctx);
            }
        }

        return handled;
    }

    #endregion Command Propagation
}
