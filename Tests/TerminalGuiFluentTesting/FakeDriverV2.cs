using System.Collections.Concurrent;
using System.Drawing;
using TerminalGuiFluentTesting;

namespace Terminal.Gui.Drivers;

public class FakeApplicationFactory
{
    /// <summary>
    /// Creates an initialized fake application which will be cleaned up when result object
    /// is disposed.
    /// </summary>
    /// <returns></returns>
    public IDisposable SetupFakeApplication ()
    {
        var cts = new CancellationTokenSource ();
        var fakeInput = new FakeNetInput (cts.Token);
        FakeOutput _output = new ();
        _output.Size = new (25, 25);


        IApplication origApp = ApplicationImpl.Instance;

        var sizeMonitor = new FakeSizeMonitor ();

        var v2 = new ApplicationV2 (new FakeNetComponentFactory (fakeInput, _output, sizeMonitor));

        ApplicationImpl.ChangeInstance (v2);
        v2.Init (null,"v2net");

        var d = (ConsoleDriverFacade<ConsoleKeyInfo>)Application.Driver;
        sizeMonitor.SizeChanging += (_, e) =>
                                           {
                                               if (e.Size != null)
                                               {
                                                   var s = e.Size.Value;
                                                   _output.Size = s;
                                                   d.OutputBuffer.SetWindowSize (s.Width, s.Height);
                                               }
                                           };

        return new FakeApplicationLifecycle (origApp,cts);
    }
}

class FakeApplicationLifecycle : IDisposable
{
    private readonly IApplication _origApp;
    private readonly CancellationTokenSource _hardStop;

    public FakeApplicationLifecycle (IApplication origApp, CancellationTokenSource hardStop)
    {
        _origApp = origApp;
        _hardStop = hardStop;
    }
    /// <inheritdoc />
    public void Dispose ()
    {
        _hardStop.Cancel();

        Application.Top?.Dispose ();
        Application.Shutdown ();
        ApplicationImpl.ChangeInstance (_origApp);
    }
}

public class FakeDriverFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="FakeDriverV2"/> using default options
    /// </summary>
    /// <returns></returns>
    public IFakeDriverV2 Create ()
    {
        return new FakeDriverV2 (
                                 new ConcurrentQueue<ConsoleKeyInfo> (),
                                 new OutputBuffer (),
                                 new FakeOutput (),
                                 () => DateTime.Now,
                                 new FakeSizeMonitor ());
    }
}

public interface IFakeDriverV2 : IConsoleDriver, IConsoleDriverFacade
{
    void SetBufferSize (int width, int height);
}

/// <summary>
/// Implementation of <see cref="IConsoleDriver"/> that uses fake input/output.
/// This is a lightweight alternative to <see cref="GuiTestContext"/> (if you don't
/// need the entire application main loop running).
/// </summary>
class FakeDriverV2 : ConsoleDriverFacade<ConsoleKeyInfo>, IFakeDriverV2
{
    public ConcurrentQueue<ConsoleKeyInfo> InputBuffer { get; }
    public FakeSizeMonitor SizeMonitor { get; }
    public OutputBuffer OutputBuffer { get; }

    public IConsoleOutput ConsoleOutput { get; }

    private FakeOutput _fakeOutput;

    internal FakeDriverV2 (
        ConcurrentQueue<ConsoleKeyInfo> inputBuffer,
        OutputBuffer outputBuffer,
        FakeOutput fakeOutput,
        Func<DateTime> datetimeFunc,
        FakeSizeMonitor sizeMonitor) :
        base (new NetInputProcessor (inputBuffer),
             outputBuffer,
             fakeOutput,
             new (new AnsiResponseParser (), datetimeFunc),
             sizeMonitor)
    {
        InputBuffer = inputBuffer;
        SizeMonitor = sizeMonitor;
        OutputBuffer = outputBuffer;
        ConsoleOutput = _fakeOutput = fakeOutput;
        SizeChanged += (_, e) =>
                       {
                           if (e.Size != null)
                           {
                               var s = e.Size.Value;
                               _fakeOutput.Size = s;
                               OutputBuffer.SetWindowSize (s.Width,s.Height);
                           }
                       };

    }

    public void SetBufferSize (int width, int height)
    {
        SizeMonitor.RaiseSizeChanging (new Size (width,height));
        OutputBuffer.SetWindowSize (width,height);
    }
}

public class FakeSizeMonitor : IWindowSizeMonitor
{
    /// <inheritdoc />
    public event EventHandler<SizeChangedEventArgs>? SizeChanging;

    /// <inheritdoc />
    public bool Poll ()
    {
        return false;
    }

    /// <summary>
    /// Raises the <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="newSize"></param>
    public void RaiseSizeChanging (Size newSize)
    {
        SizeChanging?.Invoke (this,new (newSize));
    }
}
