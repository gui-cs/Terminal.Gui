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
    /// <param name="context">Optional arbitrary, View-specific, context.</param>
    /// <typeparam name="TContext">A non-null type for <paramref name="context"/>.</typeparam>
    /// <returns><see langword="true"/> if the view successfully loaded demo data.</returns>
    public bool EnableForDesign<TContext> (ref TContext context) where TContext : notnull => EnableForDesign ();

    /// <summary>
    ///     Causes the View to enable design-time mode. This typically means that the view will load demo data and
    ///     be configured to allow for design-time manipulation.
    /// </summary>
    /// <returns><see langword="true"/> if the view successfully loaded demo data.</returns>
    public bool EnableForDesign () => false;
}
