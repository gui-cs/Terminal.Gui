using System.Drawing;
using TerminalGuiFluentTesting;

namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
public class FakeApplicationFactory
{
    /// <summary>
    ///     Creates an initialized fake application which will be cleaned up when result object
    ///     is disposed.
    /// </summary>
    /// <returns></returns>
    public IDisposable SetupFakeApplication ()
    {
        var cts = new CancellationTokenSource ();
        var fakeInput = new FakeNetInput (cts.Token);
        FakeOutput output = new ();
        output.SetSize (80, 25);

        IApplication origApp = ApplicationImpl.Instance;

        ConsoleSizeMonitor sizeMonitor = new (output);

        var impl = new ApplicationImpl (new FakeNetComponentFactory (fakeInput, output, sizeMonitor));

        ApplicationImpl.ChangeInstance (impl);

        // Initialize with a fake driver
        impl.Init (null, "fake");

        // Handle different facade types - cast to common interface instead
        var d = (IConsoleDriverFacade)Application.Driver!;

        sizeMonitor.SizeChanged += (_, e) =>
                                   {
                                       if (e.Size != null)
                                       {
                                           Size s = e.Size.Value;
                                           output.SetSize (s.Width, s.Height);
                                           d.OutputBuffer.SetSize (s.Width, s.Height);
                                       }
                                   };

        return new FakeApplicationLifecycle (origApp, cts);
    }
}
