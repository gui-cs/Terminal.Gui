
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
    /// <param name="testDriver">Which v2 testDriver to use for the test</param>
    /// <param name="logWriter"></param>
    /// <returns></returns>
    public static GuiTestContext A<T> (int width, int height, TestDriver testDriver, TextWriter? logWriter = null) where T : IRunnable, new()
    {
        return new (() => new T ()
        {
            //Id = $"{typeof (T).Name}"
        }, width, height, testDriver, logWriter, Timeout);
    }

    /// <summary>
    /// Overload that takes a function to create instance <paramref name="toplevelFactory"/> after application is initialized.
    /// </summary>
    /// <param name="toplevelFactory"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="testDriver"></param>
    /// <param name="logWriter"></param>
    /// <returns></returns>
    public static GuiTestContext A (Func<IRunnable> toplevelFactory, int width, int height, TestDriver testDriver, TextWriter? logWriter = null)
    {
        return new (toplevelFactory, width, height, testDriver, logWriter, Timeout);
    }
    /// <summary>
    ///     The global timeout to allow for any given application to run for before shutting down.
    /// </summary>
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds (30);


}
