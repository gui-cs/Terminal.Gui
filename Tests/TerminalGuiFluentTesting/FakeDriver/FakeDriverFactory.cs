#nullable enable
namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
public class FakeDriverFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="FakeConsoleDriver"/> using default options
    /// </summary>
    /// <returns></returns>
    public IFakeConsoleDriver Create ()
    {
        return new FakeConsoleDriver (
                                 new (),
                                 new (),
                                 new (),
                                 () => DateTime.Now,
                                 new ());
    }
}
