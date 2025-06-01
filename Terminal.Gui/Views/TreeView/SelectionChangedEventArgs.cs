namespace Terminal.Gui.Views;

/// <summary>Event arguments describing a change in selected object in a tree view</summary>
public class SelectionChangedEventArgs<T> : EventArgs where T : class
{
    /// <summary>Creates a new instance of event args describing a change of selection in <paramref name="tree"/></summary>
    /// <param name="tree"></param>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    public SelectionChangedEventArgs (TreeView<T> tree, T oldValue, T newValue)
    {
        Tree = tree;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>The newly selected value in the <see cref="Tree"/> (can be null)</summary>
    public T NewValue { get; }

    /// <summary>The previously selected value (can be null)</summary>
    public T OldValue { get; }

    /// <summary>The view in which the change occurred</summary>
    public TreeView<T> Tree { get; }
}
