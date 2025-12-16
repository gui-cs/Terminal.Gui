using System.Runtime.CompilerServices;

namespace Terminal.Gui.Testing;

/// <summary>
/// Extension methods for input injection.
/// </summary>
public static class InputInjectionExtensions
{
    private static readonly ConditionalWeakTable<IApplication, IInputInjector> _injectorCache = new ();

    /// <summary>
    /// Gets or creates the input injector for this application.
    /// </summary>
    /// <param name="app">The application instance.</param>
    /// <returns>The input injector for the application.</returns>
    public static IInputInjector GetInputInjector (this IApplication app)
    {
        // Cache injector per application instance
        return _injectorCache.GetValue (app, _ =>
        {
            ITimeProvider timeProvider = app.GetTimeProvider ();
            Drivers.IInputProcessor processor = app.Driver!.GetInputProcessor ();

            return new InputInjector (processor, timeProvider);
        });
    }

    /// <summary>
    /// Gets the time provider for this application.
    /// </summary>
    /// <param name="app">The application instance.</param>
    /// <returns>The time provider (defaults to SystemTimeProvider if not set).</returns>
    public static ITimeProvider GetTimeProvider (this IApplication app)
    {
        // For now, return a system time provider
        // This will be updated when we integrate with Application.Create()
        return new SystemTimeProvider ();
    }

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
