using System.ComponentModel;
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class TreeViewTests
{
    private readonly ITestOutputHelper _output;
    public TreeViewTests (ITestOutputHelper output) { _output = output; }

    /// <summary>Tests that <see cref="TreeView.Expand(object)"/> results in a correct content height</summary>
    [Fact]
    public void ContentHeight_BiggerAfterExpand ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out _, out _);
        Assert.Equal (1, tree.ContentHeight);

        tree.Expand (f);
        Assert.Equal (3, tree.ContentHeight);

        tree.Collapse (f);
        Assert.Equal (1, tree.ContentHeight);
    }

    [Fact]
    public void ContentWidth_BiggerAfterExpand ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);
        tree.BeginInit ();
        tree.EndInit ();

        tree.Viewport = new Rectangle (0, 0, 10, 10);

        //-+Factory
        Assert.Equal (9, tree.GetContentWidth (true));

        car1.Name = "123456789";

        tree.Expand (f);

        //..├-123456789
        Assert.Equal (13, tree.GetContentWidth (true));

        tree.Collapse (f);

        //-+Factory
        Assert.Equal (9, tree.GetContentWidth (true));

    }

    [Fact]
    public void ContentWidth_VisibleVsAll ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out Car car2);
        tree.BeginInit ();
        tree.EndInit ();

        // control only allows 1 row to be viewed at once
        tree.Viewport = new Rectangle (0, 0, 20, 1);

        //-+Factory
        Assert.Equal (9, tree.GetContentWidth (true));
        Assert.Equal (9, tree.GetContentWidth (false));

        car1.Name = "123456789";
        car2.Name = "12345678";

        tree.Expand (f);

        // Although expanded the bigger (longer) child node is not in the rendered area of the control
        Assert.Equal (9, tree.GetContentWidth (true));

        Assert.Equal (
                      13,
                      tree.GetContentWidth (false)
                     ); // If you ask for the global max width it includes the longer child

        // Now that we have scrolled down 1 row we should see the big child
        tree.ScrollOffsetVertical = 1;
        Assert.Equal (13, tree.GetContentWidth (true));
        Assert.Equal (13, tree.GetContentWidth (false));

        // Scroll down so only car2 is visible
        tree.ScrollOffsetVertical = 2;
        Assert.Equal (12, tree.GetContentWidth (true));
        Assert.Equal (13, tree.GetContentWidth (false));

        // Scroll way down (off bottom of control even)
        tree.ScrollOffsetVertical = 5;
        Assert.Equal (0, tree.GetContentWidth (true));
        Assert.Equal (13, tree.GetContentWidth (false));
    }

    [Fact]
    [AutoInitShutdown]
    public void CursorVisibility_MultiSelect ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };

        var n1 = new TreeNode ("normal");
        var n2 = new TreeNode ("pink");
        tv.AddObject (n1);
        tv.AddObject (n2);

        var top = new Toplevel ();
        top.Add (tv);
        Application.Begin (top);

        Assert.True (tv.MultiSelect);
        Assert.True (tv.HasFocus);
        Assert.Equal (CursorVisibility.Invisible, tv.CursorVisibility);

        tv.SelectAll ();
        tv.CursorVisibility = CursorVisibility.Default;
        Application.PositionCursor ();
        Application.Driver!.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Default, tv.CursorVisibility);
        Assert.Equal (CursorVisibility.Default, visibility);
        top.Dispose ();
    }

    [Fact]
    public void EmptyTreeView_ContentSizes ()
    {
        var emptyTree = new TreeView ();
        Assert.Equal (0, emptyTree.ContentHeight);
        Assert.Equal (0, emptyTree.GetContentWidth (true));
        Assert.Equal (0, emptyTree.GetContentWidth (false));
    }

    [Fact]
    public void EmptyTreeViewGeneric_ContentSizes ()
    {
        TreeView<string> emptyTree = new ();
        Assert.Equal (0, emptyTree.ContentHeight);
        Assert.Equal (0, emptyTree.GetContentWidth (true));
        Assert.Equal (0, emptyTree.GetContentWidth (false));
    }

    /// <summary>
    ///     Tests that <see cref="TreeView.GetChildren(object)"/> returns the child objects for the factory.  Note that
    ///     the method only works once the parent branch (Factory) is expanded to expose the child (Car)
    /// </summary>
    [Fact]
    public void GetChildren_ReturnsChildrenOnlyWhenExpanded ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        Assert.Empty (tree.GetChildren (f));
        Assert.Empty (tree.GetChildren (c1));
        Assert.Empty (tree.GetChildren (c2));

        // now when we expand the factory we discover the cars
        tree.Expand (f);

        Assert.Contains (c1, tree.GetChildren (f));
        Assert.Contains (c2, tree.GetChildren (f));
        Assert.Empty (tree.GetChildren (c1));
        Assert.Empty (tree.GetChildren (c2));

        tree.Collapse (f);

        Assert.Empty (tree.GetChildren (f));
        Assert.Empty (tree.GetChildren (c1));
        Assert.Empty (tree.GetChildren (c2));
    }

    /// <summary>
    ///     Tests that <see cref="TreeView.GetParent(object)"/> returns the parent object for Cars (Factories).  Note that
    ///     the method only works once the parent branch (Factory) is expanded to expose the child (Car)
    /// </summary>
    [Fact]
    public void GetParent_ReturnsParentOnlyWhenExpanded ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        Assert.Null (tree.GetParent (f));
        Assert.Null (tree.GetParent (c1));
        Assert.Null (tree.GetParent (c2));

        // now when we expand the factory we discover the cars
        tree.Expand (f);

        Assert.Null (tree.GetParent (f));
        Assert.Equal (f, tree.GetParent (c1));
        Assert.Equal (f, tree.GetParent (c2));

        tree.Collapse (f);

        Assert.Null (tree.GetParent (f));
        Assert.Null (tree.GetParent (c1));
        Assert.Null (tree.GetParent (c2));
    }

    /// <summary>Tests <see cref="TreeView.GetScrollOffsetOf(object)"/> for objects that are as yet undiscovered by the tree</summary>
    [Fact]
    public void GetScrollOffsetOf_MinusOneForUnRevealed ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        // to start with the tree is collapsed and only knows about the root object
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c2));

        // reveal it by expanding the root object
        tree.Expand (f);

        // tree now knows about children
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (2, tree.GetScrollOffsetOf (c2));

        // after collapsing the root node again
        tree.Collapse (f);

        // tree no longer knows about the locations of these objects
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c2));
    }

    [Fact]
    public void GoTo_OnlyAppliesToExposedObjects ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);
        tree.BeginInit ();
        tree.EndInit ();

        // Make tree bounds 1 in height so that EnsureVisible always requires updating scroll offset
        tree.Viewport = new Rectangle (0, 0, 50, 1);

        Assert.Null (tree.SelectedObject);
        Assert.Equal (0, tree.ScrollOffsetVertical);

        // car 1 is not yet exposed
        tree.GoTo (car1);

        Assert.Null (tree.SelectedObject);
        Assert.Equal (0, tree.ScrollOffsetVertical);

        tree.Expand (f);

        // Car1 is now exposed by expanding the factory
        tree.GoTo (car1);

        Assert.Equal (car1, tree.SelectedObject);
        Assert.Equal (1, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void GoToEnd_ShouldNotFailOnEmptyTreeView ()
    {
        var tree = new TreeView ();

        Exception exception = Record.Exception (() => tree.GoToEnd ());

        Assert.Null (exception);
    }

    /// <summary>
    ///     Tests that <see cref="TreeView.IsExpanded(object)"/> and <see cref="TreeView.Expand(object)"/> behaves
    ///     correctly when an object cannot be expanded (because it has no children)
    /// </summary>
    [Fact]
    public void IsExpanded_FalseIfCannotExpand ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c, out _);

        // expose the car by expanding the factory
        tree.Expand (f);

        // car is not expanded
        Assert.False (tree.IsExpanded (c));

        //try to expand the car (should have no effect because cars have no children)
        tree.Expand (c);

        Assert.False (tree.IsExpanded (c));

        // should also be ignored
        tree.Collapse (c);

        Assert.False (tree.IsExpanded (c));

    }

    /// <summary>Tests that <see cref="TreeView.Expand(object)"/> and <see cref="TreeView.IsExpanded(object)"/> are consistent</summary>
    [Fact]
    public void IsExpanded_TrueAfterExpand ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out _, out _);
        Assert.False (tree.IsExpanded (f));

        tree.Expand (f);
        Assert.True (tree.IsExpanded (f));

        tree.Collapse (f);
        Assert.False (tree.IsExpanded (f));
    }

    [Fact]
    public void MultiSelect_GetAllSelectedObjects ()
    {
        var tree = new TreeView ();

        TreeNode l1;
        TreeNode l2;
        TreeNode l3;
        TreeNode l4;

        var root = new TreeNode ("Root");
        root.Children.Add (l1 = new TreeNode ("Leaf1"));
        root.Children.Add (l2 = new TreeNode ("Leaf2"));
        root.Children.Add (l3 = new TreeNode ("Leaf3"));
        root.Children.Add (l4 = new TreeNode ("Leaf4"));

        tree.AddObject (root);
        tree.MultiSelect = true;

        tree.Expand (root);
        Assert.Empty (tree.GetAllSelectedObjects ());

        tree.SelectedObject = root;

        Assert.Single (tree.GetAllSelectedObjects (), root);

        // move selection down 1
        tree.AdjustSelection (1);

        Assert.Single (tree.GetAllSelectedObjects (), l1);

        // expand selection down 2 (e.g. shift down twice)
        tree.AdjustSelection (1, true);
        tree.AdjustSelection (1, true);

        Assert.Equal (3, tree.GetAllSelectedObjects ().Count ());
        Assert.Contains (l1, tree.GetAllSelectedObjects ());
        Assert.Contains (l2, tree.GetAllSelectedObjects ());
        Assert.Contains (l3, tree.GetAllSelectedObjects ());

        tree.Collapse (root);

        // No selected objects since the root was collapsed
        Assert.Empty (tree.GetAllSelectedObjects ());
    }

    [Fact]
    public void ObjectActivated_Called ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);

        object activated = null;
        var called = false;

        // register for the event
        tree.ObjectActivated += (s, e) =>
                                {
                                    activated = e.ActivatedObject;
                                    called = true;
                                };

        Assert.False (called);

        // no object is selected yet so no event should happen
        tree.NewKeyDownEvent (Key.Enter);

        Assert.Null (activated);
        Assert.False (called);

        // down to select factory
        tree.NewKeyDownEvent (Key.CursorDown);

        tree.NewKeyDownEvent (Key.Enter);

        Assert.True (called);
        Assert.Same (f, activated);
    }

    [Fact]

    public void ObjectActivated_CustomKey ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);

        tree.ObjectActivationKey = KeyCode.Delete;
        object activated = null;
        var called = false;

        // register for the event
        tree.ObjectActivated += (s, e) =>
                                {
                                    activated = e.ActivatedObject;
                                    called = true;
                                };

        Assert.False (called);

        // no object is selected yet so no event should happen
        tree.NewKeyDownEvent (Key.Enter);

        Assert.Null (activated);
        Assert.False (called);

        // down to select factory
        tree.NewKeyDownEvent (Key.CursorDown);

        tree.NewKeyDownEvent (Key.Enter);

        // Enter is not the activation key in this unit test
        Assert.Null (activated);
        Assert.False (called);

        // Delete is the activation key in this test so should result in activation occurring
        tree.NewKeyDownEvent (Key.Delete);

        Assert.True (called);
        Assert.Same (f, activated);

    }

    [Fact]
    public void ObjectActivationButton_DoubleClick ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);

        object activated = null;
        var called = false;

        // register for the event
        tree.ObjectActivated += (s, e) =>
                                {
                                    activated = e.ActivatedObject;
                                    called = true;
                                };

        Assert.False (called);

        // double click triggers activation
        tree.NewMouseEvent (new MouseEventArgs { Flags = MouseFlags.Button1DoubleClicked });

        Assert.True (called);
        Assert.Same (f, activated);
        Assert.Same (f, tree.SelectedObject);

    }

    [Fact]
    public void ObjectActivationButton_RightClick ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);

        tree.ObjectActivationButton = MouseFlags.Button2Clicked;
        tree.ExpandAll ();

        object activated = null;
        var called = false;

        // register for the event
        tree.ObjectActivated += (s, e) =>
                                {
                                    activated = e.ActivatedObject;
                                    called = true;
                                };

        Assert.False (called);

        // double click does nothing because we changed button binding to right click
        tree.NewMouseEvent (new MouseEventArgs { Position = new (0, 1), Flags = MouseFlags.Button1DoubleClicked });

        Assert.Null (activated);
        Assert.False (called);

        tree.NewMouseEvent (new MouseEventArgs { Position = new (0, 1), Flags = MouseFlags.Button2Clicked });

        Assert.True (called);
        Assert.Same (car1, activated);
        Assert.Same (car1, tree.SelectedObject);
    }

    [Fact]
    public void ObjectActivationButton_SetToNull ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);
        Assert.Null (tree.SelectedObject);

        Assert.True (tree.SetFocus ());
        tree.SelectedObject = null;
        Assert.Null (tree.SelectedObject);

        // disable activation
        tree.ObjectActivationButton = null;

        object activated = null;
        var called = false;

        // register for the event
        tree.ObjectActivated += (s, e) =>
                                {
                                    activated = e.ActivatedObject;
                                    called = true;
                                };

        Assert.False (called);


        // double click does nothing because we changed button to null
        tree.NewMouseEvent (new MouseEventArgs { Flags = MouseFlags.Button1DoubleClicked });

        Assert.False (called);
        Assert.Null (activated);
        Assert.Null (tree.SelectedObject);
    }

    /// <summary>
    ///     Same as <see cref="RefreshObject_AfterChangingChildrenGetterDuringRuntime"/> but uses
    ///     <see cref="TreeView.RebuildTree()"/> instead of <see cref="TreeView.RefreshObject(object, bool)"/>
    /// </summary>
    [Fact]
    public void RebuildTree_AfterChangingChildrenGetterDuringRuntime ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        var wheel = "Shiny Wheel";

        // Expand the Factory
        tree.Expand (f);

        // c1 cannot have children
        Assert.Equal (f, tree.GetParent (c1));

        // expanding it does nothing
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        // change the children getter so that now cars can have wheels
        tree.TreeBuilder = new DelegateTreeBuilder<object> (
                                                            o =>

                                                                // factories have cars
                                                                o is Factory
                                                                    ? new object [] { c1, c2 }

                                                                    // cars have wheels
                                                                    : new object [] { wheel }
                                                           );

        // still cannot expand
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        // Rebuild the tree
        tree.RebuildTree ();

        // Rebuild should not have collapsed any branches or done anything wierd
        Assert.True (tree.IsExpanded (f));

        tree.Expand (c1);
        Assert.True (tree.IsExpanded (c1));
        Assert.Equal (wheel, tree.GetChildren (c1).FirstOrDefault ());
    }

    /// <summary>
    ///     Tests how the tree adapts to changes in the ChildrenGetter delegate during runtime when some branches are
    ///     expanded and the new delegate returns children for a node that previously didn't have any children
    /// </summary>
    [Fact]
    public void RefreshObject_AfterChangingChildrenGetterDuringRuntime ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        var wheel = "Shiny Wheel";

        // Expand the Factory
        tree.Expand (f);

        // c1 cannot have children
        Assert.Equal (f, tree.GetParent (c1));

        // expanding it does nothing
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        // change the children getter so that now cars can have wheels
        tree.TreeBuilder = new DelegateTreeBuilder<object> (
                                                            o =>

                                                                // factories have cars
                                                                o is Factory
                                                                    ? new object [] { c1, c2 }

                                                                    // cars have wheels
                                                                    : new object [] { wheel }
                                                           );

        // still cannot expand
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        tree.RefreshObject (c1);
        tree.Expand (c1);
        Assert.True (tree.IsExpanded (c1));
        Assert.Equal (wheel, tree.GetChildren (c1).FirstOrDefault ());
    }

    /// <summary>
    ///     Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree
    ///     using <see cref="TreeView.RefreshObject(object, bool)"/>
    /// </summary>
    [Fact]
    public void RefreshObject_ChildRemoved ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        //reveal it by expanding the root object
        tree.Expand (f);

        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (2, tree.GetScrollOffsetOf (c2));

        // Factory now no longer makes Car c1 (only c2)
        f.Cars = new [] { c2 };

        // Tree does not know this yet
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (2, tree.GetScrollOffsetOf (c2));

        // If the user has selected the node c1
        tree.SelectedObject = c1;

        // When we refresh the tree
        tree.RefreshObject (f);

        // Now tree knows that factory has only one child node c2
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (1, tree.GetScrollOffsetOf (c2));

        // The old selection was c1 which is now gone so selection should default to the parent of that branch (the factory)
        Assert.Equal (f, tree.SelectedObject);
    }

    /// <summary>
    ///     Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree
    ///     using <see cref="TreeView.RefreshObject(object, bool)"/>
    /// </summary>
    [Fact]
    public void RefreshObject_EqualityTest ()
    {
        var obj1 = new EqualityTestObject { Name = "Bob", Age = 1 };
        var obj2 = new EqualityTestObject { Name = "Bob", Age = 2 };
        ;

        var root = "root";

        TreeView<object> tree = new ();

        tree.TreeBuilder =
            new DelegateTreeBuilder<object> (s => ReferenceEquals (s, root) ? new object [] { obj1 } : null);
        tree.AddObject (root);

        // Tree is not expanded so the root has no children yet
        Assert.Empty (tree.GetChildren (root));

        tree.Expand (root);

        // now that the tree is expanded we should get our child returned
        Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj1, child)));

        // change the getter to return an Equal object (but not the same reference - obj2)
        tree.TreeBuilder =
            new DelegateTreeBuilder<object> (s => ReferenceEquals (s, root) ? new object [] { obj2 } : null);

        // tree has cached the knowledge of what children the root has so won't know about the change (we still get obj1)
        Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj1, child)));

        // now that we refresh the root we should get the new child reference (obj2)
        tree.RefreshObject (root);
        Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj2, child)));
    }

    /// <summary>Tests illegal ranges for <see cref="TreeView.ScrollOffset"/></summary>
    [Fact]
    public void ScrollOffset_CannotBeNegative ()
    {
        TreeView<object> tree = CreateTree ();

        Assert.Equal (0, tree.ScrollOffsetVertical);

        tree.ScrollOffsetVertical = -100;
        Assert.Equal (0, tree.ScrollOffsetVertical);

        tree.ScrollOffsetVertical = 10;
        Assert.Equal (10, tree.ScrollOffsetVertical);
    }

    [Fact]
    [SetupFakeDriver]
    public void TestBottomlessTreeView_MaxDepth_3 ()
    {
        TreeView<string> tv = new () { Width = 20, Height = 10 };

        tv.TreeBuilder = new DelegateTreeBuilder<string> (
                                                          s => new [] { (int.Parse (s) + 1).ToString () }
                                                         );

        tv.AddObject ("1");
        tv.ColorScheme = new ColorScheme ();

        tv.LayoutSubviews ();
        tv.Draw ();

        // Nothing expanded
        TestHelpers.AssertDriverContentsAre (
                                             @"└+1
",
                                             _output
                                            );
        tv.MaxDepth = 3;
        tv.ExpandAll ();
        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"    
└-1
  └-2
    └-3
      └─4
",
                                             _output
                                            );
    }

    [Fact]
    [SetupFakeDriver]
    public void TestBottomlessTreeView_MaxDepth_5 ()
    {
        TreeView<string> tv = new () { Width = 20, Height = 10 };

        tv.TreeBuilder = new DelegateTreeBuilder<string> (
                                                          s => new [] { (int.Parse (s) + 1).ToString () }
                                                         );

        tv.AddObject ("1");
        tv.ColorScheme = new ColorScheme ();

        tv.LayoutSubviews ();
        tv.Draw ();

        // Nothing expanded
        TestHelpers.AssertDriverContentsAre (
                                             @"└+1
",
                                             _output
                                            );
        tv.MaxDepth = 5;
        tv.ExpandAll ();

        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"    
└-1
  └-2
    └-3
      └-4
        └-5
          └─6
",
                                             _output
                                            );
        Assert.False (tv.CanExpand ("6"));
        Assert.False (tv.IsExpanded ("6"));

        tv.Collapse ("6");

        Assert.False (tv.CanExpand ("6"));
        Assert.False (tv.IsExpanded ("6"));

        tv.Collapse ("5");

        Assert.True (tv.CanExpand ("5"));
        Assert.False (tv.IsExpanded ("5"));

        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"    
