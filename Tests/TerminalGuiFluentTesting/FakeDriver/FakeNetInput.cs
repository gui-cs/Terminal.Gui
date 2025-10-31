namespace TerminalGuiFluentTesting;

internal class FakeNetInput (CancellationToken hardStopToken) : FakeConsoleInput<ConsoleKeyInfo> (hardStopToken), INetInput
{ }
