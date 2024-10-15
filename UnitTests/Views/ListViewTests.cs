using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ListViewTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var lv = new ListView ();
        Assert.Null (lv.Source);
        Assert.True (lv.CanFocus);
        Assert.Equal (-1, lv.SelectedItem);

        lv = new () { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);

        lv = new () { Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);

        lv = new ()
        {
            Y = 1, Width = 10, Height = 20, Source = new ListWrapper<string> (["One", "Two", "Three"])
        };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Equal (new (0, 1, 10, 20), lv.Frame);

        lv = new () { Y = 1, Width = 10, Height = 20, Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Equal (new (0, 1, 10, 20), lv.Frame);
    }

    [Fact]
    [AutoInitShutdown]
    public void Ensures_Visibility_SelectedItem_On_MoveDown_And_MoveUp ()
    {
        ObservableCollection<string> source = [];

        for (var i = 0; i < 20; i++)
        {
            source.Add ($"Line{i}");
        }

        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill (), Source = new ListWrapper<string> (source) };
        var win = new Window ();
        win.Add (lv);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (12, 12);
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
                                                      output
                                                     );

        Assert.True (lv.ScrollVertical (10));
        //Application.Refresh ();
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
                                                      output
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
                                                      output
                                                     );

        Assert.True (lv.MoveEnd ());
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
                                                      output
                                                     );

        Assert.True (lv.ScrollVertical (-20));
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
                                                      output
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
                                                      output
                                                     );

        Assert.True (lv.ScrollVertical (-20));
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
                                                      output
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
                                                      output
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
                                                      output
                                                     );

        Assert.True (lv.ScrollVertical (20));
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
                                                      output
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
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsureSelectedItemVisible_SelectedItem ()
    {
        ObservableCollection<string> source = [];

        for (var i = 0; i < 10; i++)
        {
            source.Add ($"Item {i}");
        }

        var lv = new ListView { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
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
                                                      output
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
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsureSelectedItemVisible_Top ()
    {
        ObservableCollection<string> source = ["First", "Second"];
        var lv = new ListView { Width = Dim.Fill (), Height = 1, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 1;
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);

        Assert.Equal ("Second ", GetContents (0));
        Assert.Equal (new (' ', 7), GetContents (1));

        lv.MoveUp ();
        lv.Draw ();

        Assert.Equal ("First  ", GetContents (0));
        Assert.Equal (new (' ', 7), GetContents (1));

        string GetContents (int line)
        {
            var item = "";

            for (var i = 0; i < 7; i++)
            {
                item += Application.Driver?.Contents [line, i].Rune;
            }

            return item;
        }
        top.Dispose ();
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];
        var lv = new ListView { Height = 2, AllowsMarking = true, Source = new ListWrapper<string> (source) };
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

        listView.Accepting += OnAccepted;
        listView.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccepted (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Command_Accepts_and_Opens_Selected_Item ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];
        var listView = new ListView { Source = new ListWrapper<string> (source) };
        listView.SelectedItem = 0;

        var accepted = false;
        var opened = false;
        var selectedValue = string.Empty;

        listView.Accepting += Accepted;
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

        void Accepted (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Cancel_Event_Prevents_OpenSelectedItem ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];
        var listView = new ListView { Source = new ListWrapper<string> (source) };
        listView.SelectedItem = 0;

        var accepted = false;
        var opened = false;
        var selectedValue = string.Empty;

        listView.Accepting += Accepted;
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

        void Accepted (object sender, CommandEventArgs e)
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
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three", "Four"]) };

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Equal (-1, lv.SelectedItem);

        // bind shift down to move down twice in control
        lv.KeyBindings.Add (Key.CursorDown.WithShift, Command.Down, Command.Down);

        Key ev = Key.CursorDown.WithShift;

        Assert.True (lv.NewKeyDownEvent (ev), "The first time we move down 2 it should be possible");

        // After moving down twice from -1 we should be at 'Two'
        Assert.Equal (1, lv.SelectedItem);

        // clear the items
        lv.SetSource<string> (null);

        // Press key combo again - return should be false this time as none of the Commands are allowable
        Assert.False (lv.NewKeyDownEvent (ev), "We cannot move down so will not respond to this");
    }

    [Fact]
    public void AllowsMarking_True_SpaceWithShift_SelectsThenDown ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        lv.AllowsMarking = true;

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Equal (-1, lv.SelectedItem);

        // nothing is ticked
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // view should indicate that it has accepted and consumed the event
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));

        // first item should now be selected
        Assert.Equal (0, lv.SelectedItem);

        // none of the items should be ticked
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));

        // second item should now be selected
        Assert.Equal (1, lv.SelectedItem);

        // first item only should be ticked
        Assert.True (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.True (lv.Source.IsMarked (2)); // but can toggle marked

        // Press key combo again 
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2)); // untoggle toggle marked
    }

    [Fact]
    public void ListWrapper_StartsWith ()
    {
        var lw = new ListWrapper<string> (["One", "Two", "Three"]);

        Assert.Equal (1, lw.StartsWith ("t"));
        Assert.Equal (1, lw.StartsWith ("tw"));
        Assert.Equal (2, lw.StartsWith ("th"));
        Assert.Equal (1, lw.StartsWith ("T"));
        Assert.Equal (1, lw.StartsWith ("TW"));
        Assert.Equal (2, lw.StartsWith ("TH"));

        lw = new (["One", "Two", "Three"]);

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
        Exception exception = Record.Exception (() => lv.SetFocus ());
        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void RowRender_Event ()
    {
        var rendered = false;
        ObservableCollection<string> source = ["one", "two", "three"];
        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill () };
        lv.RowRender += (s, _) => rendered = true;
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);
        Assert.False (rendered);

        lv.SetSource (source);
        lv.Draw ();
        Assert.True (rendered);
        top.Dispose ();
    }

    [Fact]
    public void SelectedItem_Get_Set ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Throws<ArgumentException> (() => lv.SelectedItem = 3);
        Exception exception = Record.Exception (() => lv.SelectedItem = -1);
        Assert.Null (exception);
    }

    [Fact]
    public void SetSource_Preserves_ListWrapper_Instance_If_Not_Null ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two"]) };

        Assert.NotNull (lv.Source);

        lv.SetSource<string> (null);
        Assert.NotNull (lv.Source);

        lv.Source = null;
        Assert.Null (lv.Source);

        lv = new () { Source = new ListWrapper<string> (["One", "Two"]) };
        Assert.NotNull (lv.Source);

        lv.SetSourceAsync<string> (null);
        Assert.NotNull (lv.Source);
    }

    [Fact]
    public void SettingEmptyKeybindingThrows ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.Throws<ArgumentException> (() => lv.KeyBindings.Add (Key.Space));
    }

    private class NewListDataSource : IListDataSource
    {
#pragma warning disable CS0067
        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;
#pragma warning restore CS0067

        public int Count => 0;
        public int Length => 0;

        public bool SuspendCollectionChangedEvent { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

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

        public void Dispose ()
        {
            throw new NotImplementedException ();
        }
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
        lv.SetSource (["One", "Two", "Three", "Four"]);
        lv.SelectedItemChanged += (s, e) => selected = e.Value.ToString ();
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);

        Assert.Equal (new (1), lv.Border.Thickness);
        Assert.Equal (-1, lv.SelectedItem);
        Assert.Equal ("", lv.Text);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌─────┐
│One  │
│Two  │
│Three│
└─────┘",
                                                      output);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Clicked });
        Assert.Equal ("", selected);
        Assert.Equal (-1, lv.SelectedItem);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Clicked
                                  });
        Assert.Equal ("One", selected);
        Assert.Equal (0, lv.SelectedItem);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (1, 2), Flags = MouseFlags.Button1Clicked
                                  });
        Assert.Equal ("Two", selected);
        Assert.Equal (1, lv.SelectedItem);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (1, 3), Flags = MouseFlags.Button1Clicked
                                  });
        Assert.Equal ("Three", selected);
        Assert.Equal (2, lv.SelectedItem);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (1, 4), Flags = MouseFlags.Button1Clicked
                                  });
        Assert.Equal ("Three", selected);
        Assert.Equal (2, lv.SelectedItem);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void LeftItem_TopItem_Tests ()
    {
        ObservableCollection<string> source = [];

        for (int i = 0; i < 5; i++)
        {
            source.Add ($"Item {i}");
        }

        var lv = new ListView
        {
            X = 1,
            Width = 10,
            Height = 5,
            Source = new ListWrapper<string> (source)
        };
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
                                                      output);

        lv.LeftItem = 1;
        lv.TopItem = 1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 tem 1
 tem 2
 tem 3
 tem 4",
                                                      output);
        top.Dispose ();
    }

    [Fact]
    public void CollectionChanged_Event ()
    {
        var added = 0;
        var removed = 0;
        ObservableCollection<string> source = [];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.CollectionChanged += (sender, args) =>
                                {
                                    if (args.Action == NotifyCollectionChangedAction.Add)
                                    {
                                        added++;
                                    }
                                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                                    {
                                        removed++;
                                    }
                                };

        for (int i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }
        Assert.Equal (3, added);
        Assert.Equal (0, removed);

        added = 0;

        for (int i = 0; i < 3; i++)
        {
            source.Remove (source [0]);
        }
        Assert.Equal (0, added);
        Assert.Equal (3, removed);
        Assert.Empty (source);
    }

    [Fact]
    public void CollectionChanged_Event_Is_Only_Subscribed_Once ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        IList<string> source1 = [];
        var lv = new ListView { Source = new ListWrapper<string> (new (source1)) };

        lv.CollectionChanged += (sender, args) =>
                                {
                                    if (args.Action == NotifyCollectionChangedAction.Add)
                                    {
                                        added++;
                                    }
                                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                                    {
                                        removed++;
                                    }
                                    else
                                    {
                                        otherActions++;
                                    }
                                };

        ObservableCollection<string> source2 = [];
        lv.Source = new ListWrapper<string> (source2);
        ObservableCollection<string> source3 = [];
        lv.Source = new ListWrapper<string> (source3);
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (int i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
            source2.Add ($"Item{i}");
            source3.Add ($"Item{i}");
        }
        Assert.Equal (3, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        added = 0;

        for (int i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
            source2.Remove (source2 [0]);
            source3.Remove (source3 [0]);
        }
        Assert.Equal (0, added);
        Assert.Equal (3, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);
        Assert.Empty (source2);
        Assert.Empty (source3);
    }

    [Fact]
    public void CollectionChanged_Event_UnSubscribe_Previous_If_New_Is_Null ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        ObservableCollection<string> source1 = [];
        var lv = new ListView { Source = new ListWrapper<string> (source1) };

        lv.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                removed++;
            }
            else
            {
                otherActions++;
            }
        };

        lv.Source = new ListWrapper<string> (null);
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (int i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
        }
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (int i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
        }
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);
    }

    [Fact]
    public void ListWrapper_CollectionChanged_Event_Is_Only_Subscribed_Once ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        ObservableCollection<string> source1 = [];
        ListWrapper<string> lw = new (source1);

        lw.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                removed++;
            }
            else
            {
                otherActions++;
            }
        };

        ObservableCollection<string> source2 = [];
        lw = new (source2);
        ObservableCollection<string> source3 = [];
        lw = new (source3);
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (int i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
            source2.Add ($"Item{i}");
            source3.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        added = 0;

        for (int i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
            source2.Remove (source2 [0]);
            source3.Remove (source3 [0]);
        }
        Assert.Equal (0, added);
        Assert.Equal (3, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);
        Assert.Empty (source2);
        Assert.Empty (source3);
    }

    [Fact]
    public void ListWrapper_CollectionChanged_Event_UnSubscribe_Previous_Is_Disposed ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        ObservableCollection<string> source1 = [];
        ListWrapper<string> lw = new (source1);

        lw.CollectionChanged += Lw_CollectionChanged;

        lw.Dispose ();
        lw = new (null);
        Assert.Equal (0, lw.Count);
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (int i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
        }
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (int i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
        }
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);


        void Lw_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                removed++;
            }
            else
            {
                otherActions++;
            }
        }
    }

    [Fact]
    public void ListWrapper_SuspendCollectionChangedEvent_ResumeSuspendCollectionChangedEvent_Tests ()
    {
        var added = 0;
        ObservableCollection<string> source = [];
        ListWrapper<string> lw = new (source);

        lw.CollectionChanged += Lw_CollectionChanged;

        lw.SuspendCollectionChangedEvent = true;

        for (int i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }
        Assert.Equal (0, added);
        Assert.Equal (3, lw.Count);
        Assert.Equal (3, source.Count);

        lw.SuspendCollectionChangedEvent = false;

        for (int i = 3; i < 6; i++)
        {
            source.Add ($"Item{i}");
        }
        Assert.Equal (3, added);
        Assert.Equal (6, lw.Count);
        Assert.Equal (6, source.Count);


        void Lw_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
        }
    }

    [Fact]
    public void ListView_SuspendCollectionChangedEvent_ResumeSuspendCollectionChangedEvent_Tests ()
    {
        var added = 0;
        ObservableCollection<string> source = [];
        ListView lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.CollectionChanged += Lw_CollectionChanged;

        lv.SuspendCollectionChangedEvent ();

        for (int i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }
        Assert.Equal (0, added);
        Assert.Equal (3, lv.Source.Count);
        Assert.Equal (3, source.Count);

        lv.ResumeSuspendCollectionChangedEvent ();

        for (int i = 3; i < 6; i++)
        {
            source.Add ($"Item{i}");
        }
        Assert.Equal (3, added);
        Assert.Equal (6, lv.Source.Count);
        Assert.Equal (6, source.Count);


        void Lw_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
        }
    }
}