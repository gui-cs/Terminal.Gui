namespace ViewBaseTests.Adornments;

public class PaddingTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.True (view.Padding!.CanFocus);
        Assert.Equal (TabBehavior.NoStop, view.Padding.TabStop);
        Assert.Empty (view.Padding!.KeyBindings.GetBindings ());
    }

    [Fact]
    public void Thickness_Is_Empty_By_Default ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.Equal (Thickness.Empty, view.Padding!.Thickness);
    }
}
