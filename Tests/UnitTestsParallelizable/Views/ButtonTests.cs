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

    [Fact]
    public void HotKeyChange_Works ()
    {
        var clicked = false;
        var btn = new Button { Text = "_Test" };
        btn.Accepting += (s, e) => clicked = true;

        Assert.Equal (KeyCode.T, btn.HotKey);
        Assert.False (btn.NewKeyDownEvent (Key.T)); // Button processes, but does not handle
        Assert.True (clicked);

        clicked = false;
        Assert.False (btn.NewKeyDownEvent (Key.T.WithAlt)); // Button processes, but does not handle
        Assert.True (clicked);

        clicked = false;
        btn.HotKey = KeyCode.E;
        Assert.False (btn.NewKeyDownEvent (Key.E.WithAlt)); // Button processes, but does not handle
        Assert.True (clicked);
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Space_Fires_Accept (bool focused, int expected)
    {
        var superView = new View
        {
            CanFocus = true
        };

        Button button = new ();

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (s, e) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Space);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Enter_Fires_Accept (bool focused, int expected)
    {
        var superView = new View
        {
            CanFocus = true
        };

        Button button = new ();

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (s, e) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 1)]
    public void HotKey_Fires_Accept (bool focused, int expected)
    {
        var superView = new View
        {
            CanFocus = true
        };

        Button button = new ()
        {
            HotKey = Key.A
        };

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (s, e) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.A);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Accepts ()
    {
        var btn = new Button { Text = "_Test" };
        var accepted = false;
        btn.Accepting += (s, e) => accepted = true;

        Assert.Equal (KeyCode.T, btn.HotKey);
        btn.InvokeCommand (Command.HotKey);
        Assert.True (accepted);
    }

    [Fact]
    public void Accept_Event_Returns_True ()
    {
        var btn = new Button { Text = "Test" };
        var acceptInvoked = false;
        btn.Accepting += (s, e) => { acceptInvoked = true; e.Handled = true; };

        Assert.True (btn.InvokeCommand (Command.Accept));
        Assert.True (acceptInvoked);
    }

    [Fact]
    public void Setting_Empty_Text_Sets_HoKey_To_KeyNull ()
    {
        var btn = new Button { Text = "_Test" };

        Assert.Equal (KeyCode.T, btn.HotKey);

        btn.Text = "";

        Assert.Equal (KeyCode.Null, btn.HotKey);
    }

    [Fact]
    public void TestAssignTextToButton ()
    {
        var btn = new Button { Text = "_K Ok" };

        Assert.Equal ("_K Ok", btn.Text);

        btn.Text = "_N Btn";

        Assert.Equal ("_N Btn", btn.Text);
    }

    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var button = new Button ();
        var acceptInvoked = false;

        button.Accepting += ButtonAccept;

        bool? ret = button.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        button.Dispose ();

        return;

        void ButtonAccept (object sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }
}
