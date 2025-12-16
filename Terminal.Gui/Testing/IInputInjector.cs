namespace Terminal.Gui;

/// <summary>
/// Mode for input injection, controlling how input flows through the system.
/// </summary>
public enum InputInjectionMode
{
    /// <summary>
    /// Direct event injection - bypasses encoding/parsing, preserves all properties.
    /// Use for testing View/Application logic with precise control.
    /// </summary>
    Direct,

    /// <summary>
    /// Pipeline injection - goes through full encoding/parsing pipeline.
    /// Use for testing ANSI encoding/decoding and parser behavior.
    /// </summary>
    Pipeline,

    /// <summary>
    /// Automatic mode - uses Direct for simple test scenarios, Pipeline for full driver testing.
    /// Recommended default for most tests.
    /// </summary>
    Auto
}

/// <summary>
/// Configuration for input injection behavior.
/// </summary>
public class InputInjectionOptions
{
    /// <summary>
    /// Injection mode (Direct, Pipeline, or Auto).
    /// </summary>
    public InputInjectionMode Mode { get; set; } = InputInjectionMode.Auto;

    /// <summary>
    /// Whether to automatically process the input queue after injection.
    /// </summary>
    public bool AutoProcess { get; set; } = true;

    /// <summary>
    /// Time provider to use for timestamps and timing.
    /// </summary>
    public ITimeProvider? TimeProvider { get; set; }
}

/// <summary>
/// High-level input injection API - single entry point for all injection.
/// </summary>
public interface IInputInjector
{
    /// <summary>
    /// Injects a keyboard event. Handles encoding, queueing, and processing automatically.
    /// </summary>
    /// <param name="key">The key to inject.</param>
    /// <param name="options">Optional injection options.</param>
    void InjectKey (Key key, InputInjectionOptions? options = null);

    /// <summary>
    /// Injects a mouse event. Handles encoding, queueing, and processing automatically.
    /// </summary>
    /// <param name="mouseEvent">The mouse event to inject.</param>
    /// <param name="options">Optional injection options.</param>
    void InjectMouse (Mouse mouseEvent, InputInjectionOptions? options = null);

    /// <summary>
    /// Injects a sequence of input events with delays between them.
    /// </summary>
    /// <param name="events">The sequence of events to inject.</param>
    /// <param name="options">Optional injection options.</param>
    void InjectSequence (IEnumerable<InputEvent> events, InputInjectionOptions? options = null);

    /// <summary>
    /// Forces processing of the input queue (usually automatic).
    /// </summary>
    void ProcessQueue ();
}

/// <summary>
/// Base class for input events in sequences.
/// </summary>
public abstract record InputEvent
{
    /// <summary>
    /// Optional delay before processing this event.
    /// </summary>
    public TimeSpan? Delay { get; init; }
}

/// <summary>
/// Keyboard event in a sequence.
/// </summary>
/// <param name="Key">The key to inject.</param>
public record KeyEvent (Key Key) : InputEvent;

/// <summary>
/// Mouse event in a sequence.
/// </summary>
/// <param name="Mouse">The mouse event to inject.</param>
public record MouseEvent (Mouse Mouse) : InputEvent;
