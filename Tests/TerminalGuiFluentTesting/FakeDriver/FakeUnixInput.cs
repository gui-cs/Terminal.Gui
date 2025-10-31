
namespace TerminalGuiFluentTesting;

internal class FakeUnixInput (CancellationToken hardStopToken) : FakeInput<char> (hardStopToken), IUnixInput
{ }
