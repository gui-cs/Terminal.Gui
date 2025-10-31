namespace TerminalGuiFluentTesting;

#pragma warning disable CS1591
internal class FakeNetComponentFactory (NoOpNetInput input, FakeConsoleOutput output, ConsoleSizeMonitorImpl fakeSizeMonitor) : NetComponentFactory
{
    /// <inheritdoc/>
    public override IConsoleInput<ConsoleKeyInfo> CreateInput () { return input; }

    /// <inheritdoc/>
    public override IConsoleOutput CreateOutput () { return output; }

    /// <inheritdoc/>
    public override IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return fakeSizeMonitor;
    }
}
