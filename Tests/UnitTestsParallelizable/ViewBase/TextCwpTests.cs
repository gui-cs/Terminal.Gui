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

    // --- CR feedback tests: these must fail before fixes, pass after ---

    /// <summary>
    ///     CR Issue 1: <see cref="View.OnTextChanging"/> should raise <see cref="View.TextChanging"/>
    ///     (CWP pattern: virtual method raises the event). A subclass that calls base.OnTextChanging()
    ///     should get the event's cancellation result.
    /// </summary>
    [Fact]
    public void OnTextChanging_BaseRaisesEvent_SubclassGetsCancel () // Copilot
    {
        DelegatingView view = new ();
        view.TextChanging += (_, e) => e.Cancel = true;

        view.Text = "hello";

        // base.OnTextChanging() should have raised TextChanging and returned true (cancelled)
        Assert.True (view.BaseReturnedCancel);
        Assert.Equal (string.Empty, view.Text);
    }

    /// <summary>
    ///     CR Issue 2: Setting <see cref="View.Text"/> polymorphically on a <see cref="TextField"/>
    ///     should sync the TextField's internal model so that <c>textField.Text</c> returns the new value.
    /// </summary>
    [Fact]
    public void TextField_PolymorphicSet_SyncsInternalModel () // Copilot
    {
        TextField tf = new ();
        View v = tf;

        v.Text = "hello";

        Assert.Equal ("hello", tf.Text);
    }

    /// <summary>
    ///     CR Issue 3: Setting <see cref="View.Text"/> polymorphically on a <see cref="TextView"/>
    ///     should sync the TextView's internal model so that <c>textView.Text</c> returns the new value.
    /// </summary>
    [Fact]
    public void TextView_PolymorphicSet_SyncsInternalModel () // Copilot
    {
        TextView tv = new ();
        View v = tv;

        v.Text = "hello";

        Assert.Equal ("hello", tv.Text);
    }

    /// <summary>
    ///     CR Issue 4: A newly constructed <see cref="ProgressBar"/> should have <see cref="View.Text"/>
    ///     set to "0%" so that <see cref="ProgressBarFormat.SimplePlusPercentage"/> renders the percentage
    ///     on first draw.
    /// </summary>
    [Fact]
    public void ProgressBar_Constructor_TextShowsZeroPercent () // Copilot
    {
        ProgressBar pb = new ();

        Assert.Equal ("0%", pb.Text);
    }

    /// <summary>A test subclass that cancels text changes via <see cref="View.OnTextChanging"/>.</summary>
    private class CancellingView : View
    {
        public bool AllowChange { get; set; }

        protected override bool OnTextChanging (string newText) => !AllowChange;
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

    /// <summary>
    ///     A test subclass that delegates to <c>base.OnTextChanging()</c> and records
    ///     whether the base returned a cancel signal.
    /// </summary>
    private class DelegatingView : View
    {
        public bool BaseReturnedCancel { get; private set; }

        protected override bool OnTextChanging (string newText)
        {
            BaseReturnedCancel = base.OnTextChanging (newText);

            return BaseReturnedCancel;
        }
    }
}
