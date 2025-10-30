using System.Drawing;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace TerminalGuiFluentTesting;

public partial class GuiTestContext
{
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
        T v;
        var screen = Point.Empty;

        GuiTestContext ctx = WaitIteration (() =>
                                            {
                                                v = Find (evaluator);
                                                screen = v.ViewportToScreen (new Point (0, 0));
                                            });

        Click (btn, screen.X, screen.Y);

        return ctx;
    }

    private GuiTestContext Click (WindowsConsole.ButtonState btn, int screenX, int screenY)
    {
        switch (_driver)
        {
            case TestDriver.Windows:

                _winInput.InputBuffer!.Enqueue (
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

                return WaitUntil (() => _winInput.InputBuffer.IsEmpty);

            case TestDriver.DotNet:

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
                    SendNetKey (k, false);
                }

                return WaitIteration ();
            default:
                throw new ArgumentOutOfRangeException ();
        }
    }

    public GuiTestContext Down ()
    {
        switch (_driver)
        {
            case TestDriver.Windows:
                SendWindowsKey (ConsoleKeyMapping.VK.DOWN);

                break;
            case TestDriver.DotNet:
                foreach (ConsoleKeyInfo k in NetSequences.Down)
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
    ///     Simulates the Right cursor key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public GuiTestContext Right ()
    {
        switch (_driver)
        {
            case TestDriver.Windows:
                SendWindowsKey (ConsoleKeyMapping.VK.RIGHT);

                break;
            case TestDriver.DotNet:
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
            case TestDriver.Windows:
                SendWindowsKey (ConsoleKeyMapping.VK.LEFT);

                break;
            case TestDriver.DotNet:
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
            case TestDriver.Windows:
                SendWindowsKey (ConsoleKeyMapping.VK.UP);

                break;
            case TestDriver.DotNet:
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
            case TestDriver.Windows:
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
            case TestDriver.DotNet:
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
            case TestDriver.Windows:
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
            case TestDriver.DotNet:

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
            case TestDriver.Windows:
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
            case TestDriver.DotNet:

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
    ///     Send a full windows OS key including both down and up.
    /// </summary>
    /// <param name="fullKey"></param>
    private void SendWindowsKey (WindowsConsole.KeyEventRecord fullKey)
    {
        WindowsConsole.KeyEventRecord down = fullKey;
        WindowsConsole.KeyEventRecord up = fullKey; // because struct this is new copy

        down.bKeyDown = true;
        up.bKeyDown = false;

        _winInput.InputBuffer!.Enqueue (
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

    private void SendNetKey (ConsoleKeyInfo consoleKeyInfo, bool wait = true)
    {
        _netInput.InputBuffer!.Enqueue (consoleKeyInfo);

        if (wait)
        {
            WaitUntil (() => _netInput.InputBuffer.IsEmpty);
        }
    }

    /// <summary>
    ///     Sends a special key e.g. cursor key that does not map to a specific character
    /// </summary>
    /// <param name="specialKey"></param>
    private void SendWindowsKey (ConsoleKeyMapping.VK specialKey)
    {
        _winInput.InputBuffer!.Enqueue (
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
        WaitIteration (() => Application.RaiseKeyDownEvent (key));

        return this; //WaitIteration();
    }

    public GuiTestContext Send (Key key)
    {
        return WaitIteration (() =>
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
                              });
    }
}
