using System.Text;
using UnitTests;

namespace Terminal.Gui.ViewsTests;

public class ProgressBarTests
{
    [Fact]
    [AutoInitShutdown]
    public void Default_Constructor ()
    {
        var pb = new ProgressBar ();
        pb.BeginInit ();
        pb.EndInit ();

        Assert.False (pb.CanFocus);
        Assert.Equal (0, pb.Fraction);

        Assert.Equal (1, pb.Frame.Height);
        Assert.Equal (ProgressBarStyle.Blocks, pb.ProgressBarStyle);
        Assert.Equal (ProgressBarFormat.Simple, pb.ProgressBarFormat);
        Assert.Equal (Glyphs.BlocksMeterSegment, pb.SegmentCharacter);
    }

    [Fact]
    [AutoInitShutdown]
    public void Fraction_Redraw ()
    {
        var driver = (FakeDriver)Application.Driver;

        var pb = new ProgressBar { Width = 5 };

        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        for (var i = 0; i <= pb.Frame.Width; i++)
        {
            pb.Fraction += 0.2F;
            View.SetClipToScreen ();
            pb.Draw ();

            if (i == 0)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
            }
            else if (i == 1)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
            }
            else if (i == 2)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
            }
            else if (i == 3)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
            }
            else if (i == 4)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
            }
            else if (i == 5)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void ProgressBarFormat_MarqueeBlocks_MarqueeContinuous_Setter ()
    {
        var driver = (FakeDriver)Application.Driver;

        var pb1 = new ProgressBar { ProgressBarStyle = ProgressBarStyle.MarqueeBlocks };
        var pb2 = new ProgressBar { ProgressBarStyle = ProgressBarStyle.MarqueeContinuous };

        pb1.ProgressBarFormat = ProgressBarFormat.Simple;
        pb1.Layout ();
        Assert.Equal (ProgressBarFormat.Simple, pb1.ProgressBarFormat);
        Assert.Equal (1, pb1.Frame.Height);
        pb2.ProgressBarFormat = ProgressBarFormat.Simple;
        pb2.Layout ();
        Assert.Equal (ProgressBarFormat.Simple, pb2.ProgressBarFormat);
        Assert.Equal (1, pb2.Frame.Height);

        pb1.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
        pb1.Layout ();
        Assert.Equal (ProgressBarFormat.SimplePlusPercentage, pb1.ProgressBarFormat);
        Assert.Equal (1, pb1.Frame.Height);
        pb2.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
        pb2.Layout ();
        Assert.Equal (ProgressBarFormat.SimplePlusPercentage, pb2.ProgressBarFormat);
        Assert.Equal (1, pb2.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void ProgressBarFormat_Setter ()
    {
        var pb = new ProgressBar ();

        pb.ProgressBarFormat = ProgressBarFormat.Simple;
        pb.Layout ();
        Assert.Equal (1, pb.Frame.Height);

        pb.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
        pb.Layout ();
        Assert.Equal (1, pb.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void ProgressBarStyle_Setter ()
    {
        var driver = (FakeDriver)Application.Driver;

        var pb = new ProgressBar ();

        pb.ProgressBarStyle = ProgressBarStyle.Blocks;
        Assert.Equal (Glyphs.BlocksMeterSegment, pb.SegmentCharacter);

        pb.ProgressBarStyle = ProgressBarStyle.Continuous;
        Assert.Equal (Glyphs.ContinuousMeterSegment, pb.SegmentCharacter);

        pb.ProgressBarStyle = ProgressBarStyle.MarqueeBlocks;
        Assert.Equal (Glyphs.BlocksMeterSegment, pb.SegmentCharacter);

        pb.ProgressBarStyle = ProgressBarStyle.MarqueeContinuous;
        Assert.Equal (Glyphs.ContinuousMeterSegment, pb.SegmentCharacter);
    }

    [Fact]
    [AutoInitShutdown]
    public void Pulse_Redraw_BidirectionalMarquee_False ()
    {
        var driver = (FakeDriver)Application.Driver;

        var pb = new ProgressBar
        {
            Width = 15, ProgressBarStyle = ProgressBarStyle.MarqueeBlocks, BidirectionalMarquee = false
        };

        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        for (var i = 0; i < 38; i++)
        {
            pb.Pulse ();
            View.SetClipToScreen ();
            pb.Draw ();

            if (i == 0)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 1)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 2)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 3)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 4)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 5)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 6)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 7)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 8)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 9)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 10)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 11)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 12)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 13)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 14)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 15)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 16)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 17)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 18)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 19)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 20)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 21)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 22)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 23)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 24)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 25)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 26)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 27)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 28)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 29)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 30)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 31)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 32)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 33)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 34)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 35)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 36)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 37)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Pulse_Redraw_BidirectionalMarquee_True_Default ()
    {
        var driver = (FakeDriver)Application.Driver;

        var pb = new ProgressBar { Width = 15, ProgressBarStyle = ProgressBarStyle.MarqueeBlocks };

        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        for (var i = 0; i < 38; i++)
        {
            pb.Pulse ();
            View.SetClipToScreen ();
            pb.Draw ();

            if (i == 0)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 1)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 2)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 3)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 4)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 5)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 6)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 7)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 8)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 9)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 10)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 11)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 12)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 13)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 14)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 15)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 16)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 17)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 18)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 19)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 20)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 21)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 22)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 14].Rune);
            }
            else if (i == 23)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 24)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 25)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 26)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 27)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 28)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 29)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 30)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 31)
            {
                Assert.Equal ((Rune)' ', driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 32)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 33)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 34)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 35)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 36)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
            else if (i == 37)
            {
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 0].Rune);
                Assert.Equal (Glyphs.BlocksMeterSegment, driver.Contents [0, 1].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 2].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 3].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 5].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 6].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 7].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 8].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 9].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 10].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 11].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 12].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 13].Rune);
                Assert.Equal ((Rune)' ', driver.Contents [0, 14].Rune);
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Text_Setter_Marquee ()
    {
        var pb = new ProgressBar { Fraction = 0.25F, ProgressBarStyle = ProgressBarStyle.MarqueeBlocks };

        pb.ProgressBarFormat = ProgressBarFormat.Simple;
        pb.Text = "blabla";
        Assert.Equal ("blabla", pb.Text);

        pb.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
        pb.Text = "bleble";
        Assert.Equal ("bleble", pb.Text);
    }

    [Fact]
    [AutoInitShutdown]
    public void Text_Setter_Not_Marquee ()
    {
        var pb = new ProgressBar { Fraction = 0.25F };

        pb.ProgressBarFormat = ProgressBarFormat.Simple;
        pb.Text = "blabla";
        Assert.Equal ("25%", pb.Text);

        pb.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
        pb.Text = "bleble";
        Assert.Equal ("25%", pb.Text);
    }
}
