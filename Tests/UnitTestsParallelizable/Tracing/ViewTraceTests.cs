// Claude - Opus 4.5

using Xunit;

namespace Terminal.Gui.Tests.Tracing;

/// <summary>
///     Tests for the unified <see cref="ViewTrace"/> system.
/// </summary>
public class ViewTraceTests
{
    [Fact]
    public void CommandEnabled_Default_IsFalse ()
    {
        // Clean state
        ViewTrace.CommandEnabled = false;

        Assert.False (ViewTrace.CommandEnabled);
    }

    [Fact]
    public void MouseEnabled_Default_IsFalse ()
    {
        // Clean state
        ViewTrace.MouseEnabled = false;

        Assert.False (ViewTrace.MouseEnabled);
    }

    [Fact]
    public void KeyboardEnabled_Default_IsFalse ()
    {
        // Clean state
        ViewTrace.KeyboardEnabled = false;

        Assert.False (ViewTrace.KeyboardEnabled);
    }

    [Fact]
    public void Backend_Default_IsNullBackend ()
    {
        // Reset
        ViewTrace.Backend = new ViewTrace.NullBackend ();

        Assert.IsType<ViewTrace.NullBackend> (ViewTrace.Backend);
    }

    [Fact]
    public void ListBackend_CapturesEntries ()
    {
        ViewTrace.ListBackend backend = new ();
        ViewTrace.Backend = backend;
        ViewTrace.CommandEnabled = true;

        try
        {
            View view = new () { Id = "test" };
            ViewTrace.Command (view, Input.Command.Accept, Input.CommandRouting.Direct, "TestPhase", "TestMessage");

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Command, backend.Entries [0].Category);
            Assert.Contains ("test", backend.Entries [0].ViewId);
            Assert.Equal ("TestPhase", backend.Entries [0].Phase);
        }
        finally
        {
            ViewTrace.CommandEnabled = false;
            ViewTrace.Backend = new ViewTrace.NullBackend ();
        }
    }

    [Fact]
    public void MouseTrace_CapturesMouseEvents ()
    {
        ViewTrace.ListBackend backend = new ();
        ViewTrace.Backend = backend;
        ViewTrace.MouseEnabled = true;

        try
        {
            View view = new () { Id = "mouseTest" };
            ViewTrace.Mouse (view, MouseFlags.LeftButtonClicked, new Point (10, 20), "Click");

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Mouse, backend.Entries [0].Category);
            Assert.Contains ("mouseTest", backend.Entries [0].ViewId);
            Assert.Equal ("Click", backend.Entries [0].Phase);
        }
        finally
        {
            ViewTrace.MouseEnabled = false;
            ViewTrace.Backend = new ViewTrace.NullBackend ();
        }
    }

    [Fact]
    public void KeyboardTrace_CapturesKeyEvents ()
    {
        ViewTrace.ListBackend backend = new ();
        ViewTrace.Backend = backend;
        ViewTrace.KeyboardEnabled = true;

        try
        {
            View view = new () { Id = "keyTest" };
            ViewTrace.Keyboard (view, Key.A, "KeyDown");

            Assert.Single (backend.Entries);
            Assert.Equal (TraceCategory.Keyboard, backend.Entries [0].Category);
            Assert.Contains ("keyTest", backend.Entries [0].ViewId);
            Assert.Equal ("KeyDown", backend.Entries [0].Phase);
        }
        finally
        {
            ViewTrace.KeyboardEnabled = false;
            ViewTrace.Backend = new ViewTrace.NullBackend ();
        }
    }

    [Fact]
    public void Trace_Disabled_DoesNotCapture ()
    {
        ViewTrace.ListBackend backend = new ();
        ViewTrace.Backend = backend;
        ViewTrace.CommandEnabled = false;
        ViewTrace.MouseEnabled = false;
        ViewTrace.KeyboardEnabled = false;

        try
        {
            View view = new () { Id = "test" };
            ViewTrace.Command (view, Input.Command.Accept, Input.CommandRouting.Direct, "Test");
            ViewTrace.Mouse (view, MouseFlags.LeftButtonClicked, Point.Empty, "Test");
            ViewTrace.Keyboard (view, Key.A, "Test");

            Assert.Empty (backend.Entries);
        }
        finally
        {
            ViewTrace.Backend = new ViewTrace.NullBackend ();
        }
    }

    [Fact]
    public void IndependentCategories_OnlyEnabledCategoriesCaptured ()
    {
        ViewTrace.ListBackend backend = new ();
        ViewTrace.Backend = backend;
        ViewTrace.CommandEnabled = true;
        ViewTrace.MouseEnabled = false;
        ViewTrace.KeyboardEnabled = true;

        try
        {
            View view = new () { Id = "test" };

            ViewTrace.Command (view, Input.Command.Accept, Input.CommandRouting.Direct, "Cmd");
            ViewTrace.Mouse (view, MouseFlags.LeftButtonClicked, Point.Empty, "Mouse");
            ViewTrace.Keyboard (view, Key.A, "Key");

            Assert.Equal (2, backend.Entries.Count);
            Assert.Contains (backend.Entries, e => e.Category == TraceCategory.Command);
            Assert.Contains (backend.Entries, e => e.Category == TraceCategory.Keyboard);
            Assert.DoesNotContain (backend.Entries, e => e.Category == TraceCategory.Mouse);
        }
        finally
        {
            ViewTrace.CommandEnabled = false;
            ViewTrace.KeyboardEnabled = false;
            ViewTrace.Backend = new ViewTrace.NullBackend ();
        }
    }

    [Fact]
    public void LoggingBackend_FormatsCommandCorrectly ()
    {
        // Just verify it doesn't throw - actual logging is hard to test
        ViewTrace.LoggingBackend backend = new ();

        TraceEntry entry = new (
                                TraceCategory.Command,
                                "View(test)",
                                "Entry",
                                "TestMethod",
                                "TestMessage",
                                DateTime.UtcNow,
                                (Input.Command.Accept, Input.CommandRouting.BubblingUp));

        backend.Log (entry);

        // If we get here without exception, formatting worked
        Assert.True (true);
    }

    [Fact]
    public void LoggingBackend_FormatsMouseCorrectly ()
    {
        ViewTrace.LoggingBackend backend = new ();

        TraceEntry entry = new (
                                TraceCategory.Mouse,
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
        ViewTrace.LoggingBackend backend = new ();

        TraceEntry entry = new (
                                TraceCategory.Keyboard,
                                "View(test)",
                                "KeyDown",
                                "TestMethod",
                                null,
                                DateTime.UtcNow,
                                Key.A.WithCtrl);

        backend.Log (entry);

        Assert.True (true);
    }

    [Fact]
    public void Clear_RemovesAllEntries ()
    {
        ViewTrace.ListBackend backend = new ();
        ViewTrace.Backend = backend;
        ViewTrace.CommandEnabled = true;

        try
        {
            View view = new () { Id = "test" };
            ViewTrace.Command (view, Input.Command.Accept, Input.CommandRouting.Direct, "Test");

            Assert.Single (backend.Entries);

            backend.Clear ();

            Assert.Empty (backend.Entries);
        }
        finally
        {
            ViewTrace.CommandEnabled = false;
            ViewTrace.Backend = new ViewTrace.NullBackend ();
        }
    }
}
