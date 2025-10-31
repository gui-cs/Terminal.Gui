
namespace TerminalGuiFluentTesting;

internal class FakeUnixInput (CancellationToken hardStopToken) : FakeConsoleInput<char> (hardStopToken), IUnixInput
{ }
