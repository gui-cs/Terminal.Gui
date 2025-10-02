using System.Text;
using UICatalog;
using UnitTests;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.TextTests;

public class TextFormatterTests
{
    public TextFormatterTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    public static IEnumerable<object []> CMGlyphs =>
        new List<object []> { new object [] { $"{Glyphs.LeftBracket} Say Hello 你 {Glyphs.RightBracket}", 16, 15 } };

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 0, "")]
    [InlineData ("A", 1, "A")]
    [InlineData ("A", 2, "A")]
    [InlineData ("A", 3, " A")]
    [InlineData ("AB", 1, "A")]
    [InlineData ("AB", 2, "AB")]
    [InlineData ("ABC", 3, "ABC")]
    [InlineData ("ABC", 4, "ABC")]
    [InlineData ("ABC", 5, " ABC")]
    [InlineData ("ABC", 6, " ABC")]
    [InlineData ("ABC", 9, "   ABC")]
    public void Draw_Horizontal_Centered (string text, int width, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.Center
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = 1;
        tf.Draw (new (0, 0, width, 1), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 0, "")]
    [InlineData ("A", 1, "A")]
    [InlineData ("A", 2, "A")]
    [InlineData ("A B", 3, "A B")]
    [InlineData ("A B", 1, "A")]
    [InlineData ("A B", 2, "A")]
    [InlineData ("A B", 4, "A  B")]
    [InlineData ("A B", 5, "A   B")]
    [InlineData ("A B", 6, "A    B")]
    [InlineData ("A B", 10, "A        B")]
    [InlineData ("ABC ABC", 10, "ABC    ABC")]
    public void Draw_Horizontal_Justified (string text, int width, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.Fill
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = 1;
        tf.Draw (new (0, 0, width, 1), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 0, "")]
    [InlineData ("A", 1, "A")]
    [InlineData ("A", 2, "A")]
    [InlineData ("AB", 1, "A")]
    [InlineData ("AB", 2, "AB")]
    [InlineData ("ABC", 3, "ABC")]
    [InlineData ("ABC", 4, "ABC")]
    [InlineData ("ABC", 6, "ABC")]
    public void Draw_Horizontal_Left (string text, int width, string expectedText)

    {
        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.Start
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = 1;
        tf.Draw (new (0, 0, width, 1), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 0, "")]
    [InlineData ("A", 1, "A")]
    [InlineData ("A", 2, " A")]
    [InlineData ("AB", 1, "B")]
    [InlineData ("AB", 2, "AB")]
    [InlineData ("ABC", 3, "ABC")]
    [InlineData ("ABC", 4, " ABC")]
    [InlineData ("ABC", 6, "   ABC")]
    public void Draw_Horizontal_Right (string text, int width, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.End
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = 1;

        tf.Draw (new (Point.Empty, new (width, 1)), Attribute.Default, Attribute.Default);
        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 2, 1, "2")]
    [InlineData ("AB12", 5, 1, "21BA")]
    [InlineData ("AB\n12", 5, 2, "21\nBA")]
    [InlineData ("ABC 123 456", 7, 2, "CBA    \n654 321")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 5, 1, "はち")]
    [InlineData ("こんにちは", 10, 1, "はちにんこ")]
    [InlineData ("こんにちは\nAB\n12", 10, 3, "21        \nBA        \nはちにんこ")]
    public void Draw_Horizontal_RightLeft_BottomTop (string text, int width, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.RightLeft_BottomTop
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 2, 1, "2")]
    [InlineData ("AB12", 5, 1, "21BA")]
    [InlineData ("AB\n12", 5, 2, "BA\n21")]
    [InlineData ("ABC 123 456", 7, 2, "654 321\nCBA    ")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 5, 1, "はち")]
    [InlineData ("こんにちは", 10, 1, "はちにんこ")]
    [InlineData ("こんにちは\nAB\n12", 10, 3, "はちにんこ\nBA        \n21        ")]
    public void Draw_Horizontal_RightLeft_TopBottom (string text, int width, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.RightLeft_TopBottom
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]

    // Horizontal with Alignment.Start
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // Horizontal with Alignment.End
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0 2 4**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
**0 2 4")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*0 2 4*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0  2  4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*0 你 4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0  你 4")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0 2 4**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
**0 2 4")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*0 2 4*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0  2  4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*0 你 4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0  你 4")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4 2 0**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
**4 2 0")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*4 2 0*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4  2  0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*4 你 0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4  你 0")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4 2 0**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
**4 2 0")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*4 2 0*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4  2  0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*4 你 0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4  你 0")]

    // Horizontal with alignment.Centered
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0 2 4**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
**0 2 4
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*0 2 4*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0  2  4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*0 你 4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0  你 4
*******
*******
*******")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0 2 4**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
**0 2 4
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*0 2 4*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0  2  4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*0 你 4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0  你 4
*******
*******
*******")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4 2 0**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
**4 2 0
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*4 2 0*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4  2  0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*4 你 0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4  你 0
*******
*******
*******")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4 2 0**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
**4 2 0
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*4 2 0*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4  2  0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*4 你 0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4  你 0
*******
*******
*******")]

    // Horizontal with alignment.Justified
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // Vertical with alignment.Left
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]

    // Vertical with alignment.Right
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
******0
****** 
******2
****** 
******4
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
******0
****** 
******2
****** 
******4")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
******0
****** 
******2
****** 
******4
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
******0
****** 
****** 
******2
****** 
****** 
******4")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
*****0*
***** *
*****你
***** *
*****4*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
*****0*
***** *
*****你
***** *
*****4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*****0*
***** *
*****你
***** *
*****4*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
*****0*
***** *
***** *
*****你
***** *
***** *
*****4*")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
******0
****** 
******2
****** 
******4
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
******0
****** 
******2
****** 
******4")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
******0
****** 
******2
****** 
******4
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
******0
****** 
****** 
******2
****** 
****** 
******4")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
*****0*
***** *
*****你
***** *
*****4*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
*****0*
***** *
*****你
***** *
*****4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*****0*
***** *
*****你
***** *
*****4*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
*****0*
***** *
***** *
*****你
***** *
***** *
*****4*")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
******4
****** 
******2
****** 
******0
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
******4
****** 
******2
****** 
******0")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
******4
****** 
******2
****** 
******0
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
******4
****** 
****** 
******2
****** 
****** 
******0")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
*****4*
***** *
*****你
***** *
*****0*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
*****4*
***** *
*****你
***** *
*****0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*****4*
***** *
*****你
***** *
*****0*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
*****4*
***** *
***** *
*****你
***** *
***** *
*****0*")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
******4
****** 
******2
****** 
******0
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
******4
****** 
******2
****** 
******0")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
******4
****** 
******2
****** 
******0
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
******4
****** 
****** 
******2
****** 
****** 
******0")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
*****4*
***** *
*****你
***** *
*****0*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
*****4*
***** *
*****你
***** *
*****0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*****4*
***** *
*****你
***** *
*****0*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
*****4*
***** *
***** *
*****你
***** *
***** *
*****0*")]

    // Vertical with alignment.Centered
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
***0***
*** ***
***2***
*** ***
***4***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
***0***
*** ***
***2***
*** ***
***4***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
***0***
*** ***
***2***
*** ***
***4***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
***0***
*** ***
*** ***
***2***
*** ***
*** ***
***4***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
**0****
** ****
**你***
** ****
**4****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
**0****
** ****
**你***
** ****
**4****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
**0****
** ****
**你***
** ****
**4****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
**0****
** ****
** ****
**你***
** ****
** ****
**4****")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
***0***
*** ***
***2***
*** ***
***4***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
***0***
*** ***
***2***
*** ***
***4***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
***0***
*** ***
***2***
*** ***
***4***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
***0***
*** ***
*** ***
***2***
*** ***
*** ***
***4***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
**0****
** ****
**你***
** ****
**4****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
**0****
** ****
**你***
** ****
**4****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
**0****
** ****
**你***
** ****
**4****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
**0****
** ****
** ****
**你***
** ****
** ****
**4****")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
***4***
*** ***
***2***
*** ***
***0***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
***4***
*** ***
***2***
*** ***
***0***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
***4***
*** ***
***2***
*** ***
***0***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
***4***
*** ***
*** ***
***2***
*** ***
*** ***
***0***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
**4****
** ****
**你***
** ****
**0****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
**4****
** ****
**你***
** ****
**0****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
**4****
** ****
**你***
** ****
**0****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
**4****
** ****
** ****
**你***
** ****
** ****
**0****")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
***4***
*** ***
***2***
*** ***
***0***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
***4***
*** ***
***2***
*** ***
***0***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
***4***
*** ***
***2***
*** ***
***0***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
***4***
*** ***
*** ***
***2***
*** ***
*** ***
***0***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
**4****
** ****
**你***
** ****
**0****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
**4****
** ****
**你***
** ****
**0****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
**4****
** ****
**你***
** ****
**0****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
**4****
** ****
** ****
**你***
** ****
** ****
**0****")]

    // Vertical with alignment.Justified
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]
    public void Draw_Text_Justification (string text, Alignment horizontalTextAlignment, Alignment alignment, TextDirection textDirection, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Alignment = horizontalTextAlignment,
            VerticalAlignment = alignment,
            Direction = textDirection,
            ConstrainToSize = new (7, 7),
            Text = text
        };

        Application.Driver?.FillRect (new (0, 0, 7, 7), (Rune)'*');
        tf.Draw (new (0, 0, 7, 7), Attribute.Default, Attribute.Default);
        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 0, 1, "", 0)]
    [InlineData ("A", 1, 1, "A", 0)]
    [InlineData ("A", 2, 2, " A", 1)]
    [InlineData ("AB", 1, 1, "B", 0)]
    [InlineData ("AB", 2, 2, " A\n B", 0)]
    [InlineData ("ABC", 3, 2, "  B\n  C", 0)]
    [InlineData ("ABC", 4, 2, "   B\n   C", 0)]
    [InlineData ("ABC", 6, 2, "     B\n     C", 0)]
    [InlineData ("こんにちは", 0, 1, "", 0)]
    [InlineData ("こんにちは", 1, 0, "", 0)]
    [InlineData ("こんにちは", 1, 1, "", 0)]
    [InlineData ("こんにちは", 2, 1, "は", 0)]
    [InlineData ("こんにちは", 2, 2, "ち\nは", 0)]
    [InlineData ("こんにちは", 2, 3, "に\nち\nは", 0)]
    [InlineData ("こんにちは", 2, 4, "ん\nに\nち\nは", 0)]
    [InlineData ("こんにちは", 2, 5, "こ\nん\nに\nち\nは", 0)]
    [InlineData ("こんにちは", 2, 6, "こ\nん\nに\nち\nは", 1)]
    [InlineData ("ABCD\nこんにちは", 4, 7, "  こ\n Aん\n Bに\n Cち\n Dは", 2)]
    [InlineData ("こんにちは\nABCD", 3, 7, "こ \nんA\nにB\nちC\nはD", 2)]
    public void Draw_Vertical_Bottom_Horizontal_Right (string text, int width, int height, string expectedText, int expectedY)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.End,
            Direction = TextDirection.TopBottom_LeftRight,
            VerticalAlignment = Alignment.End
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;

        tf.Draw (new (Point.Empty, new (width, height)), Attribute.Default, Attribute.Default);
        Rectangle rect = DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
        Assert.Equal (expectedY, rect.Y);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 1, 2, "2")]
    [InlineData ("AB12", 1, 5, "2\n1\nB\nA")]
    [InlineData ("AB\n12", 2, 5, "B2\nA1")]
    [InlineData ("ABC 123 456", 2, 7, "6C\n5B\n4A\n  \n3 \n2 \n1 ")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 2, 5, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは", 2, 10, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは\nAB\n12", 4, 10, "はB2\nちA1\nに  \nん  \nこ  ")]
    public void Draw_Vertical_BottomTop_LeftRight (string text, int width, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.BottomTop_LeftRight
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 1, 2, "2")]
    [InlineData ("AB12", 1, 5, "2\n1\nB\nA")]
    [InlineData ("AB\n12", 2, 5, "2B\n1A")]
    [InlineData ("ABC 123 456", 2, 7, "C6\nB5\nA4\n  \n 3\n 2\n 1")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 2, 5, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは", 2, 10, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは\nAB\n12", 4, 10, "2Bは\n1Aち\n  に\n  ん\n  こ")]
    public void Draw_Vertical_BottomTop_RightLeft (string text, int width, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.BottomTop_RightLeft
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    // Draw tests - Note that these depend on View

    [Fact]
    [TestRespondersDisposed]
    public void Draw_Vertical_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
    {
        Application.Init (new FakeDriver ());
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Toplevel top = new ();

        var view = new View { Y = -2, Height = 10, TextDirection = TextDirection.TopBottom_LeftRight, Text = "view" };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.Equal (-2, view.Y);

                                     Application.RequestStop ();
                                 };

        try
        {
            Application.Run (top);
        }
        catch (IndexOutOfRangeException ex)
        {
            // After the fix this exception will not be caught.
            Assert.IsType<IndexOutOfRangeException> (ex);
        }

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 5, 5, "A")]
    [InlineData (
                    "AB12",
                    5,
                    5,
                    @"
A
B
1
2")]
    [InlineData (
                    "AB\n12",
                    5,
                    5,
                    @"
A1
B2")]
    [InlineData ("", 5, 1, "")]
    [InlineData (
                    "Hello Worlds",
                    1,
                    12,
                    @"
H
e
l
l
o
 
W
o
r
l
d
s")]
    [InlineData ("Hello Worlds", 12, 1, @"HelloWorlds")]
    public void Draw_Vertical_TopBottom_LeftRight (string text, int width, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.TopBottom_LeftRight
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (new (0, 0, 20, 20), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [SetupFakeDriver]
    [Theory]

    // The expectedY param is to probe that the expectedText param start at that Y coordinate
    [InlineData ("A", 0, "", 0)]
    [InlineData ("A", 1, "A", 0)]
    [InlineData ("A", 2, "A", 0)]
    [InlineData ("A", 3, "A", 1)]
    [InlineData ("AB", 1, "A", 0)]
    [InlineData ("AB", 2, "A\nB", 0)]
    [InlineData ("ABC", 2, "A\nB", 0)]
    [InlineData ("ABC", 3, "A\nB\nC", 0)]
    [InlineData ("ABC", 4, "A\nB\nC", 0)]
    [InlineData ("ABC", 5, "A\nB\nC", 1)]
    [InlineData ("ABC", 6, "A\nB\nC", 1)]
    [InlineData ("ABC", 9, "A\nB\nC", 3)]
    [InlineData ("ABCD", 2, "B\nC", 0)]
    [InlineData ("こんにちは", 0, "", 0)]
    [InlineData ("こんにちは", 1, "に", 0)]
    [InlineData ("こんにちは", 2, "ん\nに", 0)]
    [InlineData ("こんにちは", 3, "ん\nに\nち", 0)]
    [InlineData ("こんにちは", 4, "こ\nん\nに\nち", 0)]
    [InlineData ("こんにちは", 5, "こ\nん\nに\nち\nは", 0)]
    [InlineData ("こんにちは", 6, "こ\nん\nに\nち\nは", 0)]
    [InlineData ("ABCD\nこんにちは", 7, "Aこ\nBん\nCに\nDち\n は", 1)]
    [InlineData ("こんにちは\nABCD", 7, "こA\nんB\nにC\nちD\nは ", 1)]
    public void Draw_Vertical_TopBottom_LeftRight_Middle (string text, int height, string expectedText, int expectedY)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.TopBottom_LeftRight,
            VerticalAlignment = Alignment.Center
        };

        int width = text.ToRunes ().Max (r => r.GetColumns ());

        if (text.Contains ("\n"))
        {
            width++;
        }

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (new (0, 0, 5, height), Attribute.Default, Attribute.Default);

        Rectangle rect = DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
        Assert.Equal (expectedY, rect.Y);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("A", 5, "A")]
    [InlineData (
                    "AB12",
                    5,
                    @"
A
B
1
2")]
    [InlineData (
                    "AB\n12",
                    5,
                    @"
A1
B2")]
    [InlineData ("", 1, "")]
    [InlineData (
                    "AB1 2",
                    2,
                    @"
A12
B  ")]
    [InlineData (
                    "こんにちは",
                    1,
                    @"
こん")]
    [InlineData (
                    "こんにちは",
                    2,
                    @"
こに
んち")]
    [InlineData (
                    "こんにちは",
                    5,
                    @"
こ
ん
に
ち
は")]
    public void Draw_Vertical_TopBottom_LeftRight_Top (string text, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.TopBottom_LeftRight
        };

        tf.ConstrainToWidth = 5;
        tf.ConstrainToHeight = height;
        tf.Draw (new (0, 0, 5, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [Theory]
    [InlineData (14, 1, TextDirection.LeftRight_TopBottom, "Les Misęrables")]
    [InlineData (1, 14, TextDirection.TopBottom_LeftRight, "L\ne\ns\n \nM\ni\ns\nę\nr\na\nb\nl\ne\ns")]
    [InlineData (
                    4,
                    4,
                    TextDirection.TopBottom_LeftRight,
                    @"
LMre
eias
ssb 
 ęl "
                )]
    public void Draw_With_Combining_Runes (int width, int height, TextDirection textDirection, string expected)
    {
        var driver = new FakeDriver ();
        driver.Init ();

        var text = "Les Mise\u0328\u0301rables";

        var tf = new TextFormatter ();
        tf.Direction = textDirection;
        tf.Text = text;

        Assert.True (tf.WordWrap);

        tf.ConstrainToSize = new (width, height);

        tf.Draw (
                 new (0, 0, width, height),
                 new (ColorName16.White, ColorName16.Black),
                 new (ColorName16.Blue, ColorName16.Black),
                 default (Rectangle),
                 driver
                );
        DriverAssert.AssertDriverContentsWithFrameAre (expected, _output, driver);

        driver.End ();
    }

    [Fact]
    [SetupFakeDriver]
    public void FillRemaining_True_False ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (22, 5);

        Attribute [] attrs =
        {
            Attribute.Default, new (ColorName16.Green, ColorName16.BrightMagenta),
            new (ColorName16.Blue, ColorName16.Cyan)
        };
        var tf = new TextFormatter { ConstrainToSize = new (14, 3), Text = "Test\nTest long\nTest long long\n", MultiLine = true };

        tf.Draw (
                 new (1, 1, 19, 3),
                 attrs [1],
                 attrs [2]);

        Assert.False (tf.FillRemaining);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 Test          
 Test long     
 Test long long",
                                                       _output);

        DriverAssert.AssertDriverAttributesAre (
                                                @"
000000000000000000000
011110000000000000000
011111111100000000000
011111111111111000000
000000000000000000000",
                                                _output,
                                                null,
                                                attrs);

        tf.FillRemaining = true;

        tf.Draw (
                 new (1, 1, 19, 3),
                 attrs [1],
                 attrs [2]);

        DriverAssert.AssertDriverAttributesAre (
                                                @"
000000000000000000000
011111111111111111110
011111111111111111110
011111111111111111110
000000000000000000000",
                                                _output,
                                                null,
                                                attrs);
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("Hello World", 15, 1, "Hello     World")]
    [InlineData (
                    "Well Done\nNice Work",
                    15,
                    2,
                    @"
Well       Done
Nice       Work")]
    [InlineData ("你好 世界", 15, 1, "你好       世界")]
    [InlineData (
                    "做 得好\n幹 得好",
                    15,
                    2,
                    @"
做         得好
幹         得好")]
    public void Justify_Horizontal (string text, int width, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.Fill,
            ConstrainToSize = new Size (width, height),
            MultiLine = true
        };

        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [Theory]
    [InlineData (17, 1, TextDirection.LeftRight_TopBottom, 4, "This is a     Tab")]
    [InlineData (1, 17, TextDirection.TopBottom_LeftRight, 4, "T\nh\ni\ns\n \ni\ns\n \na\n \n \n \n \n \nT\na\nb")]
    [InlineData (13, 1, TextDirection.LeftRight_TopBottom, 0, "This is a Tab")]
    [InlineData (1, 13, TextDirection.TopBottom_LeftRight, 0, "T\nh\ni\ns\n \ni\ns\n \na\n \nT\na\nb")]
    public void TabWith_PreserveTrailingSpaces_False (
        int width,
        int height,
        TextDirection textDirection,
        int tabWidth,
        string expected
    )
    {
        var driver = new FakeDriver ();
        driver.Init ();

        var text = "This is a \tTab";
        var tf = new TextFormatter ();
        tf.Direction = textDirection;
        tf.TabWidth = tabWidth;
        tf.Text = text;
        tf.ConstrainToWidth = 20;
        tf.ConstrainToHeight = 20;

        Assert.True (tf.WordWrap);
        Assert.False (tf.PreserveTrailingSpaces);

        tf.Draw (
                 new (0, 0, width, height),
                 new (ColorName16.White, ColorName16.Black),
                 new (ColorName16.Blue, ColorName16.Black),
                 default (Rectangle),
                 driver
                );
        DriverAssert.AssertDriverContentsWithFrameAre (expected, _output, driver);

        driver.End ();
    }

    [Theory]
    [InlineData (17, 1, TextDirection.LeftRight_TopBottom, 4, "This is a     Tab")]
    [InlineData (1, 17, TextDirection.TopBottom_LeftRight, 4, "T\nh\ni\ns\n \ni\ns\n \na\n \n \n \n \n \nT\na\nb")]
    [InlineData (13, 1, TextDirection.LeftRight_TopBottom, 0, "This is a Tab")]
    [InlineData (1, 13, TextDirection.TopBottom_LeftRight, 0, "T\nh\ni\ns\n \ni\ns\n \na\n \nT\na\nb")]
    public void TabWith_PreserveTrailingSpaces_True (
        int width,
        int height,
        TextDirection textDirection,
        int tabWidth,
        string expected
    )
    {
        var driver = new FakeDriver ();
        driver.Init ();

        var text = "This is a \tTab";
        var tf = new TextFormatter ();

        tf.Direction = textDirection;
        tf.TabWidth = tabWidth;
        tf.PreserveTrailingSpaces = true;
        tf.Text = text;
        tf.ConstrainToWidth = 20;
        tf.ConstrainToHeight = 20;

        Assert.True (tf.WordWrap);

        tf.Draw (
                 new (0, 0, width, height),
                 new (ColorName16.White, ColorName16.Black),
                 new (ColorName16.Blue, ColorName16.Black),
                 default (Rectangle),
                 driver
                );
        DriverAssert.AssertDriverContentsWithFrameAre (expected, _output, driver);

        driver.End ();
    }

    [Theory]
    [InlineData (17, 1, TextDirection.LeftRight_TopBottom, 4, "This is a     Tab")]
    [InlineData (1, 17, TextDirection.TopBottom_LeftRight, 4, "T\nh\ni\ns\n \ni\ns\n \na\n \n \n \n \n \nT\na\nb")]
    [InlineData (13, 1, TextDirection.LeftRight_TopBottom, 0, "This is a Tab")]
    [InlineData (1, 13, TextDirection.TopBottom_LeftRight, 0, "T\nh\ni\ns\n \ni\ns\n \na\n \nT\na\nb")]
    public void TabWith_WordWrap_True (
        int width,
        int height,
        TextDirection textDirection,
        int tabWidth,
        string expected
    )
    {
        var driver = new FakeDriver ();
        driver.Init ();

        var text = "This is a \tTab";
        var tf = new TextFormatter ();

        tf.Direction = textDirection;
        tf.TabWidth = tabWidth;
        tf.WordWrap = true;
        tf.Text = text;
        tf.ConstrainToWidth = 20;
        tf.ConstrainToHeight = 20;

        Assert.False (tf.PreserveTrailingSpaces);

        tf.Draw (
                 new (0, 0, width, height),
                 new (ColorName16.White, ColorName16.Black),
                 new (ColorName16.Blue, ColorName16.Black),
                 default (Rectangle),
                 driver
                );
        DriverAssert.AssertDriverContentsWithFrameAre (expected, _output, driver);

        driver.End ();
    }

    [Fact]
    [SetupFakeDriver]
    public void UICatalog_AboutBox_Text ()
    {
        TextFormatter tf = new ()
        {
            Text = UICatalog.UICatalogTop.GetAboutBoxMessage (),
            Alignment = Alignment.Center,
            VerticalAlignment = Alignment.Start,
            WordWrap = false,
            MultiLine = true,
            HotKeySpecifier = (Rune)0xFFFF
        };

        Size tfSize = tf.FormatAndGetSize ();
        Assert.Equal (new (59, 13), tfSize);

        ((FakeDriver)Application.Driver).SetBufferSize (tfSize.Width, tfSize.Height);

        Application.Driver.FillRect (Application.Screen, (Rune)'*');
        tf.Draw (Application.Screen, Attribute.Default, Attribute.Default);

        var expectedText = """
                           UI Catalog: A comprehensive sample library and test app for
                           ***********************************************************
                            _______                  _             _   _____       _ *
                           |__   __|                (_)           | | / ____|     (_)*
                              | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ *
                              | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |*
                              | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |*
                              |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|*
                           ***********************************************************
                           **********************v2 - Pre-Alpha***********************
                           ***********************************************************
                           **********https://github.com/gui-cs/Terminal.Gui***********
                           ***********************************************************
                           """;

        DriverAssert.AssertDriverContentsAre (expectedText.ReplaceLineEndings (), _output);
    }

    #region FormatAndGetSizeTests

    // TODO: Add multi-line examples
    // TODO: Add other TextDirection examples

    [Theory]
    [SetupFakeDriver]
    [InlineData ("界1234", 10, 10, TextDirection.LeftRight_TopBottom, 6, 1, @"界1234")]
    [InlineData ("01234", 10, 10, TextDirection.LeftRight_TopBottom, 5, 1, @"01234")]
    [InlineData (
                    "界1234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    5,
                    """
                    界
                    1 
                    2 
                    3 
                    4 
                    """)]
    [InlineData (
                    "01234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    1,
                    5,
                    """
                    0
                    1
                    2
                    3
                    4
                    """)]
    [InlineData (
                    "界1234",
                    3,
                    3,
                    TextDirection.LeftRight_TopBottom,
                    3,
                    2,
                    """
                    界1
                    234
                    """)]
    [InlineData (
                    "01234",
                    3,
                    3,
                    TextDirection.LeftRight_TopBottom,
                    3,
                    2,
                    """
                    012
                    34 
                    """)]
    [InlineData (
                    "界1234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    3,
                    3,
                    """
                    界3
                    1 4
                    2  
                    """)]
    [InlineData (
                    "01234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    3,
                    """
                    03
                    14
                    2 
                    """)]
    [InlineData ("01234", 2, 1, TextDirection.LeftRight_TopBottom, 2, 1, @"01")]
    public void FormatAndGetSize_Returns_Correct_Size (
        string text,
        int width,
        int height,
        TextDirection direction,
        int expectedWidth,
        int expectedHeight,
        string expectedDraw
    )
    {
        TextFormatter tf = new ()
        {
            Direction = direction,
            ConstrainToWidth = width,
            ConstrainToHeight = height,
            Text = text
        };
        Assert.True (tf.WordWrap);
        Size size = tf.FormatAndGetSize ();
        Assert.Equal (new (expectedWidth, expectedHeight), size);

        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedDraw, _output);
    }

    [Theory]
    [SetupFakeDriver]
    [InlineData ("界1234", 10, 10, TextDirection.LeftRight_TopBottom, 6, 1, @"界1234")]
    [InlineData ("01234", 10, 10, TextDirection.LeftRight_TopBottom, 5, 1, @"01234")]
    [InlineData (
                    "界1234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    5,
                    """
                    界
                    1 
                    2 
                    3 
                    4 
                    """)]
    [InlineData (
                    "01234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    1,
                    5,
                    """
                    0
                    1
                    2
                    3
                    4
                    """)]
    [InlineData ("界1234", 3, 3, TextDirection.LeftRight_TopBottom, 3, 1, @"界1")]
    [InlineData ("01234", 3, 3, TextDirection.LeftRight_TopBottom, 3, 1, @"012")]
    [InlineData (
                    "界1234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    3,
                    """
                    界
                    1 
                    2 
                    """)]
    [InlineData (
                    "01234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    1,
                    3,
                    """
                    0
                    1
                    2
                    """)]
    public void FormatAndGetSize_WordWrap_False_Returns_Correct_Size (
        string text,
        int width,
        int height,
        TextDirection direction,
        int expectedWidth,
        int expectedHeight,
        string expectedDraw
    )
    {
        TextFormatter tf = new ()
        {
            Direction = direction,
            ConstrainToSize = new (width, height),
            Text = text,
            WordWrap = false
        };
        Assert.False (tf.WordWrap);
        Size size = tf.FormatAndGetSize ();
        Assert.Equal (new (expectedWidth, expectedHeight), size);

        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedDraw, _output);
    }

    #endregion
}
