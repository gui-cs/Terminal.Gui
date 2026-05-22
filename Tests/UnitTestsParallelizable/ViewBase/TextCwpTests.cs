// Copilot

using System.ComponentModel;

namespace ViewBaseTests;

/// <summary>
///     Tests for the CWP-compliant <see cref="View.Text"/> notifications:
///     <see cref="View.TextChanging"/> and <see cref="View.TextChanged"/>.
/// </summary>
public class TextCwpTests
{
    [Fact]
    public void Text_SetSameValue_NoEventsRaised ()
    {
        View view = new () { Text = "hello" };
        bool changingRaised = false;
        bool changedRaised = false;
        view.TextChanging += (_, _) => changingRaised = true;
        view.TextChanged += (_, _) => changedRaised = true;

        view.Text = "hello";

        Assert.False (changingRaised);
        Assert.False (changedRaised);
    }

    [Fact]
    public void Text_SetDifferentValue_BothEventsRaised ()
    {
        View view = new () { Text = "old" };
        bool changingRaised = false;
        bool changedRaised = false;
        view.TextChanging += (_, _) => changingRaised = true;
        view.TextChanged += (_, _) => changedRaised = true;

        view.Text = "new";

        Assert.True (changingRaised);
        Assert.True (changedRaised);
    }

    [Fact]
    public void TextChanging_Cancel_PreventsTextChange ()
    {
        View view = new () { Text = "original" };
        view.TextChanging += (_, e) => e.Cancel = true;

        view.Text = "modified";

        Assert.Equal ("original", view.Text);
    }

    [Fact]
    public void TextChanging_Cancel_SuppressesTextChanged ()
    {
        View view = new () { Text = "original" };
        bool changedRaised = false;
        view.TextChanging += (_, e) => e.Cancel = true;
        view.TextChanged += (_, _) => changedRaised = true;

        view.Text = "modified";

        Assert.False (changedRaised);
    }

    [Fact]
    public void TextChanging_RaisedBeforeMutation ()
    {
        View view = new () { Text = "before" };
        string? textDuringChanging = null;
        view.TextChanging += (sender, _) => textDuringChanging = ((View)sender!).Text;

        view.Text = "after";

        Assert.Equal ("before", textDuringChanging);
    }

    [Fact]
    public void TextChanged_RaisedAfterMutation ()
    {
        View view = new () { Text = "before" };
        string? textDuringChanged = null;
        view.TextChanged += (sender, _) => textDuringChanged = ((View)sender!).Text;

        view.Text = "after";

        Assert.Equal ("after", textDuringChanged);
    }

    [Fact]
    public void OnTextChanging_Override_CanCancel ()
    {
        CancellingView view = new ();
        // Set initial text before enabling cancellation
        view.AllowChange = true;
        view.Text = "initial";
        view.AllowChange = false;

        view.Text = "blocked";

        Assert.Equal ("initial", view.Text);
    }

    [Fact]
    public void OnTextChanged_Override_CalledAfterChange ()
    {
        TrackingView view = new () { Text = "start" };

        view.Text = "end";

        Assert.True (view.OnTextChangedCalled);
        Assert.Equal ("end", view.TextAtOnTextChanged);
    }

    [Fact]
    public void TextChanging_EventOrder_ChangingBeforeChanged ()
    {
        View view = new () { Text = "a" };
        List<string> order = [];
        view.TextChanging += (_, _) => order.Add ("changing");
        view.TextChanged += (_, _) => order.Add ("changed");

        view.Text = "b";

        Assert.Equal (["changing", "changed"], order);
    }

    /// <summary>A test subclass that cancels text changes via <see cref="View.OnTextChanging"/>.</summary>
    private class CancellingView : View
    {
        public bool AllowChange { get; set; }

        protected override bool OnTextChanging () => !AllowChange;
    }

    /// <summary>A test subclass that tracks calls to <see cref="View.OnTextChanged"/>.</summary>
    private class TrackingView : View
    {
        public bool OnTextChangedCalled { get; private set; }
        public string? TextAtOnTextChanged { get; private set; }

        protected override void OnTextChanged ()
        {
            OnTextChangedCalled = true;
            TextAtOnTextChanged = Text;

            base.OnTextChanged ();
        }
    }
}
