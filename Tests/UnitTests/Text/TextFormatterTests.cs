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

    #endregion
}