└-1
  └-2
    └-3
      └-4
        └+5
",
                                             _output
                                            );
    }

    [Fact]
    [SetupFakeDriver]
    public void TestGetObjectOnRow ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };
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

        tv.ColorScheme = new ColorScheme ();
        tv.LayoutSubviews ();
        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"├-normal
│ ├─pink
│ └─normal
└─pink
",
                                             _output
                                            );

        Assert.Same (n1, tv.GetObjectOnRow (0));
        Assert.Same (n1_1, tv.GetObjectOnRow (1));
        Assert.Same (n1_2, tv.GetObjectOnRow (2));
        Assert.Same (n2, tv.GetObjectOnRow (3));
        Assert.Null (tv.GetObjectOnRow (4));

        tv.Collapse (n1);

        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"├+normal
└─pink
",
                                             _output
                                            );

        Assert.Same (n1, tv.GetObjectOnRow (0));
        Assert.Same (n2, tv.GetObjectOnRow (1));
        Assert.Null (tv.GetObjectOnRow (2));
        Assert.Null (tv.GetObjectOnRow (3));
        Assert.Null (tv.GetObjectOnRow (4));
    }

    [Fact]
    [SetupFakeDriver]
    public void TestGetObjectRow ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };

        var n1 = new TreeNode ("normal");
        var n1_1 = new TreeNode ("pink");
        var n1_2 = new TreeNode ("normal");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("pink");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.ColorScheme = new ColorScheme ();
        tv.LayoutSubviews ();
        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"├-normal
