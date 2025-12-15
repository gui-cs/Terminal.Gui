using System.Buffers;
using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace DriverTests.Output;

public class AddRuneTests (ITestOutputHelper output) : FakeDriverBase
{
    [Fact]
    public void AddRune ()
    {
        IDriver driver = CreateFakeDriver ();

        driver.Rows = 25;
        driver.Cols = 80;
        driver.AddRune (new Rune ('a'));
        Assert.Equal ("a", driver.Contents? [0, 0].Grapheme);

        driver.Dispose ();
    }

    [Fact]
    public void AddRune_Accented_Letter_With_Three_Combining_Unicode_Chars ()
    {
        IDriver driver = CreateFakeDriver ();

        var expected = "ắ";

        var text = "\u1eaf";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents! [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);

        driver.ClearContents ();
        driver.Move (0, 0);

        expected = "ắ";
        text = "\u0103\u0301";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);

        driver.ClearContents ();
        driver.Move (0, 0);

        expected = "ắ";
        text = "\u0061\u0306\u0301";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);

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
        driver.Dispose ();
    }

    [Fact]
    public void AddRune_InvalidLocation_DoesNothing ()
    {
        IDriver driver = CreateFakeDriver ();

        driver.Move (driver.Cols, driver.Rows);
        driver.AddRune ('a');

        for (var col = 0; col < driver.Cols; col++)
        {
            for (var row = 0; row < driver.Rows; row++)
            {
                Assert.Equal (" ", driver.Contents? [row, col].Grapheme);
            }
        }

        driver.Dispose ();
    }

    [Fact]
    public void AddRune_MovesToNextColumn ()
    {
        IDriver driver = CreateFakeDriver ();

        driver.AddRune ('a');
        Assert.Equal ("a", driver.Contents? [0, 0].Grapheme);
        Assert.Equal (0, driver.Row);
        Assert.Equal (1, driver.Col);

        driver.AddRune ('b');
        Assert.Equal ("b", driver.Contents? [0, 1].Grapheme);
        Assert.Equal (0, driver.Row);
        Assert.Equal (2, driver.Col);

        // Move to the last column of the first row
        int lastCol = driver.Cols - 1;
        driver.Move (lastCol, 0);
        Assert.Equal (0, driver.Row);
        Assert.Equal (lastCol, driver.Col);

        // Add a rune to the last column of the first row; should increment the row or col even though it's now invalid
        driver.AddRune ('c');
        Assert.Equal ("c", driver.Contents? [0, lastCol].Grapheme);
        Assert.Equal (lastCol + 1, driver.Col);

        // Add a rune; should succeed but do nothing as it's outside of Contents
        driver.AddRune ('d');
        Assert.Equal (lastCol + 2, driver.Col);

        for (var col = 0; col < driver.Cols; col++)
        {
            for (var row = 0; row < driver.Rows; row++)
            {
                Assert.NotEqual ("d", driver.Contents? [row, col].Grapheme);
            }
        }

        driver.Dispose ();
    }

    [Fact]
    public void AddRune_MovesToNextColumn_Wide ()
    {
        IDriver driver = CreateFakeDriver ();

        // 🍕 Slice of Pizza "\U0001F355"
        OperationStatus operationStatus = Rune.DecodeFromUtf16 ("\U0001F355", out Rune rune, out int charsConsumed);
        Assert.Equal (OperationStatus.Done, operationStatus);
        Assert.Equal (charsConsumed, rune.Utf16SequenceLength);
        Assert.Equal (2, rune.GetColumns ());

        driver.AddRune (rune);
        Assert.Equal (rune.ToString (), driver.Contents? [0, 0].Grapheme);
        Assert.Equal (0, driver.Row);
        Assert.Equal (2, driver.Col);

        //driver.AddRune ('b');
        //Assert.Equal ((Text)'b', driver.Contents [0, 1].Text);
        //Assert.Equal (0, driver.Row);
        //Assert.Equal (2, driver.Col);

        //// Move to the last column of the first row
        //var lastCol = driver.Cols - 1;
        //driver.Move (lastCol, 0);
        //Assert.Equal (0, driver.Row);
        //Assert.Equal (lastCol, driver.Col);

        //// Add a rune to the last column of the first row; should increment the row or col even though it's now invalid
        //driver.AddRune ('c');
        //Assert.Equal ((Text)'c', driver.Contents [0, lastCol].Text);
        //Assert.Equal (lastCol + 1, driver.Col);

        //// Add a rune; should succeed but do nothing as it's outside of Contents
        //driver.AddRune ('d');
        //Assert.Equal (lastCol + 2, driver.Col);
        //for (var col = 0; col < driver.Cols; col++) {
        //	for (var row = 0; row < driver.Rows; row++) {
        //		Assert.NotEqual ((Text)'d', driver.Contents [row, col].Text);
        //	}
        //}

        driver.Dispose ();
    }

    [Fact]
    public void AddStr_Glyph_On_Second_Cell_Of_Wide_Glyph_Outputs_Correctly ()
    {
        IDriver? driver = CreateFakeDriver ();
        driver.SetScreenSize (6, 3);
        driver.GetOutputBuffer ().SetWideGlyphReplacement ((Rune)'①');

        driver!.Clip = new (driver.Screen);
        driver.Move (1, 0);
        driver.AddStr ("┌");
        driver.Move (2, 0);
        driver.AddStr ("─");
        driver.Move (3, 0);
        driver.AddStr ("┐");
        driver.Clip.Exclude (new Region (new (1, 0, 3, 1)));

        driver.Move (0, 0);
        driver.AddStr ("🍎🍎🍎🍎");

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ①┌─┐🍎
                                              """,
                                              output,
                                              driver);

        driver.Refresh ();

        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;0;0;0m\x1b[48;2;0;0;0m①┌─┐🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m",
                                           output, driver);
    }
}
