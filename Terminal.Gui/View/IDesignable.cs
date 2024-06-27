namespace Terminal.Gui;

/// <summary>
///     Interface declaring common functionality useful for designer implementations.
/// </summary>
public interface IDesignable
{
    /// <summary>
    ///     Causes the View to load demo data.
    /// </summary>
    /// <param name="context">Optional arbitrary, View-specific, context.</param>
    /// <typeparam name="TContext">A non-null type for <paramref name="context"/>.</typeparam>
    /// <returns><see langword="true"/> if the view successfully loaded demo data.</returns>
    public bool LoadDemoData<TContext> (in TContext context) where TContext : notnull => LoadDemoData ();

    /// <summary>
    ///     Causes the View to load demo data.
    /// </summary>
    /// <returns><see langword="true"/> if the view successfully loaded demo data.</returns>
    public bool LoadDemoData () => false;
}
