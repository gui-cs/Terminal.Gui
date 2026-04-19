namespace Terminal.Gui.Views;

/// <summary>
///     Interface for all non-generic members of <see cref="TreeView{T}"/>.
///     <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
public interface ITreeView
{
    /// <summary>Contains options for changing how the tree is rendered.</summary>
    TreeStyle Style { get; set; }

    /// <summary>Removes all objects from the tree and clears selection.</summary>
    void ClearObjects ();

    /// <summary>Sets a flag indicating this view needs to be drawn because its state has changed.</summary>
    void SetNeedsDraw ();
}
