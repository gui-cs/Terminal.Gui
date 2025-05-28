using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.TextTests;

public class AppendAutocompleteTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_AfterCloseKey_NoAutocomplete ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.Driver?.SendKeys ('f', ConsoleKey.F, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // When cancelling autocomplete
        Application.Driver?.SendKeys ('e', ConsoleKey.Escape, false, false, false);

        // Suggestion should disappear
        tf.Draw ();
        View.SetClipToScreen ();
        DriverAssert.AssertDriverContentsAre ("f", output);
        Assert.Equal ("f", tf.Text);

        // Still has focus though
        Assert.Same (tf, Application.Top.Focused);

        // But can tab away
        Application.Driver?.SendKeys ('\t', ConsoleKey.Tab, false, false, false);
        Assert.NotSame (tf, Application.Top.Focused);
        Application.Top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_AfterCloseKey_ReappearsOnLetter ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.Driver?.SendKeys ('f', ConsoleKey.F, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // When cancelling autocomplete
        Application.Driver?.SendKeys ('\0', ConsoleKey.Escape, false, false, false);

        // Suggestion should disappear
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("f", output);
        Assert.Equal ("f", tf.Text);

        // Should reappear when you press next letter
        Application.Driver?.SendKeys ('i', ConsoleKey.I, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("fi", tf.Text);
        Application.Top.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (ConsoleKey.UpArrow)]
    [InlineData (ConsoleKey.DownArrow)]
    public void TestAutoAppend_CycleSelections (ConsoleKey cycleKey)
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish", "friend");

        // f is typed and suggestion is "fish"
        Application.Driver?.SendKeys ('f', ConsoleKey.F, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // When cycling autocomplete
        Application.Driver?.SendKeys (' ', cycleKey, false, false, false);

        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("friend", output);
        Assert.Equal ("f", tf.Text);

        // Should be able to cycle in circles endlessly
        Application.Driver?.SendKeys (' ', cycleKey, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);
        Application.Top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_NoRender_WhenCursorNotAtEnd ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.Driver?.SendKeys ('f', ConsoleKey.F, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // add a space then go back 1
        Application.Driver?.SendKeys (' ', ConsoleKey.Spacebar, false, false, false);
        Application.Driver?.SendKeys ('<', ConsoleKey.LeftArrow, false, false, false);

        View.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("f", output);
        Assert.Equal ("f ", tf.Text);
        Application.Top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_NoRender_WhenNoMatch ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.Driver?.SendKeys ('f', ConsoleKey.F, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // x is typed and suggestion should disappear
        Application.Driver?.SendKeys ('x', ConsoleKey.X, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("fx", output);
        Assert.Equal ("fx", tf.Text);
        Application.Top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_ShowThenAccept_CasesDiffer ()
    {
        TextField tf = GetTextFieldsInView ();

        tf.Autocomplete = new AppendAutocomplete (tf);
        var generator = (SingleWordSuggestionGenerator)tf.Autocomplete.SuggestionGenerator;
        generator.AllSuggestions = new() { "FISH" };

        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("", output);
        tf.NewKeyDownEvent (Key.M);
        tf.NewKeyDownEvent (Key.Y);
        tf.NewKeyDownEvent (Key.Space);
        tf.NewKeyDownEvent (Key.F);
        Assert.Equal ("my f", tf.Text);

        // Even though there is no match on case we should still get the suggestion
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("my fISH", output);
        Assert.Equal ("my f", tf.Text);

        // When tab completing the case of the whole suggestion should be applied
        Application.Driver?.SendKeys ('\t', ConsoleKey.Tab, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("my FISH", output);
        Assert.Equal ("my FISH", tf.Text);
        Application.Top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_ShowThenAccept_MatchCase ()
    {
        TextField tf = GetTextFieldsInView ();

        tf.Autocomplete = new AppendAutocomplete (tf);
        var generator = (SingleWordSuggestionGenerator)tf.Autocomplete.SuggestionGenerator;
        generator.AllSuggestions = new() { "fish" };

        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("", output);

        tf.NewKeyDownEvent (new ('f'));

        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        Application.Driver?.SendKeys ('\t', ConsoleKey.Tab, false, false, false);

        View.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("fish", tf.Text);

        // Tab should autcomplete but not move focus
        Assert.Same (tf, Application.Top.Focused);

        // Second tab should move focus (nothing to autocomplete)
        Application.Driver?.SendKeys ('\t', ConsoleKey.Tab, false, false, false);
        Assert.NotSame (tf, Application.Top.Focused);
        Application.Top.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData ("ffffffffffffffffffffffffff", "ffffffffff")]
    [InlineData ("f234567890", "f234567890")]
    [InlineData ("fisérables", "fisérables")]
    public void TestAutoAppendRendering_ShouldNotOverspill (string overspillUsing, string expectRender)
    {
        TextField tf = GetTextFieldsInViewSuggesting (overspillUsing);

        // f is typed we should only see 'f' up to size of View (10)
        Application.Driver?.SendKeys ('f', ConsoleKey.F, false, false, false);
        View.SetClipToScreen ();
        tf.Draw ();
        View.SetClipToScreen ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre (expectRender, output);
        Assert.Equal ("f", tf.Text);
        Application.Top.Dispose ();
    }

    private TextField GetTextFieldsInView ()
    {
        var tf = new TextField { Width = 10 };
        var tf2 = new TextField { Y = 1, Width = 10 };

        Toplevel top = new ();
        top.Add (tf);
        top.Add (tf2);

        Application.Begin (top);

        Assert.Same (tf, top.Focused);

        return tf;
    }

    private TextField GetTextFieldsInViewSuggesting (params string [] suggestions)
    {
        TextField tf = GetTextFieldsInView ();

        tf.Autocomplete = new AppendAutocomplete (tf);
        var generator = (SingleWordSuggestionGenerator)tf.Autocomplete.SuggestionGenerator;
        generator.AllSuggestions = suggestions.ToList ();

        tf.Draw ();
        tf.PositionCursor ();
        DriverAssert.AssertDriverContentsAre ("", output);

        return tf;
    }
}
