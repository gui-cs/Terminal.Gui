namespace Terminal.Gui.ViewTests;

[Collection ("Global Test Setup")]
public class SubViewTests
{
    [Fact]
    public void SuperViewChanged_Raised_On_Add ()
    {
        var super = new View { };
        var sub = new View ();

        int superRaisedCount = 0;
        int subRaisedCount = 0;

        super.SuperViewChanged += (s, e) =>
                              {
                                    superRaisedCount++;
                              };
        sub.SuperViewChanged += (s, e) =>
                                {
                                    if (e.SuperView is {})
                                    {
                                        subRaisedCount++;
                                    }
                                };

        super.Add (sub);
        Assert.True (super.SubViews.Count == 1);
        Assert.Equal (super, sub.SuperView);
        Assert.Equal (0, superRaisedCount);
        Assert.Equal (1, subRaisedCount);
    }

    [Fact]
    public void SuperViewChanged_Raised_On_Remove ()
    {
        var super = new View { };
        var sub = new View ();

        int superRaisedCount = 0;
        int subRaisedCount = 0;

        super.SuperViewChanged += (s, e) =>
                                {
                                    superRaisedCount++;
                                };
        sub.SuperViewChanged += (s, e) =>
                              {
                                  if (e.SuperView is null)
                                  {
                                      subRaisedCount++;
                                  }
                              };

        super.Add (sub);
        Assert.True (super.SubViews.Count == 1);
        Assert.Equal (super, sub.SuperView);
        Assert.Equal (0, superRaisedCount);
        Assert.Equal (0, subRaisedCount);

        super.Remove (sub);
        Assert.Empty (super.SubViews);
        Assert.NotEqual (super, sub.SuperView);
        Assert.Equal (0, superRaisedCount);
        Assert.Equal (1, subRaisedCount);
    }

    [Fact]
    public void SuperView_Set_On_Add_Remove ()
    {
        var superView = new View ();
        var view = new View ();
        Assert.Null (view.SuperView);
        superView.Add (view);
        Assert.Equal (superView, view.SuperView);
        superView.Remove (view);
        Assert.Null (view.SuperView);
    }

    // TODO: Consider a feature that will change the ContentSize to fit the subviews.
    [Fact]
    public void Add_Does_Not_Impact_ContentSize ()
    {
        var view = new View ();
        view.SetContentSize (new Size (1, 1));

        var subview = new View
        {
            X = 10,
            Y = 10
        };

        Assert.Equal (new (1, 1), view.GetContentSize ());
        view.Add (subview);
        Assert.Equal (new (1, 1), view.GetContentSize ());
    }

    [Fact]
    public void Remove_Does_Not_Impact_ContentSize ()
    {
        var view = new View ();
        view.SetContentSize (new Size (1, 1));

        var subview = new View
        {
            X = 10,
            Y = 10
        };

        Assert.Equal (new (1, 1), view.GetContentSize ());
        view.Add (subview);
        Assert.Equal (new (1, 1), view.GetContentSize ());

        view.SetContentSize (new Size (5, 5));
        Assert.Equal (new (5, 5), view.GetContentSize ());

        view.Remove (subview);
        Assert.Equal (new (5, 5), view.GetContentSize ());
    }

    [Theory]
    [InlineData (ViewArrangement.Fixed)]
    [InlineData (ViewArrangement.Overlapped)]
    public void MoveSubViewToEnd_ViewArrangement (ViewArrangement arrangement)
    {
        View superView = new () { Arrangement = arrangement };

        var subview1 = new View
        {
            Id = "subview1"
        };

        var subview2 = new View
        {
            Id = "subview2"
        };

        var subview3 = new View
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToEnd (subview1);
        Assert.Equal ([subview2, subview3, subview1], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview2);
        Assert.Equal ([subview3, subview1, subview2], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview3);
        Assert.Equal ([subview1, subview2, subview3], superView.SubViews.ToArray ());
    }

    [Fact]
    public void MoveSubViewToStart ()
    {
        View superView = new ();

        var subview1 = new View
        {
            Id = "subview1"
        };

        var subview2 = new View
        {
            Id = "subview2"
        };

        var subview3 = new View
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToStart (subview2);
        Assert.Equal (subview2, superView.SubViews.ElementAt (0));

        superView.MoveSubViewToStart (subview3);
        Assert.Equal (subview3, superView.SubViews.ElementAt (0));
    }

