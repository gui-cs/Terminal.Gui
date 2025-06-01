
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
    /// Overload that takes an existing instance <paramref name="toplevel"/>
    /// instead of creating one.
    /// </summary>
    /// <param name="toplevel"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="v2TestDriver"></param>
    /// <returns></returns>
    public static GuiTestContext A (Toplevel toplevel, int width, int height, V2TestDriver v2TestDriver)
    {
        return new (()=>toplevel, width, height, v2TestDriver);
    }
    /// <summary>
    ///     The global timeout to allow for any given application to run for before shutting down.
    /// </summary>
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds (30);

    
}
