using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IComponentFactory{T}"/> implementation for fake/mock console I/O used in unit tests.
///     This factory creates instances that simulate console behavior without requiring a real terminal.
/// </summary>
/// <remarks>
///     <para>
///         The Fake driver demonstrates proper use of <see cref="AnsiResponseParser"/> for
///         querying terminal capabilities via ANSI escape sequences. It showcases:
///     </para>
///     <list type="bullet">
///         <item>Sending ANSI queries (e.g., <see cref="EscSeqUtils.CSI_ReportWindowSizeInChars"/>)</item>
///         <item>Registering response expectations with <see cref="AnsiResponseParser"/></item>
///         <item>Handling responses asynchronously through callbacks</item>
///         <item>Coordinating between input (response parsing) and output (query sending)</item>
///     </list>
/// </remarks>
public class FakeComponentFactory : ComponentFactoryImpl<char>
{
    /// <inheritdoc/>
    public override string? GetDriverName () => DriverRegistry.Names.FAKE;

    private readonly FakeInput? _input;
    private readonly IOutput? _output;
    private FakeSizeMonitor? _createdSizeMonitor;

    /// <summary>
    ///     Creates a new FakeComponentFactory with optional output capture.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output">Optional fake output to capture what would be written to console.</param>
    /// <param name="sizeMonitor">Optional size monitor (if null, will create FakeSizeMonitor)</param>
    public FakeComponentFactory (FakeInput? input = null, IOutput? output = null, ISizeMonitor? sizeMonitor = null)
    {
        _input = input;
        _output = output;
        _createdSizeMonitor = sizeMonitor as FakeSizeMonitor;
    }


    /// <inheritdoc/>
    public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        if (consoleOutput is FakeOutput fakeOutput)
        {
            // Create FakeSizeMonitor - the ANSI request callback will be set up
            // by MainLoopCoordinator after the driver is fully constructed
            _createdSizeMonitor = new (fakeOutput, queueAnsiRequest: null);
            return _createdSizeMonitor;
        }

        // Fallback for other output types
        return new SizeMonitorImpl (consoleOutput);
    }

    /// <inheritdoc/>
    public override IInput<char> CreateInput ()
    {
        return _input ?? new FakeInput ();
    }

    /// <inheritdoc/>
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer) { return new FakeInputProcessor (inputBuffer); }

    /// <inheritdoc/>
    public override IOutput CreateOutput ()
    {
        return _output ?? new FakeOutput ();
    }
}



