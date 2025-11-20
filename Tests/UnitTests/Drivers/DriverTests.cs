#nullable enable
using Xunit.Abstractions;

namespace UnitTests.DriverTests;

public class DriverTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData ("fake")]
    [InlineData ("windows")]
    [InlineData ("dotnet")]
    [InlineData ("unix")]
    public void All_Drivers_Init_Shutdown_Cross_Platform (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        app.Shutdown ();
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
        app.Run ().Dispose ();
        app.Shutdown ();
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
        app.Run<TestTop> ().Dispose ();

        DriverAssert.AssertDriverContentsWithFrameAre (driverName!, _output, app.Driver);

        app.Shutdown ();
    }
}

public class TestTop : Toplevel
{
    /// <inheritdoc/>
    public override void BeginInit ()
    {
        Text = Driver!.GetName ()!;
        BorderStyle = LineStyle.None;
        base.BeginInit ();
    }
}
