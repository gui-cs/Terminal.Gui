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

        // Logging.Debug ($"{this.ToIdentifyingString ()} {ctx}");

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
        // Logging.Debug ($"{this.ToIdentifyingString ()} {ctx}");

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
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        if (RaiseAccepting (ctx) is true)
        {
            // If dispatch consumed the command, the composite view needs completion.
            if (_lastDispatchOccurred)
            {
                RaiseAccepted (ctx);
            }

            return true;
        }

        // After this View's Accepting was raised (and not handled/cancelled),
        // forward Accept to the DefaultAcceptView so its Accepting/Accepted events fire too.
        // The defaultAcceptView != source check prevents self-invocation (infinite loops).
        //
        // Skip the DefaultAcceptView redirect if Accept will also bubble to an ancestor
        // via CommandsToBubbleUp. Both paths (bubble + redirect) would reach the same ancestor,
        // causing double Accepted events. The bubble path handles it.
        View? source = null;
        ctx?.Source?.TryGetTarget (out source);
        View? defaultAcceptView = DefaultAcceptView;

        var redirected = false;
        bool acceptWillBubble = CommandWillBubbleToAncestor (Command.Accept);

        if (!acceptWillBubble && defaultAcceptView is { } && defaultAcceptView != this && defaultAcceptView != source)
        {
            BubbleDown (defaultAcceptView, ctx);
            redirected = true;
        }

        // Composite views with dispatch targets always get completion on bubble.
        if (ctx?.Routing == CommandRouting.BubblingUp && GetDispatchTarget (ctx) is { })
        {
            RaiseAccepted (ctx);

            return false;
        }

        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Calling RaiseAccepted");
        RaiseAccepted (ctx);

        // Report as handled if:
        // - Accept was redirected to DefaultAcceptView (BubbleDown performed), or
        // - Accept will bubble to ancestor (so DefaultAcceptView redirect was skipped), or
        // - Accept bubbled up from a SubView (the full chain processed the command), or
        // - This view is an IAcceptTarget (e.g. Button) that genuinely handles Accept.
        // Report as not handled when Accept originated from a local key binding (e.g., Enter key)
        // on a non-IAcceptTarget view with no redirect - this allows the key to propagate up
        // the view hierarchy to reach a SuperView that can redirect to DefaultAcceptView.
        return redirected || acceptWillBubble || ctx?.Routing == CommandRouting.BubblingUp || this is IAcceptTarget;
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

        // Framework dispatch: composite views delegate commands to a target SubView.
        if (!args.Handled)
        {
            args.Handled = TryDispatchToTarget (ctx);
        }

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle Activate bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleUp (ctx, args.Handled) is true;
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
    protected internal void RaiseAccepted (ICommandContext? ctx)
    {
        OnAccepted (ctx);
        Accepted?.Invoke (this, new CommandEventArgs { Context = ctx });
    }

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
    /// <param name="ctx"></param>
    protected virtual void OnAccepted (ICommandContext? ctx) { }

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
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        if (RaiseActivating (ctx) is true)
        {
            // If dispatch consumed the command, the composite view needs completion.
            if (_lastDispatchOccurred)
            {
                RaiseActivated (ctx);
            }

            return true;
        }

        // When a SubView's activation bubbles up, the default behavior is notification:
        // Activating fires (above), but Activated and side effects (SetFocus) are skipped.
        // The originating view completes its own activation. Returning false tells TryBubbleUp
        // "not consumed" so the originator continues.
        //
        // Composite views with ConsumeDispatch=true already completed above (RaiseActivating returned true).
        // Composite views with ConsumeDispatch=false (relay) defer completion — they use the
        // dispatch target's Activated event to fire their own RaiseActivated after the originator completes.
        if (ctx?.Routing == CommandRouting.BubblingUp)
        {
            return false;
        }

        if (CanFocus)
        {
            SetFocus ();
        }

        // For relay dispatch (ConsumeDispatch=false), the dispatch target's Activated event
        // already fired RaiseActivated via the deferred completion callback (e.g., CommandView_Activated
        // in Shortcut). Skip duplicate RaiseActivated.
        if (!_lastDispatchOccurred)
        {
            RaiseActivated (ctx);
        }

        return true;
    }

    /// <summary>
    ///     Checks whether the given <paramref name="command"/> will bubble to an ancestor via
    ///     <see cref="CommandsToBubbleUp"/>. This mirrors the checks in <see cref="TryBubbleUp"/>.
    /// </summary>
    private bool CommandWillBubbleToAncestor (Command command)
    {
        if (SuperView?.CommandsToBubbleUp.Contains (command) == true)
        {
            return true;
        }

        if (SuperView is Padding padding && padding.Parent?.CommandsToBubbleUp.Contains (command) == true)
        {
            return true;
        }

        if (this is Padding selfPadding && selfPadding.Parent?.CommandsToBubbleUp.Contains (command) == true)
        {
            return true;
        }

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
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnActivating (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Invoking Activating event");
        Activating?.Invoke (this, args);

        // Framework dispatch: composite views delegate commands to a target SubView.
        if (!args.Handled)
        {
            args.Handled = TryDispatchToTarget (ctx);
        }

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle Activate bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleUp (ctx, args.Handled) is true;
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
    protected internal void RaiseActivated (ICommandContext? ctx)
    {
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

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
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        if (RaiseHandlingHotKey (ctx) is true)
        {
            // The hotkey was cancelled by OnHandlingHotKey or HandlingHotKey event.
            // Return false so the key is not consumed and can be processed as normal input
            // (e.g. text input in a TextField whose HotKey matches the character being typed).
            return false;
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

        return true;
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
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Invoking HandlingHotKey event");
        HandlingHotKey?.Invoke (this, args);

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleUp (ctx, args.Handled) is true;
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

    #region Dispatch (Composite Pattern)

    /// <summary>
    ///     Gets the SubView to dispatch commands to. Return <see langword="null"/> to skip dispatch.
    ///     The framework calls this during <see cref="RaiseActivating"/>/<see cref="RaiseAccepting"/>
    ///     after the <c>OnActivating</c> virtual and <c>OnAccepting</c> event have had a chance to cancel.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Override this in composite views that delegate commands to a primary SubView.
    ///         For example, <c>Shortcut</c> returns <c>CommandView</c> and selectors return <c>Focused</c>.
    ///     </para>
    ///     <para>
    ///         The framework guards against dispatch when:
    ///         <list type="bullet">
    ///             <item>Routing is <see cref="CommandRouting.DispatchingDown"/> (prevents re-entry)</item>
    ///             <item>No binding exists on the context (programmatic invocation — skip dispatch)</item>
    ///             <item>The binding source is within the target (prevents loops)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context.</param>
    /// <returns>The SubView to dispatch to, or <see langword="null"/> to skip dispatch.</returns>
    protected virtual View? GetDispatchTarget (ICommandContext? ctx) => null;

    /// <summary>
    ///     If <see langword="true"/>, dispatching to the target consumes the command, preventing the
    ///     original SubView from completing its own activation/acceptance. If <see langword="false"/>
    ///     (default), the dispatch is a relay and the original SubView completes normally.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, the composite view owns the state mutation. The framework
    ///         marks the command as handled after dispatch, stopping the originator. The composite's
    ///         <c>OnActivated</c>/<c>OnAccepted</c> fires to perform the mutation.
    ///     </para>
    ///     <para>
    ///         When <see langword="false"/> (relay), the command is dispatched to the target but not
    ///         consumed. The originator continues its own activation/acceptance.
    ///     </para>
    /// </remarks>
    protected virtual bool ConsumeDispatch => false;

    /// <summary>
    ///     Tracks whether a dispatch occurred during the last <see cref="RaiseActivating"/> or
    ///     <see cref="RaiseAccepting"/> call. Used by <see cref="DefaultActivateHandler"/> and
    ///     <see cref="DefaultAcceptHandler"/> to determine whether to call <c>RaiseActivated</c>/<c>RaiseAccepted</c>.
    /// </summary>
    private bool _lastDispatchOccurred;

    /// <summary>
    ///     Attempts to dispatch the command to the <see cref="GetDispatchTarget"/> view.
    ///     For <see cref="ConsumeDispatch"/>=false (relay), performs a <see cref="BubbleDown"/>.
    ///     For <see cref="ConsumeDispatch"/>=true (consume), marks the command as handled without dispatching.
    /// </summary>
    /// <returns><see langword="true"/> if the command was consumed (ConsumeDispatch=true and dispatch conditions met).</returns>
    private bool TryDispatchToTarget (ICommandContext? ctx)
    {
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        _lastDispatchOccurred = false;

        View? target = GetDispatchTarget (ctx);

        if (target is null)
        {
            return false;
        }

        // Guard: don't dispatch if already dispatching down (prevents re-entry)
        if (ctx?.Routing == CommandRouting.DispatchingDown)
        {
            return false;
        }

        // Guard: for relay dispatch (ConsumeDispatch=false), don't dispatch for programmatic
        // invocations (no binding). This prevents accidental loops in composite views like Shortcut.
        // For consume dispatch (ConsumeDispatch=true), programmatic invocations DO dispatch because
        // the composite view forwards commands to the focused SubView.
        if (!ConsumeDispatch && ctx?.Binding is null)
        {
            return false;
        }

        if (ConsumeDispatch)
        {
            // Consume pattern (OptionSelector, FlagSelector).
            // When a SubView's command bubbles up (BubblingUp), consume without dispatching.
            // The composite handles state mutation in OnActivated/OnAccepted.
            // For programmatic/direct invocations, forward to the target via BubbleDown
            // so the target gets activated (matching the old BubbleDown-in-OnActivating behavior).
            if (ctx?.Routing != CommandRouting.BubblingUp)
            {
                BubbleDown (target, ctx);
            }

            _lastDispatchOccurred = true;

            return true;
        }

        // Relay pattern (Shortcut): dispatch to target if source is not within target.
        if (IsSourceWithinView (target, ctx))
        {
            return false;
        }
        BubbleDown (target, ctx);
        _lastDispatchOccurred = true;

        return false;
    }

    /// <summary>
    ///     Checks whether the binding source of the given context is within the specified view
    ///     (i.e., is the view itself or a descendant).
    /// </summary>
    private static bool IsSourceWithinView (View target, ICommandContext? ctx)
    {
        if (ctx?.Binding?.Source is not { } weakSource || !weakSource.TryGetTarget (out View? source))
        {
            return false;
        }

        View? current = source;

        while (current is { })
        {
            if (current == target)
            {
                return true;
            }

            current = current.SuperView;
        }

        return false;
    }

    #endregion Dispatch (Composite Pattern)

    #region Command Bubbling

    /// <summary>
    ///     Gets or sets the default accept view for this View. The default accept view will have <see cref="Command.Accept"/>
    ///     invoked on it
    ///     anytime a peer View raises <see cref="Command.Accept"/> and the event is not handled, or if
    ///     <see cref="Command.Accept"/> is invoked directly on this View.
    /// </summary>
    /// <remarks>
    ///     This is used to implement the common pattern of
    ///     having an "OK" button that accepts the dialog when the user presses Enter or clicks the button, without having to
    ///     set up explicit bindings for each control in the dialog that should trigger the "OK" button's Accept behavior.
    /// </remarks>
    public View? DefaultAcceptView { get => field ?? GetSubViews (includePadding: true).FirstOrDefault (v => v is IAcceptTarget { IsDefault: true }); set; }

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
    ///     <see cref="CommandContext"/> with <see cref="ICommandContext.Routing"/> set to
    ///     <see cref="CommandRouting.DispatchingDown"/>,
    ///     which causes <see cref="TryBubbleUp"/> to skip bubbling on the target, preventing re-entry.
    /// </summary>
    /// <param name="target">The SubView to dispatch the command to.</param>
    /// <param name="ctx">The original command context, used to determine the command and source.</param>
    /// <returns>
    ///     The result of invoking the command on the target.
    /// </returns>
    protected bool? BubbleDown (View target, ICommandContext? ctx)
    {
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        CommandContext downCtx = new (ctx?.Command ?? Command.NotBound, ctx?.Source, ctx?.Binding) { Routing = CommandRouting.DispatchingDown };

        return target.InvokeCommand (downCtx.Command, downCtx);
    }

    /// <summary>
    ///     Bubbles a command to the SuperView if the command is in SuperView's <see cref="CommandsToBubbleUp"/> list.
    ///     Handles the special case of invoking <see cref="Command.Accept"/> on a peer IsDefault button.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Bubbling is a <b>notification</b>, not a consumption. The SuperView's handler is invoked, but its
    ///         return value is ignored — this method always returns <see langword="false"/> after a successful bubble.
    ///         This ensures the originating view can complete its own processing (e.g., a CheckBox can toggle,
    ///         a Shortcut can raise Activated) without being blocked by the SuperView returning <see langword="true"/>.
    ///     </para>
    ///     <para>
    ///         To cancel a SubView's command, subscribe to the SubView's <c>Activating</c>/<c>Accepting</c> event
    ///         and set <c>Handled = true</c> — that guard runs <b>before</b> <see cref="TryBubbleUp"/> is called.
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context to pass along.</param>
    /// <param name="handled">Whether the command was already handled by this View.</param>
    /// <returns>
    ///     <see langword="true"/> if the command was already handled locally.
    ///     <see langword="false"/> if the command was not handled (including after a successful bubble).
    /// </returns>
    protected bool? TryBubbleUp (ICommandContext? ctx, bool handled)
    {
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}, {handled})");

        if (handled)
        {
            return true;
        }

        if (ctx is null || ctx.Routing == CommandRouting.DispatchingDown)
        {
            return false;
        }

        CommandContext? upCtx;

        if (ctx.Command == Command.Accept)
        {
            // Check this view's DefaultAcceptView first (for when Accept is invoked directly on this view),
            // then check SuperView's DefaultAcceptView (for when Accept bubbles up from a subview)
            View? isDefaultView = DefaultAcceptView ?? SuperView?.DefaultAcceptView;

            // Get the source view to determine how to handle the redirect
            View? source = null;
            ctx.Source?.TryGetTarget (out source);

            if (isDefaultView is { } && isDefaultView != this && isDefaultView != source)
            {
                if (source is IAcceptTarget acceptTarget)
                {
                    // Non-default IAcceptTarget sources bubble up to SuperView
                    // so it can determine which accept target was activated
                    if (acceptTarget.IsDefault)
                    {
                        return false;
                    }

                    upCtx = new CommandContext (Command.Accept, ctx.Source, ctx.Binding) { Routing = CommandRouting.BubblingUp };

                    // DefaultAcceptView redirect is a special case — it IS a consumption (not just a notification)
                    return SuperView?.InvokeCommand (Command.Accept, upCtx) is true;

                    // Default IAcceptTarget source - let it flow normally without redirect
                }
            }
        }

        // Check if SuperView wants this command bubbled up to it
        if (SuperView?.CommandsToBubbleUp.Contains (ctx.Command) == true)
        {
            // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");
            upCtx = new CommandContext (ctx.Command, ctx.Source, ctx.Binding) { Routing = CommandRouting.BubblingUp };

            return SuperView.InvokeCommand (ctx.Command, upCtx);
        }

        if (SuperView is Padding padding && padding.Parent?.CommandsToBubbleUp.Contains (ctx.Command) == true)
        {
            // Check if Padding's Parent wants this command bubbled up to it
            // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");
            upCtx = new CommandContext (ctx.Command, ctx.Source, ctx.Binding) { Routing = CommandRouting.BubblingUp };

            return padding.Parent.InvokeCommand (ctx.Command, upCtx);
        }

        if (this is not Padding selfPadding || selfPadding.Parent?.CommandsToBubbleUp.Contains (ctx.Command) != true)
        {
            return handled;
        }

        // Handle when THIS view is a Padding
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");
        upCtx = new CommandContext (ctx.Command, ctx.Source, ctx.Binding) { Routing = CommandRouting.BubblingUp };

        return selfPadding.Parent.InvokeCommand (ctx.Command, upCtx);
    }

    #endregion Command Bubbling
}
