namespace Terminal.Gui;

/// <summary>
///     Extension methods for <see cref="View"/>.
/// </summary>
public static class ViewExtensions
{
    /// <summary>
    ///     Returns a string that identifies the view for debugging and logging purposes.
    /// </summary>
    /// <param name="view">The view to identify.</param>
    /// <returns>
    ///     A formatted string: Id (if set) → Title (if set) → Text (if set) → Type name.
    ///     Returns "null" if the view is null.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is primarily used in logging and debugging scenarios to provide
    ///         consistent view identification across the codebase.
    ///     </para>
    ///     <para>
    ///         Examples:
    ///         <list type="bullet">
    ///             <item>"Button (Id: submitBtn)"</item>
    ///             <item>"Window (Title: Settings)"</item>
    ///             <item>"Label (Text: Hello)"</item>
    ///             <item>"FrameView"</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static string ToIdentifyingString (this View? view)
    {
        if (view is null)
        {
            return "null";
        }

        string typeName = view.GetType ().Name;

        if (!string.IsNullOrEmpty (view.Id))
        {
            return $"{typeName} (Id: {view.Id})";
        }

        if (!string.IsNullOrEmpty (view.Title))
        {
            return $"{typeName} (Title: {view.Title})";
        }

        if (!string.IsNullOrEmpty (view.Text))
        {
            return $"{typeName} (Text: {view.Text})";
        }

        return typeName;
    }
}
