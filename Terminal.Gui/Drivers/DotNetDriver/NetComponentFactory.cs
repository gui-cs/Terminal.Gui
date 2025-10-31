#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for native csharp console I/O i.e. dotnet.
/// This factory creates instances of internal classes <see cref="NetConsoleInput"/>, <see cref="NetConsoleOutput"/> etc.
/// </summary>
public class NetComponentFactory : ComponentFactoryImpl<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    public override IConsoleInput<ConsoleKeyInfo> CreateInput ()
    {
        return new NetConsoleInput ();
    }

    /// <inheritdoc />
    public override IConsoleOutput CreateOutput ()
    {
        return new NetConsoleOutput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer)
    {
        return new NetInputProcessor (inputBuffer);
    }
}
