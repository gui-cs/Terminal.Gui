using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     A generic tooltip that hosts a view and displays it on hover.
/// </summary>
/// <typeparam name="TView">
///     The type of view being hosted. Must derive from <see cref="View"/> and have a parameterless
///     constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         This class is inspired by <see cref="Popover{TView, TResult}"/> but simplified for tooltip scenarios
///         where no result extraction is needed.
///     </para>
///     <para>
///         <b>Registration:</b> ToolTips are automatically registered with <see cref="Application.Popovers"/> when
///         <see cref="MakeVisible"/> is called. Manual registration via <see cref="ApplicationPopover.Register"/>
///         is not required.
///     </para>
///     <para>
///         <b>Key Features:</b>
///     </para>
///     <list type="bullet">
///         <item>Generic content view hosting with automatic initialization</item>
///         <item>Anchor-based positioning via <see cref="IPopoverView.Anchor"/> function</item>
///         <item>Target view weak reference for command bubbling</item>
///         <item>Automatic command bridging from content view</item>
///     </list>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
///         // Create a tooltip with a Label
///         var label = new Label { Text = "This is a tooltip" };
///         var tooltip = new ToolTip&lt;Label&gt; { ContentView = label };
///         
///         Application.Popover?.Register (tooltip);
///         tooltip.MakeVisible ();
///     </code>
/// </remarks>
public class ToolTip<TView> : PopoverImpl, IDesignable where TView : View, new ()
{
    private CommandBridge? _contentCommandBridge;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolTip{TView}"/> class.
    /// </summary>
    public ToolTip () : this (null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolTip{TView}"/> class with the specified content view.
    /// </summary>
    /// <param name="contentView">
    ///     The view to host in the tooltip. If <see langword="null"/>, a new instance will be created.
    /// </param>
    public ToolTip (TView? contentView)
    {
        // Do this to support debugging traces where Title gets set
        // Unicode Character 'REPLACEMENT CHARACTER' (U+FFFF) is used to indicate an invalid HotKeySpecifier
        base.HotKeySpecifier = (Rune)'\xffff';

        Border.Settings &= ~BorderSettings.Title;

        base.Visible = false;

#if DEBUG
        if (string.IsNullOrEmpty (contentView?.Id))
        {
            contentView?.Id = $"tooltipContentView_{Id}";
        }
#endif

        ContentView = contentView;
    }

    /// <summary>
    ///     Gets or sets the content view hosted by this tooltip.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The content view is added as a subview and commands from it are bridged to this tooltip
    ///         via a <see cref="CommandBridge"/>.
    ///     </para>
    ///     <para>
    ///         When set, the previous content view (if any) is removed and disposed. Event subscriptions
    ///         are updated accordingly.
    ///     </para>
    /// </remarks>
    public TView? ContentView
    {
        get;
        set
        {
            // If both are null, and we want to create a new instance
            if (field is null && value is null)
            {
                // Create new instance
                value = new TView ();
            }
            else if (field == value)
            {
                return;
            }

#if DEBUG
            Id = $"{value?.Id}ToolTip";
#endif

            // Unsubscribe and remove old content view
            if (field is { })
            {
                field.VisibleChanged -= ContentViewOnVisibleChanged;
                Remove (field);
                field.Dispose ();
            }

            field = value ?? new TView ();

            field.App = App;
            Add (field);

            // When ContentView is hidden, hide the ToolTip too
            field.VisibleChanged += ContentViewOnVisibleChanged;

            // Bridge Activate from ContentView → ToolTip across the non-containment boundary.
            _contentCommandBridge?.Dispose ();
            _contentCommandBridge = CommandBridge.Connect (this, field, Command.Activate, Command.Accept);
        }
    }

    /// <summary>
    ///     Gets or sets the target <see cref="View"/> of this tooltip as a weak reference.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, the tooltip automatically subscribes to the target's <see cref="View.MouseEnter"/> and
    ///         <see cref="View.MouseLeave"/> events to show/hide itself on hover.
    ///     </para>
    ///     <para>
    ///         Commands that bubble from the tooltip will be bridged to this target view.
    ///     </para>
    /// </remarks>
    public new WeakReference<View>? Target
    {
        get => base.Target;
        set
        {
            // Unsubscribe from old target's mouse events
            if (base.Target?.TryGetTarget (out View? oldTarget) == true)
            {
                oldTarget.MouseEnter -= OnTargetMouseEnter;
                oldTarget.MouseLeave -= OnTargetMouseLeave;
            }

            base.Target = value;

            // Subscribe to new target's mouse events
            if (value?.TryGetTarget (out View? newTarget) == true)
            {
                newTarget.MouseEnter += OnTargetMouseEnter;
                newTarget.MouseLeave += OnTargetMouseLeave;
            }
        }
    }

    /// <summary>
    ///     Called when the target view's mouse enters. Shows the tooltip.
    /// </summary>
    private void OnTargetMouseEnter (object? sender, CancelEventArgs e)
    {
        MakeVisible ();
    }

    /// <summary>
    ///     Called when the target view's mouse leaves. Hides the tooltip.
    /// </summary>
    private void OnTargetMouseLeave (object? sender, EventArgs e)
    {
        Visible = false;
    }

    /// <summary>
    ///     Makes the tooltip visible and positions it based on <see cref="IPopoverView.Anchor"/> or
    ///     <paramref name="idealScreenPosition"/>.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position for the tooltip. If <see langword="null"/>, positioning is determined
    ///     by <see cref="IPopoverView.Anchor"/> or the current mouse position.
    /// </param>
    /// <param name="anchor">
    ///     Optional anchor rectangle to override <see cref="IPopoverView.Anchor"/> property for this call.
    ///     If <see langword="null"/>, uses the <see cref="IPopoverView.Anchor"/> property.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         If <see cref="View.App"/> is <see langword="null"/>, this method inherits it from
    ///         <see cref="PopoverImpl.Target"/>. If the tooltip is not yet registered with
    ///         <see cref="ApplicationPopover"/>, it is auto-registered before showing.
    ///     </para>
    ///     <para>
    ///         The actual position may be adjusted to ensure the tooltip fits fully on screen.
    ///     </para>
    /// </remarks>
    public override void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        if (Visible)
        {
            return;
        }

        // Inherit App from Target if not already set
        if (App is null && TryGetTarget (out View? targetView))
        {
            App = targetView.App;
        }

        // Ensure the ToolTip is sized correctly in case this is the first time we are being made visible
        Layout ();

        SetPosition (idealScreenPosition, anchor);

        if (App?.Popovers is not { } popovers)
        {
            return;
        }

        // Auto-register if not yet registered
        if (!popovers.IsRegistered (this))
        {
            popovers.Register (this);
        }

        popovers.Show (this);
    }

