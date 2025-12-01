using Xunit.Abstractions;

namespace ViewBaseTests.Adornments;

public class AdornmentSubViewTests ()
{
    [Fact]
    public void Setting_Thickness_Causes_Adornment_SubView_Layout ()
    {
        var view = new View ();
        var subView = new View ();
        view.Margin!.Add (subView);
        view.BeginInit ();
        view.EndInit ();
        var raised = false;

        subView.SubViewLayout += LayoutStarted;
        view.Margin.Thickness = new Thickness (1, 2, 3, 4);
        view.Layout ();
        Assert.True (raised);

        return;
        void LayoutStarted (object? sender, LayoutEventArgs e)
        {
            raised = true;
        }
    }

    [Theory]
    [InlineData (0, 0, false)] // Margin has no thickness, so false
    [InlineData (0, 1, false)] // Margin has no thickness, so false
    [InlineData (1, 0, true)]
    [InlineData (1, 1, true)]
    [InlineData (2, 1, true)]
    public void Adornment_WithSubView_Finds (int viewMargin, int subViewMargin, bool expectedFound)
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new ()
        {
            Width = 10,
            Height = 10
        };
        app.Begin (runnable);

        runnable.Margin!.Thickness = new Thickness (viewMargin);
        // Turn of TransparentMouse for the test
        runnable.Margin!.ViewportSettings = ViewportSettingsFlags.None;

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };
        subView.Margin!.Thickness = new Thickness (subViewMargin);
        // Turn of TransparentMouse for the test
        subView.Margin!.ViewportSettings = ViewportSettingsFlags.None;

        runnable.Margin!.Add (subView);
        runnable.Layout ();

        var foundView = runnable.GetViewsUnderLocation (new Point (0, 0), ViewportSettingsFlags.None).LastOrDefault ();

        bool found = foundView == subView || foundView == subView.Margin;
        Assert.Equal (expectedFound, found);
    }

    [Fact]
    public void Adornment_WithNonVisibleSubView_Finds_Adornment ()
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new ()
        {
            Width = 10,
            Height = 10
        };
        app.Begin (runnable);
        runnable.Padding!.Thickness = new Thickness (1);

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Visible = false
        };
        runnable.Padding.Add (subView);
        runnable.Layout ();

        Assert.Equal (runnable.Padding, runnable.GetViewsUnderLocation (new Point (0, 0), ViewportSettingsFlags.None).LastOrDefault ());

    }
}
