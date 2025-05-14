using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit.Sdk;

namespace UnitTests;

/// <summary>
///     Enables test functions annotated with the [TestRespondersDisposed] attribute to ensure all Views are disposed.
/// </summary>
/// <remarks>
///     On Before, sets Configuration.Locations to ConfigLocations.DefaultOnly.
///     On After, sets Configuration.Locations to ConfigLocations.All.
/// </remarks>
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class TestRespondersDisposedAttribute : BeforeAfterTestAttribute
{
    public TestRespondersDisposedAttribute () { CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US"); }

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        Debug.Assert (!CM.IsEnabled, "This test left ConfigurationManager enabled!");

        base.After (methodUnderTest);

#if DEBUG_IDISPOSABLE
        Assert.True (View.EnableDebugIDisposableAsserts);
        Assert.Empty (View.Instances);
#endif
    }

    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        base.Before (methodUnderTest);
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        // Clear out any lingering Responder instances from previous tests
        View.Instances.Clear ();
        Assert.Empty (View.Instances);
#endif
    }
}