namespace Terminal.Gui.Views;

/// <summary><see cref="ITableSource"/> implementation that wraps arbitrary data.</summary>
/// <typeparam name="T"></typeparam>
public class EnumerableTableSource<T> : IEnumerableTableSource<T>
{
    private readonly T [] data;
    private readonly Dictionary<string, Func<T, object>> lambdas;

    /// <summary>Creates a new instance of the class that presents <paramref name="data"/> collection as a table.</summary>
    /// <remarks>
    ///     The elements of the <paramref name="data"/> collection are recorded during construction (immutable) but the
    ///     properties of those objects are permitted to change.
    /// </remarks>
    /// <param name="data">
    ///     The data that you want to present.  The members of this collection will be frozen after
    ///     construction.
    /// </param>
    /// <param name="columnDefinitions">
    ///     Getter methods for each property you want to present in the table. For example:
    ///     <code>
    ///  new () {
    ///     { "Colname1", (t)=>t.SomeField},
    ///     { "Colname2", (t)=>t.SomeOtherField}
    /// }
    ///  </code>
    /// </param>
    public EnumerableTableSource (IEnumerable<T> data, Dictionary<string, Func<T, object>> columnDefinitions)
    {
        this.data = data.ToArray ();
        ColumnNames = columnDefinitions.Keys.ToArray ();
        lambdas = columnDefinitions;
    }

    /// <summary>Gets the object collection hosted by this wrapper.</summary>
    public IReadOnlyCollection<T> Data => data.AsReadOnly ();

    /// <inheritdoc/>
    public object this [int row, int col] => lambdas [ColumnNames [col]] (data [row]);

    /// <inheritdoc/>
    public int Rows => data.Length;

    /// <inheritdoc/>
    public int Columns => ColumnNames.Length;

    /// <inheritdoc/>
    public string [] ColumnNames { get; }

    /// <inheritdoc/>
    public IEnumerable<T> GetAllObjects () { return Data; }

    /// <inheritdoc/>
    public T GetObjectOnRow (int row) { return Data.ElementAt (row); }
}
