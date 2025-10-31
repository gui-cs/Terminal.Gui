namespace TerminalGuiFluentTesting;

#pragma warning disable CS1591
internal class FakeFakeComponentFactory (FakeFakeConsoleInput fakeInput, FakeOutput output, ConsoleSizeMonitor fakeSizeMonitor) : FakeComponentFactory
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
