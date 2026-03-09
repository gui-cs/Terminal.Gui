using System.Text;

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

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_Default_IsTrue ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };
        Assert.True (tv.AllowLetterBasedNavigation);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_NavigatesToNextNodeStartingWithLetter ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var apple = new TreeNode ("apple");
        var banana = new TreeNode ("banana");
        var blueberry = new TreeNode ("blueberry");
        var cherry = new TreeNode ("cherry");
        tv.AddObject (apple);
        tv.AddObject (banana);
        tv.AddObject (blueberry);
        tv.AddObject (cherry);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Initially select apple
        tv.SelectedObject = apple;
        Assert.Equal (apple, tv.SelectedObject);

        // Press 'b' - should navigate to banana
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, tv.SelectedObject);

        // Press 'b' again - should navigate to blueberry
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (blueberry, tv.SelectedObject);

        // Press 'b' again - should cycle back to banana
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, tv.SelectedObject);

        // Press 'c' - should navigate to cherry
        tv.NewKeyDownEvent (Key.C);
        Assert.Equal (cherry, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_WhenDisabled_DoesNotNavigate ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };
        tv.AllowLetterBasedNavigation = false;

        var apple = new TreeNode ("apple");
        var banana = new TreeNode ("banana");
        var cherry = new TreeNode ("cherry");
        tv.AddObject (apple);
        tv.AddObject (banana);
        tv.AddObject (cherry);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Initially select apple
        tv.SelectedObject = apple;
        Assert.Equal (apple, tv.SelectedObject);

        // Press 'b' - should NOT navigate since AllowLetterBasedNavigation is false
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (apple, tv.SelectedObject);

        // Press 'c' - should still be on apple
        tv.NewKeyDownEvent (Key.C);
        Assert.Equal (apple, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_WorksWithNestedNodes ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var fruits = new TreeNode ("Fruits");
        var apple = new TreeNode ("apple");
        var banana = new TreeNode ("banana");
        fruits.Children.Add (apple);
        fruits.Children.Add (banana);

        var vegetables = new TreeNode ("Vegetables");
        var carrot = new TreeNode ("carrot");
        vegetables.Children.Add (carrot);

        tv.AddObject (fruits);
        tv.AddObject (vegetables);
        tv.Expand (fruits);
        tv.Expand (vegetables);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Initially select Fruits
        tv.SelectedObject = fruits;
        Assert.Equal (fruits, tv.SelectedObject);

        // Press 'a' - should navigate to apple
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (apple, tv.SelectedObject);

        // Press 'b' - should navigate to banana
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, tv.SelectedObject);

        // Press 'c' - should navigate to carrot
        tv.NewKeyDownEvent (Key.C);
        Assert.Equal (carrot, tv.SelectedObject);

        // Press 'V' - should navigate to Vegetables
        tv.NewKeyDownEvent (Key.V);
        Assert.Equal (vegetables, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_CaseInsensitive ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var apple = new TreeNode ("Apple");
        var banana = new TreeNode ("Banana");
        var Cherry = new TreeNode ("Cherry");
        tv.AddObject (apple);
        tv.AddObject (banana);
        tv.AddObject (Cherry);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Initially select Apple
        tv.SelectedObject = apple;
        Assert.Equal (apple, tv.SelectedObject);

        // Press lowercase 'b' - should navigate to Banana (case insensitive)
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, tv.SelectedObject);

        // Press lowercase 'c' - should navigate to Cherry
        tv.NewKeyDownEvent (Key.C);
        Assert.Equal (Cherry, tv.SelectedObject);

        // Press lowercase 'a' - should navigate back to Apple
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (apple, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_NoMatchReturnsToStart ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var apple = new TreeNode ("apple");
        var banana = new TreeNode ("banana");
        var cherry = new TreeNode ("cherry");
        tv.AddObject (apple);
        tv.AddObject (banana);
        tv.AddObject (cherry);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Select cherry
        tv.SelectedObject = cherry;
        Assert.Equal (cherry, tv.SelectedObject);

        // Press 'a' - should cycle back and find apple
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (apple, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_WorksWithNumbers ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var item1 = new TreeNode ("1st item");
        var item2 = new TreeNode ("2nd item");
        var item3 = new TreeNode ("3rd item");
        tv.AddObject (item1);
        tv.AddObject (item2);
        tv.AddObject (item3);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Initially select 1st item
        tv.SelectedObject = item1;
        Assert.Equal (item1, tv.SelectedObject);

        // Press '2' - should navigate to 2nd item
        tv.NewKeyDownEvent (Key.D2);
        Assert.Equal (item2, tv.SelectedObject);

        // Press '3' - should navigate to 3rd item
        tv.NewKeyDownEvent (Key.D3);
        Assert.Equal (item3, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_OnlyVisibleNodes ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        var root = new TreeNode ("Root");
        var apple = new TreeNode ("apple");
        var banana = new TreeNode ("banana");
        root.Children.Add (apple);
        root.Children.Add (banana);

        tv.AddObject (root);
        // Don't expand root, so children are not visible

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Select root
        tv.SelectedObject = root;
        Assert.Equal (root, tv.SelectedObject);

        // Press 'a' - should not navigate to apple (not visible)
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (root, tv.SelectedObject);

        // Now expand root to make children visible
        tv.Expand (root);
        tv.LayoutSubViews ();

        // Press 'a' - should now navigate to apple
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (apple, tv.SelectedObject);

        // Press 'b' - should navigate to banana
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_SameLetterInRootAndSubtree ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        // Tree structure:
        // Apple
        // Peach
        //   Abb
        //   Acc
        var appleRoot = new TreeNode ("Apple");
        var peach = new TreeNode ("Peach");
        var abb = new TreeNode ("Abb");
        var acc = new TreeNode ("Acc");
        peach.Children.Add (abb);
        peach.Children.Add (acc);

        tv.AddObject (appleRoot);
        tv.AddObject (peach);
        tv.Expand (peach);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Select Abb
        tv.SelectedObject = abb;
        Assert.Equal (abb, tv.SelectedObject);

        // Press 'a' - should navigate to next 'A' item which is Acc
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (acc, tv.SelectedObject);

        // Press 'a' again - should cycle to Apple (root level)
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (appleRoot, tv.SelectedObject);

        // Press 'a' again - should cycle to Abb
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (abb, tv.SelectedObject);
    }

    [Fact]
    [SetupFakeApplication]
    public void AllowLetterBasedNavigation_CaseVariationsInParentAndChildren ()
    {
        var tv = new TreeView { Driver = ApplicationImpl.Instance.Driver, Width = 20, Height = 10 };

        // Tree structure mimicking file system:
        // D:\
        // SendTo
        //   Desktop
        //   desktop.ini
        var dRoot = new TreeNode ("D:\\");
        var sendTo = new TreeNode ("SendTo");
        var desktop = new TreeNode ("Desktop");
        var desktopIni = new TreeNode ("desktop.ini");
        sendTo.Children.Add (desktop);
        sendTo.Children.Add (desktopIni);

        tv.AddObject (dRoot);
        tv.AddObject (sendTo);
        tv.Expand (sendTo);

        tv.SetScheme (new Scheme ());
        tv.LayoutSubViews ();

        // Select Desktop
        tv.SelectedObject = desktop;
        Assert.Equal (desktop, tv.SelectedObject);

        // Press 'd' - should navigate to next 'd' item which is desktop.ini, NOT back to D:\
        tv.NewKeyDownEvent (Key.D);
        Assert.Equal (desktopIni, tv.SelectedObject);

        // Press 'd' again - should cycle to D:\
        tv.NewKeyDownEvent (Key.D);
        Assert.Equal (dRoot, tv.SelectedObject);

        // Press 'd' again - should go to Desktop
        tv.NewKeyDownEvent (Key.D);
        Assert.Equal (desktop, tv.SelectedObject);
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
