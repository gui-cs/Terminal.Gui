using System.Text;
using UnitTests.ViewsTests;
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
    public void All_Drivers_Init_Shutdown_Cross_Platform (string driverName = null)
    {
        Application.Init (driverName);
        Application.Shutdown ();
    }

    [Theory]
    [InlineData ("fake")]
    [InlineData ("windows")]
    [InlineData ("dotnet")]
    [InlineData ("unix")]
    public void All_Drivers_Run_Cross_Platform (string driverName = null)
    {
        Application.Init (driverName);
        Application.StopAfterFirstIteration = true;
        Application.Run ().Dispose ();
        Application.Shutdown ();
    }

    [Theory]
    [InlineData ("fake")]
    [InlineData ("windows")]
    [InlineData ("dotnet")]
    [InlineData ("unix")]
    public void All_Drivers_LayoutAndDraw_Cross_Platform (string driverName = null)
    {
        Application.Init (driverName);
        Application.StopAfterFirstIteration = true;
        Application.Run<TestTop> ().Dispose ();

        DriverAssert.AssertDriverContentsWithFrameAre (expectedLook: driverName!, _output);

        Application.Shutdown ();

    }
}

public class TestTop : Toplevel
{
    /// <inheritdoc />
    public override void BeginInit ()
    {
        Text = Driver!.GetName ()!;
        BorderStyle = LineStyle.None;
        base.BeginInit ();
    }
}
