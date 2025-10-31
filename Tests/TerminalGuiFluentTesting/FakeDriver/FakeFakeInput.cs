namespace TerminalGuiFluentTesting;

internal class FakeFakeInput (CancellationToken hardStopToken) : FakeConsoleInput<ConsoleKeyInfo> (hardStopToken), IFakeInput
{ }
