namespace Terminal.Gui.Views;

/// <summary>Abstract implementation of <see cref="ITreeBuilder{T}"/>.</summary>
public abstract class TreeBuilder<T> : ITreeBuilder<T>
{
    /// <summary>Constructs base and initializes <see cref="SupportsCanExpand"/></summary>
    /// <param name="supportsCanExpand">Pass true if you intend to implement <see cref="CanExpand(T)"/> otherwise false</param>
    public TreeBuilder (bool supportsCanExpand) { SupportsCanExpand = supportsCanExpand; }

    /// <inheritdoc/>
    public bool SupportsCanExpand { get; protected set; }

    /// <summary>
    ///     Override this method to return a rapid answer as to whether <see cref="GetChildren(T)"/> returns results.  If
    ///     you are implementing this method ensure you passed true in base constructor or set <see cref="SupportsCanExpand"/>
    /// </summary>
    /// <param name="toExpand"></param>
    /// <returns></returns>
    public virtual bool CanExpand (T toExpand) { return GetChildren (toExpand).Any (); }

    /// <inheritdoc/>
    public abstract IEnumerable<T> GetChildren (T forObject);
}
