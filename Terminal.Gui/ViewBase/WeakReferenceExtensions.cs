namespace Terminal.Gui.ViewBase;

/// <summary>
///     Extension methods for <see cref="WeakReference{T}"/> when T is <see cref="View"/>.
/// </summary>
public static class WeakReferenceExtensions
{
    /// <summary>
    ///     Returns a formatted string representation of the <see cref="WeakReference{T}"/> to a View.
    /// </summary>
    /// <param name="weakRef">The weak reference to format.</param>
    /// <returns>A string identifying the referenced View, or "(null)" if the reference is null or dead.</returns>
    public static string ToIdentifyingString (this WeakReference<View>? weakRef)
    {
        if (weakRef is null || !weakRef.TryGetTarget (out View? view))
        {
            return "(null)";
        }

        return view.ToIdentifyingString ();
    }
}
