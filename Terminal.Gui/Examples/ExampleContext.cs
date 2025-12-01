using System.Text.Json;

namespace Terminal.Gui.Examples;

/// <summary>
///     Defines the execution context for running an example application.
///     This context is used to configure how an example should be executed, including driver selection,
///     keystroke injection, timeouts, and metrics collection.
/// </summary>
public class ExampleContext
{
    /// <summary>
    ///     Gets or sets the name of the driver to use (e.g., "FakeDriver", "DotnetDriver").
    ///     If <see langword="null"/>, the default driver for the platform is used.
    /// </summary>
    public string? DriverName { get; set; }

    /// <summary>
    ///     Gets or sets the list of key names to inject into the example during execution.
    ///     Each string should be a valid key name that can be parsed by <see cref="Input.Key.TryParse"/>.
    /// </summary>
    public List<string> KeysToInject { get; set; } = [];

    /// <summary>
    ///     Gets or sets the maximum time in milliseconds to allow the example to run before forcibly terminating it.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    ///     Gets or sets the maximum number of iterations to allow before stopping the example.
    ///     If set to -1, no iteration limit is enforced.
    /// </summary>
    public int MaxIterations { get; set; } = -1;

    /// <summary>
    ///     Gets or sets a value indicating whether to collect and report performance metrics during execution.
    /// </summary>
    public bool CollectMetrics { get; set; } = false;

    /// <summary>
    ///     Gets or sets the execution mode for the example.
    /// </summary>
    public ExecutionMode Mode { get; set; } = ExecutionMode.OutOfProcess;

    /// <summary>
    ///     The name of the environment variable used to pass the serialized <see cref="ExampleContext"/>
    ///     to example applications.
    /// </summary>
    public const string ENVIRONMENT_VARIABLE_NAME = "TERMGUI_TEST_CONTEXT";

    /// <summary>
    ///     Serializes this context to a JSON string for passing via environment variables.
    /// </summary>
    /// <returns>A JSON string representation of this context.</returns>
    public string ToJson ()
    {
        return JsonSerializer.Serialize (this);
    }

    /// <summary>
    ///     Deserializes a <see cref="ExampleContext"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized context, or <see langword="null"/> if deserialization fails.</returns>
    public static ExampleContext? FromJson (string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ExampleContext> (json);
        }
        catch
        {
            return null;
        }
    }
}
