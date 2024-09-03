namespace Terminal.Gui.LayoutTests;

public class PosAlignTests
{
    [Fact]
    public void PosAlign_Constructor ()
    {
        var posAlign = new PosAlign
        {
            Aligner = new ()
        };
        Assert.NotNull (posAlign);

        Assert.Equal (Alignment.Start, posAlign.Aligner.Alignment);
        Assert.Equal (AlignmentModes.StartToEnd, posAlign.Aligner.AlignmentModes);
        Assert.Equal (0, posAlign.Aligner.ContainerSize);
    }

    [Fact]
    public void PosAlign_StaticFactory_Defaults ()
    {
        var posAlign = Pos.Align (Alignment.Start) as PosAlign;
        Assert.NotNull (posAlign);

        Assert.Equal (Alignment.Start, posAlign.Aligner.Alignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, posAlign.Aligner.AlignmentModes);
        Assert.Equal (0, posAlign.Aligner.ContainerSize);
    }

    //[Theory]
    //[InlineData (Alignment.Start, Alignment.Start, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, true)]
    //[InlineData (Alignment.Center, Alignment.Center, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, true)]
    //[InlineData (Alignment.Start, Alignment.Center, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, false)]
    //[InlineData (Alignment.Center, Alignment.Start, AlignmentModes.AddSpaceBetweenItems, AlignmentModes.AddSpaceBetweenItems, false)]
    //[InlineData (Alignment.Start, Alignment.Start, AlignmentModes.StartToEnd, AlignmentModes.AddSpaceBetweenItems, false)]
    //public void PosAlign_Equals (Alignment align1, Alignment align2, AlignmentModes mode1, AlignmentModes mode2, bool expectedEquals)
    //{
    //    var posAlign1 = new PosAlign
    //    {
    //        Aligner = new ()
    //        {
    //            Alignment = align1,
    //            AlignmentModes = mode1
    //        }
    //    };

    //    var posAlign2 = new PosAlign
    //    {
    //        Aligner = new ()
    //        {
    //            Alignment = align2,
    //            AlignmentModes = mode2
    //        }
    //    };

    //    Assert.Equal (expectedEquals, posAlign1.Equals (posAlign2));
    //    Assert.Equal (expectedEquals, posAlign2.Equals (posAlign1));
    //}

    //[Fact]
    //public void PosAlign_Equals_CachedLocation_Not_Used ()
    //{
    //    View superView = new ()
    //    {
    //        Width = 10,
    //        Height = 25
    //    };
    //    View view = new ();
    //    superView.Add (view);

    //    Pos posAlign1 = Pos.Align (Alignment.Center);
    //    view.X = posAlign1;
    //    int pos1 = posAlign1.Calculate (10, Dim.Absolute (0)!, view, Dimension.Width);

    //    Pos posAlign2 = Pos.Align (Alignment.Center);
    //    view.Y = posAlign2;
    //    int pos2 = posAlign2.Calculate (25, Dim.Absolute (0)!, view, Dimension.Height);

    //    Assert.NotEqual (pos1, pos2);
    //    Assert.Equal (posAlign1, posAlign2);
    //}

    [Fact]
    public void PosAlign_ToString ()
    {
        Pos posAlign = Pos.Align (Alignment.Fill);
        var expectedString = "Align(alignment=Fill,modes=AddSpaceBetweenItems,groupId=0)";

        Assert.Equal (expectedString, posAlign.ToString ());
    }

    [Fact]
    public void PosAlign_Anchor ()
    {
        Pos posAlign = Pos.Align (Alignment.Start);
        var width = 50;
        int expectedAnchor = -width;

        Assert.Equal (expectedAnchor, posAlign.GetAnchor (width));
    }

    [Fact]
    public void PosAlign_CreatesCorrectInstance ()
    {
        Pos pos = Pos.Align (Alignment.Start);
        Assert.IsType<PosAlign> (pos);
    }

    // TODO: Test scenarios where views with matching GroupId's are added/removed from a Superview

    // TODO: Make AlignAndUpdateGroup internal and write low-level unit tests for it
}
