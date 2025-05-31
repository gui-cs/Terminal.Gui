using System.Collections;

namespace Terminal.Gui.Views;

/// <inheritdoc cref="CollectionNavigatorBase"/>
/// <remarks>This implementation is based on a static <see cref="Collection"/> of objects.</remarks>
internal class CollectionNavigator : CollectionNavigatorBase, IListCollectionNavigator
{
    /// <summary>Constructs a new CollectionNavigator.</summary>
    public CollectionNavigator () { }

    /// <summary>Constructs a new CollectionNavigator for the given collection.</summary>
    /// <param name="collection"></param>
    public CollectionNavigator (IList collection) { Collection = collection; }

    /// <inheritdoc/>
    public IList Collection { get; set; }

    /// <inheritdoc/>
    protected override object ElementAt (int idx) { return Collection [idx]; }

    /// <inheritdoc/>
    protected override int GetCollectionLength () { return Collection.Count; }
}