    [Fact]
    public void MoveSubViewTowardsFront ()
    {
        View superView = new ();

        var subview1 = new View
        {
            Id = "subview1"
        };

        var subview2 = new View
        {
            Id = "subview2"
        };

        var subview3 = new View
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewTowardsStart (subview2);
        Assert.Equal (subview2, superView.SubViews.ElementAt (0));

        superView.MoveSubViewTowardsStart (subview3);
        Assert.Equal (subview3, superView.SubViews.ElementAt (1));

        // Already at front, what happens?
        superView.MoveSubViewTowardsStart (subview2);
        Assert.Equal (subview2, superView.SubViews.ElementAt (0));
    }

    [Fact]
    public void MoveSubViewToEnd ()
    {
        View superView = new ();

        var subview1 = new View
        {
            Id = "subview1"
        };

        var subview2 = new View
        {
            Id = "subview2"
        };

        var subview3 = new View
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToEnd (subview1);
        Assert.Equal (subview1, superView.SubViews.ToArray () [^1]);

        superView.MoveSubViewToEnd (subview2);
        Assert.Equal (subview2, superView.SubViews.ToArray () [^1]);
    }

    [Fact]
    public void MoveSubViewTowardsEnd ()
    {
        View superView = new ();

        var subview1 = new View
        {
            Id = "subview1"
        };

        var subview2 = new View
        {
            Id = "subview2"
        };

        var subview3 = new View
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewTowardsEnd (subview2);
        Assert.Equal (subview2, superView.SubViews.ToArray() [^1]);

        superView.MoveSubViewTowardsEnd (subview1);
        Assert.Equal (subview1, superView.SubViews.ToArray() [1]);

        // Already at end, what happens?
        superView.MoveSubViewTowardsEnd (subview2);
        Assert.Equal (subview2, superView.SubViews.ToArray() [^1]);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNull_ReturnsFalse ()
    {
        // Arrange
        var start = new View ();

        // Act
        bool result = View.IsInHierarchy (start, null);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void IsInHierarchy_StartIsNull_ReturnsFalse ()
    {
        // Arrange
        var view = new View ();

        // Act
        bool result = View.IsInHierarchy (null, view);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsStart_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();

        // Act
        bool result = View.IsInHierarchy (start, start);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsDirectSubView_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();
        start.Add (subview);

        // Act
        bool result = View.IsInHierarchy (start, subview);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNestedSubView_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();
        var nestedSubView = new View ();
        start.Add (subview);
        subview.Add (nestedSubView);

        // Act
        bool result = View.IsInHierarchy (start, nestedSubView);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNotInHierarchy_ReturnsFalse ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();

        // Act
        bool result = View.IsInHierarchy (start, subview);

        // Assert
        Assert.False (result);
    }

    [Theory]
    [CombinatorialData]
    public void IsInHierarchy_ViewIsInAdornments_ReturnsTrue (bool includeAdornments)
    {
        // Arrange
        var start = new View
        {
            Id = "start"
        };

        var inPadding = new View
        {
            Id = "inPadding"
        };

        start.Padding!.Add (inPadding);

        // Act
        bool result = View.IsInHierarchy (start, inPadding, includeAdornments);

        // Assert
        Assert.Equal (includeAdornments, result);
    }

    [Fact]
    public void SuperView_Set_Raises_SuperViewChangedEvents ()
    {
        // Arrange
        var view = new View ();
        var superView = new View ();

        int superViewChangedCount = 0;
        //int superViewChangingCount = 0;

        view.SuperViewChanged += (s, e) =>
        {
            superViewChangedCount++;
        };

        //view.SuperViewChanging += (s, e) =>
        //{
        //    superViewChangingCount++;
        //};

        // Act
        superView.Add (view);

        // Assert
        //Assert.Equal (1, superViewChangingCount);
        Assert.Equal (1, superViewChangedCount);

    }

    [Fact]
    public void GetTopSuperView_Test ()
    {
        var v1 = new View ();
        var fv1 = new FrameView ();
        fv1.Add (v1);
        var tf1 = new TextField ();
        var w1 = new Window ();
        w1.Add (fv1, tf1);
        var top1 = new Toplevel ();
        top1.Add (w1);

        var v2 = new View ();
        var fv2 = new FrameView ();
        fv2.Add (v2);
        var tf2 = new TextField ();
        var w2 = new Window ();
        w2.Add (fv2, tf2);
        var top2 = new Toplevel ();
        top2.Add (w2);

        Assert.Equal (top1, v1.GetTopSuperView ());
        Assert.Equal (top2, v2.GetTopSuperView ());

        v1.Dispose ();
        fv1.Dispose ();
        tf1.Dispose ();
        w1.Dispose ();
        top1.Dispose ();
        v2.Dispose ();
        fv2.Dispose ();
        tf2.Dispose ();
        w2.Dispose ();
        top2.Dispose ();
    }


    [Fact]
    public void Initialized_Event_Comparing_With_Added_Event ()
    {
        var top = new Toplevel { Id = "0" }; // Frame: 0, 0, 80, 25; Viewport: 0, 0, 80, 25

        var winAddedToTop = new Window
        {
            Id = "t", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 0, 0, 80, 25; Viewport: 0, 0, 78, 23

        var v1AddedToWin = new View
        {
            Id = "v1", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var v2AddedToWin = new View
        {
            Id = "v2", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var svAddedTov1 = new View
        {
            Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (same as it's superview v1AddedToWin)

        int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

        winAddedToTop.SubViewAdded += (s, e) =>
        {
            Assert.Equal (e.SuperView.Frame.Width, winAddedToTop.Frame.Width);
            Assert.Equal (e.SuperView.Frame.Height, winAddedToTop.Frame.Height);
        };

        v1AddedToWin.SubViewAdded += (s, e) =>
                                     {
                                         Assert.Equal (e.SuperView.Frame.Width, v1AddedToWin.Frame.Width);
                                         Assert.Equal (e.SuperView.Frame.Height, v1AddedToWin.Frame.Height);
                                     };

        v2AddedToWin.SubViewAdded += (s, e) =>
                                     {
                                         Assert.Equal (e.SuperView.Frame.Width, v2AddedToWin.Frame.Width);
                                         Assert.Equal (e.SuperView.Frame.Height, v2AddedToWin.Frame.Height);
                                     };

        svAddedTov1.SubViewAdded += (s, e) =>
                                    {
                                        Assert.Equal (e.SuperView.Frame.Width, svAddedTov1.Frame.Width);
                                        Assert.Equal (e.SuperView.Frame.Height, svAddedTov1.Frame.Height);
                                    };

        top.Initialized += (s, e) =>
        {
            tc++;
            Assert.Equal (1, tc);
            Assert.Equal (1, wc);
            Assert.Equal (1, v1c);
            Assert.Equal (1, v2c);
            Assert.Equal (1, sv1c);

            Assert.True (top.CanFocus);
            Assert.True (winAddedToTop.CanFocus);
            Assert.False (v1AddedToWin.CanFocus);
            Assert.False (v2AddedToWin.CanFocus);
            Assert.False (svAddedTov1.CanFocus);

            Application.LayoutAndDraw ();
        };

        winAddedToTop.Initialized += (s, e) =>
        {
            wc++;
            Assert.Equal (top.Viewport.Width, winAddedToTop.Frame.Width);
            Assert.Equal (top.Viewport.Height, winAddedToTop.Frame.Height);
        };

        v1AddedToWin.Initialized += (s, e) =>
        {
            v1c++;

            // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
            // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
            // in no way should the v1AddedToWin.Frame be the same as the Top.Frame/Viewport
            // as it is a subview of winAddedToTop, which has a border!
            //Assert.Equal (top.Viewport.Width,  v1AddedToWin.Frame.Width);
            //Assert.Equal (top.Viewport.Height, v1AddedToWin.Frame.Height);
        };

        v2AddedToWin.Initialized += (s, e) =>
        {
            v2c++;

            // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
            // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
            // in no way should the v2AddedToWin.Frame be the same as the Top.Frame/Viewport
            // as it is a subview of winAddedToTop, which has a border!
            //Assert.Equal (top.Viewport.Width,  v2AddedToWin.Frame.Width);
            //Assert.Equal (top.Viewport.Height, v2AddedToWin.Frame.Height);
        };

        svAddedTov1.Initialized += (s, e) =>
        {
            sv1c++;

            // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
            // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
            // in no way should the svAddedTov1.Frame be the same as the Top.Frame/Viewport
            // because sv1AddedTov1 is a subview of v1AddedToWin, which is a subview of
            // winAddedToTop, which has a border!
            //Assert.Equal (top.Viewport.Width,  svAddedTov1.Frame.Width);
            //Assert.Equal (top.Viewport.Height, svAddedTov1.Frame.Height);
            Assert.False (svAddedTov1.CanFocus);
            //Assert.Throws<InvalidOperationException> (() => svAddedTov1.CanFocus = true);
            Assert.False (svAddedTov1.CanFocus);
        };

        v1AddedToWin.Add (svAddedTov1);
        winAddedToTop.Add (v1AddedToWin, v2AddedToWin);
        top.Add (winAddedToTop);

        top.BeginInit ();
        top.EndInit ();

        Assert.Equal (1, tc);
        Assert.Equal (1, wc);
        Assert.Equal (1, v1c);
        Assert.Equal (1, v2c);
        Assert.Equal (1, sv1c);

        Assert.True (top.CanFocus);
        Assert.True (winAddedToTop.CanFocus);
        Assert.False (v1AddedToWin.CanFocus);
        Assert.False (v2AddedToWin.CanFocus);
        Assert.False (svAddedTov1.CanFocus);

        v1AddedToWin.CanFocus = true;
        Assert.False (svAddedTov1.CanFocus); // False because sv1 was disposed and it isn't a subview of v1.
    }

    [Fact]
    public void SuperViewChanged_Raised_On_SubViewAdded_SubViewRemoved ()
    {
        var isAdded = false;

        View superView = new () { Id = "superView" };
        View subView = new () { Id = "subView" };

        superView.SubViewAdded += (s, e) =>
                                  {
                                      Assert.True (isAdded);
                                      Assert.Equal (superView, subView.SuperView);
                                      Assert.Equal (subView, e.SubView);
                                      Assert.Equal (superView, e.SuperView);
                                  };

        superView.SubViewRemoved += (s, e) =>
                                    {
                                        Assert.False (isAdded);
                                        Assert.NotEqual (superView, subView.SuperView);
                                        Assert.Equal (subView, e.SubView);
                                        Assert.Equal (superView, e.SuperView);
                                    };

        subView.SuperViewChanged += (s, e) => { isAdded = subView.SuperView == superView; };

        superView.Add (subView);
        Assert.True (isAdded);
        Assert.Equal (superView, subView.SuperView);
        Assert.Single (superView.SubViews);

        superView.Remove (subView);
        Assert.False (isAdded);
        Assert.NotEqual (superView, subView.SuperView);
        Assert.Empty (superView.SubViews);
    }

    [Fact]
    public void RemoveAll_Removes_All_SubViews ()
    {
        // Arrange
        var superView = new View ();
        var subView1 = new View ();
        var subView2 = new View ();
        var subView3 = new View ();

        superView.Add (subView1, subView2, subView3);

        // Act
        var removedViews = superView.RemoveAll ();

        // Assert
        Assert.Empty (superView.SubViews);
        Assert.Equal (3, removedViews.Count);
        Assert.Contains (subView1, removedViews);
        Assert.Contains (subView2, removedViews);
        Assert.Contains (subView3, removedViews);
    }

    [Fact]
    public void RemoveAllTView_Removes_All_SubViews_Of_Specific_Type ()
    {
        // Arrange
        var superView = new View ();
        var subView1 = new View ();
        var subView2 = new View ();
        var subView3 = new View ();
        var subView4 = new Button ();

        superView.Add (subView1, subView2, subView3, subView4);

        // Act
        var removedViews = superView.RemoveAll<Button> ();

        // Assert
        Assert.Equal (3, superView.SubViews.Count);
        Assert.DoesNotContain (subView4, superView.SubViews);
        Assert.Single (removedViews);
        Assert.Contains (subView4, removedViews);
    }

    [Fact]
    public void RemoveAllTView_Does_Not_Remove_Other_Types ()
    {
        // Arrange
        var superView = new View ();
        var subView1 = new View ();
        var subView2 = new Button ();
        var subView3 = new Label ();

        superView.Add (subView1, subView2, subView3);

        // Act
        var removedViews = superView.RemoveAll<Button> ();

        // Assert
        Assert.Equal (2, superView.SubViews.Count);
        Assert.Contains (subView1, superView.SubViews);
        Assert.Contains (subView3, superView.SubViews);
        Assert.Single (removedViews);
        Assert.Contains (subView2, removedViews);
    }
}
