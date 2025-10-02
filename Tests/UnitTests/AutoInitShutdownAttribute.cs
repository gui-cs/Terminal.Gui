using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit.Sdk;

namespace UnitTests;

/// <summary>
///     Enables test functions annotated with the [AutoInitShutdown] attribute to
///     automatically call Application.Init at start of the test and Application.Shutdown after the
///     test exits.
///     This is necessary because a) Application is a singleton and Init/Shutdown must be called
///     as a pair, and b) all unit test functions should be atomic..
/// </summary>
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class AutoInitShutdownAttribute : BeforeAfterTestAttribute
{
    /// <summary>
    ///     Initializes a [AutoInitShutdown] attribute, which determines if/how Application.Init and Application.Shutdown
    ///     are automatically called Before/After a test runs.
    /// </summary>
    /// <param name="autoInit">If true, Application.Init will be called Before the test runs.</param>
    /// <param name="consoleDriverType">
    ///     Determines which IConsoleDriver (FakeDriver, WindowsDriver, CursesDriver, NetDriver)
    ///     will be used when Application.Init is called. If null FakeDriver will be used. Only valid if
    ///     <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="useFakeClipboard">
    ///     If true, will force the use of <see cref="FakeDriver.FakeClipboard"/>. Only valid if
    ///     <see cref="IConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="fakeClipboardAlwaysThrowsNotSupportedException">
    ///     Only valid if <paramref name="autoInit"/> is true. Only
    ///     valid if <see cref="IConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="fakeClipboardIsSupportedAlwaysTrue">
    ///     Only valid if <paramref name="autoInit"/> is true. Only valid if
    ///     <see cref="IConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="verifyShutdown">If true and <see cref="Application.Initialized"/> is true, the test will fail.</param>
    public AutoInitShutdownAttribute (
        bool autoInit = true,
        Type consoleDriverType = null,
        bool useFakeClipboard = true,
        bool fakeClipboardAlwaysThrowsNotSupportedException = false,
        bool fakeClipboardIsSupportedAlwaysTrue = false,
        bool verifyShutdown = false
    )
    {
        AutoInit = autoInit;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
        _driverType = consoleDriverType ?? typeof (FakeDriver);
        FakeDriver.FakeBehaviors.UseFakeClipboard = useFakeClipboard;
        FakeDriver.FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException =
            fakeClipboardAlwaysThrowsNotSupportedException;
        FakeDriver.FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse = fakeClipboardIsSupportedAlwaysTrue;
        _verifyShutdown = verifyShutdown;
    }

    private readonly bool _verifyShutdown;
    private readonly Type _driverType;

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        // Turn off diagnostic flags in case some test left them on
        View.Diagnostics = ViewDiagnosticFlags.Off;

        if (AutoInit)
        {
            // try
            {
                if (!_verifyShutdown)
                {
                    Application.ResetState (ignoreDisposed: true);
                }

                Application.Shutdown ();
#if DEBUG_IDISPOSABLE
                if (View.Instances.Count == 0)
                {
                    Assert.Empty (View.Instances);
                }
                else
                {
                    View.Instances.Clear ();
                }
#endif
            }
            //catch (Exception e)
            //{
            //    Assert.Fail ($"Application.Shutdown threw an exception after the test exited: {e}");
            //}
            //finally
            {
#if DEBUG_IDISPOSABLE
                View.Instances.Clear ();
                Application.ResetState (true);
#endif
            }
        }

        Debug.Assert (!CM.IsEnabled, "This test left ConfigurationManager enabled!");

        // Force the ConfigurationManager to reset to its hardcoded defaults
        CM.Disable(true);
    }

    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        // Disable & force the ConfigurationManager to reset to its hardcoded defaults
        CM.Disable (true);

        //Debug.Assert(!CM.IsEnabled, "Some other test left ConfigurationManager enabled.");

        if (AutoInit)
        {
#if DEBUG_IDISPOSABLE
            View.EnableDebugIDisposableAsserts = true;

            // Clear out any lingering Responder instances from previous tests
            if (View.Instances.Count == 0)
            {
                Assert.Empty (View.Instances);
            }
            else
            {
                View.Instances.Clear ();
            }
#endif
            Application.Init ((IConsoleDriver)Activator.CreateInstance (_driverType));
        }
    }

    private bool AutoInit { get; }
}