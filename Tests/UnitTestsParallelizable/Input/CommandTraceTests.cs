namespace ViewBaseTests.Commands;

/// <summary>
///     Tests for <see cref="CommandTrace"/> infrastructure.
/// </summary>

// Claude - Opus 4.5
public class CommandTraceTests : IDisposable
{
    private readonly ICommandTraceBackend _originalBackend = CommandTrace.Backend;

    // Capture original backend to restore after test

    public void Dispose () =>

        // Restore original backend
        CommandTrace.Backend = _originalBackend;

    [Fact]
    public void Backend_DefaultIsNullBackend ()
    {
        // Reset to default by setting null - should fall back to NullBackend
        CommandTrace.Backend = null!;

        // Should get NullBackend (the shared default instance) when set to null
        Assert.IsAssignableFrom<CommandTrace.NullBackend> (CommandTrace.Backend);
    }

    [Fact]
    public void NullBackend_DoesNotThrow ()
    {
        CommandTrace.NullBackend backend = new ();

        // Should not throw
        backend.Log (new RouteTraceEntry ("TestView", Command.Activate, CommandRouting.Direct, CommandTracePhase.Entry, "TestMethod", null, DateTime.UtcNow));

        backend.Clear ();
    }

    [Fact]
    public void ListBackend_CapturesEntries ()
    {
        CommandTrace.ListBackend backend = new ();

        RouteTraceEntry entry = new ("TestView",
                                     Command.Accept,
                                     CommandRouting.BubblingUp,
                                     CommandTracePhase.Routing,
                                     "TestMethod",
                                     "Test message",
                                     DateTime.UtcNow);

        backend.Log (entry);

        Assert.Single (backend.Entries);
        Assert.Equal ("TestView", backend.Entries [0].ViewId);
        Assert.Equal (Command.Accept, backend.Entries [0].Command);
        Assert.Equal (CommandRouting.BubblingUp, backend.Entries [0].Routing);
        Assert.Equal (CommandTracePhase.Routing, backend.Entries [0].Phase);
        Assert.Equal ("TestMethod", backend.Entries [0].Method);
        Assert.Equal ("Test message", backend.Entries [0].Message);
    }

    [Fact]
    public void ListBackend_ClearRemovesEntries ()
    {
        CommandTrace.ListBackend backend = new ();

        backend.Log (new RouteTraceEntry ("TestView", Command.Activate, CommandRouting.Direct, CommandTracePhase.Entry, "TestMethod", null, DateTime.UtcNow));

        Assert.Single (backend.Entries);

        backend.Clear ();

        Assert.Empty (backend.Entries);
    }

    [Fact]
    public void TraceRoute_UsesConfiguredBackend ()
    {
        CommandTrace.ListBackend backend = new ();
        CommandTrace.Backend = backend;

        View view = new () { Id = "TestView" };

        // TraceRoute is [Conditional("DEBUG")] so only runs in Debug builds
#if DEBUG
        CommandTrace.TraceRoute (view, Command.Activate, CommandRouting.Direct, CommandTracePhase.Entry, "test message");

        Assert.Single (backend.Entries);
        Assert.Contains ("TestView", backend.Entries [0].ViewId);
        Assert.Equal (Command.Activate, backend.Entries [0].Command);
        Assert.Equal (CommandRouting.Direct, backend.Entries [0].Routing);
        Assert.Equal (CommandTracePhase.Entry, backend.Entries [0].Phase);
        Assert.Equal ("test message", backend.Entries [0].Message);
#else
        // In Release builds, TraceRoute is a no-op
        Assert.Empty (backend.Entries);
#endif
    }

    [Fact]
    public void TraceRoute_WithContext_ExtractsCommandAndRouting ()
    {
        CommandTrace.ListBackend backend = new ();
        CommandTrace.Backend = backend;

        View view = new () { Id = "TestView" };
        CommandContext ctx = new (Command.Accept, new WeakReference<View> (view), null) { Routing = CommandRouting.DispatchingDown };

#if DEBUG
        CommandTrace.TraceRoute (view, ctx, CommandTracePhase.Handler);

        Assert.Single (backend.Entries);
        Assert.Equal (Command.Accept, backend.Entries [0].Command);
        Assert.Equal (CommandRouting.DispatchingDown, backend.Entries [0].Routing);
        Assert.Equal (CommandTracePhase.Handler, backend.Entries [0].Phase);
#else
        Assert.Empty (backend.Entries);
#endif
    }

    [Fact]
    public void TraceRoute_WithNullContext_UsesDefaults ()
    {
        CommandTrace.ListBackend backend = new ();
        CommandTrace.Backend = backend;

        View view = new () { Id = "TestView" };

#if DEBUG
        CommandTrace.TraceRoute (view, null, CommandTracePhase.Exit);

        Assert.Single (backend.Entries);
        Assert.Equal (Command.NotBound, backend.Entries [0].Command);
        Assert.Equal (CommandRouting.Direct, backend.Entries [0].Routing);
#else
        Assert.Empty (backend.Entries);
#endif
    }

    [Fact]
    public void InvokeCommand_TracesHandler ()
    {
        CommandTrace.ListBackend backend = new ();
        CommandTrace.Backend = backend;

        View view = new () { Id = "TestView" };

        // Invoke a command
        view.InvokeCommand (Command.Activate);

#if DEBUG

        // Should have at least one trace entry from InvokeCommand
        Assert.NotEmpty (backend.Entries);
        Assert.Contains (backend.Entries, e => e.Phase == CommandTracePhase.Handler);
#endif
    }

    [Fact]
    public void RaiseActivating_TracesEntryAndEvent ()
    {
        CommandTrace.ListBackend backend = new ();
        CommandTrace.Backend = backend;

        View view = new () { Id = "TestView" };

        // Invoke Activate command which calls RaiseActivating
        view.InvokeCommand (Command.Activate);

#if DEBUG

        // Should have Entry traces
        Assert.Contains (backend.Entries, e => e.Phase == CommandTracePhase.Entry);

        // Should have Event trace for Activating
        Assert.Contains (backend.Entries, e => e.Phase == CommandTracePhase.Event && e.Message?.Contains ("Activating") == true);
#endif
    }

    [Fact]
    public void RouteTraceEntry_RecordEquality ()
    {
        DateTime timestamp = DateTime.UtcNow;

        RouteTraceEntry entry1 = new ("View1", Command.Accept, CommandRouting.Direct, CommandTracePhase.Entry, "Method1", "Message", timestamp);

        RouteTraceEntry entry2 = new ("View1", Command.Accept, CommandRouting.Direct, CommandTracePhase.Entry, "Method1", "Message", timestamp);

        Assert.Equal (entry1, entry2);
    }
}
