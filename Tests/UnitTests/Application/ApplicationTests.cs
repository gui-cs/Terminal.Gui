using System.Diagnostics;
using Xunit.Abstractions;
using static Terminal.Gui.Configuration.ConfigurationManager;

// Alias Console to MockConsole so we don't accidentally use Console

namespace UnitTests.ApplicationTests;

public class ApplicationTests
{
    public ApplicationTests (ITestOutputHelper output)
    {
        _output = output;

#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        View.Instances.Clear ();
        SessionToken.Instances.Clear ();
#endif
    }

    private readonly ITestOutputHelper _output;

    [Fact]
    public void AddTimeout_Fires ()
    {
        IApplication app = ApplicationImpl.Instance; // Force legacy
        app.Init ("fake");

        uint timeoutTime = 100;
        var timeoutFired = false;

        // Setup a timeout that will fire
        app.AddTimeout (
                        TimeSpan.FromMilliseconds (timeoutTime),
                        () =>
                        {
                            timeoutFired = true;

                            // Return false so the timer does not repeat
                            return false;
                        }
                       );

        // The timeout has not fired yet
        Assert.False (timeoutFired);

        // Block the thread to prove the timeout does not fire on a background thread
        Thread.Sleep ((int)timeoutTime * 2);
        Assert.False (timeoutFired);

        app.StopAfterFirstIteration = true;
        app.Run<Toplevel> ().Dispose ();

        // The timeout should have fired
        Assert.True (timeoutFired);

        app.Shutdown ();
    }

    [Fact]
    [SetupFakeApplication]
    public void Begin_Null_Toplevel_Throws ()
    {
        // Test null Toplevel
        Assert.Throws<ArgumentNullException> (() => Application.Begin (null));
    }

