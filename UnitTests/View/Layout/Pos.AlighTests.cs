using Xunit.Abstractions;
using static Terminal.Gui.Dim;
using static Terminal.Gui.Pos;

namespace Terminal.Gui.PosDimTests;

public class PosAlignTests (ITestOutputHelper output)
{
    [Fact]
    public void PosAlign_Constructor ()
    {
        var PosAlign = new PosAlign (Alignment.Justified);
        Assert.NotNull (PosAlign);
    }

    [Theory]
    [InlineData (Alignment.Left, Alignment.Left, true)]
    [InlineData (Alignment.Centered, Alignment.Centered, true)]
    [InlineData (Alignment.Left, Alignment.Centered, false)]
    [InlineData (Alignment.Centered, Alignment.Left, false)]
    public void PosAlign_Equals (Alignment align1, Alignment align2, bool expectedEquals)
    {
        var posAlign1 = new PosAlign (align1);
        var posAlign2 = new PosAlign (align2);

        Assert.Equal (expectedEquals, posAlign1.Equals (posAlign2));
        Assert.Equal (expectedEquals, posAlign2.Equals (posAlign1));
    }

    [Fact]
    public void PosAlign_ToString ()
    {
        var PosAlign = new PosAlign (Alignment.Justified);
        var expectedString = "Align(groupId=0, alignment=Justified)";

        Assert.Equal (expectedString, PosAlign.ToString ());
    }

    [Fact]
    public void PosAlign_Anchor ()
    {
        var PosAlign = new PosAlign (Alignment.Left);
        var width = 50;
        var expectedAnchor = -width;

        Assert.Equal (expectedAnchor, PosAlign.Anchor (width));
    }

    [Fact]
    public void PosAlign_CreatesCorrectInstance ()
    {
        var pos = Pos.Align (Alignment.Left);
        Assert.IsType<PosAlign> (pos);
    }

    // Tests that test Left alignment

    // 
}
