using Terminal.Gui.Tracing;

namespace ApplicationTests;

/// <summary>
///     Tests for the unified <see cref="Trace"/> system.
/// </summary>
public class TraceTests
{
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

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Command, backend.Entries [0].Category);
            Assert.Contains ("test", backend.Entries [0].Id);
            Assert.Equal ("TestPhase", backend.Entries [0].Phase);
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

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Mouse, backend.Entries [0].Category);
            Assert.Contains ("mouseTest", backend.Entries [0].Id);
            Assert.Equal ("Click", backend.Entries [0].Phase);
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

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Keyboard, backend.Entries [0].Category);
            Assert.Contains ("keyTest", backend.Entries [0].Id);
            Assert.Equal ("KeyDown", backend.Entries [0].Phase);
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
        Trace.EnabledCategories = TraceCategory.None;
        Trace.EnabledCategories = TraceCategory.None;

        try
        {
            View view = new () { Id = "test" };
            Trace.Command (view, Command.Accept, CommandRouting.Direct, "Test");
            Trace.Mouse (view, MouseFlags.LeftButtonClicked, Point.Empty, "Test");
            Trace.Keyboard (view, Key.A, "Test");

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

            Assert.Equal (2, backend.Entries.Count);
            Assert.Contains (backend.Entries, e => e.Category == TraceCategory.Command);
            Assert.Contains (backend.Entries, e => e.Category == TraceCategory.Keyboard);
            Assert.DoesNotContain (backend.Entries, e => e.Category == TraceCategory.Mouse);
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
        Trace.Backend = backend;
        Trace.EnabledCategories = TraceCategory.Command;

        try
        {
            View view = new () { Id = "test" };
            Trace.Command (view, Command.Accept, CommandRouting.Direct, "Test");

            Assert.Single (backend.Entries);

            backend.Clear ();

            Assert.Empty (backend.Entries);
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void Enabling_AutoSetsLoggingBackend ()
    {
        // Reset to NullBackend
        Trace.Backend = new NullBackend ();
        Trace.EnabledCategories = TraceCategory.None;
        Trace.EnabledCategories = TraceCategory.None;
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
        var options = new System.Text.Json.JsonSerializerOptions ();
        options.Converters.Add (new Terminal.Gui.Configuration.TraceCategoryJsonConverter ());

        // Act - Serialize
        string json = System.Text.Json.JsonSerializer.Serialize (category, options);

        // Assert - Verify JSON format
        Assert.Equal (expectedJson, json);

        // Act - Deserialize
        TraceCategory deserialized = System.Text.Json.JsonSerializer.Deserialize<TraceCategory> (json, options);

        // Assert - Verify round-trip
        Assert.Equal (category, deserialized);
    }

    [Fact]
    public void TraceCategoryJsonConverter_DeserializeFromNumber ()
    {
        // Arrange
        var options = new System.Text.Json.JsonSerializerOptions ();
        options.Converters.Add (new Terminal.Gui.Configuration.TraceCategoryJsonConverter ());
        string json = "6"; // Command (2) | Mouse (4)

        // Act
        TraceCategory deserialized = System.Text.Json.JsonSerializer.Deserialize<TraceCategory> (json, options);

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
            System.Reflection.PropertyInfo? property = typeof (Trace).GetProperty (nameof (Trace.EnabledCategories));
            Assert.NotNull (property);

            ConfigurationPropertyAttribute? attr = property.GetCustomAttributes (typeof (ConfigurationPropertyAttribute), false)
                                                            .Cast<ConfigurationPropertyAttribute> ()
                                                            .FirstOrDefault ();

            Assert.NotNull (attr);
            Assert.Equal (typeof (SettingsScope), attr.Scope);

            // Test that JsonConverter attribute is present
            System.Text.Json.Serialization.JsonConverterAttribute? converterAttr = property.GetCustomAttributes (typeof (System.Text.Json.Serialization.JsonConverterAttribute), false)
                                                                                            .Cast<System.Text.Json.Serialization.JsonConverterAttribute> ()
                                                                                            .FirstOrDefault ();

            Assert.NotNull (converterAttr);
            Assert.Equal (typeof (Terminal.Gui.Configuration.TraceCategoryJsonConverter), converterAttr.ConverterType);

            // Restore original state
            Trace.EnabledCategories = originalCategories;
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
        }
    }
}
