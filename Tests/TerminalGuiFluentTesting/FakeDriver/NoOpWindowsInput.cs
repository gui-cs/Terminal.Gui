
namespace TerminalGuiFluentTesting;

internal class NoOpWindowsInput (CancellationToken hardStopToken) : NoOpConsoleInput<WindowsConsole.InputRecord> (hardStopToken), IWindowsConsoleInput
{ }
