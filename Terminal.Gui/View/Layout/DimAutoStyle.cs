using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui;

/// <summary>
///     Specifies how <see cref="Dim.Auto"/> will compute the dimension.
/// </summary>
[GenerateEnumExtensionMethods]
[Flags]
public enum DimAutoStyle
{
    /// <summary>
    ///     The dimensions will be computed based on the View's non-Text content.
    ///     <para>
    ///         If <see cref="View.ContentSize"/> is explicitly set (is not <see langword="null"/>) then
    ///         <see cref="View.ContentSize"/>
    ///         will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         Otherwise, the Subview in <see cref="View.Subviews"/> with the largest corresponding position plus dimension
    ///         will determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/> will be ignored.
    ///     </para>
    /// </summary>
    Content = 0,

    /// <summary>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/>, formatted using the
    ///         <see cref="View.TextFormatter"/> settings,
    ///         will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimensions of the <see cref="View.Subviews"/> will be ignored.
    ///     </para>
    /// </summary>
    Text = 1,

    /// <summary>
    ///     The dimension will be computed using both the view's <see cref="View.Text"/> and
    ///     <see cref="View.Subviews"/> (whichever is larger).
    /// </summary>
    Auto = Content | Text,
}