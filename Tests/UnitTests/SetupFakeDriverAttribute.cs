using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;

namespace UnitTests;

/// <summary>
///     Enables test functions annotated with the [SetupFakeDriver] attribute to set Application.Driver to new
///     FakeDriver(). The driver is set up with 25 rows and columns.
/// </summary>
/// <remarks>
///     On Before, sets Configuration.Locations to ConfigLocations.DefaultOnly.
///     On After, sets Configuration.Locations to ConfigLocations.All.
/// </remarks>
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class SetupFakeDriverAttribute : BeforeAfterTestAttribute
{
    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        // Turn off diagnostic flags in case some test left them on
        View.Diagnostics = ViewDiagnosticFlags.Off;

        Application.ResetState (true);
        Assert.Null (Application.Driver);
        Assert.Equal (new (0, 0, 2048, 2048), Application.Screen);
        base.After (methodUnderTest);
    }

    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        Application.ResetState (true);
        Assert.Null (Application.Driver);
        Application.Driver = new FakeDriver { Rows = 25, Cols = 25 };
        Assert.Equal (new (0, 0, 25, 25), Application.Screen);
        // Ensures subscribing events, at least for the SizeChanged event
        Application.SubscribeDriverEvents ();

        base.Before (methodUnderTest);
    }
}