│ ├─pink
│ └─normal
└─pink
",
                                             _output
                                            );

        Assert.Equal (0, tv.GetObjectRow (n1));
        Assert.Equal (1, tv.GetObjectRow (n1_1));
        Assert.Equal (2, tv.GetObjectRow (n1_2));
        Assert.Equal (3, tv.GetObjectRow (n2));

        tv.Collapse (n1);

        tv.LayoutSubviews ();
        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"├+normal
└─pink
",
                                             _output
                                            );
        Assert.Equal (0, tv.GetObjectRow (n1));
        Assert.Null (tv.GetObjectRow (n1_1));
        Assert.Null (tv.GetObjectRow (n1_2));
        Assert.Equal (1, tv.GetObjectRow (n2));

        // scroll down 1
        tv.ScrollOffsetVertical = 1;

        tv.LayoutSubviews ();
        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"└─pink
",
                                             _output
                                            );
        Assert.Equal (-1, tv.GetObjectRow (n1));
        Assert.Null (tv.GetObjectRow (n1_1));
        Assert.Null (tv.GetObjectRow (n1_2));
        Assert.Equal (0, tv.GetObjectRow (n2));
    }

    [Fact]
    [SetupFakeDriver]
    public void TestTreeView_DrawLineEvent ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };

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

        tv.ColorScheme = new ColorScheme ();
        tv.LayoutSubviews ();
        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"
