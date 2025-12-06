#nullable enable
using Xunit.Abstractions;

namespace ApplicationTests;

/// <summary>
///     Parallelizable tests for IApplication that don't require the main event loop.
///     Tests using the modern non-static IApplication API.
/// </summary>
public class ApplicationTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [Fact]
    public void Begin_Null_Runnable_Throws ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        // Test null Runnable
        Assert.Throws<ArgumentNullException> (() => app.Begin (null!));

        app.Dispose ();
    }

    [Fact]
    public void Begin_Sets_Application_Top_To_Console_Size ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        Assert.Null (app.TopRunnableView);
        app.Driver!.SetScreenSize (80, 25);
        Runnable top = new ();
        SessionToken? token = app.Begin (top);
        Assert.Equal (new (0, 0, 80, 25), app.TopRunnableView!.Frame);
        app.Driver!.SetScreenSize (5, 5);
        app.LayoutAndDraw ();
        Assert.Equal (new (0, 0, 5, 5), app.TopRunnableView!.Frame);
        
        if (token is { })
        {
            app.End (token);
        }
        top.Dispose ();

        app.Dispose ();
    }

    [Fact]
    public void Init_Null_Driver_Should_Pick_A_Driver ()
    {
        IApplication app = Application.Create ();
        app.Init ();

        Assert.NotNull (app.Driver);

        app.Dispose ();
    }

    [Fact]
    public void Init_Dispose_Cleans_Up ()
    {
        IApplication app = Application.Create ();

        app.Init ("fake");

        app.Dispose ();

#if DEBUG_IDISPOSABLE
        // Validate there are no outstanding Responder-based instances 
        // after cleanup
        // Note: We can't check View.Instances in parallel tests as it's a static field
        // that would be shared across parallel test runs
#endif
    }

    [Fact]
    public void Init_Dispose_Fire_InitializedChanged ()
    {
        var initialized = false;
        var Dispose = false;

        IApplication app = Application.Create ();

        app.InitializedChanged += OnApplicationOnInitializedChanged;

        app.Init (driverName: "fake");
        Assert.True (initialized);
        Assert.False (Dispose);

        app.Dispose ();
        Assert.True (initialized);
        Assert.True (Dispose);

        app.InitializedChanged -= OnApplicationOnInitializedChanged;

        return;

        void OnApplicationOnInitializedChanged (object? s, EventArgs<bool> a)
        {
            if (a.Value)
            {
                initialized = true;
            }
            else
            {
                Dispose = true;
            }
        }
    }

    [Fact]
    public void Init_KeyBindings_Are_Not_Reset ()
    {
        IApplication app = Application.Create ();

        // Set via Keyboard property (modern API)
        app.Keyboard.QuitKey = Key.Q;
        Assert.Equal (Key.Q, app.Keyboard.QuitKey);

        app.Init ("fake");

        Assert.Equal (Key.Q, app.Keyboard.QuitKey);

        app.Dispose ();
    }

    [Fact]
    public void Init_NoParam_ForceDriver_Works ()
    {
        using IApplication app = Application.Create ();

        app.ForceDriver = "fake";
        // Note: Init() without params picks up driver configuration
        app.Init ();

        Assert.Equal ("fake", app.Driver!.GetName ());
    }

    [Fact]
    public void Init_Dispose_Resets_Instance_Properties ()
    {
        IApplication app = Application.Create ();

        // Init the app
        app.Init (driverName: "fake");

        // Verify initialized
        Assert.True (app.Initialized);
        Assert.NotNull (app.Driver);

        // Dispose cleans up
        app.Dispose ();

        // Check reset state on the instance
        CheckReset (app);

        // Create a new instance and set values
        app = Application.Create ();
        app.Init ("fake");

        app.StopAfterFirstIteration = true;
        app.Keyboard.PrevTabGroupKey = Key.A;
        app.Keyboard.NextTabGroupKey = Key.B;
        app.Keyboard.QuitKey = Key.C;
        app.Keyboard.KeyBindings.Add (Key.D, Command.Cancel);

        app.Mouse.CachedViewsUnderMouse.Clear ();
        app.Mouse.LastMousePosition = new Point (1, 1);

        // Dispose and check reset
        app.Dispose ();
        CheckReset (app);

        return;

        void CheckReset (IApplication application)
        {
            // Check that all fields and properties are reset on the instance

            // Public Properties
            Assert.Null (application.TopRunnableView);
            Assert.Null (application.Mouse.MouseGrabView);
            Assert.Null (application.Driver);
            Assert.False (application.StopAfterFirstIteration);

            // Internal properties
            Assert.False (application.Initialized);
            Assert.Null (application.MainThreadId);
            Assert.Empty (application.Mouse.CachedViewsUnderMouse);
        }
    }

    [Fact]
    public void Internal_Properties_Correct ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        Assert.True (app.Initialized);
        Assert.Null (app.TopRunnableView);
        SessionToken? rs = app.Begin (new Runnable<bool> ());
        Assert.Equal (app.TopRunnable, rs!.Runnable);
        Assert.Null (app.Mouse.MouseGrabView); // public

        app.Dispose ();
    }

    [Fact]
    public void Invoke_Adds_Idle ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        Runnable top = new ();
        SessionToken? rs = app.Begin (top);

        var actionCalled = 0;
        app.Invoke ((_) => { actionCalled++; });
        app.TimedEvents!.RunTimers ();
        Assert.Equal (1, actionCalled);
        top.Dispose ();

        app.Dispose ();
    }

    [Fact]
    public void Run_Iteration_Fires ()
    {
        var iteration = 0;

        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Iteration += Application_Iteration;
        app.Run<Runnable> ();
        app.Iteration -= Application_Iteration;

        Assert.Equal (1, iteration);
        app.Dispose ();

        return;

        void Application_Iteration (object? sender, EventArgs<IApplication?> e)
        {

            iteration++;
            app.RequestStop ();
        }
    }

    [Fact]
    public void Screen_Size_Changes ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        IDriver? driver = app.Driver;

        app.Driver!.SetScreenSize (80, 25);

        Assert.Equal (new (0, 0, 80, 25), driver!.Screen);
        Assert.Equal (new (0, 0, 80, 25), app.Screen);

        // TODO: Should not be possible to manually change these at whim!
        driver.Cols = 100;
        driver.Rows = 30;

        app.Driver!.SetScreenSize (100, 30);

        Assert.Equal (new (0, 0, 100, 30), driver.Screen);

        app.Screen = new (0, 0, driver.Cols, driver.Rows);
        Assert.Equal (new (0, 0, 100, 30), driver.Screen);

        app.Dispose ();
    }

    [Fact]
    public void Dispose_Alone_Does_Nothing ()
    {
        IApplication app = Application.Create ();
        app.Dispose ();
    }

    #region RunTests

    [Fact]
    public void Run_T_After_InitWithDriver_with_Runnable_and_Driver_Does_Not_Throw ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        // Run<Runnable<bool>> when already initialized or not with a Driver will not throw (because Window is derived from Runnable)
        // Using another type not derived from Runnable will throws at compile time
        app.Run<Window> (null, "fake");

        // Run<Runnable<bool>> when already initialized or not with a Driver will not throw (because Dialog is derived from Runnable)
        app.Run<Dialog> (null, "fake");

        app.Dispose ();
    }

    [Fact]
    public void Run_T_After_Init_Does_Not_Disposes_Application_Top ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        // Init doesn't create a Runnable and assigned it to app.TopRunnable
        // but Begin does
        var initTop = new Runnable ();

        app.Iteration += OnApplicationOnIteration;

        app.Run<Runnable> ();
        app.Iteration -= OnApplicationOnIteration;

