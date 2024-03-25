using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ApplicationTests;

public class ApplicationTests
{
    private readonly ITestOutputHelper _output;

    public ApplicationTests (ITestOutputHelper output)
    {
        _output = output;
        ConsoleDriver.RunningUnitTests = true;

#if DEBUG_IDISPOSABLE
        Responder.Instances.Clear ();
        RunState.Instances.Clear ();
#endif
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
    [AutoInitShutdown]
    public void Begin_Sets_Application_Top_To_Console_Size ()
    {
        Assert.Null (Application.Top);
        Application.Begin (new ());
        Assert.Equal (new Rectangle (0, 0, 80, 25), Application.Top.Frame);
        ((FakeDriver)Application.Driver).SetBufferSize (5, 5);
        Assert.Equal (new Rectangle (0, 0, 5, 5), Application.Top.Frame);
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

        var top = Application.Top;

#if DEBUG_IDISPOSABLE
        var exception = Record.Exception (() => Shutdown ());
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
        Init ();

        // Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
        // if we don't stop
        Application.Iteration += (s, a) => { Application.RequestStop (); };

        RunState runstate = null;

        EventHandler<RunStateEventArgs> NewRunStateFn = (s, e) =>
                                                        {
                                                            Assert.NotNull (e.State);
                                                            runstate = e.State;
                                                        };
        Application.NotifyNewRunState += NewRunStateFn;

        var topLevel = new Toplevel ();
        RunState rs = Application.Begin (topLevel);
        Assert.NotNull (rs);
        Assert.NotNull (runstate);
        Assert.Equal (rs, runstate);

        Assert.Equal (topLevel, Application.Top);
        Assert.Equal (topLevel, Application.Current);

        Application.NotifyNewRunState -= NewRunStateFn;
        Application.End (runstate);

        Assert.Null (Application.Current);
        Assert.NotNull (Application.Top);
        Assert.NotNull (Application.MainLoop);
        Assert.NotNull (Application.Driver);

        topLevel.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void Init_DriverName_Should_Pick_Correct_Driver (Type driverType)
    {
        var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
        Application.Init (driverName: driverType.Name);
        Assert.NotNull (Application.Driver);
        Assert.NotEqual(driver, Application.Driver);
        Assert.Equal (driverType, Application.Driver.GetType ());
        Shutdown ();
    }

    [Fact]
    public void Init_Null_Driver_Should_Pick_A_Driver ()
    {
        Application.Init ();

        Assert.NotNull (Application.Driver);

        Shutdown ();
    }

    [Fact]
    public void Init_ResetState_Resets_Properties ()
    {
        ConfigurationManager.ThrowOnJsonErrors = true;

        // For all the fields/properties of Application, check that they are reset to their default values

        // Set some values

        Application.Init ();
        Application._initialized = true;

        // Reset
        Application.ResetState ();

        void CheckReset ()
        {
            // Check that all fields and properties are set to their default values

            // Public Properties
            Assert.Null (Application.Top);
            Assert.Null (Application.Current);
            Assert.Null (Application.MouseGrabView);
            Assert.Null (Application.WantContinuousButtonPressedView);

            // Don't check Application.ForceDriver
            // Assert.Empty (Application.ForceDriver);
            Assert.False (Application.Force16Colors);
            Assert.Null (Application.Driver);
            Assert.Null (Application.MainLoop);
            Assert.False (Application.EndAfterFirstIteration);
            Assert.Equal (Key.Empty, Application.AlternateBackwardKey);
            Assert.Equal (Key.Empty, Application.AlternateForwardKey);
            Assert.Equal (Key.Empty, Application.QuitKey);
            Assert.Null (Application.OverlappedChildren);
            Assert.Null (Application.OverlappedTop);

            // Internal properties
            Assert.False (Application._initialized);
            Assert.Equal (Application.GetSupportedCultures (), Application.SupportedCultures);
            Assert.False (Application._forceFakeConsole);
            Assert.Equal (-1, Application._mainThreadId);
            Assert.Empty (Application._topLevels);
            Assert.Null (Application._mouseEnteredView);

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
        Application._initialized = true;
        Application._forceFakeConsole = true;
        Application._mainThreadId = 1;

        //Application._topLevels = new List<Toplevel> ();
        Application._mouseEnteredView = new View ();

        //Application.SupportedCultures = new List<CultureInfo> ();
        Application.Force16Colors = true;

        //Application.ForceDriver = "driver";
        Application.EndAfterFirstIteration = true;
        Application.AlternateBackwardKey = Key.A;
        Application.AlternateForwardKey = Key.B;
        Application.QuitKey = Key.C;

        //Application.OverlappedChildren = new List<View> ();
        //Application.OverlappedTop = 
        Application._mouseEnteredView = new View ();

        //Application.WantContinuousButtonPressedView = new View ();

        Application.ResetState ();
        CheckReset ();

        ConfigurationManager.ThrowOnJsonErrors = false;
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
        Assert.Empty (Responder.Instances);
#endif
    }

    [Fact]
    public void Init_Unbalanced_Throws ()
    {
        Application.Init (new FakeDriver ());

        Toplevel topLevel = null;

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
        topLevel = null;
        Application.InternalInit (new FakeDriver ());

        Assert.Throws<InvalidOperationException> (() => Application.Init (new FakeDriver ()));
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    public void InitWithoutTopLevelFactory_Begin_End_Cleans_Up ()
    {
        // Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
        // if we don't stop
        Application.Iteration += (s, a) => { Application.RequestStop (); };

        // NOTE: Run<T>, when called after Init has been called behaves differently than
        // when called if Init has not been called.
        Toplevel topLevel = new ();
        Application.InternalInit (new FakeDriver ());

        RunState runstate = null;

        EventHandler<RunStateEventArgs> NewRunStateFn = (s, e) =>
                                                        {
                                                            Assert.NotNull (e.State);
                                                            runstate = e.State;
                                                        };
        Application.NotifyNewRunState += NewRunStateFn;

        RunState rs = Application.Begin (topLevel);
        Assert.NotNull (rs);
        Assert.NotNull (runstate);
        Assert.Equal (rs, runstate);

        Assert.Equal (topLevel, Application.Top);
        Assert.Equal (topLevel, Application.Current);

        Application.NotifyNewRunState -= NewRunStateFn;
        Application.End (runstate);

        Assert.Null (Application.Current);
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
    [AutoInitShutdown]
    public void Internal_Properties_Correct ()
    {
        Assert.True (Application._initialized);
        Assert.Null (Application.Top);
        RunState rs = Application.Begin (new ());
        Assert.Equal (Application.Top, rs.Toplevel);
        Assert.Null (Application.MouseGrabView); // public
        Assert.Null (Application.WantContinuousButtonPressedView); // public
        Assert.False (Application.MoveToOverlappedChild (Application.Top));
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
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (1, actionCalled);
        top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void SetCurrentAsTop_Run_A_Not_Modal_Toplevel_Make_It_The_Current_Application_Top ()
    {
        var top = new Toplevel ();

        var t1 = new Toplevel ();
        var t2 = new Toplevel ();
        var t3 = new Toplevel ();

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var d = new Dialog ();
        var t4 = new Toplevel ();

        // t1, t2, t3, d, t4
        var iterations = 5;

        t1.Ready += (s, e) =>
                    {
                        Assert.Equal (t1, Application.Top);
                        Application.Run (t2);
                    };

        t2.Ready += (s, e) =>
                    {
                        Assert.Equal (t2, Application.Top);
                        Application.Run (t3);
                    };

        t3.Ready += (s, e) =>
                    {
                        Assert.Equal (t3, Application.Top);
                        Application.Run (d);
                    };

        d.Ready += (s, e) =>
                   {
                       Assert.Equal (t3, Application.Top);
                       Application.Run (t4);
                   };

        t4.Ready += (s, e) =>
                    {
                        Assert.Equal (t4, Application.Top);
                        t4.RequestStop ();
                        d.RequestStop ();
                        t3.RequestStop ();
                        t2.RequestStop ();
                    };

        // Now this will close the OverlappedContainer when all OverlappedChildren was closed
        t2.Closed += (s, _) => { t1.RequestStop (); };

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 5)
                                     {
                                         // The Current still is t4 because Current.Running is false.
                                         Assert.Equal (t4, Application.Current);
                                         Assert.False (Application.Current.Running);
                                         Assert.Equal (t4, Application.Top);
                                     }
                                     else if (iterations == 4)
                                     {
                                         // The Current is d and Current.Running is false.
                                         Assert.Equal (d, Application.Current);
                                         Assert.False (Application.Current.Running);
                                         Assert.Equal (t4, Application.Top);
                                     }
                                     else if (iterations == 3)
                                     {
                                         // The Current is t3 and Current.Running is false.
                                         Assert.Equal (t3, Application.Current);
                                         Assert.False (Application.Current.Running);
                                         Assert.Equal (t3, Application.Top);
                                     }
                                     else if (iterations == 2)
                                     {
                                         // The Current is t2 and Current.Running is false.
                                         Assert.Equal (t2, Application.Current);
                                         Assert.False (Application.Current.Running);
                                         Assert.Equal (t2, Application.Top);
                                     }
                                     else
                                     {
                                         // The Current is t1.
                                         Assert.Equal (t1, Application.Current);
                                         Assert.False (Application.Current.Running);
                                         Assert.Equal (t1, Application.Top);
                                     }

                                     iterations--;
                                 };

        Application.Run (t1);

        Assert.Equal (t1, Application.Top);
        // top wasn't run and so never was added to toplevel's stack
        Assert.NotEqual (top, Application.Top);
#if DEBUG_IDISPOSABLE
        Assert.False (Application.Top.WasDisposed);
        t1.Dispose ();
        Assert.True (Application.Top.WasDisposed);
#endif
    }

    private void Init ()
    {
        Application.Init (new FakeDriver ());
        Assert.NotNull (Application.Driver);
        Assert.NotNull (Application.MainLoop);
        Assert.NotNull (SynchronizationContext.Current);
    }

    private void Post_Init_State ()
    {
        Assert.NotNull (Application.Driver);
        Assert.NotNull (Application.Top);
        Assert.NotNull (Application.Current);
        Assert.NotNull (Application.MainLoop);

        // FakeDriver is always 80x25
        Assert.Equal (80, Application.Driver.Cols);
        Assert.Equal (25, Application.Driver.Rows);
    }

    private void Pre_Init_State ()
    {
        Assert.Null (Application.Driver);
        Assert.Null (Application.Top);
        Assert.Null (Application.Current);
        Assert.Null (Application.MainLoop);
    }

    private void Shutdown () { Application.Shutdown (); }

    private class TestToplevel : Toplevel
    {
        public TestToplevel () { IsOverlappedContainer = false; }
    }

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
                                     Assert.NotEqual(initTop, Application.Top);
#if DEBUG_IDISPOSABLE
                                     Assert.False (initTop.WasDisposed);
#endif
                                     Application.RequestStop ();
                                 };

        Application.Run<TestToplevel> ();

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
        Application.Run<TestToplevel> ();

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
        Assert.Equal (typeof (FakeDriver), Application.Driver.GetType ());

        Application.Iteration += (s, a) => { Application.RequestStop (); };

        // Init has been called, selecting FakeDriver; we're passing no driver to Run<TestTopLevel>. Should be fine.
        Application.Run<TestToplevel> ();

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
        Assert.Throws<InvalidOperationException> (() => Application.Run<TestToplevel> ());

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

        Application.Run<TestToplevel> ();
        Assert.Equal (typeof (FakeDriver), Application.Driver.GetType ());

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
        Application.Run<TestToplevel> (null, new FakeDriver ());

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
        Assert.Equal (top, Application.Current);

        Application.Iteration += (s, a) => { Application.RequestStop (); };

        Application.Run (top);

        top.Dispose ();
        Application.Shutdown ();
        Assert.Null (Application.Current);
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
        Assert.Equal (top, Application.Current);

        Application.Iteration += (s, a) => { top.Running = false; };

        Application.Run (top);

        top.Dispose ();
        Application.Shutdown ();
        Assert.Null (Application.Current);
        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [TestRespondersDisposed]

    public void Run_Loaded_Ready_Unlodaded_Events ()
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
    public void Run_Toplevel_With_Modal_View_Does_Not_Refresh_If_Not_Dirty ()
    {
        Init ();
        var count = 0;

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        Dialog d = null;
        Toplevel top = new ();
        top.DrawContent += (s, a) => count++;
        int iteration = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iteration++;

                                     if (iteration == 0)
                                     {
                                         // TODO: Don't use Dialog here as it has more layout logic. Use Window instead.
                                         d = new Dialog ();
                                         d.DrawContent += (s, a) => count++;
                                         Application.Run (d);
                                     }
                                     else if (iteration < 3)
                                     {
                                         Application.OnMouseEvent (
                                                                   new MouseEventEventArgs (
                                                                                            new MouseEvent
                                                                                            { X = 0, Y = 0, Flags = MouseFlags.ReportMousePosition }
                                                                                           )
                                                                  );
                                         Assert.False (top.NeedsDisplay);
                                         Assert.False (top.SubViewNeedsDisplay);
                                         Assert.False (top.LayoutNeeded);
                                         Assert.False (d.NeedsDisplay);
                                         Assert.False (d.SubViewNeedsDisplay);
                                         Assert.False (d.LayoutNeeded);
                                     }
                                     else
                                     {
                                         Application.RequestStop ();
                                     }
                                 };
        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();

        // 1 - First top load, 1 - Dialog load, 1 - Dialog unload, Total - 3.
        Assert.Equal (3, count);
    }

    // TODO: All Toplevel layout tests should be moved to ToplevelTests.cs
    [Fact]
    public void Run_A_Modal_Toplevel_Refresh_Background_On_Moving ()
    {
        Init ();

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var w = new Window { Width = 5, Height = 5 };
        ((FakeDriver)Application.Driver).SetBufferSize (10, 10);
        RunState rs = Application.Begin (w);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───┐
│   │
│   │
│   │
└───┘",
                                                      _output
                                                     );

        Attribute [] attributes =
        {
            // 0
            new (ColorName.White, ColorName.Black),

            // 1
            Colors.ColorSchemes ["Base"].Normal
        };

        TestHelpers.AssertDriverAttributesAre (
                                               @"
1111100000
1111100000
1111100000
1111100000
1111100000
",
                                               null,
                                               attributes
                                              );

        // TODO: In PR #2920 this breaks because the mouse is not grabbed anymore.
        // TODO: Move the mouse grap/drag mode from Toplevel to Border.
        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 0, Y = 0, Flags = MouseFlags.Button1Pressed }
                                                          )
                                 );
        Assert.Equal (w.Border, Application.MouseGrabView);

        // Move down and to the right.
        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent
                                                           {
                                                               X = 1,
                                                               Y = 1,
                                                               Flags = MouseFlags.Button1Pressed
                                                                       | MouseFlags.ReportMousePosition
                                                           }
                                                          )
                                 );
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌───┐
 │   │
 │   │
 │   │
 └───┘",
                                                      _output
                                                     );

        attributes = new []
        {
            // 0
            new (ColorName.White, ColorName.Black),

            // 1
            Colors.ColorSchemes ["Base"].Normal
        };

        TestHelpers.AssertDriverAttributesAre (
                                               @"
0000000000
0111110000
0111110000
0111110000
0111110000
0111110000
",
                                               null,
                                               attributes
                                              );

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
        Application.Run(w);

