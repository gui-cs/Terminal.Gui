
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Terminal.Gui.DrawingTests;

public class JustifierTests (ITestOutputHelper output)
{

    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void TestLeftJustification ()
    {
        int [] sizes = { 10, 20, 30 };
        var positions = Justifier.Justify (sizes, Justification.Left, 100);
        Assert.Equal (new List<int> { 0, 10, 30 }, positions);
    }

    [Fact]
    public void TestRightJustification ()
    {
        int [] sizes = { 10, 20, 30 };
        var positions = Justifier.Justify (sizes, Justification.Right, 100);
        Assert.Equal (new List<int> { 40, 50, 70 }, positions);
    }

    [Fact]
    public void TestCenterJustification ()
    {
        int [] sizes = { 10, 20, 30 };
        var positions = Justifier.Justify (sizes, Justification.Centered, 100);
        Assert.Equal (new List<int> { 20, 30, 50 }, positions);
    }

    [Fact]
    public void TestJustifiedJustification ()
    {
        int [] sizes = { 10, 20, 30 };
        var positions = Justifier.Justify (sizes, Justification.Justified, 100);
        Assert.Equal (new List<int> { 0, 30, 70 }, positions);
    }

    [Fact]
    public void TestNoItems ()
    {
        int [] sizes = { };
        var positions = Justifier.Justify (sizes, Justification.Left, 100);
        Assert.Equal (new int [] { }, positions);
    }

    [Fact]
    public void TestTenItems ()
    {
        int [] sizes = { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
        var positions = Justifier.Justify (sizes, Justification.Left, 100);
        Assert.Equal (new int [] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90 }, positions);
    }

    [Fact]
    public void TestZeroLengthItems ()
    {
        int [] sizes = { 0, 0, 0 };
        var positions = Justifier.Justify (sizes, Justification.Left, 100);
        Assert.Equal (new int [] { 0, 0, 0 }, positions);
    }

    [Fact]
    public void TestLongItems ()
    {
        int [] sizes = { 1000, 2000, 3000 };
        Assert.Throws<ArgumentException> (() => Justifier.Justify (sizes, Justification.Left, 100));
    }

    [Fact]
    public void TestNegativeLengths ()
    {
        int [] sizes = { -10, -20, -30 };
        Assert.Throws<ArgumentException> (() => Justifier.Justify (sizes, Justification.Left, 100));
    }

