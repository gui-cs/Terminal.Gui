#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// <see cref="IComponentFactory{T}"/> implementation for fake/mock console I/O used in unit tests.
/// This factory creates instances that simulate console behavior without requiring a real terminal.
/// </summary>
public class FakeComponentFactory : ComponentFactory<ConsoleKeyInfo>
{
    private readonly FakeConsoleInput<ConsoleKeyInfo>? _predefinedInput;
    private readonly FakeOutput? _output;

    /// <summary>
    /// Creates a new FakeComponentFactory with optional predefined input and output capture.
    /// </summary>
    /// <param name="predefinedInput">Optional queue of predefined input events to simulate.</param>
    /// <param name="output">Optional fake output to capture what would be written to console.</param>
    public FakeComponentFactory (FakeConsoleInput<ConsoleKeyInfo>? predefinedInput = null, FakeOutput? output = null)
    {
        _predefinedInput = predefinedInput;
        _output = output;
    }

    /// <inheritdoc/>
    public override IConsoleInput<ConsoleKeyInfo> CreateInput ()
    {
        return new FakeInput (_predefinedInput);
    }

    /// <inheritdoc />
    public override IConsoleOutput CreateOutput ()
    {
        return _output ?? new FakeOutput ();
    }

    /// <inheritdoc />
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer)
    {
        return new NetInputProcessor (inputBuffer);
    }

    /// <inheritdoc />
    public override IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return new ConsoleSizeMonitor (consoleOutput);
    }
}
