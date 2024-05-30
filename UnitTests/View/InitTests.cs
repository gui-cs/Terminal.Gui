using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>Tests View BeginInit/EndInit/Initialized functionality.</summary>
public class InitTests
{
    private readonly ITestOutputHelper _output;
    public InitTests (ITestOutputHelper output) { _output = output; }

    // Test behavior of calling BeginInit multiple times
    [Fact]
    public void BeginInit_Called_Multiple_Times_Throws ()
    {
        var view = new View ();
        var superView = new View ();
        superView.Add (view);

        Assert.False (view.IsInitialized, "View should not be initialized");
        Assert.False (superView.IsInitialized, "SuperView should not be initialized");

        superView.BeginInit ();
        superView.EndInit ();
        Assert.True (view.IsInitialized, "View should be initialized");
        Assert.True (superView.IsInitialized, "SuperView should be initialized");

        Assert.Throws<InvalidOperationException> (() => superView.BeginInit ());
    }

    [Fact]
    public void BeginInit_EndInit_Initialized ()
    {
        var view = new View ();
        Assert.False (view.IsInitialized, "View should not be initialized");
        view.BeginInit ();
        Assert.False (view.IsInitialized, "View should not be initialized");
        view.EndInit ();
        Assert.True (view.IsInitialized, "View should be initialized");
    }

    [Fact]
    public void BeginInit_EndInit_Initialized_WithSuperView ()
    {
        var view = new View ();
        var superView = new View ();
        superView.Add (view);
        Assert.False (view.IsInitialized, "View should not be initialized");
        view.BeginInit ();
        Assert.False (view.IsInitialized, "View should not be initialized");
        view.EndInit ();
        Assert.True (view.IsInitialized, "View should be initialized");

        Assert.False (superView.IsInitialized, "SuperView should not be initialized");
    }

    [Fact]
    public void BeginInit_EndInit_SuperView_Initialized ()
    {
        var view = new View ();
        var superView = new View ();
        superView.Add (view);
        Assert.False (view.IsInitialized, "View should not be initialized");
        Assert.False (superView.IsInitialized, "SuperView should not be initialized");
        superView.BeginInit ();
        Assert.False (view.IsInitialized, "View should not be initialized");
        Assert.False (superView.IsInitialized, "SuperView should not be initialized");
        superView.EndInit ();
        Assert.True (view.IsInitialized, "View should be initialized");
        Assert.True (superView.IsInitialized, "SuperView should be initialized");
    }

    [Fact]
    public void BeginInit_EndInit_SuperView_Initialized_WithSuperSuperView ()
    {
        var view = new View ();
        var superView = new View ();
        var superSuperView = new View ();
        superSuperView.Add (superView);
        superView.Add (view);
        Assert.False (view.IsInitialized, "View should not be initialized");
        Assert.False (superView.IsInitialized, "SuperView should not be initialized");
        Assert.False (superSuperView.IsInitialized, "SuperSuperView should not be initialized");
        superSuperView.BeginInit ();
        Assert.False (view.IsInitialized, "View should not be initialized");
        Assert.False (superView.IsInitialized, "SuperView should not be initialized");
        Assert.False (superSuperView.IsInitialized, "SuperSuperView should not be initialized");
        superSuperView.EndInit ();
        Assert.True (view.IsInitialized, "View should be initialized");
        Assert.True (superView.IsInitialized, "SuperView should be initialized");
        Assert.True (superSuperView.IsInitialized, "SuperSuperView should be initialized");
    }

    // Test behavior of calling EndInit multiple times
    [Fact]
    public void EndInit_Called_Multiple_Times_Throws ()
    {
        var view = new View ();
        var superView = new View ();
        superView.Add (view);

        var initialized = false;
        view.Initialized += (s, e) => initialized = true;

        var superViewInitialized = false;
        superView.Initialized += (s, e) => superViewInitialized = true;

        Assert.False (view.IsInitialized, "View should not be initialized");
        Assert.False (superView.IsInitialized, "SuperView should not be initialized");

        superView.BeginInit ();
        superView.EndInit ();
        Assert.True (view.IsInitialized, "View should be initialized");
        Assert.True (superView.IsInitialized, "SuperView should be initialized");
        Assert.True (initialized, "View: Initialized event should have been raised");
        Assert.True (superViewInitialized, "SuperView: Initialized event should have been raised");

        Assert.Throws<InvalidOperationException> (() => superView.EndInit ());
    }

    // Test calling EndInit without first calling BeginInit
    [Fact]
    public void EndInit_Called_Without_BeginInit_Throws ()
    {
        var view = new View ();
        var superView = new View ();
        superView.Add (view);

        //var initialized = false;
        //view.Initialized += (s, e) => initialized = true;

        //var superViewInitialized = false;
        //superView.Initialized += (s, e) => superViewInitialized = true;

        Assert.False (view.IsInitialized, "View should not be initialized");
        Assert.False (superView.IsInitialized, "SuperView should not be initialized");

        // TODO: Implement logic that does this in Begin/EndInit
        //Assert.Throws<InvalidOperationException> (() => superView.EndInit ());
    }

    // Initialized event
    [Fact]
    public void InitializedEvent_Fires_On_EndInit ()
    {
        var view = new View ();
        var superView = new View ();
        superView.Add (view);
        var initialized = false;
        view.Initialized += (s, e) => initialized = true;

        var superViewInitialized = false;
        superView.Initialized += (s, e) => superViewInitialized = true;

        Assert.False (initialized, "View: Initialized event should not have been raised");
        Assert.False (superViewInitialized, "SuperView: Initialized event should not have been raised");
        superView.BeginInit ();
        Assert.False (initialized, "View: Initialized event should not have been raised");
        Assert.False (superViewInitialized, "SuperView: Initialized event should not have been raised");
        superView.EndInit ();
        Assert.True (initialized, "View: Initialized event should have been raised");
        Assert.True (superViewInitialized, "SuperView: Initialized event should have been raised");
    }

    // TODO: Create tests that prove ISupportInitialize and ISupportInitializeNotifications work properly
}
