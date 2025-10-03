#nullable enable
using UnitTests.Parallelizable;

namespace Terminal.Gui.ViewLayoutEventTests;

public class ViewLayoutEventTests : GlobalTestSetup
{
    [Fact]
    public void View_WidthChanging_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.WidthChanging += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.CurrentValue;
            newValue = args.NewValue;
        };

        view.Width = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_WidthChanged_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.WidthChanged += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.OldValue;
            newValue = args.NewValue;
        };

        view.Width = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_WidthChanging_CanCancel ()
    {
        var view = new View ();
        Dim? originalWidth = view.Width;

        view.WidthChanging += (sender, args) =>
        {
            args.Handled = true; // Cancel the change
        };

        view.Width = 10;

        // Width should not have changed
        Assert.Equal (originalWidth, view.Width);
    }

    [Fact]
    public void View_WidthChanging_CanModify ()
    {
        var view = new View ();

        view.WidthChanging += (sender, args) =>
        {
            // Modify the proposed value
            args.NewValue = 20;
        };

        view.Width = 10;

        // Width should be 20 (the modified value), not 10
        var container = new View { Width = 50, Height = 20 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (20, view.Frame.Width);
    }

    [Fact]
    public void View_HeightChanging_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.HeightChanging += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.CurrentValue;
            newValue = args.NewValue;
        };

        view.Height = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_HeightChanged_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.HeightChanged += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.OldValue;
            newValue = args.NewValue;
        };

        view.Height = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_HeightChanging_CanCancel ()
    {
        var view = new View ();
        Dim? originalHeight = view.Height;

        view.HeightChanging += (sender, args) =>
        {
            args.Handled = true; // Cancel the change
        };

        view.Height = 10;

        // Height should not have changed
        Assert.Equal (originalHeight, view.Height);
    }

    [Fact]
    public void View_HeightChanging_CanModify ()
    {
        var view = new View ();

        view.HeightChanging += (sender, args) =>
        {
            // Modify the proposed value
            args.NewValue = 20;
        };

        view.Height = 10;

        // Height should be 20 (the modified value), not 10
        var container = new View { Width = 50, Height = 40 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (20, view.Frame.Height);
    }

    [Fact]
    public void View_OnWidthChanging_CanCancel ()
    {
        var testView = new TestView ();
        testView.CancelWidthChange = true;
        Dim? originalWidth = testView.Width;

        testView.Width = 10;

        // Width should not have changed
        Assert.Equal (originalWidth, testView.Width);
    }

    [Fact]
    public void View_OnHeightChanging_CanCancel ()
    {
        var testView = new TestView ();
        testView.CancelHeightChange = true;
        Dim originalHeight = testView.Height;

        testView.Height = 10;

        // Height should not have changed
        Assert.Equal (originalHeight, testView.Height);
    }

    [Fact]
    public void View_WidthChanged_BackingFieldSetBeforeEvent ()
    {
        var view = new View ();
        Dim? widthInChangedEvent = null;

        view.WidthChanged += (sender, args) =>
        {
            // The backing field should already be set when Changed event fires
            widthInChangedEvent = view.Width;
        };

        view.Width = 25;

        // The width seen in the Changed event should be the new value
        var container = new View { Width = 50, Height = 20 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (25, view.Frame.Width);
    }

    [Fact]
    public void View_HeightChanged_BackingFieldSetBeforeEvent ()
    {
        var view = new View ();
        Dim? heightInChangedEvent = null;

        view.HeightChanged += (sender, args) =>
        {
            // The backing field should already be set when Changed event fires
            heightInChangedEvent = view.Height;
        };

        view.Height = 30;

        // The height seen in the Changed event should be the new value
        var container = new View { Width = 50, Height = 40 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (30, view.Frame.Height);
    }

    private class TestView : View
    {
        public bool CancelWidthChange { get; set; }
        public bool CancelHeightChange { get; set; }

        protected override bool OnWidthChanging (App.ValueChangingEventArgs<Dim> args)
        {
            return CancelWidthChange;
        }

        protected override bool OnHeightChanging (App.ValueChangingEventArgs<Dim> args)
        {
            return CancelHeightChange;
        }
    }
}
