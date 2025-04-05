using System.Text;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Terminal.Gui.ConsoleDrivers;

namespace TerminalGuiFluentTesting;

/// <summary>
/// Fluent API context for testing a Terminal.Gui application. Create
/// an instance using <see cref="With"/> static class.
/// </summary>
public class GuiTestContext : IDisposable
{
    private readonly CancellationTokenSource _cts = new ();
    private readonly CancellationTokenSource _hardStop = new (With.Timeout);
    private readonly Task _runTask;
    private Exception _ex;
    private readonly FakeOutput _output = new ();
    private readonly FakeWindowsInput _winInput;
    private readonly FakeNetInput _netInput;
    private View? _lastView;
    private readonly StringBuilder _logsSb;
    private readonly V2TestDriver _driver;

    internal GuiTestContext (Func<Toplevel> topLevelBuilder, int width, int height, V2TestDriver driver)
    {
        IApplication origApp = ApplicationImpl.Instance;
        ILogger? origLogger = Logging.Logger;
        _logsSb = new ();
        _driver = driver;

        _netInput = new (_cts.Token);
        _winInput = new (_cts.Token);

        _output.Size = new (width, height);

        var v2 = new ApplicationV2 (
                                    () => _netInput,
                                    () => _output,
                                    () => _winInput,
                                    () => _output);

        var booting = new SemaphoreSlim (0, 1);

        // Start the application in a background thread
        _runTask = Task.Run (
                             () =>
                             {
                                 try
                                 {
                                     ApplicationImpl.ChangeInstance (v2);

                                     ILogger logger = LoggerFactory.Create (
                                                                            builder =>
                                                                                builder.SetMinimumLevel (LogLevel.Trace)
                                                                                       .AddProvider (new TextWriterLoggerProvider (new StringWriter (_logsSb))))
                                                                   .CreateLogger ("Test Logging");
                                     Logging.Logger = logger;

                                     v2.Init (null, GetDriverName ());

                                     booting.Release ();

                                     Toplevel t = topLevelBuilder ();

                                     Application.Run (t); // This will block, but it's on a background thread now

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
                                     Logging.Logger = origLogger;
                                 }
                             },
                             _cts.Token);

        // Wait for booting to complete with a timeout to avoid hangs
        if (!booting.WaitAsync (TimeSpan.FromSeconds (5)).Result)
        {
            throw new TimeoutException ("Application failed to start within the allotted time.");
        }

        WaitIteration ();
    }

    private string GetDriverName ()
    {
        return _driver switch
        {
            V2TestDriver.V2Win => "v2win",
            V2TestDriver.V2Net => "v2net",
            _ =>
                throw new ArgumentOutOfRangeException ()
        };
    }

