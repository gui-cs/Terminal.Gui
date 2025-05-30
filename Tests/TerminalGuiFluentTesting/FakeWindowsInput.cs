using Terminal.Gui;
using Terminal.Gui.Drivers;

namespace TerminalGuiFluentTesting;

internal class FakeWindowsInput (CancellationToken hardStopToken) : FakeInput<WindowsConsole.InputRecord> (hardStopToken), IWindowsInput
{ }
