#nullable enable
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using TerminalGuiFluentTesting;
using Xunit.Sdk;

namespace UnitTests;

/// <summary>
///     Enables test functions annotated with the [SetupFakeDriver] attribute to set Application.Driver to new
///     FakeDriver(). The driver is set up with 80 rows and 25 columns.
/// </summary>
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class SetupFakeApplicationAttribute : BeforeAfterTestAttribute
{
    private IDisposable? _appDispose = null!;
    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        _appDispose?.Dispose();
        var appFactory = new FakeApplicationFactory ();
        _appDispose = appFactory.SetupFakeApplication ();

        base.Before (methodUnderTest);
    }

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        _appDispose?.Dispose ();
        _appDispose = null;
        Application.ResetState (true);

        base.After (methodUnderTest);
    }

    /// <summary>
    ///     Runs a single iteration of the main loop (layout, draw, run timed events etc.)
    /// </summary>
    public static void RunIteration () { ((ApplicationImpl)ApplicationImpl.Instance).Coordinator?.RunIteration (); }


}