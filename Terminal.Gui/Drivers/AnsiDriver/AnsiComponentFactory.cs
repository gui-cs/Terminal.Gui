using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IComponentFactory{T}"/> implementation for the pure ANSI Driver.
/// </summary>
/// <remarks>
///     <para>
///         The ANSI driver demonstrates proper use of <see cref="AnsiResponseParser"/> for
///         querying terminal capabilities via ANSI escape sequences. It showcases:
///     </para>
///     <list type="bullet">
///         <item>Sending ANSI queries (e.g., <see cref="EscSeqUtils.CSI_ReportWindowSizeInChars"/>)</item>
///         <item>Registering response expectations with <see cref="AnsiResponseParser"/></item>
///         <item>Handling responses asynchronously through callbacks</item>
///         <item>Coordinating between input (response parsing) and output (query sending)</item>
///     </list>
/// </remarks>
public class AnsiComponentFactory : ComponentFactoryImpl<char>
{
    /// <inheritdoc/>
    public override string? GetDriverName () => DriverRegistry.Names.ANSI;

    private readonly AnsiInput? _input;
    private readonly IOutput? _output;
    private AnsiSizeMonitor? _createdSizeMonitor;

    /// <summary>
    ///     Creates a new ANSIComponentFactory with optional output capture.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output">Optional fake output to capture what would be written to console.</param>
    /// <param name="sizeMonitor">Optional size monitor (if null, will create ANSISizeMonitor)</param>
    public AnsiComponentFactory (AnsiInput? input = null, IOutput? output = null, ISizeMonitor? sizeMonitor = null)
    {
        _input = input;
        _output = output;
        _createdSizeMonitor = sizeMonitor as AnsiSizeMonitor;
    }


    /// <inheritdoc/>
    public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        if (consoleOutput is AnsiOutput output)
        {
            // Create ANSISizeMonitor - the ANSI request callback will be set up
            // by MainLoopCoordinator after the driver is fully constructed
            _createdSizeMonitor = new (output, queueAnsiRequest: null);
            return _createdSizeMonitor;
        }

        // Fallback for other output types
        return new SizeMonitorImpl (consoleOutput);
    }

    /// <inheritdoc/>
    public override IInput<char> CreateInput ()
    {
        return _input ?? new AnsiInput ();
    }

    /// <inheritdoc/>
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer, ITimeProvider? timeProvider = null) { return new AnsiInputProcessor (inputBuffer, timeProvider); }

    /// <inheritdoc/>
    public override IOutput CreateOutput ()
    {
        return _output ?? new AnsiOutput ();
    }
}