├-root one
│ ├─leaf 1
│ └─leaf 2
└─root two
",
                                             _output
                                            );
        Assert.Equal (4, eventArgs.Count ());

        Assert.Equal (0, eventArgs [0].Y);
        Assert.Equal (1, eventArgs [1].Y);
        Assert.Equal (2, eventArgs [2].Y);
        Assert.Equal (3, eventArgs [3].Y);

        Assert.All (eventArgs, ea => Assert.Equal (ea.Tree, tv));
        Assert.All (eventArgs, ea => Assert.False (ea.Handled));

        Assert.Equal ("├-root one", eventArgs [0].Cells.Aggregate ("", (s, n) => s += n.Rune).TrimEnd ());
        Assert.Equal ("│ ├─leaf 1", eventArgs [1].Cells.Aggregate ("", (s, n) => s += n.Rune).TrimEnd ());
        Assert.Equal ("│ └─leaf 2", eventArgs [2].Cells.Aggregate ("", (s, n) => s += n.Rune).TrimEnd ());
        Assert.Equal ("└─root two", eventArgs [3].Cells.Aggregate ("", (s, n) => s += n.Rune).TrimEnd ());

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
    [SetupFakeDriver]
    public void TestTreeView_DrawLineEvent_Handled ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };

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

        tv.ColorScheme = new ColorScheme ();
        tv.LayoutSubviews ();
        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"
