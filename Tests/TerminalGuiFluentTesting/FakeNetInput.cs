using Terminal.Gui;

namespace TerminalGuiFluentTesting;

internal class FakeNetInput (CancellationToken hardStopToken) : FakeInput<ConsoleKeyInfo> (hardStopToken), INetInput
{ }
