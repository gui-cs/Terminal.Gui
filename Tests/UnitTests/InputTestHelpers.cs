using System.Collections.Concurrent;

namespace UnitTests;

/// <summary>
///     Helper methods for testing input injection in unit tests.
///     Provides utilities to simulate the input thread and process queues without running the main loop.
/// </summary>
public static class InputTestHelpers
{
    /// <summary>
    ///     Simulates the input thread by manually draining InputImpl's internal queue
    ///     and moving items to the InputBuffer. This is needed because tests don't
    ///     start the actual input thread via Run().
    /// </summary>
    /// <typeparam name="TInputRecord">The input record type (e.g., char for ANSI driver)</typeparam>
    /// <param name="input">The input implementation (must extend InputImpl)</param>
    /// <param name="inputBuffer">The buffer to enqueue items to</param>
    public static void SimulateInputThread<TInputRecord> (
        this InputImpl<TInputRecord> input,
        ConcurrentQueue<TInputRecord> inputBuffer
    )
    {
        // Drain the test input queue and move to InputBuffer
        while (input.Peek ())
        {
            foreach (TInputRecord item in input.Read ())
            {
                inputBuffer.Enqueue (item);
            }
        }
    }

    /// <summary>
    ///     Simulates the input thread for an IApplication by accessing its driver's input processor.
    /// </summary>
    /// <param name="app">The application instance</param>
    public static void SimulateInputThread (this IApplication app)
    {
        IInputProcessor processor = app.Driver!.GetInputProcessor ();

        if (processor is not InputProcessorImpl<char> charProcessor)
        {
            return;
        }

        if (charProcessor.InputImpl is not AnsiInput ansiInput)
        {
            return;
        }

        ansiInput.SimulateInputThread (charProcessor.InputQueue);
    }

    /// <summary>
    ///     c
    ///     Processes the input queue with support for keys that may be held by the ANSI parser (like Esc).
    ///     The parser holds Esc for 50ms waiting to see if it's part of an escape sequence.
    /// </summary>
    /// <param name="processor">The input processor</param>
    /// <param name="maxAttempts">Maximum number of processing attempts with delays (default: 3)</param>
    public static void ProcessQueueWithEscapeHandling (this IInputProcessor processor, int maxAttempts = 3)
    {
        // First attempt - process immediately
        processor.ProcessQueue ();

        // For escape sequences, we may need to wait and process again
        // The parser holds escape for 50ms before releasing
        for (var attempt = 1; attempt < maxAttempts; attempt++)
        {
            Thread.Sleep (60); // Wait longer than the 50ms escape timeout
            processor.ProcessQueue (); // This should release any held escape keys
        }
    }

    /// <summary>
    ///     Processes the input queue with support for escape sequences for an IApplication.
    /// </summary>
    /// <param name="app">The application instance</param>
    /// <param name="maxAttempts">Maximum number of processing attempts with delays (default: 3)</param>
    public static void ProcessQueueWithEscapeHandling (this IApplication app, int maxAttempts = 3)
    {
        app.Driver!.GetInputProcessor ().ProcessQueueWithEscapeHandling (maxAttempts);
    }

    /// <summary>
    ///     Injects a key event and processes the input queue, simulating what happens during a main loop iteration.
    ///     Automatically handles special keys like Esc and Alt combinations that require escape sequence timeouts.
    /// </summary>
    /// <param name="app">The application instance</param>
    /// <param name="key">The key to inject</param>
    public static void InjectAndProcessKey (this IApplication app, Key key)
    {
        app.Driver!.InjectKeyEvent (key);

        // Simulate the input thread moving items from _testInput to InputBuffer
        app.SimulateInputThread ();

        // Process the queue (with special handling for Esc key and Alt combinations)
        if (key.KeyCode == KeyCode.Esc || key.IsAlt)
        {
            app.ProcessQueueWithEscapeHandling ();
        }
        else
        {
            app.Driver.GetInputProcessor ().ProcessQueue ();
        }
    }

    /// <summary>
    ///     Injects a key event and processes the input queue for a specific processor and input implementation.
    /// </summary>
    /// <typeparam name="TInputRecord">The input record type</typeparam>
    /// <param name="processor">The input processor</param>
    /// <param name="input">The input implementation (must extend InputImpl)</param>
    /// <param name="inputBuffer">The input buffer</param>
    /// <param name="key">The key to inject</param>
    public static void InjectAndProcessKey<TInputRecord> (
        this IInputProcessor processor,
        InputImpl<TInputRecord> input,
        ConcurrentQueue<TInputRecord> inputBuffer,
        Key key
    )
    {
        processor.InjectKeyDownEvent (key);
        input.SimulateInputThread (inputBuffer);

        // Process the queue (with special handling for Esc key and Alt combinations)
        if (key.KeyCode == KeyCode.Esc || key.IsAlt)
        {
            processor.ProcessQueueWithEscapeHandling ();
        }
        else
        {
            processor.ProcessQueue ();
        }
    }

    /// <summary>
    ///     Injects a mouse event and processes the input queue, simulating what happens during a main loop iteration.
    /// </summary>
    /// <param name="app">The application instance</param>
    /// <param name="mouse">The mouse event to inject</param>
    public static void InjectAndProcessMouse (this IApplication app, Mouse mouse)
    {
        app.Driver!.InjectMouseEvent (mouse);

        // Simulate the input thread moving items from _testInput to InputBuffer
        app.SimulateInputThread ();

        // Process the queue
        app.Driver.GetInputProcessor ().ProcessQueue ();
    }

    /// <summary>
    ///     Injects a mouse event and processes the input queue for a specific processor and input implementation.
    /// </summary>
    /// <typeparam name="TInputRecord">The input record type</typeparam>
    /// <param name="processor">The input processor</param>
    /// <param name="input">The input implementation (must extend InputImpl)</param>
    /// <param name="inputBuffer">The input buffer</param>
    /// <param name="mouse">The mouse event to inject</param>
    public static void InjectAndProcessMouse<TInputRecord> (
        this IInputProcessor processor,
        InputImpl<TInputRecord> input,
        ConcurrentQueue<TInputRecord> inputBuffer,
        Mouse mouse
    )
    {
        processor.InjectMouseEvent (null, mouse);
        input.SimulateInputThread (inputBuffer);
        processor.ProcessQueue ();
    }
}
