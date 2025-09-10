#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for native csharp console I/O i.e. v2net.
/// This factory creates instances of internal classes <see cref="NetInput"/>, <see cref="NetOutput"/> etc.
/// </summary>
public class NetComponentFactory : ComponentFactory<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    public override IConsoleInput<ConsoleKeyInfo> CreateInput ()
    {
        return new NetInput ();
    }

    /// <inheritdoc />
    public override IConsoleOutput CreateOutput ()
    {
        return new NetOutput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer)
    {
        return new NetInputProcessor (inputBuffer);
    }
}
