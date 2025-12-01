namespace Terminal.Gui.Examples;

/// <summary>
///     Defines a category for an example application.
///     Apply this attribute to an assembly to associate it with one or more categories for organization and filtering.
/// </summary>
/// <remarks>
///     <para>
///         Multiple instances of this attribute can be applied to a single assembly to associate the example
///         with multiple categories.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     [assembly: ExampleCategory("Text and Formatting")]
///     [assembly: ExampleCategory("Controls")]
///     </code>
/// </example>
[AttributeUsage (AttributeTargets.Assembly, AllowMultiple = true)]
public class ExampleCategoryAttribute : System.Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExampleCategoryAttribute"/> class.
    /// </summary>
    /// <param name="category">The category name.</param>
    public ExampleCategoryAttribute (string category)
    {
        Category = category;
    }

    /// <summary>
    ///     Gets or sets the category name.
    /// </summary>
    public string Category { get; set; }
}
