#nullable enable
namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
public class FakeDriverFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="FakeDriverV2"/> using default options
    /// </summary>
    /// <returns></returns>
    public IFakeDriverV2 Create ()
    {
        return new FakeDriverV2 (
                                 new (),
                                 new (),
                                 new (),
                                 () => DateTime.Now,
                                 new ());
    }
}
