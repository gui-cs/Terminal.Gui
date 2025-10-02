
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
    /// <param name="logWriter"></param>
    /// <returns></returns>
    public static GuiTestContext A<T> (int width, int height, V2TestDriver v2TestDriver, TextWriter? logWriter = null) where T : Toplevel, new ()
    {
        return new (() => new T (), width, height,v2TestDriver,logWriter);
    }

    /// <summary>
    /// Overload that takes a function to create instance <paramref name="toplevelFactory"/> after application is initialized.
    /// </summary>
    /// <param name="toplevelFactory"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="v2TestDriver"></param>
    /// <returns></returns>
    public static GuiTestContext A (Func<Toplevel> toplevelFactory, int width, int height, V2TestDriver v2TestDriver)
    {
        return new (toplevelFactory, width, height, v2TestDriver);
    }
    /// <summary>
    ///     The global timeout to allow for any given application to run for before shutting down.
    /// </summary>
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds (30);

    
}
