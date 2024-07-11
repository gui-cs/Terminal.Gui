#nullable enable
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait("Category","Output")]
public class NeedsDisplayTests ()
{
    [Fact]
    public void NeedsDisplay_False_If_Width_Height_Zero ()
    {
        View view = new () { Width = 0, Height = 0};
        view.BeginInit();
        view.EndInit();
        Assert.False (view.NeedsDisplay);
        //Assert.False (view.SubViewNeedsDisplay);
    }


    [Fact]
    public void NeedsDisplay_True_Initially_If_Width_Height_Not_Zero ()
    {
        View superView = new () { Width = 1, Height = 1};
        View view1 = new () { Width = 1, Height = 1 };
        View view2 = new () { Width = 1, Height = 1 };

        superView.Add(view1, view2);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.True (superView.NeedsDisplay);
        Assert.True (superView.SubViewNeedsDisplay);
        Assert.True (view1.NeedsDisplay);
        Assert.True (view2.NeedsDisplay);

        superView.Draw ();

        Assert.False (superView.NeedsDisplay);
        Assert.False (superView.SubViewNeedsDisplay);
        Assert.False (view1.NeedsDisplay);
        Assert.False (view2.NeedsDisplay);

        superView.SetNeedsDisplay();

        Assert.True (superView.NeedsDisplay);
        Assert.True (superView.SubViewNeedsDisplay);
        Assert.True (view1.NeedsDisplay);
        Assert.True (view2.NeedsDisplay);
    }
}
