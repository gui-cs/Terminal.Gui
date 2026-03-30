using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     Manages tooltip behavior for a given <see cref="Window"/>.
/// </summary>
/// <remarks>
///     <para>
///         This manager provides a shared tooltip instance and associates tooltip content with multiple views.
///         It centralizes hover handling (MouseEnter / MouseLeave) and ensures that only one tooltip is visible at a time.
///     </para>
///     <para>
///         ToolTip content is defined per <see cref="View"/> using a factory (<see cref="Func{View}"/>),
///         allowing dynamic content creation on each display.
///     </para>
///     <para>
///         The manager avoids adding state directly to <see cref="View"/> by maintaining an external registry.
///     </para>
/// </remarks>
public sealed class ToolTipManager : IDisposable
{
    /// <summary>
    ///     Gets the singleton instance of the ToolTipManager class.
    /// </summary>
    /// <remarks>
    ///     Use this property to access the global ToolTipManager for managing tooltips throughout the
    ///     application. This instance is not thread-safe for the moment and intended to be used as a shared resource.
    /// </remarks>
    public static ToolTipManager Instance { get; } = new ();

    // Stores tooltip registrations for each target view
    private readonly Dictionary<View, ToolTipRegistration> _registrations = new ();

    // Shared tooltip instance reused across all views
    private ToolTipHost<View>? _sharedToolTip;

    // Currently active target view (if any)
    private View? _currentTarget;

    private ToolTipManager () { }

    /// <summary>
    ///     Registers a tooltip provider for the specified view, enabling tooltips to be displayed when the user hovers over
    ///     the view.
    /// </summary>
    /// <remarks>
    ///     If a tooltip provider is already registered for the specified view, it will be replaced by
    ///     the new provider. ToolTips are shown when the mouse enters the view and hidden when the mouse leaves. To remove
    ///     a tooltip, use the appropriate removal method.
    /// </remarks>
    /// <param name="target">The view for which the tooltip should be displayed. Cannot be null.</param>
    /// <param name="provider">The provider that supplies tooltip content for the target view. Cannot be null.</param>
    public void SetToolTip (View target, ToolTipProvider provider)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (provider);

        // Ensure previous registration is removed to avoid duplicate subscriptions
        RemoveToolTip (target);

        // Subscribe to hover events
        target.MouseEnter += OnMouseEnter;
        target.MouseLeave += OnMouseLeave;
        target.Disposing += OnDisposing;

        // Store registration for later removal
        _registrations [target] = new ToolTipRegistration (OnMouseEnter, OnMouseLeave, OnDisposing);

        return;

        void OnMouseLeave (object? sender, EventArgs e)
        {
            // Only hide if this target is still the active one
            if (_currentTarget == target)
            {
                Hide ();
            }
        }

        void OnMouseEnter (object? sender, CancelEventArgs e) => ShowFor (target, provider);
    }

    private void OnDisposing (object? sender, EventArgs e)
    {
        if (sender is View target)
        {
            target.RemoveToolTip ();
        }
    }

    /// <summary>
    ///     Removes the tooltip associated with a target view.
    /// </summary>
    /// <param name="target">The target view.</param>
    /// <remarks>
    ///     This unsubscribes from events and removes any stored tooltip content.
    /// </remarks>
    public void RemoveToolTip (View target)
    {
        ArgumentNullException.ThrowIfNull (target);

        if (_registrations.TryGetValue (target, out ToolTipRegistration? registration))
        {
            // Unsubscribe from events
            target.MouseEnter -= registration.MouseEnter;
            target.MouseLeave -= registration.MouseLeave;
            target.Disposing -= registration.Disposing;

            _registrations.Remove (target);
        }

        // If the removed target is currently displayed, hide the tooltip
        if (_currentTarget == target)
        {
            Hide ();
        }
    }

    /// <summary>
    ///     Displays a tooltip for the specified target view using the provided tooltip content provider.
    /// </summary>
    /// <remarks>
    ///     If a tooltip is already visible for another view, it will be updated to display content for
    ///     the new target. The tooltip is anchored relative to the target view's position on the screen.
    /// </remarks>
    /// <param name="target">The view for which the tooltip will be shown. Cannot be null.</param>
    /// <param name="provider">The provider that supplies the tooltip content for the target view. Cannot be null.</param>
    public void ShowFor (View target, ToolTipProvider provider)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (provider);

        // Create the shared tooltip if needed
        _sharedToolTip ??= new ToolTipHost<View> ();

        // Ensure the tooltip is attached to the correct application context
        _sharedToolTip.App ??= target.App;

        // Anchor tooltip relative to the target view
        _sharedToolTip.Anchor = () => target.FrameToScreen ();

        // Update content dynamically
        _sharedToolTip.SetContent (provider.GetContent);

        _currentTarget = target;

        // Display tooltip
        _sharedToolTip.MakeVisible ();
    }

    /// <summary>
    ///     Hides the currently visible tooltip.
    /// </summary>
    public void Hide ()
    {
        _sharedToolTip?.Visible = false;

        _currentTarget = null;
    }

    /// <summary>
    ///     Releases all resources used by the manager.
    /// </summary>
    /// <remarks>
    ///     This unsubscribes all event handlers and disposes the shared tooltip.
    /// </remarks>
    public void Dispose ()
    {
        foreach ((View target, ToolTipRegistration registration) in _registrations)
        {
            target.MouseEnter -= registration.MouseEnter;
            target.MouseLeave -= registration.MouseLeave;
            target.Disposing -= registration.Disposing;
        }

        _registrations.Clear ();

        _sharedToolTip?.Dispose ();
        _sharedToolTip = null;

        _currentTarget = null;
    }

    /// <summary>
    ///     Stores event handlers and content factory for a registered view.
    /// </summary>
    private sealed record ToolTipRegistration (EventHandler<CancelEventArgs> MouseEnter,
                                               EventHandler MouseLeave,
                                               EventHandler Disposing);
}
