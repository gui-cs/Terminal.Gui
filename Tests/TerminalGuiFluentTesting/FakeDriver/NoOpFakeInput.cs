namespace TerminalGuiFluentTesting;

internal class NoOpFakeInput (CancellationToken hardStopToken) : NoOpConsoleInput<ConsoleKeyInfo> (hardStopToken)
{ }
