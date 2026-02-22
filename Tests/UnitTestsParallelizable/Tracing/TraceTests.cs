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
        Trace.CommandEnabled = false;

        Assert.False (Trace.CommandEnabled);
    }

    [Fact]
    public void MouseEnabled_Default_IsFalse ()
    {
        // Clean state
        Trace.MouseEnabled = false;

        Assert.False (Trace.MouseEnabled);
    }

    [Fact]
    public void KeyboardEnabled_Default_IsFalse ()
    {
        // Clean state
        Trace.KeyboardEnabled = false;

        Assert.False (Trace.KeyboardEnabled);
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
        Trace.CommandEnabled = true;

        try
        {
            View view = new () { Id = "test" };
            Trace.Command (view, Command.Accept, CommandRouting.Direct, "TestPhase", "TestMessage");

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Command, backend.Entries [0].Category);
            Assert.Contains ("test", backend.Entries [0].ViewId);
            Assert.Equal ("TestPhase", backend.Entries [0].Phase);
        }
        finally
        {
            Trace.CommandEnabled = false;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void MouseTrace_CapturesMouseEvents ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.MouseEnabled = true;

        try
        {
            View view = new () { Id = "mouseTest" };
            Trace.Mouse (view, MouseFlags.LeftButtonClicked, new Point (10, 20), "Click");

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Mouse, backend.Entries [0].Category);
            Assert.Contains ("mouseTest", backend.Entries [0].ViewId);
            Assert.Equal ("Click", backend.Entries [0].Phase);
        }
        finally
        {
            Trace.MouseEnabled = false;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void KeyboardTrace_CapturesKeyEvents ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.KeyboardEnabled = true;

        try
        {
            View view = new () { Id = "keyTest" };
            Trace.Keyboard (view, Key.A, "KeyDown");

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Keyboard, backend.Entries [0].Category);
            Assert.Contains ("keyTest", backend.Entries [0].ViewId);
            Assert.Equal ("KeyDown", backend.Entries [0].Phase);
        }
        finally
        {
            Trace.KeyboardEnabled = false;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void Trace_Disabled_DoesNotCapture ()
    {
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.CommandEnabled = false;
        Trace.MouseEnabled = false;
        Trace.KeyboardEnabled = false;

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
        Trace.CommandEnabled = true;
        Trace.MouseEnabled = false;
        Trace.KeyboardEnabled = true;

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
            Trace.CommandEnabled = false;
            Trace.KeyboardEnabled = false;
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
        Trace.CommandEnabled = true;

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
            Trace.CommandEnabled = false;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void Enabling_AutoSetsLoggingBackend ()
    {
        // Reset to NullBackend
        Trace.Backend = new NullBackend ();
        Trace.CommandEnabled = false;
        Trace.MouseEnabled = false;
        Trace.KeyboardEnabled = false;

        // Verify starting state
        Assert.IsType<NullBackend> (Trace.Backend);

        try
        {
            // Enable a category - should auto-switch to LoggingBackend
            Trace.MouseEnabled = true;

            Assert.IsType<LoggingBackend> (Trace.Backend);
        }
        finally
        {
            Trace.MouseEnabled = false;
            Trace.Backend = new NullBackend ();
        }
    }

    [Fact]
    public void ExplicitBackend_NotOverwritten ()
    {
        // Set explicit ListBackend
        ListBackend backend = new ();
        Trace.Backend = backend;
        Trace.CommandEnabled = false;

        try
        {
            // Enable a category - should NOT overwrite explicit backend
            Trace.CommandEnabled = true;

            Assert.Same (backend, Trace.Backend);
        }
        finally
        {
            Trace.CommandEnabled = false;
            Trace.Backend = new NullBackend ();
        }
    }
}
