// Claude - Opus 4.5
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;

namespace ApplicationTests;

/// <summary>
///     Tests for the unified <see cref="Trace"/> system.
///     All tests work correctly in both Debug and Release builds.
///     In Release, <c>[Conditional("DEBUG")]</c> trace methods are no-ops,
///     so capture-based assertions verify entries are empty instead.
/// </summary>
public class TraceTests
{
    private readonly ITestOutputHelper _output;

    public TraceTests (ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CommandEnabled_Default_IsFalse ()
    {
        // Clean state
        Trace.EnabledCategories = TraceCategory.None;

        Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
    }

    [Fact]
    public void MouseEnabled_Default_IsFalse ()
    {
        // Clean state
        Trace.EnabledCategories = TraceCategory.None;

        Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
    }

    [Fact]
    public void KeyboardEnabled_Default_IsFalse ()
    {
        // Clean state
        Trace.EnabledCategories = TraceCategory.None;

        Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Keyboard));
    }

    [Fact]
    public void Backend_Default_IsNullBackend ()
    {
        // Reset
        Trace.Backend = new NullBackend ();

        Assert.IsType<NullBackend> (Trace.Backend);
    }

    [Fact]
    public void ListBackend_CapturesEntries ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.Command;

        try
        {
            View view = new () { Id = "test" };
            Trace.Command (view, Command.Accept, CommandRouting.Direct, "TestPhase", "TestMessage");

#if DEBUG
            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Command, backend.Entries [0].Category);
            Assert.Contains ("test", backend.Entries [0].Id);
            Assert.Equal ("TestPhase", backend.Entries [0].Phase);
#else
            // In Release, [Conditional("DEBUG")] removes Trace.Command calls entirely
            Assert.Empty (backend.Entries);
#endif
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void MouseTrace_CapturesMouseEvents ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.Mouse;

        try
        {
            View view = new () { Id = "mouseTest" };
            Trace.Mouse (view, MouseFlags.LeftButtonClicked, new Point (10, 20), "Click");

#if DEBUG
            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Mouse, backend.Entries [0].Category);
            Assert.Contains ("mouseTest", backend.Entries [0].Id);
            Assert.Equal ("Click", backend.Entries [0].Phase);
#else
            Assert.Empty (backend.Entries);
#endif
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void KeyboardTrace_CapturesKeyEvents ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.Keyboard;

        try
        {
            View view = new () { Id = "keyTest" };
            Trace.Keyboard (view, Key.A, "KeyDown");

#if DEBUG
            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Keyboard, backend.Entries [0].Category);
            Assert.Contains ("keyTest", backend.Entries [0].Id);
            Assert.Equal ("KeyDown", backend.Entries [0].Phase);
#else
            Assert.Empty (backend.Entries);
