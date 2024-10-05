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
        AddCommand (Command.Accept, RaiseAccepted);

        // HotKey - SetFocus and raise HotKeyHandled
        AddCommand (Command.HotKey,
                    () =>
                    {
                        SetFocus ();

                        return RaiseHotKeyHandled ();
                    });

        // Space or single-click - Raise Selected
        AddCommand (Command.Select, () =>
                                    {
                                        bool? cancelled = RaiseSelected ();
                                        if (cancelled is null or false && CanFocus)
                                        {
                                            SetFocus ();

                                            return true;
                                        }

                                        return cancelled is true;
                                    });
    }

    /// <summary>
    ///     Called when the View's state has been accepted by the user. Calls <see cref="OnAccepted"/> which can be cancelled; if not cancelled raises <see cref="Accepted"/>.
    ///     event. The default <see cref="Command.Accept"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Accepted"/> event should raised after the state of the View has changed (after <see cref="Selected"/> is raised).
    /// </remarks>
    /// <returns>
    ///     If <see langword="true"/> the event was canceled. If <see langword="false"/> the event was raised but not canceled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseAccepted ()
    {
        HandledEventArgs args = new ();

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        args.Handled = OnAccepted (args) || args.Handled;

        if (!args.Handled)
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            Accepted?.Invoke (this, args);
        }

        // Accept is a special case where if the event is not canceled, the event is
        //  - Invoked on any peer-View with IsDefault == true
        //  - bubbled up the SuperView hierarchy.
        if (!args.Handled)
        {
            // If there's an IsDefault peer view in Subviews, try it
            var isDefaultView = SuperView?.Subviews.FirstOrDefault (v => v is Button { IsDefault: true });

            if (isDefaultView != this && isDefaultView is Button { IsDefault: true } button)
            {
                bool? handled = isDefaultView.InvokeCommand (Command.Accept);
                if (handled == true)
                {
                    return true;
                }
            }

            return SuperView?.InvokeCommand (Command.Accept) == true;
        }

        return Accepted is null ? null : args.Handled;
    }

    // TODO: Change this to CancelEventArgs
    /// <summary>
    ///     Called when the View's state has been accepted by the user. Set <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/> to stop processing.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnAccepted (HandledEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the View's state has been accepted by the user. Set
    ///     <see cref="HandledEventArgs.Handled"/> to cancel the event.
    /// </summary>
    public event EventHandler<HandledEventArgs>? Accepted;

    /// <summary>
    ///     Called when the user has selected the View or otherwise changed the state of the View. Calls <see cref="OnSelected"/> which can be cancelled; if not cancelled raises <see cref="Accepted"/>.
    ///     event. The default <see cref="Command.Select"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Selected"/> event should raised after the state of the View has been changed and before see <see cref="Accepted"/>.
    /// </remarks>
    /// <returns>
    ///     If <see langword="true"/> the event was canceled. If <see langword="false"/> the event was raised but not canceled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseSelected ()
    {
        HandledEventArgs args = new ();

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnSelected (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        Selected?.Invoke (this, args);

        return Selected is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when the user has selected the View or otherwise changed the state of the View. Set <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/> to stop processing.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnSelected (HandledEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the user has selected the View or otherwise changed the state of the View. Set
    ///     <see cref="HandledEventArgs.Handled"/>
    ///     to cancel the event.
    /// </summary>
    public event EventHandler<HandledEventArgs>? Selected;


    // TODO: What does this event really do? "Called when the user has pressed the View's hot key or otherwise invoked the View's hot key command.???"
    /// <summary>
    ///     Called when the View has handled the user pressing the View's <see cref="HotKey"/>. Calls <see cref="OnHotKeyHandled"/> which can be cancelled; if not cancelled raises <see cref="Accepted"/>.
    ///     event. The default <see cref="Command.HotKey"/> handler calls this method.
    /// </summary>
    /// <returns>
    ///     If <see langword="true"/> the event was handled. If <see langword="false"/> the event was raised but not handled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseHotKeyHandled ()
    {
        HandledEventArgs args = new ();

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnHotKeyHandled (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        HotKeyHandled?.Invoke (this, args);

        return HotKeyHandled is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when the View has handled the user pressing the View's <see cref="HotKey"/>. Set <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/> to stop processing.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnHotKeyHandled (HandledEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the <see cref="Command.HotKey"/> command is invoked. Set
    ///     <see cref="HandledEventArgs.Handled"/>
    ///     to cancel the event.
    /// </summary>
    public event EventHandler<HandledEventArgs>? HotKeyHandled;

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
}
