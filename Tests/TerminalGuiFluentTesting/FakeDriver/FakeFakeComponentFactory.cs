namespace TerminalGuiFluentTesting;

#pragma warning disable CS1591
internal class FakeFakeComponentFactory (NoOpFakeInput fakeInput, FakeConsoleOutput output, ConsoleSizeMonitorImpl fakeSizeMonitor) : FakeComponentFactory
{
    /// <inheritdoc/>
    public override IConsoleInput<ConsoleKeyInfo> CreateInput () { return fakeInput; }

    /// <inheritdoc/>
    public override IConsoleOutput CreateOutput () { return output; }

    /// <inheritdoc/>
    public override IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return fakeSizeMonitor;
    }
}
