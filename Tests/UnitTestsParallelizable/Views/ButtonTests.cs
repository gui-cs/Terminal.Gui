namespace Terminal.Gui.ViewsTests;

/// <summary>
/// Pure unit tests for <see cref="Button"/> that don't require Application static dependencies.
/// These tests can run in parallel without interference.
/// </summary>
public class ButtonTests : UnitTests.Parallelizable.ParallelizableBase
{
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new Button ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.LeftBracket} Hello {Glyphs.RightBracket}", view.TextFormatter.Text);
        view.Dispose ();
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new Button ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.LeftBracket} Hello {Glyphs.RightBracket}", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);
        view.Dispose ();
    }

    [Theory]
    [InlineData ("01234", 0, 0, 0, 0)]
    [InlineData ("01234", 1, 0, 1, 0)]
    [InlineData ("01234", 0, 1, 0, 1)]
    [InlineData ("01234", 1, 1, 1, 1)]
    [InlineData ("01234", 10, 1, 10, 1)]
    [InlineData ("01234", 10, 3, 10, 3)]
    [InlineData ("0_1234", 0, 0, 0, 0)]
    [InlineData ("0_1234", 1, 0, 1, 0)]
    [InlineData ("0_1234", 0, 1, 0, 1)]
    [InlineData ("0_1234", 1, 1, 1, 1)]
    [InlineData ("0_1234", 10, 1, 10, 1)]
    [InlineData ("0_12你", 10, 3, 10, 3)]
    [InlineData ("0_12你", 0, 0, 0, 0)]
    [InlineData ("0_12你", 1, 0, 1, 0)]
    [InlineData ("0_12你", 0, 1, 0, 1)]
    [InlineData ("0_12你", 1, 1, 1, 1)]
    [InlineData ("0_12你", 10, 1, 10, 1)]
    public void Button_AbsoluteSize_Text (string text, int width, int height, int expectedWidth, int expectedHeight)
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button
        {
            Text = text,
            Width = width,
            Height = height
        };

        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), btn1.GetContentSize ());
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (1, 0, 1, 0)]
    [InlineData (0, 1, 0, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (10, 1, 10, 1)]
    [InlineData (10, 3, 10, 3)]
    public void Button_AbsoluteSize_DefaultText (int width, int height, int expectedWidth, int expectedHeight)
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button ();
        btn1.Width = width;
        btn1.Height = height;

        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    [Fact]
    public void Button_HotKeyChanged_EventFires ()
    {
        var btn = new Button { Text = "_Yar" };

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }

    [Fact]
    public void Button_HotKeyChanged_EventFires_WithNone ()
    {
        var btn = new Button ();

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Null, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }
}
