#nullable enable
using System.Collections;
using System.Collections.Specialized;

namespace Terminal.Gui.Views;

/// <summary>Implement <see cref="IListDataSource"/> to provide custom rendering for a <see cref="ListView"/>.</summary>
public interface IListDataSource : IDisposable
{
    /// <summary>
    /// Event to raise when an item is added, removed, or moved, or the entire list is refreshed.
    /// </summary>
    event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <summary>Returns the number of elements to display</summary>
    int Count { get; }

    /// <summary>Returns the maximum length of elements to display</summary>
    int Length { get; }

    /// <summary>
    /// Allow suspending the <see cref="CollectionChanged"/> event from being invoked,
    /// if <see langword="true"/>, otherwise is <see langword="false"/>.
    /// </summary>
    bool SuspendCollectionChangedEvent { get; set; }

    /// <summary>Should return whether the specified item is currently marked.</summary>
    /// <returns><see langword="true"/>, if marked, <see langword="false"/> otherwise.</returns>
    /// <param name="item">Item index.</param>
    bool IsMarked (int item);

    /// <summary>This method is invoked to render a specified item, the method should cover the entire provided width.</summary>
    /// <returns>The render.</returns>
    /// <param name="listView">The list view to render.</param>
    /// <param name="selected">Describes whether the item being rendered is currently selected by the user.</param>
    /// <param name="item">The index of the item to render, zero for the first item and so on.</param>
    /// <param name="col">The column where the rendering will start</param>
    /// <param name="line">The line where the rendering will be done.</param>
    /// <param name="width">The width that must be filled out.</param>
    /// <param name="start">The index of the string to be displayed.</param>
    /// <remarks>
    ///     The default color will be set before this method is invoked, and will be based on whether the item is selected
    ///     or not.
    /// </remarks>
    void Render (
        ListView listView,
        bool selected,
        int item,
        int col,
        int line,
        int width,
        int start = 0
    );

    /// <summary>Flags the item as marked.</summary>
    /// <param name="item">Item index.</param>
    /// <param name="value">If set to <see langword="true"/> value.</param>
    void SetMark (int item, bool value);

    /// <summary>Return the source as IList.</summary>
    /// <returns></returns>
    IList ToList ();
}
