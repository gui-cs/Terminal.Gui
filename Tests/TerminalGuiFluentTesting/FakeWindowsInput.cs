
namespace TerminalGuiFluentTesting;

internal class FakeWindowsInput (CancellationToken hardStopToken) : FakeInput<WindowsConsole.InputRecord> (hardStopToken), IWindowsInput
{ }
