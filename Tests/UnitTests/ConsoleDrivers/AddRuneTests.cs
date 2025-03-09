using System.Buffers;
using System.Text;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.DriverTests;

public class AddRuneTests
{
    private readonly ITestOutputHelper _output;

    public AddRuneTests (ITestOutputHelper output)
    {
        ConsoleDriver.RunningUnitTests = true;
        _output = output;
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void AddRune (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        driver.Init ();

        driver.Rows = 25;
        driver.Cols = 80;
        driver.Init ();
        driver.AddRune (new Rune ('a'));
        Assert.Equal ((Rune)'a', driver.Contents [0, 0].Rune);

        driver.End ();
    }

    [Fact]
    public void AddRune_Accented_Letter_With_Three_Combining_Unicode_Chars ()
    {
        var driver = new FakeDriver ();
        driver.Init ();

        var expected = new Rune ('ắ');

        var text = "\u1eaf";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents [0, 0].Rune);
        Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);

        driver.ClearContents ();
        driver.Move (0, 0);

        text = "\u0103\u0301";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents [0, 0].Rune);
        Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);

        driver.ClearContents ();
        driver.Move (0, 0);

        text = "\u0061\u0306\u0301";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents [0, 0].Rune);
        Assert.Equal ((Rune)' ', driver.Contents [0, 1].Rune);

        //		var s = "a\u0301\u0300\u0306";

        //		DriverAsserts.AssertDriverContentsWithFrameAre (@"
        //ắ", output);

        //		tf.Text = "\u1eaf";
        //		Application.Refresh ();
        //		DriverAsserts.AssertDriverContentsWithFrameAre (@"
        //ắ", output);

        //		tf.Text = "\u0103\u0301";
        //		Application.Refresh ();
        //		DriverAsserts.AssertDriverContentsWithFrameAre (@"
        //ắ", output);

        //		tf.Text = "\u0061\u0306\u0301";
        //		Application.Refresh ();
        //		DriverAsserts.AssertDriverContentsWithFrameAre (@"
        //ắ", output);
        driver.End ();
    }

    [Fact]
    public void AddRune_InvalidLocation_DoesNothing ()
    {
        var driver = new FakeDriver ();
        driver.Init ();

        driver.Move (driver.Cols, driver.Rows);
        driver.AddRune ('a');

        for (var col = 0; col < driver.Cols; col++)
        {
            for (var row = 0; row < driver.Rows; row++)
            {
                Assert.Equal ((Rune)' ', driver.Contents [row, col].Rune);
            }
        }

        driver.End ();
    }

    [Fact]
    public void AddRune_MovesToNextColumn ()
    {
        var driver = new FakeDriver ();
        driver.Init ();

        driver.AddRune ('a');
        Assert.Equal ((Rune)'a', driver.Contents [0, 0].Rune);
        Assert.Equal (0, driver.Row);
        Assert.Equal (1, driver.Col);

        driver.AddRune ('b');
        Assert.Equal ((Rune)'b', driver.Contents [0, 1].Rune);
        Assert.Equal (0, driver.Row);
        Assert.Equal (2, driver.Col);

        // Move to the last column of the first row
        int lastCol = driver.Cols - 1;
        driver.Move (lastCol, 0);
        Assert.Equal (0, driver.Row);
        Assert.Equal (lastCol, driver.Col);

        // Add a rune to the last column of the first row; should increment the row or col even though it's now invalid
        driver.AddRune ('c');
        Assert.Equal ((Rune)'c', driver.Contents [0, lastCol].Rune);
        Assert.Equal (lastCol + 1, driver.Col);

        // Add a rune; should succeed but do nothing as it's outside of Contents
        driver.AddRune ('d');
        Assert.Equal (lastCol + 2, driver.Col);

        for (var col = 0; col < driver.Cols; col++)
        {
            for (var row = 0; row < driver.Rows; row++)
            {
                Assert.NotEqual ((Rune)'d', driver.Contents [row, col].Rune);
            }
        }

        driver.End ();
    }

    [Fact]
    public void AddRune_MovesToNextColumn_Wide ()
    {
        var driver = new FakeDriver ();
        driver.Init ();

        // 🍕 Slice of Pizza "\U0001F355"
        OperationStatus operationStatus = Rune.DecodeFromUtf16 ("\U0001F355", out Rune rune, out int charsConsumed);
        Assert.Equal (OperationStatus.Done, operationStatus);
        Assert.Equal (charsConsumed, rune.Utf16SequenceLength);
        Assert.Equal (2, rune.GetColumns ());

        driver.AddRune (rune);
        Assert.Equal (rune, driver.Contents [0, 0].Rune);
        Assert.Equal (0, driver.Row);
        Assert.Equal (2, driver.Col);

        //driver.AddRune ('b');
        //Assert.Equal ((Rune)'b', driver.Contents [0, 1].Rune);
        //Assert.Equal (0, driver.Row);
        //Assert.Equal (2, driver.Col);

        //// Move to the last column of the first row
        //var lastCol = driver.Cols - 1;
        //driver.Move (lastCol, 0);
        //Assert.Equal (0, driver.Row);
        //Assert.Equal (lastCol, driver.Col);

        //// Add a rune to the last column of the first row; should increment the row or col even though it's now invalid
        //driver.AddRune ('c');
        //Assert.Equal ((Rune)'c', driver.Contents [0, lastCol].Rune);
        //Assert.Equal (lastCol + 1, driver.Col);

        //// Add a rune; should succeed but do nothing as it's outside of Contents
        //driver.AddRune ('d');
        //Assert.Equal (lastCol + 2, driver.Col);
        //for (var col = 0; col < driver.Cols; col++) {
        //	for (var row = 0; row < driver.Rows; row++) {
        //		Assert.NotEqual ((Rune)'d', driver.Contents [row, col].Rune);
        //	}
        //}

        driver.End ();
    }
}
