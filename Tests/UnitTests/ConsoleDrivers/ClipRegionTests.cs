using System.Text;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.DriverTests;

public class ClipRegionTests
{
    private readonly ITestOutputHelper _output;

    public ClipRegionTests (ITestOutputHelper output)
    {
        ConsoleDriver.RunningUnitTests = true;
        this._output = output;
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void AddRune_Is_Clipped (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        Application.Init (driver);
        Application.Driver!.Rows = 25;
        Application.Driver!.Cols = 80;

        driver.Move (0, 0);
        driver.AddRune ('x');
        Assert.Equal ((Rune)'x', driver.Contents [0, 0].Rune);

        driver.Move (5, 5);
        driver.AddRune ('x');
        Assert.Equal ((Rune)'x', driver.Contents [5, 5].Rune);

        // Clear the contents
        driver.FillRect (new Rectangle (0, 0, driver.Rows, driver.Cols), ' ');
        Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);

        // Setup the region with a single rectangle, fill screen with 'x'
        driver.Clip = new (new Rectangle (5, 5, 5, 5));
        driver.FillRect (new Rectangle (0, 0, driver.Rows, driver.Cols), 'x');
        Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
        Assert.Equal ((Rune)' ', driver.Contents [4, 9].Rune);
        Assert.Equal ((Rune)'x', driver.Contents [5, 5].Rune);
        Assert.Equal ((Rune)'x', driver.Contents [9, 9].Rune);
        Assert.Equal ((Rune)' ', driver.Contents [10, 10].Rune);

        Application.Shutdown ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void Clip_Set_To_Empty_AllInvalid (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        Application.Init (driver);

        // Define a clip rectangle
        driver.Clip = new (Rectangle.Empty);

        // negative
        Assert.False (driver.IsValidLocation (default, 4, 5));
        Assert.False (driver.IsValidLocation (default, 5, 4));
        Assert.False (driver.IsValidLocation (default, 10, 9));
        Assert.False (driver.IsValidLocation (default, 9, 10));
        Assert.False (driver.IsValidLocation (default, -1, 0));
        Assert.False (driver.IsValidLocation (default, 0, -1));
        Assert.False (driver.IsValidLocation (default, -1, -1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows));

        Application.Shutdown ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void IsValidLocation (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        Application.Init (driver);
        Application.Driver!.Rows = 10;
        Application.Driver!.Cols = 10;

        // positive
        Assert.True (driver.IsValidLocation (default, 0, 0));
        Assert.True (driver.IsValidLocation (default, 1, 1));
        Assert.True (driver.IsValidLocation (default, driver.Cols - 1, driver.Rows - 1));

        // negative
        Assert.False (driver.IsValidLocation (default, -1, 0));
        Assert.False (driver.IsValidLocation (default, 0, -1));
        Assert.False (driver.IsValidLocation (default, -1, -1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows));

        // Define a clip rectangle
        driver.Clip = new(new Rectangle(5, 5, 5, 5));

        // positive
        Assert.True (driver.IsValidLocation (default, 5, 5));
        Assert.True (driver.IsValidLocation (default, 9, 9));

        // negative
        Assert.False (driver.IsValidLocation (default, 4, 5));
        Assert.False (driver.IsValidLocation (default, 5, 4));
        Assert.False (driver.IsValidLocation (default, 10, 9));
        Assert.False (driver.IsValidLocation (default, 9, 10));
        Assert.False (driver.IsValidLocation (default, -1, 0));
        Assert.False (driver.IsValidLocation (default, 0, -1));
        Assert.False (driver.IsValidLocation (default, -1, -1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (default, driver.Cols, driver.Rows));

        Application.Shutdown ();
    }
}
