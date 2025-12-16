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
        InjectMouseEvent (new ()
        {
            Flags = MouseFlags.RightButtonPressed,
            ScreenPosition = new (screenX, screenY),
            Position = new (screenX, screenY)
        });

        return InjectMouseEvent (new ()
        {
            Flags = MouseFlags.RightButtonReleased,
            ScreenPosition = new (screenX, screenY),
            Position = new (screenX, screenY)
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
        InjectMouseEvent (new ()
        {
            Flags = MouseFlags.LeftButtonPressed,
            ScreenPosition = new (screenX, screenY),
            Position = new (screenX, screenY)
        });

        return InjectMouseEvent (new ()
        {
            Flags = MouseFlags.LeftButtonReleased,
            ScreenPosition = new (screenX, screenY),
            Position = new (screenX, screenY)
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
        return InjectMouseEvent (new ()
        {
            Flags = MouseFlags.LeftButtonClicked
        }, evaluator);
    }

    /// <summary>
    /// Injects a mouse event to the current driver's input processor.
    /// This method sets the <see cref="Mouse.Timestamp"/> to <see cref="DateTime.Now"/>.
    /// </summary>
    /// <param name="mouse"></param>
    /// <returns></returns>
    private GuiTestContext InjectMouseEvent (Mouse mouse)
    {
            // Enqueue the mouse event
        WaitIteration ((app) =>
        {
            if (app.Driver is { })
            {
                mouse.Timestamp = DateTime.Now;
                mouse.Position = mouse.ScreenPosition;

                app.Driver.GetInputProcessor ().InjectMouseEvent (app, mouse);
            }
            else
            {
                Fail ("Expected Application.Driver to be non-null.");
            }
        });

        // Wait for the event to be processed (similar to InjectKeyEvent)
        return WaitIteration ();
    }

    /// <summary>
    /// Injects a mouse event to the current driver's input processor.
    /// This method sets the <see cref="Mouse.Timestamp"/> to <see cref="DateTime.Now"/>.
    /// </summary>
    /// <param name="mouse"></param>
    /// <param name="evaluator"></param>
    /// <returns></returns>
    private GuiTestContext InjectMouseEvent<TView> (Mouse mouse, Func<TView, bool> evaluator) where TView : View
    {
        var screen = Point.Empty;

        GuiTestContext ctx = WaitIteration ((_) =>
                                            {
                                                TView v = Find (evaluator);
                                                screen = v.ViewportToScreen (new Point (0, 0));
                                            });
        mouse.ScreenPosition = screen;
        mouse.Position = screen;

        InjectMouseEvent (mouse);

        return ctx;
    }

    /// <summary>
    ///     Injects a key down event to the current driver's input processor.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <summary>
    ///     Enqueues a key down event to the current driver's input processor.
    /// </summary>
    public GuiTestContext InjectKeyEvent (Key key)
    {
        //Logging.Trace ($"Enqueuing key: {key}");

        // Enqueue the key event and wait for it to be processed.
        // We do this by subscribing to the Driver.KeyDown event and waiting until it is raised.
        // This prevents the application from missing the key event if we enqueue it and immediately return.
        bool keyReceived = false;
        if (App?.Driver is { })
        {
            App.Driver.KeyDown += DriverOnKeyDown;
            App.Driver.InjectKeyEvent (key);
            WaitUntil (() => keyReceived);
        }

        return this;

        void DriverOnKeyDown (object? sender, Key e)
        {
            App.Driver.KeyDown -= DriverOnKeyDown;
            keyReceived = true;
        }

    }
}
