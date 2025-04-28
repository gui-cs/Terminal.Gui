using System.Collections.ObjectModel;
using Moq;

namespace Terminal.Gui.ViewsTests;

public class ListViewTests
{
    [Fact]
    public void ListViewCollectionNavigatorMatcher_DefaultBehaviour ()
    {
        ObservableCollection<string> source = new () { "apricot", "arm", "bat", "batman", "bates hotel", "candle" };
        ListView lv = new ListView { Source = new ListWrapper<string> (source) };

        // Keys are consumed during navigation
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.True (lv.NewKeyDownEvent (Key.A));
        Assert.True (lv.NewKeyDownEvent (Key.T));

        Assert.Equal ("bat", (string)lv.Source.ToList () [lv.SelectedItem]);
    }

    [Fact]
    public void ListViewCollectionNavigatorMatcher_IgnoreKeys ()
    {
        ObservableCollection<string> source = new () { "apricot", "arm", "bat", "batman", "bates hotel", "candle" };
        ListView lv = new ListView { Source = new ListWrapper<string> (source) };


        var matchNone = new Mock<ICollectionNavigatorMatcher> ();

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
        ObservableCollection<string> source = new () { "apricot", "arm", "bat", "batman", "bates hotel", "candle" };
        ListView lv = new ListView { Source = new ListWrapper<string> (source) };


        var matchNone = new Mock<ICollectionNavigatorMatcher> ();

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

        Assert.Equal ("candle", (string)lv.Source.ToList () [lv.SelectedItem]);
    }

    [Fact]
    public void ListView_CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        ObservableCollection<string> source = new () { "apricot", "arm", "bat", "batman", "bates hotel", "candle" };
        ListView lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        lv.KeyBindings.Add (Key.B, Command.Down);

        Assert.Equal (-1, lv.SelectedItem);

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
    public void CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        ObservableCollection<string> source = new () { "apricot", "arm", "bat", "batman", "bates hotel", "candle" };
        ListView lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        lv.KeyBindings.Add (Key.B, Command.Down);

        Assert.Equal (-1, lv.SelectedItem);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (0, lv.SelectedItem);

        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (1, lv.SelectedItem);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (lv.NewKeyDownEvent (Key.C));
        Assert.Equal (5, lv.SelectedItem);
    }
}
