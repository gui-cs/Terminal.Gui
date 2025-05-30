using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.Drivers.FakeConsole;

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
    [InlineData (typeof (FakeDriver))]

    //[InlineData (typeof (NetDriver))]
    //[InlineData (typeof (ANSIDriver))]
    //[InlineData (typeof (WindowsDriver))]
    //[InlineData (typeof (CursesDriver))]
    public void Left_And_Top_Is_Always_Zero (Type driverType)
    {
        var driver = (FakeDriver)Activator.CreateInstance (driverType);
        Application.Init (driver);

        Assert.Equal (0, Console.WindowLeft);
        Assert.Equal (0, Console.WindowTop);

        driver.SetWindowPosition (5, 5);
        Assert.Equal (0, Console.WindowLeft);
        Assert.Equal (0, Console.WindowTop);

        Application.Shutdown ();
    }
}
