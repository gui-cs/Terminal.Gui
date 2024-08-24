using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests;

public class ConsoleScrollingTests
{
    private readonly ITestOutputHelper output;

    public ConsoleScrollingTests (ITestOutputHelper output)
    {
        ConsoleDriver.RunningUnitTests = true;
        this.output = output;
    }

    [Theory]
    [MemberData (
                    nameof (TestHelpers.GetDriversOnly),
                    [$"{nameof (NetDriver)},{nameof (WindowsDriver)},{nameof (CursesDriver)}"],
                    MemberType = typeof (TestHelpers))]
    public void Left_And_Top_Is_Always_Zero<T> (T driver) where T : ConsoleDriver
    {
        Application.Init (driver);

        Assert.Equal (0, Console.WindowLeft);
        Assert.Equal (0, Console.WindowTop);

        driver.SetWindowPosition (5, 5);
        Assert.Equal (0, Console.WindowLeft);
        Assert.Equal (0, Console.WindowTop);

        Application.Shutdown ();
    }
}
