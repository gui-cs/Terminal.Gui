using System.Text;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public class TreeViewTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void CursorVisibility_MultiSelect ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var n1 = new TreeNode ("normal");
        var n2 = new TreeNode ("pink");
        tv.AddObject (n1);
        tv.AddObject (n2);

        var top = new Runnable ();
        top.Add (tv);
        Application.Begin (top);

        Assert.True (tv.MultiSelect);
        Assert.True (tv.HasFocus);
        Assert.False (tv.Cursor.IsVisible);

        tv.SelectAll ();
        tv.Cursor = tv.Cursor with { Position = new Point (0, 0), Style = CursorStyle.BlinkingBlock };
        Assert.True (tv.Cursor.IsVisible);
        Application.Navigation.UpdateCursor ();
        Assert.True (Application.Driver!.GetCursor ().IsVisible);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void TestBottomlessTreeView_MaxDepth_3 ()
    {
        TreeView<string> tv = new () { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        tv.TreeBuilder = new DelegateTreeBuilder<string> (s => new [] { (int.Parse (s) + 1).ToString () });

        tv.AddObject ("1");
        tv.SetScheme (new Scheme ());

        tv.LayoutSubViews ();
        tv.Draw ();

        // Nothing expanded
        DriverAssert.AssertDriverContentsAre (@"└+1
",
                                              output);
        tv.MaxDepth = 3;
        tv.ExpandAll ();
        tv.SetClipToScreen ();
        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"    
└-1
  └-2
    └-3
      └─4
",
                                              output);
    }

    [Fact]
    [SetupFakeApplication]
    public void TestBottomlessTreeView_MaxDepth_5 ()
    {
        TreeView<string> tv = new () { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        tv.TreeBuilder = new DelegateTreeBuilder<string> (s => new [] { (int.Parse (s) + 1).ToString () });

        tv.AddObject ("1");
        tv.SetScheme (new Scheme ());

        tv.LayoutSubViews ();
        tv.Draw ();

        // Nothing expanded
        DriverAssert.AssertDriverContentsAre (@"└+1
",
                                              output);
        tv.MaxDepth = 5;
        tv.ExpandAll ();
        tv.SetClipToScreen ();

        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"    
└-1
  └-2
    └-3
      └-4
        └-5
          └─6
",
                                              output);
        Assert.False (tv.CanExpand ("6"));
        Assert.False (tv.IsExpanded ("6"));

        tv.Collapse ("6");

        Assert.False (tv.CanExpand ("6"));
        Assert.False (tv.IsExpanded ("6"));

        tv.Collapse ("5");

        Assert.True (tv.CanExpand ("5"));
        Assert.False (tv.IsExpanded ("5"));
        tv.SetClipToScreen ();

        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"    
└-1
  └-2
    └-3
      └-4
        └+5
",
                                              output);
    }

    [Fact]
    [SetupFakeApplication]
    public void TestGetObjectOnRow ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };
        tv.BeginInit ();
        tv.EndInit ();
        var n1 = new TreeNode ("normal");
        var n1_1 = new TreeNode ("pink");
        var n1_2 = new TreeNode ("normal");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("pink");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"├-normal
│ ├─pink
│ └─normal
└─pink
",
                                              output);

        Assert.Same (n1, tv.GetObjectOnRow (0));
        Assert.Same (n1_1, tv.GetObjectOnRow (1));
        Assert.Same (n1_2, tv.GetObjectOnRow (2));
        Assert.Same (n2, tv.GetObjectOnRow (3));
        Assert.Null (tv.GetObjectOnRow (4));

        tv.Collapse (n1);
        tv.SetClipToScreen ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"├+normal
└─pink
",
                                              output);

        Assert.Same (n1, tv.GetObjectOnRow (0));
        Assert.Same (n2, tv.GetObjectOnRow (1));
        Assert.Null (tv.GetObjectOnRow (2));
        Assert.Null (tv.GetObjectOnRow (3));
        Assert.Null (tv.GetObjectOnRow (4));
    }

    [Fact]
    [SetupFakeApplication]
    public void TestGetObjectRow ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var n1 = new TreeNode ("normal");
        var n1_1 = new TreeNode ("pink");
        var n1_2 = new TreeNode ("normal");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("pink");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();
        tv.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"├-normal
│ ├─pink
│ └─normal
└─pink
",
                                              output);

        Assert.Equal (0, tv.GetObjectRow (n1));
        Assert.Equal (1, tv.GetObjectRow (n1_1));
        Assert.Equal (2, tv.GetObjectRow (n1_2));
        Assert.Equal (3, tv.GetObjectRow (n2));

        tv.Collapse (n1);

        tv.LayoutSubViews ();
        tv.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"├+normal
