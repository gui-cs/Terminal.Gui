using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.DriverTests;

public class MainLoopDriverTests
{
    public MainLoopDriverTests (ITestOutputHelper output) { ConsoleDriver.RunningUnitTests = true; }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_AddIdle_ValidIdleHandler_ReturnsToken (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);
        var idleHandlerInvoked = false;

        bool IdleHandler ()
        {
            idleHandlerInvoked = true;

            return false;
        }

        Func<bool> token = mainLoop.AddIdle (IdleHandler);

        Assert.NotNull (token);
        Assert.False (idleHandlerInvoked); // Idle handler should not be invoked immediately
        mainLoop.RunIteration (); // Run an iteration to process the idle handler
        Assert.True (idleHandlerInvoked); // Idle handler should be invoked after processing
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_AddTimeout_ValidParameters_ReturnsToken (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);
        var callbackInvoked = false;

        object token = mainLoop.TimedEvents.AddTimeout (
                                            TimeSpan.FromMilliseconds (100),
                                            () =>
                                            {
                                                callbackInvoked = true;

                                                return false;
                                            }
                                           );

        Assert.NotNull (token);
        mainLoop.RunIteration (); // Run an iteration to process the timeout
        Assert.False (callbackInvoked); // Callback should not be invoked immediately
        Thread.Sleep (200); // Wait for the timeout
        mainLoop.RunIteration (); // Run an iteration to process the timeout
        Assert.True (callbackInvoked); // Callback should be invoked after the timeout
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_CheckTimersAndIdleHandlers_IdleHandlersActive_ReturnsTrue (
        Type driverType,
        Type mainLoopDriverType
    )
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        mainLoop.AddIdle (() => false);
        bool result = mainLoop.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeout);

        Assert.True (result);
        Assert.Equal (-1, waitTimeout);
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_CheckTimersAndIdleHandlers_NoTimersOrIdleHandlers_ReturnsFalse (
        Type driverType,
        Type mainLoopDriverType
    )
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        bool result = mainLoop.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeout);

        Assert.False (result);
        Assert.Equal (-1, waitTimeout);
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_CheckTimersAndIdleHandlers_TimersActive_ReturnsTrue (
        Type driverType,
        Type mainLoopDriverType
    )
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        mainLoop.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (100), () => false);
        bool result = mainLoop.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeout);

        Assert.True (result);
        Assert.True (waitTimeout >= 0);
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_Constructs_Disposes (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        // Check default values
        Assert.NotNull (mainLoop);
        Assert.Equal (mainLoopDriver, mainLoop.MainLoopDriver);
        Assert.Empty (mainLoop.TimedEvents.IdleHandlers);
        Assert.Empty (mainLoop.TimedEvents.Timeouts);
        Assert.False (mainLoop.Running);

        // Clean up
        mainLoop.Dispose ();

        // TODO: It'd be nice if we could really verify IMainLoopDriver.TearDown was called
        // and that it was actually cleaned up.
        Assert.Null (mainLoop.MainLoopDriver);
        Assert.Empty (mainLoop.TimedEvents.IdleHandlers);
        Assert.Empty (mainLoop.TimedEvents.Timeouts);
        Assert.False (mainLoop.Running);
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_RemoveIdle_InvalidToken_ReturnsFalse (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        bool result = mainLoop.TimedEvents.RemoveIdle (() => false);

        Assert.False (result);
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_RemoveIdle_ValidToken_ReturnsTrue (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        bool IdleHandler () { return false; }

        Func<bool> token = mainLoop.AddIdle (IdleHandler);
        bool result = mainLoop.TimedEvents.RemoveIdle (token);

        Assert.True (result);
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_RemoveTimeout_InvalidToken_ReturnsFalse (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        bool result = mainLoop.TimedEvents.RemoveTimeout (new object ());

        Assert.False (result);
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_RemoveTimeout_ValidToken_ReturnsTrue (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);

        object token = mainLoop.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (100), () => false);
        bool result = mainLoop.TimedEvents.RemoveTimeout (token);

        Assert.True (result);
        mainLoop.Dispose ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    [InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    [InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    [InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]

    //[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
    public void MainLoop_RunIteration_ValidIdleHandler_CallsIdleHandler (Type driverType, Type mainLoopDriverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, driver);
        var mainLoop = new MainLoop (mainLoopDriver);
        var idleHandlerInvoked = false;

        Func<bool> idleHandler = () =>
                                 {
                                     idleHandlerInvoked = true;

                                     return false;
                                 };

        mainLoop.AddIdle (idleHandler);
        mainLoop.RunIteration (); // Run an iteration to process the idle handler

        Assert.True (idleHandlerInvoked);
        mainLoop.Dispose ();
    }

    //[Theory]
    //[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
    //[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
    //[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
    //[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
    //public void MainLoop_Invoke_ValidAction_RunsAction (Type driverType, Type mainLoopDriverType)
    //{
    //	var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
    //	var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
    //	var mainLoop = new MainLoop (mainLoopDriver);
    //	var actionInvoked = false;

    //	mainLoop.Invoke (() => { actionInvoked = true; });
    //	mainLoop.RunIteration (); // Run an iteration to process the action.

    //	Assert.True (actionInvoked);
    //	mainLoop.Dispose ();
    //}
}
