namespace Terminal.Gui.Examples;

/// <summary>
///     Contains information about a discovered example application.
/// </summary>
public class ExampleInfo
{
    /// <summary>
    ///     Gets or sets the display name of the example.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a description of what the example demonstrates.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the full path to the example's assembly file.
    /// </summary>
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the list of categories this example belongs to.
    /// </summary>
    public List<string> Categories { get; set; } = new ();

    /// <summary>
    ///     Gets or sets the demo keystroke sequences defined for this example.
    /// </summary>
    public List<DemoKeyStrokeSequence> DemoKeyStrokes { get; set; } = new ();

    /// <summary>
    ///     Returns a string representation of this example info.
    /// </summary>
    /// <returns>A string containing the name and description.</returns>
    public override string ToString ()
    {
        return $"{Name}: {Description}";
    }
}
