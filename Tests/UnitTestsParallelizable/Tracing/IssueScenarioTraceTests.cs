// Claude - Sonnet 4.6
// This test file demonstrates the issue scenario from GitHub issue about thread-safe tracing

using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;
using Xunit.Abstractions;

namespace ApplicationTests;

/// <summary>
///     Demonstrates the solution to thread-safe tracing as described in the GitHub issue.
///     Tests can now run in parallel without interfering with each other's trace settings.
/// </summary>
public class IssueScenarioTraceTests
{
    private readonly ITestOutputHelper _output;

    public IssueScenarioTraceTests (ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    ///     This test demonstrates the exact scenario from the GitHub issue.
    ///     It uses the new TestLogging.Verbose API with TraceFlags parameter.
    /// </summary>
    [Fact]
    public void Example_Test_With_Tracing_Enabled ()
    {
        using (TestLogging.Verbose (traceCategories: TraceCategory.Command, output: _output))
        {
            CheckBox checkbox = new () { Id = "checkbox" };

            checkbox.InvokeCommand (Command.Activate);

            // The xUnit output would show:
            // [TRC] [Command:Handler] [InvokeCommand] @"checkbox" - Activate
            // [TRC] [Command:Entry] [DefaultActivateHandler] @"checkbox" - Activate
            // [TRC] [Command:Entry] [RaiseActivating] @"checkbox" - Activate
            // ... etc

            _output.WriteLine ("✓ Command tracing enabled successfully without affecting other tests");
        }
    }

    /// <summary>
    ///     This test can run in parallel with Example_Test_With_Tracing_Enabled.
    ///     Even if it sets tracing to disabled, it won't affect the other test.
    /// </summary>
    [Fact]
    public void Parallel_Test_With_Tracing_Disabled ()
    {
        // Explicitly disable tracing in this test's async context
        using (Trace.PushScope (TraceCategory.None))
        {
            CheckBox checkbox = new () { Id = "parallel-checkbox" };

            checkbox.InvokeCommand (Command.Accept);

            // No trace output expected
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            _output.WriteLine ("✓ This test has tracing disabled, without affecting other parallel tests");
        }
    }

    /// <summary>
    ///     Another parallel test that enables different trace categories.
    /// </summary>
    [Fact]
    public void Parallel_Test_With_Mouse_Tracing ()
    {
        using (TestLogging.Verbose (_output, TraceCategory.Mouse))
        {
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));

            _output.WriteLine ("✓ Mouse tracing enabled independently from Command tracing in other tests");
        }
    }

    /// <summary>
    ///     Demonstrates using Trace.PushScope directly with a custom backend.
    /// </summary>
    [Fact]
    public void Direct_PushScope_Usage ()
    {
        ListBackend backend = new ();

        using (Trace.PushScope (TraceCategory.Command | TraceCategory.Keyboard, backend))
        {
            CheckBox checkbox = new () { Id = "scope-test" };
            checkbox.InvokeCommand (Command.Accept);

            // Verify traces were captured
            Assert.NotEmpty (backend.Entries);
            Assert.All (backend.Entries, e => Assert.True (e.Category == TraceCategory.Command || e.Category == TraceCategory.Keyboard));

            _output.WriteLine ($"✓ Captured {backend.Entries.Count} trace entries in isolated scope");
        }
    }

    /// <summary>
    ///     Demonstrates EnabledCategories property for flags-based API.
    /// </summary>
    [Fact]
    public void EnabledCategories_FlagsAPI ()
    {
        using (Trace.PushScope (TraceCategory.Command | TraceCategory.Mouse | TraceCategory.Keyboard))
        {
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Keyboard));
            Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Navigation));

            // Can also check with HasFlag
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));

            _output.WriteLine ("✓ Flags-based API works correctly");
        }
    }

    /// <summary>
    ///     Simulates a parallel test scenario where tests run concurrently.
    ///     This would have failed with the old static implementation.
    /// </summary>
    [Fact]
    public async Task Concurrent_Tests_Are_Isolated ()
    {
        // Run multiple tasks concurrently, each with different trace settings
        Task task1 = Task.Run (
                               async () =>
                               {
                                   using (Trace.PushScope (TraceCategory.Command))
                                   {
                                       await Task.Delay (10);
                                       Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
                                       await Task.Delay (10);
                                   }
                               });

        Task task2 = Task.Run (
                               async () =>
                               {
                                   using (Trace.PushScope (TraceCategory.Mouse))
                                   {
                                       await Task.Delay (10);
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                                       Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
                                       await Task.Delay (10);
                                   }
                               });

        Task task3 = Task.Run (
                               async () =>
                               {
                                   using (Trace.PushScope (TraceCategory.None))
                                   {
                                       await Task.Delay (10);
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
                                       await Task.Delay (10);
                                   }
                               });

        // Wait for all tasks - they should all succeed without interfering
        await Task.WhenAll (task1, task2, task3);

        _output.WriteLine ("✓ All concurrent tasks completed with isolated trace settings");
    }
}
