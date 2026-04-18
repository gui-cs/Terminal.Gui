// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

namespace Terminal.Gui.Views;

/// <summary>
///     Convenience implementation of generic <see cref="TreeView{T}"/> for any tree were all nodes implement
///     <see cref="ITreeNode"/>. <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
public class TreeView : TreeView<ITreeNode>, IDesignable
{
    /// <summary>
    ///     Creates a new instance of the tree control with absolute positioning and initialises
    ///     <see cref="TreeBuilder{T}"/> with default <see cref="ITreeNode"/> based builder.
    /// </summary>
    public TreeView ()
    {
        CanFocus = true;

        TreeBuilder = new TreeNodeBuilder ();
        AspectGetter = o => o is null ? "Null" : o.Text ?? o.ToString () ?? "Unnamed Node";
    }

    bool IDesignable.EnableForDesign ()
    {
        var root1 = new TreeNode ("Root1");
        root1.Children.Add (new TreeNode ("Child1.1"));
        root1.Children.Add (new TreeNode ("Child1.2"));

        var root2 = new TreeNode ("Root2");
        root2.Children.Add (new TreeNode ("Child2.1"));
        root2.Children.Add (new TreeNode ("Child2.2"));

        AddObject (root1);
        AddObject (root2);

        ExpandAll ();

        return true;
    }
}