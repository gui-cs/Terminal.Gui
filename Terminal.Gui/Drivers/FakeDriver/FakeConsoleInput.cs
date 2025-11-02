#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implements a fake console input for testing purposes.
/// </summary>
public class FakeConsoleInput : ConsoleInputImpl<ConsoleKeyInfo>
{
    /// <summary>
    /// Creates a new FakeConsoleInput.
    /// </summary>
    public FakeConsoleInput ()
    { }

    /// <inheritdoc/>
    protected override bool Peek ()
    {
        return false;
    }

    /// <inheritdoc/>
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        return [];
    }
}
