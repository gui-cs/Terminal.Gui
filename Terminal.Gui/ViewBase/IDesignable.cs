namespace Terminal.Gui.ViewBase;

/// <summary>
///     Interface declaring common functionality useful for designer implementations.
/// </summary>
public interface IDesignable
{
    /// <summary>
    ///     Causes the View to enable design-time mode. This typically means that the view will load demo data and
    ///     be configured to allow for design-time manipulation.
    /// </summary>
    /// <param name="targetView"></param>
    /// <typeparam name="TContext">A non-null type for <paramref name="targetView"/>.</typeparam>
    /// <returns><see langword="true"/> if the view successfully loaded demo data.</returns>
    public bool EnableForDesign<TContext> (ref TContext targetView) where TContext : notnull => EnableForDesign ();

    /// <summary>
    ///     Causes the View to enable design-time mode. This typically means that the view will load demo data and
    ///     be configured to allow for design-time manipulation.
    /// </summary>
    /// <returns><see langword="true"/> if the view successfully loaded demo data.</returns>
    public bool EnableForDesign () => false;

    /// <summary>
    ///     Returns a tuirec-format keystroke string for recording a demo GIF of this view.
    ///     The string uses tuirec token syntax (e.g. <c>"wait:500,Enter,wait:800,Escape"</c>).
    /// </summary>
    /// <returns>
    ///     A keystroke string for tuirec, or <see langword="null"/> if no demo interaction is defined
    ///     (the view will be recorded as a static 2-second capture).
    /// </returns>
    public string? GetDemoKeyStrokes () => null;
}