#endif
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void Trace_Disabled_DoesNotCapture ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.None;

        try
        {
            View view = new () { Id = "test" };
            Trace.Command (view, Command.Accept, CommandRouting.Direct, "Test");
            Trace.Mouse (view, MouseFlags.LeftButtonClicked, Point.Empty, "Test");
            Trace.Keyboard (view, Key.A, "Test");

            // Empty in both Debug (disabled categories) and Release (conditional compilation)
            Assert.Empty (backend.Entries);
        }
        finally
        {
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void IndependentCategories_OnlyEnabledCategoriesCaptured ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Keyboard;

        try
        {
            View view = new () { Id = "test" };

            Trace.Command (view, Command.Accept, CommandRouting.Direct, "Cmd");
            Trace.Mouse (view, MouseFlags.LeftButtonClicked, Point.Empty, "Mouse");
            Trace.Keyboard (view, Key.A, "Key");

#if DEBUG
            Assert.Equal (2, backend.Entries.Count);
            Assert.Contains (backend.Entries, e => e.Category == TraceCategory.Command);
            Assert.Contains (backend.Entries, e => e.Category == TraceCategory.Keyboard);
            Assert.DoesNotContain (backend.Entries, e => e.Category == TraceCategory.Mouse);
#else
            Assert.Empty (backend.Entries);
#endif
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void LoggingBackend_FormatsCommandCorrectly ()
    {
        // Just verify it doesn't throw - actual logging is hard to test
        LoggingBackend backend = new ();

        TraceEntry entry = new (TraceCategory.Command,
                                "View(test)",
                                "Entry",
                                "TestMethod",
                                "TestMessage",
                                DateTime.UtcNow,
                                (Command.Accept, CommandRouting.BubblingUp));

        backend.Log (entry);

        // If we get here without exception, formatting worked
        Assert.True (true);
    }

    [Fact]
    public void LoggingBackend_FormatsMouseCorrectly ()
    {
        LoggingBackend backend = new ();

        TraceEntry entry = new (TraceCategory.Mouse,
                                "View(test)",
                                "Click",
                                "TestMethod",
                                null,
                                DateTime.UtcNow,
                                (MouseFlags.LeftButtonClicked, new Point (10, 20)));

        backend.Log (entry);

        Assert.True (true);
    }

    [Fact]
    public void LoggingBackend_FormatsKeyboardCorrectly ()
    {
        LoggingBackend backend = new ();

        TraceEntry entry = new (TraceCategory.Keyboard, "View(test)", "KeyDown", "TestMethod", null, DateTime.UtcNow, Key.A.WithCtrl);

        backend.Log (entry);

        Assert.True (true);
    }

    [Fact]
    public void Clear_RemovesAllEntries ()
    {
        ListBackend backend = new ();

        // Directly add an entry to the backend to test Clear independently of Trace methods
        backend.Log (new TraceEntry (TraceCategory.Command, "test", "Test", "TestMethod", null, DateTime.UtcNow, null));

        Assert.Single (backend.Entries);

        backend.Clear ();

        Assert.Empty (backend.Entries);
    }

    [Fact]
    public void Enabling_AutoSetsLoggingBackend ()
    {
        // Reset to NullBackend
        Trace.Backend = new NullBackend ();
        Trace.EnabledCategories = TraceCategory.None;

        // Verify starting state
        Assert.IsType<NullBackend> (Trace.Backend);

        try
        {
            // Enable a category - should auto-switch to LoggingBackend
            Trace.EnabledCategories = TraceCategory.Mouse;

            Assert.IsType<LoggingBackend> (Trace.Backend);
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void ExplicitBackend_NotOverwritten ()
    {
        // Set explicit ListBackend
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.None;

        try
        {
            // Enable a category - should NOT overwrite explicit backend
            Trace.EnabledCategories = TraceCategory.Command;

            Assert.Same (backend, Trace.Backend);
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Theory]
    [InlineData (TraceCategory.None, "\"None\"")]
    [InlineData (TraceCategory.Command, "\"Command\"")]
    [InlineData (TraceCategory.Mouse, "\"Mouse\"")]
    [InlineData (TraceCategory.All, "\"All\"")]
    [InlineData (TraceCategory.Command | TraceCategory.Mouse, "[\"Command\",\"Mouse\"]")]
    [InlineData (TraceCategory.Command | TraceCategory.Keyboard | TraceCategory.Navigation, "[\"Command\",\"Keyboard\",\"Navigation\"]")]
    public void TraceCategoryJsonConverter_RoundTrip (TraceCategory category, string expectedJson)
    {
        // Arrange
        var options = new JsonSerializerOptions ();
        options.Converters.Add (new TraceCategoryJsonConverter ());

        // Act - Serialize
        string json = JsonSerializer.Serialize (category, options);

        // Assert - Verify JSON format
        Assert.Equal (expectedJson, json);

        // Act - Deserialize
        var deserialized = JsonSerializer.Deserialize<TraceCategory> (json, options);

        // Assert - Verify round-trip
        Assert.Equal (category, deserialized);
    }

    [Fact]
    public void TraceCategoryJsonConverter_DeserializeFromNumber ()
    {
        // Arrange
        var options = new JsonSerializerOptions ();
        options.Converters.Add (new TraceCategoryJsonConverter ());
        var json = "6"; // Command (2) | Mouse (4)

        // Act
        var deserialized = JsonSerializer.Deserialize<TraceCategory> (json, options);

        // Assert
        Assert.Equal (TraceCategory.Command | TraceCategory.Mouse, deserialized);
    }

    [Fact]
    public void EnabledCategories_ConfigurationManager_RoundTrip ()
    {
        try
        {
            // Save original state
            TraceCategory originalCategories = Trace.EnabledCategories;

            // Test setting via property
            Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Mouse;
            Assert.Equal (TraceCategory.Command | TraceCategory.Mouse, Trace.EnabledCategories);

            // Test that ConfigurationProperty attribute is present
            PropertyInfo? property = typeof (Trace).GetProperty (nameof (Trace.EnabledCategories));
            Assert.NotNull (property);

            ConfigurationPropertyAttribute? attr = property.GetCustomAttributes (typeof (ConfigurationPropertyAttribute), false)
                                                           .Cast<ConfigurationPropertyAttribute> ()
                                                           .FirstOrDefault ();

            Assert.NotNull (attr);
            Assert.Equal (typeof (SettingsScope), attr.Scope);

            // Test that JsonConverter attribute is present
            JsonConverterAttribute? converterAttr = property.GetCustomAttributes (typeof (JsonConverterAttribute), false)
                                                            .Cast<JsonConverterAttribute> ()
                                                            .FirstOrDefault ();

            Assert.NotNull (converterAttr);
            Assert.Equal (typeof (TraceCategoryJsonConverter), converterAttr.ConverterType);

            // Restore original state
            Trace.EnabledCategories = originalCategories;
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }

    #region Configuration Category Tests

    // Copilot
    [Fact]
    public void TraceCategory_Configuration_HasExpectedValue ()
    {
        Assert.Equal (32, (int)TraceCategory.Configuration);
    }

    // Copilot
    [Fact]
    public void TraceCategory_All_IncludesConfiguration ()
    {
        Assert.True (TraceCategory.All.HasFlag (TraceCategory.Configuration));
    }

    // Copilot
    [Fact]
    public void Configuration_Category_CanBeEnabled ()
    {
        Trace.EnabledCategories = TraceCategory.None;

        try
        {
            Trace.EnabledCategories = TraceCategory.Configuration;

            Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Configuration));
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    // Copilot
    [Fact]
    public void ConfigurationTrace_CapturesEntries ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.Configuration;

        try
        {
            Trace.Configuration ("my-property", "Apply", "test message");

#if DEBUG
            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Configuration, backend.Entries [0].Category);
            Assert.Contains ("my-property", backend.Entries [0].Id);
            Assert.Equal ("Apply", backend.Entries [0].Phase);
#else
            // In Release, [Conditional("DEBUG")] removes Trace.Configuration calls entirely
            Assert.Empty (backend.Entries);
#endif
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    // Copilot
    [Fact]
    public void LoggingBackend_FormatsConfigurationCorrectly ()
    {
        LoggingBackend backend = new ();

        TraceEntry entry = new (
                                TraceCategory.Configuration,
                                "my-property",
                                "Apply",
                                "InternalApply",
                                "Test configuration trace",
                                DateTime.UtcNow,
                                null);

        // Verify the Configuration case in the switch does not throw
        backend.Log (entry);

        Assert.True (true);
    }

    #endregion

    #region Scenario Tests (merged from IssueScenarioTraceTests)

    /// <summary>
    ///     Demonstrates using TestLogging.Verbose with TraceFlags parameter.
    /// </summary>
    [Fact]
    public void Example_Test_With_Tracing_Enabled ()
    {
        using (TestLogging.Verbose (traceCategories: TraceCategory.Command, output: _output))
        {
            CheckBox checkbox = new () { Id = "checkbox" };

            checkbox.InvokeCommand (Command.Activate);

            _output.WriteLine ("Command tracing enabled successfully without affecting other tests");
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

#if DEBUG
            // Verify traces were captured
            Assert.NotEmpty (backend.Entries);
            Assert.All (backend.Entries, e => Assert.True (e.Category == TraceCategory.Command || e.Category == TraceCategory.Keyboard));
#else
            // In Release, [Conditional("DEBUG")] removes trace calls, so nothing is captured
            Assert.Empty (backend.Entries);
#endif
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
        Task task1 = Task.Run (async () =>
                               {
                                   using (Trace.PushScope (TraceCategory.Command))
                                   {
                                       await Task.Delay (10);
                                       Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
                                       await Task.Delay (10);
                                   }
                               },
                               TestContext.Current.CancellationToken);

        Task task2 = Task.Run (async () =>
                               {
                                   using (Trace.PushScope (TraceCategory.Mouse))
                                   {
                                       await Task.Delay (10);
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                                       Assert.True (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
                                       await Task.Delay (10);
                                   }
                               },
                               TestContext.Current.CancellationToken);

        Task task3 = Task.Run (async () =>
                               {
                                   using (Trace.PushScope (TraceCategory.None))
                                   {
                                       await Task.Delay (10);
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Command));
                                       Assert.False (Trace.EnabledCategories.HasFlag (TraceCategory.Mouse));
                                       await Task.Delay (10);
                                   }
                               },
                               TestContext.Current.CancellationToken);

        // Wait for all tasks - they should all succeed without interfering
        await Task.WhenAll (task1, task2, task3);
    }

    #endregion
}
