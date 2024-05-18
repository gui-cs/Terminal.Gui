
namespace Terminal.Gui.PosDimTests;

public class PosAlignTests ()
{
    [Fact]
    public void PosAlign_Constructor ()
    {
        var posAlign = new PosAlign (Alignment.Fill);
        Assert.NotNull (posAlign);
    }

    [Theory]
    [InlineData (Alignment.Start, Alignment.Start, true)]
    [InlineData (Alignment.Center, Alignment.Center, true)]
    [InlineData (Alignment.Start, Alignment.Center, false)]
    [InlineData (Alignment.Center, Alignment.Start, false)]
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
        var posAlign = new PosAlign (Alignment.Fill);
        var expectedString = "Align(groupId=0, alignment=Fill)";

        Assert.Equal (expectedString, posAlign.ToString ());
    }

    [Fact]
    public void PosAlign_Anchor ()
    {
        var posAlign = new PosAlign (Alignment.Start);
        var width = 50;
        var expectedAnchor = -width;

        Assert.Equal (expectedAnchor, posAlign.GetAnchor (width));
    }

    [Fact]
    public void PosAlign_CreatesCorrectInstance ()
    {
        var pos = Pos.Align (Alignment.Start);
        Assert.IsType<PosAlign> (pos);
    }

    // Tests that test Left alignment

    // 
}
