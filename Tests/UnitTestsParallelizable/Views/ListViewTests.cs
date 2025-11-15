#nullable enable
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Moq;

namespace UnitTests_Parallelizable.ViewsTests;

public class ListViewTests
{
    [Fact]
    public void CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        lv.KeyBindings.Add (Key.B, Command.Down);

        Assert.Null (lv.SelectedItem);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (0, lv.SelectedItem);

        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (1, lv.SelectedItem);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (lv.NewKeyDownEvent (Key.C));
        Assert.Equal (5, lv.SelectedItem);
    }

    [Fact]
    public void ListView_CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        lv.KeyBindings.Add (Key.B, Command.Down);

        Assert.Null (lv.SelectedItem);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (0, lv.SelectedItem);

        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (1, lv.SelectedItem);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (lv.NewKeyDownEvent (Key.C));
        Assert.Equal (5, lv.SelectedItem);
    }

    [Fact]
    public void ListViewCollectionNavigatorMatcher_DefaultBehaviour ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        // Keys are consumed during navigation
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.True (lv.NewKeyDownEvent (Key.A));
        Assert.True (lv.NewKeyDownEvent (Key.T));

        Assert.Equal ("bat", (string)lv.Source.ToList () [lv.SelectedItem!.Value]!);
    }

    [Fact]
    public void ListViewCollectionNavigatorMatcher_IgnoreKeys ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        Mock<ICollectionNavigatorMatcher> matchNone = new ();

        matchNone.Setup (m => m.IsCompatibleKey (It.IsAny<Key> ()))
                 .Returns (false);

        lv.KeystrokeNavigator.Matcher = matchNone.Object;

        // Keys are ignored because IsCompatibleKey returned false i.e. don't use these keys for navigation
        Assert.False (lv.NewKeyDownEvent (Key.B));
        Assert.False (lv.NewKeyDownEvent (Key.A));
        Assert.False (lv.NewKeyDownEvent (Key.T));

        // assert IsMatch never called
        matchNone.Verify (m => m.IsMatch (It.IsAny<string> (), It.IsAny<object> ()), Times.Never ());
    }

    [Fact]
    public void ListViewCollectionNavigatorMatcher_OverrideMatching ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        Mock<ICollectionNavigatorMatcher> matchNone = new ();

        matchNone.Setup (m => m.IsCompatibleKey (It.IsAny<Key> ()))
                 .Returns (true);

        // Match any string starting with b to "candle" (psych!)
        matchNone.Setup (m => m.IsMatch (It.IsAny<string> (), It.IsAny<object> ()))
                 .Returns ((string s, object key) => s.StartsWith ('B') && key?.ToString () == "candle");

        lv.KeystrokeNavigator.Matcher = matchNone.Object;

        // Keys are consumed during navigation
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (5, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.A));
        Assert.Equal (5, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.T));
        Assert.Equal (5, lv.SelectedItem);

        Assert.Equal ("candle", (string)lv.Source.ToList () [lv.SelectedItem!.Value]!);
    }

    #region ListView Tests (from ListViewTests.cs - parallelizable)

    [Fact]
    public void Constructors_Defaults ()
    {
        var lv = new ListView ();
        Assert.Null (lv.Source);
        Assert.True (lv.CanFocus);
        Assert.Null (lv.SelectedItem);
        Assert.False (lv.AllowsMultipleSelection);

        lv = new () { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);

        lv = new () { Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);

        lv = new ()
        {
            Y = 1, Width = 10, Height = 20, Source = new ListWrapper<string> (["One", "Two", "Three"])
        };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);
        Assert.Equal (new (0, 1, 10, 20), lv.Frame);

        lv = new () { Y = 1, Width = 10, Height = 20, Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);
        Assert.Equal (new (0, 1, 10, 20), lv.Frame);
    }

    private class NewListDataSource : IListDataSource
    {
#pragma warning disable CS0067
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore CS0067

        public int Count => 0;
        public int Length => 0;

        public bool SuspendCollectionChangedEvent
        {
            get => throw new NotImplementedException ();
            set => throw new NotImplementedException ();
        }

        public bool IsMarked (int item) { throw new NotImplementedException (); }

        public void Render (
            ListView container,
            bool selected,
            int item,
            int col,
            int line,
            int width,
            int viewportX = 0
        )
        {
            throw new NotImplementedException ();
        }

        public void SetMark (int item, bool value) { throw new NotImplementedException (); }
        public IList ToList () { return new List<string> { "One", "Two", "Three" }; }

        public void Dispose () { throw new NotImplementedException (); }
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];
        var lv = new ListView { Height = 2, AllowsMarking = true, Source = new ListWrapper<string> (source) };
        lv.BeginInit ();
        lv.EndInit ();
        Assert.Null (lv.SelectedItem);
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
        Assert.False (lv.Source.IsMarked (lv.SelectedItem!.Value));
        Assert.True (lv.NewKeyDownEvent (Key.Space));
        Assert.True (lv.Source.IsMarked (lv.SelectedItem!.Value));
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

        void OnAccepted (object? sender, CommandEventArgs e) { accepted = true; }
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
            e.Handled = true;
        }
    }

    [Fact]
    public void ListViewProcessKeyReturnValue_WithMultipleCommands ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three", "Four"]) };

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Null (lv.SelectedItem);

        // bind shift down to move down twice in control
        lv.KeyBindings.Add (Key.CursorDown.WithShift, Command.Down, Command.Down);

        Key ev = Key.CursorDown.WithShift;

        Assert.True (lv.NewKeyDownEvent (ev), "The first time we move down 2 it should be possible");

        // After moving down twice from null we should be at 'Two'
        Assert.Equal (1, lv.SelectedItem);

        // clear the items
        lv.SetSource<string> (null);

        // Press key combo again - return should be false this time as none of the Commands are allowable
        Assert.False (lv.NewKeyDownEvent (ev), "We cannot move down so will not respond to this");
    }

    [Fact]
    public void AllowsMarking_True_SpaceWithShift_SelectsThenDown_SingleSelection ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        lv.AllowsMarking = true;
        lv.AllowsMultipleSelection = false;

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Null (lv.SelectedItem);

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
        Assert.False (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.True (lv.Source.IsMarked (2)); // but can toggle marked

        // Press key combo again 
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2)); // untoggle toggle marked
    }

    [Fact]
    public void AllowsMarking_True_SpaceWithShift_SelectsThenDown_MultipleSelection ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        lv.AllowsMarking = true;
        lv.AllowsMultipleSelection = true;

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Null (lv.SelectedItem);

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
        ListWrapper<string> lw = new (["One", "Two", "Three"]);

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
    public void SelectedItem_Get_Set ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.Null (lv.SelectedItem);
        Assert.Throws<ArgumentException> (() => lv.SelectedItem = 3);
        Exception exception = Record.Exception (() => lv.SelectedItem = null);
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

        for (var i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (0, removed);

        added = 0;

        for (var i = 0; i < 3; i++)
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

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
            source2.Add ($"Item{i}");
            source3.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        added = 0;

        for (var i = 0; i < 3; i++)
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

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
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

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
            source2.Add ($"Item{i}");
            source3.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        added = 0;

        for (var i = 0; i < 3; i++)
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

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
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

        for (var i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (3, lw.Count);
        Assert.Equal (3, source.Count);

        lw.SuspendCollectionChangedEvent = false;

        for (var i = 3; i < 6; i++)
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
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.CollectionChanged += Lw_CollectionChanged;

        lv.SuspendCollectionChangedEvent ();

        for (var i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (3, lv.Source.Count);
        Assert.Equal (3, source.Count);

        lv.ResumeSuspendCollectionChangedEvent ();

        for (var i = 3; i < 6; i++)
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

    #endregion
}
