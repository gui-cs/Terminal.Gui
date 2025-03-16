using System.Collections.Concurrent;
using System.Drawing;
using Terminal.Gui;

namespace TerminalGuiFluentAssertions;

class FakeInput<T> : IConsoleInput<T>
{
    /// <inheritdoc />
    public void Dispose () { }

    /// <inheritdoc />
    public void Initialize (ConcurrentQueue<T> inputBuffer) { }

    /// <inheritdoc />
    public void Run (CancellationToken token)
    {
        // Simulate an infinite loop that checks for cancellation
        token.WaitHandle.WaitOne (); // Blocks until the token is cancelled
    }
}

class FakeNetInput : FakeInput<ConsoleKeyInfo>, INetInput
{

}

class FakeWindowsInput : FakeInput<WindowsConsole.InputRecord>, IWindowsInput
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
public class GuiTestContext<T> where T : Toplevel, new()
{
    private readonly CancellationTokenSource _cts;
    private readonly Task _runTask;

    internal GuiTestContext (int width, int height)
    {
        IApplication origApp = ApplicationImpl.Instance;

        var netInput = new FakeNetInput ();
        var winInput = new FakeWindowsInput ();
        var output = new FakeOutput ();

        output.Size = new (width, height);

        var v2 = new ApplicationV2(
                                    () => netInput,
                                    ()=>output,
                                    () => winInput,
                                    () => output);

        // Create a cancellation token
        _cts = new ();

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
                                 {

                                 }
                                 finally
                                 {
                                     ApplicationImpl.ChangeInstance (origApp);
                                 }
                             }, _cts.Token);

        Application.Shutdown ();
    }

    /// <summary>
    /// Stops the application and waits for the background thread to exit.
    /// </summary>
    public void Stop ()
    {
        _cts.Cancel ();
        Application.Invoke (()=>Application.RequestStop());
        _runTask.Wait (); // Ensure the background thread exits
    }

    // Cleanup to avoid state bleed between tests
    public void Dispose ()
    {
    }
}

