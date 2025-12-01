using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for win32 only I/O.
/// This factory creates instances of internal classes <see cref="WindowsInput"/>, <see cref="WindowsOutput"/> etc.
/// </summary>
public class WindowsComponentFactory : ComponentFactoryImpl<WindowsConsole.InputRecord>
{
    /// <inheritdoc />
    public override IInput<WindowsConsole.InputRecord> CreateInput ()
    {
        return new WindowsInput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<WindowsConsole.InputRecord> inputBuffer)
    {
        return new WindowsInputProcessor (inputBuffer);
    }

    /// <inheritdoc />
    public override IOutput CreateOutput ()
    {
        return new WindowsOutput ();
    }
}
