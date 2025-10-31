#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for win32 only I/O.
/// This factory creates instances of internal classes <see cref="WindowsConsoleInput"/>, <see cref="WindowsConsoleOutput"/> etc.
/// </summary>
public class WindowsComponentFactory : ComponentFactoryImpl<WindowsConsole.InputRecord>
{
    /// <inheritdoc />
    public override IConsoleInput<WindowsConsole.InputRecord> CreateInput ()
    {
        return new WindowsConsoleInput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<WindowsConsole.InputRecord> inputBuffer)
    {
        return new WindowsInputProcessor (inputBuffer);
    }

    /// <inheritdoc />
    public override IConsoleOutput CreateOutput ()
    {
        return new WindowsConsoleOutput ();
    }
}
