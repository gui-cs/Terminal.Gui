#nullable enable
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.ApplicationTests;

/// <summary>
///     Integration tests for IApplication's IRunnable support.
///     Tests the full lifecycle of IRunnable instances through Application methods.
/// </summary>
public class ApplicationRunnableIntegrationTests (ITestOutputHelper output) : IDisposable
{
    private readonly ITestOutputHelper _output = output;
    private IApplication? _app;

    private IApplication GetApp ()
    {
        if (_app is null)
        {
            _app = Application.Create ();
            _app.Init ("fake");
        }

        return _app;
    }

    public void Dispose ()
    {
        _app?.Shutdown ();
        _app = null;
    }

    [Fact]
    public void Begin_AddsRunnableToStack ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        int stackCountBefore = app.RunnableSessionStack?.Count ?? 0;

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert
        Assert.NotNull (token);
        Assert.NotNull (token.Runnable);
        Assert.Same (runnable, token.Runnable);
        Assert.Equal (stackCountBefore + 1, app.RunnableSessionStack?.Count ?? 0);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void Begin_ThrowsOnNullRunnable ()
    {
        // Arrange
        IApplication app = GetApp ();

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => app.Begin ((IRunnable)null!));
    }

    [Fact]
    public void Begin_RaisesIsRunningChangingEvent ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        var isRunningChangingRaised = false;
        bool? oldValue = null;
        bool? newValue = null;

        runnable.IsRunningChanging += (s, e) =>
                                      {
                                          isRunningChangingRaised = true;
                                          oldValue = e.CurrentValue;
                                          newValue = e.NewValue;
                                      };

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert
        Assert.True (isRunningChangingRaised);
        Assert.False (oldValue);
        Assert.True (newValue);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void Begin_RaisesIsRunningChangedEvent ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        var isRunningChangedRaised = false;
        bool? receivedValue = null;

        runnable.IsRunningChanged += (s, e) =>
                                     {
                                         isRunningChangedRaised = true;
                                         receivedValue = e.Value;
                                     };

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert
        Assert.True (isRunningChangedRaised);
        Assert.True (receivedValue);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void Begin_RaisesIsModalChangingEvent ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        var isModalChangingRaised = false;
        bool? oldValue = null;
        bool? newValue = null;

        runnable.IsModalChanging += (s, e) =>
                                    {
                                        isModalChangingRaised = true;
                                        oldValue = e.CurrentValue;
                                        newValue = e.NewValue;
                                    };

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert
        Assert.True (isModalChangingRaised);
        Assert.False (oldValue);
        Assert.True (newValue);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void Begin_RaisesIsModalChangedEvent ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        var isModalChangedRaised = false;
        bool? receivedValue = null;

        runnable.IsModalChanged += (s, e) =>
                                   {
                                       isModalChangedRaised = true;
                                       receivedValue = e.Value;
                                   };

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert
        Assert.True (isModalChangedRaised);
        Assert.True (receivedValue);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void Begin_SetsIsRunningToTrue ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert
        Assert.True (runnable.IsRunning);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void Begin_SetsIsModalToTrue ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert
        Assert.True (runnable.IsModal);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void End_RemovesRunnableFromStack ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);
        int stackCountBefore = app.RunnableSessionStack?.Count ?? 0;

        // Act
        app.End (token);

        // Assert
        Assert.Equal (stackCountBefore - 1, app.RunnableSessionStack?.Count ?? 0);
    }

    [Fact]
    public void End_ThrowsOnNullToken ()
    {
        // Arrange
        IApplication app = GetApp ();

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => app.End ((RunnableSessionToken)null!));
    }

    [Fact]
    public void End_RaisesIsRunningChangingEvent ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);
        var isRunningChangingRaised = false;
        bool? oldValue = null;
        bool? newValue = null;

        runnable.IsRunningChanging += (s, e) =>
                                      {
                                          isRunningChangingRaised = true;
                                          oldValue = e.CurrentValue;
                                          newValue = e.NewValue;
                                      };

        // Act
        app.End (token);

        // Assert
        Assert.True (isRunningChangingRaised);
        Assert.True (oldValue);
        Assert.False (newValue);
    }

    [Fact]
    public void End_RaisesIsRunningChangedEvent ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);
        var isRunningChangedRaised = false;
        bool? receivedValue = null;

        runnable.IsRunningChanged += (s, e) =>
                                     {
                                         isRunningChangedRaised = true;
                                         receivedValue = e.Value;
                                     };

        // Act
        app.End (token);

        // Assert
        Assert.True (isRunningChangedRaised);
        Assert.False (receivedValue);
    }

    [Fact]
    public void End_SetsIsRunningToFalse ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);

        // Act
        app.End (token);

        // Assert
        Assert.False (runnable.IsRunning);
    }

    [Fact]
    public void End_SetsIsModalToFalse ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);

        // Act
        app.End (token);

        // Assert
        Assert.False (runnable.IsModal);
    }

    [Fact]
    public void End_ClearsTokenRunnable ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);

        // Act
        app.End (token);

        // Assert
        Assert.Null (token.Runnable);
    }

    [Fact]
    public void NestedBegin_MaintainsStackOrder ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable1 = new () { Id = "1" };
        Runnable<int> runnable2 = new () { Id = "2" };

        // Act
        RunnableSessionToken token1 = app.Begin (runnable1);
        RunnableSessionToken token2 = app.Begin (runnable2);

        // Assert - runnable2 should be on top
        Assert.True (runnable2.IsModal);
        Assert.False (runnable1.IsModal);
        Assert.True (runnable1.IsRunning); // Still running, just not modal
        Assert.True (runnable2.IsRunning);

        // Cleanup
        app.End (token2);
        app.End (token1);
    }

    [Fact]
    public void NestedEnd_RestoresPreviousModal ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable1 = new () { Id = "1" };
        Runnable<int> runnable2 = new () { Id = "2" };
        RunnableSessionToken token1 = app.Begin (runnable1);
        RunnableSessionToken token2 = app.Begin (runnable2);

        // Act - End the top runnable
        app.End (token2);

        // Assert - runnable1 should become modal again
        Assert.True (runnable1.IsModal);
        Assert.False (runnable2.IsModal);
        Assert.True (runnable1.IsRunning);
        Assert.False (runnable2.IsRunning);

        // Cleanup
        app.End (token1);
    }

    [Fact]
    public void RequestStop_WithIRunnable_WorksCorrectly ()
    {
        // Arrange
        IApplication app = GetApp ();
        StoppableRunnable runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);

        // Act
        app.RequestStop (runnable);

        // Assert - RequestStop should trigger End eventually
        // For now, just verify it doesn't throw
        Assert.NotNull (runnable);

        // Cleanup
        app.End (token);
    }

    [Fact]
    public void RequestStop_WithNull_UsesTopRunnable ()
    {
        // Arrange
        IApplication app = GetApp ();
        StoppableRunnable runnable = new ();
        RunnableSessionToken token = app.Begin (runnable);

        // Act
        app.RequestStop ((IRunnable?)null);

        // Assert - Should not throw
        Assert.NotNull (runnable);

        // Cleanup
        app.End (token);
    }

    [Fact (Skip = "Run methods with main loop are not suitable for parallel tests - use non-parallel UnitTests instead")]
    public void RunGeneric_CreatesAndReturnsRunnable ()
    {
        // Arrange
        IApplication app = GetApp ();
        app.StopAfterFirstIteration = true;

        // Act - With fluent API, Run<T>() returns IApplication for chaining
        IApplication result = app.Run<TestRunnable> ();

        // Assert
        Assert.NotNull (result);
        Assert.Same (app, result); // Fluent API returns this

        // Note: Run blocks until stopped, but StopAfterFirstIteration makes it return immediately
        // The runnable is automatically disposed by Shutdown()
    }

    [Fact (Skip = "Run methods with main loop are not suitable for parallel tests - use non-parallel UnitTests instead")]
    public void RunGeneric_ThrowsIfNotInitialized ()
    {
        // Arrange
        IApplication app = Application.Create ();

        // Don't call Init

        // Act & Assert
        Assert.Throws<NotInitializedException> (() => app.Run<TestRunnable> ());

        // Cleanup
        app.Shutdown ();
    }

    [Fact]
    public void Begin_CanBeCanceled_ByIsRunningChanging ()
    {
        // Arrange
        IApplication app = GetApp ();
        CancelableRunnable runnable = new () { CancelStart = true };

        // Act
        RunnableSessionToken token = app.Begin (runnable);

        // Assert - Should not be added to stack if canceled
        Assert.False (runnable.IsRunning);

        // Token is still created but runnable not added to stack
        Assert.NotNull (token);
    }

    [Fact]
    public void End_CanBeCanceled_ByIsRunningChanging ()
    {
        // Arrange
        IApplication app = GetApp ();
        CancelableRunnable runnable = new () { CancelStop = true };
        RunnableSessionToken token = app.Begin (runnable);
        runnable.CancelStop = true; // Enable cancellation

        // Act
        app.End (token);

        // Assert - Should still be running if canceled
        Assert.True (runnable.IsRunning);

        // Force end by disabling cancellation
        runnable.CancelStop = false;
        app.End (token);
    }

    [Fact]
    public void MultipleRunnables_IndependentResults ()
    {
        // Arrange
        IApplication app = GetApp ();
        Runnable<int> runnable1 = new ();
        Runnable<string> runnable2 = new ();

        // Act
        runnable1.Result = 42;
        runnable2.Result = "test";

        // Assert
        Assert.Equal (42, runnable1.Result);
        Assert.Equal ("test", runnable2.Result);
    }

    /// <summary>
    ///     Test runnable that can be stopped.
    /// </summary>
    private class StoppableRunnable : Runnable<int>
    {
        public bool WasStopRequested { get; private set; }

        public override void RequestStop ()
        {
            WasStopRequested = true;
            base.RequestStop ();
        }
    }

    /// <summary>
    ///     Test runnable for generic Run tests.
    /// </summary>
    private class TestRunnable : Runnable<int>
    {
        public TestRunnable () { Id = "TestRunnable"; }
    }

    /// <summary>
    ///     Test runnable that can cancel lifecycle changes.
    /// </summary>
    private class CancelableRunnable : Runnable<int>
    {
        public bool CancelStart { get; set; }
        public bool CancelStop { get; set; }

        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            if (newIsRunning && CancelStart)
            {
                return true; // Cancel starting
            }

            if (!newIsRunning && CancelStop)
            {
                return true; // Cancel stopping
            }

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }
    }
}
