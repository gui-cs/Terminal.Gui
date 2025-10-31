#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for native unix console I/O.
/// This factory creates instances of internal classes <see cref="UnixConsoleInput"/>, <see cref="UnixConsoleOutput"/> etc.
/// </summary>
public class UnixComponentFactory : ComponentFactoryImpl<char>
{
    /// <inheritdoc />
    public override IConsoleInput<char> CreateInput ()
    {
        return new UnixConsoleInput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer)
    {
        return new UnixInputProcessor (inputBuffer);
    }

    /// <inheritdoc />
    public override IConsoleOutput CreateOutput ()
    {
        return new UnixConsoleOutput ();
    }
}