#if DEBUG_IDISPOSABLE
        Assert.False (w.WasDisposed);
#endif

        Assert.NotNull (w);
        Assert.Equal (string.Empty, w.Title); // Valid - w has not been disposed. The user may want to run it again
        Assert.NotNull (Application.Top);
        Assert.Equal(w, Application.Top);
        Assert.NotEqual(top, Application.Top);
        Assert.Null (Application.Current);

        Application.Run(w); // Valid - w has not been disposed.

#if DEBUG_IDISPOSABLE
        Assert.False (w.WasDisposed);
        var exception = Record.Exception (() => Application.Shutdown()); // Invalid - w has not been disposed.
        Assert.NotNull (exception);

        w.Dispose ();
        Assert.True (w.WasDisposed);
        exception = Record.Exception (() => Application.Run (w)); // Invalid - w has been disposed. Run it in debug mode will throw, otherwise the user may want to run it again
        Assert.NotNull (exception);

        exception = Record.Exception (() => Assert.Equal (string.Empty, w.Title)); // Invalid - w has been disposed and cannot be accessed
        Assert.NotNull (exception);
        exception = Record.Exception (() => w.Title = "NewTitle"); // Invalid - w has been disposed and cannot be accessed
        Assert.NotNull (exception);
#endif
        Application.Shutdown ();
        Assert.NotNull (w);
        Assert.Null (Application.Current);
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
        var top = Application.Run (null, driver);
#if DEBUG_IDISPOSABLE
        Assert.Equal(top, Application.Top);
        Assert.False (top.WasDisposed);
        var exception = Record.Exception (() => Application.Shutdown ());
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
        var exception = Record.Exception (() => Application.Shutdown ());
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
    public void Run_t_Creates_Top_Without_Init ()
    {
        var driver = new FakeDriver ();

        Assert.Null (Application.Top);

        Application.Iteration += (s, e) =>
                                 {
                                     Assert.NotNull (Application.Top);
                                     Application.RequestStop ();
                                 };
        Application.Run (new (), null, driver);
#if DEBUG_IDISPOSABLE
        Assert.False (Application.Top.WasDisposed);
        var exception = Record.Exception (() => Application.Shutdown ());
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
    public async void Shutdown_Allows_Async ()
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
