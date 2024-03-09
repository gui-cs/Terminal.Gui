using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdornmentSubViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Adornment_WithSubView_FindDeepestView_Finds ()
    {
        var view = new View () {
            Width = 10,
            Height = 10

        };
        view.Padding.Thickness = new Thickness (1);

        var subView = new View () {
            X = 0,
            Y =0,
            Width = 1,
            Height = 1
        };
        view.Padding.Add (subView);

        Assert.Equal (subView, View.FindDeepestView (view, 0, 0));
    }

    [Fact]
    public void Adornment_WithNonVisibleSubView_FindDeepestView_Finds_Adornment ()
    {
        var view = new View ()
        {
            Width = 10,
            Height = 10

        };
        view.Padding.Thickness = new Thickness (1);

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Visible = false
        };
        view.Padding.Add (subView);

        Assert.Equal (view.Padding, View.FindDeepestView (view, 0, 0));
    }
}
