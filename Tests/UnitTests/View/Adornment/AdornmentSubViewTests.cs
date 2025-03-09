using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdornmentSubViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (0, 0, false)] // Margin has no thickness, so false
    [InlineData (0, 1, false)] // Margin has no thickness, so false
    [InlineData (1, 0, true)]
    [InlineData (1, 1, true)]
    [InlineData (2, 1, true)]
    public void Adornment_WithSubView_GetViewsUnderMouse_Finds (int viewMargin, int subViewMargin, bool expectedFound)
    {
        Application.Top = new Toplevel()
        {
            Width = 10,
            Height = 10
        };
        Application.Top.Margin.Thickness = new Thickness (viewMargin);

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };
        subView.Margin.Thickness = new Thickness (subViewMargin);
        Application.Top.Margin.Add (subView);
        Application.Top.Layout ();

        var foundView = View.GetViewsUnderMouse (new Point(0, 0)).LastOrDefault ();

        bool found = foundView == subView || foundView == subView.Margin;
        Assert.Equal (expectedFound, found);
        Application.Top.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }

    [Fact]
    public void Adornment_WithNonVisibleSubView_GetViewsUnderMouse_Finds_Adornment ()
    {
        Application.Top = new Toplevel ()
        {
            Width = 10,
            Height = 10
        };
        Application.Top.Padding.Thickness = new Thickness (1);

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Visible = false
        };
        Application.Top.Padding.Add (subView);
        Application.Top.Layout ();

        Assert.Equal (Application.Top.Padding, View.GetViewsUnderMouse (new Point(0, 0)).LastOrDefault ());
        Application.Top?.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }
}
