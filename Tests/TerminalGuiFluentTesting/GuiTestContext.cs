using System.Drawing;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TerminalGuiFluentTesting;

/// <summary>
///     Fluent API context for testing a Terminal.Gui application. Create
///     an instance using <see cref="With"/> static class.
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
    private bool _finished;
    private readonly object _threadLock = new ();

    internal GuiTestContext (Func<Toplevel> topLevelBuilder, int width, int height, V2TestDriver driver)
    {
        lock (_threadLock)
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
            _runTask = Task.Run (() =>
                                 {
                                     while (Application.Top is { })
                                     {
                                         Task.Delay (300).Wait ();
                                     }
                                 })
                           .ContinueWith (
                                          (task, _) =>
                                          {
                                              try
                                              {
                                                  if (task.IsFaulted)
                                                  {
                                                      _ex = task.Exception ?? new Exception ("Unknown error in background task");
                                                  }

                                                  // Ensure we are not running on the main thread
                                                  if (ApplicationImpl.Instance != origApp)
                                                  {
                                                      throw new InvalidOperationException (
                                                                                           "Application instance is not the original one, this should not happen.");
                                                  }

                                                  ApplicationImpl.ChangeInstance (v2);

                                                  ILogger logger = LoggerFactory.Create (builder =>
                                                                                             builder.SetMinimumLevel (LogLevel.Trace)
                                                                                                    .AddProvider (
                                                                                                         new TextWriterLoggerProvider (
                                                                                                              new StringWriter (_logsSb))))
                                                                                .CreateLogger ("Test Logging");
                                                  Logging.Logger = logger;

                                                  v2.Init (null, GetDriverName ());

                                                  booting.Release ();

                                                  Toplevel t = topLevelBuilder ();
                                                  t.Closed += (s, e) => { _finished = true; };
                                                  Application.Run (t); // This will block, but it's on a background thread now

                                                  t.Dispose ();
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
                                                  _finished = true;
                                              }
                                          },
                                          _cts.Token);

            // Wait for booting to complete with a timeout to avoid hangs
            if (!booting.WaitAsync (TimeSpan.FromSeconds (10)).Result)
            {
                throw new TimeoutException ("Application failed to start within the allotted time.");
            }

            WaitIteration ();
        }
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

        Application.Invoke (() => { Application.RequestStop (); });

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
    ///     Hard stops the application and waits for the background thread to exit.
    /// </summary>
    public void HardStop ()
    {
        _hardStop.Cancel ();
        Stop ();
    }

    /// <summary>
    ///     Cleanup to avoid state bleed between tests
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
    ///     Simulates changing the console size e.g. by resizing window in your operating system
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

        return this; //WaitIteration();
    }

    /// <summary>
    ///     Writes all Terminal.Gui engine logs collected so far to the <paramref name="writer"/>
    /// </summary>
    /// <param name="writer"></param>
    /// <returns></returns>
    public GuiTestContext WriteOutLogs (TextWriter writer)
    {
        writer.WriteLine (_logsSb.ToString ());

        return this; //WaitIteration();
    }

    /// <summary>
    ///     Waits until the end of the current iteration of the main loop. Optionally
    ///     running a given <paramref name="a"/> action on the UI thread at that time.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public GuiTestContext WaitIteration (Action? a = null)
    {
        // If application has already exited don't wait!
        if (_finished || _cts.Token.IsCancellationRequested || _hardStop.Token.IsCancellationRequested)
        {
            return this;
        }

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
    ///     Performs the supplied <paramref name="doAction"/> immediately.
    ///     Enables running commands without breaking the Fluent API calls.
    /// </summary>
    /// <param name="doAction"></param>
    /// <returns></returns>
    public GuiTestContext Then (Action doAction)
    {
        try
        {
            doAction ();
        }
        catch (Exception)
        {
            HardStop ();

            throw;
        }

        return this;
    }

    /// <summary>
    ///     Simulates a right click at the given screen coordinates on the current driver.
    ///     This is a raw input event that goes through entire processing pipeline as though
    ///     user had pressed the mouse button physically.
    /// </summary>
    /// <param name="screenX">0 indexed screen coordinates</param>
    /// <param name="screenY">0 indexed screen coordinates</param>
    /// <returns></returns>
    public GuiTestContext RightClick (int screenX, int screenY) { return Click (WindowsConsole.ButtonState.Button3Pressed, screenX, screenY); }

    /// <summary>
    ///     Simulates a left click at the given screen coordinates on the current driver.
    ///     This is a raw input event that goes through entire processing pipeline as though
    ///     user had pressed the mouse button physically.
    /// </summary>
    /// <param name="screenX">0 indexed screen coordinates</param>
    /// <param name="screenY">0 indexed screen coordinates</param>
    /// <returns></returns>
    public GuiTestContext LeftClick (int screenX, int screenY) { return Click (WindowsConsole.ButtonState.Button1Pressed, screenX, screenY); }

    public GuiTestContext LeftClick<T> (Func<T, bool> evaluator) where T : View { return Click (WindowsConsole.ButtonState.Button1Pressed, evaluator); }

    private GuiTestContext Click<T> (WindowsConsole.ButtonState btn, Func<T, bool> evaluator) where T : View
    {
        T v = Find (evaluator);
        Point screen = v.ViewportToScreen (new Point (0, 0));

        return Click (btn, screen.X, screen.Y);
    }

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

                foreach (ConsoleKeyInfo k in NetSequences.Click (netButton, screenX, screenY))
                {
                    SendNetKey (k);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return WaitIteration ();

        ;
    }

    public GuiTestContext Down ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.DOWN);

                break;
            case V2TestDriver.V2Net:
                foreach (ConsoleKeyInfo k in NetSequences.Down)
                {
                    SendNetKey (k);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return WaitIteration ();

        ;
    }

    /// <summary>
    ///     Simulates the Right cursor key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Right ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.RIGHT);

                break;
            case V2TestDriver.V2Net:
                foreach (ConsoleKeyInfo k in NetSequences.Right)
                {
                    SendNetKey (k);
                }

                WaitIteration ();

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return WaitIteration ();
    }

    /// <summary>
    ///     Simulates the Left cursor key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Left ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.LEFT);

                break;
            case V2TestDriver.V2Net:
                foreach (ConsoleKeyInfo k in NetSequences.Left)
                {
                    SendNetKey (k);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return WaitIteration ();
    }

    /// <summary>
    ///     Simulates the up cursor key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Up ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (ConsoleKeyMapping.VK.UP);

                break;
            case V2TestDriver.V2Net:
                foreach (ConsoleKeyInfo k in NetSequences.Up)
                {
                    SendNetKey (k);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return WaitIteration ();
    }

    /// <summary>
    ///     Simulates pressing the Return/Enter (newline) key.
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

        return WaitIteration ();
    }

    /// <summary>
    ///     Simulates pressing the Esc (Escape) key.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Escape ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (
                                new WindowsConsole.KeyEventRecord
                                {
                                    UnicodeChar = '\u001b',
                                    dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                                    wRepeatCount = 1,
                                    wVirtualKeyCode = ConsoleKeyMapping.VK.ESCAPE,
                                    wVirtualScanCode = 1
                                });

                break;
            case V2TestDriver.V2Net:

                // Note that this accurately describes how Esc comes in. Typically, ConsoleKey is None
                // even though you would think it would be Escape - it isn't
                SendNetKey (new ('\u001b', ConsoleKey.None, false, false, false));

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return this;
    }

    /// <summary>
    ///     Simulates pressing the Tab key.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Tab ()
    {
        switch (_driver)
        {
            case V2TestDriver.V2Win:
                SendWindowsKey (
                                new WindowsConsole.KeyEventRecord
                                {
                                    UnicodeChar = '\t',
                                    dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                                    wRepeatCount = 1,
                                    wVirtualKeyCode = 0,
                                    wVirtualScanCode = 0
                                });

                break;
            case V2TestDriver.V2Net:

                // Note that this accurately describes how Tab comes in. Typically, ConsoleKey is None
                // even though you would think it would be Tab - it isn't
                SendNetKey (new ('\t', ConsoleKey.None, false, false, false));

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        return this;
    }

    /// <summary>
    ///     Registers a right click handler on the <see cref="LastView"/> added view (or root view) that
    ///     will open the supplied <paramref name="contextMenu"/>.
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
    ///     The last view added (e.g. with <see cref="Add"/>) or the root/current top.
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

    private void SendNetKey (ConsoleKeyInfo consoleKeyInfo) { _netInput.InputBuffer.Enqueue (consoleKeyInfo); }

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
    ///     Sends a key to the application. This goes directly to Application and does not go through
    ///     a driver.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public GuiTestContext RaiseKeyDownEvent (Key key)
    {
        Application.RaiseKeyDownEvent (key);

        return this; //WaitIteration();
    }

    /// <summary>
    ///     Sets the input focus to the given <see cref="View"/>.
    ///     Throws <see cref="ArgumentException"/> if focus did not change due to system
    ///     constraints e.g. <paramref name="toFocus"/>
    ///     <see cref="View.CanFocus"/> is <see langword="false"/>
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

    /// <summary>
    ///     Tabs through the UI until a View matching the <paramref name="evaluator"/>
    ///     is found (of Type T) or all views are looped through (back to the beginning)
    ///     in which case triggers hard stop and Exception
    /// </summary>
    /// <param name="evaluator">Delegate that returns true if the passed View is the one
    /// you are trying to focus. Leave <see langword="null"/> to focus the first view of type
    /// <typeparamref name="T"/></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public GuiTestContext Focus<T> (Func<T, bool>? evaluator = null) where T : View
    {
        evaluator ??= _ => true;
        Toplevel? t = Application.Top;

        HashSet<View> seen = new ();

        if (t == null)
        {
            Fail ("Application.Top was null when trying to set focus");

            return this;
        }

        do
        {
            View? next = t.MostFocused;

            // Is view found?
            if (next is T v && evaluator (v))
            {
                return this;
            }

            // No, try tab to the next (or first)
            Tab ();
            WaitIteration ();
            next = t.MostFocused;

            if (next is null)
            {
                Fail ("Failed to tab to a view which matched the Type and evaluator constraints of the test because MostFocused became or was always null");

                return this;
            }

            // Track the views we have seen
            // We have looped around to the start again if it was already there
            if (!seen.Add (next))
            {
                Fail ("Failed to tab to a view which matched the Type and evaluator constraints of the test before looping back to the original View");

                return this;
            }
        }
        while (true);
    }

    private T Find<T> (Func<T, bool> evaluator) where T : View
    {
        Toplevel? t = Application.Top;

        if (t == null)
        {
            Fail ("Application.Top was null when attempting to find view");
        }

        T? f = FindRecursive (t!, evaluator);

        if (f == null)
        {
            Fail ("Failed to tab to a view which matched the Type and evaluator constraints in any SubViews of top");
        }

        return f!;
    }

    private T? FindRecursive<T> (View current, Func<T, bool> evaluator) where T : View
    {
        foreach (View subview in current.SubViews)
        {
            if (subview is T match && evaluator (match))
            {
                return match;
            }

            // Recursive call
            T? result = FindRecursive (subview, evaluator);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void Fail (string reason)
    {
        Stop ();

        throw new (reason);
    }

    public GuiTestContext Send (Key key)
    {
        if (Application.Driver is IConsoleDriverFacade facade)
        {
            facade.InputProcessor.OnKeyDown (key);
            facade.InputProcessor.OnKeyUp (key);
        }
        else
        {
            Fail ("Expected Application.Driver to be IConsoleDriverFacade");
        }

        return this;
    }

    /// <summary>
    /// Returns the last set position of the cursor.
    /// </summary>
    /// <returns></returns>
    public Point GetCursorPosition ()
    {
        return _output.CursorPosition;
    }
}
