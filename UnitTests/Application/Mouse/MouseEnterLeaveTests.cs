namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class ApplicationMouseEnterLeaveTests
{
    private class TestView : View
    {
        public TestView ()
        {
            X = 1;
            Y = 1;
            Width = 1;
            Height = 1;
        }

        public bool HandleOnEnter { get; }
        public bool HandleOnLeave { get; }

        public int OnMouseEnterCalled { get; private set; }
        public int OnMouseLeaveCalled { get; private set; }

        protected internal override bool? OnMouseEnter (MouseEvent mouseEvent)
        {
            OnMouseEnterCalled++;
            mouseEvent.Handled = HandleOnEnter;

            base.OnMouseEnter (mouseEvent);

            return mouseEvent.Handled;
        }

        protected internal override bool OnMouseLeave (MouseEvent mouseEvent)
        {
            OnMouseLeaveCalled++;
            mouseEvent.Handled = HandleOnLeave;

            base.OnMouseLeave (mouseEvent);

            return mouseEvent.Handled;
        }
    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseEntersView_CallsOnMouseEnter ()
    {
        // Arrange
        Application.Top = new () { Frame = new (0, 0, 10, 10) };
        var view = new TestView ();
        Application.Top.Add (view);
        var mousePosition = new Point (1, 1);
        List<View> currentViewsUnderMouse = new () { view };
        var mouseEvent = new MouseEvent ()
        {
            Position = mousePosition,
            ScreenPosition = mousePosition
        };

        Application._cachedViewsUnderMouse.Clear ();

        try
        {
            // Act
            Application.RaiseMouseEnterLeaveEvents (mousePosition, currentViewsUnderMouse, mouseEvent);

            // Assert
            Assert.Equal (1, view.OnMouseEnterCalled);
        }
        finally
        {
            // Cleanup
            Application.Top?.Dispose ();
            Application.ResetState ();
        }
    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseLeavesView_CallsOnMouseLeave ()
    {
        // Arrange
        Application.Top = new () { Frame = new (0, 0, 10, 10) };
        var view = new TestView ();
        Application.Top.Add (view);
        var mousePosition = new Point (0, 0);
        List<View> currentViewsUnderMouse = new ();
        var mouseEvent = new MouseEvent ();

        Application._cachedViewsUnderMouse.Clear ();
        Application._cachedViewsUnderMouse.Add (view);

        try
        {
            // Act
            Application.RaiseMouseEnterLeaveEvents (mousePosition, currentViewsUnderMouse, mouseEvent);

            // Assert
            Assert.Equal (0, view.OnMouseEnterCalled);
            Assert.Equal (1, view.OnMouseLeaveCalled);
        }
        finally
        {
            // Cleanup
            Application.Top?.Dispose ();
            Application.ResetState ();
        }
    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseMovesBetweenAdjacentViews_CallsOnMouseEnterAndLeave ()
    {
        // Arrange
        Application.Top = new () { Frame = new (0, 0, 10, 10) };
        var view1 = new TestView (); // at 1,1 to 2,2
        var view2 = new TestView () // at 2,2 to 3,3
        {
            X = 2,
            Y = 2,
        };
        Application.Top.Add (view1);
        Application.Top.Add (view2);



        Application._cachedViewsUnderMouse.Clear ();

        try
        {
            // Act
            var mousePosition = new Point (0, 0);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (0, view1.OnMouseEnterCalled);
            Assert.Equal (0, view1.OnMouseLeaveCalled);
            Assert.Equal (0, view2.OnMouseEnterCalled);
            Assert.Equal (0, view2.OnMouseLeaveCalled);

            // Act
            mousePosition = new Point (1, 1);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (0, view1.OnMouseLeaveCalled);
            Assert.Equal (0, view2.OnMouseEnterCalled);
            Assert.Equal (0, view2.OnMouseLeaveCalled);

            // Act
            mousePosition = new Point (2, 2);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (1, view1.OnMouseLeaveCalled);
            Assert.Equal (1, view2.OnMouseEnterCalled);
            Assert.Equal (0, view2.OnMouseLeaveCalled);


            // Act
            mousePosition = new Point (3, 3);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (1, view1.OnMouseLeaveCalled);
            Assert.Equal (1, view2.OnMouseEnterCalled);
            Assert.Equal (1, view2.OnMouseLeaveCalled);

            // Act
            mousePosition = new Point (0, 0);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (1, view1.OnMouseLeaveCalled);
            Assert.Equal (1, view2.OnMouseEnterCalled);
            Assert.Equal (1, view2.OnMouseLeaveCalled);
        }
        finally
        {
            // Cleanup
            Application.Top?.Dispose ();
            Application.ResetState ();
        }
    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_NoViewsUnderMouse_DoesNotCallOnMouseEnterOrLeave ()
    {
        // Arrange
        Application.Top = new () { Frame = new (0, 0, 10, 10) };
        var view = new TestView ();
        Application.Top.Add (view);
        var mousePosition = new Point (0, 0);
        List<View> currentViewsUnderMouse = new ();
        var mouseEvent = new MouseEvent ();

        Application._cachedViewsUnderMouse.Clear ();

        try
        {
            // Act
            Application.RaiseMouseEnterLeaveEvents (mousePosition, currentViewsUnderMouse, mouseEvent);

            // Assert
            Assert.Equal (0, view.OnMouseEnterCalled);
            Assert.Equal (0, view.OnMouseLeaveCalled);
        }
        finally
        {
            // Cleanup
            Application.Top?.Dispose ();
            Application.ResetState ();
        }
    }


    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseMovesBetweenOverlappingViews_CallsOnMouseEnterAndLeave ()
    {
        // Arrange
        Application.Top = new () { Frame = new (0, 0, 10, 10) };
        var view1 = new TestView ()
        {
            Width = 2
        }; // at 1,1 to 3,2
        var view2 = new TestView () // at 2,2 to 4,3
        {
            Width = 2,
            X = 2,
            Y = 2,
        };
        Application.Top.Add (view1);
        Application.Top.Add (view2);

        Application._cachedViewsUnderMouse.Clear ();

        try
        {
            // Act
            var mousePosition = new Point (0, 0);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (0, view1.OnMouseEnterCalled);
            Assert.Equal (0, view1.OnMouseLeaveCalled);
            Assert.Equal (0, view2.OnMouseEnterCalled);
            Assert.Equal (0, view2.OnMouseLeaveCalled);

            // Act
            mousePosition = new Point (1, 1);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (0, view1.OnMouseLeaveCalled);
            Assert.Equal (0, view2.OnMouseEnterCalled);
            Assert.Equal (0, view2.OnMouseLeaveCalled);

            // Act
            mousePosition = new Point (2, 2);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (1, view1.OnMouseLeaveCalled);
            Assert.Equal (1, view2.OnMouseEnterCalled);
            Assert.Equal (0, view2.OnMouseLeaveCalled);


            // Act
            mousePosition = new Point (3, 3);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (1, view1.OnMouseLeaveCalled);
            Assert.Equal (1, view2.OnMouseEnterCalled);
            Assert.Equal (1, view2.OnMouseLeaveCalled);

            // Act
            mousePosition = new Point (0, 0);
            Application.RaiseMouseEnterLeaveEvents (mousePosition, View.GetViewsUnderMouse (mousePosition), new MouseEvent ()
            {
                Position = mousePosition,
                ScreenPosition = mousePosition
            });

            // Assert
            Assert.Equal (1, view1.OnMouseEnterCalled);
            Assert.Equal (1, view1.OnMouseLeaveCalled);
            Assert.Equal (1, view2.OnMouseEnterCalled);
            Assert.Equal (1, view2.OnMouseLeaveCalled);
        }
        finally
        {
            // Cleanup
            Application.Top?.Dispose ();
            Application.ResetState ();
        }
    }
}
