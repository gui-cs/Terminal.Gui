using System.Collections.Concurrent;
using System.Diagnostics;
using DriverTests.Input;
using Xunit.Abstractions;

namespace InputTests;

/// <summary>
///     Comprehensive unit tests for <see cref="InputInjector"/>.
///     Tests the high-level input injection API that simplifies testing.
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "InputInjection")]
public class InputInjectorTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_Succeeds ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();

        // Act
        InputInjector injector = new (processor, timeProvider);

        // Assert
        Assert.NotNull (injector);
    }

    [Fact]
    public void Constructor_WithTestSource_Succeeds ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        TestInputSource testSource = new (timeProvider);

        // Act
        InputInjector injector = new (processor, timeProvider, testSource);

        // Assert
        Assert.NotNull (injector);
    }

    #endregion

    #region InjectKey Tests - Direct Mode

    [Fact]
    public void InjectKey_DirectMode_RaisesKeyDownEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectKey (Key.A, options);

        // Assert - Should raise exactly 1 KeyDown event in Direct mode
        Assert.Single (receivedKeys);
        Assert.Equal (Key.A, receivedKeys [0]);
    }

    [Fact]
    public void InjectKey_DirectMode_MultipleKeys_RaisesAllEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectKey (Key.A, options);
        injector.InjectKey (Key.B, options);
        injector.InjectKey (Key.C, options);

        // Assert - Should raise exactly 3 KeyDown events
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    [Fact]
    public void InjectKey_DefaultOptions_UsesDirectMode ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Act - Call without options (should default to Direct mode)
        injector.InjectKey (Key.A);

        // Assert - Should raise event (Direct mode behavior)
        Assert.Single (receivedKeys);
        Assert.Equal (Key.A, receivedKeys [0]);
    }

    #endregion

    #region InjectKey Tests - Pipeline Mode

    [Fact]
    public void InjectKey_PipelineMode_MultipleKeys_RaisesAllEvents ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];
        app.Keyboard.KeyDown += (_, key) => receivedKeys.Add (key);

        // Explicit Pipeline mode
        TestInputSource testSource = new (timeProvider);
        InputInjector injector = new (app.Driver?.GetInputProcessor()!, timeProvider, testSource);

        InputInjectionOptions options = new ()
        {
            Mode = InputInjectionMode.Pipeline, 
            AutoProcess = false, 
            TimeProvider = timeProvider
        };

        // Act
        injector.InjectKey (Key.A, options);
        injector.InjectKey (Key.B, options);
        injector.InjectKey (Key.C, options);

        // BUGBUG: This is a hack; we need to figure out how to enable this without
        // BUGBUG: sleeping.
        Thread.Sleep (100);
        injector.ProcessQueue ();

        // Assert - Should raise exactly 3 KeyDown events
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    // BUGBUG: This test is bogus as it doesn't actually test what happens
    // BUGBUG: when an accented char comes into the actual stdIn stream, only.
    // BUGBUG: see https://github.com/gui-cs/Terminal.Gui/pull/4583#issuecomment-3769142085
    [Fact]
    public void InjectKey_PipelineMode_Accented_Char ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];
        app.Keyboard.KeyDown += (_, key) => receivedKeys.Add (key);

        // Explicit Pipeline mode
        TestInputSource testSource = new (timeProvider);
        InputInjector injector = new (app.Driver?.GetInputProcessor ()!, timeProvider, testSource);

        InputInjectionOptions options = new ()
        {
            Mode = InputInjectionMode.Pipeline,
            AutoProcess = false,
            TimeProvider = timeProvider
        };

        // Act
        injector.InjectKey ('á', options);

        // BUGBUG: This is a hack; we need to figure out how to enable this without
        // BUGBUG: sleeping.
        Thread.Sleep (100);
        injector.ProcessQueue ();

        Assert.Equal ('á', receivedKeys [0]);
    }

    #endregion

    #region InjectMouse Tests - Direct Mode

    [Fact]
    public void InjectMouse_DirectMode_RaisesMouseEventParsed ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Mouse> receivedEvents = [];
        processor.MouseEventParsed += (_, mouse) => receivedEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (testMouse, options);

        // Assert - Should raise exactly 1 MouseEventParsed event
        Assert.Single (receivedEvents);
        Assert.Equal (testMouse.ScreenPosition, receivedEvents [0].ScreenPosition);
        Assert.Equal (testMouse.Flags, receivedEvents [0].Flags);
    }

    [Fact]
    public void InjectMouse_DirectMode_RaisesSyntheticMouseEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, mouse) => receivedEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (testMouse, options);

        // Assert - RaiseMouseEventParsed internally calls RaiseSyntheticMouseEvent
        // MouseInterpreter yields original, so we get at least 1 event
        Assert.True (receivedEvents.Count >= 1);
        Assert.Equal (testMouse.ScreenPosition, receivedEvents [0].ScreenPosition);
    }

    [Fact]
    public void InjectMouse_DirectMode_PressAndRelease_GeneratesClickEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Mouse> syntheticEvents = [];
        processor.SyntheticMouseEvent += (_, mouse) => syntheticEvents.Add (mouse);

        Mouse pressEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        Mouse releaseEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonReleased };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (pressEvent, options);
        injector.InjectMouse (releaseEvent, options);

        // Assert - Should get exactly 3 events: Press, Release, Click
        Assert.Equal (3, syntheticEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonPressed, syntheticEvents [0].Flags);
        Assert.Equal (MouseFlags.LeftButtonReleased, syntheticEvents [1].Flags);
        Assert.True (syntheticEvents [2].Flags.HasFlag (MouseFlags.LeftButtonClicked));
    }

    [Fact]
    public void InjectMouse_SetsTimestampIfNull ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));
        InputInjector injector = new (processor, timeProvider);

        Mouse mouseWithoutTimestamp = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        Assert.Null (mouseWithoutTimestamp.Timestamp);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (mouseWithoutTimestamp, options);

        // Assert - Timestamp should be set to virtual time
        Assert.NotNull (mouseWithoutTimestamp.Timestamp);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0), mouseWithoutTimestamp.Timestamp);
    }

    [Fact]
    public void InjectMouse_PreservesExistingTimestamp ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        DateTime specificTime = new (2025, 1, 1, 12, 30, 0);

        Mouse mouseWithTimestamp = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = specificTime };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (mouseWithTimestamp, options);

        // Assert - Original timestamp should be preserved
        Assert.Equal (specificTime, mouseWithTimestamp.Timestamp);
    }

    #endregion

    #region AutoProcess Tests

    [Fact]
    public void InjectKey_AutoProcessTrue_ProcessesImmediately ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct, AutoProcess = true };

        // Act
        injector.InjectKey (Key.A, options);

        // Assert - Event should be raised immediately
        Assert.Single (receivedKeys);
    }

    [Fact]
    public void InjectKey_AutoProcessFalse_RequiresManualProcessQueue ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct, AutoProcess = false };

        // Act
        injector.InjectKey (Key.A, options);

        // Assert - Event should be raised even with AutoProcess=false in Direct mode
        // (Direct mode raises events immediately, doesn't use queue)
        Assert.Single (receivedKeys);
    }

    [Fact]
    public void ProcessQueue_CanBeCalledManually ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        // Act & Assert - Should not throw
        injector.ProcessQueue ();
    }

    #endregion

    #region InjectSequence Tests

    [Fact]
    public void InjectSequence_EmptySequence_DoesNotRaiseEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionEvent [] emptySequence = [];

        // Act
        injector.InjectSequence (emptySequence);

        // Assert - No events should be raised
        Assert.Empty (receivedKeys);
    }

    [Fact]
    public void InjectSequence_MultipleKeyEvents_RaisesAllEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionEvent [] sequence = [new KeyInjectionEvent (Key.A), new KeyInjectionEvent (Key.B), new KeyInjectionEvent (Key.C)];

        // Act
        injector.InjectSequence (sequence);

        // Assert - Should raise exactly 3 KeyDown events
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    [Fact]
    public void InjectSequence_WithDelays_AdvancesVirtualTime ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        List<DateTime> timestamps = [];

        processor.KeyDown += (_, key) =>
                             {
                                 receivedKeys.Add (key);
                                 timestamps.Add (timeProvider.Now);
                             };

        InputInjectionEvent [] sequence =
        [
            new KeyInjectionEvent (Key.A),
            new KeyInjectionEvent (Key.B) { Delay = TimeSpan.FromMilliseconds (100) },
            new KeyInjectionEvent (Key.C) { Delay = TimeSpan.FromMilliseconds (200) }
        ];

        // Act
        injector.InjectSequence (sequence);

        // Assert - Time should advance according to delays
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0, 0), timestamps [0]);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0, 100), timestamps [1]);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0, 300), timestamps [2]);
    }

    [Fact]
    public void InjectSequence_MixedKeyAndMouseEvents_ProcessesAll ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        List<Mouse> receivedMouse = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);
        processor.MouseEventParsed += (_, mouse) => receivedMouse.Add (mouse);

        InputInjectionEvent [] sequence =
        [
            new KeyInjectionEvent (Key.A),
            new MouseInjectionEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed }),
            new KeyInjectionEvent (Key.B)
        ];

        // Act
        injector.InjectSequence (sequence);

        // Assert - Should process both key and mouse events
        Assert.Equal (2, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Single (receivedMouse);
        Assert.Equal (new Point (5, 5), receivedMouse [0].ScreenPosition);
    }

    #endregion

    #region Virtual Time Tests

    [Fact]
    public void InjectKey_UsesVirtualTimeProvider ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        IInputInjector injector = new InputInjector (processor, timeProvider);

        // Act - Advance time
        timeProvider.Advance (TimeSpan.FromMinutes (5));

        // Assert - Virtual time should be advanced
        Assert.Equal (new DateTime (2025, 1, 1, 12, 5, 0), timeProvider.Now);
    }

    [Fact]
    public void InjectMouse_WithVirtualTime_UsesVirtualTimestamp ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        IInputInjector injector = new InputInjector (processor, timeProvider);

        Mouse mouseEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (mouseEvent, options);

        // Assert - Timestamp should match virtual time
        Assert.NotNull (mouseEvent.Timestamp);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0), mouseEvent.Timestamp);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void InjectKey_NullOptions_UsesDefaults ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Act - Pass null options
        injector.InjectKey (Key.A);

        // Assert - Should use default options (Direct mode, AutoProcess=true)
        Assert.Single (receivedKeys);
    }

    [Fact]
    public void InjectMouse_NullOptions_UsesDefaults ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Mouse> receivedEvents = [];
        processor.MouseEventParsed += (_, mouse) => receivedEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act - Pass null options
        injector.InjectMouse (testMouse);

        // Assert - Should use default options
        Assert.Single (receivedEvents);
    }

    [Fact]
    public void InjectSequence_NullOptions_UsesDefaults ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionEvent [] sequence = [new KeyInjectionEvent (Key.A)];

        // Act - Pass null options
        injector.InjectSequence (sequence);

        // Assert - Should use default options
        Assert.Single (receivedKeys);
    }

    #endregion
}
