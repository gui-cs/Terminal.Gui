﻿using System.Text;
using UICatalog;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.TextTests;

public class TextFormatterTests
{
    public TextFormatterTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    [Theory]
    [InlineData ("")]
    [InlineData (null)]
    [InlineData ("test")]
    public void ClipAndJustify_Invalid_Returns_Original (string text)
    {
        string expected = string.IsNullOrEmpty (text) ? text : "";
        Assert.Equal (expected, TextFormatter.ClipAndJustify (text, 0, Alignment.Start));
        Assert.Equal (expected, TextFormatter.ClipAndJustify (text, 0, Alignment.Start));

        Assert.Throws<ArgumentOutOfRangeException> (
                                                    () =>
                                                        TextFormatter.ClipAndJustify (text, -1, Alignment.Start)
                                                   );
    }

    [Theory]
    [InlineData ("test", "", 0)]
    [InlineData ("test", "te", 2)]
    [InlineData ("test", "test", int.MaxValue)]
    [InlineData ("A sentence has words.", "A sentence has words.", 22)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words.", int.MaxValue)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
    [InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
    [InlineData ("A\tsentence\thas\twords.", "A sentence has words.", int.MaxValue)]
    [InlineData ("A\tsentence\thas\twords.", "A sentence", 10)]
    [InlineData ("line1\nline2\nline3long!", "line1\nline2\nline3long!", int.MaxValue)]
    [InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
    [InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
    [InlineData ("Ð ÑÐ", "Ð ÑÐ", 5)] // should fit
    [InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
    [InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
    public void ClipAndJustify_Valid_Centered (string text, string justifiedText, int maxWidth)
    {
        var alignment = Alignment.Center;
        var textDirection = TextDirection.LeftRight_TopBottom;
        var tabWidth = 1;

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        int expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        Assert.True (justifiedText.GetRuneCount () <= maxWidth);
        Assert.True (justifiedText.GetColumns () <= maxWidth);
        Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());

        Assert.Equal (
                      expectedClippedWidth,
                      justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1))
                     );
        Assert.True (expectedClippedWidth <= maxWidth);

        Assert.Equal (
                      StringExtensions.ToString (justifiedText.ToRunes () [..expectedClippedWidth]),
                      justifiedText
                     );
    }

    [Theory]
    [InlineData ("test", "", 0)]
    [InlineData ("test", "te", 2)]
    [InlineData ("test", "test", int.MaxValue)] // This doesn't throw because it only create a word with length 1
    [InlineData ("A sentence has words.", "A  sentence has words.", 22)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
    [InlineData (
                    "A sentence has words.",
                    "A                                                                                                                                                                 sentence                                                                                                                                                                 has                                                                                                                                                                words.",
                    500
                )] // should fit
    [InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
    [InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
    // Now throw System.OutOfMemoryException. See https://stackoverflow.com/questions/20672920/maxcapacity-of-stringbuilder
    //[InlineData ("A\tsentence\thas\twords.", "A sentence has words.", int.MaxValue)]
    [InlineData ("A\tsentence\thas\twords.", "A sentence", 10)]
    [InlineData (
                    "line1\nline2\nline3long!",
                    "line1\nline2\nline3long!",
                    int.MaxValue
                )] // This doesn't throw because it only create a line with length 1
    [InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
    [InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
    [InlineData ("Ð ÑÐ", "Ð  ÑÐ", 5)] // should fit
    [InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
    [InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
    public void ClipAndJustify_Valid_Justified (string text, string justifiedText, int maxWidth)
    {
        var alignment = Alignment.Fill;
        var textDirection = TextDirection.LeftRight_TopBottom;
        var tabWidth = 1;

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        int expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        Assert.True (justifiedText.GetRuneCount () <= maxWidth);
        Assert.True (justifiedText.GetColumns () <= maxWidth);
        Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());

        Assert.Equal (
                      expectedClippedWidth,
                      justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1))
                     );
        Assert.True (expectedClippedWidth <= maxWidth);

        Assert.Equal (
                      StringExtensions.ToString (justifiedText.ToRunes () [..expectedClippedWidth]),
                      justifiedText
                     );

        // see Justify_ tests below
    }

    [Theory]
    [InlineData ("test", "", 0)]
    [InlineData ("test", "te", 2)]
    [InlineData ("test", "test", int.MaxValue)]
    [InlineData ("A sentence has words.", "A sentence has words.", 22)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words.", int.MaxValue)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
    [InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
    [InlineData ("A\tsentence\thas\twords.", "A sentence has words.", int.MaxValue)]
    [InlineData ("A\tsentence\thas\twords.", "A sentence", 10)]
    [InlineData ("line1\nline2\nline3long!", "line1\nline2\nline3long!", int.MaxValue)]
    [InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
    [InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
    [InlineData ("Ð ÑÐ", "Ð ÑÐ", 5)] // should fit
    [InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
    [InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
    public void ClipAndJustify_Valid_Left (string text, string justifiedText, int maxWidth)
    {
        var alignment = Alignment.Start;
        var textDirection = TextDirection.LeftRight_BottomTop;
        var tabWidth = 1;

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        int expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        Assert.True (justifiedText.GetRuneCount () <= maxWidth);
        Assert.True (justifiedText.GetColumns () <= maxWidth);
        Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());

        Assert.Equal (
                      expectedClippedWidth,
                      justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1))
                     );
        Assert.True (expectedClippedWidth <= maxWidth);

        Assert.Equal (
                      StringExtensions.ToString (justifiedText.ToRunes () [..expectedClippedWidth]),
                      justifiedText
                     );
    }

    [Theory]
    [InlineData ("test", "", 0)]
    [InlineData ("test", "te", 2)]
    [InlineData ("test", "test", int.MaxValue)]
    [InlineData ("A sentence has words.", "A sentence has words.", 22)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words.", int.MaxValue)] // should fit
    [InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
    [InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
    [InlineData ("A\tsentence\thas\twords.", "A sentence has words.", int.MaxValue)]
    [InlineData ("A\tsentence\thas\twords.", "A sentence", 10)]
    [InlineData ("line1\nline2\nline3long!", "line1\nline2\nline3long!", int.MaxValue)]
    [InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
    [InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
    [InlineData ("Ð ÑÐ", "Ð ÑÐ", 5)] // should fit
    [InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
    [InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
    public void ClipAndJustify_Valid_Right (string text, string justifiedText, int maxWidth)
    {
        var alignment = Alignment.End;
        var textDirection = TextDirection.LeftRight_BottomTop;
        var tabWidth = 1;

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        int expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);

        Assert.Equal (
                      justifiedText,
                      TextFormatter.ClipAndJustify (text, maxWidth, alignment, textDirection, tabWidth)
                     );
        Assert.True (justifiedText.GetRuneCount () <= maxWidth);
        Assert.True (justifiedText.GetColumns () <= maxWidth);
        Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());

        Assert.Equal (
                      expectedClippedWidth,
                      justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1))
                     );
        Assert.True (expectedClippedWidth <= maxWidth);

        Assert.Equal (
                      StringExtensions.ToString (justifiedText.ToRunes () [..expectedClippedWidth]),
                      justifiedText
                     );
    }

    public static IEnumerable<object []> CMGlyphs =>
        new List<object []> { new object [] { $"{CM.Glyphs.LeftBracket} Say Hello 你 {CM.Glyphs.RightBracket}", 16, 15 } };

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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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
        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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
        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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
        Rectangle rect = TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        Rectangle rect = TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
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
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output, driver);

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

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 Test          
 Test long     
 Test long long",
                                                      _output);

        TestHelpers.AssertDriverAttributesAre (
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

        TestHelpers.AssertDriverAttributesAre (
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

    [Theory]
    [InlineData ("_k Before", true, 0, (KeyCode)'K')] // lower case should return uppercase Hotkey
    [InlineData ("a_k Second", true, 1, (KeyCode)'K')]
    [InlineData ("Last _k", true, 5, (KeyCode)'K')]
    [InlineData ("After k_", false, -1, KeyCode.Null)]
    [InlineData ("Multiple _k and _R", true, 9, (KeyCode)'K')]
    [InlineData ("Non-english: _кдать", true, 13, (KeyCode)'к')] // Lower case Cryllic K (к)
    [InlineData ("_k Before", true, 0, (KeyCode)'K', true)] // Turn on FirstUpperCase and verify same results
    [InlineData ("a_k Second", true, 1, (KeyCode)'K', true)]
    [InlineData ("Last _k", true, 5, (KeyCode)'K', true)]
    [InlineData ("After k_", false, -1, KeyCode.Null, true)]
    [InlineData ("Multiple _k and _r", true, 9, (KeyCode)'K', true)]
    [InlineData ("Non-english: _кдать", true, 13, (KeyCode)'к', true)] // Cryllic K (К)
    public void FindHotKey_AlphaLowerCase_Succeeds (
        string text,
        bool expectedResult,
        int expectedHotPos,
        KeyCode expectedKey,
        bool supportFirstUpperCase = false
    )
    {
        var hotKeySpecifier = (Rune)'_';

        bool result = TextFormatter.FindHotKey (
                                                text,
                                                hotKeySpecifier,
                                                out int hotPos,
                                                out Key hotKey,
                                                supportFirstUpperCase
                                               );

        if (expectedResult)
        {
            Assert.True (result);
        }
        else
        {
            Assert.False (result);
        }

        Assert.Equal (expectedResult, result);
        Assert.Equal (expectedHotPos, hotPos);
        Assert.Equal (expectedKey, hotKey);
    }

    [Theory]
    [InlineData ("_K Before", true, 0, (KeyCode)'K')]
    [InlineData ("a_K Second", true, 1, (KeyCode)'K')]
    [InlineData ("Last _K", true, 5, (KeyCode)'K')]
    [InlineData ("After K_", false, -1, KeyCode.Null)]
    [InlineData ("Multiple _K and _R", true, 9, (KeyCode)'K')]
    [InlineData ("Non-english: _Кдать", true, 13, (KeyCode)'К')] // Cryllic K (К)
    [InlineData ("_K Before", true, 0, (KeyCode)'K', true)] // Turn on FirstUpperCase and verify same results
    [InlineData ("a_K Second", true, 1, (KeyCode)'K', true)]
    [InlineData ("Last _K", true, 5, (KeyCode)'K', true)]
    [InlineData ("After K_", false, -1, KeyCode.Null, true)]
    [InlineData ("Multiple _K and _R", true, 9, (KeyCode)'K', true)]
    [InlineData ("Non-english: _Кдать", true, 13, (KeyCode)'К', true)] // Cryllic K (К)
    public void FindHotKey_AlphaUpperCase_Succeeds (
        string text,
        bool expectedResult,
        int expectedHotPos,
        KeyCode expectedKey,
        bool supportFirstUpperCase = false
    )
    {
        var hotKeySpecifier = (Rune)'_';

        bool result = TextFormatter.FindHotKey (
                                                text,
                                                hotKeySpecifier,
                                                out int hotPos,
                                                out Key hotKey,
                                                supportFirstUpperCase
                                               );

        if (expectedResult)
        {
            Assert.True (result);
        }
        else
        {
            Assert.False (result);
        }

        Assert.Equal (expectedResult, result);
        Assert.Equal (expectedHotPos, hotPos);
        Assert.Equal (expectedKey, hotKey);
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    [InlineData ("no hotkey")]
    [InlineData ("No hotkey, Upper Case")]
    [InlineData ("Non-english: Сохранить")]
    public void FindHotKey_Invalid_ReturnsFalse (string text)
    {
        var hotKeySpecifier = (Rune)'_';
        var supportFirstUpperCase = false;
        var hotPos = 0;
        Key hotKey = KeyCode.Null;
        var result = false;

        result = TextFormatter.FindHotKey (
                                           text,
                                           hotKeySpecifier,
                                           out hotPos,
                                           out hotKey,
                                           supportFirstUpperCase
                                          );
        Assert.False (result);
        Assert.Equal (-1, hotPos);
        Assert.Equal (KeyCode.Null, hotKey);
    }

    [Theory]
    [InlineData ("\"k before")]
    [InlineData ("ak second")]
    [InlineData ("last k")]
    [InlineData ("multiple k and r")]
    [InlineData ("12345")]
    [InlineData ("`~!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?")] // punctuation
    [InlineData (" ~  s  gui.cs   master ↑10")] // ~IsLetterOrDigit + Unicode
    [InlineData ("non-english: кдать")] // Lower case Cryllic K (к)
    public void FindHotKey_Legacy_FirstUpperCase_NotFound_Returns_False (string text)
    {
        var supportFirstUpperCase = true;

        var hotKeySpecifier = (Rune)0;

        bool result = TextFormatter.FindHotKey (
                                                text,
                                                hotKeySpecifier,
                                                out int hotPos,
                                                out Key hotKey,
                                                supportFirstUpperCase
                                               );
        Assert.False (result);
        Assert.Equal (-1, hotPos);
        Assert.Equal (KeyCode.Null, hotKey);
    }

    [Theory]
    [InlineData ("K Before", true, 0, (KeyCode)'K')]
    [InlineData ("aK Second", true, 1, (KeyCode)'K')]
    [InlineData ("last K", true, 5, (KeyCode)'K')]
    [InlineData ("multiple K and R", true, 9, (KeyCode)'K')]
    [InlineData ("non-english: Кдать", true, 13, (KeyCode)'К')] // Cryllic K (К)
    public void FindHotKey_Legacy_FirstUpperCase_Succeeds (
        string text,
        bool expectedResult,
        int expectedHotPos,
        KeyCode expectedKey
    )
    {
        var supportFirstUpperCase = true;

        var hotKeySpecifier = (Rune)0;

        bool result = TextFormatter.FindHotKey (
                                                text,
                                                hotKeySpecifier,
                                                out int hotPos,
                                                out Key hotKey,
                                                supportFirstUpperCase
                                               );

        if (expectedResult)
        {
            Assert.True (result);
        }
        else
        {
            Assert.False (result);
        }

        Assert.Equal (expectedResult, result);
        Assert.Equal (expectedHotPos, hotPos);
        Assert.Equal (expectedKey, hotKey);
    }

    [Theory]
    [InlineData ("_1 Before", true, 0, (KeyCode)'1')] // Digits 
    [InlineData ("a_1 Second", true, 1, (KeyCode)'1')]
    [InlineData ("Last _1", true, 5, (KeyCode)'1')]
    [InlineData ("After 1_", false, -1, KeyCode.Null)]
    [InlineData ("Multiple _1 and _2", true, 9, (KeyCode)'1')]
    [InlineData ("_1 Before", true, 0, (KeyCode)'1', true)] // Turn on FirstUpperCase and verify same results
    [InlineData ("a_1 Second", true, 1, (KeyCode)'1', true)]
    [InlineData ("Last _1", true, 5, (KeyCode)'1', true)]
    [InlineData ("After 1_", false, -1, KeyCode.Null, true)]
    [InlineData ("Multiple _1 and _2", true, 9, (KeyCode)'1', true)]
    public void FindHotKey_Numeric_Succeeds (
        string text,
        bool expectedResult,
        int expectedHotPos,
        KeyCode expectedKey,
        bool supportFirstUpperCase = false
    )
    {
        var hotKeySpecifier = (Rune)'_';

        bool result = TextFormatter.FindHotKey (
                                                text,
                                                hotKeySpecifier,
                                                out int hotPos,
                                                out Key hotKey,
                                                supportFirstUpperCase
                                               );

        if (expectedResult)
        {
            Assert.True (result);
        }
        else
        {
            Assert.False (result);
        }

        Assert.Equal (expectedResult, result);
        Assert.Equal (expectedHotPos, hotPos);
        Assert.Equal (expectedKey, hotKey);
    }

    [Theory]
    [InlineData ("_\"k before", true, (KeyCode)'"')] // BUGBUG: Not sure why this fails. " is a normal char
    [InlineData ("\"_k before", true, KeyCode.K)]
    [InlineData ("_`~!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?", true, (KeyCode)'`')]
    [InlineData ("`_~!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?", true, (KeyCode)'~')]
    [InlineData (
                    "`~!@#$%^&*()-__=+[{]}\\|;:'\",<.>/?",
                    true,
                    (KeyCode)'='
                )] // BUGBUG: Not sure why this fails. Ignore the first and consider the second
    [InlineData ("_ ~  s  gui.cs   master ↑10", true, (KeyCode)'')] // ~IsLetterOrDigit + Unicode
    [InlineData (" ~  s  gui.cs  _ master ↑10", true, (KeyCode)'')] // ~IsLetterOrDigit + Unicode
    [InlineData ("non-english: _кдать", true, (KeyCode)'к')] // Lower case Cryllic K (к)
    public void FindHotKey_Symbols_Returns_Symbol (string text, bool found, KeyCode expected)
    {
        var hotKeySpecifier = (Rune)'_';

        bool result = TextFormatter.FindHotKey (text, hotKeySpecifier, out int _, out Key hotKey);
        Assert.Equal (found, result);
        Assert.Equal (expected, hotKey);
    }

    [Fact]
    public void Format_Dont_Throw_ArgumentException_With_WordWrap_As_False_And_Keep_End_Spaces_As_True ()
    {
        Exception exception = Record.Exception (
                                                () =>
                                                    TextFormatter.Format (
                                                                          "Some text",
                                                                          4,
                                                                          Alignment.Start,
                                                                          false,
                                                                          true
                                                                         )
                                               );
        Assert.Null (exception);
    }

    [Theory]
    [InlineData (
                    "Hello world, how are you today? Pretty neat!",
                    44,
                    80,
                    "Hello      world,      how      are      you      today?      Pretty      neat!"
                )]
    public void Format_Justified_Always_Returns_Text_Width_Equal_To_Passed_Width_Horizontal (
        string text,
        int runeCount,
        int maxWidth,
        string justifiedText
    )
    {
        Assert.Equal (runeCount, text.GetRuneCount ());

        var fmtText = string.Empty;

        for (int i = text.GetRuneCount (); i < maxWidth; i++)
        {
            fmtText = TextFormatter.Format (text, i, Alignment.Fill, true) [0];
            Assert.Equal (i, fmtText.GetRuneCount ());
            char c = fmtText [^1];
            Assert.True (text.EndsWith (c));
        }

        Assert.Equal (justifiedText, fmtText);
    }

    [Theory]
    [InlineData (
                    "Hello world, how are you today? Pretty neat!",
                    44,
                    80,
                    "Hello      world,      how      are      you      today?      Pretty      neat!"
                )]
    public void Format_Justified_Always_Returns_Text_Width_Equal_To_Passed_Width_Vertical (
        string text,
        int runeCount,
        int maxWidth,
        string justifiedText
    )
    {
        Assert.Equal (runeCount, text.GetRuneCount ());

        var fmtText = string.Empty;

        for (int i = text.GetRuneCount (); i < maxWidth; i++)
        {
            fmtText = TextFormatter.Format (
                                            text,
                                            i,
                                            Alignment.Fill,
                                            false,
                                            true,
                                            0,
                                            TextDirection.TopBottom_LeftRight
                                           ) [0];
            Assert.Equal (i, fmtText.GetRuneCount ());
            char c = fmtText [^1];
            Assert.True (text.EndsWith (c));
        }

        Assert.Equal (justifiedText, fmtText);
    }

    [Theory]
    [InlineData ("Truncate", 3, "Tru")]
    [InlineData ("デモエムポンズ", 3, "デ")]
    public void Format_Truncate_Simple_And_Wide_Runes (string text, int width, string expected)
    {
        List<string> list = TextFormatter.Format (text, width, false, false);
        Assert.Equal (expected, list [^1]);
    }

    [Theory]
    [MemberData (nameof (FormatEnvironmentNewLine))]
    public void Format_With_PreserveTrailingSpaces_And_Without_PreserveTrailingSpaces (
        string text,
        int width,
        IEnumerable<string> expected
    )
    {
        var preserveTrailingSpaces = false;
        List<string> formated = TextFormatter.Format (text, width, false, true, preserveTrailingSpaces);
        Assert.Equal (expected, formated);

        preserveTrailingSpaces = true;
        formated = TextFormatter.Format (text, width, false, true, preserveTrailingSpaces);
        Assert.Equal (expected, formated);
    }

    [Theory]
    [InlineData (
                    " A sentence has words. \n This is the second Line - 2. ",
                    4,
                    -50,
                    Alignment.Start,
                    true,
                    false,
                    new [] { " A", "sent", "ence", "has", "word", "s. ", " Thi", "s is", "the", "seco", "nd", "Line", "- 2." },
                    " Asentencehaswords.  This isthesecondLine- 2."
                )]
    [InlineData (
                    " A sentence has words. \n This is the second Line - 2. ",
                    4,
                    -50,
                    Alignment.Start,
                    true,
                    true,
                    new []
                    {
                        " A ",
                        "sent",
                        "ence",
                        " ",
                        "has ",
                        "word",
                        "s. ",
                        " ",
                        "This",
                        " is ",
                        "the ",
                        "seco",
                        "nd ",
                        "Line",
                        " - ",
                        "2. "
                    },
                    " A sentence has words.  This is the second Line - 2. "
                )]
    public void Format_WordWrap_PreserveTrailingSpaces (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        bool preserveTrailingSpaces,
        IEnumerable<string> resultLines,
        string expectedWrappedText
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        List<string> list = TextFormatter.Format (text, maxWidth, alignment, wrap, preserveTrailingSpaces);
        Assert.Equal (list.Count, resultLines.Count ());
        Assert.Equal (resultLines, list);
        var wrappedText = string.Empty;

        foreach (string txt in list)
        {
            wrappedText += txt;
        }

        Assert.Equal (expectedWrappedText, wrappedText);
    }

    public static IEnumerable<object []> FormatEnvironmentNewLine =>
        new List<object []>
        {
            new object []
            {
                $"Line1{Environment.NewLine}Line2{Environment.NewLine}Line3{Environment.NewLine}",
                60,
                new [] { "Line1", "Line2", "Line3" }
            }
        };

    [Theory]
    [InlineData ("Hello World", 11)]
    [InlineData ("こんにちは世界", 14)]
    public void GetColumns_Simple_And_Wide_Runes (string text, int width) { Assert.Equal (width, text.GetColumns ()); }

    [Theory]
    [InlineData (new [] { "0123456789" }, 1)]
    [InlineData (new [] { "Hello World" }, 1)]
    [InlineData (new [] { "Hello", "World" }, 2)]
    [InlineData (new [] { "こんにちは", "世界" }, 4)]
    public void GetColumnsRequiredForVerticalText_List_GetsWidth (IEnumerable<string> text, int expectedWidth)
    {
        Assert.Equal (expectedWidth, TextFormatter.GetColumnsRequiredForVerticalText (text.ToList ()));
    }

    [Theory]
    [InlineData (new [] { "Hello World" }, 1, 0, 1, 1)]
    [InlineData (new [] { "Hello", "World" }, 2, 1, 1, 1)]
    [InlineData (new [] { "こんにちは", "世界" }, 4, 1, 1, 2)]
    public void GetColumnsRequiredForVerticalText_List_Simple_And_Wide_Runes (
        IEnumerable<string> text,
        int expectedWidth,
        int index,
        int length,
        int expectedIndexWidth
    )
    {
        Assert.Equal (expectedWidth, TextFormatter.GetColumnsRequiredForVerticalText (text.ToList ()));
        Assert.Equal (expectedIndexWidth, TextFormatter.GetColumnsRequiredForVerticalText (text.ToList (), index, length));
    }

    [Fact]
    public void GetColumnsRequiredForVerticalText_List_With_Combining_Runes ()
    {
        List<string> text = new () { "Les Mis", "e\u0328\u0301", "rables" };
        Assert.Equal (1, TextFormatter.GetColumnsRequiredForVerticalText (text, 1, 1));
    }

    [Theory]
    [InlineData ("Hello World", 6, 6)]
    [InlineData ("こんにちは 世界", 6, 3)]
    [MemberData (nameof (CMGlyphs))]
    public void GetLengthThatFits_List_Simple_And_Wide_Runes (string text, int columns, int expectedLength)
    {
        List<Rune> runes = text.ToRuneList ();
        Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (runes, columns));
    }

    [Theory]
    [InlineData ("test", 3, 3)]
    [InlineData ("test", 4, 4)]
    [InlineData ("test", 10, 4)]
    public void GetLengthThatFits_Runelist (string text, int columns, int expectedLength)
    {
        List<Rune> runes = text.ToRuneList ();

        Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (runes, columns));
    }

    [Theory]
    [InlineData ("Hello World", 6, 6)]
    [InlineData ("こんにちは 世界", 6, 3)]
    public void GetLengthThatFits_Simple_And_Wide_Runes (string text, int columns, int expectedLength)
    {
        Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (text, columns));
    }

    [Theory]
    [InlineData ("test", 3, 3)]
    [InlineData ("test", 4, 4)]
    [InlineData ("test", 10, 4)]
    [InlineData ("test", 1, 1)]
    [InlineData ("test", 0, 0)]
    [InlineData ("test", -1, 0)]
    [InlineData (null, -1, 0)]
    [InlineData ("", -1, 0)]
    public void GetLengthThatFits_String (string text, int columns, int expectedLength)
    {
        Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (text, columns));
    }

    [Fact]
    public void GetLengthThatFits_With_Combining_Runes ()
    {
        var text = "Les Mise\u0328\u0301rables";
        Assert.Equal (16, TextFormatter.GetLengthThatFits (text, 14));
    }

    [Fact]
    public void GetMaxColsForWidth_With_Combining_Runes ()
    {
        List<string> text = new () { "Les Mis", "e\u0328\u0301", "rables" };
        Assert.Equal (1, TextFormatter.GetMaxColsForWidth (text, 1));
    }

    //[Fact]
    //public void GetWidestLineLength_With_Combining_Runes ()
    //{
    //    var text = "Les Mise\u0328\u0301rables";
    //    Assert.Equal (1, TextFormatter.GetWidestLineLength (text, 1, 1));
    //}

    [Fact]
    public void Internal_Tests ()
    {
        var tf = new TextFormatter ();
        Assert.Equal (KeyCode.Null, tf.HotKey);
        tf.HotKey = KeyCode.CtrlMask | KeyCode.Q;
        Assert.Equal (KeyCode.CtrlMask | KeyCode.Q, tf.HotKey);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [Theory]
    [InlineData ("")]
    [InlineData (null)]
    [InlineData ("test")]
    public void Justify_Invalid (string text)
    {
        Assert.Equal (text, TextFormatter.Justify (text, 0));
        Assert.Equal (text, TextFormatter.Justify (text, 0));
        Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.Justify (text, -1));
    }

    [Theory]

    // Even # of spaces
    //            0123456789
    [InlineData ("012 456 89", "012 456 89", 10, 0, "+", true)]
    [InlineData ("012 456 89", "012++456+89", 11, 1)]
    [InlineData ("012 456 89", "012 456 89", 12, 2, "++", true)]
    [InlineData ("012 456 89", "012+++456++89", 13, 3)]
    [InlineData ("012 456 89", "012 456 89", 14, 4, "+++", true)]
    [InlineData ("012 456 89", "012++++456+++89", 15, 5)]
    [InlineData ("012 456 89", "012 456 89", 16, 6, "++++", true)]
    [InlineData ("012 456 89", "012 456 89", 30, 20, "+++++++++++", true)]
    [InlineData ("012 456 89", "012+++++++++++++456++++++++++++89", 33, 23)]

    // Odd # of spaces
    //            01234567890123
    [InlineData ("012 456 89 end", "012 456 89 end", 14, 0, "+", true)]
    [InlineData ("012 456 89 end", "012++456+89+end", 15, 1)]
    [InlineData ("012 456 89 end", "012++456++89+end", 16, 2)]
    [InlineData ("012 456 89 end", "012 456 89 end", 17, 3, "++", true)]
    [InlineData ("012 456 89 end", "012+++456++89++end", 18, 4)]
    [InlineData ("012 456 89 end", "012+++456+++89++end", 19, 5)]
    [InlineData ("012 456 89 end", "012 456 89 end", 20, 6, "+++", true)]
    [InlineData ("012 456 89 end", "012++++++++456++++++++89+++++++end", 34, 20)]
    [InlineData ("012 456 89 end", "012+++++++++456+++++++++89++++++++end", 37, 23)]

    // Unicode
    // Even # of chars
    //            0123456789
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 10, 0, "+", true)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ++Ð²Ð+Ñ", 11, 1)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 12, 2, "++", true)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ+++Ð²Ð++Ñ", 13, 3)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 14, 4, "+++", true)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ++++Ð²Ð+++Ñ", 15, 5)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 16, 6, "++++", true)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 30, 20, "+++++++++++", true)]
    [InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ+++++++++++++Ð²Ð++++++++++++Ñ", 33, 23)]

    // Unicode
    // Odd # of chars
    //            0123456789
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð ÑÐ Ð²Ð Ñ", 10, 0, "+", true)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð++ÑÐ+Ð²Ð+Ñ", 11, 1)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð++ÑÐ++Ð²Ð+Ñ", 12, 2)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð ÑÐ Ð²Ð Ñ", 13, 3, "++", true)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð+++ÑÐ++Ð²Ð++Ñ", 14, 4)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð+++ÑÐ+++Ð²Ð++Ñ", 15, 5)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð ÑÐ Ð²Ð Ñ", 16, 6, "+++", true)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð++++++++ÑÐ++++++++Ð²Ð+++++++Ñ", 30, 20)]
    [InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð+++++++++ÑÐ+++++++++Ð²Ð++++++++Ñ", 33, 23)]
    public void Justify_Sentence (
        string text,
        string justifiedText,
        int forceToWidth,
        int widthOffset,
        string replaceWith = null,
        bool replace = false
    )
    {
        var fillChar = '+';

        Assert.Equal (forceToWidth, text.GetRuneCount () + widthOffset);

        if (replace)
        {
            justifiedText = text.Replace (" ", replaceWith);
        }

        Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
        Assert.True (Math.Abs (forceToWidth - justifiedText.GetRuneCount ()) < text.Count (s => s == ' '));
        Assert.True (Math.Abs (forceToWidth - justifiedText.GetColumns ()) < text.Count (s => s == ' '));
    }

    [Theory]
    [InlineData ("word")] // Even # of chars
    [InlineData ("word.")] // Odd # of chars
    [InlineData ("Ð¿ÑÐ¸Ð²ÐµÑ")] // Unicode (even #)
    [InlineData ("Ð¿ÑÐ¸Ð²ÐµÑ.")] // Unicode (odd # of chars)
    public void Justify_SingleWord (string text)
    {
        string justifiedText = text;
        var fillChar = '+';

        int width = text.GetRuneCount ();
        Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
        width = text.GetRuneCount () + 1;
        Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
        width = text.GetRuneCount () + 2;
        Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
        width = text.GetRuneCount () + 10;
        Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
        width = text.GetRuneCount () + 11;
        Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
    }

    [SetupFakeDriver]
    [Theory]
    [InlineData ("Hello World", 1, 15, "H\ne\nl\nl\no\n \n \n \n \n \nW\no\nr\nl\nd")]
    [InlineData (
                    "Well Done\nNice Work",
                    2,
                    15,
                    @"
WN
ei
lc
le
  
  
  
  
  
  
  
DW
oo
nr
ek")]
    [InlineData ("你好 世界", 2, 15, "你\n好\n  \n  \n  \n  \n  \n  \n  \n  \n  \n  \n  \n世\n界")]
    [InlineData (
                    "做 得好\n幹 得好",
                    4,
                    15,
                    @"
做幹
    
    
    
    
    
    
    
    
    
    
    
    
得得
好好")]
    public void Justify_Vertical (string text, int width, int height, string expectedText)
    {
        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.TopBottom_LeftRight,
            VerticalAlignment = Alignment.Fill,
            ConstrainToSize = new Size (width, height),
            MultiLine = true
        };

        tf.Draw (new (0, 0, width, height), Attribute.Default, Attribute.Default);

        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
    }

    [Theory]
    [InlineData ("Single Line 界", 14)]
    [InlineData ("First Line 界\nSecond Line 界\nThird Line 界\n", 14)]
    public void MaxWidthLine_With_And_Without_Newlines (string text, int expected) { Assert.Equal (expected, TextFormatter.GetWidestLineLength (text)); }

    [Theory]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    0,
                    false,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    1,
                    false,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    1,
                    0,
                    false,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    0,
                    true,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    1,
                    true,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    1,
                    0,
                    true,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    6,
                    5,
                    false,
                    new [] { "First " }
                )]
    [InlineData ("1\n2\n3\n4\n5\n6", 6, 5, false, new [] { "1 2 3 " })]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    6,
                    5,
                    true,
                    new [] { "First ", "Second", "Third ", "Forty ", "Fiftee" }
                )]
    [InlineData ("第一行\n第二行\n第三行\n四十行\n第十五行\n七十行", 5, 5, false, new [] { "第一" })]
    [InlineData ("第一行\n第二行\n第三行\n四十行\n第十五行\n七十行", 5, 5, true, new [] { "第一", "第二", "第三", "四十", "第十" })]
    public void MultiLine_WordWrap_False_Horizontal_Direction (
        string text,
        int maxWidth,
        int maxHeight,
        bool multiLine,
        IEnumerable<string> resultLines
    )
    {
        var tf = new TextFormatter
        {
            Text = text, ConstrainToSize = new (maxWidth, maxHeight), WordWrap = false, MultiLine = multiLine
        };

        Assert.False (tf.WordWrap);
        Assert.True (tf.MultiLine == multiLine);
        Assert.Equal (TextDirection.LeftRight_TopBottom, tf.Direction);

        List<string> splitLines = tf.GetLines ();
        Assert.Equal (splitLines.Count, resultLines.Count ());
        Assert.Equal (splitLines, resultLines);
    }

    [Theory]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    0,
                    false,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    1,
                    false,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    1,
                    0,
                    false,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    0,
                    true,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    0,
                    1,
                    true,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    1,
                    0,
                    true,
                    new [] { "" }
                )]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    6,
                    5,
                    false,
                    new [] { "First" }
                )]
    [InlineData ("1\n2\n3\n4\n5\n6", 6, 5, false, new [] { "1 2 3" })]
    [InlineData (
                    "First Line\nSecond Line\nThird Line\nForty Line\nFifteenth Line\nSeventy Line",
                    6,
                    5,
                    true,
                    new [] { "First", "Secon", "Third", "Forty", "Fifte", "Seven" }
                )]
    [InlineData ("第一行\n第二行\n第三行\n四十行\n第十五行\n七十行", 5, 5, false, new [] { "第一行 第" })]
    [InlineData ("第一行\n第二行\n第三行\n四十行\n第十五行\n七十行", 5, 5, true, new [] { "第一行", "第二行" })]
    public void MultiLine_WordWrap_False_Vertical_Direction (
        string text,
        int maxWidth,
        int maxHeight,
        bool multiLine,
        IEnumerable<string> resultLines
    )
    {
        var tf = new TextFormatter
        {
            Text = text,
            ConstrainToSize = new (maxWidth, maxHeight),
            WordWrap = false,
            MultiLine = multiLine,
            Direction = TextDirection.TopBottom_LeftRight
        };

        Assert.False (tf.WordWrap);
        Assert.True (tf.MultiLine == multiLine);
        Assert.Equal (TextDirection.TopBottom_LeftRight, tf.Direction);

        List<string> splitLines = tf.GetLines ();
        Assert.Equal (splitLines.Count, resultLines.Count ());
        Assert.Equal (splitLines, resultLines);
    }

    [Fact]
    public void NeedsFormat_Sets ()
    {
        var testText = "test";
        var testBounds = new Rectangle (0, 0, 100, 1);
        var tf = new TextFormatter ();

        tf.Text = "test";
        Assert.True (tf.NeedsFormat); // get_Lines causes a Format
        Assert.NotEmpty (tf.GetLines ());
        Assert.False (tf.NeedsFormat); // get_Lines causes a Format
        Assert.Equal (testText, tf.Text);
        tf.Draw (testBounds, new (), new ());
        Assert.False (tf.NeedsFormat);

        tf.ConstrainToSize = new (1, 1);
        Assert.True (tf.NeedsFormat);
        Assert.NotEmpty (tf.GetLines ());
        Assert.False (tf.NeedsFormat); // get_Lines causes a Format

        tf.Alignment = Alignment.Center;
        Assert.True (tf.NeedsFormat);
        Assert.NotEmpty (tf.GetLines ());
        Assert.False (tf.NeedsFormat); // get_Lines causes a Format
    }

    // Test that changing TextFormatter does not impact View dimensions if Dim.Auto is not in play
    [Fact]
    public void Not_Used_TextFormatter_Does_Not_Change_View_Size ()
    {
        View view = new ()
        {
            Text = "_1234"
        };
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.Text = "ABC";
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.Alignment = Alignment.Fill;
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.VerticalAlignment = Alignment.Center;
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.HotKeySpecifier = (Rune)'*';
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.Text = "*ABC";
        Assert.Equal (Size.Empty, view.Frame.Size);
    }

    [Fact]
    public void Not_Used_TextSettings_Do_Not_Change_View_Size ()
    {
        View view = new ()
        {
            Text = "_1234"
        };
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextAlignment = Alignment.Fill;
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.VerticalTextAlignment = Alignment.Center;
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.HotKeySpecifier = (Rune)'*';
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.Text = "*ABC";
        Assert.Equal (Size.Empty, view.Frame.Size);
    }

    [Theory]
    [InlineData ("", -1, Alignment.Start, false, 0)]
    [InlineData (null, 0, Alignment.Start, false, 1)]
    [InlineData (null, 0, Alignment.Start, true, 1)]
    [InlineData ("", 0, Alignment.Start, false, 1)]
    [InlineData ("", 0, Alignment.Start, true, 1)]
    public void Reformat_Invalid (string text, int maxWidth, Alignment alignment, bool wrap, int linesCount)
    {
        if (maxWidth < 0)
        {
            Assert.Throws<ArgumentOutOfRangeException> (
                                                        () =>
                                                            TextFormatter.Format (text, maxWidth, alignment, wrap)
                                                       );
        }
        else
        {
            List<string> list = TextFormatter.Format (text, maxWidth, alignment, wrap);
            Assert.NotEmpty (list);
            Assert.True (list.Count == linesCount);
            Assert.Equal (string.Empty, list [0]);
        }
    }

    [Theory]
    [InlineData ("A sentence has words.\nLine 2.", 0, -29, Alignment.Start, false, 1, true)]
    [InlineData ("A sentence has words.\nLine 2.", 1, -28, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.\nLine 2.", 5, -24, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.\nLine 2.", 28, -1, Alignment.Start, false, 1, false)]

    // no clip
    [InlineData ("A sentence has words.\nLine 2.", 29, 0, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.\nLine 2.", 30, 1, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.\r\nLine 2.", 0, -30, Alignment.Start, false, 1, true)]
    [InlineData ("A sentence has words.\r\nLine 2.", 1, -29, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.\r\nLine 2.", 5, -25, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.\r\nLine 2.", 29, -1, Alignment.Start, false, 1, false, 1)]
    [InlineData ("A sentence has words.\r\nLine 2.", 30, 0, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.\r\nLine 2.", 31, 1, Alignment.Start, false, 1, false)]
    public void Reformat_NoWordrap_NewLines_MultiLine_False (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        int linesCount,
        bool stringEmpty,
        int clipWidthOffset = 0
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth) + clipWidthOffset;
        List<string> list = TextFormatter.Format (text, maxWidth, alignment, wrap);
        Assert.NotEmpty (list);
        Assert.True (list.Count == linesCount);

        if (stringEmpty)
        {
            Assert.Equal (string.Empty, list [0]);
        }
        else
        {
            Assert.NotEqual (string.Empty, list [0]);
        }

        if (text.Contains ("\r\n") && maxWidth > 0)
        {
            Assert.Equal (
                          StringExtensions.ToString (text.ToRunes () [..expectedClippedWidth])
                                          .Replace ("\r\n", " "),
                          list [0]
                         );
        }
        else if (text.Contains ('\n') && maxWidth > 0)
        {
            Assert.Equal (
                          StringExtensions.ToString (text.ToRunes () [..expectedClippedWidth])
                                          .Replace ("\n", " "),
                          list [0]
                         );
        }
        else
        {
            Assert.Equal (StringExtensions.ToString (text.ToRunes () [..expectedClippedWidth]), list [0]);
        }
    }

    [Theory]
    [InlineData ("A sentence has words.\nLine 2.", 0, -29, Alignment.Start, false, 1, true, new [] { "" })]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    1,
                    -28,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A", "L" }
                )]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    5,
                    -24,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sen", "Line " }
                )]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    28,
                    -1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    //// no clip
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    29,
                    0,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    30,
                    1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData ("A sentence has words.\r\nLine 2.", 0, -30, Alignment.Start, false, 1, true, new [] { "" })]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    1,
                    -29,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A", "L" }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    5,
                    -25,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sen", "Line " }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    29,
                    -1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    30,
                    0,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    31,
                    1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    public void Reformat_NoWordrap_NewLines_MultiLine_True (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        int linesCount,
        bool stringEmpty,
        IEnumerable<string> resultLines
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);

        List<string> list = TextFormatter.Format (
                                                  text,
                                                  maxWidth,
                                                  alignment,
                                                  wrap,
                                                  false,
                                                  0,
                                                  TextDirection.LeftRight_TopBottom,
                                                  true
                                                 );
        Assert.NotEmpty (list);
        Assert.True (list.Count == linesCount);

        if (stringEmpty)
        {
            Assert.Equal (string.Empty, list [0]);
        }
        else
        {
            Assert.NotEqual (string.Empty, list [0]);
        }

        Assert.Equal (list, resultLines);
    }

    [Theory]
    [InlineData ("A sentence has words.\nLine 2.", 0, -29, Alignment.Start, false, 1, true, new [] { "" })]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    1,
                    -28,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A", "L" }
                )]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    5,
                    -24,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sen", "Line " }
                )]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    28,
                    -1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    //// no clip
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    29,
                    0,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData (
                    "A sentence has words.\nLine 2.",
                    30,
                    1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData ("A sentence has words.\r\nLine 2.", 0, -30, Alignment.Start, false, 1, true, new [] { "" })]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    1,
                    -29,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A", "L" }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    5,
                    -25,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sen", "Line " }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    29,
                    -1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    30,
                    0,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    [InlineData (
                    "A sentence has words.\r\nLine 2.",
                    31,
                    1,
                    Alignment.Start,
                    false,
                    2,
                    false,
                    new [] { "A sentence has words.", "Line 2." }
                )]
    public void Reformat_NoWordrap_NewLines_MultiLine_True_Vertical (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        int linesCount,
        bool stringEmpty,
        IEnumerable<string> resultLines
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);

        List<string> list = TextFormatter.Format (
                                                  text,
                                                  maxWidth,
                                                  alignment,
                                                  wrap,
                                                  false,
                                                  0,
                                                  TextDirection.TopBottom_LeftRight,
                                                  true
                                                 );
        Assert.NotEmpty (list);
        Assert.True (list.Count == linesCount);

        if (stringEmpty)
        {
            Assert.Equal (string.Empty, list [0]);
        }
        else
        {
            Assert.NotEqual (string.Empty, list [0]);
        }

        Assert.Equal (list, resultLines);
    }

    [Theory]
    [InlineData ("", 0, 0, Alignment.Start, false, 1, true)]
    [InlineData ("", 1, 1, Alignment.Start, false, 1, true)]
    [InlineData ("A sentence has words.", 0, -21, Alignment.Start, false, 1, true)]
    [InlineData ("A sentence has words.", 1, -20, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.", 5, -16, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.", 20, -1, Alignment.Start, false, 1, false)]

    // no clip
    [InlineData ("A sentence has words.", 21, 0, Alignment.Start, false, 1, false)]
    [InlineData ("A sentence has words.", 22, 1, Alignment.Start, false, 1, false)]
    public void Reformat_NoWordrap_SingleLine (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        int linesCount,
        bool stringEmpty
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        List<string> list = TextFormatter.Format (text, maxWidth, alignment, wrap);
        Assert.NotEmpty (list);
        Assert.True (list.Count == linesCount);

        if (stringEmpty)
        {
            Assert.Equal (string.Empty, list [0]);
        }
        else
        {
            Assert.NotEqual (string.Empty, list [0]);
        }

        Assert.Equal (StringExtensions.ToString (text.ToRunes () [..expectedClippedWidth]), list [0]);
    }

    [Theory]

    // Unicode
    [InlineData (
                    "\u2460\u2461\u2462\n\u2460\u2461\u2462\u2463\u2464",
                    8,
                    -1,
                    Alignment.Start,
                    true,
                    false,
                    new [] { "\u2460\u2461\u2462", "\u2460\u2461\u2462\u2463\u2464" }
                )]

    // no clip
    [InlineData (
                    "\u2460\u2461\u2462\n\u2460\u2461\u2462\u2463\u2464",
                    9,
                    0,
                    Alignment.Start,
                    true,
                    false,
                    new [] { "\u2460\u2461\u2462", "\u2460\u2461\u2462\u2463\u2464" }
                )]
    [InlineData (
                    "\u2460\u2461\u2462\n\u2460\u2461\u2462\u2463\u2464",
                    10,
                    1,
                    Alignment.Start,
                    true,
                    false,
                    new [] { "\u2460\u2461\u2462", "\u2460\u2461\u2462\u2463\u2464" }
                )]
    public void Reformat_Unicode_Wrap_Spaces_NewLines (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        bool preserveTrailingSpaces,
        IEnumerable<string> resultLines
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        List<string> list = TextFormatter.Format (text, maxWidth, alignment, wrap, preserveTrailingSpaces);
        Assert.Equal (list.Count, resultLines.Count ());
        Assert.Equal (resultLines, list);
    }

    [Theory]

    // Unicode
    // Even # of chars
    //       0123456789
    [InlineData ("\u2660Ð¿ÑÐ Ð²Ð Ñ", 10, -1, Alignment.Start, true, false, new [] { "\u2660Ð¿ÑÐ Ð²Ð", "Ñ" })]

    // no clip
    [InlineData ("\u2660Ð¿ÑÐ Ð²Ð Ñ", 11, 0, Alignment.Start, true, false, new [] { "\u2660Ð¿ÑÐ Ð²Ð Ñ" })]
    [InlineData ("\u2660Ð¿ÑÐ Ð²Ð Ñ", 12, 1, Alignment.Start, true, false, new [] { "\u2660Ð¿ÑÐ Ð²Ð Ñ" })]

    // Unicode
    // Odd # of chars
    //            0123456789
    [InlineData ("\u2660 ÑÐ Ð²Ð Ñ", 9, -1, Alignment.Start, true, false, new [] { "\u2660 ÑÐ Ð²Ð", "Ñ" })]

    // no clip
    [InlineData ("\u2660 ÑÐ Ð²Ð Ñ", 10, 0, Alignment.Start, true, false, new [] { "\u2660 ÑÐ Ð²Ð Ñ" })]
    [InlineData ("\u2660 ÑÐ Ð²Ð Ñ", 11, 1, Alignment.Start, true, false, new [] { "\u2660 ÑÐ Ð²Ð Ñ" })]
    public void Reformat_Unicode_Wrap_Spaces_No_NewLines (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        bool preserveTrailingSpaces,
        IEnumerable<string> resultLines
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        List<string> list = TextFormatter.Format (text, maxWidth, alignment, wrap, preserveTrailingSpaces);
        Assert.Equal (list.Count, resultLines.Count ());
        Assert.Equal (resultLines, list);
    }

    [Theory]

    // Even # of spaces
    //            0123456789
    [InlineData ("012 456 89", 0, -10, Alignment.Start, true, true, true, new [] { "" })]
    [InlineData (
                    "012 456 89",
                    1,
                    -9,
                    Alignment.Start,
                    true,
                    true,
                    false,
                    new [] { "0", "1", "2", " ", "4", "5", "6", " ", "8", "9" },
                    "01245689"
                )]
    [InlineData ("012 456 89", 5, -5, Alignment.Start, true, true, false, new [] { "012 ", "456 ", "89" })]
    [InlineData ("012 456 89", 9, -1, Alignment.Start, true, true, false, new [] { "012 456 ", "89" })]

    // no clip
    [InlineData ("012 456 89", 10, 0, Alignment.Start, true, true, false, new [] { "012 456 89" })]
    [InlineData ("012 456 89", 11, 1, Alignment.Start, true, true, false, new [] { "012 456 89" })]

    // Odd # of spaces
    //            01234567890123
    [InlineData ("012 456 89 end", 13, -1, Alignment.Start, true, true, false, new [] { "012 456 89 ", "end" })]

    // no clip
    [InlineData ("012 456 89 end", 14, 0, Alignment.Start, true, true, false, new [] { "012 456 89 end" })]
    [InlineData ("012 456 89 end", 15, 1, Alignment.Start, true, true, false, new [] { "012 456 89 end" })]
    public void Reformat_Wrap_Spaces_No_NewLines (
        string text,
        int maxWidth,
        int widthOffset,
        Alignment alignment,
        bool wrap,
        bool preserveTrailingSpaces,
        bool stringEmpty,
        IEnumerable<string> resultLines,
        string noSpaceText = ""
    )
    {
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        List<string> list = TextFormatter.Format (text, maxWidth, alignment, wrap, preserveTrailingSpaces);
        Assert.NotEmpty (list);
        Assert.True (list.Count == resultLines.Count ());

        if (stringEmpty)
        {
            Assert.Equal (string.Empty, list [0]);
        }
        else
        {
            Assert.NotEqual (string.Empty, list [0]);
        }

        Assert.Equal (resultLines, list);

        if (maxWidth > 0)
        {
            // remove whitespace chars
            if (maxWidth < 5)
            {
                expectedClippedWidth = text.GetRuneCount () - text.Sum (r => r == ' ' ? 1 : 0);
            }
            else
            {
                expectedClippedWidth = Math.Min (
                                                 text.GetRuneCount (),
                                                 maxWidth - text.Sum (r => r == ' ' ? 1 : 0)
                                                );
            }

            list = TextFormatter.Format (text, maxWidth, Alignment.Start, wrap);

            if (maxWidth == 1)
            {
                Assert.Equal (expectedClippedWidth, list.Count);
                Assert.Equal (noSpaceText, string.Concat (list.ToArray ()));
            }

            if (maxWidth > 1 && maxWidth < 10)
            {
                Assert.Equal (
                              StringExtensions.ToString (text.ToRunes () [..expectedClippedWidth]),
                              list [0]
                             );
            }
        }
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    [InlineData ("a")]
    public void RemoveHotKeySpecifier_InValid_ReturnsOriginal (string text)
    {
        var hotKeySpecifier = (Rune)'_';

        if (text == null)
        {
            Assert.Null (TextFormatter.RemoveHotKeySpecifier (text, 0, hotKeySpecifier));
            Assert.Null (TextFormatter.RemoveHotKeySpecifier (text, -1, hotKeySpecifier));
            Assert.Null (TextFormatter.RemoveHotKeySpecifier (text, 100, hotKeySpecifier));
        }
        else
        {
            Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, 0, hotKeySpecifier));
            Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, -1, hotKeySpecifier));
            Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, 100, hotKeySpecifier));
        }
    }

    [Theory]
    [InlineData ("all lower case", 0)]
    [InlineData ("K Before", 0)]
    [InlineData ("aK Second", 1)]
    [InlineData ("Last K", 5)]
    [InlineData ("fter K", 7)]
    [InlineData ("Multiple K and R", 9)]
    [InlineData ("Non-english: Кдать", 13)]
    public void RemoveHotKeySpecifier_Valid_Legacy_ReturnsOriginal (string text, int hotPos)
    {
        var hotKeySpecifier = (Rune)'_';

        Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, hotPos, hotKeySpecifier));
    }

    [Theory]
    [InlineData ("_K Before", 0, "K Before")]
    [InlineData ("a_K Second", 1, "aK Second")]
    [InlineData ("Last _K", 5, "Last K")]
    [InlineData ("After K_", 7, "After K")]
    [InlineData ("Multiple _K and _R", 9, "Multiple K and _R")]
    [InlineData ("Non-english: _Кдать", 13, "Non-english: Кдать")]
    public void RemoveHotKeySpecifier_Valid_ReturnsStripped (string text, int hotPos, string expectedText)
    {
        var hotKeySpecifier = (Rune)'_';

        Assert.Equal (expectedText, TextFormatter.RemoveHotKeySpecifier (text, hotPos, hotKeySpecifier));
    }

    [Theory]
    [InlineData ("test", 0, 't', "test")]
    [InlineData ("test", 1, 'e', "test")]
    [InlineData ("Ok", 0, 'O', "Ok")]
    [InlineData ("[◦ Ok ◦]", 3, 'O', "[◦ Ok ◦]")]
    [InlineData ("^k", 0, '^', "^k")]
    public void ReplaceHotKeyWithTag (string text, int hotPos, uint tag, string expected)
    {
        var tf = new TextFormatter ();
        List<Rune> runes = text.ToRuneList ();
        Rune rune;

        if (Rune.TryGetRuneAt (text, hotPos, out rune))
        {
            Assert.Equal (rune, (Rune)tag);
        }

        string result = TextFormatter.ReplaceHotKeyWithTag (text, hotPos);
        Assert.Equal (result, expected);
        Assert.Equal ((Rune)tag, result.ToRunes () [hotPos]);
        Assert.Equal (text.GetRuneCount (), runes.Count);
        Assert.Equal (text, StringExtensions.ToString (runes));
    }

    public static IEnumerable<object []> SplitEnvironmentNewLine =>
        new List<object []>
        {
            new object []
            {
                $"First Line 界{Environment.NewLine}Second Line 界{Environment.NewLine}Third Line 界",
                new [] { "First Line 界", "Second Line 界", "Third Line 界" }
            },
            new object []
            {
                $"First Line 界{Environment.NewLine}Second Line 界{Environment.NewLine}Third Line 界{Environment.NewLine}",
                new [] { "First Line 界", "Second Line 界", "Third Line 界", "" }
            }
        };

    [Theory]
    [MemberData (nameof (SplitEnvironmentNewLine))]
    public void SplitNewLine_Ending__With_Or_Without_NewLine_Probably_CRLF (
        string text,
        IEnumerable<string> expected
    )
    {
        List<string> splited = TextFormatter.SplitNewLine (text);
        Assert.Equal (expected, splited);
    }

    [Theory]
    [InlineData (
                    "First Line 界\nSecond Line 界\nThird Line 界\n",
                    new [] { "First Line 界", "Second Line 界", "Third Line 界", "" }
                )]
    public void SplitNewLine_Ending_With_NewLine_Only_LF (string text, IEnumerable<string> expected)
    {
        List<string> splited = TextFormatter.SplitNewLine (text);
        Assert.Equal (expected, splited);
    }

    [Theory]
    [InlineData (
                    "First Line 界\nSecond Line 界\nThird Line 界",
                    new [] { "First Line 界", "Second Line 界", "Third Line 界" }
                )]
    public void SplitNewLine_Ending_Without_NewLine_Only_LF (string text, IEnumerable<string> expected)
    {
        List<string> splited = TextFormatter.SplitNewLine (text);
        Assert.Equal (expected, splited);
    }

    [Theory]
    [InlineData ("New Test 你", 10, 10, 20320, 20320, 9, "你")]
    [InlineData ("New Test \U0001d539", 10, 11, 120121, 55349, 9, "𝔹")]
    public void String_Array_Is_Not_Always_Equal_ToRunes_Array (
        string text,
        int runesLength,
        int stringLength,
        int runeValue,
        int stringValue,
        int index,
        string expected
    )
    {
        Rune [] usToRunes = text.ToRunes ();
        Assert.Equal (runesLength, usToRunes.Length);
        Assert.Equal (stringLength, text.Length);
        Assert.Equal (runeValue, usToRunes [index].Value);
        Assert.Equal (stringValue, text [index]);
        Assert.Equal (expected, usToRunes [index].ToString ());

        if (char.IsHighSurrogate (text [index]))
        {
            // Rune array length isn't equal to string array
            Assert.Equal (expected, new (new [] { text [index], text [index + 1] }));
        }
        else
        {
            // Rune array length is equal to string array
            Assert.Equal (expected, text [index].ToString ());
        }
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
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output, driver);

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
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output, driver);

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
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output, driver);

        driver.End ();
    }

    [Theory]
    [InlineData ("123456789", 3, "123")]
    [InlineData ("Hello World", 8, "Hello Wo")]
    public void TestClipOrPad_LongWord (string text, int fillPad, string expectedText)
    {
        // word is long but we want it to fill # space only
        Assert.Equal (expectedText, TextFormatter.ClipOrPad (text, fillPad));
    }

    [Theory]
    [InlineData ("fff", 6, "fff   ")]
    [InlineData ("Hello World", 16, "Hello World     ")]
    public void TestClipOrPad_ShortWord (string text, int fillPad, string expectedText)
    {
        // word is short but we want it to fill # so it should be padded
        Assert.Equal (expectedText, TextFormatter.ClipOrPad (text, fillPad));
    }

    [Theory]
    [InlineData ("你", TextDirection.LeftRight_TopBottom, 2, 1)]
    [InlineData ("你", TextDirection.TopBottom_LeftRight, 2, 1)]
    [InlineData ("你你", TextDirection.LeftRight_TopBottom, 4, 1)]
    [InlineData ("你你", TextDirection.TopBottom_LeftRight, 2, 2)]
    public void Text_Set_SizeIsCorrect (string text, TextDirection textDirection, int expectedWidth, int expectedHeight)
    {
        var tf = new TextFormatter { Direction = textDirection, Text = text };
        tf.ConstrainToWidth = 10;
        tf.ConstrainToHeight = 10;

        Assert.Equal (new (expectedWidth, expectedHeight), tf.FormatAndGetSize ());
    }

    [Fact]
    [SetupFakeDriver]
    public void UICatalog_AboutBox_Text ()
    {
        TextFormatter tf = new ()
        {
            Text = UICatalogApp.GetAboutBoxMessage (),
            Alignment = Alignment.Center,
            VerticalAlignment = Alignment.Start,
            WordWrap = false,
            MultiLine = true,
            HotKeySpecifier = (Rune)0xFFFF
        };

        Size tfSize = tf.FormatAndGetSize ();
        Assert.Equal (new (58, 13), tfSize);

        ((FakeDriver)Application.Driver).SetBufferSize (tfSize.Width, tfSize.Height);

        Application.Driver.FillRect (Application.Screen, (Rune)'*');
        tf.Draw (Application.Screen, Attribute.Default, Attribute.Default);

        var expectedText = """
                           ******UI Catalog: A comprehensive sample library for******
                           **********************************************************
                            _______                  _             _   _____       _ 
                           |__   __|                (_)           | | / ____|     (_)
                              | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ 
                              | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |
                              | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |
                              |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|
                           **********************************************************
                           **********************v2 - Pre-Alpha**********************
                           **********************************************************
                           **********https://github.com/gui-cs/Terminal.Gui**********
                           **********************************************************
                           """;

        TestHelpers.AssertDriverContentsAre (expectedText.ReplaceLineEndings (), _output);
    }

    [Fact]
    public void WordWrap_BigWidth ()
    {
        List<string> wrappedLines;

        var text = "Constantinople";
        wrappedLines = TextFormatter.WordWrapText (text, 100);
        Assert.True (wrappedLines.Count == 1);
        Assert.Equal ("Constantinople", wrappedLines [0]);
    }

    [Fact]
    public void WordWrap_Invalid ()
    {
        var text = string.Empty;
        var width = 0;

        Assert.Empty (TextFormatter.WordWrapText (null, width));
        Assert.Empty (TextFormatter.WordWrapText (text, width));
        Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.WordWrapText (text, -1));
    }

    [Theory]
    [InlineData ("A sentence has words.", 3, -18, new [] { "A", "sen", "ten", "ce", "has", "wor", "ds." })]
    [InlineData (
                    "A sentence has words.",
                    2,
                    -19,
                    new [] { "A", "se", "nt", "en", "ce", "ha", "s", "wo", "rd", "s." }
                )]
    [InlineData (
                    "A sentence has words.",
                    1,
                    -20,
                    new [] { "A", "s", "e", "n", "t", "e", "n", "c", "e", "h", "a", "s", "w", "o", "r", "d", "s", "." }
                )]
    public void WordWrap_Narrow_Default (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        // Calls WordWrapText (text, width) and thus preserveTrailingSpaces defaults to false
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("A sentence has words.", 21, 0, new [] { "A sentence has words." })]
    [InlineData ("A sentence has words.", 20, -1, new [] { "A sentence has", "words." })]
    [InlineData ("A sentence has words.", 15, -6, new [] { "A sentence has", "words." })]
    [InlineData ("A sentence has words.", 14, -7, new [] { "A sentence has", "words." })]
    [InlineData ("A sentence has words.", 13, -8, new [] { "A sentence", "has words." })]

    // Unicode 
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.",
                    42,
                    0,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.",
                    41,
                    -1,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", "words." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.",
                    36,
                    -6,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", "words." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.",
                    35,
                    -7,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", "words." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.",
                    34,
                    -8,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ)", "has words." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.",
                    25,
                    -17,
                    new [] { "A Unicode sentence", "(Ð¿ÑÐ¸Ð²ÐµÑ) has words." }
                )]
    public void WordWrap_NoNewLines_Default (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        // Calls WordWrapText (text, width) and thus preserveTrailingSpaces defaults to false
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("これが最初の行です。 こんにちは世界。 これが2行目です。", 29, 0, new [] { "これが最初の行です。", "こんにちは世界。", "これが2行目です。" })]
    public void WordWrap_PreserveTrailingSpaces_False_Unicode_Wide_Runes (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("文に は言葉 があり ます。", 14, 0, new [] { "文に は言葉", "があり ます。" })]
    [InlineData ("文に は言葉 があり ます。", 3, -11, new [] { "文", "に", "は", "言", "葉", "が", "あ", "り", "ま", "す", "。" })]
    [InlineData ("文に は言葉 があり ます。", 2, -12, new [] { "文", "に", "は", "言", "葉", "が", "あ", "り", "ま", "す", "。" })]
    [InlineData (
                    "文に は言葉 があり ます。",
                    1,
                    -13,
                    new [] { " ", " ", " " }
                )] // Just Spaces; should result in a single space for each line
    public void WordWrap_PreserveTrailingSpaces_False_Wide_Runes (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData (null, 1, new string [] { })] // null input
    [InlineData ("", 1, new string [] { })] // Empty input
    [InlineData ("1 34", 1, new [] { "1", "3", "4" })] // Single Spaces
    [InlineData ("1", 1, new [] { "1" })] // Short input
    [InlineData ("12", 1, new [] { "1", "2" })]
    [InlineData ("123", 1, new [] { "1", "2", "3" })]
    [InlineData ("123456", 1, new [] { "1", "2", "3", "4", "5", "6" })] // No spaces
    [InlineData (" ", 1, new [] { " " })] // Just Spaces; should result in a single space
    [InlineData ("  ", 1, new [] { " " })]
    [InlineData ("   ", 1, new [] { " ", " " })]
    [InlineData ("    ", 1, new [] { " ", " " })]
    [InlineData ("12 456", 1, new [] { "1", "2", "4", "5", "6" })] // Single Spaces
    [InlineData (" 2 456", 1, new [] { " ", "2", "4", "5", "6" })] // Leading spaces should be preserved.
    [InlineData (" 2 456 8", 1, new [] { " ", "2", "4", "5", "6", "8" })]
    [InlineData (
                    "A sentence has words. ",
                    1,
                    new [] { "A", "s", "e", "n", "t", "e", "n", "c", "e", "h", "a", "s", "w", "o", "r", "d", "s", "." }
                )] // Complex example
    [InlineData ("12  567", 1, new [] { "1", "2", " ", "5", "6", "7" })] // Double Spaces
    [InlineData ("  3 567", 1, new [] { " ", "3", "5", "6", "7" })] // Double Leading spaces should be preserved.
    [InlineData ("  3  678  1", 1, new [] { " ", "3", " ", "6", "7", "8", " ", "1" })]
    [InlineData ("1  456", 1, new [] { "1", " ", "4", "5", "6" })]
    [InlineData (
                    "A  sentence   has words.  ",
                    1,
                    new []
                    {
                        "A", " ", "s", "e", "n", "t", "e", "n", "c", "e", " ", "h", "a", "s", "w", "o", "r", "d", "s", ".", " "
                    }
                )] // Double space Complex example
    public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_1 (
        string text,
        int width,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines = TextFormatter.WordWrapText (text, width);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());
        Assert.Equal (resultLines, wrappedLines);
        var breakLines = "";

        foreach (string line in wrappedLines)
        {
            breakLines += $"{line}{Environment.NewLine}";
        }

        var expected = string.Empty;

        foreach (string line in resultLines)
        {
            expected += $"{line}{Environment.NewLine}";
        }

        Assert.Equal (expected, breakLines);
    }

    [Theory]
    [InlineData (null, 3, new string [] { })] // null input
    [InlineData ("", 3, new string [] { })] // Empty input
    [InlineData ("1", 3, new [] { "1" })] // Short input
    [InlineData ("12", 3, new [] { "12" })]
    [InlineData ("123", 3, new [] { "123" })]
    [InlineData ("123456", 3, new [] { "123", "456" })] // No spaces
    [InlineData ("1234567", 3, new [] { "123", "456", "7" })] // No spaces
    [InlineData (" ", 3, new [] { " " })] // Just Spaces; should result in a single space
    [InlineData ("  ", 3, new [] { "  " })]
    [InlineData ("   ", 3, new [] { "   " })]
    [InlineData ("    ", 3, new [] { "   " })]
    [InlineData ("12 456", 3, new [] { "12", "456" })] // Single Spaces
    [InlineData (" 2 456", 3, new [] { " 2", "456" })] // Leading spaces should be preserved.
    [InlineData (" 2 456 8", 3, new [] { " 2", "456", "8" })]
    [InlineData (
                    "A sentence has words. ",
                    3,
                    new [] { "A", "sen", "ten", "ce", "has", "wor", "ds." }
                )] // Complex example
    [InlineData ("12  567", 3, new [] { "12 ", "567" })] // Double Spaces
    [InlineData ("  3 567", 3, new [] { "  3", "567" })] // Double Leading spaces should be preserved.
    [InlineData ("  3  678  1", 3, new [] { "  3", " 67", "8 ", "1" })]
    [InlineData ("1  456", 3, new [] { "1 ", "456" })]
    [InlineData (
                    "A  sentence      has words.  ",
                    3,
                    new [] { "A ", "sen", "ten", "ce ", "   ", "has", "wor", "ds.", " " }
                )] // Double space Complex example
    public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_3 (
        string text,
        int width,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines = TextFormatter.WordWrapText (text, width);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());
        Assert.Equal (resultLines, wrappedLines);
        var breakLines = "";

        foreach (string line in wrappedLines)
        {
            breakLines += $"{line}{Environment.NewLine}";
        }

        var expected = string.Empty;

        foreach (string line in resultLines)
        {
            expected += $"{line}{Environment.NewLine}";
        }

        Assert.Equal (expected, breakLines);
    }

    [Theory]
    [InlineData (null, 50, new string [] { })] // null input
    [InlineData ("", 50, new string [] { })] // Empty input
    [InlineData ("1", 50, new [] { "1" })] // Short input
    [InlineData ("12", 50, new [] { "12" })]
    [InlineData ("123", 50, new [] { "123" })]
    [InlineData ("123456", 50, new [] { "123456" })] // No spaces
    [InlineData ("1234567", 50, new [] { "1234567" })] // No spaces
    [InlineData (" ", 50, new [] { " " })] // Just Spaces; should result in a single space
    [InlineData ("  ", 50, new [] { "  " })]
    [InlineData ("   ", 50, new [] { "   " })]
    [InlineData ("12 456", 50, new [] { "12 456" })] // Single Spaces
    [InlineData (" 2 456", 50, new [] { " 2 456" })] // Leading spaces should be preserved.
    [InlineData (" 2 456 8", 50, new [] { " 2 456 8" })]
    [InlineData ("A sentence has words. ", 50, new [] { "A sentence has words. " })] // Complex example
    [InlineData ("12  567", 50, new [] { "12  567" })] // Double Spaces
    [InlineData ("  3 567", 50, new [] { "  3 567" })] // Double Leading spaces should be preserved.
    [InlineData ("  3  678  1", 50, new [] { "  3  678  1" })]
    [InlineData ("1  456", 50, new [] { "1  456" })]
    [InlineData (
                    "A  sentence      has words.  ",
                    50,
                    new [] { "A  sentence      has words.  " }
                )] // Double space Complex example
    public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_50 (
        string text,
        int width,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines = TextFormatter.WordWrapText (text, width);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());
        Assert.Equal (resultLines, wrappedLines);
        var breakLines = "";

        foreach (string line in wrappedLines)
        {
            breakLines += $"{line}{Environment.NewLine}";
        }

        var expected = string.Empty;

        foreach (string line in resultLines)
        {
            expected += $"{line}{Environment.NewLine}";
        }

        Assert.Equal (expected, breakLines);
    }

    [Theory]
    [InlineData ("A sentence has words.", 14, -7, new [] { "A sentence ", "has words." })]
    [InlineData ("A sentence has words.", 8, -13, new [] { "A ", "sentence", " has ", "words." })]
    [InlineData ("A sentence has words.", 6, -15, new [] { "A ", "senten", "ce ", "has ", "words." })]
    [InlineData ("A sentence has words.", 3, -18, new [] { "A ", "sen", "ten", "ce ", "has", " ", "wor", "ds." })]
    [InlineData (
                    "A sentence has words.",
                    2,
                    -19,
                    new [] { "A ", "se", "nt", "en", "ce", " ", "ha", "s ", "wo", "rd", "s." }
                )]
    [InlineData (
                    "A sentence has words.",
                    1,
                    -20,
                    new []
                    {
                        "A", " ", "s", "e", "n", "t", "e", "n", "c", "e", " ", "h", "a", "s", " ", "w", "o", "r", "d", "s", "."
                    }
                )]
    public void WordWrap_PreserveTrailingSpaces_True (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("文に は言葉 があり ます。", 14, 0, new [] { "文に は言葉 ", "があり ます。" })]
    [InlineData ("文に は言葉 があり ます。", 3, -11, new [] { "文", "に ", "は", "言", "葉 ", "が", "あ", "り ", "ま", "す", "。" })]
    [InlineData (
                    "文に は言葉 があり ます。",
                    2,
                    -12,
                    new [] { "文", "に", " ", "は", "言", "葉", " ", "が", "あ", "り", " ", "ま", "す", "。" }
                )]
    [InlineData ("文に は言葉 があり ます。", 1, -13, new string [] { })]
    public void WordWrap_PreserveTrailingSpaces_True_Wide_Runes (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("A sentence has words. ", 3, new [] { "A ", "sen", "ten", "ce ", "has", " ", "wor", "ds.", " " })]
    [InlineData (
                    "A   sentence          has  words.  ",
                    3,
                    new [] { "A  ", " ", "sen", "ten", "ce ", "   ", "   ", "   ", "has", "  ", "wor", "ds.", "  " }
                )]
    public void WordWrap_PreserveTrailingSpaces_True_With_Simple_Runes_Width_3 (
        string text,
        int width,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines = TextFormatter.WordWrapText (text, width, true);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());
        Assert.Equal (resultLines, wrappedLines);
        var breakLines = "";

        foreach (string line in wrappedLines)
        {
            breakLines += $"{line}{Environment.NewLine}";
        }

        var expected = string.Empty;

        foreach (string line in resultLines)
        {
            expected += $"{line}{Environment.NewLine}";
        }

        Assert.Equal (expected, breakLines);

        // Double space Complex example - this is how VS 2022 does it
        //text = "A  sentence      has words.  ";
        //breakLines = "";
        //wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: true);
        //foreach (var line in wrappedLines) {
        //	breakLines += $"{line}{Environment.NewLine}";
        //}
        //expected = "A  " + Environment.NewLine +
        //	" se" + Environment.NewLine +
        //	" nt" + Environment.NewLine +
        //	" en" + Environment.NewLine +
        //	" ce" + Environment.NewLine +
        //	"  " + Environment.NewLine +
        //	"  " + Environment.NewLine +
        //	"  " + Environment.NewLine +
        //	" ha" + Environment.NewLine +
        //	" s " + Environment.NewLine +
        //	" wo" + Environment.NewLine +
        //	" rd" + Environment.NewLine +
        //	" s." + Environment.NewLine;
        //Assert.Equal (expected, breakLines);
    }

    [Theory]
    [InlineData ("A sentence\t\t\t has words.", 14, -10, new [] { "A sentence\t", "\t\t has ", "words." })]
    [InlineData (
                    "A sentence\t\t\t has words.",
                    8,
                    -16,
                    new [] { "A ", "sentence", "\t\t", "\t ", "has ", "words." }
                )]
    [InlineData (
                    "A sentence\t\t\t has words.",
                    3,
                    -21,
                    new [] { "A ", "sen", "ten", "ce", "\t", "\t", "\t", " ", "has", " ", "wor", "ds." }
                )]
    [InlineData (
                    "A sentence\t\t\t has words.",
                    2,
                    -22,
                    new [] { "A ", "se", "nt", "en", "ce", "\t", "\t", "\t", " ", "ha", "s ", "wo", "rd", "s." }
                )]
    [InlineData (
                    "A sentence\t\t\t has words.",
                    1,
                    -23,
                    new []
                    {
                        "A",
                        " ",
                        "s",
                        "e",
                        "n",
                        "t",
                        "e",
                        "n",
                        "c",
                        "e",
                        "\t",
                        "\t",
                        "\t",
                        " ",
                        "h",
                        "a",
                        "s",
                        " ",
                        "w",
                        "o",
                        "r",
                        "d",
                        "s",
                        "."
                    }
                )]
    public void WordWrap_PreserveTrailingSpaces_True_With_Tab (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines,
        int tabWidth = 4
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true, tabWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("Constantinople", 14, 0, new [] { "Constantinople" })]
    [InlineData ("Constantinople", 12, -2, new [] { "Constantinop", "le" })]
    [InlineData ("Constantinople", 9, -5, new [] { "Constanti", "nople" })]
    [InlineData ("Constantinople", 7, -7, new [] { "Constan", "tinople" })]
    [InlineData ("Constantinople", 5, -9, new [] { "Const", "antin", "ople" })]
    [InlineData ("Constantinople", 4, -10, new [] { "Cons", "tant", "inop", "le" })]
    [InlineData (
                    "Constantinople",
                    1,
                    -13,
                    new [] { "C", "o", "n", "s", "t", "a", "n", "t", "i", "n", "o", "p", "l", "e" }
                )]
    public void WordWrap_SingleWordLine (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("This\u00A0is\n\u00A0a\u00A0sentence.", 20, 0, new [] { "This\u00A0is\u00A0a\u00A0sentence." })]
    [InlineData ("This\u00A0is\n\u00A0a\u00A0sentence.", 19, -1, new [] { "This\u00A0is\u00A0a\u00A0sentence." })]
    [InlineData (
                    "\u00A0\u00A0\u00A0\u00A0\u00A0test\u00A0sentence.",
                    19,
                    0,
                    new [] { "\u00A0\u00A0\u00A0\u00A0\u00A0test\u00A0sentence." }
                )]
    public void WordWrap_Unicode_2LinesWithNonBreakingSpace (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 19, 0, new [] { "This\u00A0is\u00A0a\u00A0sentence." })]
    [InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 18, -1, new [] { "This\u00A0is\u00A0a\u00A0sentence", "." })]
    [InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 17, -2, new [] { "This\u00A0is\u00A0a\u00A0sentenc", "e." })]
    [InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 14, -5, new [] { "This\u00A0is\u00A0a\u00A0sent", "ence." })]
    [InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 10, -9, new [] { "This\u00A0is\u00A0a\u00A0", "sentence." })]
    [InlineData (
                    "This\u00A0is\u00A0a\u00A0sentence.",
                    7,
                    -12,
                    new [] { "This\u00A0is", "\u00A0a\u00A0sent", "ence." }
                )]
    [InlineData (
                    "This\u00A0is\u00A0a\u00A0sentence.",
                    5,
                    -14,
                    new [] { "This\u00A0", "is\u00A0a\u00A0", "sente", "nce." }
                )]
    [InlineData (
                    "This\u00A0is\u00A0a\u00A0sentence.",
                    1,
                    -18,
                    new []
                    {
                        "T", "h", "i", "s", "\u00A0", "i", "s", "\u00A0", "a", "\u00A0", "s", "e", "n", "t", "e", "n", "c", "e", "."
                    }
                )]
    public void WordWrap_Unicode_LineWithNonBreakingSpace (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    [Theory]
    [InlineData (
                    "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ",
                    51,
                    0,
                    new [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ" }
                )]
    [InlineData (
                    "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ",
                    50,
                    -1,
                    new [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ" }
                )]
    [InlineData (
                    "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ",
                    46,
                    -5,
                    new [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮ", "ฯะัาำ" }
                )]
    [InlineData (
                    "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ",
                    26,
                    -25,
                    new [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบ", "ปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ" }
                )]
    [InlineData (
                    "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ",
                    17,
                    -34,
                    new [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑ", "ฒณดตถทธนบปผฝพฟภมย", "รฤลฦวศษสหฬอฮฯะัาำ" }
                )]
    [InlineData (
                    "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ",
                    13,
                    -38,
                    new [] { "กขฃคฅฆงจฉชซฌญ", "ฎฏฐฑฒณดตถทธนบ", "ปผฝพฟภมยรฤลฦว", "ศษสหฬอฮฯะัาำ" }
                )]
    [InlineData (
                    "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ",
                    1,
                    -50,
                    new []
                    {
                        "ก",
                        "ข",
                        "ฃ",
                        "ค",
                        "ฅ",
                        "ฆ",
                        "ง",
                        "จ",
                        "ฉ",
                        "ช",
                        "ซ",
                        "ฌ",
                        "ญ",
                        "ฎ",
                        "ฏ",
                        "ฐ",
                        "ฑ",
                        "ฒ",
                        "ณ",
                        "ด",
                        "ต",
                        "ถ",
                        "ท",
                        "ธ",
                        "น",
                        "บ",
                        "ป",
                        "ผ",
                        "ฝ",
                        "พ",
                        "ฟ",
                        "ภ",
                        "ม",
                        "ย",
                        "ร",
                        "ฤ",
                        "ล",
                        "ฦ",
                        "ว",
                        "ศ",
                        "ษ",
                        "ส",
                        "ห",
                        "ฬ",
                        "อ",
                        "ฮ",
                        "ฯ",
                        "ะั",
                        "า",
                        "ำ"
                    }
                )]
    public void WordWrap_Unicode_SingleWordLine (
        string text,
        int maxWidth,
        int widthOffset,
        IEnumerable<string> resultLines
    )
    {
        List<string> wrappedLines;

        IEnumerable<Rune> zeroWidth = text.EnumerateRunes ().Where (r => r.GetColumns () == 0);
        Assert.Single (zeroWidth);
        Assert.Equal ('ั', zeroWidth.ElementAt (0).Value);
        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth
                     >= (wrappedLines.Count > 0
                             ? wrappedLines.Max (
                                                 l => l.GetRuneCount ()
                                                      + zeroWidth.Count ()
                                                      - 1
                                                      + widthOffset
                                                )
                             : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
    }

    /// <summary>WordWrap strips CRLF</summary>
    [Theory]
    [InlineData (
                    "A sentence has words.\nA paragraph has lines.",
                    44,
                    0,
                    new [] { "A sentence has words.A paragraph has lines." }
                )]
    [InlineData (
                    "A sentence has words.\nA paragraph has lines.",
                    43,
                    -1,
                    new [] { "A sentence has words.A paragraph has lines." }
                )]
    [InlineData (
                    "A sentence has words.\nA paragraph has lines.",
                    38,
                    -6,
                    new [] { "A sentence has words.A paragraph has", "lines." }
                )]
    [InlineData (
                    "A sentence has words.\nA paragraph has lines.",
                    34,
                    -10,
                    new [] { "A sentence has words.A paragraph", "has lines." }
                )]
    [InlineData (
                    "A sentence has words.\nA paragraph has lines.",
                    27,
                    -17,
                    new [] { "A sentence has words.A", "paragraph has lines." }
                )]

    // Unicode 
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.",
                    69,
                    0,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has Линии." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.",
                    68,
                    -1,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has Линии." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.",
                    63,
                    -6,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has", "Линии." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.",
                    59,
                    -10,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт", "has Линии." }
                )]
    [InlineData (
                    "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.",
                    52,
                    -17,
                    new [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode", "Пункт has Линии." }
                )]
    public void WordWrap_WithNewLines (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
    {
        List<string> wrappedLines;

        Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
        int expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
        wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
        Assert.Equal (wrappedLines.Count, resultLines.Count ());

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0)
                    );

        Assert.True (
                     expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0)
                    );
        Assert.Equal (resultLines, wrappedLines);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedDraw, _output);
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

        TestHelpers.AssertDriverContentsWithFrameAre (expectedDraw, _output);
    }

    #endregion
}
