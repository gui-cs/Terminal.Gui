using JetBrains.Annotations;

namespace Terminal.Gui.ViewsTests;

[TestSubject (typeof (Bar))]
public class BarTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var bar = new Bar ();

        Assert.NotNull (bar);
        Assert.True (bar.CanFocus);
        Assert.IsType<DimAuto> (bar.Width);
        Assert.IsType<DimAuto> (bar.Height);

        // TOOD: more
    }

}
