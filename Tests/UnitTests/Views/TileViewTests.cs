using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class TileViewTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void Test_SplitTop_WholeBottom ()
    {
        var tileView = new TileView (2)
        {
            Width = 20, Height = 10, Orientation = Orientation.Horizontal, LineStyle = LineStyle.Single
        };

        Assert.True (tileView.TrySplitTile (0, 2, out TileView top));

        top.Tiles.ElementAt (0).ContentView!.Add (new Label { Text = "bleh" });
        top.Tiles.ElementAt (1).ContentView!.Add (new Label { Text = "blah" });
        top.Layout ();

        tileView.Tiles.ElementAt (1).ContentView!.Add (new Label { Text = "Hello" });
        tileView.SetScheme (new ());
        top.SetScheme (new ());

        top.Layout ();
        tileView.Layout ();
        tileView.Draw ();

        var looksLike =
            @"
┌─────────┬────────┐
│bleh     │blah    │
│         │        │
│         │        │
│         │        │
├─────────┴────────┤
│Hello             │
│                  │
│                  │
└──────────────────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter1 ()
    {
        TileView tv = Get5x1TilesView ();

        tv.Tiles.ElementAt (0).MinSize = int.MaxValue;

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 0; x <= 5; x++)
        {
            // All these values would result in tile 0 getting smaller
            // so are not allowed (tile[0] has a min size of Int.Max)
            Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        for (var x = 6; x < 10; x++)
        {
            // All these values would result in tile 0 getting bigger
            // so are allowed
            Assert.True (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        for (var x = 10; x < 100; x++)
        {
            // These values would result in the first splitter moving past
            // the second splitter so are not allowed
            Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────────┬┬────┬────┬───┐
│11111111││3333│4444│555│
│        ││    │    │   │
└────────┴┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter1_NoBorder ()
    {
        TileView tv = Get5x1TilesView (false);

        tv.Tiles.ElementAt (0).MinSize = int.MaxValue;

        Application.LayoutAndDraw ();

        var looksLike =
            @"
11111│2222│3333│4444│5555
     │    │    │    │
     │    │    │    │
     │    │    │    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 0; x <= 5; x++)
        {
            // All these values would result in tile 0 getting smaller
            // so are not allowed (tile[0] has a min size of Int.Max)
            Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        for (var x = 6; x < 10; x++)
        {
            // All these values would result in tile 0 getting bigger
            // so are allowed
            Assert.True (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        for (var x = 10; x < 100; x++)
        {
            // These values would result in the first splitter moving past
            // the second splitter so are not allowed
            Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
111111111││3333│4444│5555
         ││    │    │
         ││    │    │
         ││    │    │

";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter2 ()
    {
        TileView tv = Get5x1TilesView ();

        tv.Tiles.ElementAt (1).MinSize = 2;
        tv.Tiles.ElementAt (2).MinSize = 3;

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x > 7; x--)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 7; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────┬──┬──────┬────┬───┐
│1111│22│333333│4444│555│
│    │  │      │    │   │
└────┴──┴──────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x < 12; x++)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 12; x < 25; x++)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.Top!.Layout ();
        tv.Draw ();

        looksLike =
            @"
┌────┬─────┬───┬────┬───┐
│1111│22222│333│4444│555│
│    │     │   │    │   │
└────┴─────┴───┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter2_NoBorder ()
    {
        TileView tv = Get5x1TilesView (false);

        tv.Tiles.ElementAt (1).MinSize = 2;
        tv.Tiles.ElementAt (2).MinSize = 3;
        Application.LayoutAndDraw ();

        var looksLike =
            @"
11111│2222│3333│4444│5555
     │    │    │    │
     │    │    │    │
     │    │    │    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x > 7; x--)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 7; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"

11111│22│333333│4444│5555
     │  │      │    │
     │  │      │    │
     │  │      │    │

";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x < 12; x++)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 12; x < 25; x++)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"  
11111│22222│333│4444│5555
     │     │   │    │
     │     │   │    │
     │     │   │    │

";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter4 ()
    {
        TileView tv = Get5x1TilesView ();

        tv.Tiles.ElementAt (3).MinSize = 2;
        tv.Tiles.ElementAt (4).MinSize = 1;

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x > 17; x--)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 17; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────┬────┬────┬──┬─────┐
│1111│2222│3333│44│55555│
│    │    │    │  │     │
└────┴────┴────┴──┴─────┘

";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x < 23; x++)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 23; x < 100; x++)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────┬────┬────┬──────┬─┐
│1111│2222│3333│444444│5│
│    │    │    │      │ │
└────┴────┴────┴──────┴─┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter4_NoBorder ()
    {
        TileView tv = Get5x1TilesView (false);

        tv.Tiles.ElementAt (3).MinSize = 2;
        tv.Tiles.ElementAt (4).MinSize = 1;

        Application.LayoutAndDraw ();

        var looksLike =
            @"
11111│2222│3333│4444│5555
     │    │    │    │
     │    │    │    │
     │    │    │    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x > 17; x--)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 17; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"   
11111│2222│3333│44│555555
     │    │    │  │
     │    │    │  │
     │    │    │  │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x < 24; x++)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 24; x < 100; x++)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
11111│2222│3333│4444444│5
     │    │    │       │
     │    │    │       │
     │    │    │       │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter1_CannotCrossBorder ()
    {
        TileView tv = Get5x1TilesView ();

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 5; x > 0; x--)
        {
            Assert.True (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        Assert.False (tv.SetSplitterPos (0, 0));

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌┬────────┬────┬────┬───┐
││22222222│3333│4444│555│
││        │    │    │   │
└┴────────┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 6; x < 10; x++)
        {
            Assert.True (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        for (var x = 10; x < 100; x++)
        {
            // These values would result in the first splitter moving past
            // the second splitter so are not allowed
            Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────────┬┬────┬────┬───┐
│11111111││3333│4444│555│
│        ││    │    │   │
└────────┴┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter1_CannotCrossBorder_NoBorder ()
    {
        TileView tv = Get5x1TilesView (false);

        Application.LayoutAndDraw ();

        var looksLike =
            @"
11111│2222│3333│4444│5555
     │    │    │    │
     │    │    │    │
     │    │    │    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 5; x >= 0; x--)
        {
            Assert.True (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
│222222222│3333│4444│5555
│         │    │    │
│         │    │    │
│         │    │    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 6; x < 10; x++)
        {
            Assert.True (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        for (var x = 10; x < 100; x++)
        {
            // These values would result in the first splitter moving past
            // the second splitter so are not allowed
            Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
111111111││3333│4444│5555
         ││    │    │
         ││    │    │
         ││    │    │

";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter2_CannotMoveOverNeighbours ()
    {
        TileView tv = Get5x1TilesView ();

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x > 5; x--)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 5; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────┬┬────────┬────┬───┐
│1111││33333333│4444│555│
│    ││        │    │   │
└────┴┴────────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x < 15; x++)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 15; x < 25; x++)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────┬────────┬┬────┬───┐
│1111│22222222││4444│555│
│    │        ││    │   │
└────┴────────┴┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter2_CannotMoveOverNeighbours_NoBorder ()
    {
        TileView tv = Get5x1TilesView (false);

        Application.LayoutAndDraw ();

        var looksLike =
            @"
11111│2222│3333│4444│5555
     │    │    │    │
     │    │    │    │
     │    │    │    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x > 5; x--)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 5; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
11111││33333333│4444│5555
     ││        │    │
     ││        │    │
     ││        │    │

";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 10; x < 15; x++)
        {
            Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        for (var x = 15; x < 25; x++)
        {
            Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
11111│22222222││4444│5555
     │        ││    │
     │        ││    │
     │        ││    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter4_CannotMoveOverNeighbours ()
    {
        TileView tv = Get5x1TilesView ();

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x > 15; x--)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 15; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────┬────┬────┬┬───────┐
│1111│2222│3333││5555555│
│    │    │    ││       │
└────┴────┴────┴┴───────┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x < 24; x++)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 24; x < 100; x++)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────┬────┬────┬───────┬┐
│1111│2222│3333│4444444││
│    │    │    │       ││
└────┴────┴────┴───────┴┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter4_CannotMoveOverNeighbours_NoBorder ()
    {
        TileView tv = Get5x1TilesView (false);

        Application.LayoutAndDraw ();

        var looksLike =
            @"   
11111│2222│3333│4444│5555
     │    │    │    │
     │    │    │    │
     │    │    │    │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x > 15; x--)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 15; x > 0; x--)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"  
11111│2222│3333││55555555
     │    │    ││
     │    │    ││
     │    │    ││

";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        for (var x = 20; x < 25; x++)
        {
            Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        for (var x = 25; x < 100; x++)
        {
            Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
        }

        Application.LayoutAndDraw ();

        looksLike =
            @"
11111│2222│3333│44444444│
     │    │    │        │
     │    │    │        │
     │    │    │        │
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    public void TestDisposal_NoEarlyDisposalsOfUsersViews_DuringInsertTile ()
    {
        TileView tv = GetTileView (20, 10);

        var myReusableView = new DisposeCounter ();

        // I want my view in the first tile
        tv.Tiles.ElementAt (0).ContentView.Add (myReusableView);
        Assert.Equal (0, myReusableView.DisposalCount);

        // I've changed my mind, I want 3 tiles now
        tv.InsertTile (0);
        tv.InsertTile (2);

        // but I still want my view in the first tile
        // BUGBUG: Adding a view twice is not legit
        tv.Tiles.ElementAt (0).ContentView.Add (myReusableView);

        Assert.Multiple (
                         () => Assert.Equal (0, myReusableView.DisposalCount),
                         () =>
                         {
                             tv.Dispose ();
                             Assert.True (myReusableView.DisposalCount >= 1);
                         }
                        );
    }

    [Fact]
    public void TestDisposal_NoEarlyDisposalsOfUsersViews_DuringRebuildForTileCount ()
    {
        TileView tv = GetTileView (20, 10);

        var myReusableView = new DisposeCounter ();

        // I want my view in the first tile
        tv.Tiles.ElementAt (0).ContentView.Add (myReusableView);
        Assert.Equal (0, myReusableView.DisposalCount);

        // I've changed my mind, I want 3 tiles now
        tv.RebuildForTileCount (3);

        // but I still want my view in the first tile
        // BUGBUG: Adding a view twice is not legit
        tv.Tiles.ElementAt (0).ContentView.Add (myReusableView);

        Assert.Multiple (
                         () => Assert.Equal (0, myReusableView.DisposalCount),
                         () =>
                         {
                             tv.Dispose ();
                             Assert.Equal (1, myReusableView.DisposalCount);
                         }
                        );

        Assert.NotNull (Application.Top);
        Application.Top.Dispose ();
        Application.Shutdown ();
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    public void TestDisposal_NoEarlyDisposalsOfUsersViews_DuringRemoveTile (int idx)
    {
        TileView tv = GetTileView (20, 10);

        var myReusableView = new DisposeCounter ();

        // I want my view in the first tile
        tv.Tiles.ElementAt (0).ContentView.Add (myReusableView);
        Assert.Equal (0, myReusableView.DisposalCount);

        tv.RemoveTile (idx);

        // but I still want my view in the first tile
        // BUGBUG: Adding a view twice is not legit
        tv.Tiles.ElementAt (0).ContentView.Add (myReusableView);

        Assert.Multiple (
                         () => Assert.Equal (0, myReusableView.DisposalCount),
                         () =>
                         {
                             tv.Dispose ();
                             Assert.True (myReusableView.DisposalCount >= 1);
                         }
                        );

        Assert.NotNull (Application.Top);
        Application.Top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer2LeftAnd1Right_RendersNicely ()
    {
        TileView tileView = GetNestedContainer2Left1Right (false);

        Assert.Equal (20, tileView.Frame.Width);
        Assert.Equal (10, tileView.Tiles.ElementAt (0).ContentView.Frame.Width);
        Assert.Equal (9, tileView.Tiles.ElementAt (1).ContentView.Frame.Width);

        Assert.IsType<TileView> (tileView.Tiles.ElementAt (0).ContentView);
        var left = (TileView)tileView.Tiles.ElementAt (0).ContentView;
        Assert.Same (left.SuperView, tileView);

        Assert.Equal (2, left.Tiles.ElementAt (0).ContentView.SubViews.Count);
        Assert.IsType<Label> (left.Tiles.ElementAt (0).ContentView.SubViews.ElementAt (0));
        Assert.IsType<Label> (left.Tiles.ElementAt (0).ContentView.SubViews.ElementAt (1));
        var onesTop = (Label)left.Tiles.ElementAt (0).ContentView.SubViews.ElementAt (0);
        var onesBottom = (Label)left.Tiles.ElementAt (0).ContentView.SubViews.ElementAt (1);

        Assert.Same (left.Tiles.ElementAt (0).ContentView, onesTop.SuperView);
        Assert.Same (left.Tiles.ElementAt (0).ContentView, onesBottom.SuperView);

        Assert.Equal (10, onesTop.Frame.Width);
        Assert.Equal (10, onesBottom.Frame.Width);

        tileView.Draw ();

        var looksLike =
            @"    
1111111111│222222222
1111111111│222222222
          │
          │
          │
──────────┤
          │
          │
          │
          │";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_RendersNicely ()
    {
        TileView tileView = GetNestedContainer3Right1Down (false);

        tileView.Draw ();

        var looksLike =
            @"
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222├──────
111111│222222│444444
111111│222222│444444
111111│222222│444444
111111│222222│444444
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // It looks good but lets double check the measurements incase
        // anything is sticking out but drawn over

        // 3 panels + 2 splitters
        Assert.Equal (5, tileView.SubViews.Count);

        // Check X and Widths of Tiles
        Assert.Equal (0, tileView.Tiles.ElementAt (0).ContentView.Frame.X);
        Assert.Equal (6, tileView.Tiles.ElementAt (0).ContentView.Frame.Width);

        Assert.Equal (7, tileView.Tiles.ElementAt (1).ContentView.Frame.X);
        Assert.Equal (6, tileView.Tiles.ElementAt (1).ContentView.Frame.Width);

        Assert.Equal (14, tileView.Tiles.ElementAt (2).ContentView.Frame.X);
        Assert.Equal (6, tileView.Tiles.ElementAt (2).ContentView.Frame.Width);

        // Check Y and Heights of Tiles
        Assert.Equal (0, tileView.Tiles.ElementAt (0).ContentView.Frame.Y);
        Assert.Equal (10, tileView.Tiles.ElementAt (0).ContentView.Frame.Height);

        Assert.Equal (0, tileView.Tiles.ElementAt (1).ContentView.Frame.Y);
        Assert.Equal (10, tileView.Tiles.ElementAt (1).ContentView.Frame.Height);

        Assert.Equal (0, tileView.Tiles.ElementAt (2).ContentView.Frame.Y);
        Assert.Equal (10, tileView.Tiles.ElementAt (2).ContentView.Frame.Height);

        // Check Sub containers in last panel
        var subSplit = (TileView)tileView.Tiles.ElementAt (2).ContentView;
        Assert.Equal (0, subSplit.Tiles.ElementAt (0).ContentView.Frame.X);
        Assert.Equal (6, subSplit.Tiles.ElementAt (0).ContentView.Frame.Width);
        Assert.Equal (0, subSplit.Tiles.ElementAt (0).ContentView.Frame.Y);
        Assert.Equal (5, subSplit.Tiles.ElementAt (0).ContentView.Frame.Height);

        //Assert.IsType<TextView> (subSplit.Tiles.ElementAt (0).ContentView.SubViews.Single ());

        Assert.Equal (0, subSplit.Tiles.ElementAt (1).ContentView.Frame.X);
        Assert.Equal (6, subSplit.Tiles.ElementAt (1).ContentView.Frame.Width);
        Assert.Equal (6, subSplit.Tiles.ElementAt (1).ContentView.Frame.Y);
        Assert.Equal (4, subSplit.Tiles.ElementAt (1).ContentView.Frame.Height);

        //Assert.IsType<TextView> (subSplit.Tiles.ElementAt (1).ContentView.SubViews.Single ());
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_TileVisibility_WithBorder ()
    {
        TileView tileView = GetNestedContainer3Right1Down (true);

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌─────┬──────┬─────┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├─────┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = true;
        tileView.Tiles.ElementAt (2).ContentView.Visible = true;

        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────────────┬─────┐
│222222222222│33333│
│222222222222│33333│
│222222222222│33333│
│222222222222│33333│
│222222222222├─────┤
│222222222222│44444│
│222222222222│44444│
│222222222222│44444│
└────────────┴─────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = true;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌────────────┬─────┐
│111111111111│33333│
│111111111111│33333│
│111111111111│33333│
│111111111111│33333│
│111111111111├─────┤
│111111111111│44444│
│111111111111│44444│
│111111111111│44444│
└────────────┴─────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = true;
        tileView.Tiles.ElementAt (1).ContentView.Visible = true;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌─────┬────────────┐
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
└─────┴────────────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = true;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌──────────────────┐
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
└──────────────────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = true;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌──────────────────┐
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
└──────────────────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        tileView.Tiles.ElementAt (2).ContentView.Visible = true;
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌──────────────────┐
│333333333333333333│
│333333333333333333│
│333333333333333333│
│333333333333333333│
├──────────────────┤
│444444444444444444│
│444444444444444444│
│444444444444444444│
└──────────────────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_TileVisibility_WithoutBorder ()
    {
        TileView tileView = GetNestedContainer3Right1Down (false);
        Application.LayoutAndDraw ();

        var looksLike =
            @"
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222├──────
111111│222222│444444
111111│222222│444444
111111│222222│444444
111111│222222│444444";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = true;
        tileView.Tiles.ElementAt (2).ContentView.Visible = true;
        Application.LayoutAndDraw ();

        looksLike =
            @"
2222222222222│333333
2222222222222│333333
2222222222222│333333
2222222222222│333333
2222222222222│333333
2222222222222├──────
2222222222222│444444
2222222222222│444444
2222222222222│444444
2222222222222│444444";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = true;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        tileView.Tiles.ElementAt (2).ContentView.Visible = true;
        Application.LayoutAndDraw ();

        looksLike =
            @"
1111111111111│333333
1111111111111│333333
1111111111111│333333
1111111111111│333333
1111111111111│333333
1111111111111├──────
1111111111111│444444
1111111111111│444444
1111111111111│444444
1111111111111│444444";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = true;
        tileView.Tiles.ElementAt (1).ContentView.Visible = true;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = true;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = true;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        tileView.Tiles.ElementAt (2).ContentView.Visible = true;
        Application.LayoutAndDraw ();

        looksLike =
            @"
33333333333333333333
33333333333333333333
33333333333333333333
33333333333333333333
33333333333333333333
────────────────────
44444444444444444444
44444444444444444444
44444444444444444444
44444444444444444444";

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        tileView.Tiles.ElementAt (0).ContentView.Visible = false;
        tileView.Tiles.ElementAt (1).ContentView.Visible = false;
        tileView.Tiles.ElementAt (2).ContentView.Visible = false;
        Application.LayoutAndDraw ();

        looksLike =
            @"
			 ";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_TitleDoesNotOverspill ()
    {
        TileView tileView = GetNestedContainer3Right1Down (true, true, 1);
        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌ T1 ─┬ T3 ──┬ T2 ─┐
│11111│333333│22222│
│11111│333333│22222│
│11111│333333│22222│
│11111│333333│22222│
│11111├ T4 ──┤22222│
│11111│444444│22222│
│11111│444444│22222│
│11111│444444│22222│
└─────┴──────┴─────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_TitleTriesToOverspill ()
    {
        TileView tileView = GetNestedContainer3Right1Down (true, true, 1);

        tileView.Tiles.ElementAt (0).Title = new ('x', 100);

        ((TileView)tileView.Tiles.ElementAt (1).ContentView)
            .Tiles.ElementAt (1)
            .Title = new ('y', 100);

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌ xxxx┬ T3 ──┬ T2 ─┐
│11111│333333│22222│
│11111│333333│22222│
│11111│333333│22222│
│11111│333333│22222│
│11111├ yyyyy┤22222│
│11111│444444│22222│
│11111│444444│22222│
│11111│444444│22222│
└─────┴──────┴─────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_WithBorder_RemovingTiles ()
    {
        TileView tileView = GetNestedContainer3Right1Down (true);

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌─────┬──────┬─────┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├─────┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        Tile toRemove = tileView.Tiles.ElementAt (1);
        Tile removed = tileView.RemoveTile (1);
        Assert.Same (toRemove, removed);
        Assert.DoesNotContain (removed, tileView.Tiles);
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌─────────┬────────┐
│111111111│33333333│
│111111111│33333333│
│111111111│33333333│
│111111111│33333333│
│111111111├────────┤
│111111111│44444444│
│111111111│44444444│
│111111111│44444444│
└─────────┴────────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // cannot remove at this index because there is only one horizontal tile left
        Assert.Null (tileView.RemoveTile (2));
        tileView.RemoveTile (0);
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌──────────────────┐
│333333333333333333│
│333333333333333333│
│333333333333333333│
│333333333333333333│
├──────────────────┤
│444444444444444444│
│444444444444444444│
│444444444444444444│
└──────────────────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        Assert.NotNull (tileView.RemoveTile (0));
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // cannot remove
        Assert.Null (tileView.RemoveTile (0));
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_WithBorder_RendersNicely ()
    {
        TileView tileView = GetNestedContainer3Right1Down (true);
        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌─────┬──────┬─────┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├─────┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // It looks good but lets double check the measurements incase
        // anything is sticking out but drawn over

        // 3 panels + 2 splitters
        Assert.Equal (5, tileView.SubViews.Count);

        // Check X and Widths of Tiles
        Assert.Equal (1, tileView.Tiles.ElementAt (0).ContentView.Frame.X);
        Assert.Equal (5, tileView.Tiles.ElementAt (0).ContentView.Frame.Width);

        Assert.Equal (7, tileView.Tiles.ElementAt (1).ContentView.Frame.X);
        Assert.Equal (6, tileView.Tiles.ElementAt (1).ContentView.Frame.Width);

        Assert.Equal (14, tileView.Tiles.ElementAt (2).ContentView.Frame.X);
        Assert.Equal (5, tileView.Tiles.ElementAt (2).ContentView.Frame.Width);

        // Check Y and Heights of Tiles
        Assert.Equal (1, tileView.Tiles.ElementAt (0).ContentView.Frame.Y);
        Assert.Equal (8, tileView.Tiles.ElementAt (0).ContentView.Frame.Height);

        Assert.Equal (1, tileView.Tiles.ElementAt (1).ContentView.Frame.Y);
        Assert.Equal (8, tileView.Tiles.ElementAt (1).ContentView.Frame.Height);

        Assert.Equal (1, tileView.Tiles.ElementAt (2).ContentView.Frame.Y);
        Assert.Equal (8, tileView.Tiles.ElementAt (2).ContentView.Frame.Height);

        // Check Sub containers in last panel
        var subSplit = (TileView)tileView.Tiles.ElementAt (2).ContentView;
        Assert.Equal (0, subSplit.Tiles.ElementAt (0).ContentView.Frame.X);
        Assert.Equal (5, subSplit.Tiles.ElementAt (0).ContentView.Frame.Width);
        Assert.Equal (0, subSplit.Tiles.ElementAt (0).ContentView.Frame.Y);
        Assert.Equal (4, subSplit.Tiles.ElementAt (0).ContentView.Frame.Height);

        //Assert.IsType<TextView> (subSplit.Tiles.ElementAt (0).ContentView.SubViews.Single ());

        Assert.Equal (0, subSplit.Tiles.ElementAt (1).ContentView.Frame.X);
        Assert.Equal (5, subSplit.Tiles.ElementAt (1).ContentView.Frame.Width);
        Assert.Equal (5, subSplit.Tiles.ElementAt (1).ContentView.Frame.Y);
        Assert.Equal (3, subSplit.Tiles.ElementAt (1).ContentView.Frame.Height);

        //Assert.IsType<TextView> (subSplit.Tiles.ElementAt (1).ContentView.SubViews.Single ());
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedContainer3RightAnd1Down_WithTitledBorder_RendersNicely ()
    {
        TileView tileView = GetNestedContainer3Right1Down (true, true);

        tileView.Draw ();

        var looksLike =
            @"
┌ T1 ─┬ T2 ──┬ T3 ─┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├ T4 ─┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedNonRoots_OnlyOneRoot_OnlyRootCanHaveBorders ()
    {
        var tv = new TileView
        {
            Width = 10, Height = 5, LineStyle = LineStyle.Single
        };

        tv.TrySplitTile (1, 2, out TileView tv2);
       // tv2.Scheme = new ();
        tv2.LineStyle = LineStyle.Single;
        tv2.Orientation = Orientation.Horizontal;

        Assert.True (tv.IsRootTileView ());

        var top = new Toplevel ();
        top.Add (tv);
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        tv.LayoutSubViews ();
        tv.Tiles.ElementAt (1).ContentView.LayoutSubViews ();
        tv2.LayoutSubViews ();

        // tv2 is not considered a root because 
        // it was created via TrySplitTile so it
        // will have its lines joined to
        // parent and cannot have its own border
        Assert.False (tv2.IsRootTileView ());

        tv.Draw ();

        var looksLike =
            @"
┌────┬───┐
│    │   │
│    ├───┤
│    │   │
└────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestNestedRoots_BothRoots_BothCanHaveBorders ()
    {
        var tv = new TileView
        {
            Width = 10, Height = 5, LineStyle = LineStyle.Single
        };

        var tv2 = new TileView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            LineStyle = LineStyle.Single,
            Orientation = Orientation.Horizontal
        };

        Assert.True (tv.IsRootTileView ());
        tv.Tiles.ElementAt (1).ContentView.Add (tv2);

        var top = new Toplevel ();
        top.Add (tv);
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        tv.LayoutSubViews ();
        tv.Tiles.ElementAt (1).ContentView.LayoutSubViews ();
        tv2.LayoutSubViews ();

        // tv2 is still considered a root because 
        // it was manually created by API user. That
        // means it will not have its lines joined to
        // parents and it is permitted to have a border
        Assert.True (tv2.IsRootTileView ());

        tv.Draw ();

        var looksLike =
            @"
┌────┬───┐
│    │┌─┐│
│    │├─┤│
│    │└─┘│
└────┴───┘
";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_CannotSetSplitterPosToFuncEtc ()
    {
        TileView tileView = Get11By3TileView ();

        var ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos (0, Pos.Right (tileView)));
        Assert.Equal ("Only Percent and Absolute values are supported. Passed value was PosView", ex.Message);

        ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos (0, Pos.Func (_ => 1)));
        Assert.Equal ("Only Percent and Absolute values are supported. Passed value was PosFunc", ex.Message);

        // Also not allowed because this results in a PosCombine
        ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos (0, Pos.Percent (50) - 1));

        Assert.Equal (
                      "Only Percent and Absolute values are supported. Passed value was PosCombine",
                      ex.Message
                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Horizontal ()
    {
        TileView tileView = Get11By3TileView (out LineView line);
        tileView.Orientation = Orientation.Horizontal;
        tileView.Layout ();
        tileView.Draw ();

        var looksLike =
            @"    
11111111111
───────────
22222222222";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Keyboard movement on splitter should have no effect if it is not focused
        bool handled = tileView.NewKeyDownEvent (Key.CursorDown);
        Assert.False (handled);
        tileView.SetNeedsDraw ();
        tileView.Draw ();
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Horizontal_Focused ()
    {
        TileView tileView = Get11By3TileView (out LineView line);

        tileView.Orientation = Orientation.Horizontal;
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));

        Assert.True (line.HasFocus);
        Application.LayoutAndDraw ();

        var looksLike =
            @"    
11111111111
─────◊─────
22222222222";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Now move splitter line down
        tileView.NewKeyDownEvent (Key.CursorDown);

        Application.LayoutAndDraw ();

        looksLike =
            @"    
11111111111
11111111111
─────◊─────";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // And 2 up
        line.NewKeyDownEvent (Key.CursorUp);
        Application.LayoutAndDraw ();

        line.NewKeyDownEvent (Key.CursorUp);
        Application.LayoutAndDraw ();

        looksLike =
            @"    
─────◊─────
22222222222
22222222222";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Horizontal_View1MinSize_Absolute ()
    {
        TileView tileView = Get11By3TileView (out LineView line);
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));

        tileView.Orientation = Orientation.Horizontal;
        tileView.Tiles.ElementAt (0).MinSize = 1;

        // 0 should not be allowed because it brings us below minimum size of View1
        Assert.False (tileView.SetSplitterPos (0, 0));

        // position should remain where it was, at 50%
        Assert.Equal (Pos.Percent (50), tileView.SplitterDistances.ElementAt (0));
        Application.LayoutAndDraw ();

        var looksLike =
            @"    
11111111111
─────◊─────
22222222222";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Now move splitter line down (allowed
        line.NewKeyDownEvent (Key.CursorDown);
        Application.LayoutAndDraw ();

        looksLike =
            @"    
11111111111
11111111111
─────◊─────";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // And up 2 (only 1 is allowed because of minimum size of 1 on view1)
        line.NewKeyDownEvent (Key.CursorUp);
        line.NewKeyDownEvent (Key.CursorUp);

        tileView.SetNeedsDraw ();
        Application.LayoutAndDraw ();

        looksLike =
            @"    
11111111111
─────◊─────
22222222222";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void TestTileView_IndexOf (bool recursive)
    {
        var tv = new TileView ();
        var lbl1 = new Label ();
        var lbl2 = new Label ();
        var frame = new FrameView ();
        var sub = new Label ();
        frame.Add (sub);

        // IndexOf returns -1 when view not found
        Assert.Equal (-1, tv.IndexOf (lbl1, recursive));
        Assert.Equal (-1, tv.IndexOf (lbl2, recursive));

        // IndexOf supports looking for Tile.View
        Assert.Equal (0, tv.IndexOf (tv.Tiles.ElementAt (0).ContentView, recursive));
        Assert.Equal (1, tv.IndexOf (tv.Tiles.ElementAt (1).ContentView, recursive));

        // IndexOf supports looking for Tile.View.SubViews
        tv.Tiles.ElementAt (0).ContentView.Add (lbl1);
        Assert.Equal (0, tv.IndexOf (lbl1, recursive));

        tv.Tiles.ElementAt (1).ContentView.Add (lbl2);
        Assert.Equal (1, tv.IndexOf (lbl2, recursive));

        // IndexOf supports looking deep into subviews only when
        // the recursive true value is passed
        tv.Tiles.ElementAt (1).ContentView.Add (frame);

        if (recursive)
        {
            Assert.Equal (1, tv.IndexOf (sub, recursive));
        }
        else
        {
            Assert.Equal (-1, tv.IndexOf (sub, recursive));
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_InsertPanelAtEnd ()
    {
        TileView tileView = Get11By3TileView (out LineView line, true);
        tileView.InsertTile (2);

        tileView.Layout ();
        tileView.Draw ();

        // so should ignore the 2 distance and stick to 6
        var looksLike =
            @"
┌──┬───┬──┐
│11│222│  │
└──┴───┴──┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_InsertPanelAtStart ()
    {
        TileView tileView = Get11By3TileView (out LineView line, true);
        tileView.InsertTile (0);

        tileView.Layout ();
        tileView.Draw ();

        // so should ignore the 2 distance and stick to 6
        var looksLike =
            @"
┌──┬───┬──┐
│  │111│22│
└──┴───┴──┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_InsertPanelMiddle ()
    {
        TileView tileView = Get11By3TileView (out LineView line, true);
        tileView.InsertTile (1);

        tileView.Layout ();
        tileView.Draw ();

        // so should ignore the 2 distance and stick to 6
        var looksLike =
            @"
┌──┬───┬──┐
│11│   │22│
└──┴───┴──┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical ()
    {
        TileView tileView = Get11By3TileView (out LineView line);

        tileView.Layout ();
        tileView.Draw ();

        var looksLike =
            @"
11111│22222
11111│22222
     │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Keyboard movement on splitter should have no effect if it is not focused
        tileView.NewKeyDownEvent (Key.CursorRight);
        tileView.SetNeedsDraw ();
        tileView.Draw ();
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_Focused ()
    {
        TileView tileView = Get11By3TileView (out LineView line);
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));

        Application.LayoutAndDraw ();

        var looksLike =
            @"
11111│22222
11111◊22222
     │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Now while focused move the splitter 1 unit right
        line.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        looksLike =
            @"
111111│2222
111111◊2222
      │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // and 2 to the left
        line.NewKeyDownEvent (Key.CursorLeft);
        tileView.Layout ();
        line.NewKeyDownEvent (Key.CursorLeft);
        Application.LayoutAndDraw ();

        looksLike =
            @"
1111│222222
1111◊222222
    │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_Focused_50PercentSplit ()
    {
        TileView tileView = Get11By3TileView (out LineView line);
        tileView.SetSplitterPos (0, Pos.Percent (50));
        Assert.IsType<PosPercent> (tileView.SplitterDistances.ElementAt (0));
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));
        Application.LayoutAndDraw ();

        var looksLike =
            @"
11111│22222
11111◊22222
     │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Now while focused move the splitter 1 unit right
        line.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        looksLike =
            @"
111111│2222
111111◊2222
      │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Even when moving the splitter location it should stay a Percentage based one
        Assert.IsType<PosPercent> (tileView.SplitterDistances.ElementAt (0));

        // and 2 to the left
        line.NewKeyDownEvent (Key.CursorLeft);
        tileView.Layout ();
        line.NewKeyDownEvent (Key.CursorLeft);
        Application.LayoutAndDraw ();

        looksLike =
            @"
1111│222222
1111◊222222
    │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Even when moving the splitter location it should stay a Percentage based one
        Assert.IsType<PosPercent> (tileView.SplitterDistances.ElementAt (0));
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_Focused_WithBorder ()
    {
        TileView tileView = Get11By3TileView (out LineView line, true);
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));
        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┐
│1111◊2222│
└────┴────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Now while focused move the splitter 1 unit right
        line.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌─────┬───┐
│11111◊222│
└─────┴───┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // and 2 to the left
        line.NewKeyDownEvent (Key.CursorLeft);
        tileView.Layout ();
        line.NewKeyDownEvent (Key.CursorLeft);
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌───┬─────┐
│111◊22222│
└───┴─────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_View1MinSize_Absolute ()
    {
        TileView tileView = Get11By3TileView (out LineView line);
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));
        tileView.Tiles.ElementAt (0).MinSize = 6;

        // distance is too small (below 6)
        Assert.False (tileView.SetSplitterPos (0, 2));

        // Should stay where it was originally at (50%)
        Assert.Equal (Pos.Percent (50), tileView.SplitterDistances.ElementAt (0));
        Application.LayoutAndDraw ();

        // so should ignore the 2 distance and stick to 6
        var looksLike =
            @"
11111│22222
11111◊22222
     │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Keyboard movement on splitter should have no effect because it
        // would take us below the minimum splitter size
        line.NewKeyDownEvent (Key.CursorLeft);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // but we can continue to move the splitter right if we want
        line.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        looksLike =
            @"
111111│2222
111111◊2222
      │     ";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_View1MinSize_Absolute_WithBorder ()
    {
        TileView tileView = Get11By3TileView (out LineView line, true);
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));
        tileView.Tiles.ElementAt (0).MinSize = 5;

        // distance is too small (below 5)
        Assert.False (tileView.SetSplitterPos (0, 2));

        // Should stay where it was originally at (50%)
        Assert.Equal (Pos.Percent (50), tileView.SplitterDistances.ElementAt (0));
        Application.LayoutAndDraw ();

        // so should ignore the 2 distance and stick to 5
        var looksLike =
            @"
┌────┬────┐
│1111◊2222│
└────┴────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Keyboard movement on splitter should have no effect because it
        // would take us below the minimum splitter size
        line.NewKeyDownEvent (Key.CursorLeft);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // but we can continue to move the splitter right if we want
        line.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌─────┬───┐
│11111◊222│
└─────┴───┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_View2MinSize_Absolute ()
    {
        TileView tileView = Get11By3TileView (out LineView line);
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));
        tileView.Tiles.ElementAt (1).MinSize = 6;
        Application.LayoutAndDraw ();

        // distance leaves too little space for view2 (less than 6 would remain)
        Assert.False (tileView.SetSplitterPos (0, 8));

        //  Should stay where it was originally at (50%)
        Assert.Equal (Pos.Percent (50), tileView.SplitterDistances.ElementAt (0));

        Application.LayoutAndDraw ();

        // so should ignore the 2 distance and stick to 6
        var looksLike =
            @"
11111│22222
11111◊22222
     │     ";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Keyboard movement on splitter should have no effect because it
        // would take us below the minimum splitter size
        line.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // but we can continue to move the splitter left if we want
        line.NewKeyDownEvent (Key.CursorLeft);
        Application.LayoutAndDraw ();

        looksLike =
            @"
1111│222222
1111◊222222
    │    ";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_View2MinSize_Absolute_WithBorder ()
    {
        TileView tileView = Get11By3TileView (out LineView line, true);
        tileView.NewKeyDownEvent (new (tileView.ToggleResizable));
        tileView.Tiles.ElementAt (1).MinSize = 5;

        // distance leaves too little space for view2 (less than 5 would remain)
        Assert.False (tileView.SetSplitterPos (0, 8));
        Application.LayoutAndDraw ();

        //  Should stay where it was originally at (50%)
        Assert.Equal (Pos.Percent (50), tileView.SplitterDistances.ElementAt (0));

        Application.LayoutAndDraw ();

        // so should ignore the 2 distance and stick to 6
        var looksLike =
            @"
┌────┬────┐
│1111◊2222│
└────┴────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Keyboard movement on splitter should have no effect because it
        // would take us below the minimum splitter size
        line.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // but we can continue to move the splitter left if we want
        line.NewKeyDownEvent (Key.CursorLeft);
        Application.LayoutAndDraw ();

        looksLike =
            @"
┌───┬─────┐
│111◊22222│
└───┴─────┘";

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTileView_Vertical_WithBorder ()
    {
        TileView tileView = Get11By3TileView (out LineView line, true);

        Application.LayoutAndDraw ();

        var looksLike =
            @"
┌────┬────┐
│1111│2222│
└────┴────┘";
        DriverAssert.AssertDriverContentsAre (looksLike, output);

        // Keyboard movement on splitter should have no effect if it is not focused
        tileView.NewKeyDownEvent (Key.CursorRight);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTrySplit_ShouldRetainTitle ()
    {
        var tv = new TileView ();
        tv.Tiles.ElementAt (0).Title = "flibble";
        tv.TrySplitTile (0, 2, out TileView subTileView);

        // We moved the content so the title should also have been moved
        Assert.Equal ("flibble", subTileView.Tiles.ElementAt (0).Title);

        // Secondly we should have cleared the old title (it should have been moved not copied)
        Assert.Empty (tv.Tiles.ElementAt (0).Title);
    }

    private TileView Get11By3TileView (out LineView line, bool withBorder = false)
    {
        TileView split = Get11By3TileView (withBorder);
        line = GetLine (split);

        return split;
    }

    private TileView Get11By3TileView (bool withBorder = false) { return GetTileView (11, 3, withBorder); }

    private TileView Get5x1TilesView (bool border = true)
    {
        var tv = new TileView (5)
        {
            Width = 25, Height = 4, LineStyle = LineStyle.Single
        };

        if (!border)
        {
            tv.LineStyle = LineStyle.None;
        }

        tv.Tiles.ElementAt (0)
          .ContentView.Add (
                            new Label { Width = Dim.Fill (), Height = 1, Text = new ('1', 100) }
                           );

        tv.Tiles.ElementAt (1)
          .ContentView.Add (
                            new Label { Width = Dim.Fill (), Height = 1, Text = new ('2', 100) }
                           );

        tv.Tiles.ElementAt (2)
          .ContentView.Add (
                            new Label { Width = Dim.Fill (), Height = 1, Text = new ('3', 100) }
                           );

        tv.Tiles.ElementAt (3)
          .ContentView.Add (
                            new Label { Width = Dim.Fill (), Height = 1, Text = new ('4', 100) }
                           );

        tv.Tiles.ElementAt (4)
          .ContentView.Add (
                            new Label { Width = Dim.Fill (), Height = 1, Text = new ('5', 100) }
                           );

        Application.Top = new ();
        Application.Top.Add (tv);

        Application.Begin (Application.Top);

        return tv;
    }

    private LineView GetLine (TileView tileView) { return tileView.SubViews.OfType<LineView> ().Single (); }

    /// <summary>Creates a vertical orientation root container with left pane split into two (with horizontal splitter line).</summary>
    /// <param name="withBorder"></param>
    /// <returns></returns>
    private TileView GetNestedContainer2Left1Right (bool withBorder)
    {
        TileView container = GetTileView (20, 10, withBorder);
        Assert.True (container.TrySplitTile (0, 2, out TileView newContainer));

        newContainer.Orientation = Orientation.Horizontal;

        container.LayoutSubViews ();

        return container;
    }

    /// <summary>Creates a vertical orientation root container with 3 tiles. The rightmost is split horizontally</summary>
    /// <param name="withBorder"></param>
    /// <returns></returns>
    private TileView GetNestedContainer3Right1Down (bool withBorder, bool withTitles = false, int split = 2)
    {
        Application.Top = new ();
        var container = new TileView (3) { Width = 20, Height = 10 };
        container.LineStyle = withBorder ? LineStyle.Single : LineStyle.None;

        Assert.True (container.TrySplitTile (split, 2, out TileView newContainer));

        newContainer.Orientation = Orientation.Horizontal;

        var i = 0;

        foreach (Tile tile in container.Tiles.Union (newContainer.Tiles))
        {
            if (tile.ContentView is TileView)
            {
                continue;
            }

            i++;

            if (withTitles)
            {
                tile.Title = "T" + i;
            }

            tile.ContentView.Add (
                                  new TextView
                                  {
                                      Width = Dim.Fill (),
                                      Height = Dim.Fill (),
                                      Text =
                                          string.Join (
                                                       '\n',
                                                       Enumerable.Repeat (
                                                                          new string (i.ToString () [0], 100),
                                                                          10
                                                                         )
                                                                 .ToArray ()
                                                      ),
                                      WordWrap = false
                                  }
                                 );
        }

        Application.Top.Add (container);
        Application.Begin (Application.Top);

        return container;
    }

    private TileView GetTileView (int width, int height, bool withBorder = false)
    {
        var container = new TileView
        {
            Id = "container",
            Width = width, Height = height
        };

        container.LineStyle = withBorder ? LineStyle.Single : LineStyle.None;

        container.Tiles.ElementAt (0)
                 .ContentView.Add (
                                   new Label { Id = "label1", Width = Dim.Fill (), Height = 1, Text = new ('1', 100) }
                                  );

        container.Tiles.ElementAt (0)
                 .ContentView.Add (
                                   new Label
                                   {
                                       Id = "label2",
                                       Width = Dim.Fill (),
                                       Height = 1,
                                       Y = 1,
                                       Text = new ('1', 100)
                                   }
                                  );

        container.Tiles.ElementAt (1)
                 .ContentView.Add (
                                   new Label { Id = "label3", Width = Dim.Fill (), Height = 1, Text = new ('2', 100) }
                                  );

        container.Tiles.ElementAt (1)
                 .ContentView.Add (
                                   new Label
                                   {
                                       Id = "label4",
                                       Width = Dim.Fill (),
                                       Height = 1,
                                       Y = 1,
                                       Text = new ('2', 100)
                                   }
                                  );

        container.Tiles.ElementAt (0).MinSize = 0;
        container.Tiles.ElementAt (1).MinSize = 0;

        Application.Top = new ();
        Application.Top.Add (container);

        Application.Begin (Application.Top);

        return container;
    }

    private class DisposeCounter : View
    {
        public int DisposalCount;

        protected override void Dispose (bool disposing)
        {
            DisposalCount++;
            base.Dispose (disposing);
        }
    }
}
