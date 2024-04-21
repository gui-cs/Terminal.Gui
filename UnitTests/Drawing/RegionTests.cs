#nullable enable
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
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
        Assert.Equal (new Point (10, 10), Region.Union (regions).Location);
        regions = [new (new RectangleF (10, 10, 30, 30))];
        Assert.Equal (new Point (10, 10), Region.Union (regions).Location);
    }

    [Fact]
    public void Size_HashSet ()
    {
        HashSet<Region> regions = [new (new Rectangle (10, 10, 30, 30))];
        Assert.Equal (new Size (30, 30), Region.Union (regions).Size);
        regions = [new (new RectangleF (10, 10, 30, 30))];
        Assert.Equal (new Size (30, 30), Region.Union (regions).Size);
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

        var region2 = new Region (new (0, 0, 10, 10));
        Assert.Equal (new Rectangle  (0, 0, 10, 10), region.Union (region2));

        Assert.Equal (new Rectangle (1, 1, 9, 9), region.Intersect (new Rectangle (1, 1, 20, 20)));

        Assert.Equal (new Rectangle (1, 1, 9, 9), region.Intersect (region2));
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
            [new (new (0, 0, 5, 5)), new (new (10, 10, 5, 5)), new (new (20, 20, 5, 5))];

        Assert.True (Region.Contains (regions, 1, 1));
        Assert.False (Region.Contains (regions, 30, 30));
    }
}