├-root one
FFFFFFFFFF
│ └─leaf 2
└─root two
",
                                             _output
                                            );
    }

    [Fact]
    [SetupFakeDriver]
    public void TestTreeView_DrawLineEvent_WithScrolling ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };

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

        tv.ColorScheme = new ColorScheme ();
        tv.LayoutSubviews ();
        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"
─leaf 1
─leaf 2
oot two
",
                                             _output
                                            );
        Assert.Equal (3, eventArgs.Count ());

        Assert.Equal (0, eventArgs [0].Y);
        Assert.Equal (1, eventArgs [1].Y);
        Assert.Equal (2, eventArgs [2].Y);

        Assert.All (eventArgs, ea => Assert.Equal (ea.Tree, tv));
        Assert.All (eventArgs, ea => Assert.False (ea.Handled));

        Assert.Equal ("─leaf 1", eventArgs [0].Cells.Aggregate ("", (s, n) => s += n.Rune).TrimEnd ());
        Assert.Equal ("─leaf 2", eventArgs [1].Cells.Aggregate ("", (s, n) => s += n.Rune).TrimEnd ());
        Assert.Equal ("oot two", eventArgs [2].Cells.Aggregate ("", (s, n) => s += n.Rune).TrimEnd ());

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
    [SetupFakeDriver]
    public void TestTreeView_Filter ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };

        var n1 = new TreeNode ("root one");
        var n1_1 = new TreeNode ("leaf 1");
        var n1_2 = new TreeNode ("leaf 2");
        n1.Children.Add (n1_1);
        n1.Children.Add (n1_2);

        var n2 = new TreeNode ("root two");
        tv.AddObject (n1);
        tv.AddObject (n2);
        tv.Expand (n1);

        tv.ColorScheme = new ColorScheme ();
        tv.LayoutSubviews ();
        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"
