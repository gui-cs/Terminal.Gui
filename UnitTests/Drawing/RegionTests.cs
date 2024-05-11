#nullable enable
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

[Trait ("Category", "Drawing")]
public class RegionTests (ITestOutputHelper output)
{
    [Fact]
    public void HashSet_Does_Not_Allows_Duplicates ()
    {
        // Confirming Rectangle and RectangleF are equatable
        Assert.True (Rectangle.Empty == RectangleF.Empty);
        Rectangle rect = new Rectangle (10, 10, 10, 10);
        RectangleF rectF = new RectangleF (10, 10, 10, 10);
        Assert.True (rect == rectF);
        rect.Offset (1, 1);
        Assert.Equal (new RectangleF (11, 11, 10, 10), rect);
        rectF.Offset (1, 1);
        Assert.Equal (new Rectangle (11, 11, 10, 10), rectF);
        rect.Inflate (1, 1);
        Assert.Equal (new RectangleF (10, 10, 12, 12), rect);
        rectF.Inflate (1, 1);
        Assert.Equal (new Rectangle (10, 10, 12, 12), rectF);
        HashSet<Region> regions = [new (new Rectangle (10, 10, 10, 10))];
        Assert.True (regions.First ().Equals (new Region (new Rectangle (10, 10, 10, 10))));
        Assert.False (regions.Add (new (new Rectangle (10, 10, 10, 10))));
        Assert.Single (regions);
    }

    [Fact]
    public void Union_Array_Throws_If_Null ()
    {
        Region [] regions = null!;
        Assert.Throws<ArgumentNullException> (() => Region.Union (regions));
    }

    [Fact]
    public void Union_Empty_Array_Return_Rectangle_Empty ()
    {
        Region [] regions = [];
        Assert.Equal (Rectangle.Empty, Region.Union (regions));
    }

    [Fact]
    public void Union_HashSet_Throws_If_Null ()
    {
        HashSet<Region> regions = null!;
        Assert.Throws<ArgumentNullException> (() => Region.Union (regions));
    }

    [Fact]
    public void Union_Empty_HashSet_Return_Rectangle_Empty ()
    {
        HashSet<Region> regions = new ();
        Assert.Equal (Rectangle.Empty, Region.Union (regions));
    }

    [Fact]
    public void Union_Throws_If_Null ()
    {
        Region region = new ();
        Assert.Throws<ArgumentNullException> (() => region.Union (null!));
    }

    [Theory]
    [MemberData (nameof (UnionDataWithArrays))]
    public void Union_Array_Tests (Rectangle expected, Region [] regions)
    {
        Assert.Equal (expected, Region.Union (regions));
    }

