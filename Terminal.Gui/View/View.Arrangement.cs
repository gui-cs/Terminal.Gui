namespace Terminal.Gui;

public partial class View
{
    /// <summary>
    ///    Gets or sets the user actions that are enabled for the view within it's <see cref="SuperView"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Sizing or moving a view is only possible if the <see cref="View"/> is part of a <see cref="SuperView"/> and
    ///     the relevant position and dimensions of the <see cref="View"/> are independent of other SubViews
    /// </para>
    /// </remarks>
    public ViewArrangement Arrangement { get; set; }
}