    [Fact]
    [SetupFakeApplication]
    public void Begin_Sets_Application_Top_To_Console_Size ()
    {
        Assert.Null (Application.TopRunnable);
        Application.Driver!.SetScreenSize (80, 25);
        Toplevel top = new ();
        Application.Begin (top);
        Assert.Equal (new (0, 0, 80, 25), Application.TopRunnable!.Frame);
        Application.Driver!.SetScreenSize (5, 5);
        Assert.Equal (new (0, 0, 5, 5), Application.TopRunnable!.Frame);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void End_And_Shutdown_Should_Not_Dispose_ApplicationTop ()
    {
        Assert.Null (Application.TopRunnable);

        SessionToken rs = Application.Begin (new ());
        Application.TopRunnable!.Title = "End_And_Shutdown_Should_Not_Dispose_ApplicationTop";
        Assert.Equal (rs.Toplevel, Application.TopRunnable);
        Application.End (rs);

#if DEBUG_IDISPOSABLE
        Assert.True (rs.WasDisposed);
        Assert.False (Application.TopRunnable!.WasDisposed); // Is true because the rs.Toplevel is the same as Application.TopRunnable
#endif

        Assert.Null (rs.Toplevel);

        Toplevel top = Application.TopRunnable;

#if DEBUG_IDISPOSABLE
        Exception exception = Record.Exception (Application.Shutdown);
        Assert.NotNull (exception);
        Assert.False (top.WasDisposed);
        top.Dispose ();
        Assert.True (top.WasDisposed);
#endif
    }

    [Fact]
    [SetupFakeApplication]
    public void Init_Begin_End_Cleans_Up ()
    {
        // Start stopwatch
        var stopwatch = new Stopwatch ();
        stopwatch.Start ();

        SessionToken sessionToken = null;

        EventHandler<SessionTokenEventArgs> newSessionTokenFn = (s, e) =>
                                                                {
                                                                    Assert.NotNull (e.State);
                                                                    sessionToken = e.State;
                                                                };
        Application.SessionBegun += newSessionTokenFn;

        var topLevel = new Toplevel ();
        SessionToken rs = Application.Begin (topLevel);
        Assert.NotNull (rs);
        Assert.NotNull (sessionToken);
        Assert.Equal (rs, sessionToken);

        Assert.Equal (topLevel, Application.TopRunnable);

        Application.SessionBegun -= newSessionTokenFn;
        Application.End (sessionToken);

        Assert.NotNull (Application.TopRunnable);
        Assert.NotNull (Application.Driver);

        topLevel.Dispose ();

        // Stop stopwatch
        stopwatch.Stop ();

        _output.WriteLine ($"Load took {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void Init_KeyBindings_Are_Not_Reset ()
    {
        Debug.Assert (!IsEnabled);

        try
        {
            // arrange
            ThrowOnJsonErrors = true;

            Application.QuitKey = Key.Q;
            Assert.Equal (Key.Q, Application.QuitKey);

            Application.Init ("fake");

            Assert.Equal (Key.Q, Application.QuitKey);
        }
        finally
        {
            Application.ResetState ();
        }
    }

    [Fact]
    public void Init_NoParam_ForceDriver_Works ()
    {
        Application.ForceDriver = "Fake";
        Application.Init ();

        Assert.Equal ("fake", Application.Driver!.GetName ());
        Application.ResetState ();
    }


    [Fact]
    public void Init_Null_Driver_Should_Pick_A_Driver ()
    {
        Application.Init ();

        Assert.NotNull (Application.Driver);

        Application.Shutdown ();
    }

    [Fact]
    public void Init_ResetState_Resets_Properties ()
    {
        ThrowOnJsonErrors = true;

        // For all the fields/properties of Application, check that they are reset to their default values

        // Set some values

        Application.Init (driverName: "fake");

        // Application.IsInitialized = true;

        // Reset
        Application.ResetState ();

        CheckReset ();

        // Set the values that can be set
        Application.Initialized = true;
        Application.MainThreadId = 1;

        //Application._topLevels = new List<Toplevel> ();
        Application.CachedViewsUnderMouse.Clear ();

        //Application.SupportedCultures = new List<CultureInfo> ();
        Application.Force16Colors = true;

        //Application.ForceDriver = "driver";
        Application.StopAfterFirstIteration = true;
        Application.PrevTabGroupKey = Key.A;
        Application.NextTabGroupKey = Key.B;
        Application.QuitKey = Key.C;
        Application.KeyBindings.Add (Key.D, Command.Cancel);

        Application.CachedViewsUnderMouse.Clear ();

        //Application.WantContinuousButtonPressedView = new View ();

        // Mouse
        Application.LastMousePosition = new Point (1, 1);

        Application.ResetState ();
        CheckReset ();

        ThrowOnJsonErrors = false;

        return;

        void CheckReset ()
        {
            // Check that all fields and properties are set to their default values

            // Public Properties
            Assert.Null (Application.TopRunnable);
            Assert.Null (Application.Mouse.MouseGrabView);

            // Don't check Application.ForceDriver
            // Assert.Empty (Application.ForceDriver);
            // Don't check Application.Force16Colors
            //Assert.False (Application.Force16Colors);
            Assert.Null (Application.Driver);
            Assert.False (Application.StopAfterFirstIteration);

            // Commented out because if CM changed the defaults, those changes should
            // persist across Inits.
            //Assert.Equal (Key.Tab.WithShift, Application.PrevTabKey);
            //Assert.Equal (Key.Tab, Application.NextTabKey);
            //Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
            //Assert.Equal (Key.F6, Application.NextTabGroupKey);
            //Assert.Equal (Key.Esc, Application.QuitKey);

            // Internal properties
            Assert.False (Application.Initialized);
            Assert.Equal (Application.GetSupportedCultures (), Application.SupportedCultures);
            Assert.Equal (Application.GetAvailableCulturesFromEmbeddedResources (), Application.SupportedCultures);
            Assert.Null (Application.MainThreadId);
            Assert.Empty (Application.SessionStack);
            Assert.Empty (Application.CachedViewsUnderMouse);

            // Mouse
            // Do not reset _lastMousePosition
            //Assert.Null (Application._lastMousePosition);

            // Navigation
            // Assert.Null (Application.Navigation);

            // Popover
            //Assert.Null (Application.Popover);

            // Events - Can't check
            //Assert.Null (GetEventSubscribers (typeof (Application), "InitializedChanged"));
            //Assert.Null (GetEventSubscribers (typeof (Application), "SessionBegun"));
            //Assert.Null (GetEventSubscribers (typeof (Application), "Iteration"));
            //Assert.Null (GetEventSubscribers (typeof (Application), "ScreenChanged"));
            //Assert.Null (GetEventSubscribers (typeof (Application.Mouse), "MouseEvent"));
            //Assert.Null (GetEventSubscribers (typeof (Application.Keyboard), "KeyDown"));
            //Assert.Null (GetEventSubscribers (typeof (Application.Keyboard), "KeyUp"));
        }
    }

    [Fact]
    public void Init_Shutdown_Cleans_Up ()
    {
        // Verify initial state is per spec
        //Pre_Init_State ();

        Application.Init ("fake");

        // Verify post-Init state is correct
        //Post_Init_State ();

        Application.Shutdown ();

        // Verify state is back to initial
        //Pre_Init_State ();
#if DEBUG_IDISPOSABLE

        // Validate there are no outstanding Responder-based instances 
        // after a scenario was selected to run. This proves the main UI Catalog
        // 'app' closed cleanly.
        Assert.Empty (View.Instances);
#endif
    }

    [Fact]
    public void Init_Shutdown_Fire_InitializedChanged ()
    {
        var initialized = false;
        var shutdown = false;

        Application.InitializedChanged += OnApplicationOnInitializedChanged;

        Application.Init (driverName: "fake");
        Assert.True (initialized);
        Assert.False (shutdown);

        Application.Shutdown ();
        Assert.True (initialized);
        Assert.True (shutdown);

        Application.InitializedChanged -= OnApplicationOnInitializedChanged;

        return;

        void OnApplicationOnInitializedChanged (object s, EventArgs<bool> a)
        {
            if (a.Value)
            {
                initialized = true;
            }
            else
            {
                shutdown = true;
            }
        }
    }

    [Fact]
    [SetupFakeApplication]
    public void Init_Unbalanced_Throws ()
    {
        Assert.Throws<InvalidOperationException> (() =>
                                                      Application.Init ("fake")
                                                 );
    }

    [Fact]
    [SetupFakeApplication]
    public void Init_Unbalanced_Throws2 ()
    {
        // Now try the other way
        Assert.Throws<InvalidOperationException> (() => Application.Init ("fake"));
    }

    [Fact]
    public void Init_WithoutTopLevelFactory_Begin_End_Cleans_Up ()
    {
        Application.StopAfterFirstIteration = true;

        // NOTE: Run<T>, when called after Init has been called behaves differently than
        // when called if Init has not been called.
        Toplevel topLevel = new ();
        Application.Init ("fake");

        SessionToken sessionToken = null;

        EventHandler<SessionTokenEventArgs> newSessionTokenFn = (s, e) =>
                                                                {
                                                                    Assert.NotNull (e.State);
                                                                    sessionToken = e.State;
                                                                };
        Application.SessionBegun += newSessionTokenFn;

        SessionToken rs = Application.Begin (topLevel);
        Assert.NotNull (rs);
        Assert.NotNull (sessionToken);
        Assert.Equal (rs, sessionToken);

        Assert.Equal (topLevel, Application.TopRunnable);

        Application.SessionBegun -= newSessionTokenFn;
        Application.End (sessionToken);

        Assert.NotNull (Application.TopRunnable);
        Assert.NotNull (Application.Driver);

        topLevel.Dispose ();
        Application.Shutdown ();

        Assert.Null (Application.TopRunnable);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [SetupFakeApplication]
    public void Internal_Properties_Correct ()
    {
        Assert.True (Application.Initialized);
        Assert.Null (Application.TopRunnable);
        SessionToken rs = Application.Begin (new ());
        Assert.Equal (Application.TopRunnable, rs.Toplevel);
        Assert.Null (Application.Mouse.MouseGrabView); // public
        Application.TopRunnable!.Dispose ();
    }

    // Invoke Tests
    // TODO: Test with threading scenarios
    [Fact]
    [SetupFakeApplication]
    public void Invoke_Adds_Idle ()
    {
        Toplevel top = new ();
        SessionToken rs = Application.Begin (top);

        var actionCalled = 0;
        Application.Invoke ((_) => { actionCalled++; });
        Application.TimedEvents!.RunTimers ();
        Assert.Equal (1, actionCalled);
        top.Dispose ();
    }

    [Fact]
    public void Run_Iteration_Fires ()
    {
        var iteration = 0;

        Application.Init ("fake");

        Application.Iteration += Application_Iteration;
        Application.Run<Toplevel> ().Dispose ();
        Application.Iteration -= Application_Iteration;

        Assert.Equal (1, iteration);
        Application.Shutdown ();

        return;

        void Application_Iteration (object sender, EventArgs<IApplication?> e)
        {
            if (iteration > 0)
            {
                Assert.Fail ();
            }

            iteration++;
            Application.RequestStop ();
        }
    }

    [Fact]
    [SetupFakeApplication]
    public void Screen_Size_Changes ()
    {
        IDriver driver = Application.Driver;

        Application.Driver!.SetScreenSize (80, 25);

        Assert.Equal (new (0, 0, 80, 25), driver!.Screen);
        Assert.Equal (new (0, 0, 80, 25), Application.Screen);

        // TODO: Should not be possible to manually change these at whim!
        driver.Cols = 100;
        driver.Rows = 30;

        // IDriver.Screen isn't assignable
        //driver.Screen = new (0, 0, driver.Cols, Rows);

        Application.Driver!.SetScreenSize (100, 30);

        Assert.Equal (new (0, 0, 100, 30), driver.Screen);

        // Assert does not make sense
        // Assert.NotEqual (new (0, 0, 100, 30), Application.Screen);
        // Assert.Equal (new (0, 0, 80, 25), Application.Screen);
        Application.Screen = new (0, 0, driver.Cols, driver.Rows);
        Assert.Equal (new (0, 0, 100, 30), driver.Screen);
    }

    [Fact]
    public void Shutdown_Alone_Does_Nothing () { Application.Shutdown (); }

    //[Fact]
    //public void InitState_Throws_If_Driver_Is_Null ()
    //{
    //    Assert.Throws<ArgumentNullException> (static () => Application.SubscribeDriverEvents ());
    //}

    #region RunTests

    [Fact]
    [SetupFakeApplication]
    public void Run_T_After_InitWithDriver_with_TopLevel_Does_Not_Throws ()
    {
        Application.StopAfterFirstIteration = true;

        // Run<Toplevel> when already initialized or not with a Driver will not throw (because Window is derived from Toplevel)
        // Using another type not derived from Toplevel will throws at compile time
        Application.Run<Window> ();
        Assert.True (Application.TopRunnable is Window);

        Application.TopRunnable!.Dispose ();
    }

    [Fact]
    public void Run_T_After_InitWithDriver_with_TopLevel_and_Driver_Does_Not_Throws ()
    {
        Application.StopAfterFirstIteration = true;

        // Run<Toplevel> when already initialized or not with a Driver will not throw (because Window is derived from Toplevel)
        // Using another type not derived from Toplevel will throws at compile time
        Application.Run<Window> (null, "fake");
        Assert.True (Application.TopRunnable is Window);

        Application.TopRunnable!.Dispose ();

        // Run<Toplevel> when already initialized or not with a Driver will not throw (because Dialog is derived from Toplevel)
        Application.Run<Dialog> (null, "fake");
        Assert.True (Application.TopRunnable is Dialog);

        Application.TopRunnable!.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_T_After_Init_Does_Not_Disposes_Application_Top ()
    {
        // Init doesn't create a Toplevel and assigned it to Application.TopRunnable
        // but Begin does
        var initTop = new Toplevel ();

        Application.Iteration += OnApplicationOnIteration;

        Application.Run<Toplevel> ();
        Application.Iteration -= OnApplicationOnIteration;

#if DEBUG_IDISPOSABLE
        Assert.False (initTop.WasDisposed);
        initTop.Dispose ();
        Assert.True (initTop.WasDisposed);
#endif
        Application.TopRunnable!.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication?> a)
        {
            Assert.NotEqual (initTop, Application.TopRunnable);
#if DEBUG_IDISPOSABLE
            Assert.False (initTop.WasDisposed);
#endif
            Application.RequestStop ();
        }
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_T_After_InitWithDriver_with_TestTopLevel_DoesNotThrow ()
    {
        Application.StopAfterFirstIteration = true;

        // Init has been called and we're passing no driver to Run<TestTopLevel>. This is ok.
        Application.Run<Toplevel> ();

        Application.TopRunnable!.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_T_After_InitNullDriver_with_TestTopLevel_DoesNotThrow ()
    {
        Application.StopAfterFirstIteration = true;

        // Init has been called, selecting FakeDriver; we're passing no driver to Run<TestTopLevel>. Should be fine.
        Application.Run<Toplevel> ();

        Application.TopRunnable!.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_T_Init_Driver_Cleared_with_TestTopLevel_Throws ()
    {
        Application.Driver = null;

        // Init has been called, but Driver has been set to null. Bad.
        Assert.Throws<InvalidOperationException> (() => Application.Run<Toplevel> ());
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_T_NoInit_DoesNotThrow ()
    {
        Application.StopAfterFirstIteration = true;

        Application.Run<Toplevel> ().Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_T_NoInit_WithDriver_DoesNotThrow ()
    {
        Application.StopAfterFirstIteration = true;

        // Init has NOT been called and we're passing a valid driver to Run<TestTopLevel>. This is ok.
        Application.Run<Toplevel> (null, "fake");

        Application.TopRunnable!.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_RequestStop_Stops ()
    {
        var top = new Toplevel ();
        SessionToken rs = Application.Begin (top);
        Assert.NotNull (rs);

        Application.Iteration += OnApplicationOnIteration;
        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;

        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication?> a) { Application.RequestStop (); }
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_Sets_Running_True ()
    {
        var top = new Toplevel ();
        SessionToken rs = Application.Begin (top);
        Assert.NotNull (rs);

        Application.Iteration += OnApplicationOnIteration;
        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;

        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication?> a)
        {
            Assert.True (top.Running);
            top.RequestStop ();
        }
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_RunningFalse_Stops ()
    {
        var top = new Toplevel ();
        SessionToken rs = Application.Begin (top);
        Assert.NotNull (rs);

        Application.Iteration += OnApplicationOnIteration;
        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;

        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication?> a) { top.Running = false; }
    }

    [Fact]
    [SetupFakeApplication]
    public void Run_Loaded_Ready_Unloaded_Events ()
    {
        Application.StopAfterFirstIteration = true;

        Toplevel top = new ();
        var count = 0;
        top.Loaded += (s, e) => count++;
        top.Ready += (s, e) => count++;
        top.Unloaded += (s, e) => count++;
        Application.Run (top);
        top.Dispose ();
    }

    // TODO: All Toplevel layout tests should be moved to ToplevelTests.cs
    [Fact]
    [SetupFakeApplication]
    public void Run_A_Modal_Toplevel_Refresh_Background_On_Moving ()
    {
        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var w = new Window
        {
            Width = 5, Height = 5,
            Arrangement = ViewArrangement.Movable
        };
        Application.Driver!.SetScreenSize (10, 10);
        SessionToken rs = Application.Begin (w);

        // Don't use visuals to test as style of border can change over time.
        Assert.Equal (new (0, 0), w.Frame.Location);

        Application.RaiseMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (w.Border, Application.Mouse.MouseGrabView);
        Assert.Equal (new (0, 0), w.Frame.Location);

        // Move down and to the right.
        Application.RaiseMouseEvent (new () { ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
        Assert.Equal (new (1, 1), w.Frame.Location);

        Application.End (rs);
        w.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void End_Does_Not_Dispose ()
    {
        var top = new Toplevel ();

        Window w = new ();
        Application.StopAfterFirstIteration = true;
        Application.Run (w);

#if DEBUG_IDISPOSABLE
        Assert.False (w.WasDisposed);
#endif

        Assert.NotNull (w);
        Assert.Equal (string.Empty, w.Title); // Valid - w has not been disposed. The user may want to run it again
        Assert.NotNull (Application.TopRunnable);
        Assert.Equal (w, Application.TopRunnable);
        Assert.NotEqual (top, Application.TopRunnable);

        Application.Run (w); // Valid - w has not been disposed.

#if DEBUG_IDISPOSABLE
        Assert.False (w.WasDisposed);
        Exception exception = Record.Exception (Application.Shutdown); // Invalid - w has not been disposed.
        Assert.NotNull (exception);

        w.Dispose ();
        Assert.True (w.WasDisposed);

        //exception = Record.Exception (
        //                              () => Application.Run (
        //                                                     w)); // Invalid - w has not been disposed. Run it in debug mode will throw, otherwise the user may want to run it again
        //Assert.NotNull (exception);

        // TODO: Re-enable this when we are done debug logging of ctx.Source.Title in RaiseSelecting
        //exception = Record.Exception (() => Assert.Equal (string.Empty, w.Title)); // Invalid - w has been disposed and cannot be accessed
        //Assert.NotNull (exception);
        //exception = Record.Exception (() => w.Title = "NewTitle"); // Invalid - w has been disposed and cannot be accessed
        //Assert.NotNull (exception);
#endif
    }

    [Fact]
    public void Run_Creates_Top_Without_Init ()
    {
        Assert.Null (Application.TopRunnable);
        Application.StopAfterFirstIteration = true;

        Application.Iteration += OnApplicationOnIteration;
        Toplevel top = Application.Run (null, "fake");
        Application.Iteration -= OnApplicationOnIteration;
#if DEBUG_IDISPOSABLE
        Assert.Equal (top, Application.TopRunnable);
        Assert.False (top.WasDisposed);
        Exception exception = Record.Exception (Application.Shutdown);
        Assert.NotNull (exception);
        Assert.False (top.WasDisposed);
#endif

        // It's up to caller to dispose it
        top.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (top.WasDisposed);
#endif
        Assert.NotNull (Application.TopRunnable);

        Application.Shutdown ();
        Assert.Null (Application.TopRunnable);

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication?> e) { Assert.NotNull (Application.TopRunnable); }
    }

    [Fact]
    public void Run_T_Creates_Top_Without_Init ()
    {
        Assert.Null (Application.TopRunnable);

        Application.StopAfterFirstIteration = true;

        Application.Run<Toplevel> (null, "fake");
#if DEBUG_IDISPOSABLE
        Assert.False (Application.TopRunnable!.WasDisposed);
        Exception exception = Record.Exception (Application.Shutdown);
        Assert.NotNull (exception);
        Assert.False (Application.TopRunnable!.WasDisposed);

        // It's up to caller to dispose it
        Application.TopRunnable!.Dispose ();
        Assert.True (Application.TopRunnable!.WasDisposed);
#endif
        Assert.NotNull (Application.TopRunnable);

        Application.Shutdown ();
        Assert.Null (Application.TopRunnable);
    }

    [Fact]
    public void Run_t_Does_Not_Creates_Top_Without_Init ()
    {
        // When a Toplevel is created it must already have all the Application configuration loaded
        // This is only possible by two ways:
        // 1 - Using Application.Init first
        // 2 - Using Application.Run() or Application.Run<T>()
        // The Application.Run(new(Toplevel)) must always call Application.Init() first because
        // the new(Toplevel) may be a derived class that is possible using Application static
        // properties that is only available after the Application.Init was called

        Assert.Null (Application.TopRunnable);

        Assert.Throws<NotInitializedException> (() => Application.Run (new Toplevel ()));

        Application.Init ("fake");

        Application.Iteration += OnApplication_OnIteration;
        Application.Run (new Toplevel ());
        Application.Iteration -= OnApplication_OnIteration;
#if DEBUG_IDISPOSABLE
        Assert.False (Application.TopRunnable!.WasDisposed);
        Exception exception = Record.Exception (Application.Shutdown);
        Assert.NotNull (exception);
        Assert.False (Application.TopRunnable!.WasDisposed);

        // It's up to caller to dispose it
        Application.TopRunnable!.Dispose ();
        Assert.True (Application.TopRunnable!.WasDisposed);
#endif
        Assert.NotNull (Application.TopRunnable);

        Application.Shutdown ();
        Assert.Null (Application.TopRunnable);

        return;

        void OnApplication_OnIteration (object s, EventArgs<IApplication?> e)
        {
            Assert.NotNull (Application.TopRunnable);
            Application.RequestStop ();
        }
    }

    private class TestToplevel : Toplevel
    { }

    [Fact]
    public void Run_T_With_V2_Driver_Does_Not_Call_ResetState_After_Init ()
    {
        Assert.False (Application.Initialized);
        Application.Init ("fake");
        Assert.True (Application.Initialized);

        Task.Run (() => { Task.Delay (300).Wait (); })
            .ContinueWith (
                           (t, _) =>
                           {
                               // no longer loading
                               Application.Invoke ((app) => { app.RequestStop (); });
                           },
                           TaskScheduler.FromCurrentSynchronizationContext ());
        Application.Run<TestToplevel> ();
        Assert.NotNull (Application.Driver);
        Assert.NotNull (Application.TopRunnable);
        Assert.False (Application.TopRunnable!.Running);
        Application.TopRunnable!.Dispose ();
        Application.Shutdown ();
    }

    // TODO: Add tests for Run that test errorHandler

    #endregion

    #region ShutdownTests

    [Fact]
    public async Task Shutdown_Allows_Async ()
    {
        var isCompletedSuccessfully = false;

        async Task TaskWithAsyncContinuation ()
        {
            await Task.Yield ();
            await Task.Yield ();

            isCompletedSuccessfully = true;
        }

        Application.Shutdown ();

        Assert.False (isCompletedSuccessfully);
        await TaskWithAsyncContinuation ();
        Thread.Sleep (100);
        Assert.True (isCompletedSuccessfully);
    }

    [Fact]
    public void Shutdown_Resets_SyncContext ()
    {
        Application.Shutdown ();
        Assert.Null (SynchronizationContext.Current);
    }

    #endregion
}
