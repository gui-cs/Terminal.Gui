using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class TitleTests
{
    // Unit tests that verify look & feel of title are in BorderTests.cs

    [Fact]
    public void Set_Title_Fires_TitleChanged ()
    {
        var r = new View ();
        Assert.Equal (string.Empty, r.Title);

        string expectedOld = null;
        string expected = null;

        r.TitleChanged += (s, args) =>
                          {
                              Assert.Equal (r.Title, args.Value);
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
                               Assert.Equal (expectedOld, args.CurrentValue);
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
}
