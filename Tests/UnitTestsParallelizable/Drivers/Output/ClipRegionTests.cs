#nullable enable
using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace DriverTests.Output;

public class ClipRegionTests (ITestOutputHelper output) : FakeDriverBase
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void AddRune_Is_Clipped ()
    {
        IDriver? driver = CreateFakeDriver ();

        driver.Move (0, 0);
        driver.AddRune ('x');
        Assert.Equal ("x", driver.Contents! [0, 0].Grapheme);

        driver.Move (5, 5);
        driver.AddRune ('x');
        Assert.Equal ("x", driver.Contents [5, 5].Grapheme);

        // Clear the contents
        driver.FillRect (new Rectangle (0, 0, driver.Rows, driver.Cols), ' ');
        Assert.Equal (" ", driver.Contents [0, 0].Grapheme);

        // Setup the region with a single rectangle, fill screen with 'x'
        driver.Clip = new (new Rectangle (5, 5, 5, 5));
        driver.FillRect (new Rectangle (0, 0, driver.Rows, driver.Cols), 'x');
        Assert.Equal (" ", driver.Contents [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [4, 9].Grapheme);
        Assert.Equal ("x", driver.Contents [5, 5].Grapheme);
        Assert.Equal ("x", driver.Contents [9, 9].Grapheme);
        Assert.Equal (" ", driver.Contents [10, 10].Grapheme);
    }

    [Fact]
    public void Clip_Set_To_Empty_AllInvalid ()
    {
        IDriver? driver = CreateFakeDriver ();

        // Define a clip rectangle
        driver.Clip = new (Rectangle.Empty);

        // negative
        Assert.False (driver.IsValidLocation (null!, 4, 5));
        Assert.False (driver.IsValidLocation (null!, 5, 4));
        Assert.False (driver.IsValidLocation (null!, 10, 9));
        Assert.False (driver.IsValidLocation (null!, 9, 10));
        Assert.False (driver.IsValidLocation (null!, -1, 0));
        Assert.False (driver.IsValidLocation (null!, 0, -1));
        Assert.False (driver.IsValidLocation (null!, -1, -1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows));
    }

    [Fact]
    public void IsValidLocation ()
    {
        IDriver? driver = CreateFakeDriver ();
        driver.Rows = 10;
        driver.Cols = 10;

        // positive
        Assert.True (driver.IsValidLocation (null!, 0, 0));
        Assert.True (driver.IsValidLocation (null!, 1, 1));
        Assert.True (driver.IsValidLocation (null!, driver.Cols - 1, driver.Rows - 1));

        // negative
        Assert.False (driver.IsValidLocation (null!, -1, 0));
        Assert.False (driver.IsValidLocation (null!, 0, -1));
        Assert.False (driver.IsValidLocation (null!, -1, -1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows));

        // Define a clip rectangle
        driver.Clip = new (new Rectangle (5, 5, 5, 5));

        // positive
        Assert.True (driver.IsValidLocation (null!, 5, 5));
        Assert.True (driver.IsValidLocation (null!, 9, 9));

        // negative
        Assert.False (driver.IsValidLocation (null!, 4, 5));
        Assert.False (driver.IsValidLocation (null!, 5, 4));
        Assert.False (driver.IsValidLocation (null!, 10, 9));
        Assert.False (driver.IsValidLocation (null!, 9, 10));
        Assert.False (driver.IsValidLocation (null!, -1, 0));
        Assert.False (driver.IsValidLocation (null!, 0, -1));
        Assert.False (driver.IsValidLocation (null!, -1, -1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (null!, driver.Cols, driver.Rows));
    }
}
