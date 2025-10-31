namespace TerminalGuiFluentTesting;

#pragma warning disable CS1591
internal class FakeWindowsComponentFactory (FakeWindowsInput winInput, FakeOutput output, ConsoleSizeMonitor fakeSizeMonitor)
    : WindowsComponentFactory
{
    /// <inheritdoc/>
    public override IConsoleInput<WindowsConsole.InputRecord> CreateInput () { return winInput; }

    /// <inheritdoc/>
    public override IConsoleOutput CreateOutput () { return output; }

    /// <inheritdoc/>
    public override IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return fakeSizeMonitor;
    }
}