#if DEBUG_IDISPOSABLE
        Assert.False (initTop.WasDisposed);
        initTop.Dispose ();
        Assert.True (initTop.WasDisposed);
#endif
        initTop.Dispose ();

        app.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            Assert.NotEqual (initTop, app.TopRunnableView);
#if DEBUG_IDISPOSABLE
            Assert.False (initTop.WasDisposed);
#endif
            app.RequestStop ();
        }
    }

    [Fact]
    public void Run_T_After_InitWithDriver_with_TestRunnable_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.StopAfterFirstIteration = true;

        // Init has been called and we're passing no driver to Run<TestRunnable>. This is ok.
        app.Run<Window> ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_After_InitNullDriver_with_TestRunnable_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.StopAfterFirstIteration = true;

        // Init has been called, selecting FakeDriver; we're passing no driver to Run<TestRunnable>. Should be fine.
        app.Run<Window> ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_NoInit_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        app.Run<Window> ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_NoInit_WithDriver_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        // Init has NOT been called and we're passing a valid driver to Run<TestRunnable>. This is ok.
        app.Run<Runnable> (null, "fake");

        app.Dispose ();
    }

    [Fact]
    public void Run_Sets_Running_True ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        var top = new Runnable ();
        SessionToken? rs = app.Begin (top);
        Assert.NotNull (rs);

        app.Iteration += OnApplicationOnIteration;
        app.Run (top);
        app.Iteration -= OnApplicationOnIteration;

        top.Dispose ();

        app.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            Assert.True (top.IsRunning);
            top.RequestStop ();
        }
    }

    [Fact]
    public void Run_A_Modal_Runnable_Refresh_Background_On_Moving ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var w = new Window
        {
            Width = 5, Height = 5,
            Arrangement = ViewArrangement.Movable
        };
        app.Driver!.SetScreenSize (10, 10);
        SessionToken? rs = app.Begin (w);

        // Don't use visuals to test as style of border can change over time.
        Assert.Equal (new (0, 0), w.Frame.Location);

        app.Mouse.RaiseMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (w.Border, app.Mouse.MouseGrabView);
        Assert.Equal (new (0, 0), w.Frame.Location);

        // Move down and to the right.
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
        Assert.Equal (new (1, 1), w.Frame.Location);

        app.End (rs!);
        w.Dispose ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_Creates_Top_Without_Init ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        app.SessionEnded += OnApplicationOnSessionEnded;

        app.Run<Window> (null, "fake");

        Assert.Null (app.TopRunnableView);

        app.Dispose ();
        Assert.Null (app.TopRunnableView);

        return;

        void OnApplicationOnSessionEnded (object? sender, SessionTokenEventArgs e)
        {
            app.SessionEnded -= OnApplicationOnSessionEnded;
            e.State.Result = (e.State.Runnable as IRunnable<object?>)?.Result;
        }
    }

    #endregion

    #region DisposeTests

    [Fact]
    public async Task Dispose_Allows_Async ()
    {
        var isCompletedSuccessfully = false;

        async Task TaskWithAsyncContinuation ()
        {
            await Task.Yield ();
            await Task.Yield ();

            isCompletedSuccessfully = true;
        }

        IApplication app = Application.Create ();
        app.Dispose ();

        Assert.False (isCompletedSuccessfully);
        await TaskWithAsyncContinuation ();
        Thread.Sleep (100);
        Assert.True (isCompletedSuccessfully);
    }

    [Fact]
    public void Dispose_Resets_SyncContext ()
    {
        IApplication app = Application.Create ();
        app.Dispose ();
        Assert.Null (SynchronizationContext.Current);
    }

    #endregion

}
