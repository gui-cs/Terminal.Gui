// Copilot
#nullable enable
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="ProgressBar"/> behavior.
/// </summary>
public class ProgressBarTests : TestDriverBase
{
    [Fact]
    public void ProgressBarStyle_Setter_UpdatesSegmentCharacter ()
    {
        ProgressBar pb = new ();

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
    public void Text_Setter_NotMarqueeStyle_ShowsPercentage ()
    {
        // In non-marquee mode, Text always returns the formatted percentage.
        ProgressBar pb = new () { Fraction = 0.25F };
        pb.Text = "ignored";

        Assert.Equal ("25%", pb.Text);
    }

    [Fact]
    public void Fraction_Half_Renders_HalfFilledBar ()
    {
        // Width=4, Fraction=0.5 → mid = (int)(0.5*4) = 2 → blocks at cols 0,1; spaces at cols 2,3
        IDriver driver = CreateTestDriver (4, 1);
        driver.Clip = new Region (driver.Screen);

        ProgressBar pb = new ()
        {
            Driver = driver,
            Width = 4,
            Fraction = 0.5F
        };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();

        string block = Glyphs.BlocksMeterSegment.ToString ();
        Assert.Equal (block, driver.Contents! [0, 0].Grapheme);
        Assert.Equal (block, driver.Contents [0, 1].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 2].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 3].Grapheme);
    }

    [Fact]
    public void Pulse_FirstCall_DrawsMarkerAtStartPosition ()
    {
        // Width=5 → _activityPos.Length = Math.Min(5/3=1, 5) = 1, initialised at pos 0.
        // First Pulse sets activity mode; the single marker is at column 0.
        IDriver driver = CreateTestDriver (5, 1);
        driver.Clip = new Region (driver.Screen);

        ProgressBar pb = new ()
        {
            Driver = driver,
            Width = 5,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Pulse ();
        pb.Draw ();

        string block = Glyphs.BlocksMeterSegment.ToString ();
        Assert.Equal (block, driver.Contents! [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 2].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 3].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 4].Grapheme);
    }

    [Fact]
    public void UseProgressIndicator_Fraction_Writes_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true };

        pb.Fraction = 0.5F;

        Assert.Contains (EscSeqUtils.OSC_SetProgressValue (50), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void UseProgressIndicator_Pulse_Writes_Indeterminate_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver (5, 1);
        driver.ProgressIndicator = new ProgressIndicator (driver);
        driver.Clip = new Region (driver.Screen);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true, Width = 5 };

        pb.BeginInit ();
        pb.EndInit ();
        pb.Frame = new Rectangle (0, 0, 5, 1);
        pb.LayoutSubViews ();

        pb.Pulse ();

        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void UseProgressIndicator_Hidden_ProgressBar_Still_Writes_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true, Visible = false };

        pb.Fraction = 0.25F;

        Assert.Contains (EscSeqUtils.OSC_SetProgressValue (25), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void UseProgressIndicator_LegacyConsole_Does_Not_Write_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        driver.IsLegacyConsole = true;
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true };

        pb.Fraction = 0.5F;

        Assert.DoesNotContain (EscSeqUtils.OSC_SetProgressValue (50), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void UseProgressIndicator_Disabling_Clears_Terminal_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true };

        pb.Fraction = 0.5F;
        pb.SyncWithTerminal = false;

        Assert.Contains (EscSeqUtils.OSC_ClearProgress (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void Dispose_Without_UseProgressIndicator_Does_Not_Write_Clear_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        ProgressBar pb = new () { Driver = driver };

        pb.Dispose ();

        Assert.DoesNotContain (EscSeqUtils.OSC_ClearProgress (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }
}
