using UnitTests.Parallelizable;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.Drivers.FakeConsole;

namespace UnitTests_Parallelizable.ConsoleDriverTests;

public class DriverTests : ParallelizableBase
{

}