├-root one
│ ├─leaf 1
│ └─leaf 2
└─root two
",
                                             _output
                                            );
        TreeViewTextFilter<ITreeNode> filter = new (tv);
        tv.Filter = filter;

        // matches nothing
        filter.Text = "asdfjhasdf";
        tv.Draw ();

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"",
                                             _output
                                            );

        // Matches everything
        filter.Text = "root";
        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"
├-root one
│ ├─leaf 1
│ └─leaf 2
└─root two
",
                                             _output
                                            );

        // Matches 2 leaf nodes
        filter.Text = "leaf";
        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"
├-root one
│ ├─leaf 1
│ └─leaf 2
",
                                             _output
                                            );

        // Matches 1 leaf nodes
        filter.Text = "leaf 1";
        tv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"
├-root one
│ ├─leaf 1
",
                                             _output
                                            );
    }

    [Fact]
    [SetupFakeDriver]
    public void TestTreeViewColor ()
    {
        var tv = new TreeView { Width = 20, Height = 10 };
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

        tv.ColorScheme = new ColorScheme ();
        tv.LayoutSubviews ();
        tv.Draw ();

        // create a new color scheme
        var pink = new Attribute (Color.Magenta, Color.Black);
        var hotpink = new Attribute (Color.BrightMagenta, Color.Black);

        // Normal drawing of the tree view
        TestHelpers.AssertDriverContentsAre (
                                             @"
├-normal
│ ├─pink
│ └─normal
└─pink
",
                                             _output
                                            );

        // Should all be the same color
        TestHelpers.AssertDriverAttributesAre (
                                               @"
0000000000
0000000000
0000000000
0000000000
",
                                               Application.Driver,
                                               tv.ColorScheme.Normal,
                                               pink
                                              );

        var pinkScheme = new ColorScheme { Normal = pink, Focus = hotpink };

        // and a delegate that uses the pink color scheme 
        // for nodes "pink"
        tv.ColorGetter = n => n.Text.Equals ("pink") ? pinkScheme : null;

        // redraw now that the custom color
        // delegate is registered
        tv.SetNeedsDisplay ();
        tv.Draw ();

        // Same text
        TestHelpers.AssertDriverContentsAre (
                                             @"
├-normal
│ ├─pink
│ └─normal
└─pink
",
                                             _output
                                            );

        // but now the item (only not lines) appear
        // in pink when they are the word "pink"
        TestHelpers.AssertDriverAttributesAre (
                                               @"
00000000
00001111
0000000000
001111
",
                                               Application.Driver,
                                               tv.ColorScheme.Normal,
                                               pink
                                              );
    }

    [Fact]
    public void TreeNode_WorksWithoutDelegate ()
    {
        var tree = new TreeView ();

        var root = new TreeNode ("Root");
        root.Children.Add (new TreeNode ("Leaf1"));
        root.Children.Add (new TreeNode ("Leaf2"));

        tree.AddObject (root);

        tree.Expand (root);
        Assert.Equal (2, tree.GetChildren (root).Count ());
    }

    /// <summary>Test object which considers for equality only <see cref="Name"/></summary>
    private class EqualityTestObject
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public override bool Equals (object obj) { return obj is EqualityTestObject eto && Equals (Name, eto.Name); }
        public override int GetHashCode () { return Name?.GetHashCode () ?? base.GetHashCode (); }
    }

    #region Test Setup Methods

    private class Factory
    {
        public Car [] Cars { get; set; }
        public override string ToString () { return "Factory"; }
    }

    private class Car
    {
        public string Name { get; set; }
        public override string ToString () { return Name; }
    }

    private TreeView<object> CreateTree () { return CreateTree (out _, out _, out _); }

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


    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new TreeView ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var treeView = new TreeView ();
        var accepted = false;

        treeView.Accepting += OnAccept;
        treeView.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;
        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }


    [Fact]
    public void Accept_Command_Accepts_and_ActivatesObject ()
    {
        var treeView = CreateTree (out Factory f, out Car car1, out _);
        Assert.NotNull (car1);
        treeView.SelectedObject = car1;

        var accepted = false;
        var activated = false;
        object selectedObject = null;

        treeView.Accepting += Accept;
        treeView.ObjectActivated += ObjectActivated;

        treeView.InvokeCommand (Command.Accept);

        Assert.True (accepted);
        Assert.True (activated);
        Assert.Equal (car1, selectedObject);

        return;

        void ObjectActivated (object sender, ObjectActivatedEventArgs<object> e)
        {
            activated = true;
            selectedObject = e.ActivatedObject;
        }
        void Accept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Cancel_Event_Prevents_ObjectActivated ()
    {
        var treeView = CreateTree (out Factory f, out Car car1, out _);
        treeView.SelectedObject = car1;
        var accepted = false;
        var activated = false;
        object selectedObject = null;

        treeView.Accepting += Accept;
        treeView.ObjectActivated += ObjectActivated;

        treeView.InvokeCommand (Command.Accept);

        Assert.True (accepted);
        Assert.False (activated);
        Assert.Null (selectedObject);

        return;

        void ObjectActivated (object sender, ObjectActivatedEventArgs<object> e)
        {
            activated = true;
            selectedObject = e.ActivatedObject;
        }

        void Accept (object sender, CommandEventArgs e)
        {
            accepted = true;
            e.Cancel = true;
        }
    }
}
