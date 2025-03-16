using System.Collections.Concurrent;
using System.Drawing;
using FluentAssertions;
using FluentAssertions.Numeric;
using Terminal.Gui;
using Terminal.Gui.ConsoleDrivers;
using static Unix.Terminal.Curses;

namespace TerminalGuiFluentAssertions;

class FakeInput<T>(CancellationToken hardStopToken)  : IConsoleInput<T>
{
    /// <inheritdoc />
    public void Dispose () { }

    /// <inheritdoc />
    public void Initialize (ConcurrentQueue<T> inputBuffer) { InputBuffer = inputBuffer;}

    public ConcurrentQueue<T> InputBuffer { get; set; }

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
    public IOutputBuffer LastBuffer { get; set; }
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
        LastBuffer = buffer;
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
    private readonly FakeWindowsInput winInput;
    private View _lastView;

    internal GuiTestContext (int width, int height)
    {
        IApplication origApp = ApplicationImpl.Instance;

        var netInput = new FakeNetInput (_cts.Token);
        winInput = new FakeWindowsInput (_cts.Token);

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

                                     v2.Init (null,"v2win");

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

        WaitIteration ();
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
                           _lastView = v;
                       });

        return this;
    }

    public GuiTestContext<T> ResizeConsole (int width, int height)
    {
        _output.Size = new Size (width,height);

        return WaitIteration ();
    }
    public GuiTestContext<T> ScreenShot (string title, TextWriter writer)
    {
        writer.WriteLine(title);
        var text = Application.ToString ();

        writer.WriteLine(text);

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

    public GuiTestContext<T> RightClick (int screenX, int screenY)
    {
        return Click (WindowsConsole.ButtonState.Button3Pressed,screenX, screenY);
    }

    public GuiTestContext<T> LeftClick (int screenX, int screenY)
    {
        return Click (WindowsConsole.ButtonState.Button1Pressed, screenX, screenY);
    }

    private GuiTestContext<T> Click (WindowsConsole.ButtonState btn, int screenX, int screenY)
    {
        winInput.InputBuffer.Enqueue (new WindowsConsole.InputRecord ()
        {
            EventType = WindowsConsole.EventType.Mouse,
            MouseEvent = new WindowsConsole.MouseEventRecord ()
            {
                ButtonState = btn,
                MousePosition = new WindowsConsole.Coord ((short)screenX, (short)screenY)
            }
        });

        winInput.InputBuffer.Enqueue (new WindowsConsole.InputRecord ()
        {
            EventType = WindowsConsole.EventType.Mouse,
            MouseEvent = new WindowsConsole.MouseEventRecord ()
            {
                ButtonState = WindowsConsole.ButtonState.NoButtonPressed,
                MousePosition = new WindowsConsole.Coord ((short)screenX, (short)screenY)
            }
        });

        WaitIteration ();

        return this;
    }

    public GuiTestContext<T> Down ()
    {
        winInput.InputBuffer.Enqueue (new WindowsConsole.InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord
            {
                bKeyDown = true,
                wRepeatCount = 0,
                wVirtualKeyCode = ConsoleKeyMapping.VK.DOWN,
                wVirtualScanCode = 0,
                UnicodeChar = '\0',
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed
            }
        });

        winInput.InputBuffer.Enqueue (new WindowsConsole.InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord
            {
                bKeyDown = false,
                wRepeatCount = 0,
                wVirtualKeyCode = ConsoleKeyMapping.VK.DOWN,
                wVirtualScanCode = 0,
                UnicodeChar = '\0',
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed
            }
        });


        WaitIteration ();

        return this;
    }
    public GuiTestContext<T> Enter ()
    {
        winInput.InputBuffer.Enqueue (new WindowsConsole.InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord
            {
                bKeyDown = true,
                wRepeatCount = 0,
                wVirtualKeyCode = ConsoleKeyMapping.VK.RETURN,
                wVirtualScanCode = 0,
                UnicodeChar = '\0',
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed
            }
        });

        winInput.InputBuffer.Enqueue (new WindowsConsole.InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord
            {
                bKeyDown = false,
                wRepeatCount = 0,
                wVirtualKeyCode = ConsoleKeyMapping.VK.RETURN,
                wVirtualScanCode = 0,
                UnicodeChar = '\0',
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed
            }
        });

        WaitIteration ();

        return this;
    }
    public GuiTestContext<T> WithContextMenu (ContextMenu ctx, MenuBarItem menuItems)
    {
        LastView.MouseEvent += (s, e) =>
                               {
                                   if (e.Flags.HasFlag (MouseFlags.Button3Clicked))
                                   {
                                       ctx.Show (menuItems);
                                   }
                               };

        return this;
    }

    public View LastView  => _lastView ?? Application.Top ?? throw new Exception ("Could not determine which view to add to");


}

