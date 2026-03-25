using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
/// Manages tooltip behavior for a given <see cref="Window"/>.
/// </summary>
/// <remarks>
/// <para>
/// This manager provides a shared tooltip instance and associates tooltip content with multiple views.
/// It centralizes hover handling (MouseEnter / MouseLeave) and ensures that only one tooltip is visible at a time.
/// </para>
/// <para>
/// Tooltip content is defined per <see cref="View"/> using a factory (<see cref="Func{View}"/>),
/// allowing dynamic content creation on each display.
/// </para>
/// <para>
/// The manager avoids adding state directly to <see cref="View"/> by maintaining an external registry.
/// </para>
/// </remarks>
public sealed class TooltipManager : IDisposable
{
    /// <summary>
    /// Gets the singleton instance of the TooltipManager class.
    /// </summary>
    /// <remarks>Use this property to access the global TooltipManager for managing tooltips throughout the
    /// application. This instance is thread-safe and intended to be used as a shared resource.</remarks>
    public static TooltipManager Instance { get; } = new TooltipManager ();

    // Stores tooltip registrations for each target view
    private readonly Dictionary<View, Registration> _registrations = new ();

    // Shared tooltip instance reused across all views
    private ToolTip<View>? _sharedTooltip;

    // Currently active target view (if any)
    private View? _currentTarget;

    private TooltipManager()
    {

    }

    /// <summary>
    /// Associates a tooltip content factory with a target view.
    /// </summary>
    /// <param name="target">The view that will trigger the tooltip.</param>
    /// <param name="contentFactory">
    /// A factory that creates the tooltip content each time it is shown.
    /// </param>
    /// <remarks>
    /// <para>
    /// The tooltip is displayed when the mouse enters the target view and hidden when it leaves.
    /// </para>
    /// <para>
    /// The content is recreated on each display to avoid state retention and parent conflicts.
    /// </para>
    /// </remarks>
    public void SetTooltipContent (View target, Func<View> contentFactory)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (contentFactory);

        // Ensure previous registration is removed to avoid duplicate subscriptions
        RemoveTooltipContent (target);

        void OnMouseEnter (object? sender, CancelEventArgs e)
        {
            ShowFor (target, contentFactory);
        }

        void OnMouseLeave (object? sender, EventArgs e)
        {
            // Only hide if this target is still the active one
            if (_currentTarget == target)
            {
                Hide ();
            }
        }

        // Subscribe to hover events
        target.MouseEnter += OnMouseEnter;
        target.MouseLeave += OnMouseLeave;


        // Store registration for later removal
        _registrations [target] = new Registration (contentFactory, OnMouseEnter, OnMouseLeave);
    }

    /// <summary>
    /// Associates a text-based tooltip with a target view.
    /// </summary>
    /// <param name="target">The target view.</param>
    /// <param name="textFactory">
    /// A factory that provides the tooltip text dynamically.
    /// </param>
    /// <remarks>
    /// This is a convenience wrapper that creates a <see cref="Label"/> internally.
    /// </remarks>
    public void SetTooltipText (View target, Func<string> textFactory)
    {
        ArgumentNullException.ThrowIfNull (textFactory);

        SetTooltipContent (
            target,
            () => new Label
            {
                Text = textFactory ()
            }
        );
    }

    /// <summary>
    /// Removes the tooltip associated with a target view.
    /// </summary>
    /// <param name="target">The target view.</param>
    /// <remarks>
    /// This unsubscribes from events and removes any stored tooltip content.
    /// </remarks>
    public void RemoveTooltipContent (View target)
    {
        ArgumentNullException.ThrowIfNull (target);

        if (_registrations.TryGetValue (target, out Registration? registration))
        {
            // Unsubscribe from events
            target.MouseEnter -= registration.MouseEnter;
            target.MouseLeave -= registration.MouseLeave;

            _registrations.Remove (target);
        }

        // If the removed target is currently displayed, hide the tooltip
        if (_currentTarget == target)
        {
            Hide ();
        }
    }

    /// <summary>
    /// Shows the tooltip for the specified target view.
    /// </summary>
    /// <param name="target">The target view.</param>
    /// <param name="contentFactory">The content factory.</param>
    /// <remarks>
    /// This method reuses a single shared tooltip instance and updates its content.
    /// </remarks>
    public void ShowFor (View target, Func<View> contentFactory)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (contentFactory);

        // Create the shared tooltip if needed
        _sharedTooltip ??= new ToolTip<View> ();

        // Ensure the tooltip is attached to the correct application context
        _sharedTooltip.App ??= target.App;

        // Anchor tooltip relative to the target view
        _sharedTooltip.Anchor = () => target.FrameToScreen ();

        // Update content dynamically
        _sharedTooltip.SetContent (contentFactory);

        _currentTarget = target;
        target.App?.Popovers?.Register (_sharedTooltip);

        // Display tooltip
        _sharedTooltip.MakeVisible ();
    }

    /// <summary>
    /// Hides the currently visible tooltip.
    /// </summary>
    public void Hide ()
    {
        _sharedTooltip?.Visible = false;

        _currentTarget = null;
    }

    /// <summary>
    /// Releases all resources used by the manager.
    /// </summary>
    /// <remarks>
    /// This unsubscribes all event handlers and disposes the shared tooltip.
    /// </remarks>
    public void Dispose ()
    {
        foreach ((View target, Registration registration) in _registrations)
        {
            target.MouseEnter -= registration.MouseEnter;
            target.MouseLeave -= registration.MouseLeave;
        }

        _registrations.Clear ();

        _sharedTooltip?.Dispose ();
        _sharedTooltip = null;

        _currentTarget = null;
    }

    /// <summary>
    /// Stores event handlers and content factory for a registered view.
    /// </summary>
    private sealed record Registration (
        Func<View> ContentFactory,
        EventHandler<CancelEventArgs> MouseEnter,
        EventHandler MouseLeave
    );
}