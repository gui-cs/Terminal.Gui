using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class TitleTests (ITestOutputHelper output)
{
    [Fact]
    public void Set_Title_Fires_TitleChanged ()
    {
        var r = new View ();
        Assert.Equal (string.Empty, r.Title);

        string expectedOld = null;
        string expected = null;

        r.TitleChanged += (s, args) =>
                          {
                              Assert.Equal (expectedOld, args.OldValue);
                              Assert.Equal (r.Title, args.NewValue);
                          };

        expected = "title";
        expectedOld = r.Title;
        r.Title = expected;
        Assert.Equal (expected, r.Title);
        r.Dispose ();
    }

    [Fact]
    public void Set_Title_Fires_TitleChanging ()
    {
        var r = new View ();
        Assert.Equal (string.Empty, r.Title);

        string expectedOld = null;
        string expectedDuring = null;
        string expectedAfter = null;
        var cancel = false;

        r.TitleChanging += (s, args) =>
                           {
                               Assert.Equal (expectedOld, args.OldValue);
                               Assert.Equal (expectedDuring, args.NewValue);
                               args.Cancel = cancel;
                           };

        expectedOld = string.Empty;
        r.Title = expectedDuring = expectedAfter = "title";
        Assert.Equal (expectedAfter, r.Title);

        expectedOld = r.Title;
        r.Title = expectedDuring = expectedAfter = "a different title";
        Assert.Equal (expectedAfter, r.Title);

        // Now setup cancelling the change and change it back to "title"
        cancel = true;
        expectedOld = r.Title;
        r.Title = expectedDuring = "title";
        Assert.Equal (expectedAfter, r.Title);
        r.Dispose ();
    }

    // Setting Text does NOT set the HotKey
    [Fact]
    public void Title_Does_Set_HotKey ()
    {
        var view = new View { HotKeySpecifier = (Rune)'_', Title = "_Hello World" };

        Assert.Equal (Key.H, view.HotKey);
    }

    [SetupFakeDriver]
    [Fact]
    public void Change_View_Size_Update_Title_Size ()
    {
        var view = new View
        {
            Title = "_Hello World",
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Single
        };
        var top = new Toplevel ();
        top.Add (view);
        top.BeginInit ();
        top.EndInit ();

        Assert.Equal (string.Empty, view.Text);
        Assert.Equal (new (2, 2), view.Frame.Size);
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌┐
└┘",
                                                      output);

        var text = "This text will increment the view size and display the title.";
        view.Text = text;
        top.Draw ();
        Assert.Equal (text, view.Text);

        // SetupFakeDriver only create a screen with 25 cols and 25 rows
        Assert.Equal (new (text.Length, 1), view.GetContentSize ());

        top.Dispose ();
    }
}