    [Theory]
    [InlineData (Justification.Left, new int [] { 10, 20, 30 }, 100, new int [] { 0, 10, 30 })]
    [InlineData (Justification.Left, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 66 })]
    [InlineData (Justification.Left, new int [] { 10 }, 101, new int [] { 0 })]
    [InlineData (Justification.Left, new int [] { 10, 20 }, 101, new int [] { 0, 10 })]
    [InlineData (Justification.Left, new int [] { 10, 20, 30 }, 101, new int [] { 0, 10, 30 })]
    [InlineData (Justification.Left, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 10, 30, 60 })]
    [InlineData (Justification.Left, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 10, 30, 60, 100 })]

    [InlineData (Justification.Right, new int [] { 10, 20, 30 }, 100, new int [] { 40, 50, 70 })]
    [InlineData (Justification.Right, new int [] { 33, 33, 33 }, 100, new int [] { 1, 34, 67 })]
    [InlineData (Justification.Right, new int [] { 10 }, 101, new int [] { 91 })]
    [InlineData (Justification.Right, new int [] { 10, 20 }, 101, new int [] { 71, 81 })]
    [InlineData (Justification.Right, new int [] { 10, 20, 30 }, 101, new int [] { 41, 51, 71 })]
    [InlineData (Justification.Right, new int [] { 10, 20, 30, 40 }, 101, new int [] { 1, 11, 31, 61 })]
    [InlineData (Justification.Right, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 1, 11, 31, 61, 101 })]

    [InlineData (Justification.Centered, new int [] { 10, 20, 30 }, 100, new int [] { 20, 30, 50 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 99, new int [] { 0, 33, 66 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 66 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 101, new int [] { 1, 34, 67 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 102, new int [] { 1, 34, 67 })]
    [InlineData (Justification.Centered, new int [] { 33, 33, 33 }, 104, new int [] { 2, 35, 68 })]
    [InlineData (Justification.Centered, new int [] { 10 }, 101, new int [] { 45 })]
    [InlineData (Justification.Centered, new int [] { 10, 20 }, 101, new int [] { 35, 45 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30 }, 101, new int [] { 20, 30, 50 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 10, 30, 60 })]
    [InlineData (Justification.Centered, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 10, 30, 60, 100 })]

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

    [InlineData (Justification.LeftJustified, new int [] { 10, 20, 30 }, 100, new int [] { 0, 11, 70 })]
    [InlineData (Justification.LeftJustified, new int [] { 33, 33, 33 }, 100, new int [] { 0, 34, 67 })]
    [InlineData (Justification.LeftJustified, new int [] { 10 }, 101, new int [] { 0 })]
    [InlineData (Justification.LeftJustified, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    [InlineData (Justification.LeftJustified, new int [] { 10, 20, 30 }, 101, new int [] { 0, 11, 71 })]
    [InlineData (Justification.LeftJustified, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 11, 32, 61 })]
    [InlineData (Justification.LeftJustified, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 11, 32, 63, 101 })]
    [InlineData (Justification.LeftJustified, new int [] { 3, 3, 3 }, 21, new int [] { 0, 4, 18 })]
    [InlineData (Justification.LeftJustified, new int [] { 3, 4, 5 }, 21, new int [] { 0, 4, 16 })]

    [InlineData (Justification.RightJustified, new int [] { 10, 20, 30 }, 100, new int [] { 0, 49, 70 })]
    [InlineData (Justification.RightJustified, new int [] { 33, 33, 33 }, 100, new int [] { 0, 33, 67 })]
    [InlineData (Justification.RightJustified, new int [] { 10 }, 101, new int [] { 0 })]
    [InlineData (Justification.RightJustified, new int [] { 10, 20 }, 101, new int [] { 0, 81 })]
    [InlineData (Justification.RightJustified, new int [] { 10, 20, 30 }, 101, new int [] { 0, 50, 71 })]
    [InlineData (Justification.RightJustified, new int [] { 10, 20, 30, 40 }, 101, new int [] { 0, 9, 30, 61 })]
    [InlineData (Justification.RightJustified, new int [] { 10, 20, 30, 40, 50 }, 151, new int [] { 0, 8, 29, 60, 101 })]
    [InlineData (Justification.RightJustified, new int [] { 3, 3, 3 }, 21, new int [] { 0, 14, 18 })]
    [InlineData (Justification.RightJustified, new int [] { 3, 4, 5 }, 21, new int [] { 0, 11, 16 })]

    public void TestJustifications (Justification justification, int [] sizes, int totalSize, int [] expected)
    {
        var positions = Justifier.Justify (sizes, justification, totalSize);
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

        if (!expected.SequenceEqual(positions))
        {
            _output.WriteLine ($"Expected: {RenderJustification (justification, sizes, totalSize, expected)}");
            _output.WriteLine ($"Actual: {RenderJustification (justification, sizes, totalSize, positions)}");
            Assert.Fail(" Expected and actual do not match");
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
            try
            {
                for (int j = 0; j < sizes [position]; j++)
                {
                    items [positions [position] + j] = (position + 1).ToString () [0];
                }
            } catch(Exception e)
            {
                output.AppendLine ($"{e.Message} - position = {position}, positions[{position}]: {positions[position]}, sizes[{position}]: {sizes[position]}, totalSize: {totalSize}");
                output.Append (new string (items).Replace ('\0', ' '));

               Assert.Fail(e.Message + output.ToString ());
            }
        }

        output.Append (new string (items).Replace ('\0', ' '));

        return output.ToString ();
    }

}
