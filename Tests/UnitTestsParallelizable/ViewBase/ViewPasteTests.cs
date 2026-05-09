namespace ViewBaseTests;

// Claude - Opus 4.7
public class ViewPasteTests
{
    [Fact]
    public void NewPasteEvent_RaisesPasteEvent ()
    {
        View view = new ();

        string? captured = null;
        view.Pasted += (_, args) => captured = args.Text;

        bool handled = view.NewPasteEvent (new ("hello"));

        Assert.Equal ("hello", captured);
        Assert.False (handled);
    }

    [Fact]
    public void NewPasteEvent_OnPasted_CalledBeforeEvent ()
    {
        var subject = new TrackingView ();
        var raiseOrder = new List<string> ();

        subject.Pasted += (_, _) => raiseOrder.Add ("event");
        subject.OnPastedCalled = () => raiseOrder.Add ("virtual");

        subject.NewPasteEvent (new ("body"));

        Assert.Equal (["virtual", "event"], raiseOrder);
    }

    [Fact]
    public void NewPasteEvent_OnPastedReturnsTrue_ShortCircuits ()
    {
        var subject = new TrackingView { OnPastedReturn = true };
        var eventFired = false;
        subject.Pasted += (_, _) => eventFired = true;

        bool handled = subject.NewPasteEvent (new ("body"));

        Assert.True (handled);
        Assert.False (eventFired);
    }

    [Fact]
    public void NewPasteEvent_HandledByEvent_BlocksBubbling ()
    {
        View parent = new ();
        View child = new ();
        parent.Add (child);

        var parentSawPaste = false;
        parent.Pasted += (_, _) => parentSawPaste = true;
        child.Pasted += (_, args) => args.Handled = true;

        bool handled = child.NewPasteEvent (new ("x"));

        Assert.True (handled);
        Assert.False (parentSawPaste);
    }

    [Fact]
    public void NewPasteEvent_NotHandled_BubblesToSuperView ()
    {
        View parent = new ();
        View child = new ();
        parent.Add (child);

        string? parentText = null;
        parent.Pasted += (_, args) => parentText = args.Text;

        child.NewPasteEvent (new ("bubble"));

        Assert.Equal ("bubble", parentText);
    }

    [Fact]
    public void NewPasteEvent_DisabledView_ShortCircuits ()
    {
        View view = new () { Enabled = false };

        var fired = false;
        view.Pasted += (_, _) => fired = true;

        bool handled = view.NewPasteEvent (new ("x"));

        Assert.False (fired);
        Assert.False (handled);
    }

    [Fact]
    public void TextField_OnPasted_InsertsTextAtCursor ()
    {
        TextField field = new () { Text = "abc", ReadOnly = false };
        field.MoveEnd ();

        bool handled = field.NewPasteEvent (new ("XYZ"));

        Assert.True (handled);
        Assert.Equal ("abcXYZ", field.Text);
    }

    [Fact]
    public void TextField_OnPasted_ReadOnly_DoesNotInsert ()
    {
        TextField field = new () { Text = "abc", ReadOnly = true };

        bool handled = field.NewPasteEvent (new ("XYZ"));

        Assert.False (handled);
        Assert.Equal ("abc", field.Text);
    }

    [Fact]
    public void TextView_OnPasted_InsertsText ()
    {
        TextView textView = new () { Text = "abc", ReadOnly = false };
        textView.MoveEnd ();

        bool handled = textView.NewPasteEvent (new ("XYZ"));

        Assert.True (handled);
        Assert.Contains ("XYZ", textView.Text);
    }

    [Fact]
    public void TextView_OnPasted_ReadOnly_DoesNotInsert ()
    {
        TextView textView = new () { Text = "abc", ReadOnly = true };

        bool handled = textView.NewPasteEvent (new ("XYZ"));

        Assert.False (handled);
        Assert.Equal ("abc", textView.Text);
    }

    [Fact]
    public void TextField_OnPasted_TakesFirstLineOnly ()
    {
        TextField field = new () { Text = string.Empty };

        bool handled = field.NewPasteEvent (new ("first\nsecond\rthird"));

        Assert.True (handled);
        Assert.Equal ("first", field.Text);
    }

    [Fact]
    public void TextField_OnPasted_StripsControlCharsIncludingEscape ()
    {
        TextField field = new () { Text = string.Empty };

        // Embed an ESC[31m color sequence and a literal bell character — both must be stripped.
        bool handled = field.NewPasteEvent (new ("ab\u001b[31mc\u0007d"));

        Assert.True (handled);
        Assert.Equal ("ab[31mcd", field.Text);
    }

    [Fact]
    public void TextField_OnPasted_AllControlChars_DoesNotInsert ()
    {
        TextField field = new () { Text = "x" };

        bool handled = field.NewPasteEvent (new ("\u001b\u0007\u0003"));

        Assert.False (handled);
        Assert.Equal ("x", field.Text);
    }

    [Fact]
    public void TextView_OnPasted_NormalizesCarriageReturnsToLogicalLineBreaks ()
    {
        TextView textView = new () { Text = string.Empty };

        // Mix of CR (terminal default), CRLF, and bare LF — all should become logical line breaks.
        bool handled = textView.NewPasteEvent (new ("a\rb\r\nc\nd"));

        Assert.True (handled);
        Assert.Equal ($"a{Environment.NewLine}b{Environment.NewLine}c{Environment.NewLine}d", textView.Text);
    }

    [Fact]
    public void TextView_OnPasted_StripsEscapeAndOtherControlChars ()
    {
        TextView textView = new () { Text = string.Empty };

        bool handled = textView.NewPasteEvent (new ("a\u001b[31mb\u0007c\td"));

        Assert.True (handled);
        Assert.Equal ("a[31mbc\td", textView.Text);
    }

    private sealed class TrackingView : View
    {
        public Action? OnPastedCalled { get; set; }
        public bool OnPastedReturn { get; set; }

        protected override bool OnPasted (PasteEventArgs args)
        {
            OnPastedCalled?.Invoke ();

            return OnPastedReturn;
        }
    }
}
