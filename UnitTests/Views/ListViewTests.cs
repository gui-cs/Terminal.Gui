using System.Collections;
using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ListViewTests
{
    private readonly ITestOutputHelper _output;
    public ListViewTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Constructors_Defaults ()
    {
        var lv = new ListView ();
        Assert.Null (lv.Source);
        Assert.True (lv.CanFocus);
        Assert.Equal (-1, lv.SelectedItem);

        lv = new ListView { Source = new ListWrapper (new List<string> { "One", "Two", "Three" }) };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);

        lv = new ListView { Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);

        lv = new ListView
        {
            Y = 1, Width = 10, Height = 20, Source = new ListWrapper (new List<string> { "One", "Two", "Three" })
        };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Equal (new Rectangle (0, 1, 10, 20), lv.Frame);

        lv = new ListView { Y = 1, Width = 10, Height = 20, Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Equal (new Rectangle (0, 1, 10, 20), lv.Frame);
    }

    [Fact]
    [AutoInitShutdown]
    public void Ensures_Visibility_SelectedItem_On_MoveDown_And_MoveUp ()
    {
        List<string> source = new ();

        for (var i = 0; i < 20; i++)
        {
            source.Add ($"Line{i}");
        }

        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill (), Source = new ListWrapper (source) };
        var win = new Window ();
        win.Add (lv);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (12, 12);
        Application.Refresh ();

        Assert.Equal (-1, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.ScrollDown (10));
        lv.Draw ();
        Assert.Equal (-1, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.MoveDown ());
        lv.Draw ();
        Assert.Equal (0, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.MoveEnd ());
        lv.Draw ();
        Assert.Equal (19, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line19    │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.ScrollUp (20));
        lv.Draw ();
        Assert.Equal (19, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.MoveDown ());
        lv.Draw ();
        Assert.Equal (19, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.ScrollUp (20));
        lv.Draw ();
        Assert.Equal (19, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.MoveDown ());
        lv.Draw ();
        Assert.Equal (19, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.MoveHome ());
        lv.Draw ();
        Assert.Equal (0, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.ScrollDown (20));
        lv.Draw ();
        Assert.Equal (0, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line19    │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
└──────────┘",
                                                      _output
                                                     );

        Assert.True (lv.MoveUp ());
        lv.Draw ();
        Assert.Equal (0, lv.SelectedItem);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsureSelectedItemVisible_SelectedItem ()
    {
        List<string> source = new ();

        for (var i = 0; i < 10; i++)
        {
            source.Add ($"Item {i}");
        }

        var lv = new ListView { Width = 10, Height = 5, Source = new ListWrapper (source) };
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
Item 0
Item 1
Item 2
Item 3
Item 4",
                                                      _output
                                                     );

        // EnsureSelectedItemVisible is auto enabled on the OnSelectedChanged
        lv.SelectedItem = 6;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
Item 2
Item 3
Item 4
Item 5
Item 6",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsureSelectedItemVisible_Top ()
    {
        List<string> source = new () { "First", "Second" };
        var lv = new ListView { Width = Dim.Fill (), Height = 1, Source = new ListWrapper (source) };
        lv.SelectedItem = 1;
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);

        Assert.Equal ("Second ", GetContents (0));
        Assert.Equal (new string (' ', 7), GetContents (1));

        lv.MoveUp ();
        lv.Draw ();

        Assert.Equal ("First  ", GetContents (0));
        Assert.Equal (new string (' ', 7), GetContents (1));

        string GetContents (int line)
        {
            var item = "";

            for (var i = 0; i < 7; i++)
            {
                item += Application.Driver.Contents [line, i].Rune;
            }

            return item;
        }
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        List<string> source = new () { "One", "Two", "Three" };
        var lv = new ListView { Height = 2, AllowsMarking = true, Source = new ListWrapper (source) };
        lv.BeginInit ();
        lv.EndInit ();
        Assert.Equal (-1, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.PageDown));
        Assert.Equal (2, lv.SelectedItem);
        Assert.Equal (2, lv.TopItem);
        Assert.True (lv.NewKeyDownEvent (Key.PageUp));
        Assert.Equal (0, lv.SelectedItem);
        Assert.Equal (0, lv.TopItem);
        Assert.False (lv.Source.IsMarked (lv.SelectedItem));
        Assert.True (lv.NewKeyDownEvent (Key.Space));
        Assert.True (lv.Source.IsMarked (lv.SelectedItem));
        var opened = false;
        lv.OpenSelectedItem += (s, _) => opened = true;
        Assert.True (lv.NewKeyDownEvent (Key.Enter));
        Assert.True (opened);
        Assert.True (lv.NewKeyDownEvent (Key.End));
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.Home));
        Assert.Equal (0, lv.SelectedItem);
    }

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new ListView ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var listView = new ListView ();
        var accepted = false;

        listView.Accept += OnAccept;
        listView.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;
        void OnAccept (object sender, CancelEventArgs e) { accepted = true; }
    }


    [Fact]
    public void Accept_Command_Accepts_and_Opens_Selected_Item ()
    {
        List<string> source = ["One", "Two", "Three"];
        var listView = new ListView {Source = new ListWrapper (source) };
        listView.SelectedItem = 0;

        var accepted = false;
        var opened = false;
        string selectedValue = string.Empty;

        listView.Accept += Accept;
        listView.OpenSelectedItem += OpenSelectedItem;

        listView.InvokeCommand (Command.Accept);

        Assert.True (accepted);
        Assert.True (opened);
        Assert.Equal (source [0], selectedValue);

        return;

        void OpenSelectedItem (object sender, ListViewItemEventArgs e)
        {
            opened = true;
            selectedValue = e.Value.ToString ();
        }
        void Accept (object sender, CancelEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Cancel_Event_Prevents_OpenSelectedItem ()
    {
        List<string> source = ["One", "Two", "Three"];
        var listView = new ListView { Source = new ListWrapper (source) };
        listView.SelectedItem = 0;

        var accepted = false;
        var opened = false;
        string selectedValue = string.Empty;

        listView.Accept += Accept;
        listView.OpenSelectedItem += OpenSelectedItem;

        listView.InvokeCommand (Command.Accept);

        Assert.True (accepted);
        Assert.False (opened);
        Assert.Equal (string.Empty, selectedValue);

        return;

        void OpenSelectedItem (object sender, ListViewItemEventArgs e)
        {
            opened = true;
            selectedValue = e.Value.ToString ();
        }

        void Accept (object sender, CancelEventArgs e)
        {
            accepted = true;
            e.Cancel = true;
        }
    }

    /// <summary>
    ///     Tests that when none of the Commands in a chained keybinding are possible the
    ///     <see cref="View.NewKeyDownEvent"/> returns the appropriate result
    /// </summary>
    [Fact]
    public void ListViewProcessKeyReturnValue_WithMultipleCommands ()
    {
        var lv = new ListView { Source = new ListWrapper (new List<string> { "One", "Two", "Three", "Four" }) };

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Equal (-1, lv.SelectedItem);

        // bind shift down to move down twice in control
        lv.KeyBindings.Add (Key.CursorDown.WithShift, Command.LineDown, Command.LineDown);

        var ev = Key.CursorDown.WithShift;

        Assert.True (lv.NewKeyDownEvent (ev), "The first time we move down 2 it should be possible");

        // After moving down twice from -1 we should be at 'Two'
        Assert.Equal (1, lv.SelectedItem);

        // clear the items
        lv.SetSource (null);

        // Press key combo again - return should be false this time as none of the Commands are allowable
        Assert.False (lv.NewKeyDownEvent (ev), "We cannot move down so will not respond to this");
    }

    [Fact]
    public void ListViewSelectThenDown ()
    {
        var lv = new ListView { Source = new ListWrapper (new List<string> { "One", "Two", "Three" }) };
        lv.AllowsMarking = true;

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Equal (-1, lv.SelectedItem);

        // nothing is ticked
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        lv.KeyBindings.Add (Key.Space.WithShift, Command.Select, Command.LineDown);

        var ev = Key.Space.WithShift;

        // view should indicate that it has accepted and consumed the event
        Assert.True (lv.NewKeyDownEvent (ev));

        // first item should now be selected
        Assert.Equal (0, lv.SelectedItem);

        // none of the items should be ticked
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (ev));

        // second item should now be selected
        Assert.Equal (1, lv.SelectedItem);

        // first item only should be ticked
        Assert.True (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (ev));
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (ev));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.True (lv.Source.IsMarked (2)); // but can toggle marked

        // Press key combo again 
        Assert.True (lv.NewKeyDownEvent (ev));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2)); // untoggle toggle marked
    }

    [Fact]
    public void ListWrapper_StartsWith ()
    {
        var lw = new ListWrapper (new List<string> { "One", "Two", "Three" });

        Assert.Equal (1, lw.StartsWith ("t"));
        Assert.Equal (1, lw.StartsWith ("tw"));
        Assert.Equal (2, lw.StartsWith ("th"));
        Assert.Equal (1, lw.StartsWith ("T"));
        Assert.Equal (1, lw.StartsWith ("TW"));
        Assert.Equal (2, lw.StartsWith ("TH"));

        lw = new ListWrapper (new List<string> { "One", "Two", "Three" });

        Assert.Equal (1, lw.StartsWith ("t"));
        Assert.Equal (1, lw.StartsWith ("tw"));
        Assert.Equal (2, lw.StartsWith ("th"));
        Assert.Equal (1, lw.StartsWith ("T"));
        Assert.Equal (1, lw.StartsWith ("TW"));
        Assert.Equal (2, lw.StartsWith ("TH"));
    }

    [Fact]
    public void OnEnter_Does_Not_Throw_Exception ()
    {
        var lv = new ListView ();
        var top = new View ();
        top.Add (lv);
        Exception exception = Record.Exception (lv.SetFocus);
        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void RowRender_Event ()
    {
        var rendered = false;
        List<string> source = new () { "one", "two", "three" };
        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill () };
        lv.RowRender += (s, _) => rendered = true;
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);
        Assert.False (rendered);

        lv.SetSource (source);
        lv.Draw ();
        Assert.True (rendered);
    }

    [Fact]
    public void SelectedItem_Get_Set ()
    {
        var lv = new ListView { Source = new ListWrapper (new List<string> { "One", "Two", "Three" }) };
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Throws<ArgumentException> (() => lv.SelectedItem = 3);
        Exception exception = Record.Exception (() => lv.SelectedItem = -1);
        Assert.Null (exception);
    }

    [Fact]
    public void SetSource_Preserves_ListWrapper_Instance_If_Not_Null ()
    {
        var lv = new ListView { Source = new ListWrapper (new List<string> { "One", "Two" }) };

        Assert.NotNull (lv.Source);

        lv.SetSource (null);
        Assert.NotNull (lv.Source);

        lv.Source = null;
        Assert.Null (lv.Source);

        lv = new ListView { Source = new ListWrapper (new List<string> { "One", "Two" }) };
        Assert.NotNull (lv.Source);

        lv.SetSourceAsync (null);
        Assert.NotNull (lv.Source);
    }

    [Fact]
    public void SettingEmptyKeybindingThrows ()
    {
        var lv = new ListView { Source = new ListWrapper (new List<string> { "One", "Two", "Three" }) };
        Assert.Throws<ArgumentException> (() => lv.KeyBindings.Add (Key.Space));
    }

    private class NewListDataSource : IListDataSource
    {
        public int Count => throw new NotImplementedException ();
        public int Length => throw new NotImplementedException ();
        public bool IsMarked (int item) { throw new NotImplementedException (); }

        public void Render (
            ListView container,
            ConsoleDriver driver,
            bool selected,
            int item,
            int col,
            int line,
            int width,
            int start = 0
        )
        {
            throw new NotImplementedException ();
        }

        public void SetMark (int item, bool value) { throw new NotImplementedException (); }
        public IList ToList () { return new List<string> { "One", "Two", "Three" }; }
    }

    [Fact]
    [AutoInitShutdown]
    public void Clicking_On_Border_Is_Ignored ()
    {
        var selected = "";
        var lv = new ListView
        {
            Height = 5,
            Width = 7,
            BorderStyle = LineStyle.Single
        };
        lv.SetSource (new List<string> { "One", "Two", "Three", "Four" });
        lv.SelectedItemChanged += (s, e) => selected = e.Value.ToString ();
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);

        Assert.Equal (new Thickness (1), lv.Border.Thickness);
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Equal ("", lv.Text);
        TestHelpers.AssertDriverContentsWithFrameAre (@"
┌─────┐
│One  │
│Two  │
│Three│
└─────┘", _output);

        Application.OnMouseEvent (new (new ()
        {
            X = 0,
            Y = 0,
            Flags = MouseFlags.Button1Clicked
        }));
        Assert.Equal ("", selected);
        Assert.Equal (-1, lv.SelectedItem);

        Application.OnMouseEvent (new (new ()
        {
            X = 1,
            Y = 1,
            Flags = MouseFlags.Button1Clicked
        }));
        Assert.Equal ("One", selected);
        Assert.Equal (0, lv.SelectedItem);

        Application.OnMouseEvent (new (new ()
        {
            X = 1,
            Y = 2,
            Flags = MouseFlags.Button1Clicked
        }));
        Assert.Equal ("Two", selected);
        Assert.Equal (1, lv.SelectedItem);

        Application.OnMouseEvent (new (new ()
        {
            X = 1,
            Y = 3,
            Flags = MouseFlags.Button1Clicked
        }));
        Assert.Equal ("Three", selected);
        Assert.Equal (2, lv.SelectedItem);

        Application.OnMouseEvent (new (new ()
        {
            X = 1,
            Y = 4,
            Flags = MouseFlags.Button1Clicked
        }));
        Assert.Equal ("Three", selected);
        Assert.Equal (2, lv.SelectedItem);
    }
}