    public static TheoryData<Rectangle, Region []> UnionDataWithArrays =>
        new ()
        {
            {
                new Rectangle (10, 10, 70, 70),
                [
                    new Region (new RectangleF (10, 10, 10, 10)),
                    new Region (new Rectangle (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ]
            },
            {
                new Rectangle (-10, -10, 90, 90),
                [
                    new Region (new Rectangle (-10, -10, 10, 10)),
                    new Region (new RectangleF (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ]
            }
        };

    [Theory]
    [MemberData (nameof (UnionDataWithHashSet))]
    public void Union_HashSet_Tests (Rectangle expected, HashSet<Region> regions)
    {
        Assert.Equal (expected, Region.Union (regions));
    }

    public static TheoryData<Rectangle, HashSet<Region>> UnionDataWithHashSet =>
        new ()
        {
            {
                new Rectangle (10, 10, 70, 70),
                [
                    new Region (new RectangleF (10, 10, 10, 10)),
                    new Region (new Rectangle (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ]
            },
            {
                new Rectangle (-10, -10, 90, 90),
                [
                    new Region (new RectangleF (-10, -10, 10, 10)),
                    new Region (new Rectangle (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ]
            }
        };

    [Fact]
    public void Location_HashSet ()
    {
        HashSet<Region> regions = [new (new Rectangle (10, 10, 30, 30))];
        Assert.Equal (new Point (10, 10), Region.Union (regions)!.Location);
        regions = [new (new RectangleF (10, 10, 30, 30))];
        Assert.Equal (new Point (10, 10), Region.Union (regions)!.Location);
    }

    [Fact]
    public void Size_HashSet ()
    {
        HashSet<Region> regions = [new (new Rectangle (10, 10, 30, 30))];
        Assert.Equal (new Size (30, 30), Region.Union (regions)!.Size);
        regions = [new (new RectangleF (10, 10, 30, 30))];
        Assert.Equal (new Size (30, 30), Region.Union (regions)!.Size);
    }

    [Fact]
    public void ViewsGetBounds_Test ()
    {
        var subView1 = new View { Width = 10, Height = 10 };
        var subView2 = new View { X = -1, Y = -1, Width = 20, Height = 20 };
        var super = new View ();
        super.Add (subView1, subView2);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubviews ();

        Assert.Equal (new Rectangle (0, 0, 20, 20), Region.GetViewsBounds ([.. super.Subviews]));

        subView2.ViewportSettings |= ViewportSettings.AllowNegativeX | ViewportSettings.AllowNegativeY;
        subView2.Viewport = subView2.Frame;
        Assert.Equal (new Rectangle (-1, -1, 20, 20), Region.GetViewsBounds ([.. super.Subviews]));
    }

    [Fact]
    public void Intersect_Array_Throws_If_Null ()
    {
        Region [] regions = null!;
        Assert.Throws<ArgumentNullException> (() => Region.Intersect (regions, Rectangle.Empty));
    }

    [Fact]
    public void Intersect_Empty_Array_Return_Rectangle_Empty ()
    {
        Region [] regions = [];
        Assert.Equal (Rectangle.Empty, Region.Intersect (regions, Rectangle.Empty));
    }

    [Fact]
    public void Intersect_HashSet_Throws_If_Null ()
    {
        HashSet<Region> regions = null!;
        Assert.Throws<ArgumentNullException> (() => Region.Intersect (regions, Rectangle.Empty));
    }

    [Fact]
    public void Intersect_Empty_HashSet_Return_Rectangle_Empty ()
    {
        HashSet<Region> regions = new ();
        Assert.Equal (Rectangle.Empty, Region.Intersect (regions, Rectangle.Empty));
    }

    [Fact]
    public void Intersect_Throws_If_Null ()
    {
        Region region = new ();
        Assert.Throws<ArgumentNullException> (() => region.Intersect (null!));
    }

    [Theory]
    [MemberData (nameof (IntersectDataWithArrays))]
    public void Intersect_Array_Tests (Region [] regions, Rectangle rect, Rectangle expected)
    {
        Assert.Equal (expected, Region.Intersect (regions, rect));
    }

    public static TheoryData<Region [], Rectangle, Rectangle> IntersectDataWithArrays =>
        new ()
        {
            {
                [
                    new Region (new Rectangle (10, 10, 10, 10)),
                    new Region (new RectangleF (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ],
                new Rectangle (10, 10, 70, 70),
                new Rectangle (10, 10, 10, 10)
            },
            {
                [
                    new Region (new Rectangle (-10, -10, 10, 10)),
                    new Region (new RectangleF (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ],
                new Rectangle (-10, -10, 90, 90),
                new Rectangle (-10, -10, 10, 10)
            },
            {
                [
                    new Region (new Rectangle (10, 10, 70, 70)),
                    new Region (new RectangleF (30, 30, 40, 40)),
                    new Region (new Rectangle (50, 50, 20, 20)),
                    new Region (new Rectangle (60, 60, 10, 10))
                ],
                new Rectangle (20, 20, 60, 60),
                new Rectangle (20, 20, 60, 60)
            },
            {
                [
                    new Region (new Rectangle (-10, -10, 70, 70)),
                    new Region (new RectangleF (30, 30, 40, 40)),
                    new Region (new Rectangle (50, 50, 20, 20)),
                    new Region (new Rectangle (60, 60, 10, 10))
                ],
                new Rectangle (20, 20, 60, 60),
                new Rectangle (20, 20, 40, 40)
            }
        };

    [Theory]
    [MemberData (nameof (IntersectDataWithHashSet))]
    public void Intersect_HashSet_Tests (HashSet<Region> regions, Rectangle rect, Rectangle expected)
    {
        Assert.Equal (expected, Region.Intersect (regions, rect));
    }

    public static TheoryData<HashSet<Region>, Rectangle, Rectangle> IntersectDataWithHashSet =>
        new ()
        {
            {
                [
                    new Region (new RectangleF (10, 10, 10, 10)),
                    new Region (new Rectangle (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ],
                new Rectangle (10, 10, 70, 70),
                new Rectangle (10, 10, 10, 10)
            },
            {
                [
                    new Region (new RectangleF (-10, -10, 10, 10)),
                    new Region (new Rectangle (30, 30, 10, 10)),
                    new Region (new Rectangle (50, 50, 10, 10)),
                    new Region (new Rectangle (70, 70, 10, 10))
                ],
                new Rectangle (-10, -10, 90, 90),
                new Rectangle (-10, -10, 10, 10)
            },
            {
                [
                    new Region (new RectangleF (10, 10, 70, 70)),
                    new Region (new Rectangle (30, 30, 40, 40)),
                    new Region (new Rectangle (50, 50, 20, 20)),
                    new Region (new Rectangle (60, 60, 10, 10))
                ],
                new Rectangle (20, 20, 60, 60),
                new Rectangle (20, 20, 60, 60)
            },
            {
                [
                    new Region (new RectangleF (-10, -10, 70, 70)),
                    new Region (new Rectangle (30, 30, 40, 40)),
                    new Region (new Rectangle (50, 50, 20, 20)),
                    new Region (new Rectangle (60, 60, 10, 10))
                ],
                new Rectangle (20, 20, 60, 60),
                new Rectangle (20, 20, 40, 40)
            }
        };

    [Fact]
    public void Constructor_Defaults ()
    {
        var region = new Region ();
        Assert.Equal (Rectangle.Empty, region.Union (Rectangle.Empty));

        var region2 = new Region (new RectangleF (0, 0, 10, 10));
        Assert.Equal (new Rectangle (0, 0, 10, 10), region.Union (region2));

        Assert.Equal (new Rectangle (1, 1, 9, 9), region.Intersect (new Rectangle (1, 1, 20, 20)));

        Assert.Equal (new Rectangle (1, 1, 9, 9), region.Intersect (region2));

        var region3 = new Region (new RegionData ([]));
        Assert.Equal (Rectangle.Empty, region3.Union (Rectangle.Empty));
    }

    [Fact]
    public void Constructor_Throws_If_Null ()
    {
        RegionData rgnData = null!;
        Assert.Throws<ArgumentNullException> (() => new Region (rgnData));
    }

    [Fact]
    public void GetHashCode_Test ()
    {
        var rect = Rectangle.Empty;
        var region = new Region (rect);
        Assert.Equal (HashCode.Combine (rect), region.GetHashCode ());
    }

    [Fact]
    public void Contains_Tests ()
    {
        HashSet<Region> regions =
            [new (new Rectangle (0, 0, 5, 5)), new (new RectangleF (10, 10, 5, 5)), new (new Rectangle (20, 20, 5, 5))];

        Assert.True (Region.Contains (regions, 1, 1));
        Assert.False (Region.Contains (regions, 30, 30));
    }

    [Fact]
    public void Clone_Tests ()
    {
        var region = new Region (new Rectangle (1, 2, 3, 4));
        var regionCloned = region.Clone ();
        Assert.Equal (regionCloned, region);
    }

    [Theory]
    [MemberData (nameof (RegionDataData))]
    public void CreateRegionFromRegionData_Tests (string text, Region expectedRegion)
    {
        RegionData rgnData = new RegionData (text.ToRunes ());
        Region region = new Region (rgnData);

        Assert.Equal (expectedRegion, region);
    }

    public static TheoryData<string, Region> RegionDataData =>
        new ()
        {
            {
                "This a RegionData test.", new Region (new Rectangle (0, 0, 23, 1))
            },
            {
                "Ends with a new line.\n", new Region (new Rectangle (0, 0, 21, 2))
            },
            {
                "Ends with a new line.\r\n", new Region (new Rectangle (0, 0, 21, 2))
            },
            {
                "Ends with a new line." + Environment.NewLine, new Region (new Rectangle (0, 0, 21, 2))
            },
            {
                "First line.\nSecond line.\nThird line.", new Region (new Rectangle (0, 0, 12, 3))
            },
            {
                "文に は言葉 があり ます。", new Region (new Rectangle (0, 0, 25, 1))
            },
            {
                "文に は言葉 があり ます。\nこんにちは世界。", new Region (new Rectangle (0, 0, 25, 2))
            },

        };

    [Fact]
    public void GetRegionData_Return_Null_If_Size_Is_Zero ()
    {
        Region region = new Region (new Rectangle (10, 10, 0, 0));

        Assert.Null (region.GetRegionData ());
    }

    [SetupFakeDriver]
    [Fact]
    public void GetRegionData_Return_Driver_Content_Within_Region ()
    {
        Toplevel top = new Toplevel { X = 10, Y = 10, Text = "This a test for Region\n and for RegionData 你\ud835\udd39" }; // 𝔹
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
          This a test for
          Region         
           and for       
          RegionData 你𝔹 ",
                                                      output);

        Region region = new Region (new Rectangle (10, 10, 30, 20));
        RegionData rgnData = region.GetRegionData ()!;

        Assert.Equal (
                      "This a test forRegion          and for       RegionData 你\ud835\udd39",
                      StringExtensions.ToString (rgnData.Data).Replace ("\0", "").Trim ());

        Application.Driver.Move (0, 16);
        Application.Driver.AddStr (StringExtensions.ToString (rgnData.Data [..15]));
        Application.Driver.Move (0, 17);
        Application.Driver.AddStr (StringExtensions.ToString (rgnData.Data [15..30]));
        Application.Driver.Move (0, 18);
        Application.Driver.AddStr (StringExtensions.ToString (rgnData.Data [30..45]));
        Application.Driver.Move (0, 19);
        Application.Driver.AddStr (StringExtensions.ToString (rgnData.Data [45..60]));

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
          This a test for
          Region         
           and for       
          RegionData 你𝔹 
                         
                         
This a test for          
Region                   
 and for                 
RegionData 你𝔹           ",
                                                      output);

        top.Dispose ();
        Application.Shutdown ();
    }
}