    /// <summary>
    ///     Sets the position of the tooltip based on <paramref name="idealScreenPosition"/> or <paramref name="anchor"/>.
    ///     The actual position may be adjusted to ensure full visibility on screen.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position. If <see langword="null"/>, uses <paramref name="anchor"/> or the current mouse
    ///     position.
    /// </param>
    /// <param name="anchor">
    ///     Optional anchor rectangle. If <see langword="null"/>, uses the <see cref="IPopoverView.Anchor"/> property.
    /// </param>
    /// <remarks>
    ///     This method only sets the position; it does not make the tooltip visible. Use <see cref="MakeVisible"/> to
    ///     both position and show the tooltip.
    /// </remarks>
    public void SetPosition (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        // Try anchor parameter, then Anchor property, then mouse position
        Rectangle? effectiveAnchor = anchor ?? Anchor?.Invoke ();

        if (effectiveAnchor is { })
        {
            idealScreenPosition = new Point (effectiveAnchor.Value.X, effectiveAnchor.Value.Y + effectiveAnchor.Value.Height);
        }

        idealScreenPosition ??= App?.Mouse.LastMousePosition;

        if (idealScreenPosition is null || ContentView is null)
        {
            return;
        }

        Point pos = idealScreenPosition.Value;

        if (!ContentView.IsInitialized)
        {
            ContentView.App ??= App;
            ContentView.BeginInit ();
            ContentView.EndInit ();
        }

        pos = GetAdjustedPosition (ContentView, pos);

        ContentView.X = pos.X;
        ContentView.Y = pos.Y;
    }