└─pink
",
                                              output);
        Assert.Equal (0, tv.GetObjectRow (n1));
        Assert.Null (tv.GetObjectRow (n1_1));
        Assert.Null (tv.GetObjectRow (n1_2));
        Assert.Equal (1, tv.GetObjectRow (n2));

        // scroll down 1
        tv.ScrollOffsetVertical = 1;

        tv.LayoutSubViews ();
        tv.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"└─pink
",
                                              output);
        Assert.Equal (-1, tv.GetObjectRow (n1));
        Assert.Null (tv.GetObjectRow (n1_1));
        Assert.Null (tv.GetObjectRow (n1_2));
        Assert.Equal (0, tv.GetObjectRow (n2));
    }

    [Fact]
    [SetupFakeApplication]
    public void TestTreeView_DrawLineEvent ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        List<DrawTreeViewLineEventArgs<ITreeNode>> eventArgs = new ();

        tv.DrawLine += (s, e) => { eventArgs.Add (e); };

        var n1 = new TreeNode ("root one");
        var n1_1 = new TreeNode ("leaf 1");
        var n1_2 = new TreeNode ("leaf 2");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("root two");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();
        tv.SetClipToScreen ();
        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"
├-root one
│ ├─leaf 1
│ └─leaf 2
└─root two
",
                                              output);
        Assert.Equal (4, eventArgs.Count ());

        Assert.Equal (0, eventArgs [0].Y);
        Assert.Equal (1, eventArgs [1].Y);
        Assert.Equal (2, eventArgs [2].Y);
        Assert.Equal (3, eventArgs [3].Y);

        Assert.All (eventArgs, ea => Assert.Equal (ea.Tree, tv));
        Assert.All (eventArgs, ea => Assert.False (ea.Handled));

        Assert.Equal ("├-root one", eventArgs [0].Cells.Aggregate ("", (s, n) => s += n.Grapheme).TrimEnd ());
        Assert.Equal ("│ ├─leaf 1", eventArgs [1].Cells.Aggregate ("", (s, n) => s += n.Grapheme).TrimEnd ());
        Assert.Equal ("│ └─leaf 2", eventArgs [2].Cells.Aggregate ("", (s, n) => s += n.Grapheme).TrimEnd ());
        Assert.Equal ("└─root two", eventArgs [3].Cells.Aggregate ("", (s, n) => s += n.Grapheme).TrimEnd ());

        Assert.Equal (1, eventArgs [0].IndexOfExpandCollapseSymbol);
        Assert.Equal (3, eventArgs [1].IndexOfExpandCollapseSymbol);
        Assert.Equal (3, eventArgs [2].IndexOfExpandCollapseSymbol);
        Assert.Equal (1, eventArgs [3].IndexOfExpandCollapseSymbol);

        Assert.Equal (2, eventArgs [0].IndexOfModelText);
        Assert.Equal (4, eventArgs [1].IndexOfModelText);
        Assert.Equal (4, eventArgs [2].IndexOfModelText);
        Assert.Equal (2, eventArgs [3].IndexOfModelText);

        Assert.Equal ("root one", eventArgs [0].Model.Text);
        Assert.Equal ("leaf 1", eventArgs [1].Model.Text);
        Assert.Equal ("leaf 2", eventArgs [2].Model.Text);
        Assert.Equal ("root two", eventArgs [3].Model.Text);
    }

    [Fact]
    [SetupFakeApplication]
    public void TestTreeView_DrawLineEvent_Handled ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        tv.DrawLine += (s, e) =>
                       {
                           if (e.Model.Text.Equals ("leaf 1"))
                           {
                               e.Handled = true;

                               for (var i = 0; i < 10; i++)
                               {
                                   e.Tree.AddRune (i, e.Y, new Rune ('F'));
                               }
                           }
                       };

        var n1 = new TreeNode ("root one");
        var n1_1 = new TreeNode ("leaf 1");
        var n1_2 = new TreeNode ("leaf 2");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("root two");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();
        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"
├-root one
FFFFFFFFFF
│ └─leaf 2
└─root two
",
                                              output);
    }

    [Fact]
    [SetupFakeApplication]
    public void TestTreeView_DrawLineEvent_WithScrolling ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        List<DrawTreeViewLineEventArgs<ITreeNode>> eventArgs = new ();

        tv.DrawLine += (s, e) => { eventArgs.Add (e); };

        tv.ScrollOffsetHorizontal = 3;
        tv.ScrollOffsetVertical = 1;

        var n1 = new TreeNode ("root one");
        var n1_1 = new TreeNode ("leaf 1");
        var n1_2 = new TreeNode ("leaf 2");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("root two");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();
        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"
