namespace Terminal.Gui;

/// <summary>
///     Extension methods for <see cref="WeakReference{T}"/> where T is <see cref="View"/>.
/// </summary>
public static class WeakReferenceExtensions
{
    /// <summary>
    ///     Returns a string that identifies the view referenced by the weak reference for debugging and logging purposes.
    /// </summary>
    /// <param name="weakReference">The weak reference to a view.</param>
    /// <returns>
    ///     A formatted string: Id (if set) → Title (if set) → Text (if set) → Type name.
    ///     Returns "null" if the weak reference is null.
    ///     Returns "[dead reference]" if the target has been garbage collected.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is primarily used in logging and debugging scenarios to provide
    ///         consistent view identification when working with weak references in command contexts.
    ///     </para>
    ///     <para>
    ///         Examples:
    ///         <list type="bullet">
    ///             <item>"Button (Id: submitBtn)"</item>
    ///             <item>"Window (Title: Settings)"</item>
    ///             <item>"[dead reference]"</item>
    ///             <item>"null"</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static string ToIdentifyingString (this WeakReference<View>? weakReference)
    {
        if (weakReference is null)
        {
            return "null";
        }

        if (weakReference.TryGetTarget (out View? view))
        {
            return view.ToIdentifyingString ();
        }

        return "[dead reference]";
    }
}
