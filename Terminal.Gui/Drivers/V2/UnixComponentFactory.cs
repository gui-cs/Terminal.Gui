#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for native unix console I/O i.e. v2unix.
/// This factory creates instances of internal classes <see cref="UnixInput"/>, <see cref="UnixOutput"/> etc.
/// </summary>
public class UnixComponentFactory : ComponentFactory<char>
{
    /// <inheritdoc />
    public override IConsoleInput<char> CreateInput ()
    {
        return new UnixInput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer)
    {
        return new UnixInputProcessor (inputBuffer);
    }

    /// <inheritdoc />
    public override IConsoleOutput CreateOutput ()
    {
        return new UnixOutput ();
    }
}
