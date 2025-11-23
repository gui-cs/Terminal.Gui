using System.Drawing;
using TerminalGuiFluentTesting;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Provides methods to create and manage a fake application for testing purposes.
/// </summary>
public class FakeApplicationFactory
{
    /// <summary>
    ///     Creates an initialized fake application which will be cleaned up when result object
    ///     is disposed.
    /// </summary>
    /// <returns></returns>
    public IDisposable SetupFakeApplication ()
    {
        CancellationTokenSource hardStopTokenSource = new CancellationTokenSource ();
        FakeInput fakeInput = new FakeInput ();
        fakeInput.ExternalCancellationTokenSource = hardStopTokenSource;
        FakeOutput output = new ();
        output.SetSize (80, 25);

        SizeMonitorImpl sizeMonitor = new (output);

        ApplicationImpl impl = new (new FakeComponentFactory (fakeInput, output, sizeMonitor));
        ApplicationImpl.SetInstance (impl);

        // Initialize with a fake driver
        impl.Init ("fake");

        return new FakeApplicationLifecycle (hardStopTokenSource);
    }
}
