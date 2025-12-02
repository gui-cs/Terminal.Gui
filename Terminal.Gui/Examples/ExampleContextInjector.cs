namespace Terminal.Gui.Examples;

/// <summary>
///     Handles automatic injection of test context into running examples.
///     This class monitors for the presence of an <see cref="ExampleContext"/> in the environment
///     and automatically injects keystrokes via <see cref="Application.Driver"/> after the application initializes.
/// </summary>
public static class ExampleContextInjector
{
    private static bool _initialized;

    /// <summary>
    ///     Sets up automatic key injection if a test context is present in the environment.
    ///     Call this method before calling <see cref="Application.Init"/> or <see cref="IApplication.Init"/>.
    /// </summary>
    /// <param name="app"></param>
    /// <remarks>
    ///     This method is safe to call multiple times - it will only set up injection once.
    ///     The actual key injection happens after the application is initialized, via the
    ///     <see cref="Application.InitializedChanged"/> event.
    /// </remarks>
    public static void SetupAutomaticInjection (IApplication? app)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        // Check for test context in environment variable
        string? contextJson = Environment.GetEnvironmentVariable (ExampleContext.ENVIRONMENT_VARIABLE_NAME);

        if (string.IsNullOrEmpty (contextJson))
        {
            return;
        }

        ExampleContext? context = ExampleContext.FromJson (contextJson);

        if (context is null || context.KeysToInject.Count == 0)
        {
            return;
        }

        // Subscribe to InitializedChanged to inject keys after initialization
        app.SessionBegun += AppOnSessionBegun;

        return;

        void AppOnSessionBegun (object? sender, SessionTokenEventArgs e) 
        {

            // Application has been initialized, inject the keys
            if (app.Driver is null)
            {
                return;
            }

            foreach (string keyStr in context.KeysToInject)
            {
                if (Input.Key.TryParse (keyStr, out Input.Key? key) && key is { })
                {
                    app.Keyboard.RaiseKeyDownEvent (key);
                }
            }

            // Unsubscribe after injecting keys once
            app.SessionBegun -= AppOnSessionBegun;
        }
    }
}
