using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IComponentFactory{T}"/> implementation for fake/mock console I/O used in unit tests.
///     This factory creates instances that simulate console behavior without requiring a real terminal.
/// </summary>
public class FakeComponentFactory : ComponentFactoryImpl<ConsoleKeyInfo>
{
    private readonly FakeInput? _input;
    private readonly IOutput? _output;
    private readonly ISizeMonitor? _sizeMonitor;

    /// <summary>
    ///     Creates a new FakeComponentFactory with optional output capture.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output">Optional fake output to capture what would be written to console.</param>
    /// <param name="sizeMonitor"></param>
    public FakeComponentFactory (FakeInput? input = null, IOutput? output = null, ISizeMonitor? sizeMonitor = null)
    {
        _input = input;
        _output = output;
        _sizeMonitor = sizeMonitor;
    }


    /// <inheritdoc/>
    public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return _sizeMonitor ?? new SizeMonitorImpl (consoleOutput);
    }

    /// <inheritdoc/>
    public override IInput<ConsoleKeyInfo> CreateInput ()
    {
        return _input ?? new FakeInput ();
    }

    /// <inheritdoc/>
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) { return new FakeInputProcessor (inputBuffer); }

    /// <inheritdoc/>
    public override IOutput CreateOutput ()
    {
        return _output ?? new FakeOutput ();
    }
}
