namespace Terminal.Gui;

/// <summary>
///     Indicates that the view supports design mode.
/// </summary>
public interface IDesignable
{
    /// <summary>
    ///     Causes the View to load demo data.
    /// </summary>
    /// <param name="ctx">Optional arbitrary, View-specific, context.</param>
    /// <returns><see langword="true"/> if the view succesfully loaded demo data.</returns>
    public bool LoadDemoData (object ctx = null);
}
