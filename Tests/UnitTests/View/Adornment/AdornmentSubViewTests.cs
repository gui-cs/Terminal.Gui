using Xunit.Abstractions;

namespace UnitTests.ViewTests;

public class AdornmentSubViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (0, 0, false)] // Margin has no thickness, so false
    [InlineData (0, 1, false)] // Margin has no thickness, so false
    [InlineData (1, 0, true)]
    [InlineData (1, 1, true)]
    [InlineData (2, 1, true)]
    public void Adornment_WithSubView_Finds (int viewMargin, int subViewMargin, bool expectedFound)
    {
        Application.TopRunnableView = new Toplevel()
        {
            Width = 10,
            Height = 10
        };
        Application.TopRunnableView.Margin!.Thickness = new Thickness (viewMargin);
        // Turn of TransparentMouse for the test
        Application.TopRunnableView.Margin!.ViewportSettings = ViewportSettingsFlags.None;

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

        Application.TopRunnableView.Margin!.Add (subView);
        Application.TopRunnableView.Layout ();

        var foundView = Application.TopRunnableView.GetViewsUnderLocation (new Point(0, 0), ViewportSettingsFlags.None).LastOrDefault ();

        bool found = foundView == subView || foundView == subView.Margin;
        Assert.Equal (expectedFound, found);
        Application.TopRunnableView.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }

    [Fact]
    public void Adornment_WithNonVisibleSubView_Finds_Adornment ()
    {
        Application.TopRunnableView = new Toplevel ()
        {
            Width = 10,
            Height = 10
        };
        Application.TopRunnableView.Padding.Thickness = new Thickness (1);

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Visible = false
        };
        Application.TopRunnableView.Padding.Add (subView);
        Application.TopRunnableView.Layout ();

        Assert.Equal (Application.TopRunnableView.Padding, Application.TopRunnableView.GetViewsUnderLocation (new Point(0, 0), ViewportSettingsFlags.None).LastOrDefault ());
        Application.TopRunnableView?.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }
}
