namespace UnitTests.ApplicationTests;

/// <summary>
///     Tests to ensure that mixing legacy static Application and modern instance-based models
///     throws appropriate exceptions.
/// </summary>
[Collection ("Global Test Setup")]
public class ApplicationModelFencingTests
{
    public ApplicationModelFencingTests ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();
    }

    [Fact]
    public void Create_ThenInstanceAccess_ThrowsInvalidOperationException ()
    {
        // Create a modern instance-based application
        IApplication app = Application.Create ();
        app.Init ("fake");

        // Attempting to initialize using the legacy static model should throw
        InvalidOperationException ex = Assert.Throws<InvalidOperationException> (() =>
        {
            ApplicationImpl.Instance.Init ("fake");
        });

        Assert.Contains ("Cannot use legacy static Application model", ex.Message);
        Assert.Contains ("after using modern instance-based model", ex.Message);

        // Clean up
        app.Shutdown ();
    }

    [Fact]
    public void InstanceAccess_ThenCreate_ThrowsInvalidOperationException ()
    {
        // Initialize using the legacy static model
        IApplication staticInstance = ApplicationImpl.Instance;
        staticInstance.Init ("fake");

        // Attempting to create and initialize with modern instance-based model should throw
        InvalidOperationException ex = Assert.Throws<InvalidOperationException> (() =>
        {
            IApplication app = Application.Create ();
            app.Init ("fake");
        });

        Assert.Contains ("Cannot use modern instance-based model", ex.Message);
        Assert.Contains ("after using legacy static Application model", ex.Message);

        // Clean up
        staticInstance.Shutdown ();
    }

    [Fact]
    public void Init_ThenCreate_ThrowsInvalidOperationException ()
    {
        // Initialize using legacy static API
        IApplication staticInstance = ApplicationImpl.Instance;
        staticInstance.Init ("fake");

        // Attempting to create a modern instance-based application should throw
        InvalidOperationException ex = Assert.Throws<InvalidOperationException> (() =>
        {
            IApplication _ = Application.Create ();
        });

        Assert.Contains ("Cannot use modern instance-based model", ex.Message);
        Assert.Contains ("after using legacy static Application model", ex.Message);

        // Clean up
        staticInstance.Shutdown ();
    }

    [Fact]
    public void Create_ThenInit_ThrowsInvalidOperationException ()
    {
        // Create a modern instance-based application
        IApplication app = Application.Create ();
        app.Init ("fake");

        // Attempting to initialize using the legacy static model should throw
        InvalidOperationException ex = Assert.Throws<InvalidOperationException> (() =>
        {
            ApplicationImpl.Instance.Init ("fake");
        });

        Assert.Contains ("Cannot use legacy static Application model", ex.Message);
        Assert.Contains ("after using modern instance-based model", ex.Message);

        // Clean up
        app.Shutdown ();
    }

    [Fact]
    public void MultipleCreate_Calls_DoNotThrow ()
    {
        // Multiple calls to Create should not throw
        IApplication app1 = Application.Create ();
        IApplication app2 = Application.Create ();
        IApplication app3 = Application.Create ();

        Assert.NotNull (app1);
        Assert.NotNull (app2);
        Assert.NotNull (app3);

        // Clean up
        app1.Shutdown ();
        app2.Shutdown ();
        app3.Shutdown ();
    }

    [Fact]
    public void MultipleInstanceAccess_DoesNotThrow ()
    {
        // Multiple accesses to Instance should not throw (it's a singleton)
        IApplication instance1 = ApplicationImpl.Instance;
        IApplication instance2 = ApplicationImpl.Instance;
        IApplication instance3 = ApplicationImpl.Instance;

        Assert.NotNull (instance1);
        Assert.Same (instance1, instance2);
        Assert.Same (instance2, instance3);

        // Clean up
        instance1.Shutdown ();
    }

    [Fact]
    public void ResetModelUsageTracking_AllowsSwitchingModels ()
    {
        // Use modern model
        IApplication app1 = Application.Create ();
        app1.Shutdown ();

        // Reset the tracking
        ApplicationImpl.ResetModelUsageTracking ();

        // Should now be able to use legacy model
        IApplication staticInstance = ApplicationImpl.Instance;
        Assert.NotNull (staticInstance);
        staticInstance.Shutdown ();

        // Reset again
        ApplicationImpl.ResetModelUsageTracking ();

        // Should be able to use modern model again
        IApplication app2 = Application.Create ();
        Assert.NotNull (app2);
        app2.Shutdown ();
    }
}
