using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class JustifierTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    public static IEnumerable<object []> JustificationEnumValues ()
    {
        foreach (object number in Enum.GetValues (typeof (Justification)))
        {
            yield return new [] { number };
        }
    }

    [Theory]
    [MemberData (nameof (JustificationEnumValues))]
    public void NoItems_Works (Justification justification)
    {
        int [] sizes = { };
        int [] positions = new Justifier ().Justify (sizes, justification, 100);
        Assert.Equal (new int [] { }, positions);
    }

    //[Theory]
    //[MemberData (nameof (JustificationEnumValues))]
    //public void Items_Width_Cannot_Exceed_TotalSize (Justification justification)
    //{
    //    int [] sizes = { 1000, 2000, 3000 };
    //    Assert.Throws<ArgumentException> (() => new Justifier ().Justify (sizes, justification, 100));
    //}

    [Theory]
    [MemberData (nameof (JustificationEnumValues))]
    public void Negative_Widths_Not_Allowed (Justification justification)
    {
        Assert.Throws<ArgumentException> (() => new Justifier ().Justify (new [] { -10, 20, 30 }, justification, 100));
        Assert.Throws<ArgumentException> (() => new Justifier ().Justify (new [] { 10, -20, 30 }, justification, 100));
        Assert.Throws<ArgumentException> (() => new Justifier ().Justify (new [] { 10, 20, -30 }, justification, 100));
    }

    [Theory]
    [InlineData (Justification.Left, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Justification.Left, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Justification.Left, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Justification.Left, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Justification.Left, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Justification.Left, new [] { 1 }, 3, new [] { 0 })]
    [InlineData (Justification.Left, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Justification.Left, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Justification.Left, new [] { 1, 1 }, 4, new [] { 0, 2 })]
    [InlineData (Justification.Left, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 10, new [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 11, new [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 12, new [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 13, new [] { 0, 2, 5 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Justification.Left, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Justification.Left, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Justification.Left, new [] { 10, 20 }, 101, new [] { 0, 11 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30 }, 100, new [] { 0, 11, 32 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30 }, 101, new [] { 0, 11, 32 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.Right, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Justification.Right, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Justification.Right, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 10, new [] { 2, 4, 7 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 11, new [] { 3, 5, 8 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 12, new [] { 4, 6, 9 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 13, new [] { 5, 7, 10 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30 }, 100, new [] { 38, 49, 70 })]
    [InlineData (Justification.Right, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Justification.Right, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Justification.Right, new [] { 10, 20 }, 101, new [] { 70, 81 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30 }, 101, new [] { 39, 50, 71 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.Centered, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Justification.Centered, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Justification.Centered, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Justification.Centered, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Justification.Centered, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Justification.Centered, new [] { 1 }, 3, new [] { 1 })]
    [InlineData (Justification.Centered, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Justification.Centered, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Justification.Centered, new [] { 1, 1 }, 4, new [] { 0, 2 })]
    [InlineData (Justification.Centered, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 10, new [] { 1, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 11, new [] { 1, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 9, new [] { 0, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 10, new [] { 0, 4, 7 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 11, new [] { 0, 4, 8 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 12, new [] { 0, 4, 8 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 13, new [] { 1, 5, 9 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 101, new [] { 0, 34, 68 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 102, new [] { 0, 34, 68 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 103, new [] { 1, 35, 69 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 104, new [] { 1, 35, 69 })]
    [InlineData (Justification.Centered, new [] { 10 }, 101, new [] { 45 })]
    [InlineData (Justification.Centered, new [] { 10, 20 }, 101, new [] { 35, 46 })]
    [InlineData (Justification.Centered, new [] { 10, 20, 30 }, 100, new [] { 19, 30, 51 })]
    [InlineData (Justification.Centered, new [] { 10, 20, 30 }, 101, new [] { 19, 30, 51 })]
    [InlineData (Justification.Centered, new [] { 10, 20, 30, 40 }, 100, new [] { 0, 10, 30, 60 })]
    [InlineData (Justification.Centered, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Centered, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.Centered, new [] { 3, 4, 5, 6 }, 25, new [] { 2, 6, 11, 17 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30 }, 100, new [] { 0, 30, 70 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30 }, 101, new [] { 0, 31, 71 })]
    [InlineData (Justification.Justified, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Justification.Justified, new [] { 11, 17, 23 }, 100, new [] { 0, 36, 77 })]
    [InlineData (Justification.Justified, new [] { 1, 2, 3 }, 11, new [] { 0, 4, 8 })]
    [InlineData (Justification.Justified, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Justification.Justified, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Justification.Justified, new [] { 3, 3, 3 }, 21, new [] { 0, 9, 18 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5 }, 21, new [] { 0, 8, 16 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 18, new [] { 0, 3, 7, 12 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 19, new [] { 0, 4, 8, 13 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 20, new [] { 0, 4, 9, 14 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 21, new [] { 0, 4, 9, 15 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 22, new [] { 0, 8, 14, 19 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 23, new [] { 0, 8, 15, 20 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 24, new [] { 0, 8, 15, 21 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 25, new [] { 0, 9, 16, 22 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 26, new [] { 0, 9, 17, 23 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 31, new [] { 0, 11, 20, 28 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1 }, 2, new [] { 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1 }, 3, new [] { 2 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 8, new [] { 0, 2, 5 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 9, new [] { 0, 2, 6 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 10, new [] { 0, 2, 7 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 11, new [] { 0, 2, 8 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 3, 3, 3 }, 21, new [] { 0, 4, 18 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 3, 4, 5 }, 21, new [] { 0, 4, 16 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30 }, 100, new [] { 0, 11, 70 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30 }, 101, new [] { 0, 11, 71 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 0, 0, 0 }, 1, new [] { 0, 0, 1 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1 }, 3, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 4 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 8, new [] { 0, 2, 5 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 9, new [] { 0, 3, 6 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 10, new [] { 0, 4, 7 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 11, new [] { 0, 5, 8 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 7 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 12, new [] { 0, 1, 4, 8 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 3, 3, 3 }, 21, new [] { 0, 14, 18 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 3, 4, 5 }, 21, new [] { 0, 11, 16 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 67 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30 }, 100, new [] { 0, 49, 70 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30 }, 101, new [] { 0, 50, 71 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 10, 30, 61 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 10, 30, 60, 101 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 3, 3, 3 }, 21, new [] { 0, 14, 18 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 3, 4, 5 }, 21, new [] { 0, 11, 16 })]
    public void TestJustifications_PutSpaceBetweenItems (Justification justification, int [] sizes, int totalSize, int [] expected)
    {
        int [] positions = new Justifier { PutSpaceBetweenItems = true }.Justify (sizes, justification, totalSize);
        AssertJustification (justification, sizes, totalSize, positions, expected);
    }

    [Theory]
    [InlineData (Justification.Left, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Justification.Left, new [] { 0, 0 }, 1, new [] { 0, 0 })]
    [InlineData (Justification.Left, new [] { 0, 0, 0 }, 1, new [] { 0, 0, 0 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 10, new [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 11, new [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 12, new [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3 }, 13, new [] { 0, 1, 3 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Left, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30 }, 100, new [] { 0, 10, 30 })]
    [InlineData (Justification.Left, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 66 })]
    [InlineData (Justification.Left, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Justification.Left, new [] { 10, 20 }, 101, new [] { 0, 10 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30 }, 101, new [] { 0, 10, 30 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 10, 30, 60 })]
    [InlineData (Justification.Left, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 10, 30, 60, 100 })]
    [InlineData (Justification.Right, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Justification.Right, new [] { 0, 0 }, 1, new [] { 1, 1 })]
    [InlineData (Justification.Right, new [] { 0, 0, 0 }, 1, new [] { 1, 1, 1 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 7, new [] { 1, 2, 4 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 10, new [] { 4, 5, 7 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 11, new [] { 5, 6, 8 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 12, new [] { 6, 7, 9 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3 }, 13, new [] { 7, 8, 10 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Right, new [] { 1, 2, 3, 4 }, 11, new [] { 1, 2, 4, 7 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30 }, 100, new [] { 40, 50, 70 })]
    [InlineData (Justification.Right, new [] { 33, 33, 33 }, 100, new [] { 1, 34, 67 })]
    [InlineData (Justification.Right, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Justification.Right, new [] { 10, 20 }, 101, new [] { 71, 81 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30 }, 101, new [] { 41, 51, 71 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30, 40 }, 101, new [] { 1, 11, 31, 61 })]
    [InlineData (Justification.Right, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 1, 11, 31, 61, 101 })]
    [InlineData (Justification.Centered, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Justification.Centered, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Justification.Centered, new [] { 1 }, 3, new [] { 1 })]
    [InlineData (Justification.Centered, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Justification.Centered, new [] { 1, 1 }, 3, new [] { 0, 1 })]
    [InlineData (Justification.Centered, new [] { 1, 1 }, 4, new [] { 1, 2 })]
    [InlineData (Justification.Centered, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 3 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 10, new [] { 2, 3, 5 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3 }, 11, new [] { 2, 3, 5 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 9, new [] { 0, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 10, new [] { 0, 3, 6 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 11, new [] { 1, 4, 7 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 12, new [] { 1, 4, 7 })]
    [InlineData (Justification.Centered, new [] { 3, 3, 3 }, 13, new [] { 2, 5, 8 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 66 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 101, new [] { 1, 34, 67 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 102, new [] { 1, 34, 67 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 103, new [] { 2, 35, 68 })]
    [InlineData (Justification.Centered, new [] { 33, 33, 33 }, 104, new [] { 2, 35, 68 })]
    [InlineData (Justification.Centered, new [] { 3, 4, 5, 6 }, 25, new [] { 3, 6, 10, 15 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30 }, 100, new [] { 0, 30, 70 })]
    [InlineData (Justification.Justified, new [] { 10, 20, 30 }, 101, new [] { 0, 31, 71 })]
    [InlineData (Justification.Justified, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Justification.Justified, new [] { 11, 17, 23 }, 100, new [] { 0, 36, 77 })]
    [InlineData (Justification.Justified, new [] { 1, 2, 3 }, 11, new [] { 0, 4, 8 })]
    [InlineData (Justification.Justified, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Justification.Justified, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Justification.Justified, new [] { 3, 3, 3 }, 21, new [] { 0, 9, 18 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5 }, 21, new [] { 0, 8, 16 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 18, new [] { 0, 3, 7, 12 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 19, new [] { 0, 4, 8, 13 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 20, new [] { 0, 4, 9, 14 })]
    [InlineData (Justification.Justified, new [] { 3, 4, 5, 6 }, 21, new [] { 0, 4, 9, 15 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 22, new [] { 0, 8, 14, 19 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 23, new [] { 0, 8, 15, 20 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 24, new [] { 0, 8, 15, 21 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 25, new [] { 0, 9, 16, 22 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 26, new [] { 0, 9, 17, 23 })]
    [InlineData (Justification.Justified, new [] { 6, 5, 4, 3 }, 31, new [] { 0, 11, 20, 28 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 0, 0, 0 }, 1, new [] { 0, 0, 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1 }, 2, new [] { 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1 }, 3, new [] { 2 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 4 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 8, new [] { 0, 1, 5 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 9, new [] { 0, 1, 6 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 10, new [] { 0, 1, 7 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3 }, 11, new [] { 0, 1, 8 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 7 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 12, new [] { 0, 1, 3, 8 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 3, 3, 3 }, 21, new [] { 0, 3, 18 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 3, 4, 5 }, 21, new [] { 0, 3, 16 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 67 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30 }, 100, new [] { 0, 10, 70 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30 }, 101, new [] { 0, 10, 71 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 10, 30, 61 })]
    [InlineData (Justification.LastRightRestLeft, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 10, 30, 60, 101 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1 }, 3, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 8, new [] { 0, 3, 5 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 9, new [] { 0, 4, 6 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 10, new [] { 0, 5, 7 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3 }, 11, new [] { 0, 6, 8 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 12, new [] { 0, 3, 5, 8 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 3, 3, 3 }, 21, new [] { 0, 15, 18 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 3, 4, 5 }, 21, new [] { 0, 12, 16 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30 }, 100, new [] { 0, 50, 70 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30 }, 101, new [] { 0, 51, 71 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Justification.FirstLeftRestRight, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    public void TestJustifications_NoSpaceBetweenItems (Justification justification, int [] sizes, int totalSize, int [] expected)
    {
        int [] positions = new Justifier { PutSpaceBetweenItems = false }.Justify (sizes, justification, totalSize);
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

        for (var i = 0; i <= totalSize / 10; i++)
        {
            output.Append (i.ToString ().PadRight (9) + " ");
        }

        output.AppendLine ();

        for (var i = 0; i < totalSize; i++)
        {
            output.Append (i % 10);
        }

        output.AppendLine ();

        var items = new char [totalSize];

        for (var position = 0; position < positions.Length; position++)
        {
            // try
            {
                for (var j = 0; j < sizes [position] && positions [position] + j < totalSize; j++)
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
