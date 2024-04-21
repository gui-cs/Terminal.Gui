
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Terminal.Gui.DrawingTests;

public class JustifierTests (ITestOutputHelper output)
{

    private readonly ITestOutputHelper _output = output;

    public static IEnumerable<object []> JustificationEnumValues ()
    {
        foreach (var number in Enum.GetValues (typeof (Justification)))
        {
            yield return new object [] { number };
        }
    }

    [Theory]
    [MemberData (nameof (JustificationEnumValues))]
    public void NoItems_Works (Justification justification)
    {
        int [] sizes = { };
        var positions = new Justifier ().Justify (sizes, justification, 100);
        Assert.Equal (new int [] { }, positions);
    }

    [Theory]
    [MemberData (nameof (JustificationEnumValues))]
    public void Items_Width_Cannot_Exceed_TotalSize (Justification justification)
    {
        int [] sizes = { 1000, 2000, 3000 };
        Assert.Throws<ArgumentException> (() => new Justifier ().Justify (sizes, justification, 100));
    }

    [Theory]
    [MemberData (nameof (JustificationEnumValues))]
    public void Negative_Widths_Not_Allowed (Justification justification)
    {
        Assert.Throws<ArgumentException> (() => new Justifier ().Justify (new int [] { -10, 20, 30 }, justification, 100));
        Assert.Throws<ArgumentException> (() => new Justifier ().Justify (new int [] { 10, -20, 30 }, justification, 100));
        Assert.Throws<ArgumentException> (() => new Justifier ().Justify (new int [] { 10, 20, -30 }, justification, 100));
    }

