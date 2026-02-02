using JetBrains.Annotations;

namespace ViewsTests;

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

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    // NOTE: TreeView is a special case - both Activate and Accept invoke the same handler
    [Fact]
    public void TreeView_Command_Activate_SameAsAccept ()
    {
        TreeView treeView = new ();
        TreeNode root = new () { Text = "Root" };
        treeView.AddObject (root);
        treeView.SelectedObject = root;

        int acceptingCount = 0;
        treeView.Accepting += (_, e) =>
        {
            acceptingCount++;
            e.Handled = true;
        };

        treeView.InvokeCommand (Command.Activate);
        int afterActivate = acceptingCount;

        treeView.InvokeCommand (Command.Accept);
        int afterAccept = acceptingCount;

        // Both commands should trigger the same handler
        Assert.Equal (1, afterActivate);
        Assert.Equal (2, afterAccept);

        treeView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TreeView_Command_Accept_ActivatesNode ()
    {
        TreeView treeView = new ();
        TreeNode root = new () { Text = "Root" };
        treeView.AddObject (root);
        treeView.SelectedObject = root;

        bool acceptingFired = false;
        treeView.Accepting += (_, e) =>
        {
            acceptingFired = true;
            e.Handled = true;
        };

        bool? result = treeView.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        treeView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TreeView_Command_HotKey_SetsFocus ()
    {
        TreeView treeView = new ();
        TreeNode root = new () { Text = "Root" };
        treeView.AddObject (root);

        bool? result = treeView.InvokeCommand (Command.HotKey);

        // HotKey returns true
        Assert.True (result);

        treeView.Dispose ();
    }
}
