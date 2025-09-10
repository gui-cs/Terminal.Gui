#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for win32 windows only I/O i.e. v2win.
/// This factory creates instances of internal classes <see cref="WindowsInput"/>, <see cref="WindowsOutput"/> etc.
/// </summary>
public class WindowsComponentFactory : ComponentFactory<WindowsConsole.InputRecord>
{
    /// <inheritdoc />
    public override IConsoleInput<WindowsConsole.InputRecord> CreateInput ()
    {
        return new WindowsInput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<WindowsConsole.InputRecord> inputBuffer)
    {
        return new WindowsInputProcessor (inputBuffer);
    }

    /// <inheritdoc />
    public override IConsoleOutput CreateOutput ()
    {
        return new WindowsOutput ();
    }
}
