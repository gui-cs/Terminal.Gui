namespace Terminal.Gui;

/// <summary>
/// Implementation of <see cref="CheckBoxTableSourceWrapperBase"/> which records toggled rows
/// by a property on row objects.
/// </summary>
public class CheckBoxTableSourceWrapperByObject<T> : CheckBoxTableSourceWrapperBase {
    private readonly IEnumerableTableSource<T> _toWrap;
    readonly Func<T, bool> _getter;
    readonly Action<T, bool> _setter;

    /// <summary>
    /// Creates a new instance of the class wrapping the collection <paramref name="toWrap"/>.
    /// </summary>
    /// <param name="tableView">The table you will use the source with.</param>
    /// <param name="toWrap">The collection of objects you will record checked state for</param>
    /// <param name="getter">Delegate method for retrieving checked state from your objects of type <typeparamref name="T"/>.</param>
    /// <param name="setter">Delegate method for setting new checked states on your objects of type <typeparamref name="T"/>.</param>
    public CheckBoxTableSourceWrapperByObject (
        TableView tableView,
        IEnumerableTableSource<T> toWrap,
        Func<T, bool> getter,
        Action<T, bool> setter
    ) : base (tableView, toWrap) {
        this._toWrap = toWrap;
        this._getter = getter;
        this._setter = setter;
    }

    /// <inheritdoc/>
    protected override bool IsChecked (int row) { return _getter (_toWrap.GetObjectOnRow (row)); }

    /// <inheritdoc/>
    protected override void ToggleAllRows () { ToggleRows (Enumerable.Range (0, _toWrap.Rows).ToArray ()); }

    /// <inheritdoc/>
    protected override void ToggleRow (int row) {
        var d = _toWrap.GetObjectOnRow (row);
        _setter (d, !_getter (d));
    }

    /// <inheritdoc/>
    protected override void ToggleRows (int[] range) {
        // if all are ticked untick them
        if (range.All (IsChecked)) {
            // select none
            foreach (var r in range) {
                _setter (_toWrap.GetObjectOnRow (r), false);
            }
        } else {
            // otherwise tick all
            foreach (var r in range) {
                _setter (_toWrap.GetObjectOnRow (r), true);
            }
        }
    }

    /// <inheritdoc/>
    protected override void ClearAllToggles () {
        foreach (var e in _toWrap.GetAllObjects ()) {
            _setter (e, false);
        }
    }
}
