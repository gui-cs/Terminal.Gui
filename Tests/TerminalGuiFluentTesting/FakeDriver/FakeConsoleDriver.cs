#nullable enable
using System.Collections.Concurrent;
using System.Drawing;
using TerminalGuiFluentTesting;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IConsoleDriver"/> that uses fake input/output.
///     This is a lightweight alternative to <see cref="GuiTestContext"/> (if you don't
///     need the entire application main loop running).
/// </summary>
internal class FakeConsoleDriver : ConsoleDriverFacade<ConsoleKeyInfo>, IFakeConsoleDriver
{
    internal FakeConsoleDriver (
        ConcurrentQueue<ConsoleKeyInfo> inputBuffer,
        OutputBuffer outputBuffer,
        FakeOutput fakeOutput,
        Func<DateTime> datetimeFunc,
        FakeSizeMonitor sizeMonitor
    ) :
        base (
              new NetInputProcessor (inputBuffer),
              outputBuffer,
              fakeOutput,
              new (new AnsiResponseParser (), datetimeFunc),
              sizeMonitor)
    {
        FakeOutput fakeOutput1;
        InputBuffer = inputBuffer;
        SizeMonitor = sizeMonitor;
        OutputBuffer = outputBuffer;
        ConsoleOutput = fakeOutput1 = fakeOutput;

        SizeChanged += (_, e) =>
                       {
                           if (e.Size != null)
                           {
                               Size s = e.Size.Value;
                               fakeOutput1.Size = s;
                               OutputBuffer.SetWindowSize (s.Width, s.Height);
                           }
                       };
    }

    public void SetBufferSize (int width, int height)
    {
        SizeMonitor.RaiseSizeChanging (new (width, height));
        OutputBuffer.SetWindowSize (width, height);
    }

    public IConsoleOutput ConsoleOutput { get; }
    public ConcurrentQueue<ConsoleKeyInfo> InputBuffer { get; }
    public new OutputBuffer OutputBuffer { get; }
    public FakeSizeMonitor SizeMonitor { get; }
}