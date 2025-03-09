using UnitTests;

namespace Terminal.Gui.ViewsTests;

public class TimeFieldTests
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var tf = new TimeField ();
        tf.Layout ();
        Assert.False (tf.IsShortFormat);
        Assert.Equal (TimeSpan.MinValue, tf.Time);
        Assert.Equal (1, tf.CursorPosition);
        Assert.Equal (new Rectangle (0, 0, 10, 1), tf.Frame);

        TimeSpan time = DateTime.Now.TimeOfDay;
        tf = new TimeField { Time = time };
        tf.Layout ();
        Assert.False (tf.IsShortFormat);
        Assert.Equal (time, tf.Time);
        Assert.Equal (1, tf.CursorPosition);
        Assert.Equal (new Rectangle (0, 0, 10, 1), tf.Frame);

        tf = new TimeField { X = 1, Y = 2, Time = time };
        tf.Layout ();
        Assert.False (tf.IsShortFormat);
        Assert.Equal (time, tf.Time);
        Assert.Equal (1, tf.CursorPosition);
        Assert.Equal (new Rectangle (1, 2, 10, 1), tf.Frame);

        tf = new TimeField { X = 3, Y = 4, Time = time, IsShortFormat = true };
        tf.Layout ();
        Assert.True (tf.IsShortFormat);
        Assert.Equal (time, tf.Time);
        Assert.Equal (1, tf.CursorPosition);
        Assert.Equal (new Rectangle (3, 4, 7, 1), tf.Frame);

        tf.IsShortFormat = false;
        tf.Layout ();
        Assert.Equal (new Rectangle (3, 4, 10, 1), tf.Frame);
        Assert.Equal (10, tf.Width);
    }

    [Fact]
    [AutoInitShutdown (useFakeClipboard: true)]
    public void Copy_Paste ()
    {
        var tf1 = new TimeField { Time = TimeSpan.Parse ("12:12:19") };
        var tf2 = new TimeField { Time = TimeSpan.Parse ("12:59:01") };

        // Select all text
        Assert.True (tf2.NewKeyDownEvent (Key.End.WithShift));
        Assert.Equal (1, tf2.SelectedStart);
        Assert.Equal (8, tf2.SelectedLength);
        Assert.Equal (9, tf2.CursorPosition);

        // Copy from tf2
        Assert.True (tf2.NewKeyDownEvent (Key.C.WithCtrl));

        // Paste into tf1
        Assert.True (tf1.NewKeyDownEvent (Key.V.WithCtrl));
        Assert.Equal (" 12:59:01", tf1.Text);
        Assert.Equal (9, tf1.CursorPosition);
    }

    [Fact]
    public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format ()
    {
        var tf = new TimeField ();
        Assert.Equal (1, tf.CursorPosition);
        tf.CursorPosition = 0;
        Assert.Equal (1, tf.CursorPosition);
        tf.CursorPosition = 9;
        Assert.Equal (8, tf.CursorPosition);
        tf.IsShortFormat = true;
        tf.CursorPosition = 0;
        Assert.Equal (1, tf.CursorPosition);
        tf.CursorPosition = 6;
        Assert.Equal (5, tf.CursorPosition);
    }

    [Fact]
    public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format_After_Selection ()
    {
        var tf = new TimeField ();

        // Start selection
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.Equal (1, tf.SelectedStart);
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal (0, tf.CursorPosition);

        // Without selection
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (-1, tf.SelectedStart);
        Assert.Equal (0, tf.SelectedLength);
        Assert.Equal (1, tf.CursorPosition);
        tf.CursorPosition = 8;
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (8, tf.SelectedStart);
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal (9, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (-1, tf.SelectedStart);
        Assert.Equal (0, tf.SelectedLength);
        Assert.Equal (8, tf.CursorPosition);
        Assert.False (tf.IsShortFormat);
        Assert.False (tf.IsInitialized);
        tf.BeginInit ();
        tf.EndInit ();
        tf.IsShortFormat = true;
        Assert.Equal (5, tf.CursorPosition);

        // Start selection
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (5, tf.SelectedStart);
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal (6, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (-1, tf.SelectedStart);
        Assert.Equal (0, tf.SelectedLength);
        Assert.Equal (5, tf.CursorPosition);
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var tf = new TimeField { Time = TimeSpan.Parse ("12:12:19") };
        tf.BeginInit ();
        tf.EndInit ();
        Assert.Equal (9, tf.CursorPosition);
        tf.CursorPosition = 1;
        tf.ReadOnly = true;
        Assert.True (tf.NewKeyDownEvent (Key.Delete));
        Assert.Equal (" 12:12:19", tf.Text);
        tf.ReadOnly = false;
        Assert.True (tf.NewKeyDownEvent (Key.D.WithCtrl));
        Assert.Equal (" 02:12:19", tf.Text);
        tf.CursorPosition = 4;
        tf.ReadOnly = true;
        Assert.True (tf.NewKeyDownEvent (Key.Delete));
        Assert.Equal (" 02:12:19", tf.Text);
        tf.ReadOnly = false;
        Assert.True (tf.NewKeyDownEvent (Key.Backspace));
        Assert.Equal (" 02:02:19", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.Home));
        Assert.Equal (1, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.End));
        Assert.Equal (8, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.A.WithCtrl));
        Assert.Equal (1, tf.CursorPosition);
        Assert.Equal (9, tf.Text.Length);
        Assert.True (tf.NewKeyDownEvent (Key.E.WithCtrl));
        Assert.Equal (8, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (7, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (8, tf.CursorPosition);

        // Non-numerics are ignored
        Assert.False (tf.NewKeyDownEvent (Key.A));
        tf.ReadOnly = true;
        tf.CursorPosition = 1;
        Assert.True (tf.NewKeyDownEvent (Key.D1));
        Assert.Equal (" 02:02:19", tf.Text);
        tf.ReadOnly = false;
        Assert.True (tf.NewKeyDownEvent (Key.D1));
        Assert.Equal (" 12:02:19", tf.Text);
        Assert.Equal (2, tf.CursorPosition);
#if UNIX_KEY_BINDINGS
        Assert.True (tf.NewKeyDownEvent (Key.D.WithAlt));
        Assert.Equal (" 10:02:19", tf.Text);
#endif
    }

    [Fact]
    public void Typing_With_Selection_Normalize_Format ()
    {
        var tf = new TimeField { Time = TimeSpan.Parse ("12:12:19") };

        // Start selection at before the first separator :
        tf.CursorPosition = 2;

        // Now select the separator :
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (2, tf.SelectedStart);
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal (3, tf.CursorPosition);

        // Type 3 over the separator
        Assert.True (tf.NewKeyDownEvent (Key.D3));

        // The format was normalized and replaced again with :
        Assert.Equal (" 12:12:19", tf.Text);
        Assert.Equal (4, tf.CursorPosition);
    }
}
