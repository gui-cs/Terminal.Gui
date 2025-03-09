namespace Terminal.Gui;

/// <summary>
///     Interface for main Terminal.Gui loop manager in v2.
/// </summary>
public interface IMainLoopCoordinator
{
    /// <summary>
    ///     Create all required subcomponents and boot strap.
    /// </summary>
    /// <returns></returns>
    public Task StartAsync ();

    /// <summary>
    ///     Stops the input thread, blocking till it exits.
    ///     Call this method only from the main UI loop.
    /// </summary>
    public void Stop ();

    /// <summary>
    ///     Run a single iteration of the main UI loop
    /// </summary>
    void RunIteration ();
}
