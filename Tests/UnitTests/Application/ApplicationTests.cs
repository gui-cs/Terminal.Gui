using System.Diagnostics;
using UnitTests;
using Xunit.Abstractions;
using static Terminal.Gui.ConfigurationManager;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ApplicationTests;

public class ApplicationTests
{
    public ApplicationTests (ITestOutputHelper output)
    {
        _output = output;
        ConsoleDriver.RunningUnitTests = true;
        Locations = ConfigLocations.Default;

#if DEBUG_IDISPOSABLE
        View.DebugIDisposable = true;
        View.Instances.Clear ();
        RunState.Instances.Clear ();
#endif
    }

    private readonly ITestOutputHelper _output;

    private object _timeoutLock;

    [Fact]
    public void AddTimeout_Fires ()
    {
        Assert.Null (_timeoutLock);
        _timeoutLock = new ();

        uint timeoutTime = 250;
        var initialized = false;
        var iteration = 0;
        var shutdown = false;
        object timeout = null;
        var timeoutCount = 0;

        Application.InitializedChanged += OnApplicationOnInitializedChanged;

        Application.Init (new FakeDriver ());
        Assert.True (initialized);
        Assert.False (shutdown);

        _output.WriteLine ("Application.Run<Toplevel> ().Dispose ()..");
        Application.Run<Toplevel> ().Dispose ();
        _output.WriteLine ("Back from Application.Run<Toplevel> ().Dispose ()");

        Assert.True (initialized);
        Assert.False (shutdown);

        Assert.Equal (1, timeoutCount);
        Application.Shutdown ();

        Application.InitializedChanged -= OnApplicationOnInitializedChanged;

        lock (_timeoutLock)
        {
            if (timeout is { })
            {
                Application.RemoveTimeout (timeout);
                timeout = null;
            }
        }

        Assert.True (initialized);
        Assert.True (shutdown);

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif
        lock (_timeoutLock)
        {
            _timeoutLock = null;
        }

        return;

        void OnApplicationOnInitializedChanged (object s, EventArgs<bool> a)
        {
            if (a.CurrentValue)
            {
                Application.Iteration += OnApplicationOnIteration;
                initialized = true;

                lock (_timeoutLock)
                {
                    _output.WriteLine ($"Setting timeout for {timeoutTime}ms");
                    timeout = Application.AddTimeout (TimeSpan.FromMilliseconds (timeoutTime), TimeoutCallback);
                }
            }
            else
            {
                Application.Iteration -= OnApplicationOnIteration;
                shutdown = true;
            }
        }

        bool TimeoutCallback ()
        {
            lock (_timeoutLock)
            {
                _output.WriteLine ($"TimeoutCallback. Count: {++timeoutCount}. Application Iteration: {iteration}");

                if (timeout is { })
                {
                    _output.WriteLine ("  Nulling timeout.");
                    timeout = null;
                }
            }

            // False means "don't re-do timer and remove it"
            return false;
        }

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            lock (_timeoutLock)
            {
                if (timeoutCount > 0)
                {
                    _output.WriteLine ($"Iteration #{iteration} - Timeout fired. Calling Application.RequestStop.");
                    Application.RequestStop ();

                    return;
                }
            }

            iteration++;

            // Simulate a delay
            Thread.Sleep ((int)timeoutTime / 10);

            // Worst case scenario - something went wrong
            if (Application.Initialized && iteration > 25)
            {
                _output.WriteLine ($"Too many iterations ({iteration}): Calling Application.RequestStop.");
                Application.RequestStop ();
            }
        }
    }

    [Fact]
    public void Begin_Null_Toplevel_Throws ()
    {
        // Setup Mock driver
        Init ();

        // Test null Toplevel
        Assert.Throws<ArgumentNullException> (() => Application.Begin (null));

        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [AutoInitShutdown (verifyShutdown: true)]
    public void Begin_Sets_Application_Top_To_Console_Size ()
    {
        Assert.Null (Application.Top);
        Toplevel top = new ();
        Application.Begin (top);
        Assert.Equal (new (0, 0, 80, 25), Application.Top.Frame);
        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);
        Assert.Equal (new (0, 0, 5, 5), Application.Top.Frame);
        top.Dispose ();
    }

    [Fact]
    public void End_And_Shutdown_Should_Not_Dispose_ApplicationTop ()
    {
        Init ();

        RunState rs = Application.Begin (new ());
        Assert.Equal (rs.Toplevel, Application.Top);
        Application.End (rs);

#if DEBUG_IDISPOSABLE
        Assert.True (rs.WasDisposed);
        Assert.False (Application.Top.WasDisposed); // Is true because the rs.Toplevel is the same as Application.Top
#endif

        Assert.Null (rs.Toplevel);

        Toplevel top = Application.Top;

#if DEBUG_IDISPOSABLE
        Exception exception = Record.Exception (() => Shutdown ());
        Assert.NotNull (exception);
        Assert.False (top.WasDisposed);
        top.Dispose ();
        Assert.True (top.WasDisposed);
#endif
        Shutdown ();
        Assert.Null (Application.Top);
    }

    [Fact]
    public void Init_Begin_End_Cleans_Up ()
    {
        // Start stopwatch
        Stopwatch stopwatch = new Stopwatch ();
        stopwatch.Start ();

        Init ();

        // Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
        // if we don't stop
        Application.Iteration += (s, a) => { Application.RequestStop (); };

        RunState runstate = null;

        EventHandler<RunStateEventArgs> newRunStateFn = (s, e) =>
                                                        {
                                                            Assert.NotNull (e.State);
                                                            runstate = e.State;
                                                        };
        Application.NotifyNewRunState += newRunStateFn;

        var topLevel = new Toplevel ();
        RunState rs = Application.Begin (topLevel);
        Assert.NotNull (rs);
        Assert.NotNull (runstate);
        Assert.Equal (rs, runstate);

        Assert.Equal (topLevel, Application.Top);

        Application.NotifyNewRunState -= newRunStateFn;
        Application.End (runstate);

        Assert.NotNull (Application.Top);
        Assert.NotNull (Application.MainLoop);
        Assert.NotNull (Application.Driver);

        topLevel.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);

        // Stop stopwatch
        stopwatch.Stop ();

        _output.WriteLine ($"Load took {stopwatch.ElapsedMilliseconds} ms");

    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void Init_DriverName_Should_Pick_Correct_Driver (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        Application.Init (driverName: driverType.Name);
        Assert.NotNull (Application.Driver);
        Assert.NotEqual (driver, Application.Driver);
        Assert.Equal (driverType, Application.Driver?.GetType ());
        Shutdown ();
    }

    [Fact]
    public void Init_Null_Driver_Should_Pick_A_Driver ()
    {
        Application.Init ();

        Assert.NotNull (Application.Driver);

        Shutdown ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void Init_ResetState_Resets_Properties (Type driverType)
    {
        ThrowOnJsonErrors = true;

        // For all the fields/properties of Application, check that they are reset to their default values

        // Set some values

        Application.Init (driverName: driverType.Name);

        // Application.IsInitialized = true;

        // Reset
        Application.ResetState ();

        void CheckReset ()
        {
            // Check that all fields and properties are set to their default values

            // Public Properties
            Assert.Null (Application.Top);
            Assert.Null (Application.MouseGrabView);
            Assert.Null (Application.WantContinuousButtonPressedView);

            // Don't check Application.ForceDriver
            // Assert.Empty (Application.ForceDriver);
            // Don't check Application.Force16Colors
            //Assert.False (Application.Force16Colors);
            Assert.Null (Application.Driver);
            Assert.Null (Application.MainLoop);
            Assert.False (Application.EndAfterFirstIteration);
            Assert.Equal (Key.Tab.WithShift, Application.PrevTabKey);
            Assert.Equal (Key.Tab, Application.NextTabKey);
            Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
            Assert.Equal (Key.F6, Application.NextTabGroupKey);
            Assert.Equal (Key.Esc, Application.QuitKey);

            // Internal properties
            Assert.False (Application.Initialized);
            Assert.Equal (Application.GetSupportedCultures (), Application.SupportedCultures);
            Assert.Equal (Application.GetAvailableCulturesFromEmbeddedResources (), Application.SupportedCultures);
            Assert.False (Application._forceFakeConsole);
            Assert.Equal (-1, Application.MainThreadId);
            Assert.Empty (Application.TopLevels);
            Assert.Empty (Application._cachedViewsUnderMouse);

            // Mouse
            // Do not reset _lastMousePosition
            //Assert.Null (Application._lastMousePosition);

            // Navigation
            Assert.Null (Application.Navigation);

            // Events - Can't check
            //Assert.Null (Application.NotifyNewRunState);
            //Assert.Null (Application.NotifyNewRunState);
            //Assert.Null (Application.Iteration);
            //Assert.Null (Application.SizeChanging);
            //Assert.Null (Application.GrabbedMouse);
            //Assert.Null (Application.UnGrabbingMouse);
            //Assert.Null (Application.GrabbedMouse);
            //Assert.Null (Application.UnGrabbedMouse);
            //Assert.Null (Application.MouseEvent);
            //Assert.Null (Application.KeyDown);
            //Assert.Null (Application.KeyUp);
        }

        CheckReset ();

        // Set the values that can be set
        Application.Initialized = true;
        Application._forceFakeConsole = true;
        Application.MainThreadId = 1;

        //Application._topLevels = new List<Toplevel> ();
        Application._cachedViewsUnderMouse.Clear ();

        //Application.SupportedCultures = new List<CultureInfo> ();
        Application.Force16Colors = true;

        //Application.ForceDriver = "driver";
        Application.EndAfterFirstIteration = true;
        Application.PrevTabGroupKey = Key.A;
        Application.NextTabGroupKey = Key.B;
        Application.QuitKey = Key.C;
        Application.KeyBindings.Add (Key.D, Command.Cancel);

        Application._cachedViewsUnderMouse.Clear ();

        //Application.WantContinuousButtonPressedView = new View ();

        // Mouse
        Application._lastMousePosition = new Point (1, 1);

        Application.Navigation = new ();

        Application.ResetState ();
        CheckReset ();

        ThrowOnJsonErrors = false;
    }

    [Fact]
    public void Init_Shutdown_Cleans_Up ()
    {
        // Verify initial state is per spec
        //Pre_Init_State ();

        Application.Init (new FakeDriver ());

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
    public void Shutdown_Alone_Does_Nothing () { Application.Shutdown (); }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void Init_Shutdown_Fire_InitializedChanged (Type driverType)
    {
        var initialized = false;
        var shutdown = false;

        Application.InitializedChanged += OnApplicationOnInitializedChanged;

        Application.Init (driverName: driverType.Name);
        Assert.True (initialized);
        Assert.False (shutdown);

        Application.Shutdown ();
        Assert.True (initialized);
        Assert.True (shutdown);

        Application.InitializedChanged -= OnApplicationOnInitializedChanged;

        return;

        void OnApplicationOnInitializedChanged (object s, EventArgs<bool> a)
        {
            if (a.CurrentValue)
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
    public void Init_Unbalanced_Throws ()
    {
        Application.Init (new FakeDriver ());

        Assert.Throws<InvalidOperationException> (
                                                  () =>
                                                      Application.InternalInit (
                                                                                new FakeDriver ()
                                                                               )
                                                 );
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);

        // Now try the other way
        Application.InternalInit (new FakeDriver ());

        Assert.Throws<InvalidOperationException> (() => Application.Init (new FakeDriver ()));
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    public void Init_WithoutTopLevelFactory_Begin_End_Cleans_Up ()
    {
        // Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
        // if we don't stop
        Application.Iteration += (s, a) => { Application.RequestStop (); };

        // NOTE: Run<T>, when called after Init has been called behaves differently than
        // when called if Init has not been called.
        Toplevel topLevel = new ();
        Application.InternalInit (new FakeDriver ());

        RunState runstate = null;

        EventHandler<RunStateEventArgs> newRunStateFn = (s, e) =>
                                                        {
                                                            Assert.NotNull (e.State);
                                                            runstate = e.State;
                                                        };
        Application.NotifyNewRunState += newRunStateFn;

        RunState rs = Application.Begin (topLevel);
        Assert.NotNull (rs);
        Assert.NotNull (runstate);
        Assert.Equal (rs, runstate);

        Assert.Equal (topLevel, Application.Top);

        Application.NotifyNewRunState -= newRunStateFn;
        Application.End (runstate);

        Assert.NotNull (Application.Top);
        Assert.NotNull (Application.MainLoop);
        Assert.NotNull (Application.Driver);

        topLevel.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    public void Init_NoParam_ForceDriver_Works ()
    {
        Application.ForceDriver = "FakeDriver";
        Application.Init ();
        Assert.IsType<FakeDriver> (Application.Driver);
        Application.ResetState ();
    }

    [Fact]
    public void Init_KeyBindings_Set_To_Defaults ()
    {
        // arrange
        Locations = ConfigLocations.All;
        ThrowOnJsonErrors = true;

        Application.QuitKey = Key.Q;

        Application.Init (new FakeDriver ());

        Assert.Equal (Key.Esc, Application.QuitKey);

        Application.Shutdown ();
    }

    [Fact]
    public void Init_KeyBindings_Set_To_Custom ()
    {
        // arrange
        Locations = ConfigLocations.Runtime;
        ThrowOnJsonErrors = true;

        RuntimeConfig = """
                         {
                               "Application.QuitKey": "Ctrl-Q"
                         }
                 """;

        Assert.Equal (Key.Esc, Application.QuitKey);

        // Act
        Application.Init (new FakeDriver ());

        Assert.Equal (Key.Q.WithCtrl, Application.QuitKey);

        Assert.True (Application.KeyBindings.TryGet (Key.Q.WithCtrl, out _));

        Application.Shutdown ();
        Locations = ConfigLocations.Default;
    }

    [Fact]
    [AutoInitShutdown (verifyShutdown: true)]
    public void Internal_Properties_Correct ()
    {
        Assert.True (Application.Initialized);
        Assert.Null (Application.Top);
        RunState rs = Application.Begin (new ());
        Assert.Equal (Application.Top, rs.Toplevel);
        Assert.Null (Application.MouseGrabView); // public
        Assert.Null (Application.WantContinuousButtonPressedView); // public
        Application.Top.Dispose ();
    }

    // Invoke Tests
    // TODO: Test with threading scenarios
    [Fact]
    public void Invoke_Adds_Idle ()
    {
        Application.Init (new FakeDriver ());
        var top = new Toplevel ();
        RunState rs = Application.Begin (top);
        var firstIteration = false;

        var actionCalled = 0;
        Application.Invoke (() => { actionCalled++; });
        Application.MainLoop.Running = true;
        Application.RunIteration (ref rs, firstIteration);
        Assert.Equal (1, actionCalled);
        top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void Run_Iteration_Fires ()
    {
        var iteration = 0;

        Application.Init (new FakeDriver ());

        Application.Iteration += Application_Iteration;
        Application.Run<Toplevel> ().Dispose ();

        Assert.Equal (1, iteration);
        Application.Shutdown ();

        return;

        void Application_Iteration (object sender, IterationEventArgs e)
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
    public void Screen_Size_Changes ()
    {
        var driver = new FakeDriver ();
        Application.Init (driver);
        Assert.Equal (new (0, 0, 80, 25), driver.Screen);
        Assert.Equal (new (0, 0, 80, 25), Application.Screen);

        driver.Cols = 100;
        driver.Rows = 30;
        // IConsoleDriver.Screen isn't assignable
        //driver.Screen = new (0, 0, driver.Cols, Rows);
        Assert.Equal (new (0, 0, 100, 30), driver.Screen);
        Assert.NotEqual (new (0, 0, 100, 30), Application.Screen);
        Assert.Equal (new (0, 0, 80, 25), Application.Screen);
        Application.Screen = new (0, 0, driver.Cols, driver.Rows);
        Assert.Equal (new (0, 0, 100, 30), driver.Screen);

        Application.Shutdown ();
    }

    [Fact]
    public void InitState_Throws_If_Driver_Is_Null ()
    {
        Assert.Throws<ArgumentNullException> (static () => Application.SubscribeDriverEvents ());
    }

    private void Init ()
    {
        Application.Init (new FakeDriver ());
        Assert.NotNull (Application.Driver);
        Assert.NotNull (Application.MainLoop);
        Assert.NotNull (SynchronizationContext.Current);
    }

    private void Shutdown () { Application.Shutdown (); }

    #region RunTests

    [Fact]
    public void Run_T_After_InitWithDriver_with_TopLevel_Does_Not_Throws ()
    {
        // Setup Mock driver
        Init ();

        Application.Iteration += (s, e) => Application.RequestStop ();

        // Run<Toplevel> when already initialized or not with a Driver will not throw (because Window is derived from Toplevel)
        // Using another type not derived from Toplevel will throws at compile time
        Application.Run<Window> ();
        Assert.True (Application.Top is Window);

        Application.Top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    public void Run_T_After_InitWithDriver_with_TopLevel_and_Driver_Does_Not_Throws ()
    {
        // Setup Mock driver
        Init ();

        Application.Iteration += (s, e) => Application.RequestStop ();

        // Run<Toplevel> when already initialized or not with a Driver will not throw (because Window is derived from Toplevel)
        // Using another type not derived from Toplevel will throws at compile time
        Application.Run<Window> (null, new FakeDriver ());
        Assert.True (Application.Top is Window);

        Application.Top.Dispose ();

        // Run<Toplevel> when already initialized or not with a Driver will not throw (because Dialog is derived from Toplevel)
        Application.Run<Dialog> (null, new FakeDriver ());
        Assert.True (Application.Top is Dialog);

        Application.Top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_T_After_Init_Does_Not_Disposes_Application_Top ()
    {
        Init ();

        // Init doesn't create a Toplevel and assigned it to Application.Top
        // but Begin does
        var initTop = new Toplevel ();

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.NotEqual (initTop, Application.Top);
#if DEBUG_IDISPOSABLE
                                     Assert.False (initTop.WasDisposed);
#endif
                                     Application.RequestStop ();
                                 };

        Application.Run<Toplevel> ();

#if DEBUG_IDISPOSABLE
        Assert.False (initTop.WasDisposed);
        initTop.Dispose ();
        Assert.True (initTop.WasDisposed);
#endif
        Application.Top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_T_After_InitWithDriver_with_TestTopLevel_DoesNotThrow ()
    {
        // Setup Mock driver
        Init ();

        Application.Iteration += (s, a) => { Application.RequestStop (); };

        // Init has been called and we're passing no driver to Run<TestTopLevel>. This is ok.
        Application.Run<Toplevel> ();

        Application.Top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_T_After_InitNullDriver_with_TestTopLevel_DoesNotThrow ()
    {
        Application.ForceDriver = "FakeDriver";

        Application.Init ();
        Assert.Equal (typeof (FakeDriver), Application.Driver?.GetType ());

        Application.Iteration += (s, a) => { Application.RequestStop (); };

        // Init has been called, selecting FakeDriver; we're passing no driver to Run<TestTopLevel>. Should be fine.
        Application.Run<Toplevel> ();

        Application.Top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_T_Init_Driver_Cleared_with_TestTopLevel_Throws ()
    {
        Init ();

        Application.Driver = null;

        // Init has been called, but Driver has been set to null. Bad.
        Assert.Throws<InvalidOperationException> (() => Application.Run<Toplevel> ());

        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_T_NoInit_DoesNotThrow ()
    {
        Application.ForceDriver = "FakeDriver";

        Application.Iteration += (s, a) => { Application.RequestStop (); };

        Application.Run<Toplevel> ();
        Assert.Equal (typeof (FakeDriver), Application.Driver?.GetType ());

        Application.Top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_T_NoInit_WithDriver_DoesNotThrow ()
    {
        Application.Iteration += (s, a) => { Application.RequestStop (); };

        // Init has NOT been called and we're passing a valid driver to Run<TestTopLevel>. This is ok.
        Application.Run<Toplevel> (null, new FakeDriver ());

        Application.Top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_RequestStop_Stops ()
    {
        // Setup Mock driver
        Init ();

        var top = new Toplevel ();
        RunState rs = Application.Begin (top);
        Assert.NotNull (rs);

        Application.Iteration += (s, a) => { Application.RequestStop (); };

        Application.Run (top);

        top.Dispose ();
        Application.Shutdown ();
        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_RunningFalse_Stops ()
    {
        // Setup Mock driver
        Init ();

        var top = new Toplevel ();
        RunState rs = Application.Begin (top);
        Assert.NotNull (rs);

        Application.Iteration += (s, a) => { top.Running = false; };

        Application.Run (top);

        top.Dispose ();
        Application.Shutdown ();
        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Run_Loaded_Ready_Unloaded_Events ()
    {
        Init ();
        Toplevel top = new ();
        var count = 0;
        top.Loaded += (s, e) => count++;
        top.Ready += (s, e) => count++;
        top.Unloaded += (s, e) => count++;
        Application.Iteration += (s, a) => Application.RequestStop ();
        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
        Assert.Equal (3, count);
    }

    // TODO: All Toplevel layout tests should be moved to ToplevelTests.cs
    [Fact]
    public void Run_A_Modal_Toplevel_Refresh_Background_On_Moving ()
    {
        Init ();

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var w = new Window
        {
            Width = 5, Height = 5,
            Arrangement = ViewArrangement.Movable
        };
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 10);
        RunState rs = Application.Begin (w);

        // Don't use visuals to test as style of border can change over time.
        Assert.Equal (new (0, 0), w.Frame.Location);

        Application.RaiseMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (w.Border, Application.MouseGrabView);
        Assert.Equal (new (0, 0), w.Frame.Location);

        // Move down and to the right.
        Application.RaiseMouseEvent (new () { ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
        Assert.Equal (new (1, 1), w.Frame.Location);

        Application.End (rs);
        w.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void End_Does_Not_Dispose ()
    {
        Init ();

        var top = new Toplevel ();

        Window w = new ();
        w.Ready += (s, e) => Application.RequestStop (); // Causes `End` to be called
        Application.Run (w);

#if DEBUG_IDISPOSABLE
        Assert.False (w.WasDisposed);
#endif

        Assert.NotNull (w);
        Assert.Equal (string.Empty, w.Title); // Valid - w has not been disposed. The user may want to run it again
        Assert.NotNull (Application.Top);
        Assert.Equal (w, Application.Top);
        Assert.NotEqual (top, Application.Top);

        Application.Run (w); // Valid - w has not been disposed.

#if DEBUG_IDISPOSABLE
        Assert.False (w.WasDisposed);
        Exception exception = Record.Exception (Application.Shutdown); // Invalid - w has not been disposed.
        Assert.NotNull (exception);

        w.Dispose ();
        Assert.True (w.WasDisposed);

        //exception = Record.Exception (
        //                              () => Application.Run (
        //                                                     w)); // Invalid - w has been disposed. Run it in debug mode will throw, otherwise the user may want to run it again
        //Assert.NotNull (exception);

        exception = Record.Exception (() => Assert.Equal (string.Empty, w.Title)); // Invalid - w has been disposed and cannot be accessed
        Assert.NotNull (exception);
        exception = Record.Exception (() => w.Title = "NewTitle"); // Invalid - w has been disposed and cannot be accessed
        Assert.NotNull (exception);
#endif
        Application.Shutdown ();
        Assert.NotNull (w);
        Assert.NotNull (top);
        Assert.Null (Application.Top);
    }

    [Fact]
    public void Run_Creates_Top_Without_Init ()
    {
        var driver = new FakeDriver ();

        Assert.Null (Application.Top);

        Application.Iteration += (s, e) =>
                                 {
                                     Assert.NotNull (Application.Top);
                                     Application.RequestStop ();
                                 };
        Toplevel top = Application.Run (null, driver);
#if DEBUG_IDISPOSABLE
        Assert.Equal (top, Application.Top);
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
        Assert.NotNull (Application.Top);

        Application.Shutdown ();
        Assert.Null (Application.Top);
    }

    [Fact]
    public void Run_T_Creates_Top_Without_Init ()
    {
        var driver = new FakeDriver ();

        Assert.Null (Application.Top);

        Application.Iteration += (s, e) =>
                                 {
                                     Assert.NotNull (Application.Top);
                                     Application.RequestStop ();
                                 };
        Application.Run<Toplevel> (null, driver);
#if DEBUG_IDISPOSABLE
        Assert.False (Application.Top.WasDisposed);
        Exception exception = Record.Exception (Application.Shutdown);
        Assert.NotNull (exception);
        Assert.False (Application.Top.WasDisposed);

        // It's up to caller to dispose it
        Application.Top.Dispose ();
        Assert.True (Application.Top.WasDisposed);
#endif
        Assert.NotNull (Application.Top);

        Application.Shutdown ();
        Assert.Null (Application.Top);
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
        var driver = new FakeDriver ();

        Assert.Null (Application.Top);

        Assert.Throws<InvalidOperationException> (() => Application.Run (new Toplevel ()));

        Application.Init (driver);

        Application.Iteration += (s, e) =>
                                 {
                                     Assert.NotNull (Application.Top);
                                     Application.RequestStop ();
                                 };
        Application.Run (new Toplevel ());
#if DEBUG_IDISPOSABLE
        Assert.False (Application.Top.WasDisposed);
        Exception exception = Record.Exception (Application.Shutdown);
        Assert.NotNull (exception);
        Assert.False (Application.Top.WasDisposed);

        // It's up to caller to dispose it
        Application.Top.Dispose ();
        Assert.True (Application.Top.WasDisposed);
#endif
        Assert.NotNull (Application.Top);

        Application.Shutdown ();
        Assert.Null (Application.Top);
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

        Init ();
        Application.Shutdown ();

        Assert.False (isCompletedSuccessfully);
        await TaskWithAsyncContinuation ();
        Thread.Sleep (100);
        Assert.True (isCompletedSuccessfully);
    }

    [Fact]
    public void Shutdown_Resets_SyncContext ()
    {
        Init ();
        Application.Shutdown ();
        Assert.Null (SynchronizationContext.Current);
    }

    #endregion
}
