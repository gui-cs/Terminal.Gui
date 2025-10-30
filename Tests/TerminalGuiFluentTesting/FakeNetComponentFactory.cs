namespace TerminalGuiFluentTesting;

#pragma warning disable CS1591
internal class FakeNetComponentFactory (FakeNetInput netInput, FakeOutput output, ConsoleSizeMonitor fakeSizeMonitor) : NetComponentFactory
{
    /// <inheritdoc/>
    public override IConsoleInput<ConsoleKeyInfo> CreateInput () { return netInput; }

    /// <inheritdoc/>
    public override IConsoleOutput CreateOutput () { return output; }

    /// <inheritdoc/>
    public override IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return fakeSizeMonitor;
    }
}
