
using static Unix.Terminal.Delegates;

namespace Terminal.Gui.PosDimTests;

public class PosAlignTests ()
{
    [Fact]
    public void PosAlign_Constructor ()
    {
        var posAlign = new PosAlign ()
        {
            Aligner = new Aligner(),
        };
        Assert.NotNull (posAlign);
    }

    [Theory]
    [InlineData (Alignment.Start, Alignment.Start, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, true)]
    [InlineData (Alignment.Center, Alignment.Center, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, true)]
    [InlineData (Alignment.Start, Alignment.Center, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, false)]
    [InlineData (Alignment.Center, Alignment.Start, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, false)]
    [InlineData (Alignment.Start, Alignment.Start, AlignmentModes.StartToEnd, AlignmentModes.AddSpaceBetweenItems, false)]
    public void PosAlign_Equals (Alignment align1, Alignment align2, AlignmentModes mode1, AlignmentModes mode2, bool expectedEquals)
    {
        var posAlign1 = new PosAlign ()
        {
            Aligner = new Aligner ()
            {
                Alignment = align1,
                AlignmentModes = mode1
            }
        };
        var posAlign2 = new PosAlign ()
        {
            Aligner = new Aligner ()
            {
                Alignment = align2,
                AlignmentModes = mode2
            }
        };

        Assert.Equal (expectedEquals, posAlign1.Equals (posAlign2));
        Assert.Equal (expectedEquals, posAlign2.Equals (posAlign1));
    }

    [Fact]
    public void PosAlign_Equals_CachedLocation_Not_Used ()
    {
        View superView = new ()
        {
            Width = 10,
            Height = 25
        };
        View view = new ();
        superView.Add (view);

        var posAlign1 = Pos.Align (Alignment.Center, AlignmentModes.AddSpaceBetweenItems);
        view.X = posAlign1;
        var pos1 =  posAlign1.Calculate (10, Dim.Absolute(0)!, view, Dimension.Width);

        var posAlign2 = Pos.Align (Alignment.Center, AlignmentModes.AddSpaceBetweenItems);
        view.Y = posAlign2;
        var pos2 = posAlign2.Calculate (25, Dim.Absolute (0)!, view, Dimension.Height);

        Assert.NotEqual(pos1, pos2);
        Assert.Equal (posAlign1, posAlign2);
    }

    [Fact]
    public void PosAlign_ToString ()
    {
        var posAlign = Pos.Align (Alignment.Fill);
        var expectedString = "Align(alignment=Fill,modes=AddSpaceBetweenItems,groupId=0)";

        Assert.Equal (expectedString, posAlign.ToString ());
    }

    [Fact]
    public void PosAlign_Anchor ()
    {
        var posAlign = Pos.Align (Alignment.Start);
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

    // TODO: Test scenarios where views with matching GroupId's are added/removed from a Superview

    // TODO: Make AlignAndUpdateGroup internal and write low-level unit tests for it
}