    /// <summary>
    ///     Stops the application and waits for the background thread to exit.
    /// </summary>
    public GuiTestContext Stop ()
    {
        if (_runTask.IsCompleted)
        {
            return this;
        }

        Application.Invoke (() => Application.RequestStop ());

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

    /// <summary>
    /// Cleanup to avoid state bleed between tests
    /// </summary>
    public void Dispose ()
    {
        Stop ();

        if (_hardStop.IsCancellationRequested)
        {
            throw new (
                       "Application was hard stopped, typically this means it timed out or did not shutdown gracefully. Ensure you call Stop in your test");
        }

        _hardStop.Cancel ();
    }

    /// <summary>
    ///     Adds the given <paramref name="v"/> to the current top level view
    ///     and performs layout.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public GuiTestContext Add (View v)
    {
        WaitIteration (
                       () =>
                       {
                           Toplevel top = Application.Top ?? throw new ("Top was null so could not add view");
                           top.Add (v);
                           top.Layout ();
                           _lastView = v;
                       });

        return this;
    }

    /// <summary>
    /// Simulates changing the console size e.g. by resizing window in your operating system
    /// </summary>
    /// <param name="width">new Width for the console.</param>
    /// <param name="height">new Height for the console.</param>
    /// <returns></returns>
    public GuiTestContext ResizeConsole (int width, int height)
    {
        _output.Size = new (width, height);

        return WaitIteration ();
    }

    public GuiTestContext ScreenShot (string title, TextWriter writer)
    {
        writer.WriteLine (title + ":");
        var text = Application.ToString ();

        writer.WriteLine (text);

        return WaitIteration ();
    }

    /// <summary>
    /// Writes all Terminal.Gui engine logs collected so far to the <paramref name="writer"/>
    /// </summary>
    /// <param name="writer"></param>
    /// <returns></returns>
    public GuiTestContext WriteOutLogs (TextWriter writer)
    {
        writer.WriteLine (_logsSb.ToString ());

        return WaitIteration ();
    }

    /// <summary>
    /// Waits until the end of the current iteration of the main loop. Optionally
    /// running a given <paramref name="a"/> action on the UI thread at that time.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public GuiTestContext WaitIteration (Action? a = null)
    {
        a ??= () => { };
        var ctsLocal = new CancellationTokenSource ();

        Application.Invoke (
                            () =>
                            {
                                a ();
                                ctsLocal.Cancel ();
                            });

        // Blocks until either the token or the hardStopToken is cancelled.
        WaitHandle.WaitAny (
                            new []
                            {
                                _cts.Token.WaitHandle,
                                _hardStop.Token.WaitHandle,
                                ctsLocal.Token.WaitHandle
                            });

        return this;
    }

    /// <summary>
    /// Performs the supplied <paramref name="doAction"/> immediately.
    /// Enables running commands without breaking the Fluent API calls.
    /// </summary>
    /// <param name="doAction"></param>
    /// <returns></returns>
    public GuiTestContext Then (Action doAction)
    {
        try
        {
            doAction ();
        }
        catch(Exception)
        {
            Stop ();
            _hardStop.Cancel();

            throw;

        }

        return this;
    }

    /// <summary>
    /// Simulates a right click at the given screen coordinates on the current driver.
    /// This is a raw input event that goes through entire processing pipeline as though
    /// user had pressed the mouse button physically.
    /// </summary>
    /// <param name="screenX">0 indexed screen coordinates</param>
    /// <param name="screenY">0 indexed screen coordinates</param>
    /// <returns></returns>
    public GuiTestContext RightClick (int screenX, int screenY) { return Click (WindowsConsole.ButtonState.Button3Pressed, screenX, screenY); }

    /// <summary>
    /// Simulates a left click at the given screen coordinates on the current driver.
    /// This is a raw input event that goes through entire processing pipeline as though
    /// user had pressed the mouse button physically.
    /// </summary>
    /// <param name="screenX">0 indexed screen coordinates</param>
    /// <param name="screenY">0 indexed screen coordinates</param>
    /// <returns></returns>
    public GuiTestContext LeftClick (int screenX, int screenY) { return Click (WindowsConsole.ButtonState.Button1Pressed, screenX, screenY); }

    private GuiTestContext Click (WindowsConsole.ButtonState btn, int screenX, int screenY)
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:

                _winInput.InputBuffer.Enqueue (
                                               new ()
                                               {
                                                   EventType = WindowsConsole.EventType.Mouse,
                                                   MouseEvent = new ()
                                                   {
                                                       ButtonState = btn,
                                                       MousePosition = new ((short)screenX, (short)screenY)
                                                   }
                                               });

                _winInput.InputBuffer.Enqueue (
                                               new ()
                                               {
                                                   EventType = WindowsConsole.EventType.Mouse,
                                                   MouseEvent = new ()
                                                   {
                                                       ButtonState = WindowsConsole.ButtonState.NoButtonPressed,
                                                       MousePosition = new ((short)screenX, (short)screenY)
                                                   }
                                               });
                break;
            case V2TestDriver.V2Net:

                int netButton = btn switch
                {
                    WindowsConsole.ButtonState.Button1Pressed => 0,
                    WindowsConsole.ButtonState.Button2Pressed => 1,
                    WindowsConsole.ButtonState.Button3Pressed => 2,
                    WindowsConsole.ButtonState.RightmostButtonPressed => 2,
                    _ => throw new ArgumentOutOfRangeException (nameof (btn))
                };
                foreach (var k in NetSequences.Click (netButton, screenX, screenY))
                {
                    SendNetKey (k);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        WaitIteration ();

        return this;
    }

    public GuiTestContext Down ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.DOWN);
                WaitIteration ();
                break;
            case V2TestDriver.V2Net:
                foreach (var k in NetSequences.Down)
                {
                    SendNetKey (k);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }


        return this;
    }

    /// <summary>
    /// Simulates the Right cursor key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Right ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.RIGHT);
                WaitIteration ();
                break;
            case V2TestDriver.V2Net:
                foreach (var k in NetSequences.Right)
                {
                    SendNetKey (k);
                }
                WaitIteration ();
                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return this;
    }

    /// <summary>
    /// Simulates the Left cursor key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Left ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.LEFT);
                WaitIteration ();
                break;
            case V2TestDriver.V2Net:
                foreach (var k in NetSequences.Left)
                {
                    SendNetKey (k);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return this;
    }

    /// <summary>
    /// Simulates the up cursor key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Up ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.UP);
                WaitIteration ();
                break;
            case V2TestDriver.V2Net:
                foreach (var k in NetSequences.Up)
                {
                    SendNetKey (k);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return this;
    }

    /// <summary>
    /// Simulates pressing the Return/Enter (newline) key.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Enter ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (
                                new WindowsConsole.KeyEventRecord
                                {
                                    UnicodeChar = '\r',
                                    dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                                    wRepeatCount = 1,
                                    wVirtualKeyCode = ConsoleKeyMapping.VK.RETURN,
                                    wVirtualScanCode = 28
                                });
                break;
            case V2TestDriver.V2Net:
                SendNetKey (new ('\r', ConsoleKey.Enter, false, false, false));
                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return this;
    }

    /// <summary>
    /// Registers a right click handler on the <see cref="LastView"/> added view (or root view) that
    /// will open the supplied <paramref name="contextMenu"/>.
    /// </summary>
    /// <param name="contextMenu"></param>
    /// <returns></returns>
    public GuiTestContext WithContextMenu (PopoverMenu? contextMenu)
    {
        LastView.MouseEvent += (s, e) =>
                               {
                                   if (e.Flags.HasFlag (MouseFlags.Button3Clicked))
                                   {
                                       // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
                                       // and the context menu is disposed when it is closed.
                                       Application.Popover?.Register (contextMenu);
                                       contextMenu?.MakeVisible (e.ScreenPosition);
                                   }
                               };

        return this;
    }

    /// <summary>
    /// The last view added (e.g. with <see cref="Add"/>) or the root/current top.
    /// </summary>
    public View LastView => _lastView ?? Application.Top ?? throw new ("Could not determine which view to add to");

    /// <summary>
    ///     Send a full windows OS key including both down and up.
    /// </summary>
    /// <param name="fullKey"></param>
    private void SendWindowsKey (WindowsConsole.KeyEventRecord fullKey)
    {
        WindowsConsole.KeyEventRecord down = fullKey;
        WindowsConsole.KeyEventRecord up = fullKey; // because struct this is new copy

        down.bKeyDown = true;
        up.bKeyDown = false;

        _winInput.InputBuffer.Enqueue (
                                       new ()
                                       {
                                           EventType = WindowsConsole.EventType.Key,
                                           KeyEvent = down
                                       });

        _winInput.InputBuffer.Enqueue (
                                       new ()
                                       {
                                           EventType = WindowsConsole.EventType.Key,
                                           KeyEvent = up
                                       });

        WaitIteration ();
    }


    private void SendNetKey (ConsoleKeyInfo consoleKeyInfo)
    {
        _netInput.InputBuffer.Enqueue (consoleKeyInfo);
    }

    /// <summary>
    ///     Sends a special key e.g. cursor key that does not map to a specific character
    /// </summary>
    /// <param name="specialKey"></param>
    private void SendWindowsKey (ConsoleKeyMapping.VK specialKey)
    {
        _winInput.InputBuffer.Enqueue (
                                       new ()
                                       {
                                           EventType = WindowsConsole.EventType.Key,
                                           KeyEvent = new ()
                                           {
                                               bKeyDown = true,
                                               wRepeatCount = 0,
                                               wVirtualKeyCode = specialKey,
                                               wVirtualScanCode = 0,
                                               UnicodeChar = '\0',
                                               dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed
                                           }
                                       });

        _winInput.InputBuffer.Enqueue (
                                       new ()
                                       {
                                           EventType = WindowsConsole.EventType.Key,
                                           KeyEvent = new ()
                                           {
                                               bKeyDown = false,
                                               wRepeatCount = 0,
                                               wVirtualKeyCode = specialKey,
                                               wVirtualScanCode = 0,
                                               UnicodeChar = '\0',
                                               dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed
                                           }
                                       });

        WaitIteration ();
    }

    /// <summary>
    /// Sets the input focus to the given <see cref="View"/>.
    /// Throws <see cref="ArgumentException"/> if focus did not change due to system
    /// constraints e.g. <paramref name="toFocus"/>
    /// <see cref="View.CanFocus"/> is <see langword="false"/>
    /// </summary>
    /// <param name="toFocus"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public GuiTestContext Focus (View toFocus)
    {
        toFocus.FocusDeepest (NavigationDirection.Forward, TabBehavior.TabStop);

        if (!toFocus.HasFocus)
        {
            throw new ArgumentException ("Failed to set focus, FocusDeepest did not result in HasFocus becoming true. Ensure view is added and focusable");
        }

        return WaitIteration ();
    }
}
