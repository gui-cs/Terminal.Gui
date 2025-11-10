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
    public GuiTestContext RightClick (int screenX, int screenY)
    {
        return EnqueueMouseEvent (new ()
        {
            Flags = MouseFlags.Button3Clicked,
            ScreenPosition = new (screenX, screenY)
        });
    }

    /// <summary>
    ///     Simulates a left click at the given screen coordinates on the current driver.
    ///     This is a raw input event that goes through entire processing pipeline as though
    ///     user had pressed the mouse button physically.
    /// </summary>
    /// <param name="screenX">0 indexed screen coordinates</param>
    /// <param name="screenY">0 indexed screen coordinates</param>
    /// <returns></returns>
    public GuiTestContext LeftClick (int screenX, int screenY)
    {
        return EnqueueMouseEvent (new ()
        {
            Flags = MouseFlags.Button1Clicked,
            ScreenPosition = new (screenX, screenY),
        });
    }

    /// <summary>
    ///     Simulates a left mouse click on the top-left cell of the Viewport of the View of type TView determined by the
    ///     <paramref name="evaluator"/>.
    /// </summary>
    /// <typeparam name="TView"></typeparam>
    /// <param name="evaluator"></param>
    /// <returns></returns>
    public GuiTestContext LeftClick<TView> (Func<TView, bool> evaluator) where TView : View
    {
        return EnqueueMouseEvent (new ()
        {
            Flags = MouseFlags.Button1Clicked,
        }, evaluator);
    }

    private GuiTestContext EnqueueMouseEvent (MouseEventArgs mouseEvent)
    {
            // Enqueue the mouse event
        WaitIteration (() =>
        {
            if (Application.Driver is { })
            {
                mouseEvent.Position = mouseEvent.ScreenPosition;

                Application.Driver.InputProcessor.EnqueueMouseEvent (mouseEvent);
            }
            else
            {
                Fail ("Expected Application.Driver to be non-null.");
            }
        });

        // Wait for the event to be processed (similar to EnqueueKeyEvent)
        return WaitIteration ();
    }


    private GuiTestContext EnqueueMouseEvent<TView> (MouseEventArgs mouseEvent, Func<TView, bool> evaluator) where TView : View
    {
        var screen = Point.Empty;

        GuiTestContext ctx = WaitIteration (() =>
                                            {
                                                TView v = Find (evaluator);
                                                screen = v.ViewportToScreen (new Point (0, 0));
                                            });
        mouseEvent.ScreenPosition = screen;
        mouseEvent.Position = new Point (0, 0);

        EnqueueMouseEvent (mouseEvent);

        return ctx;
    }

    //private GuiTestContext Click (WindowsConsole.ButtonState btn, int screenX, int screenY)
    //{
    //    switch (_driverType)
    //    {
    //        case TestDriver.Windows:

    //            _winInput!.InputQueue!.Enqueue (
    //                                             new ()
    //                                             {
    //                                                 EventType = WindowsConsole.EventType.Mouse,
    //                                                 MouseEvent = new ()
    //                                                 {
    //                                                     ButtonState = btn,
    //                                                     MousePosition = new ((short)screenX, (short)screenY)
    //                                                 }
    //                                             });

    //            _winInput.InputQueue.Enqueue (
    //                                           new ()
    //                                           {
    //                                               EventType = WindowsConsole.EventType.Mouse,
    //                                               MouseEvent = new ()
    //                                               {
    //                                                   ButtonState = WindowsConsole.ButtonState.NoButtonPressed,
    //                                                   MousePosition = new ((short)screenX, (short)screenY)
    //                                               }
    //                                           });

    //            return WaitUntil (() => _winInput.InputQueue.IsEmpty);

    //        case TestDriver.DotNet:

    //            int netButton = btn switch
    //            {
    //                WindowsConsole.ButtonState.Button1Pressed => 0,
    //                WindowsConsole.ButtonState.Button2Pressed => 1,
    //                WindowsConsole.ButtonState.Button3Pressed => 2,
    //                WindowsConsole.ButtonState.RightmostButtonPressed => 2,
    //                _ => throw new ArgumentOutOfRangeException (nameof (btn))
    //            };

    //            foreach (ConsoleKeyInfo k in NetSequences.Click (netButton, screenX, screenY))
    //            {
    //                SendNetKey (k, false);
    //            }

    //            return WaitIteration ();

    //        case TestDriver.Unix:

    //            int unixButton = btn switch
    //            {
    //                WindowsConsole.ButtonState.Button1Pressed => 0,
    //                WindowsConsole.ButtonState.Button2Pressed => 1,
    //                WindowsConsole.ButtonState.Button3Pressed => 2,
    //                WindowsConsole.ButtonState.RightmostButtonPressed => 2,
    //                _ => throw new ArgumentOutOfRangeException (nameof (btn))
    //            };

    //            foreach (ConsoleKeyInfo k in NetSequences.Click (unixButton, screenX, screenY))
    //            {
    //                SendUnixKey (k.KeyChar, false);
    //            }

    //            return WaitIteration ();

    //        case TestDriver.Fake:

    //            int fakeButton = btn switch
    //            {
    //                WindowsConsole.ButtonState.Button1Pressed => 0,
    //                WindowsConsole.ButtonState.Button2Pressed => 1,
    //                WindowsConsole.ButtonState.Button3Pressed => 2,
    //                WindowsConsole.ButtonState.RightmostButtonPressed => 2,
    //                _ => throw new ArgumentOutOfRangeException (nameof (btn))
    //            };

    //            foreach (ConsoleKeyInfo k in NetSequences.Click (fakeButton, screenX, screenY))
    //            {
    //                SendFakeKey (k, false);
    //            }

    //            return WaitIteration ();

    //        default:
    //            throw new ArgumentOutOfRangeException ();
    //    }
    //}

    /// <summary>
    ///     Enqueues a key down event to the current driver's input processor.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <summary>
    ///     Enqueues a key down event to the current driver's input processor.
    /// </summary>
    public GuiTestContext EnqueueKeyEvent (Key key)
    {
        //Logging.Trace ($"Enqueuing key: {key}");

        // Enqueue the key event and wait for it to be processed.
        // We do this by subscribing to the Driver.KeyDown event and waiting until it is raised.
        // This prevents the application from missing the key event if we enqueue it and immediately return.
        bool keyReceived = false;
        if (_applicationImpl?.Driver is { })
        {
            _applicationImpl.Driver.KeyDown += DriverOnKeyDown;
            _applicationImpl.Driver.EnqueueKeyEvent (key);
            WaitUntil (() => keyReceived);
        }
        else
        {
            Fail ("Expected Application.Driver to be non-null.");
        }


        return this;

        void DriverOnKeyDown (object? sender, Key e)
        {
            _applicationImpl.Driver.KeyDown -= DriverOnKeyDown;
            keyReceived = true;
        }

    }
}
