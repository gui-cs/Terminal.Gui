namespace TerminalGuiFluentTesting;

#pragma warning disable CS1591
internal class FakeUnixComponentFactory (NoOpUnixInput unixInput, FakeConsoleOutput output, ConsoleSizeMonitorImpl fakeSizeMonitor)
    : UnixComponentFactory
{
    /// <inheritdoc/>
    public override IConsoleInput<char> CreateInput () { return unixInput; }

    /// <inheritdoc/>
    public override IConsoleOutput CreateOutput () { return output; }

    /// <inheritdoc/>
    public override IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return fakeSizeMonitor;
    }
}
