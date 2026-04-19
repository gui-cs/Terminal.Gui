namespace Terminal.Gui.Views;

/// <summary>
///     Interface for all non-generic members of <see cref="TreeView{T}"/>.
///     <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
public interface ITreeView
{
    /// <summary>Gets or sets whether the user can select multiple tree nodes at once.</summary>
    bool MultiSelect { get; set; }

    /// <summary>Gets or sets whether the user can navigate the tree using letter keys.</summary>
    bool AllowLetterBasedNavigation { get; set; }

    /// <summary>Gets or sets the maximum depth to which the tree will expand.</summary>
    int MaxDepth { get; set; }

    /// <summary>Contains options for changing how the tree is rendered.</summary>
    TreeStyle Style { get; set; }

    /// <summary>Removes all objects from the tree and clears selection.</summary>
    void ClearObjects ();

    /// <summary>Fully expands all nodes in the tree.</summary>
    void ExpandAll ();

    /// <summary>Collapses all root nodes in the tree.</summary>
    void CollapseAll ();

    /// <summary>Rebuilds the tree structure from the data source.</summary>
    void RebuildTree ();

    /// <summary>Sets a flag indicating this view needs to be drawn because its state has changed.</summary>
    void SetNeedsDraw ();
}
