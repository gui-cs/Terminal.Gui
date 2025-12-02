#nullable enable
using UnitTests;
using Xunit.Abstractions;

namespace DriverTests;

public class DriverTests (ITestOutputHelper output) : FakeDriverBase
{
    [Theory]
    [InlineData ("", true)]
    [InlineData ("a", true)]
    [InlineData ("👩‍❤️‍💋‍👨", false)]
    public void IsValidLocation (string text, bool positive)
    {
        IDriver driver = CreateFakeDriver ();
        driver.SetScreenSize (10, 10);

        // positive
        Assert.True (driver.IsValidLocation (text, 0, 0));
        Assert.True (driver.IsValidLocation (text, 1, 1));
        Assert.Equal (positive, driver.IsValidLocation (text, driver.Cols - 1, driver.Rows - 1));

        // negative
        Assert.False (driver.IsValidLocation (text, -1, 0));
        Assert.False (driver.IsValidLocation (text, 0, -1));
        Assert.False (driver.IsValidLocation (text, -1, -1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows));

        // Define a clip rectangle
        driver.Clip = new (new Rectangle (5, 5, 5, 5));

        // positive
        Assert.True (driver.IsValidLocation (text, 5, 5));
        Assert.Equal (positive, driver.IsValidLocation (text, 9, 9));

        // negative
        Assert.False (driver.IsValidLocation (text, 4, 5));
        Assert.False (driver.IsValidLocation (text, 5, 4));
        Assert.False (driver.IsValidLocation (text, 10, 9));
        Assert.False (driver.IsValidLocation (text, 9, 10));
        Assert.False (driver.IsValidLocation (text, -1, 0));
        Assert.False (driver.IsValidLocation (text, 0, -1));
        Assert.False (driver.IsValidLocation (text, -1, -1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows));

        driver.End ();
    }

    [Theory]
    [InlineData ("fake")]
    [InlineData ("windows")]
    [InlineData ("dotnet")]
    [InlineData ("unix")]
    public void All_Drivers_Init_Dispose_Cross_Platform (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        app.Dispose ();
    }

    [Theory]
    [InlineData ("fake")]
    [InlineData ("windows")]
    [InlineData ("dotnet")]
    [InlineData ("unix")]
    public void All_Drivers_Run_Cross_Platform (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        app.StopAfterFirstIteration = true;
        app.Run<Runnable<bool>> ();
        app.Dispose ();
    }

    [Theory]
    [InlineData ("fake")]
    [InlineData ("windows")]
    [InlineData ("dotnet")]
    [InlineData ("unix")]
    public void All_Drivers_LayoutAndDraw_Cross_Platform (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        app.StopAfterFirstIteration = true;
        app.Run<TestTop> ();

        DriverAssert.AssertDriverContentsWithFrameAre (driverName!, output, app.Driver);

        app.Dispose ();
    }

    [Fact]
    public void IsVirtualTerminal_Returns_Expected_Values ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);

        driver.IsVirtualTerminal = false;
        Assert.False (driver.IsVirtualTerminal);
    }

    [Fact]
    public void IsVirtualTerminal_True_Force16Colors_True_False ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);
        Assert.False (driver.Force16Colors);

        driver.Force16Colors = true;
        Assert.True (driver.IsVirtualTerminal);
        Assert.True (driver.Force16Colors);
    }

    [Fact]
    public void IsVirtualTerminal_False_Force16Colors_Is_Always_True ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);
        Assert.False (driver.Force16Colors);

        driver.IsVirtualTerminal = false;
        Assert.True (driver.Force16Colors);

        driver.Force16Colors = false;
        Assert.True (driver.Force16Colors);
    }

    [Fact]
    public void IsVirtualTerminal_True_False_SupportsTrueColor_Is_Always_True_False ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);
        Assert.True (driver.SupportsTrueColor);

        driver.IsVirtualTerminal = false;
        Assert.False (driver.SupportsTrueColor);
    }
}

public class TestTop : Runnable
{
    /// <inheritdoc/>
    public override void BeginInit ()
    {
        Text = Driver!.GetName ()!;
        BorderStyle = LineStyle.None;
        base.BeginInit ();
    }
}
