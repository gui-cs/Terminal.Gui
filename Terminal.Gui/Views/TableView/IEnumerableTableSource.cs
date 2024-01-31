namespace Terminal.Gui {
    /// <summary>
    /// Interface for all <see cref="ITableSource"/> which present
    /// an object per row (of type <typeparamref name="T"/>).
    /// </summary>
    public interface IEnumerableTableSource<T> : ITableSource {
        /// <summary>
        /// Return the object on the given row.
        /// </summary>
        T GetObjectOnRow (int row);

        /// <summary>
        /// Return all objects in the table.
        /// </summary>
        IEnumerable<T> GetAllObjects ();
    }
}
