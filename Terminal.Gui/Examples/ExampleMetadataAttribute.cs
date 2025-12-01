namespace Terminal.Gui.Examples;

/// <summary>
///     Defines metadata (Name and Description) for an example application.
///     Apply this attribute to an assembly to mark it as an example that can be discovered and run.
/// </summary>
/// <remarks>
///     <para>
///         This attribute is used by the example discovery system to identify and describe standalone example programs.
///         Each example should have exactly one <see cref="ExampleMetadataAttribute"/> applied to its assembly.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     [assembly: ExampleMetadata("Character Map", "Unicode character viewer and selector")]
///     </code>
/// </example>
[AttributeUsage (AttributeTargets.Assembly)]
public class ExampleMetadataAttribute : System.Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExampleMetadataAttribute"/> class.
    /// </summary>
    /// <param name="name">The display name of the example.</param>
    /// <param name="description">A brief description of what the example demonstrates.</param>
    public ExampleMetadataAttribute (string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    ///     Gets or sets the display name of the example.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets a brief description of what the example demonstrates.
    /// </summary>
    public string Description { get; set; }
}
