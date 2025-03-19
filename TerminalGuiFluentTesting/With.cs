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
    /// <returns></returns>
    public static GuiTestContext A<T> (int width, int height) where T : Toplevel, new () { return new (() => new T (), width, height); }

    /// <summary>
    ///     The global timeout to allow for any given application to run for before shutting down.
    /// </summary>
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds (30);
}