    [Theory]
    [InlineData (Justification.Left, new int [] { 0 }, 1, new int [] { 0 })]
    [InlineData (Justification.Left, new int [] { 0, 0 }, 1, new int [] { 0, 1 })]
    [InlineData (Justification.Left, new int [] { 0, 0, 0 }, 1, new int [] { 0, 1, 1 })]
    [InlineData (Justification.Left, new int [] { 1 }, 1, new int [] { 0 })]
    [InlineData (Justification.Left, new int [] { 1 }, 2, new int [] { 0 })]
    [InlineData (Justification.Left, new int [] { 1 }, 3, new int [] { 0 })]
    [InlineData (Justification.Left, new int [] { 1, 1 }, 2, new int [] { 0, 1 })]
    [InlineData (Justification.Left, new int [] { 1, 1 }, 3, new int [] { 0, 2 })]
    [InlineData (Justification.Left, new int [] { 1, 1 }, 4, new int [] { 0, 2 })]
    [InlineData (Justification.Left, new int [] { 1, 1, 1 }, 3, new int [] { 0, 1, 2 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3 }, 7, new int [] { 0, 2, 4 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3 }, 10, new int [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3 }, 11, new int [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3 }, 12, new int [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3 }, 13, new int [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Left, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 2, 4, 7 })]
    [InlineData (Justification.Left, new int [] { 33, 33, 33 }, 100, new int [] { 0, 34, 67 })]
    [InlineData (Justification.Left, new int [] { 10 }, 101, new int [] { 0 })]
    [InlineData (Justification.Left, new int [] { 10, 20 }, 101, new int [] { 0, 11 })]
    [InlineData (Justification.Left, new int [] { 10, 20, 30 }, 100, new int [] { 0, 11, 32 })]
    [InlineData (Justification.Left, new int [] { 10, 20, 30 }, 101, new int [] { 0, 11, 32 })]
    [InlineData (Justification.Left, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Left, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 11, 31, 61, 101 })]

    [InlineData (Justification.Right, new int [] { 0 }, 1, new int [] { 1 })]
    [InlineData (Justification.Right, new int [] { 0, 0 }, 1, new int [] { 0, 1 })]
    [InlineData (Justification.Right, new int [] { 0, 0, 0 }, 1, new int [] { 0, 1, 1 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3 }, 7, new int [] { 0, 2, 4 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3 }, 10, new int [] { 2, 4, 7 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3 }, 11, new int [] { 3, 5, 8 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3 }, 12, new int [] { 4, 6, 9 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3 }, 13, new int [] { 5, 7, 10 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Right, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 2, 4, 7 })]
    [InlineData (Justification.Right, new int [] { 10, 20, 30 }, 100, new int [] { 38, 49, 70 })]
    [InlineData (Justification.Right, new int [] { 33, 33, 33 }, 100, new int [] { 0, 34, 67 })]
    [InlineData (Justification.Right, new int [] { 10 }, 101, new int [] { 91 })]
    [InlineData (Justification.Right, new int [] { 10, 20 }, 101, new int [] { 70, 81 })]
    [InlineData (Justification.Right, new int [] { 10, 20, 30 }, 101, new int [] { 39, 50, 71 })]
    [InlineData (Justification.Right, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Right, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 11, 31, 61, 101 })]

    [InlineData (Justification.Centered, new int [] { 0 }, 1, new int [] { 0 })]
    [InlineData (Justification.Centered, new int [] { 0, 0 }, 1, new int [] { 0, 1 })]
    [InlineData (Justification.Centered, new int [] { 0, 0, 0 }, 1, new int [] { 0, 1, 1 })]
    [InlineData (Justification.Centered, new int [] { 1 }, 1, new int [] { 0 })]
    [InlineData (Justification.Centered, new int [] { 1 }, 2, new int [] { 0 })]
    [InlineData (Justification.Centered, new int [] { 1 }, 3, new int [] { 1 })]
    [InlineData (Justification.Centered, new int [] { 1, 1 }, 2, new int [] { 0, 1 })]
    [InlineData (Justification.Centered, new int [] { 1, 1 }, 3, new int [] { 0, 2 })]
    [InlineData (Justification.Centered, new int [] { 1, 1 }, 4, new int [] { 0, 2 })]
    [InlineData (Justification.Centered, new int [] { 1, 1, 1 }, 3, new int [] { 0, 1, 2 })]
    [InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    [InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 7, new int [] { 0, 2, 4 })]
    [InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 10, new int [] { 1, 3, 6 })]
    [InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 11, new int [] { 1, 3, 6 })]
    [InlineData (Justification.Centered, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Centered, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 2, 4, 7 })]
    [InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 9, new int [] { 0, 3, 6 })]
    [InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 10, new int [] { 0, 4, 7 })]
    [InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 11, new int [] { 0, 4, 8 })]
    [InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 12, new int [] { 0, 4, 8 })]
    [InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 13, new int [] { 1, 5, 9 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 100, new int [] { 0, 34, 67 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 101, new int [] { 0, 34, 68 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 102, new int [] { 0, 34, 68 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 103, new int [] { 1, 35, 69 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 104, new int [] { 1, 35, 69 })]
    [InlineData (Justification.Centered, new int [] { 10 }, 101, new int [] { 45 })]
    [InlineData (Justification.Centered, new int [] { 10, 20 }, 101, new int [] { 35, 46 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30 }, 100, new int [] { 19, 30, 51 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30 }, 101, new int [] { 19, 30, 51 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30, 40 }, 100, new int [] { 0, 10, 30, 60 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.Centered, new int [] { 3, 4, 5, 6 }, 25, new int [] { 2, 6, 11, 17 })]

    [InlineData (Justification.Justified, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.Justified, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Justified, new int [] { 10, 20, 30 }, 100, new int [] { 0, 30, 70 })]
    [InlineData (Justification.Justified, new int [] { 10, 20, 30 }, 101, new int [] { 0, 31, 71 })]
    [InlineData (Justification.Justified, new int [] { 33, 33, 33 }, 100, new int [] { 0, 34, 67 })]
    [InlineData (Justification.Justified, new int [] { 11, 17, 23 }, 100, new int [] { 0, 36, 77 })]
    [InlineData (Justification.Justified, new int [] { 1, 2, 3 }, 11, new int [] { 0, 4, 8 })]
    [InlineData (Justification.Justified, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    [InlineData (Justification.Justified, new int [] { 10 }, 101, new int [] { 0 })]
    [InlineData (Justification.Justified, new int [] { 3, 3, 3 }, 21, new int [] { 0, 9, 18 })]
    [InlineData (Justification.Justified, new int [] { 3, 4, 5 }, 21, new int [] { 0, 8, 16 })]
    [InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 18, new int [] { 0, 3, 7, 12 })]
    [InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 19, new int [] { 0, 4, 8, 13 })]
    [InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 20, new int [] { 0, 4, 9, 14 })]
    [InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 21, new int [] { 0, 4, 9, 15 })]
    [InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 22, new int [] { 0, 8, 14, 19 })]
    [InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 23, new int [] { 0, 8, 15, 20, })]
    [InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 24, new int [] { 0, 8, 15, 21 })]
    [InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 25, new int [] { 0, 9, 16, 22 })]
    [InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 26, new int [] { 0, 9, 17, 23 })]
    [InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 31, new int [] { 0, 11, 20, 28 })]

    [InlineData (Justification.OneRightRestLeft, new int [] { 0 }, 1, new int [] { 0 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 0, 0 }, 1, new int [] { 0, 1 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 0, 0, 0 }, 1, new int [] { 0, 1, 1 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1 }, 1, new int [] { 0 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1 }, 2, new int [] { 0 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1 }, 3, new int [] { 0 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 1 }, 2, new int [] { 0, 1 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 1 }, 3, new int [] { 0, 2 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 1 }, 4, new int [] { 0, 3 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 1, 1 }, 3, new int [] { 0, 1, 2 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 7, new int [] { 0, 2, 4 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 8, new int [] { 0, 2, 5 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 9, new int [] { 0, 2, 6 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 10, new int [] { 0, 2, 7 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 11, new int [] { 0, 2, 8 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 2, 4, 7 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 3, 3, 3 }, 21, new int [] { 0, 4, 18 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 3, 4, 5 }, 21, new int [] { 0, 4, 16 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 33, 33, 33 }, 100, new int [] { 0, 34, 67 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 10 }, 101, new int [] { 0 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30 }, 100, new int [] { 0, 11, 70 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30 }, 101, new int [] { 0, 11, 71 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 11, 31, 61 })]
    [InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 11, 31, 61, 101 })]

    //[InlineData (Justification.SplitLeft, new int [] { 10, 20, 30 }, 100, new int [] { 0, 49, 70 })]
    //[InlineData (Justification.SplitLeft, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 67 })]
    //[InlineData (Justification.SplitLeft, new int [] { 10 }, 101, new int [] { 0 })]
    //[InlineData (Justification.SplitLeft, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    //[InlineData (Justification.SplitLeft, new int [] { 10, 20, 30 }, 101, new int [] { 0, 50, 71 })]
    //[InlineData (Justification.SplitLeft, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 9, 30, 61 })]
    //[InlineData (Justification.SplitLeft, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 8, 29, 60, 101 })]
    //[InlineData (Justification.SplitLeft, new int [] { 3, 3, 3 }, 21, new int [] { 0, 14, 18 })]
    //[InlineData (Justification.SplitLeft, new int [] { 3, 4, 5 }, 21, new int [] { 0, 11, 16 })]

    public void TestJustifications_1Space (Justification justification, int [] sizes, int totalSize, int [] expected)
    {
        var positions = new Justifier () { MaxSpaceBetweenItems = 1 }.Justify (sizes, justification, totalSize);
        AssertJustification (justification, sizes, totalSize, positions, expected);
    }

    [Theory]
    //[InlineData (Justification.Left, new int [] { 0 }, 1, new int [] { 0 })]
    //[InlineData (Justification.Left, new int [] { 0, 0 }, 1, new int [] { 0, 0 })]
    //[InlineData (Justification.Left, new int [] { 0, 0, 0 }, 1, new int [] { 0, 0, 0 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3 }, 7, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3 }, 10, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3 }, 11, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3 }, 12, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3 }, 13, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    //[InlineData (Justification.Left, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 1, 3, 6 })]
    //[InlineData (Justification.Left, new int [] { 10, 20, 30 }, 100, new int [] { 0, 10, 30 })]
    //[InlineData (Justification.Left, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 66 })]
    //[InlineData (Justification.Left, new int [] { 10 }, 101, new int [] { 0 })]
    //[InlineData (Justification.Left, new int [] { 10, 20 }, 101, new int [] { 0, 10 })]
    //[InlineData (Justification.Left, new int [] { 10, 20, 30 }, 101, new int [] { 0, 10, 30 })]
    //[InlineData (Justification.Left, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 10, 30, 60 })]
    //[InlineData (Justification.Left, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 10, 30, 60, 100 })]

    //[InlineData (Justification.Right, new int [] { 0 }, 1, new int [] { 1 })]
    //[InlineData (Justification.Right, new int [] { 0, 0 }, 1, new int [] { 1, 1 })]
    //[InlineData (Justification.Right, new int [] { 0, 0, 0 }, 1, new int [] { 1, 1, 1 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3 }, 7, new int [] { 1, 2, 4 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3 }, 10, new int [] { 4, 5, 7 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3 }, 11, new int [] { 5, 6, 8 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3 }, 12, new int [] { 6, 7, 9 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3 }, 13, new int [] { 7, 8, 10 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    //[InlineData (Justification.Right, new int [] { 1, 2, 3, 4 }, 11, new int [] { 1, 2, 4, 7 })]
    //[InlineData (Justification.Right, new int [] { 10, 20, 30 }, 100, new int [] { 40, 50, 70 })]
    //[InlineData (Justification.Right, new int [] { 33, 33, 33 }, 100, new int [] { 1, 34, 67 })]
    //[InlineData (Justification.Right, new int [] { 10 }, 101, new int [] { 91 })]
    //[InlineData (Justification.Right, new int [] { 10, 20 }, 101, new int [] { 71, 81 })]
    //[InlineData (Justification.Right, new int [] { 10, 20, 30 }, 101, new int [] { 41, 51, 71 })]
    //[InlineData (Justification.Right, new int [] { 10, 20, 30, 40 }, 101, new int [] { 1, 11, 31, 61 })]
    //[InlineData (Justification.Right, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 1, 11, 31, 61, 101 })]

    //[InlineData (Justification.Centered, new int [] { 1 }, 1, new int [] { 0 })]
    //[InlineData (Justification.Centered, new int [] { 1 }, 2, new int [] { 0 })]
    //[InlineData (Justification.Centered, new int [] { 1 }, 3, new int [] { 1 })]
    //[InlineData (Justification.Centered, new int [] { 1, 1 }, 2, new int [] { 0, 1 })]
    //[InlineData (Justification.Centered, new int [] { 1, 1 }, 3, new int [] { 0, 1 })]
    //[InlineData (Justification.Centered, new int [] { 1, 1 }, 4, new int [] { 1, 2 })]
    //[InlineData (Justification.Centered, new int [] { 1, 1, 1 }, 3, new int [] { 0, 1, 2 })]
    //[InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 7, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 10, new int [] { 2, 3, 5 })]
    //[InlineData (Justification.Centered, new int [] { 1, 2, 3 }, 11, new int [] { 2, 3, 5 })]
    //[InlineData (Justification.Centered, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    //[InlineData (Justification.Centered, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 1, 3, 6 })]
    //[InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 9, new int [] { 0, 3, 6 })]
    //[InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 10, new int [] { 0, 3, 6 })]
    //[InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 11, new int [] { 1, 4, 7 })]
    //[InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 12, new int [] { 1, 4, 7 })]
    //[InlineData (Justification.Centered, new int [] { 3, 3, 3 }, 13, new int [] { 2, 5, 8 })]
    //[InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 66 })]
    //[InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 101, new int [] { 1, 34, 67 })]
    //[InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 102, new int [] { 1, 34, 67 })]
    //[InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 103, new int [] { 2, 35, 68 })]
    //[InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 104, new int [] { 2, 35, 68 })]
    //[InlineData (Justification.Centered, new int [] { 3, 4, 5, 6 }, 25, new int [] { 3, 6, 10, 15 })]

    //[InlineData (Justification.Justified, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 11, 31, 61, 101 })]
    //[InlineData (Justification.Justified, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 11, 31, 61 })]
    //[InlineData (Justification.Justified, new int [] { 10, 20, 30 }, 100, new int [] { 0, 30, 70 })]
    //[InlineData (Justification.Justified, new int [] { 10, 20, 30 }, 101, new int [] { 0, 31, 71 })]
    //[InlineData (Justification.Justified, new int [] { 33, 33, 33 }, 100, new int [] { 0, 34, 67 })]
    //[InlineData (Justification.Justified, new int [] { 11, 17, 23 }, 100, new int [] { 0, 36, 77 })]
    //[InlineData (Justification.Justified, new int [] { 1, 2, 3 }, 11, new int [] { 0, 4, 8 })]
    //[InlineData (Justification.Justified, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    //[InlineData (Justification.Justified, new int [] { 10 }, 101, new int [] { 0 })]
    //[InlineData (Justification.Justified, new int [] { 3, 3, 3 }, 21, new int [] { 0, 9, 18 })]
    //[InlineData (Justification.Justified, new int [] { 3, 4, 5 }, 21, new int [] { 0, 8, 16 })]
    //[InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 18, new int [] { 0, 3, 7, 12 })]
    //[InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 19, new int [] { 0, 4, 8, 13 })]
    //[InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 20, new int [] { 0, 4, 9, 14 })]
    //[InlineData (Justification.Justified, new int [] { 3, 4, 5, 6 }, 21, new int [] { 0, 4, 9, 15 })]
    //[InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 22, new int [] { 0, 8, 14, 19 })]
    //[InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 23, new int [] { 0, 8, 15, 20, })]
    //[InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 24, new int [] { 0, 8, 15, 21 })]
    //[InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 25, new int [] { 0, 9, 16, 22 })]
    //[InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 26, new int [] { 0, 9, 17, 23 })]
    //[InlineData (Justification.Justified, new int [] { 6, 5, 4, 3 }, 31, new int [] { 0, 11, 20, 28 })]

    //[InlineData (Justification.OneRightRestLeft, new int [] { 0 }, 1, new int [] { 0 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 0, 0 }, 1, new int [] { 0, 1 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 0, 0, 0 }, 1, new int [] { 0, 0, 1 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1 }, 1, new int [] { 0 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1 }, 2, new int [] { 0 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1 }, 3, new int [] { 0 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 1 }, 2, new int [] { 0, 1 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 1 }, 3, new int [] { 0, 2 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 1 }, 4, new int [] { 0, 3 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 1, 1 }, 3, new int [] { 0, 1, 2 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 7, new int [] { 0, 1, 4 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 8, new int [] { 0, 1, 5 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 9, new int [] { 0, 1, 6 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 10, new int [] { 0, 1, 7 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3 }, 11, new int [] { 0, 1, 8 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 1, 3, 7 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 1, 2, 3, 4 }, 12, new int [] { 0, 1, 3, 8 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 3, 3, 3 }, 21, new int [] { 0, 3, 18 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 3, 4, 5 }, 21, new int [] { 0, 3, 16 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 67 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 10 }, 101, new int [] { 0 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30 }, 100, new int [] { 0, 10, 70 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30 }, 101, new int [] { 0, 10, 71 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 10, 30, 61 })]
    //[InlineData (Justification.OneRightRestLeft, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 10, 30, 60, 101, })]


    [InlineData (Justification.OneLeftRestRight, new int [] { 0 }, 1, new int [] { 0 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 0, 0 }, 1, new int [] { 0, 1 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 0, 0, 0 }, 1, new int [] { 0, 1, 1 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1 }, 1, new int [] { 0 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1 }, 2, new int [] { 1 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1 }, 3, new int [] { 2 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 1 }, 2, new int [] { 0, 1 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 1 }, 3, new int [] { 0, 2 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 1 }, 4, new int [] { 0, 3 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 1, 1 }, 3, new int [] { 0, 1, 2 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3 }, 6, new int [] { 0, 1, 3 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3 }, 7, new int [] { 0, 2, 4 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3 }, 8, new int [] { 0, 3, 5 })]
    [InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3 }, 9, new int [] { 0, 4, 6 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3 }, 10, new int [] { 0, 1, 7 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3 }, 11, new int [] { 0, 1, 8 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3, 4 }, 10, new int [] { 0, 1, 3, 6 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3, 4 }, 11, new int [] { 0, 1, 3, 7 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 1, 2, 3, 4 }, 12, new int [] { 0, 1, 3, 8 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 3, 3, 3 }, 21, new int [] { 0, 3, 18 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 3, 4, 5 }, 21, new int [] { 0, 3, 16 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 67 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 10 }, 101, new int [] { 0 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 10, 20, 30 }, 100, new int [] { 0, 10, 70 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 10, 20, 30 }, 101, new int [] { 0, 10, 71 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 10, 30, 61 })]
    //[InlineData (Justification.OneLeftRestRight, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 10, 30, 60, 101, })]



    public void TestJustifications_0Space (Justification justification, int [] sizes, int totalSize, int [] expected)
    {
        var positions = new Justifier () { MaxSpaceBetweenItems = 0 }.Justify (sizes, justification, totalSize);
        AssertJustification (justification, sizes, totalSize, positions, expected);
    }

    public void AssertJustification (Justification justification, int [] sizes, int totalSize, int [] positions, int [] expected)
    {
        try
        {
            _output.WriteLine ($"Testing: {RenderJustification (justification, sizes, totalSize, expected)}");
        }
        catch (Exception e)
        {
            _output.WriteLine ($"Exception rendering expected: {e.Message}");
            _output.WriteLine ($"Actual: {RenderJustification (justification, sizes, totalSize, positions)}");
        }

        if (!expected.SequenceEqual (positions))
        {
            _output.WriteLine ($"Expected: {RenderJustification (justification, sizes, totalSize, expected)}");
            _output.WriteLine ($"Actual: {RenderJustification (justification, sizes, totalSize, positions)}");
            Assert.Fail (" Expected and actual do not match");
        }
    }


    public string RenderJustification (Justification justification, int [] sizes, int totalSize, int [] positions)
    {
        var output = new StringBuilder ();
        output.AppendLine ($"Justification: {justification}, Positions: {string.Join (", ", positions)}, TotalSize: {totalSize}");
        for (int i = 0; i <= totalSize / 10; i++)
        {
            output.Append (i.ToString ().PadRight (9) + " ");
        }
        output.AppendLine ();

        for (int i = 0; i < totalSize; i++)
        {
            output.Append (i % 10);
        }
        output.AppendLine ();

        var items = new char [totalSize];
        for (int position = 0; position < positions.Length; position++)
        {
            // try
            {
                for (int j = 0; j < sizes [position] && positions [position] + j < totalSize; j++)
                {
                    items [positions [position] + j] = (position + 1).ToString () [0];
                }
            }
            //catch (Exception e)
            //{
            //    output.AppendLine ($"{e.Message} - position = {position}, positions[{position}]: {positions [position]}, sizes[{position}]: {sizes [position]}, totalSize: {totalSize}");
            //    output.Append (new string (items).Replace ('\0', ' '));

            //    Assert.Fail (e.Message + output.ToString ());
            //}
        }

        output.Append (new string (items).Replace ('\0', ' '));

        return output.ToString ();
    }

}
