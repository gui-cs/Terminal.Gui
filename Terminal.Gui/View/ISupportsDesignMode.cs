namespace Terminal.Gui;

/// <summary>
/// Indicates that the view supports design mode.
/// </summary>
public interface ISupportsDesignMode
{
    /// <summary>
    /// Call this to tell the View to load "demo data"
    /// </summary>
    /// <returns><see langword="true"/> if the view succesfully loaded demo data.</returns>
    public bool LoadDemoData ();
}
