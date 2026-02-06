namespace Terminal.Gui.ViewBase;

/// <summary>
///     Extension methods for <see cref="View"/> to support debugging and logging.
/// </summary>
public static class ViewExtensions
{
    /// <summary>
    ///     Returns a formatted string that identifies the View for debugging/logging purposes.
    /// </summary>
    /// <param name="view">The view to identify.</param>
    /// <returns>A string identifying the View using Id, Title, Text, or type name.</returns>
    public static string ToIdentifyingString (this View? view)
    {
        if (view is null)
        {
            return "(null)";
        }

        if (!string.IsNullOrEmpty (view.Id))
        {
            return $"{view.Id}";
        }

        if (!string.IsNullOrEmpty (view.Title))
        {
            return $"(\"{view.Title}\")";
        }

        if (!string.IsNullOrEmpty (view.Text))
        {
            return $"(\"{view.Text}\")";
        }

        return view.GetType ().Name;
    }
}
