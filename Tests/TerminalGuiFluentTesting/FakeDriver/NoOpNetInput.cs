namespace TerminalGuiFluentTesting;

internal class NoOpNetInput (CancellationToken hardStopToken) : NoOpConsoleInput<ConsoleKeyInfo> (hardStopToken)
{ }
