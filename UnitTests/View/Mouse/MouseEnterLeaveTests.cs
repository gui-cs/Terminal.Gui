namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class MouseEnterLeaveTests
{
    private class TestView : View
    {
        public TestView ()
        {
            MouseEnter += OnMouseEnterHandler;
            MouseLeave += OnMouseLeaveHandler;
        }

        public bool HandleOnEnter { get; init; }
        public bool HandleOnLeave { get; }

        public bool HandleEnterEvent { get; init; }
        public bool HandleLeaveEvent { get; }

        public bool OnMouseEnterCalled { get; private set; }
        public bool OnMouseLeaveCalled { get; private set; }

        protected internal override bool? OnMouseEnter (MouseEvent mouseEvent)
        {
            OnMouseEnterCalled = true;
            mouseEvent.Handled = HandleOnEnter;

            base.OnMouseEnter (mouseEvent);

            return mouseEvent.Handled;
        }

        protected internal override bool OnMouseLeave (MouseEvent mouseEvent)
        {
            OnMouseLeaveCalled = true;
            mouseEvent.Handled = HandleOnLeave;

            base.OnMouseLeave (mouseEvent);

            return mouseEvent.Handled;
        }

        public bool MouseEnterRaised { get; private set; }
        public bool MouseLeaveRaised { get; private set; }

        private void OnMouseEnterHandler (object s, MouseEventEventArgs e)
        {
            MouseEnterRaised = true;

            if (HandleEnterEvent)
            {
                e.Handled = true;
            }
        }

        private void OnMouseLeaveHandler (object s, MouseEventEventArgs e)
        {
            MouseLeaveRaised = true;

            if (HandleLeaveEvent)
            {
                e.Handled = true;
            }
        }
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsEnabledAndVisible_CallsOnMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.True (view.OnMouseEnterCalled);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsDisabled_DoesNotCallOnMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = false,
            Visible = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.False (view.OnMouseEnterCalled);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsNotVisible_DoesNotCallOnMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.False (view.OnMouseEnterCalled);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsVisible_CallsOnMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true, Visible = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseLeaveEvent (mouseEvent);

        // Assert
        Assert.True (view.OnMouseLeaveCalled);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsNotVisible_CallsOnMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseLeaveEvent (mouseEvent);

        // Assert
        Assert.True (view.OnMouseLeaveCalled);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    // Events

    [Fact]
    public void NewMouseEnterEvent_ViewIsEnabledAndVisible_RaisesMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.True (view.MouseEnterRaised);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsDisabled_DoesNotRaiseMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = false,
            Visible = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.False (view.MouseEnterRaised);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsNotVisible_DoesNotRaiseMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.False (view.MouseEnterRaised);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsVisible_RaisesMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true, 
            Visible = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseLeaveEvent (mouseEvent);

        // Assert
        Assert.True (view.MouseLeaveRaised);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsNotVisible_DoesNotRaiseMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseLeaveEvent (mouseEvent);

        // Assert
        Assert.True (view.MouseLeaveRaised);
        Assert.False (handled);
        Assert.False (mouseEvent.Handled);

        // Cleanup
        view.Dispose ();
    }

    // Handled tests
    [Fact]
    public void NewMouseEnterEvent_HandleOnMouseEnter_Event_Not_Raised ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true,
            HandleOnEnter = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.True (view.OnMouseEnterCalled);
        Assert.True (handled);
        Assert.True (mouseEvent.Handled);

        Assert.False (view.MouseEnterRaised);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_HandleMouseEnterEvent ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true,
            HandleEnterEvent = true
        };

        var mouseEvent = new MouseEvent ();

        // Act
        bool? handled = view.NewMouseEnterEvent (mouseEvent);

        // Assert
        Assert.True (view.OnMouseEnterCalled);
        Assert.True (handled);
        Assert.True (mouseEvent.Handled);

        Assert.True (view.MouseEnterRaised);

        // Cleanup
        view.Dispose ();
    }
}
