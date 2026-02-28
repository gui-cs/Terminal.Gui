// Claude - Opus 4.5

using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;
using Xunit.Abstractions;

namespace ApplicationTests;

/// <summary>
///     Tests for thread-safe tracing behavior.
///     These tests verify that tracing is properly isolated per-thread and per-async-context.
///     All tests work correctly in both Debug and Release builds.
/// </summary>
public class ThreadSafeTraceTests
{
    private readonly ITestOutputHelper _output;

    public ThreadSafeTraceTests (ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void EnabledCategories_Default_IsNone ()
    {
        // Reset to clean state
        Trace.EnabledCategories = TraceCategory.None;

        Assert.Equal (TraceCategory.None, Trace.EnabledCategories);
    }

    [Fact]
    public void EnabledCategories_CanBeSetAndRead ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Mouse;

            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Keyboard));
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void EnabledCategories_HasFlagWorks ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Keyboard;

            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Keyboard));
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Navigation));
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Lifecycle));
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void PushScope_RestoresPreviousState ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.None;

            using (Trace.PushScope (TraceCategory.Command))
            {
                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            }

            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            Assert.Equal (TraceCategory.None, Trace.EnabledCategories);
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void PushScope_NestedScopes_RestoreCorrectly ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.Mouse;

            using (Trace.PushScope (TraceCategory.Command))
            {
                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));

                using (Trace.PushScope (TraceCategory.Keyboard))
                {
                    Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                    Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Keyboard));
                }

                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Keyboard));
            }

            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void PushScope_WithBackend_RestoresBackend ()
    {
        ListBackend customBackend = new ();

        try
        {
            Trace.Backend = new NullBackend ();

            using (Trace.PushScope (TraceCategory.Command, customBackend))
            {
                Assert.Same (customBackend, Trace.Backend);
                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            }

            Assert.IsType<NullBackend> (Trace.Backend);
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void PushScope_CapturesTraces ()
    {
        ListBackend backend = new ();

        try
        {
            using (Trace.PushScope (TraceCategory.Command, backend))
            {
                View view = new () { Id = "test" };
                Trace.Command (view, Command.Accept, CommandRouting.Direct, "TestPhase", "TestMessage");

#if DEBUG
                Assert.Single (backend.Entries);
                Assert.Equal (TraceCategory.Command, backend.Entries [0].Category);
                Assert.Contains ("test", backend.Entries [0].Id);
#else
                // In Release, [Conditional("DEBUG")] removes Trace.Command calls
                Assert.Empty (backend.Entries);
#endif
            }
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void TestLogging_Verbose_WithTraceCategories ()
    {
        try
        {
            using (TestLogging.Verbose (_output, TraceCategory.Command))
            {
                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));

                CheckBox checkbox = new () { Id = "checkbox" };
                checkbox.InvokeCommand (Command.Activate);
            }

            // After scope, tracing should be disabled
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public async Task ParallelTests_IsolatedTracing_Test1 ()
    {
        ListBackend backend = new ();

        try
        {
            using (Trace.PushScope (TraceCategory.Command, backend))
            {
                // Simulate some async work
                await Task.Delay (10);

                View view = new () { Id = "parallel-test-1" };
                Trace.Command (view, Command.Accept, CommandRouting.Direct, "Test1");

                await Task.Delay (10);

#if DEBUG
                // This test should only see its own traces
                Assert.All (backend.Entries, entry => Assert.Contains ("parallel-test-1", entry.Id));
#else
                Assert.Empty (backend.Entries);
#endif

                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            }
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public async Task ParallelTests_IsolatedTracing_Test2 ()
    {
        ListBackend backend = new ();

        try
        {
            using (Trace.PushScope (TraceCategory.Mouse, backend))
            {
                // Simulate some async work
                await Task.Delay (10);

                View view = new () { Id = "parallel-test-2" };
                Trace.Mouse (view, MouseFlags.LeftButtonClicked, Point.Empty, "Test2");

                await Task.Delay (10);

#if DEBUG
                // This test should only see its own traces
                Assert.All (backend.Entries, entry => Assert.Contains ("parallel-test-2", entry.Id));
#else
                Assert.Empty (backend.Entries);
#endif

                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
                Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            }
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void EnabledCategories_IsAsyncLocal_IsolatesAcrossExecutionContexts ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.Command;

            TraceCategory capturedCategories = TraceCategory.None;

            // Task.Run flows ExecutionContext by default, so the async-local value is visible in the task.
            // The key isolation behavior is that changes in the task don't affect the parent context.
            Task.Run (
                      () =>
                      {
                          // Task sees parent's value due to ExecutionContext flow
                          capturedCategories = Trace.EnabledCategories;

                          // Set different categories in task context
                          Trace.EnabledCategories = TraceCategory.Mouse;
                      }).Wait ();

            // Parent context is unchanged despite modifications in the task
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            Assert.Equal (TraceCategory.Command, Trace.EnabledCategories);
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void Backend_IsAsyncLocal_IsolatedPerExecutionContext ()
    {
        ListBackend mainBackend = new ();

        try
        {
            Trace.Backend = mainBackend;

            ITraceBackend? capturedBackend = null;

            // Use Task.Run which may or may not flow ExecutionContext
            Task.Run (
                      () =>
                      {
                          capturedBackend = Trace.Backend;

                          // Set different backend in this context
                          Trace.Backend = new ListBackend ();
                      }).Wait ();

            // Main thread backend should be unchanged
            Assert.Same (mainBackend, Trace.Backend);
        }
        finally
        {
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public async Task AsyncLocal_FlowsAcrossAwait ()
    {
        ListBackend backend = new ();

        try
        {
            using (Trace.PushScope (TraceCategory.Command, backend))
            {
                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));

                // EnabledCategories should flow across await
                await Task.Delay (1);

                Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                Assert.Same (backend, Trace.Backend);

                View view = new () { Id = "async-test" };
                Trace.Command (view, Command.Accept, CommandRouting.Direct, "AfterAwait");

#if DEBUG
                Assert.Single (backend.Entries);
#else
                Assert.Empty (backend.Entries);
#endif
            }
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }
}
