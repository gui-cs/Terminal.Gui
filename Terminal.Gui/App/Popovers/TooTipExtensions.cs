namespace Terminal.Gui.App;

/// <summary>
/// Provides helper extension methods to attach tooltips to <see cref="View"/> instances.
/// </summary>
/// <remarks>
/// These methods delegate to the <see cref="TooltipManager"/> associated with a <see cref="IApplication"/>.
/// They allow attaching tooltip content without adding state directly to <see cref="View"/>.
/// </remarks>
public static class TooltipExtensions
{
    /// <summary>
    /// Associates a tooltip content factory with the specified view.
    /// </summary>
    /// <param name="view">The target view.</param>
    /// <param name="contentFactory">
    /// A factory that creates the tooltip content each time it is displayed.
    /// </param>
    /// <remarks>
    /// The tooltip will be shown when the mouse enters the view and hidden when it leaves.
    /// </remarks>
    public static void SetTooltipContent (this View view, Func<View> contentFactory)
    {
        ArgumentNullException.ThrowIfNull (view);
        ArgumentNullException.ThrowIfNull (contentFactory);

        TooltipManager.Instance.SetTooltipContent (view, contentFactory);
    }

    /// <summary>
    /// Associates a text-based tooltip with the specified view.
    /// </summary>
    /// <param name="view">The target view.</param>
    /// <param name="textFactory">
    /// A factory that provides the tooltip text dynamically.
    /// </param>
    /// <remarks>
    /// This is a convenience method that creates a <see cref="Label"/> internally.
    /// </remarks>
    public static void SetTooltipText (this View view, Func<string> textFactory)
    {
        ArgumentNullException.ThrowIfNull (view);
        ArgumentNullException.ThrowIfNull (textFactory);

        TooltipManager.Instance.SetTooltipText (view, textFactory);
    }

    /// <summary>
    /// Removes any tooltip associated with the specified view.
    /// </summary>
    /// <param name="view">The target view.</param>
    public static void RemoveTooltip (this View view)
    {
        ArgumentNullException.ThrowIfNull (view);

        TooltipManager.Instance.RemoveTooltipContent (view);
    }
}