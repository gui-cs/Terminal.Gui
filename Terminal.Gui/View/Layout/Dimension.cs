using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui;

/// <summary>
///     Indicates the dimension for <see cref="Dim"/> operations.
/// </summary>

[GenerateEnumExtensionMethods]
public enum Dimension
{
    /// <summary>
    ///     No dimension specified.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The height dimension.
    /// </summary>
    Height = 1,

    /// <summary>
    ///     The width dimension.
    /// </summary>
    Width = 2
}