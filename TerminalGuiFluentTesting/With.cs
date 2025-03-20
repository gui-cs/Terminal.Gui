using Terminal.Gui;

namespace TerminalGuiFluentTesting;

/// <summary>
///     Entry point to fluent assertions.
/// </summary>
public static class With
{
    /// <summary>
    ///     Entrypoint to fluent assertions
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="v2TestDriver">Which v2 v2TestDriver to use for the test</param>
    /// <returns></returns>
    public static GuiTestContext A<T> (int width, int height, V2TestDriver v2TestDriver) where T : Toplevel, new ()
    {
        return new (() => new T (), width, height,v2TestDriver);
    }

    /// <summary>
    ///     The global timeout to allow for any given application to run for before shutting down.
    /// </summary>
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds (30);
}
