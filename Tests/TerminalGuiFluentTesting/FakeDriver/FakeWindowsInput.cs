
namespace TerminalGuiFluentTesting;

internal class FakeWindowsInput (CancellationToken hardStopToken) : FakeConsoleInput<WindowsConsole.InputRecord> (hardStopToken), IWindowsInput
{ }
