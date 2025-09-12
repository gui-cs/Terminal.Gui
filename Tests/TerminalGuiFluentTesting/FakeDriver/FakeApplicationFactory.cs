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

        var v2 = new ApplicationV2 (new FakeNetComponentFactory (fakeInput, output, sizeMonitor));

        ApplicationImpl.ChangeInstance (v2);
        v2.Init (null, "v2net");

        ConsoleDriverFacade<ConsoleKeyInfo> d = (ConsoleDriverFacade<ConsoleKeyInfo>)Application.Driver!;

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
