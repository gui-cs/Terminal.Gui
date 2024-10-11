#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Event arguments for <see cref="Command"/> events.
/// </summary>
public class CommandEventArgs : CancelEventArgs
{
    /// <summary>
    ///     The context for the command.
    /// </summary>
    public CommandContext Context { get; init; }
}
