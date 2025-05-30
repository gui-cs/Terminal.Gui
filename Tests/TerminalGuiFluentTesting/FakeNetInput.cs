using Terminal.Gui;
using Terminal.Gui.Drivers;

namespace TerminalGuiFluentTesting;

internal class FakeNetInput (CancellationToken hardStopToken) : FakeInput<ConsoleKeyInfo> (hardStopToken), INetInput
{ }
