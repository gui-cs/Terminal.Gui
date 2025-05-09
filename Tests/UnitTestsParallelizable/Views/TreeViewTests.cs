using JetBrains.Annotations;

namespace Terminal.Gui.ViewsTests;

[TestSubject (typeof (TreeView))]
public class TreeViewTests
{

    [Fact]
    public void TreeView_CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        var tree = new TreeView ();
        tree.AddObjects ([
                             new TreeNode(){ Text="apricot" },
                             new TreeNode(){ Text="arm" },
                             new TreeNode(){ Text="bat" },
                             new TreeNode(){ Text="batman" },
                             new TreeNode(){ Text="bates hotel" },
                             new TreeNode(){ Text="candle" },
                         ]);

        tree.SetFocus ();

        tree.KeyBindings.Add (Key.B, Command.Down);

        Assert.Equal ("apricot", tree.SelectedObject.Text);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (tree.NewKeyDownEvent (Key.B));
        Assert.NotNull (tree.SelectedObject);
        Assert.Equal ("arm", tree.SelectedObject.Text);

        Assert.True (tree.NewKeyDownEvent (Key.B));
        Assert.Equal ("bat", tree.SelectedObject.Text);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (tree.NewKeyDownEvent (Key.C));
        Assert.Equal ("candle", tree.SelectedObject.Text);
    }
}
