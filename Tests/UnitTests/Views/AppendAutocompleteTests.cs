using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.TextTests;

public class AppendAutocompleteTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_AfterCloseKey_NoAutocomplete ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.RaiseKeyDownEvent ('f');
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // When cancelling autocomplete
        Application.RaiseKeyDownEvent (Key.Esc);

        // Suggestion should disappear
        tf.Draw ();
        tf.SetClipToScreen ();
        DriverAssert.AssertDriverContentsAre ("f", output);
        Assert.Equal ("f", tf.Text);

        // Still has focus though
        Assert.Same (tf, Application.TopRunnableView.Focused);

        // But can tab away
        Application.RaiseKeyDownEvent ('\t');
        Assert.NotSame (tf, Application.TopRunnableView.Focused);
        Application.TopRunnableView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_AfterCloseKey_ReappearsOnLetter ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.RaiseKeyDownEvent ('f');
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // When cancelling autocomplete
        Application.RaiseKeyDownEvent (Key.Esc);

        // Suggestion should disappear
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("f", output);
        Assert.Equal ("f", tf.Text);

        // Should reappear when you press next letter
        Application.RaiseKeyDownEvent (Key.I);
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("fi", tf.Text);
        Application.TopRunnableView.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.CursorDown)]
    public void TestAutoAppend_CycleSelections (KeyCode cycleKey)
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish", "friend");

        // f is typed and suggestion is "fish"
        Application.RaiseKeyDownEvent ('f');
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // When cycling autocomplete
        Application.RaiseKeyDownEvent (cycleKey);

        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("friend", output);
        Assert.Equal ("f", tf.Text);

        // Should be able to cycle in circles endlessly
        Application.RaiseKeyDownEvent (cycleKey);
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);
        Application.TopRunnableView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_NoRender_WhenCursorNotAtEnd ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.RaiseKeyDownEvent ('f');
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // add a space then go back 1
        Application.RaiseKeyDownEvent (' ');
        Application.RaiseKeyDownEvent (Key.CursorLeft);

        tf.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("f", output);
        Assert.Equal ("f ", tf.Text);
        Application.TopRunnableView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_NoRender_WhenNoMatch ()
    {
        TextField tf = GetTextFieldsInViewSuggesting ("fish");

        // f is typed and suggestion is "fish"
        Application.RaiseKeyDownEvent ('f');
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        // x is typed and suggestion should disappear
        Application.RaiseKeyDownEvent (Key.X);
        tf.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("fx", output);
        Assert.Equal ("fx", tf.Text);
        Application.TopRunnableView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_ShowThenAccept_CasesDiffer ()
    {
        TextField tf = GetTextFieldsInView ();

        tf.Autocomplete = new AppendAutocomplete (tf);
        var generator = (SingleWordSuggestionGenerator)tf.Autocomplete.SuggestionGenerator;
        generator.AllSuggestions = new() { "FISH" };

        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("", output);
        tf.NewKeyDownEvent (Key.M);
        tf.NewKeyDownEvent (Key.Y);
        tf.NewKeyDownEvent (Key.Space);
        tf.NewKeyDownEvent (Key.F);
        Assert.Equal ("my f", tf.Text);

        // Even though there is no match on case we should still get the suggestion
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("my fISH", output);
        Assert.Equal ("my f", tf.Text);

        // When tab completing the case of the whole suggestion should be applied
        Application.RaiseKeyDownEvent ('\t');
        tf.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("my FISH", output);
        Assert.Equal ("my FISH", tf.Text);
        Application.TopRunnableView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestAutoAppend_ShowThenAccept_MatchCase ()
    {
        TextField tf = GetTextFieldsInView ();

        tf.Autocomplete = new AppendAutocomplete (tf);
        var generator = (SingleWordSuggestionGenerator)tf.Autocomplete.SuggestionGenerator;
        generator.AllSuggestions = new() { "fish" };

        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("", output);

        tf.NewKeyDownEvent (new ('f'));

        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("f", tf.Text);

        Application.RaiseKeyDownEvent ('\t');

        tf.SetClipToScreen ();
        tf.Draw ();
        DriverAssert.AssertDriverContentsAre ("fish", output);
        Assert.Equal ("fish", tf.Text);

        // Tab should autcomplete but not move focus
        Assert.Same (tf, Application.TopRunnableView.Focused);

        // Second tab should move focus (nothing to autocomplete)
        Application.RaiseKeyDownEvent ('\t');
        Assert.NotSame (tf, Application.TopRunnableView.Focused);
        Application.TopRunnableView.Dispose ();
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
        Application.RaiseKeyDownEvent ('f');
        tf.SetClipToScreen ();
        tf.Draw ();
        tf.SetClipToScreen ();

        DriverAssert.AssertDriverContentsAre (expectRender, output);
        Assert.Equal ("f", tf.Text);
        Application.TopRunnableView.Dispose ();
    }

    private TextField GetTextFieldsInView ()
    {
        var tf = new TextField { Width = 10 };
        var tf2 = new TextField { Y = 1, Width = 10 };

        Runnable top = new ();
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

        DriverAssert.AssertDriverContentsAre ("", output);

        return tf;
    }
}
