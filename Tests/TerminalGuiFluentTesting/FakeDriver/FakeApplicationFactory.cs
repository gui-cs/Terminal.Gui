#nullable enable
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
        output.Size = new (25, 25);

        IApplication origApp = ApplicationImpl.Instance;

        var sizeMonitor = new FakeSizeMonitor ();

        var impl = new ApplicationImpl (new FakeNetComponentFactory (fakeInput, output, sizeMonitor));

        ApplicationImpl.ChangeInstance (impl);
        impl.Init (null, "dotnet");

        // Handle different facade types - cast to common interface instead
        var d = (IConsoleDriverFacade)Application.Driver!;

        sizeMonitor.SizeChanging += (_, e) =>
                                    {
                                        if (e.Size != null)
                                        {
                                            Size s = e.Size.Value;
                                            output.Size = s;
                                            d.OutputBuffer.SetWindowSize (s.Width, s.Height);
                                        }
                                    };

        return new FakeApplicationLifecycle (origApp, cts);
    }
}
