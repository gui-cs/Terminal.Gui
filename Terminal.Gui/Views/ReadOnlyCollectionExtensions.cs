namespace Terminal.Gui;

internal static class ReadOnlyCollectionExtensions
{
    public static int IndexOf<T> (this IReadOnlyCollection<T> self, Func<T, bool> predicate)
    {
        var i = 0;

        foreach (T element in self)
        {
            if (predicate (element))
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    public static int IndexOf<T> (this IReadOnlyCollection<T> self, T toFind)
    {
        var i = 0;

        foreach (T element in self)
        {
            if (Equals (element, toFind))
            {
                return i;
            }

            i++;
        }

        return -1;
    }
}
