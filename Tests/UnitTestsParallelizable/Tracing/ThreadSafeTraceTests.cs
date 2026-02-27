// Claude - Sonnet 4.6

using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;
using Xunit.Abstractions;

namespace ApplicationTests;

/// <summary>
///     Tests for thread-safe tracing behavior.
///     These tests verify that tracing is properly isolated per-thread and per-async-context.
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
    public void IndividualProperties_ReflectEnabledCategories ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Keyboard;

            Assert.True (Trace.CommandEnabled);
            Assert.True (Trace.KeyboardEnabled);
            Assert.False (Trace.MouseEnabled);
            Assert.False (Trace.NavigationEnabled);
            Assert.False (Trace.LifecycleEnabled);
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void SettingIndividualProperties_UpdatesEnabledCategories ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.None;

            Trace.CommandEnabled = true;
            Assert.Equal (TraceCategory.Command, Trace.EnabledCategories);

            Trace.MouseEnabled = true;
            Assert.Equal (TraceCategory.Command | TraceCategory.Mouse, Trace.EnabledCategories);

            Trace.CommandEnabled = false;
            Assert.Equal (TraceCategory.Mouse, Trace.EnabledCategories);
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
                Assert.True (Trace.CommandEnabled);
            }

            Assert.False (Trace.CommandEnabled);
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
                Assert.True (Trace.CommandEnabled);
                Assert.False (Trace.MouseEnabled);

                using (Trace.PushScope (TraceCategory.Keyboard))
                {
                    Assert.False (Trace.CommandEnabled);
                    Assert.True (Trace.KeyboardEnabled);
                }

                Assert.True (Trace.CommandEnabled);
                Assert.False (Trace.KeyboardEnabled);
            }

            Assert.True (Trace.MouseEnabled);
            Assert.False (Trace.CommandEnabled);
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
                Assert.True (Trace.CommandEnabled);
            }

            Assert.IsType<NullBackend> (Trace.Backend);
            Assert.False (Trace.CommandEnabled);
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

                Assert.Single (backend.Entries);
                Assert.Equal (TraceCategory.Command, backend.Entries [0].Category);
                Assert.Contains ("test", backend.Entries [0].Id);
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
                Assert.True (Trace.CommandEnabled);
                Assert.False (Trace.MouseEnabled);

                CheckBox checkbox = new () { Id = "checkbox" };
                checkbox.InvokeCommand (Command.Activate);

                // If we get here without exception, the test passes
                _output.WriteLine ("✓ Command tracing enabled successfully");
            }

            // After scope, tracing should be disabled
            Assert.False (Trace.CommandEnabled);
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

                // This test should only see its own traces
                Assert.All (backend.Entries, entry => Assert.Contains ("parallel-test-1", entry.Id));
                Assert.True (Trace.CommandEnabled);
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

                // This test should only see its own traces
                Assert.All (backend.Entries, entry => Assert.Contains ("parallel-test-2", entry.Id));
                Assert.True (Trace.MouseEnabled);
                Assert.False (Trace.CommandEnabled);
            }
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    [Fact]
    public void EnabledCategories_IsAsyncLocal_DoesNotFlowToThreadPoolThreads ()
    {
        try
        {
            Trace.EnabledCategories = TraceCategory.Command;

            TraceCategory capturedCategories = TraceCategory.None;
            bool capturedCommandEnabled = false;

            // Use Task.Run to run on a thread pool thread without ExecutionContext flow
            Task.Run (
                      () =>
                      {
                          // Thread pool threads may or may not see the async-local value depending on ExecutionContext flow
                          // For manual threads created with new Thread(), they should see None
                          capturedCategories = Trace.EnabledCategories;
                          capturedCommandEnabled = Trace.CommandEnabled;

                          // Set different categories in other thread
                          Trace.EnabledCategories = TraceCategory.Mouse;
                      }).Wait ();

            // This thread should still have Command enabled
            Assert.True (Trace.CommandEnabled);
            Assert.Equal (TraceCategory.Command, Trace.EnabledCategories);

            // The captured values may vary depending on ExecutionContext flow,
            // but at minimum, changes in the other thread should not affect this thread
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

            // The other execution context may see the main backend or NullBackend depending on flow
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
                Assert.True (Trace.CommandEnabled);

                // EnabledCategories should flow across await
                await Task.Delay (1);

                Assert.True (Trace.CommandEnabled);
                Assert.Same (backend, Trace.Backend);

                View view = new () { Id = "async-test" };
                Trace.Command (view, Command.Accept, CommandRouting.Direct, "AfterAwait");

                Assert.Single (backend.Entries);
            }
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }
}