─leaf 1
─leaf 2
oot two
",
                                              output);
        Assert.Equal (3, eventArgs.Count ());

        Assert.Equal (0, eventArgs [0].Y);
        Assert.Equal (1, eventArgs [1].Y);
        Assert.Equal (2, eventArgs [2].Y);

        Assert.All (eventArgs, ea => Assert.Equal (ea.Tree, tv));
        Assert.All (eventArgs, ea => Assert.False (ea.Handled));

        Assert.Equal ("─leaf 1", eventArgs [0].Cells.Aggregate ("", (s, n) => s += n.Grapheme).TrimEnd ());
        Assert.Equal ("─leaf 2", eventArgs [1].Cells.Aggregate ("", (s, n) => s += n.Grapheme).TrimEnd ());
        Assert.Equal ("oot two", eventArgs [2].Cells.Aggregate ("", (s, n) => s += n.Grapheme).TrimEnd ());

        Assert.Equal (0, eventArgs [0].IndexOfExpandCollapseSymbol);
        Assert.Equal (0, eventArgs [1].IndexOfExpandCollapseSymbol);
        Assert.Null (eventArgs [2].IndexOfExpandCollapseSymbol);

        Assert.Equal (1, eventArgs [0].IndexOfModelText);
        Assert.Equal (1, eventArgs [1].IndexOfModelText);
        Assert.Equal (-1, eventArgs [2].IndexOfModelText);

        Assert.Equal ("leaf 1", eventArgs [0].Model.Text);
        Assert.Equal ("leaf 2", eventArgs [1].Model.Text);
        Assert.Equal ("root two", eventArgs [2].Model.Text);
    }

    [Fact]
    [SetupFakeApplication]
    public void TestTreeView_Filter ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var n1 = new TreeNode ("root one");
        var n1_1 = new TreeNode ("leaf 1");
        var n1_2 = new TreeNode ("leaf 2");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("root two");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();
        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"
├-root one
│ ├─leaf 1
│ └─leaf 2
└─root two
",
                                              output);
        TreeViewTextFilter<ITreeNode> filter = new (tv);
        tv.Filter = filter;

        // matches nothing
        filter.Text = "asdfjhasdf";
        tv.SetClipToScreen ();
        tv.Draw ();

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"", output);

        // Matches everything
        filter.Text = "root";
        tv.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"
├-root one
│ ├─leaf 1
│ └─leaf 2
└─root two
",
                                              output);

        // Matches 2 leaf nodes
        filter.Text = "leaf";
        tv.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"
├-root one
│ ├─leaf 1
│ └─leaf 2
",
                                              output);

        // Matches 1 leaf nodes
        filter.Text = "leaf 1";
        tv.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (@"
├-root one
│ ├─leaf 1
",
                                              output);
    }

    [Fact]
    [SetupFakeApplication]
    public void TestTreeViewColor ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };
        tv.BeginInit ();
        tv.EndInit ();
        var n1 = new TreeNode ("normal");
        var n1_1 = new TreeNode ("pink");
        var n1_2 = new TreeNode ("normal");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("pink");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();
        tv.Draw ();

        // create a new scheme
        var pink = new Attribute (Color.Magenta, Color.Black);
        var hotpink = new Attribute (Color.BrightMagenta, Color.Black);

        // Normal drawing of the tree view
        DriverAssert.AssertDriverContentsAre (@"
├-normal
│ ├─pink
│ └─normal
└─pink
",
                                              output);

        // Should all be the same color
        DriverAssert.AssertDriverAttributesAre (@"
0000000000
0000000000
0000000000
0000000000
",
                                                output,
                                                Application.Driver,
                                                tv.GetScheme ().Normal,
                                                pink);

        var pinkScheme = new Scheme { Normal = pink, Focus = hotpink };

        // and a delegate that uses the pink scheme 
        // for nodes "pink"
        tv.ColorGetter = n => n.Text.Equals ("pink") ? pinkScheme : null;

        // redraw now that the custom color
        // delegate is registered
        tv.SetNeedsDraw ();
        tv.SetClipToScreen ();
        tv.Draw ();

        // Same text
        DriverAssert.AssertDriverContentsAre (@"
├-normal
│ ├─pink
│ └─normal
└─pink
",
                                              output);

        // but now the item (only not lines) appear
        // in pink when they are the word "pink"
        DriverAssert.AssertDriverAttributesAre (@"
00000000
00001111
0000000000
001111
",
                                                output,
                                                Application.Driver,
                                                tv.GetScheme ().Normal,
                                                pink);
    }

    #region Test Setup Methods

    private class Factory
    {
        public Car [] Cars { get; set; }
        public override string ToString () => "Factory";
    }

    private class Car
    {
        public string Name { get; set; }
        public override string ToString () => Name;
    }

    private TreeView<object> CreateTree () => CreateTree (out _, out _, out _);

    private TreeView<object> CreateTree (out Factory factory1, out Car car1, out Car car2)
    {
        car1 = new Car ();
        car2 = new Car ();

        factory1 = new Factory { Cars = new [] { car1, car2 } };

        TreeView<object> tree = new (new DelegateTreeBuilder<object> (s => s is Factory f ? f.Cars : null));
        tree.AddObject (factory1);

        return tree;
    }

    #endregion
}
