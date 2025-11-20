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
        Application.TopRunnable = new Toplevel()
        {
            Width = 10,
            Height = 10
        };
        Application.TopRunnable.Margin!.Thickness = new Thickness (viewMargin);
        // Turn of TransparentMouse for the test
        Application.TopRunnable.Margin!.ViewportSettings = ViewportSettingsFlags.None;

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

        Application.TopRunnable.Margin!.Add (subView);
        Application.TopRunnable.Layout ();

        var foundView = Application.TopRunnable.GetViewsUnderLocation (new Point(0, 0), ViewportSettingsFlags.None).LastOrDefault ();

        bool found = foundView == subView || foundView == subView.Margin;
        Assert.Equal (expectedFound, found);
        Application.TopRunnable.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }

    [Fact]
    public void Adornment_WithNonVisibleSubView_Finds_Adornment ()
    {
        Application.TopRunnable = new Toplevel ()
        {
            Width = 10,
            Height = 10
        };
        Application.TopRunnable.Padding.Thickness = new Thickness (1);

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Visible = false
        };
        Application.TopRunnable.Padding.Add (subView);
        Application.TopRunnable.Layout ();

        Assert.Equal (Application.TopRunnable.Padding, Application.TopRunnable.GetViewsUnderLocation (new Point(0, 0), ViewportSettingsFlags.None).LastOrDefault ());
        Application.TopRunnable?.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }
}
