using Moq;

namespace DriverTests.Ansi;

// Copilot

/// <summary>
///     Tests for <see cref="AnsiComponentFactory.CreateSizeMonitor"/> to verify the correct
///     <see cref="ISizeMonitor"/> implementation is selected based on <see cref="Driver.SizeDetection"/>
///     and whether an injected monitor is present.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiComponentFactorySizeMonitorTests
{
    /// <summary>
    ///     When a size monitor is provided to the constructor it must be returned as-is,
    ///     regardless of <see cref="Driver.SizeDetection"/>.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_InjectedMonitor_IsReturnedDirectly ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        Mock<ISizeMonitor> injected = new ();

        AnsiComponentFactory factory = new (null, output, injected.Object);

        ISizeMonitor result = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

        Assert.Same (injected.Object, result);
    }

    /// <summary>
    ///     When <see cref="SizeDetectionMode.AnsiQuery"/> is active (the default),
    ///     <see cref="AnsiComponentFactory.CreateSizeMonitor"/> should return an
    ///     <see cref="AnsiSizeMonitor"/> for an <see cref="AnsiOutput"/> argument.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_AnsiQuery_ReturnsAnsiSizeMonitor ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            Driver.SizeDetection = SizeDetectionMode.AnsiQuery;

            AnsiOutput output = new ();
            output.SetSize (80, 25);

            AnsiComponentFactory factory = new ();

            ISizeMonitor result = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

            Assert.IsType<AnsiSizeMonitor> (result);
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }

    /// <summary>
    ///     When <see cref="SizeDetectionMode.Polling"/> is active,
    ///     <see cref="AnsiComponentFactory.CreateSizeMonitor"/> should return a
    ///     <see cref="SizeMonitorImpl"/> and configure <see cref="AnsiOutput.NativeSizeQuery"/>
    ///     so that <c>GetSize()</c> queries the OS instead of returning the stale cache.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_Polling_ReturnsSizeMonitorImpl_AndSetsNativeSizeQuery ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            Driver.SizeDetection = SizeDetectionMode.Polling;

            AnsiOutput output = new ();
            output.SetSize (80, 25);

            AnsiComponentFactory factory = new ();

            ISizeMonitor result = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

            Assert.IsType<SizeMonitorImpl> (result);

            // NativeSizeQuery must have been wired up so GetSize() can query the OS.
            Assert.NotNull (output.NativeSizeQuery);
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }

    /// <summary>
    ///     In <see cref="SizeDetectionMode.Polling"/> mode the <c>NativeSizeQuery</c> delegate
    ///     causes <see cref="AnsiOutput.GetSize"/> to return the OS-provided size rather than the
    ///     stale 80×25 cache, so <see cref="SizeMonitorImpl"/> correctly detects terminal resizes.
    /// </summary>
    [Fact]
    public void Polling_NativeSizeQuery_OverridesStaleCache ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25); // cached, stale

        // Simulate OS reporting 120×40
        Size fakeOsSize = new (120, 40);
        output.NativeSizeQuery = () => fakeOsSize;

        Assert.Equal (fakeOsSize, output.GetSize ());
    }

    /// <summary>
    ///     Verifies that <see cref="AnsiComponentFactory.CreateNativeSizeQuery"/> returns a callable
    ///     delegate (non-null) on every supported platform.
    /// </summary>
    [Fact]
    public void CreateNativeSizeQuery_ReturnsNonNullDelegate ()
    {
        Func<Size?> query = AnsiComponentFactory.CreateNativeSizeQuery ();

        Assert.NotNull (query);

        // The delegate must be callable without throwing in a test environment.
        // It may return null when there is no real terminal, and that is fine.
        Size? size = null;
        Exception? ex = Record.Exception (() => { size = query (); });
        Assert.Null (ex);
    }

    /// <summary>
    ///     Validates the full pipeline: in <see cref="SizeDetectionMode.Polling"/> mode,
    ///     the <see cref="SizeMonitorImpl"/> wrapping the <see cref="AnsiOutput"/> fires
    ///     <see cref="ISizeMonitor.SizeChanged"/> when the OS size changes.
    /// </summary>
    [Fact]
    public void Polling_SizeMonitorImpl_FiresSizeChanged_WhenNativeSizeChanges ()
    {
        // Test the SizeMonitorImpl+AnsiOutput pipeline directly with a controllable NativeSizeQuery.
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        // Wire up a fake native size query that starts at 80x25.
        Size reportedSize = new (80, 25);
        output.NativeSizeQuery = () => reportedSize;

        // Constructor captures current size so first Poll() is a no-op.
        SizeMonitorImpl monitor = new (output);

        List<SizeChangedEventArgs> events = [];
        monitor.SizeChanged += (_, e) => events.Add (e);

        // First poll: size unchanged (80x25) → no event.
        monitor.Poll ();
        Assert.Empty (events);

        // Simulate a terminal resize reported by the OS.
        reportedSize = new Size (120, 40);

        monitor.Poll ();

        Assert.Single (events);
        Assert.Equal (new Size (120, 40), events [0].Size);
    }

    /// <summary>
    ///     In <see cref="SizeDetectionMode.AnsiQuery"/> mode the injected-monitor code path
    ///     is still respected — injected monitors are always returned regardless of mode.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_InjectedMonitor_WinsOverMode ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            foreach (SizeDetectionMode mode in Enum.GetValues<SizeDetectionMode> ())
            {
                Driver.SizeDetection = mode;

                AnsiOutput output = new ();
                output.SetSize (80, 25);

                Mock<ISizeMonitor> injected = new ();

                AnsiComponentFactory factory = new (null, output, injected.Object);

                ISizeMonitor result = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

                Assert.Same (injected.Object, result);
            }
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }
}
