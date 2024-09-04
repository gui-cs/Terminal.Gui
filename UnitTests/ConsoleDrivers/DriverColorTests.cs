#nullable enable

// Alias Console to MockConsole so we don't accidentally use Console

using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests;

[Trait("Category", "Color")]
[Trait("Category", "Console Drivers")]
public class DriverColorTests
{
    public DriverColorTests () { ConsoleDriver.RunningUnitTests = true; }

    [Theory]
    [MemberData (nameof (TestHelpers.GetDriversOnly), [""], MemberType = typeof (TestHelpers))]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    public void Force16Colors_Sets<T> (T driver) where T : ConsoleDriver
    {
        driver.Init ();

        driver.Force16Colors = true;
        Assert.True (driver.Force16Colors);

        driver.End ();
    }

    [Theory]
    [MemberData (nameof (TestHelpers.GetDriversOnly), [""], MemberType = typeof (TestHelpers))]
    public void SetColors_Changes_Colors<T> (T driver) where T : ConsoleDriver
    {
        driver.Init ();

        Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
        Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);

        Console.ForegroundColor = ConsoleColor.Red;
        Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);

        Console.BackgroundColor = ConsoleColor.Green;
        Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);

        Console.ResetColor ();
        Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
        Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);

        driver.End ();
    }

    [Theory]
    [MemberData (nameof (TestHelpers.DriversAndTrueColorSupport), MemberType = typeof (TestHelpers))]
    [Trait("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    public void SupportsTrueColor_Defaults<T> (T driver, bool expectedSetting) where T : ConsoleDriver
    {
        driver.Init ();

        Assert.Equal (expectedSetting, driver.SupportsTrueColor);

        driver.End ();
    }
}
