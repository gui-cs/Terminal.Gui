namespace Terminal.Gui.ViewsTests;

/// <summary>
/// Pure unit tests for <see cref="Label"/> that don't require Application.Driver or Application context.
/// These tests can run in parallel without interference.
/// </summary>
public class LabelTests : UnitTests.Parallelizable.ParallelizableBase
{
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var label = new Label ();
        label.Title = "Hello";
        Assert.Equal ("Hello", label.Title);
        Assert.Equal ("Hello", label.TitleTextFormatter.Text);

        Assert.Equal ("Hello", label.Text);
        Assert.Equal ("Hello", label.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var label = new Label ();
        label.Text = "Hello";
        Assert.Equal ("Hello", label.Text);
        Assert.Equal ("Hello", label.TextFormatter.Text);

        Assert.Equal ("Hello", label.Title);
        Assert.Equal ("Hello", label.TitleTextFormatter.Text);
    }

    [Theory]
    [CombinatorialData]
    public void HotKey_Command_SetsFocus_OnNextSubView (bool hasHotKey)
    {
        var superView = new View { CanFocus = true };
        var label = new Label ();
        label.HotKey = hasHotKey ? Key.A.WithAlt : Key.Empty;
        var nextSubView = new View { CanFocus = true };
        superView.Add (label, nextSubView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (label.HasFocus);
        Assert.False (nextSubView.HasFocus);

        label.InvokeCommand (Command.HotKey);
        Assert.False (label.HasFocus);
        Assert.Equal (hasHotKey, nextSubView.HasFocus);
    }

    [Theory]
    [CombinatorialData]
    public void MouseClick_SetsFocus_OnNextSubView (bool hasHotKey)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var label = new Label { X = 2 };
        label.HotKey = hasHotKey ? Key.X.WithAlt : Key.Empty;

        var nextSubView = new View { CanFocus = true, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, label, nextSubView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (focusedView.HasFocus);
        Assert.False (label.HasFocus);
        Assert.False (nextSubView.HasFocus);

        label.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked });
        Assert.False (label.HasFocus);
        Assert.Equal (hasHotKey, nextSubView.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var label = new Label ();
        var accepted = false;

        label.Accepting += LabelOnAccept;
        label.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void LabelOnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var label = new Label ();
        Assert.Equal (string.Empty, label.Text);
        Assert.Equal (Alignment.Start, label.TextAlignment);
        Assert.False (label.CanFocus);
        Assert.Equal (new (0, 0, 0, 0), label.Frame);
        Assert.Equal (KeyCode.Null, label.HotKey);
    }

    [Fact]
    public void Label_HotKeyChanged_EventFires ()
    {
        var label = new Label ();
        var fired = false;
        Key oldKey = Key.Empty;
        Key newKey = Key.Empty;

        label.HotKeyChanged += (s, e) =>
        {
            fired = true;
            oldKey = e.OldKey;
            newKey = e.NewKey;
        };

        label.HotKey = Key.A.WithAlt;

        Assert.True (fired);
        Assert.Equal (Key.Empty, oldKey);
        Assert.Equal (Key.A.WithAlt, newKey);
    }

    [Fact]
    public void Label_HotKeyChanged_EventFires_WithNone ()
    {
        var label = new Label { HotKey = Key.A.WithAlt };
        var fired = false;
        Key oldKey = Key.Empty;
        Key newKey = Key.Empty;

        label.HotKeyChanged += (s, e) =>
        {
            fired = true;
            oldKey = e.OldKey;
            newKey = e.NewKey;
        };

        label.HotKey = Key.Empty;

        Assert.True (fired);
        Assert.Equal (Key.A.WithAlt, oldKey);
        Assert.Equal (Key.Empty, newKey);
    }

    [Fact]
    public void TestAssignTextToLabel ()
    {
        var label = new Label ();
        label.Text = "Test";
        Assert.Equal ("Test", label.Text);
    }

}
