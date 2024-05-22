using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui;

/// <summary>
///     Describes whether an operation should add or subtract values.
/// </summary>
[GenerateEnumExtensionMethods]
public enum AddOrSubtract
{
    /// <summary>
    ///     The operation should use addition.
    /// </summary>
    Add = 0,

    /// <summary>
    ///     The operation should use subtraction.
    /// </summary>
    Subtract = 1
}
