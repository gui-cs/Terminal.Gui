namespace Terminal.Gui.ViewBase;

internal static class ViewCollectionHelpers
{
    /// <summary>Returns a defensive copy of any <see cref="IEnumerable{T}"/>.</summary>
    internal static View [] Snapshot (this IEnumerable<View> source)
    {
        if (source is IList<View> list)
        {
            // The list parameter might be the live `_subviews`, so freeze it under a lock
            lock (list)
            {
                return [.. list]; // C# 12 slice copy (= new List<View>(list).ToArray())
            }
        }

        // Anything else (LINQ result, iterator block, etc.) we just enumerate.
        return source.ToArray (); // Safe because it’s not shared mutable state
    }
}
