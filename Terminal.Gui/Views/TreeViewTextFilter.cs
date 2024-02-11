namespace Terminal.Gui;

/// <summary>
///     <see cref="ITreeViewFilter{T}"/> implementation which searches the <see cref="TreeView{T}.AspectGetter"/> of the model for the given
///     <see cref="Text"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class TreeViewTextFilter<T> : ITreeViewFilter<T> where T : class
{
    /// <summary>
    ///     Creates a new instance of the filter for use with <paramref name="forTree"/>. Set <see cref="Text"/> to begin filtering.
    /// </summary>
    /// <param name="forTree"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public TreeViewTextFilter (TreeView<T> forTree) { _forTree = forTree ?? throw new ArgumentNullException (nameof (forTree)); }

    private readonly TreeView<T> _forTree;
    private string text;

    /// <summary>The case sensitivity of the search match. Defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</summary>
    public StringComparison Comparer { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>The text that will be searched for in the <see cref="TreeView{T}"/></summary>
    public string Text
    {
        get => text;
        set
        {
            text = value;
            RefreshTreeView ();
        }
    }

    /// <summary>
    ///     Returns <typeparamref name="T"/> if there is no <see cref="Text"/> or the text matches the
    ///     <see cref="TreeView{T}.AspectGetter"/> of the <paramref name="model"/>.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public bool IsMatch (T model)
    {
        if (string.IsNullOrWhiteSpace (Text))
        {
            return true;
        }

        return _forTree.AspectGetter (model)?.IndexOf (Text, Comparer) != -1;
    }

    private void RefreshTreeView ()
    {
        _forTree.InvalidateLineMap ();
        _forTree.SetNeedsDisplay ();
    }
}
