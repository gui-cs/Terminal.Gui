namespace Terminal.Gui.Testing;

/// <summary>
///     Extension methods for input injection. See <see cref="InputInjector"/> for details.
/// </summary>
public static class InputInjectionExtensions
{
    /// <param name="app">The application instance.</param>
    extension (IApplication app)
    {
        /// <summary>
        ///     Injects a key event (convenience method).
        /// </summary>
        /// <param name="key">The key to inject.</param>
        public void InjectKey (Key key) { app.GetInputInjector ().InjectKey (key); }

        /// <summary>
        ///     Injects a mouse event (convenience method).
        /// </summary>
        /// <param name="mouseEvent">The mouse event to inject.</param>
        public void InjectMouse (Mouse mouseEvent) { app.GetInputInjector ().InjectMouse (mouseEvent); }

        /// <summary>
        ///     Injects a sequence of events (convenience method).
        /// </summary>
        /// <param name="events">The events to inject.</param>
        public void InjectSequence (params InputInjectionEvent [] events) { app.GetInputInjector ().InjectSequence (events); }
    }
}
