
namespace TerminalGuiFluentTesting;

internal class NoOpUnixInput (CancellationToken hardStopToken) : NoOpConsoleInput<char> (hardStopToken), IUnixConsoleInput
{ }
