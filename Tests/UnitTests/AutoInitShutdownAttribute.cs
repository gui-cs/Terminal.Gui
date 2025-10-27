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
    ///     Determines which IConsoleDriver (FakeDriver, WindowsDriver, UnixDriver, DotNetDriver)
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
        _driverType = consoleDriverType;
        FakeDriver.FakeBehaviors.UseFakeClipboard = useFakeClipboard;
        FakeDriver.FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException =
            fakeClipboardAlwaysThrowsNotSupportedException;
        FakeDriver.FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse = fakeClipboardIsSupportedAlwaysTrue;
        _verifyShutdown = verifyShutdown;
    }

    private readonly bool _verifyShutdown;
    private readonly Type _driverType;
    private IDisposable _v2Cleanup;

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest?.Name ?? "Unknown Test"}");

        // Turn off diagnostic flags in case some test left them on
        View.Diagnostics = ViewDiagnosticFlags.Off;

        _v2Cleanup?.Dispose ();

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
        Debug.WriteLine ($"Before: {methodUnderTest?.Name ?? "Unknown Test"}");

        Debug.Assert (!CM.IsEnabled, "A previous test left ConfigurationManager enabled!");

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
            if (_driverType == null)
            {
                Application.Top = null;
                Application.TopLevels.Clear ();

                var fa = new FakeApplicationFactory ();
                _v2Cleanup = fa.SetupFakeApplication ();
                AutoInitShutdownAttribute.FakeResize (new Size (80,25));
            }
            else
            {
                Application.Init ((IConsoleDriver)Activator.CreateInstance (_driverType));
            }
        }
    }

    private bool AutoInit { get; }

    /// <summary>
    ///     Simulates a terminal resize in tests that use <see cref="AutoInitShutdownAttribute"/>.
    ///     This method updates the driver's output buffer size, triggers size change events,
    ///     and forces a layout/draw cycle.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is designed for use in unit tests to simulate terminal resize events.
    ///         It works with both the fluent testing infrastructure (<see cref="FakeSizeMonitor"/>) and
    ///         the library's built-in <see cref="FakeWindowSizeMonitor"/>.
    ///     </para>
    ///     <para>
    ///         The method performs the following operations:
    ///         <list type="number">
    ///             <item>Updates the output buffer size via <see cref="IOutputBuffer.SetWindowSize"/></item>
    ///             <item>Raises the size changing event through the appropriate size monitor</item>
    ///             <item>Forces a layout and draw cycle via <see cref="Application.LayoutAndDraw"/></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong> This method is not thread-safe. Tests using FakeResize
    ///         should not run in parallel if they share driver state.
    ///     </para>
    ///     <para>
    ///         <strong>Requirements:</strong> Your test must use <see cref="AutoInitShutdownAttribute"/>
    ///         with autoInit=true for this method to work correctly.
    ///     </para>
    /// </remarks>
    /// <param name="size">The new terminal size (width, height).</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <see cref="Application.Driver"/> is null or not initialized.
    /// </exception>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     [AutoInitShutdown]
    ///     public void TestResize()
    ///     {
    ///         // Initial size is 80x25
    ///         Assert.Equal(80, Application.Driver.Cols);
    ///         
    ///         // Simulate resize to 120x30
    ///         AutoInitShutdownAttribute.FakeResize(new Size(120, 30));
    ///         
    ///         // Verify new size
    ///         Assert.Equal(120, Application.Driver.Cols);
    ///         Assert.Equal(30, Application.Driver.Rows);
    ///     }
    ///     </code>
    /// </example>
    public static void FakeResize (Size size)
    {
        var d = (IConsoleDriverFacade)Application.Driver!;
        d.OutputBuffer.SetWindowSize (size.Width, size.Height);
        
        // Handle both FakeSizeMonitor (from test project) and FakeWindowSizeMonitor (from main library)
        if (d.WindowSizeMonitor is FakeSizeMonitor fakeSizeMonitor)
        {
            fakeSizeMonitor.RaiseSizeChanging (size);
        }
        else if (d.WindowSizeMonitor is FakeWindowSizeMonitor fakeWindowSizeMonitor)
        {
            // For FakeWindowSizeMonitor, use the RaiseSizeChanging method
            fakeWindowSizeMonitor.RaiseSizeChanging (size);
        }

        Application.LayoutAndDraw (true);
    }

    /// <summary>
    /// Runs a single iteration of the main loop (layout, draw, run timed events etc.)
    /// </summary>
    public static void RunIteration ()
    {
        var a = (ApplicationImpl)ApplicationImpl.Instance;
        a.Coordinator?.RunIteration ();
    }
}