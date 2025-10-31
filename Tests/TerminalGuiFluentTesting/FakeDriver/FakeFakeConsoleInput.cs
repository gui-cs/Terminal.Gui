namespace TerminalGuiFluentTesting;

internal class FakeFakeConsoleInput (CancellationToken hardStopToken) : FakeInput<ConsoleKeyInfo> (hardStopToken), IFakeConsoleInput
{ }
