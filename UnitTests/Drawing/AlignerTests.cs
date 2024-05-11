using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class AlignerTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    public static IEnumerable<object []> AlignmentEnumValues ()
    {
        foreach (object number in Enum.GetValues (typeof (Alignment)))
        {
            yield return new [] { number };
        }
    }

    [Theory]
    [MemberData (nameof (AlignmentEnumValues))]
    public void Alignment_Round_Trips (Alignment alignment)
    {
        string serialized = JsonSerializer.Serialize<Alignment> (alignment);
        var deserialized = JsonSerializer.Deserialize<Alignment> (serialized);

        Assert.Equal (alignment, deserialized);
    }

    [Theory]
    [MemberData (nameof (AlignmentEnumValues))]
    public void NoItems_Works (Alignment alignment)
    {
        int [] sizes = [];
        int [] positions = Aligner.Align (alignment, false, 100, sizes);
        Assert.Equal (new int [] { }, positions);
    }

    [Theory]
    [MemberData (nameof (AlignmentEnumValues))]
    public void Negative_Widths_Not_Allowed (Alignment alignment)
    {
        Assert.Throws<ArgumentException> (
                                          () => new Aligner
                                          {
                                              Alignment = alignment,
                                              ContainerSize = 100
                                          }.Align (new [] { -10, 20, 30 }));

        Assert.Throws<ArgumentException> (
                                          () => new Aligner
                                          {
                                              Alignment = alignment,
                                              ContainerSize = 100
                                          }.Align (new [] { 10, -20, 30 }));

        Assert.Throws<ArgumentException> (
                                          () => new Aligner
                                          {
                                              Alignment = alignment,
                                              ContainerSize = 100
                                          }.Align (new [] { 10, 20, -30 }));
    }

    [Theory]
    [InlineData (Alignment.Left, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Alignment.Left, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Alignment.Left, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Alignment.Left, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Alignment.Left, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Alignment.Left, new [] { 1 }, 3, new [] { 0 })]
    [InlineData (Alignment.Left, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Alignment.Left, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Alignment.Left, new [] { 1, 1 }, 4, new [] { 0, 2 })]
    [InlineData (Alignment.Left, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 10, new [] { 0, 2, 5 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 11, new [] { 0, 2, 5 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 12, new [] { 0, 2, 5 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 13, new [] { 0, 2, 5 })]
    [InlineData (
                    Alignment.Left,
                    new [] { 1, 2, 3 },
                    5,
                    new [] { 0, 1, 3 })] // 5 is too small to fit the items. The first item is at 0, the items to the right are clipped.
    [InlineData (Alignment.Left, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Alignment.Left, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Alignment.Left, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Alignment.Left, new [] { 10, 20 }, 101, new [] { 0, 11 })]
    [InlineData (Alignment.Left, new [] { 10, 20, 30 }, 100, new [] { 0, 11, 32 })]
    [InlineData (Alignment.Left, new [] { 10, 20, 30 }, 101, new [] { 0, 11, 32 })]
    [InlineData (Alignment.Left, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Alignment.Left, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Alignment.Right, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Alignment.Right, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Alignment.Right, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 10, new [] { 2, 4, 7 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 11, new [] { 3, 5, 8 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 12, new [] { 4, 6, 9 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 13, new [] { 5, 7, 10 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 5, new [] { -1, 0, 2 })] // 5 is too small to fit the items. The first item is at -1.
    [InlineData (Alignment.Right, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30 }, 100, new [] { 38, 49, 70 })]
    [InlineData (Alignment.Right, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Alignment.Right, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Alignment.Right, new [] { 10, 20 }, 101, new [] { 70, 81 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30 }, 101, new [] { 39, 50, 71 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Alignment.Centered, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Alignment.Centered, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Alignment.Centered, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Alignment.Centered, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Alignment.Centered, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Alignment.Centered, new [] { 1 }, 3, new [] { 1 })]
    [InlineData (Alignment.Centered, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Alignment.Centered, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Alignment.Centered, new [] { 1, 1 }, 4, new [] { 0, 2 })]
    [InlineData (Alignment.Centered, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 10, new [] { 1, 3, 6 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 11, new [] { 1, 3, 6 })]
    [InlineData (
                    Alignment.Centered,
                    new [] { 1, 2, 3 },
                    5,
                    new [] { 0, 1, 3 })] // 5 is too small to fit the items. The first item is at 0, the items to the right are clipped.
    [InlineData (Alignment.Centered, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 9, new [] { 0, 3, 6 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 10, new [] { 0, 4, 7 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 11, new [] { 0, 4, 8 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 12, new [] { 0, 4, 8 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 13, new [] { 1, 5, 9 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 101, new [] { 0, 34, 68 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 102, new [] { 0, 34, 68 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 103, new [] { 1, 35, 69 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 104, new [] { 1, 35, 69 })]
    [InlineData (Alignment.Centered, new [] { 10 }, 101, new [] { 45 })]
    [InlineData (Alignment.Centered, new [] { 10, 20 }, 101, new [] { 35, 46 })]
    [InlineData (Alignment.Centered, new [] { 10, 20, 30 }, 100, new [] { 19, 30, 51 })]
    [InlineData (Alignment.Centered, new [] { 10, 20, 30 }, 101, new [] { 19, 30, 51 })]
    [InlineData (Alignment.Centered, new [] { 10, 20, 30, 40 }, 100, new [] { 0, 10, 30, 60 })]
    [InlineData (Alignment.Centered, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Alignment.Centered, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Alignment.Centered, new [] { 3, 4, 5, 6 }, 25, new [] { 2, 6, 11, 17 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30 }, 100, new [] { 0, 30, 70 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30 }, 101, new [] { 0, 31, 71 })]
    [InlineData (Alignment.Justified, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Alignment.Justified, new [] { 11, 17, 23 }, 100, new [] { 0, 36, 77 })]
    [InlineData (Alignment.Justified, new [] { 1, 2, 3 }, 11, new [] { 0, 4, 8 })]
    [InlineData (Alignment.Justified, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Alignment.Justified, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Alignment.Justified, new [] { 3, 3, 3 }, 21, new [] { 0, 9, 18 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5 }, 21, new [] { 0, 8, 16 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 18, new [] { 0, 3, 7, 12 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 19, new [] { 0, 4, 8, 13 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 20, new [] { 0, 4, 9, 14 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 21, new [] { 0, 4, 9, 15 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 22, new [] { 0, 8, 14, 19 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 23, new [] { 0, 8, 15, 20 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 24, new [] { 0, 8, 15, 21 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 25, new [] { 0, 9, 16, 22 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 26, new [] { 0, 9, 17, 23 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 31, new [] { 0, 11, 20, 28 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1 }, 2, new [] { 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1 }, 3, new [] { 2 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 8, new [] { 0, 2, 5 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 9, new [] { 0, 2, 6 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 10, new [] { 0, 2, 7 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 11, new [] { 0, 2, 8 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 5, new [] { -1, 0, 2 })] // 5 is too small to fit the items. The first item is at -1.})]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 3, 3, 3 }, 21, new [] { 0, 4, 18 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 3, 4, 5 }, 21, new [] { 0, 4, 16 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30 }, 100, new [] { 0, 11, 70 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30 }, 101, new [] { 0, 11, 71 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 0, 0, 0 }, 1, new [] { 0, 0, 1 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1 }, 3, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 4 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 8, new [] { 0, 2, 5 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 9, new [] { 0, 3, 6 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 10, new [] { 0, 4, 7 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 11, new [] { 0, 5, 8 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 5, new [] { 0, 1, 3 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 7 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 12, new [] { 0, 1, 4, 8 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 3, 3, 3 }, 21, new [] { 0, 14, 18 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 3, 4, 5 }, 21, new [] { 0, 11, 16 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 67 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30 }, 100, new [] { 0, 49, 70 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30 }, 101, new [] { 0, 50, 71 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 10, 30, 61 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 10, 30, 60, 101 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 3, 4, 5 }, 21, new [] { 0, 11, 16 })]
    public void Alignment_SpaceBetweenItems (Alignment alignment, int [] sizes, int containerSize, int [] expected)
    {
        int [] positions = new Aligner
        {
            SpaceBetweenItems = true,
            Alignment = alignment,
            ContainerSize = containerSize
        }.Align (sizes);
        AssertAlignment (alignment, sizes, containerSize, positions, expected);
    }

    [Theory]
    [InlineData (Alignment.Left, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Alignment.Left, new [] { 0, 0 }, 1, new [] { 0, 0 })]
    [InlineData (Alignment.Left, new [] { 0, 0, 0 }, 1, new [] { 0, 0, 0 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 10, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 11, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 12, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3 }, 13, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.Left, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 6 })]
    [InlineData (
                    Alignment.Left,
                    new [] { 1, 2, 3 },
                    5,
                    new [] { 0, 1, 3 })] // 5 is too small to fit the items. The first item is at 0, the items to the right are clipped.
    [InlineData (Alignment.Left, new [] { 10, 20, 30 }, 100, new [] { 0, 10, 30 })]
    [InlineData (Alignment.Left, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 66 })]
    [InlineData (Alignment.Left, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Alignment.Left, new [] { 10, 20 }, 101, new [] { 0, 10 })]
    [InlineData (Alignment.Left, new [] { 10, 20, 30 }, 101, new [] { 0, 10, 30 })]
    [InlineData (Alignment.Left, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 10, 30, 60 })]
    [InlineData (Alignment.Left, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 10, 30, 60, 100 })]
    [InlineData (Alignment.Right, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Alignment.Right, new [] { 0, 0 }, 1, new [] { 1, 1 })]
    [InlineData (Alignment.Right, new [] { 0, 0, 0 }, 1, new [] { 1, 1, 1 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 7, new [] { 1, 2, 4 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 10, new [] { 4, 5, 7 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 11, new [] { 5, 6, 8 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 12, new [] { 6, 7, 9 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 13, new [] { 7, 8, 10 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3 }, 5, new [] { -1, 0, 2 })] // 5 is too small to fit the items. The first item is at -1.
    [InlineData (Alignment.Right, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.Right, new [] { 1, 2, 3, 4 }, 11, new [] { 1, 2, 4, 7 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30 }, 100, new [] { 40, 50, 70 })]
    [InlineData (Alignment.Right, new [] { 33, 33, 33 }, 100, new [] { 1, 34, 67 })]
    [InlineData (Alignment.Right, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Alignment.Right, new [] { 10, 20 }, 101, new [] { 71, 81 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30 }, 101, new [] { 41, 51, 71 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30, 40 }, 101, new [] { 1, 11, 31, 61 })]
    [InlineData (Alignment.Right, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 1, 11, 31, 61, 101 })]
    [InlineData (Alignment.Centered, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Alignment.Centered, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Alignment.Centered, new [] { 1 }, 3, new [] { 1 })]
    [InlineData (Alignment.Centered, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Alignment.Centered, new [] { 1, 1 }, 3, new [] { 0, 1 })]
    [InlineData (Alignment.Centered, new [] { 1, 1 }, 4, new [] { 1, 2 })]
    [InlineData (Alignment.Centered, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 3 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 10, new [] { 2, 3, 5 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3 }, 11, new [] { 2, 3, 5 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.Centered, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 9, new [] { 0, 3, 6 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 10, new [] { 0, 3, 6 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 11, new [] { 1, 4, 7 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 12, new [] { 1, 4, 7 })]
    [InlineData (Alignment.Centered, new [] { 3, 3, 3 }, 13, new [] { 2, 5, 8 })]
    [InlineData (
                    Alignment.Centered,
                    new [] { 1, 2, 3 },
                    5,
                    new [] { 0, 1, 3 })] // 5 is too small to fit the items. The first item is at 0, the items to the right are clipped.
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 66 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 101, new [] { 1, 34, 67 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 102, new [] { 1, 34, 67 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 103, new [] { 2, 35, 68 })]
    [InlineData (Alignment.Centered, new [] { 33, 33, 33 }, 104, new [] { 2, 35, 68 })]
    [InlineData (Alignment.Centered, new [] { 3, 4, 5, 6 }, 25, new [] { 3, 6, 10, 15 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30 }, 100, new [] { 0, 30, 70 })]
    [InlineData (Alignment.Justified, new [] { 10, 20, 30 }, 101, new [] { 0, 31, 71 })]
    [InlineData (Alignment.Justified, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Alignment.Justified, new [] { 11, 17, 23 }, 100, new [] { 0, 36, 77 })]
    [InlineData (Alignment.Justified, new [] { 1, 2, 3 }, 11, new [] { 0, 4, 8 })]
    [InlineData (Alignment.Justified, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Alignment.Justified, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Alignment.Justified, new [] { 3, 3, 3 }, 21, new [] { 0, 9, 18 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5 }, 21, new [] { 0, 8, 16 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 18, new [] { 0, 3, 7, 12 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 19, new [] { 0, 4, 8, 13 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 20, new [] { 0, 4, 9, 14 })]
    [InlineData (Alignment.Justified, new [] { 3, 4, 5, 6 }, 21, new [] { 0, 4, 9, 15 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 22, new [] { 0, 8, 14, 19 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 23, new [] { 0, 8, 15, 20 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 24, new [] { 0, 8, 15, 21 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 25, new [] { 0, 9, 16, 22 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 26, new [] { 0, 9, 17, 23 })]
    [InlineData (Alignment.Justified, new [] { 6, 5, 4, 3 }, 31, new [] { 0, 11, 20, 28 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 0 }, 1, new [] { 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 0, 0, 0 }, 1, new [] { 0, 0, 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1 }, 2, new [] { 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1 }, 3, new [] { 2 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 7, new [] { 0, 1, 4 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 8, new [] { 0, 1, 5 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 9, new [] { 0, 1, 6 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 10, new [] { 0, 1, 7 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3 }, 11, new [] { 0, 1, 8 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 1, 3, 7 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 1, 2, 3, 4 }, 12, new [] { 0, 1, 3, 8 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 3, 3, 3 }, 21, new [] { 0, 3, 18 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 3, 4, 5 }, 21, new [] { 0, 3, 16 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 33, 33, 33 }, 100, new [] { 0, 33, 67 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10 }, 101, new [] { 91 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30 }, 100, new [] { 0, 10, 70 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30 }, 101, new [] { 0, 10, 71 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 10, 30, 61 })]
    [InlineData (Alignment.LastRightRestLeft, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 10, 30, 60, 101 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 0 }, 1, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 0, 0 }, 1, new [] { 0, 1 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 0, 0, 0 }, 1, new [] { 0, 1, 1 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1 }, 1, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1 }, 2, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1 }, 3, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1 }, 2, new [] { 0, 1 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1 }, 3, new [] { 0, 2 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1 }, 4, new [] { 0, 3 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 1, 1 }, 3, new [] { 0, 1, 2 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 6, new [] { 0, 1, 3 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 7, new [] { 0, 2, 4 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 8, new [] { 0, 3, 5 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 9, new [] { 0, 4, 6 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 10, new [] { 0, 5, 7 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3 }, 11, new [] { 0, 6, 8 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 10, new [] { 0, 1, 3, 6 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 11, new [] { 0, 2, 4, 7 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 1, 2, 3, 4 }, 12, new [] { 0, 3, 5, 8 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 3, 3, 3 }, 21, new [] { 0, 15, 18 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 3, 4, 5 }, 21, new [] { 0, 12, 16 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 33, 33, 33 }, 100, new [] { 0, 34, 67 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10 }, 101, new [] { 0 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20 }, 101, new [] { 0, 81 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30 }, 100, new [] { 0, 50, 70 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30 }, 101, new [] { 0, 51, 71 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30, 40 }, 101, new [] { 0, 11, 31, 61 })]
    [InlineData (Alignment.FirstLeftRestRight, new [] { 10, 20, 30, 40, 50 }, 151, new [] { 0, 11, 31, 61, 101 })]
    public void Alignment_NoSpaceBetweenItems (Alignment alignment, int [] sizes, int containerSize, int [] expected)
    {
        int [] positions = new Aligner
        {
            SpaceBetweenItems = false,
            Alignment = alignment,
            ContainerSize = containerSize
        }.Align (sizes);
        AssertAlignment (alignment, sizes, containerSize, positions, expected);
    }

    private void AssertAlignment (Alignment alignment, int [] sizes, int totalSize, int [] positions, int [] expected)
    {
        try
        {
            _output.WriteLine ($"Testing: {RenderAlignment (alignment, sizes, totalSize, expected)}");
        }
        catch (Exception e)
        {
            _output.WriteLine ($"Exception rendering expected: {e.Message}");
            _output.WriteLine ($"Actual: {RenderAlignment (alignment, sizes, totalSize, positions)}");
        }

        if (!expected.SequenceEqual (positions))
        {
            _output.WriteLine ($"Expected: {RenderAlignment (alignment, sizes, totalSize, expected)}");
            _output.WriteLine ($"Actual: {RenderAlignment (alignment, sizes, totalSize, positions)}");
            Assert.Fail (" Expected and actual do not match");
        }
    }

    private string RenderAlignment (Alignment alignment, int [] sizes, int totalSize, int [] positions)
    {
        var output = new StringBuilder ();
        output.AppendLine ($"Alignment: {alignment}, Positions: {string.Join (", ", positions)}, TotalSize: {totalSize}");

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
                    if (positions [position] + j >= 0)
                    {
                        items [positions [position] + j] = (position + 1).ToString () [0];
                    }
                }
            }
        }

        output.Append (new string (items).Replace ('\0', ' '));

        return output.ToString ();
    }
}
