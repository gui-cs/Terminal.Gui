#nullable enable
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using TerminalGuiFluentTesting;
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
    private IDisposable? _appDispose = null!;

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        _appDispose?.Dispose ();
        _appDispose = null;

        base.After (methodUnderTest);
    }

    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        Assert.Null (_appDispose);

        var appFactory = new FakeApplicationFactory ();
        _appDispose = appFactory.SetupFakeApplication ();

        base.Before (methodUnderTest);
    }
}