    /// <summary>
    ///     Calculates an adjusted screen-relative position for the content view to ensure full visibility.
    /// </summary>
    /// <param name="view">The view to position.</param>
    /// <param name="idealLocation">The ideal screen-relative location.</param>
    /// <returns>The adjusted screen-relative position that ensures maximum visibility.</returns>
    /// <remarks>
    ///     This method adjusts the position to keep the view fully visible on screen, considering screen boundaries.
    /// </remarks>
    protected virtual Point GetAdjustedPosition (View view, Point idealLocation)
    {
        int screenWidth = App?.Screen.Width ?? 0;
        int screenHeight = App?.Screen.Height ?? 0;

        int viewWidth = view.Frame.Width;
        int viewHeight = view.Frame.Height;

        // Clamp horizontally: prefer idealLocation.X, but shift left if it would overflow
        int nx = idealLocation.X;

        if (nx + viewWidth > screenWidth)
        {
            nx = Math.Max (screenWidth - viewWidth, 0);
        }

        nx = Math.Max (nx, 0);

        // Vertically: prefer below idealLocation
        int ny = idealLocation.Y;

        if (ny + viewHeight > screenHeight)
        {
            // Doesn't fit below — position so bottom is 1 row above the ideal Y
            ny = idealLocation.Y - viewHeight - 1;
        }

        ny = Math.Max (ny, 0);

        return new Point (nx, ny);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     When becoming visible, the content view is shown.
    ///     When becoming hidden, the content view is hidden.
    /// </remarks>
    protected override void OnVisibleChanged ()
    {
        if (Visible)
        {
            ContentView?.Visible = true;
        }
        else
        {
            ContentView?.Visible = false;
        }

        base.OnVisibleChanged (); // PopoverImpl handles Hide
    }

    /// <summary>
    ///     Called when the ContentView becomes invisible, to hide the ToolTip.
    /// </summary>
    private void ContentViewOnVisibleChanged (object? sender, EventArgs e)
    {
        if (sender is View { Visible: false } view && view == ContentView && Visible)
        {
            Visible = false;
        }
    }

    /// <summary>
    ///     Enables the tooltip for use in design-time scenarios.
    /// </summary>
    /// <typeparam name="TContext">The type of the target view context.</typeparam>
    /// <param name="targetView">The target view to associate with the tooltip.</param>
    /// <returns><see langword="true"/> if successfully enabled for design; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    ///     This method creates a default content view for design-time use.
    ///     Override to provide custom design-time behavior.
    /// </remarks>
    public virtual bool EnableForDesign<TContext> (ref TContext targetView) where TContext : notnull
    {
        ContentView ??= new TView ();

        if (ContentView is IDesignable designable)
        {
            return designable.EnableForDesign (ref targetView);
        }

        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     This method unsubscribes from content view events, target mouse events, and disposes the content view and command bridge.
    /// </remarks>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from target's mouse events
            if (Target?.TryGetTarget (out View? targetView) == true)
            {
                targetView.MouseEnter -= OnTargetMouseEnter;
                targetView.MouseLeave -= OnTargetMouseLeave;
            }

            ContentView?.VisibleChanged -= ContentViewOnVisibleChanged;
            ContentView?.Dispose ();
            ContentView = null;

            _contentCommandBridge?.Dispose ();
            _contentCommandBridge = null;
        }

        base.Dispose (disposing);
    }
}
