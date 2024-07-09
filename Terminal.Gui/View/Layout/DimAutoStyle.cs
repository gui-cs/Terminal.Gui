using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui;

/// <summary>
///     Specifies how <see cref="Dim.Auto"/> will compute the dimension.
/// </summary>
[Flags]
[GenerateEnumExtensionMethods (FastHasFlags = true)]
public enum DimAutoStyle
{
    /// <summary>
    ///     The dimensions will be computed based on the View's <see cref="View.GetContentSize ()"/> and/or <see cref="View.Subviews"/>.
    ///     <para>
    ///         If <see cref="View.ContentSizeTracksViewport"/> is <see langword="true"/>, <see cref="View.GetContentSize ()"/> will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         Otherwise, the Subview in <see cref="View.Subviews"/> with the largest corresponding position plus dimension
    ///         will determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/> will be ignored.
    ///     </para>
    /// </summary>
    Content = 1,

    /// <summary>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/>, formatted using the
    ///         <see cref="View.TextFormatter"/> settings,
    ///         will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimensions of <see cref="View.GetContentSize ()"/> and/or <see cref="View.Subviews"/> will be ignored.
    ///     </para>
    /// </summary>
    Text = 2,

    /// <summary>
    ///     The dimension will be computed using the largest of the view's <see cref="View.Text"/>, <see cref="View.GetContentSize ()"/>, and
    ///     <see cref="View.Subviews"/> corresponding dimension
    /// </summary>
    Auto = Content | Text,
}