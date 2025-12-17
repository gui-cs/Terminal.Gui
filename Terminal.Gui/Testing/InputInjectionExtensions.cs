using System.Runtime.CompilerServices;

namespace Terminal.Gui.Testing;

/// <summary>
/// Extension methods for input injection.
/// </summary>
public static class InputInjectionExtensions
{
    /// <summary>
    /// Injects a key event (convenience method).
    /// </summary>
    /// <param name="app">The application instance.</param>
    /// <param name="key">The key to inject.</param>
    public static void InjectKey (this IApplication app, Key key)
    {
        app.GetInputInjector ().InjectKey (key);
    }

    /// <summary>
    /// Injects a mouse event (convenience method).
    /// </summary>
    /// <param name="app">The application instance.</param>
    /// <param name="mouseEvent">The mouse event to inject.</param>
    public static void InjectMouse (this IApplication app, Mouse mouseEvent)
    {
        app.GetInputInjector ().InjectMouse (mouseEvent);
    }

    /// <summary>
    /// Injects a sequence of events (convenience method).
    /// </summary>
    /// <param name="app">The application instance.</param>
    /// <param name="events">The events to inject.</param>
    public static void InjectSequence (this IApplication app, params InputEvent [] events)
    {
        app.GetInputInjector ().InjectSequence (events);
    }
}
