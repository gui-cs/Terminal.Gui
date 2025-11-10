using System.Text;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace UnitTests.DriverTests;

public class ClipRegionTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void AddRune_Is_Clipped ()
    {
        Application.Init (null, "fake");

        Application.Driver!.Move (0, 0);
        Application.Driver!.AddRune ('x');
        Assert.Equal ((Rune)'x', Application.Driver!.Contents! [0, 0].Rune);

        Application.Driver?.Move (5, 5);
        Application.Driver?.AddRune ('x');
        Assert.Equal ((Rune)'x', Application.Driver!.Contents [5, 5].Rune);

        // Clear the contents
        Application.Driver?.FillRect (new Rectangle (0, 0, Application.Driver.Rows, Application.Driver.Cols), ' ');
        Assert.Equal ((Rune)' ', Application.Driver?.Contents [0, 0].Rune);

        // Setup the region with a single rectangle, fill screen with 'x'
        Application.Driver!.Clip = new (new Rectangle (5, 5, 5, 5));
        Application.Driver.FillRect (new Rectangle (0, 0, Application.Driver.Rows, Application.Driver.Cols), 'x');
        Assert.Equal ((Rune)' ', Application.Driver?.Contents [0, 0].Rune);
        Assert.Equal ((Rune)' ', Application.Driver?.Contents [4, 9].Rune);
        Assert.Equal ((Rune)'x', Application.Driver?.Contents [5, 5].Rune);
        Assert.Equal ((Rune)'x', Application.Driver?.Contents [9, 9].Rune);
        Assert.Equal ((Rune)' ', Application.Driver?.Contents [10, 10].Rune);

        Application.Shutdown ();
    }

    [Fact]
    public void Clip_Set_To_Empty_AllInvalid ()
    {
        Application.Init (null, "fake");

        // Define a clip rectangle
        Application.Driver!.Clip = new (Rectangle.Empty);

        // negative
        Assert.False (Application.Driver.IsValidLocation (default, 4, 5));
        Assert.False (Application.Driver.IsValidLocation (default, 5, 4));
        Assert.False (Application.Driver.IsValidLocation (default, 10, 9));
        Assert.False (Application.Driver.IsValidLocation (default, 9, 10));
        Assert.False (Application.Driver.IsValidLocation (default, -1, 0));
        Assert.False (Application.Driver.IsValidLocation (default, 0, -1));
        Assert.False (Application.Driver.IsValidLocation (default, -1, -1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows - 1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows - 1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows));

        Application.Shutdown ();
    }

    [Fact]
    public void IsValidLocation ()
    {
        Application.Init (null, "fake");
        Application.Driver!.Rows = 10;
        Application.Driver!.Cols = 10;

        // positive
        Assert.True (Application.Driver.IsValidLocation (default, 0, 0));
        Assert.True (Application.Driver.IsValidLocation (default, 1, 1));
        Assert.True (Application.Driver.IsValidLocation (default, Application.Driver.Cols - 1, Application.Driver.Rows - 1));

        // negative
        Assert.False (Application.Driver.IsValidLocation (default, -1, 0));
        Assert.False (Application.Driver.IsValidLocation (default, 0, -1));
        Assert.False (Application.Driver.IsValidLocation (default, -1, -1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows - 1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows - 1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows));

        // Define a clip rectangle
        Application.Driver.Clip = new (new Rectangle (5, 5, 5, 5));

        // positive
        Assert.True (Application.Driver.IsValidLocation (default, 5, 5));
        Assert.True (Application.Driver.IsValidLocation (default, 9, 9));

        // negative
        Assert.False (Application.Driver.IsValidLocation (default, 4, 5));
        Assert.False (Application.Driver.IsValidLocation (default, 5, 4));
        Assert.False (Application.Driver.IsValidLocation (default, 10, 9));
        Assert.False (Application.Driver.IsValidLocation (default, 9, 10));
        Assert.False (Application.Driver.IsValidLocation (default, -1, 0));
        Assert.False (Application.Driver.IsValidLocation (default, 0, -1));
        Assert.False (Application.Driver.IsValidLocation (default, -1, -1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows - 1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows - 1));
        Assert.False (Application.Driver.IsValidLocation (default, Application.Driver.Cols, Application.Driver.Rows));

        Application.Shutdown ();
    }
}
