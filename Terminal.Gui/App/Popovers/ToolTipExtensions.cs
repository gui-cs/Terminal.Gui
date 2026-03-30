namespace Terminal.Gui.App;

/// <summary>
///     Provides extension methods for associating tooltips with views in a Terminal.Gui application.
/// </summary>
/// <remarks>
///     These extension methods enable developers to attach or remove tooltips from any View instance.
///     Tooltips can be specified as static text, dynamically generated text, or as a custom View for advanced scenarios.
///     Tooltips enhance user experience by providing contextual information when users interact with UI elements.
/// </remarks>
public static class ToolTipExtensions
{
    /// <param name="view">The view to which the tooltip will be attached. Cannot be null.</param>
    extension (View view)
    {
        /// <summary>
        ///     Associates a tooltip with the specified view, displaying the provided text when the user hovers over or focuses
        ///     on the view.
        /// </summary>
        /// <remarks>
        ///     If a tooltip is already set for the view, this method replaces it with the new text. Tooltips
        ///     provide additional context or guidance to users interacting with the UI element.
        /// </remarks>
        /// <param name="text">The text to display in the tooltip. If null or empty, no tooltip will be shown.</param>
        public void SetToolTip (string text)
        {
            ArgumentNullException.ThrowIfNull (view);
            TooltipManager.Instance.SetToolTip (view, new ToolTipProvider (text));
        }

        /// <summary>
        ///     Associates a dynamic tooltip with the specified view using a factory function to generate the tooltip text.
        /// </summary>
        /// <remarks>
        ///     Use this method to provide tooltips that can change dynamically based on application state or
        ///     user interaction. The tooltip text is evaluated at the time the tooltip is displayed, allowing for
        ///     context-sensitive information.
        /// </remarks>
        /// <param name="textFactory">
        ///     A function that returns the tooltip text to display. This function is invoked each time the tooltip is shown.
        ///     Cannot be null.
        /// </param>
        public void SetToolTip (Func<string> textFactory)
        {
            ArgumentNullException.ThrowIfNull (view);
            ArgumentNullException.ThrowIfNull (textFactory);
            TooltipManager.Instance.SetToolTip (view, new ToolTipProvider (textFactory));
        }

        /// <summary>
        ///     Associates a dynamic tooltip with the specified view using a factory function to generate the tooltip content.
        /// </summary>
        /// <remarks>
        ///     The tooltip content is generated each time the tooltip is shown by invoking the provided
        ///     factory function. This allows the tooltip to reflect dynamic or context-sensitive information.
        /// </remarks>
        /// <param name="contentFactory">A function that returns the content view to display as the tooltip. Cannot be null.</param>
        public void SetToolTip (Func<View> contentFactory)
        {
            ArgumentNullException.ThrowIfNull (view);
            ArgumentNullException.ThrowIfNull (contentFactory);
            TooltipManager.Instance.SetToolTip (view, new ToolTipProvider (contentFactory));
        }

        /// <summary>
        ///     Removes the tooltip associated with the specified view, if one exists.
        /// </summary>
        /// <remarks>
        ///     If the specified view does not have an associated tooltip, this method has no
        ///     effect.
        /// </remarks>
        public void RemoveToolTip ()
        {
            ArgumentNullException.ThrowIfNull (view);
            TooltipManager.Instance.RemoveToolTip (view);
        }
    }
}
