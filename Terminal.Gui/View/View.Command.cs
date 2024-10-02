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
        AddCommand (Command.Accept, RaiseAcceptEvent);

        AddCommand (
                    Command.HotKey,
                    () =>
                    {
                        SetFocus ();

                        return RaiseHotKeyCommandEvent ();
                    });

        AddCommand (Command.Select, RaiseSelectEvent);
    }

    /// <summary>
    ///     Called when the <see cref="Command.Accept"/> command is invoked. Raises <see cref="Accept"/>
    ///     event.
    /// </summary>
    /// <returns>
    ///     If <see langword="true"/> the event was canceled. If <see langword="false"/> the event was raised but not canceled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseAcceptEvent ()
    {
        HandledEventArgs args = new ();

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        args.Handled = OnAccept (args) || args.Handled;

        if (!args.Handled)
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            Accept?.Invoke (this, args);
        }

        // Accept is a special case where if the event is not canceled, the event is bubbled up the SuperView hierarchy.
        if (!args.Handled)
        {
            return SuperView?.InvokeCommand (Command.Accept) == true;
        }

        return Accept is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when the <see cref="Command.Accept"/> command is received. Set <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/> to stop processing.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnAccept (HandledEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the <see cref="Command.Accept"/> command is invoked. Set
    ///     <see cref="HandledEventArgs.Handled"/>
    ///     to cancel the event.
    /// </summary>
    public event EventHandler<HandledEventArgs>? Accept;

    /// <summary>
    ///     Called when the <see cref="Command.Select"/> command is invoked. Raises <see cref="Select"/>
    ///     event.
    /// </summary>
    /// <returns>
    ///     If <see langword="true"/> the event was canceled. If <see langword="false"/> the event was raised but not canceled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseSelectEvent ()
    {
        HandledEventArgs args = new ();

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnSelect (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        Select?.Invoke (this, args);

        return Select is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when the <see cref="Command.Select"/> command is received. Set <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/> to stop processing.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnSelect (HandledEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the <see cref="Command.Select"/> command is invoked. Set
    ///     <see cref="HandledEventArgs.Handled"/>
    ///     to cancel the event.
    /// </summary>
    public event EventHandler<HandledEventArgs>? Select;

    /// <summary>
    ///     Called when the <see cref="Command.HotKey"/> command is invoked. Raises <see cref="HotKey"/>
    ///     event.
    /// </summary>
    /// <returns>
    ///     If <see langword="true"/> the event was handled. If <see langword="false"/> the event was raised but not handled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? RaiseHotKeyCommandEvent ()
    {
        HandledEventArgs args = new ();

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnHotKeyCommand (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        HotKeyCommand?.Invoke (this, args);

        return HotKeyCommand is null ? null : args.Handled;
    }

    /// <summary>
    ///     Called when the <see cref="Command.HotKey"/> command is received. Set <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/> to stop processing.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnHotKeyCommand (HandledEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the <see cref="Command.HotKey"/> command is invoked. Set
    ///     <see cref="HandledEventArgs.Handled"/>
    ///     to cancel the event.
    /// </summary>
    public event EventHandler<HandledEventArgs>? HotKeyCommand;

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
    ///     <see langword="true"/> if the command was invoked the command was handled.
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
    ///     handled the command. <see langword="false"/> if the command was invoked, and it did not handle the command.
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
