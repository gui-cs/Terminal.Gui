namespace Terminal.Gui.Input;

/// <summary>
///     Bridges command routing across non-containment boundaries (e.g., between a <see cref="MenuBarItem"/>
///     and its <see cref="PopoverMenu"/>, which is registered with Application.Popover rather than the
///     SuperView hierarchy).
/// </summary>
/// <remarks>
///     <para>
///         The bridge subscribes to the remote view's <c>Accepted</c> and/or <c>Activated</c> events.
///         When fired, it creates a new immutable <see cref="CommandContext"/> with
///         <see cref="CommandRouting.Bridged"/> routing and invokes the command on the owner via
///         <see cref="View.InvokeCommand(Command, ICommandContext?)"/>.
///     </para>
///     <para>
///         Both references are weak — the bridge does not prevent GC of either view.
///         The bridge becomes inert if either <see cref="WeakReference{T}"/> target is collected.
///     </para>
///     <para>
///         The bridge is one-way: remote fires event → owner receives command.
///         If bidirectional routing is needed, create two bridges.
///     </para>
/// </remarks>
public class CommandBridge : IDisposable
{
    private readonly WeakReference<View> _owner;
    private readonly WeakReference<View> _remote;
    private readonly Command [] _commands;
    private bool _disposed;

    private CommandBridge (View owner, View remote, Command [] commands)
    {
        _owner = new WeakReference<View> (owner);
        _remote = new WeakReference<View> (remote);
        _commands = commands;

        // Subscribe to the remote view's completion events for bridged commands.
        if (_commands.Contains (Command.Accept))
        {
            remote.Accepted += OnRemoteAccepted;
        }

        if (_commands.Contains (Command.Activate))
        {
            remote.Activated += OnRemoteActivated;
        }
    }

    /// <summary>
    ///     Connects an owner view to a remote view for the specified commands.
    ///     When the remote view raises <c>Accepted</c> or <c>Activated</c> for any of the
    ///     specified commands, the owner view receives the command with
    ///     <see cref="CommandRouting.Bridged"/> routing.
    /// </summary>
    /// <param name="owner">The view that will receive bridged commands.</param>
    /// <param name="remote">The view whose events will be bridged.</param>
    /// <param name="commands">The commands to bridge (e.g., <c>Command.Accept</c>, <c>Command.Activate</c>).</param>
    /// <returns>A <see cref="CommandBridge"/> that can be disposed to tear down the connection.</returns>
    public static CommandBridge Connect (View owner, View remote, params Command [] commands) => new (owner, remote, commands);

    /// <summary>
    ///     Tears down event subscriptions. Safe to call multiple times.
    /// </summary>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_remote.TryGetTarget (out View? remote))
        {
            remote.Accepted -= OnRemoteAccepted;
            remote.Activated -= OnRemoteActivated;
        }
    }

    private void OnRemoteAccepted (object? sender, CommandEventArgs e)
    {
        if (_disposed || !_owner.TryGetTarget (out View? owner))
        {
            return;
        }

        CommandContext bridgedCtx = new ()
        {
            Command = Command.Accept,
            Source = e.Context?.Source,
            Binding = e.Context?.Binding,
            Routing = CommandRouting.Bridged
        };

        owner.RaiseAccepted (bridgedCtx);
    }

    private void OnRemoteActivated (object? sender, EventArgs<ICommandContext?> e)
    {
        if (_disposed || !_owner.TryGetTarget (out View? owner))
        {
            return;
        }

        CommandContext bridgedCtx = new ()
        {
            Command = Command.Activate,
            Source = e.Value?.Source,
            Binding = e.Value?.Binding,
            Routing = CommandRouting.Bridged
        };

        owner.RaiseActivated (bridgedCtx);
    }
}
