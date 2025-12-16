using Xunit.Abstractions;

namespace ApplicationTests.Init;

/// <summary>
///     Comprehensive tests for ApplicationImpl.Begin/End logic that manages Current and SessionStack.
///     These tests ensure the fragile state management logic is robust and catches regressions.
///     Tests work directly with ApplicationImpl instances to avoid global Application state issues.
/// </summary>
public class InitTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    
    [Fact]
    public void Init_Unbalanced_Throws ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Assert.Throws<InvalidOperationException> (() =>
                                                      app.Init (DriverRegistry.Names.ANSI)
                                                 );
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

        app.Init (DriverRegistry.Names.ANSI);

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

        app.Init (driverName: DriverRegistry.Names.ANSI);
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

        app.Init (DriverRegistry.Names.ANSI);

        Assert.Equal (Key.Q, app.Keyboard.QuitKey);

        app.Dispose ();
    }

    [Fact]
    public void Init_NoParam_ForceDriver_Works ()
    {
        using IApplication app = Application.Create ();

        app.ForceDriver = DriverRegistry.Names.ANSI;
        // Note: Init() without params picks up driver configuration
        app.Init ();

        Assert.Equal (DriverRegistry.Names.ANSI, app.Driver!.GetName ());
    }

    [Fact]
    public void Init_Dispose_Resets_Instance_Properties ()
    {
        IApplication app = Application.Create ();

        // Init the app
        app.Init (driverName: DriverRegistry.Names.ANSI);

        // Verify initialized
        Assert.True (app.Initialized);
        Assert.NotNull (app.Driver);

        // Dispose cleans up
        app.Dispose ();

        // Check reset state on the instance
        CheckReset (app);

        // Create a new instance and set values
        app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

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

}
