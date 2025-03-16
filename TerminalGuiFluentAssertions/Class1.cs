using System.Collections.Concurrent;
using System.Drawing;
using FluentAssertions;
using FluentAssertions.Numeric;
using Terminal.Gui;

namespace TerminalGuiFluentAssertions;

class FakeInput<T>(CancellationToken hardStopToken)  : IConsoleInput<T>
{
    /// <inheritdoc />
    public void Dispose () { }

    /// <inheritdoc />
    public void Initialize (ConcurrentQueue<T> inputBuffer) { }

    /// <inheritdoc />
    public void Run (CancellationToken token)
    {
        // Blocks until either the token or the hardStopToken is cancelled.
        WaitHandle.WaitAny (new [] { token.WaitHandle, hardStopToken.WaitHandle });
    }
}

class FakeNetInput (CancellationToken hardStopToken) : FakeInput<ConsoleKeyInfo> (hardStopToken), INetInput
{

}

class FakeWindowsInput (CancellationToken hardStopToken) : FakeInput<WindowsConsole.InputRecord> (hardStopToken), IWindowsInput
{

}

class FakeOutput : IConsoleOutput
{
    public Size Size { get; set; }

    /// <inheritdoc />
    public void Dispose ()
    {

    }

    /// <inheritdoc />
    public void Write (ReadOnlySpan<char> text)
    {

    }

    /// <inheritdoc />
    public void Write (IOutputBuffer buffer)
    {

    }

    /// <inheritdoc />
    public Size GetWindowSize ()
    {
        return Size;
    }

    /// <inheritdoc />
    public void SetCursorVisibility (CursorVisibility visibility)
    {

    }

    /// <inheritdoc />
    public void SetCursorPosition (int col, int row)
    {

    }

}
/// <summary>
/// Entry point to fluent assertions.
/// </summary>
public static class With
{
    /// <summary>
    /// Entrypoint to fluent assertions
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static GuiTestContext<T> A<T> (int width, int height) where T : Toplevel, new ()
    {
        return new GuiTestContext<T> (width,height);
    }
}
public class GuiTestContext<T> : IDisposable where T : Toplevel, new()
{
    private readonly CancellationTokenSource _cts = new ();
    private readonly CancellationTokenSource _hardStop = new ();
    private readonly Task _runTask;
    private Exception _ex;
    private readonly FakeOutput _output = new ();

    internal GuiTestContext (int width, int height)
    {
        IApplication origApp = ApplicationImpl.Instance;

        var netInput = new FakeNetInput (_cts.Token);
        var winInput = new FakeWindowsInput (_cts.Token);

        _output.Size = new (width, height);

        var v2 = new ApplicationV2(
                                    () => netInput,
                                    ()=>_output,
                                    () => winInput,
                                    () => _output);


        // Start the application in a background thread
        _runTask = Task.Run (() =>
                             {
                                 try
                                 {
                                     ApplicationImpl.ChangeInstance (v2);

                                     v2.Init ();

                                     Application.Run<T> (); // This will block, but it's on a background thread now

                                     Application.Shutdown ();
                                 }
                                 catch (OperationCanceledException)
                                 { }
                                 catch (Exception ex)
                                 {
                                     _ex = ex;
                                 }
                                 finally
                                 {
                                     ApplicationImpl.ChangeInstance (origApp);
                                 }
                             }, _cts.Token);
    }

    /// <summary>
    /// Stops the application and waits for the background thread to exit.
    /// </summary>
    public GuiTestContext<T> Stop ()
    {
        if (_runTask.IsCompleted)
        {
            return this;
        }

        Application.Invoke (()=> Application.RequestStop ());

        // Wait for the application to stop, but give it a 1-second timeout
        if (!_runTask.Wait (TimeSpan.FromMilliseconds (1000)))
        {
            _cts.Cancel ();
            // Timeout occurred, force the task to stop
            _hardStop.Cancel ();
            throw new TimeoutException ("Application failed to stop within the allotted time.");
        }
        _cts.Cancel ();

        if (_ex != null)
        {
            throw _ex; // Propagate any exception that happened in the background task
        }

        return this;
    }

    // Cleanup to avoid state bleed between tests
    public void Dispose ()
    {
        Stop ();
        _hardStop.Cancel();
    }

    /// <summary>
    /// Adds the given <paramref name="v"/> to the current top level view
    /// and performs layout.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public GuiTestContext<T> Add (View v)
    {
        WaitIteration (
                       () =>
                       {
                           var top = Application.Top ?? throw new Exception("Top was null so could not add view");
                           top.Add (v);
                           top.Layout ();
                       });

        return this;
    }

    public GuiTestContext<T> ResizeConsole (int width, int height)
    {
        _output.Size = new Size (width,height);

        return WaitIteration ();
    }
    public GuiTestContext<T> WaitIteration (Action? a = null)
    {
        a ??= () => { };
        var ctsLocal = new CancellationTokenSource ();


        Application.Invoke (()=>
                            {
                                a();
                                ctsLocal.Cancel ();
                            });

        // Blocks until either the token or the hardStopToken is cancelled.
        WaitHandle.WaitAny (new []
        {
            _cts.Token.WaitHandle,
            _hardStop.Token.WaitHandle,
            ctsLocal.Token.WaitHandle
        });
        return this;
    }

    public GuiTestContext<T> Assert<T2> (AndConstraint<T2> be)
    {
        return this;
    }
}

