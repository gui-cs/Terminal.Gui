using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

/// <summary>
///     A generic popover that hosts a view and optionally extracts a typed result.
/// </summary>
/// <typeparam name="TView">The type of view being hosted. Must derive from <see cref="View"/> and have a parameterless constructor.</typeparam>
/// <typeparam name="TResult">
///     The type of result data extracted from the content view.
///     <para>
///         <strong>Important:</strong> Use nullable types (e.g., <c>MenuItem?</c>, <c>int?</c>, <c>string?</c>)
///         so that <see langword="null"/> can indicate no result or cancellation. Using non-nullable value types
///         (e.g., <c>int</c>) will return their default values, making it impossible to distinguish
///         no result from a valid result.
///     </para>
/// </typeparam>
/// <remarks>
///     <para>
///         This class extracts the generic popover-hosting logic from <see cref="PopoverMenu"/> to enable
///         reusable popover behavior for any view type.
///     </para>
///     <para>
///         <b>IMPORTANT:</b> Must be registered with <see cref="Application.Popover"/> via
///         <see cref="ApplicationPopover.Register"/> before calling <see cref="MakeVisible"/> or
///         <see cref="ApplicationPopover.Show"/>.
///     </para>
///     <para>
///         <b>Key Features:</b>
///     </para>
///     <list type="bullet">
///         <item>Generic content view hosting with automatic initialization</item>
///         <item>Result extraction via <see cref="ResultExtractor"/> or <see cref="IValue{TResult}"/></item>
///         <item>CWP-based <see cref="IsOpen"/> property synchronized with <see cref="View.Visible"/></item>
///         <item>Anchor-based positioning via <see cref="Anchor"/> function</item>
///         <item>Target view weak reference for command bubbling</item>
///         <item>Automatic command bridging from content view</item>
///     </list>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
///         // Create a popover with a ListView that returns the selected string
///         var listView = new ListView { Source = new ListWrapper&lt;string&gt; (["Option 1", "Option 2"]) };
///         var popover = new Popover&lt;ListView, string&gt; { ContentView = listView };
///         popover.ResultExtractor = lv => lv.Source?.ToList ()?.ElementAtOrDefault (lv.SelectedItem) as string;
///         
///         Application.Popover?.Register (popover);
///         popover.MakeVisible ();
///     </code>
/// </remarks>
public class Popover<TView, TResult> : PopoverBaseImpl, IDesignable
    where TView : View, new ()
{
    private CommandBridge? _contentCommandBridge;
    private TView? _contentView;
    private bool _isOpen;
    private TResult? _result;
    private CommandBridge? _targetCommandBridge;
    private WeakReference<View?>? _target;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Popover{TView, TResult}"/> class.
    /// </summary>
    public Popover () : this (null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Popover{TView, TResult}"/> class with the specified content view.
    /// </summary>
    /// <param name="contentView">
    ///     The view to host in the popover. If <see langword="null"/>, a new instance will be created.
    /// </param>
    public Popover (TView? contentView)
    {
        // Do this to support debugging traces where Title gets set
        // Unicode Character 'REPLACEMENT CHARACTER' (U+FFFF) is used to indicate an invalid HotKeySpecifier
        base.HotKeySpecifier = (Rune)'\xffff';

        Border?.Settings &= ~BorderSettings.Title;

        base.Visible = false;

        ContentView = contentView;
    }

    /// <summary>
    ///     Gets or sets the content view hosted by this popover.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The content view is added as a subview and commands from it are bridged to this popover
    ///         via a <see cref="CommandBridge"/>.
    ///     </para>
    ///     <para>
    ///         When set, the previous content view (if any) is removed and disposed. Event subscriptions
    ///         are updated accordingly.
    ///     </para>
    /// </remarks>
    public TView? ContentView
    {
        get => _contentView;
        set
        {
            // If both are null and we want to create a new instance
            if (_contentView is null && value is null)
            {
                // Create new instance
                value = new TView ();
            }
            else if (_contentView == value)
            {
                return;
            }

#if DEBUG
            Id = $"{value?.Id}Popover";
#endif

            // Unsubscribe and remove old content view
            if (_contentView is { })
            {
                _contentView.VisibleChanged -= ContentViewOnVisibleChanged;
                Remove (_contentView);
                _contentView.Dispose ();
            }

            _contentView = value ?? new TView ();

            _contentView.App = App;
            Add (_contentView);

            // When ContentView is hidden, hide the Popover too
            _contentView.VisibleChanged += ContentViewOnVisibleChanged;

            // Bridge Activate from ContentView → Popover across the non-containment boundary.
            _contentCommandBridge?.Dispose ();
            _contentCommandBridge = CommandBridge.Connect (this, _contentView, Command.Activate);
        }
    }

    /// <summary>
    ///     Gets or sets a weak reference to the target view that initiated this popover.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, creates a <see cref="CommandBridge"/> from the popover to the target view for
    ///         <see cref="Command.Activate"/>, and subscribes to the target's <see cref="View.HasFocusChanged"/>
    ///         to auto-close the popover when the target loses focus.
    ///     </para>
    ///     <para>
    ///         The weak reference ensures the popover doesn't prevent garbage collection of the target.
    ///     </para>
    ///     <para>
    ///         Typically set by the view that creates and shows the popover (e.g., a MenuBarItem or ComboBox).
    ///     </para>
    /// </remarks>
    public WeakReference<View?>? Target
    {
        get => _target;
        set
        {
            if (_target == value)
            {
                return;
            }

            // Unsubscribe from old target
            if (_target?.TryGetTarget (out View? oldTarget) == true && oldTarget is { })
            {
                Trace.Command (this, "TargetCleanup", $"Old={oldTarget.ToIdentifyingString ()}");
                oldTarget.HasFocusChanged -= OnTargetHasFocusChanged;
            }

            _targetCommandBridge?.Dispose ();
            _targetCommandBridge = null;

            _target = value;

            // Subscribe to new target
            if (_target?.TryGetTarget (out View? newTarget) == true && newTarget is { })
            {
                Trace.Command (this, "TargetSet", $"New={newTarget.ToIdentifyingString ()}");
                newTarget.HasFocusChanged += OnTargetHasFocusChanged;
                _targetCommandBridge = CommandBridge.Connect (newTarget, this, Command.Activate);
            }
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the popover is open (visible).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is synchronized with <see cref="View.Visible"/>. Setting <c>IsOpen = true</c>
    ///         calls <see cref="MakeVisible"/> to show the popover. Setting <c>IsOpen = false</c> sets
    ///         <see cref="View.Visible"/> to <see langword="false"/>.
    ///     </para>
    ///     <para>
    ///         The change can be cancelled via the <see cref="IsOpenChanging"/> event.
    ///     </para>
    /// </remarks>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (!CWPPropertyHelper.ChangeProperty (
                    this,
                    ref _isOpen,
                    value,
                    OnIsOpenChanging,
                    IsOpenChanging,
                    newValue =>
                    {
                        // Synchronize with Visible
                        if (newValue && !Visible)
                        {
                            MakeVisible ();
                        }
                        else if (!newValue && Visible)
                        {
                            Visible = false;
                        }
                    },
                    OnIsOpenChanged,
                    IsOpenChanged,
                    out _))
            {
                return;
            }
        }
    }

    /// <summary>
    ///     Raised when <see cref="IsOpen"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? IsOpenChanging;

    /// <summary>
    ///     Raised when <see cref="IsOpen"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? IsOpenChanged;

    /// <summary>
    ///     Gets or sets a function that computes the anchor rectangle for positioning the popover.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The anchor rectangle defines the area relative to which the popover should be positioned.
    ///         If <see langword="null"/>, the popover will be positioned at the mouse cursor or a default location.
    ///     </para>
    ///     <para>
    ///         The function is called by <see cref="MakeVisible"/> to determine the ideal position.
    ///         Returning <see langword="null"/> from the function uses default positioning logic.
    ///     </para>
    /// </remarks>
    public Func<Rectangle?>? Anchor { get; set; }

    /// <summary>
    ///     Gets or sets the function that extracts the result from the content view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This function is called when the popover is hidden to extract the final result.
    ///         If <see langword="null"/>, automatic extraction is attempted via <see cref="IValue{TResult}"/>.
    ///     </para>
    ///     <para>
    ///         The result is stored in <see cref="Result"/> and raises <see cref="ResultChanged"/>.
    ///     </para>
    /// </remarks>
    public Func<TView, TResult?>? ResultExtractor { get; set; }

    /// <summary>
    ///     Gets the result extracted from the content view when the popover was last closed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is set when the popover becomes hidden, either by extracting via
    ///         <see cref="ResultExtractor"/> or automatically via <see cref="IValue{TResult}"/>.
    ///     </para>
    ///     <para>
    ///         If no result extractor is provided and the content view doesn't implement
    ///         <see cref="IValue{TResult}"/>, this property remains <see langword="null"/>.
    ///     </para>
    /// </remarks>
    public TResult? Result
    {
        get => _result;
        protected set
        {
            if (EqualityComparer<TResult>.Default.Equals (_result, value))
            {
                return;
            }

            TResult? oldValue = _result;
            _result = value;
            ResultChanged?.Invoke (this, new ValueChangedEventArgs<TResult?> (oldValue, _result));
        }
    }

    /// <summary>
    ///     Raised when <see cref="Result"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<TResult?>>? ResultChanged;

    /// <summary>
    ///     Makes the popover visible and positions it based on <see cref="Anchor"/> or <paramref name="idealScreenPosition"/>.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position for the popover. If <see langword="null"/>, positioning is determined
    ///     by <see cref="Anchor"/> or the current mouse position.
    /// </param>
    /// <param name="anchor">
    ///     Optional anchor rectangle to override <see cref="Anchor"/> property for this call.
    ///     If <see langword="null"/>, uses the <see cref="Anchor"/> property.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         If the popover has not been registered with <see cref="Application.Popover"/>, this method
    ///         will auto-register it before showing.
    ///     </para>
    ///     <para>
    ///         The actual position may be adjusted to ensure the popover fits fully on screen.
    ///     </para>
    /// </remarks>
    public virtual void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        if (Visible)
        {
            return;
        }

        // Inherit App from Target if not already set
        if (App is null && Target?.TryGetTarget (out View? targetView) == true && targetView!.App is { } targetApp)
        {
            App = targetApp;
        }

        // Auto-register if needed
        if (App is { Popovers: { } popovers } && !popovers.IsRegistered (this))
        {
            popovers.Register (this);
        }

        // Ensure the Popover is sized correctly in case this is the first time we are being made visible
        Layout ();

        SetPosition (idealScreenPosition, anchor);

        App?.Popovers?.Show (this);
    }

    /// <summary>
    ///     Sets the position of the popover based on <paramref name="idealScreenPosition"/> or <paramref name="anchor"/>.
    ///     The actual position may be adjusted to ensure full visibility on screen.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position. If <see langword="null"/>, uses <paramref name="anchor"/> or the current mouse position.
    /// </param>
    /// <param name="anchor">
    ///     Optional anchor rectangle. If <see langword="null"/>, uses the <see cref="Anchor"/> property.
    /// </param>
    /// <remarks>
    ///     This method only sets the position; it does not make the popover visible. Use <see cref="MakeVisible"/> to
    ///     both position and show the popover.
    /// </remarks>
    public virtual void SetPosition (Point? idealScreenPosition = null, Rectangle? anchor = null)
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
        View.GetLocationEnsuringFullVisibility (view, idealLocation.X, idealLocation.Y, out int nx, out int ny);

        return new Point (nx, ny);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     When becoming visible, the content view is shown and <see cref="IsOpen"/> is set to <see langword="true"/>.
    ///     When becoming hidden, the content view is hidden, <see cref="Result"/> is extracted, and <see cref="IsOpen"/>
    ///     is set to <see langword="false"/>.
    /// </remarks>
    protected override void OnVisibleChanged ()
    {
        base.OnVisibleChanged ();

        if (Visible)
        {
            if (ContentView is { })
            {
                ContentView.Visible = true;
            }

            // Update IsOpen without triggering doWork (to avoid recursion)
            if (!_isOpen)
            {
                _isOpen = true;
                OnIsOpenChanged (new ValueChangedEventArgs<bool> (false, true));
                IsOpenChanged?.Invoke (this, new ValueChangedEventArgs<bool> (false, true));
            }
        }
        else
        {
            if (ContentView is { })
            {
                ContentView.Visible = false;
            }

            // Extract result before updating IsOpen
            ExtractResult ();

            // Specific to Popover
            App?.Popovers?.Hide (this);

            // Update IsOpen without triggering doWork (to avoid recursion)
            if (_isOpen)
            {
                _isOpen = false;
                OnIsOpenChanged (new ValueChangedEventArgs<bool> (true, false));
                IsOpenChanged?.Invoke (this, new ValueChangedEventArgs<bool> (true, false));
            }
        }
    }

    /// <summary>
    ///     Extracts the result from the content view using <see cref="ResultExtractor"/> or <see cref="IValue{TResult}"/>.
    /// </summary>
    private void ExtractResult ()
    {
        if (ContentView is null)
        {
            return;
        }

        if (ResultExtractor is { })
        {
            Result = ResultExtractor (ContentView);
        }
        else if (ContentView is IValue<TResult> iValue)
        {
            Result = iValue.Value;
        }
    }

    /// <summary>
    ///     Called when <see cref="IsOpen"/> is about to change. Override to customize behavior.
    /// </summary>
    /// <param name="args">The event arguments containing current and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise, <see langword="false"/>.</returns>
    protected virtual bool OnIsOpenChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>
    ///     Called when <see cref="IsOpen"/> has changed. Override to perform custom actions.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnIsOpenChanged (ValueChangedEventArgs<bool> args) { }

    /// <summary>
    ///     Called when the target view's focus state changes. Auto-closes the popover when target loses focus.
    /// </summary>
    private void OnTargetHasFocusChanged (object? sender, EventArgs e)
    {
        if (sender is View { HasFocus: false })
        {
            IsOpen = false;
        }
    }

    /// <summary>
    ///     Called when the ContentView becomes invisible, to hide the Popover.
    /// </summary>
    private void ContentViewOnVisibleChanged (object? sender, EventArgs e)
    {
        if (sender is View { Visible: false } view && view == ContentView && Visible)
        {
            Visible = false;
        }
    }

    /// <summary>
    ///     Enables the popover for use in design-time scenarios.
    /// </summary>
    /// <typeparam name="TContext">The type of the target view context.</typeparam>
    /// <param name="targetView">The target view to associate with the popover.</param>
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
    ///     This method unsubscribes from content view events and disposes the content view and command bridge.
    /// </remarks>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            if (ContentView is { } contentView)
            {
                contentView.VisibleChanged -= ContentViewOnVisibleChanged;
            }

            // Clean up Target subscriptions and bridge
            if (_target?.TryGetTarget (out View? targetView) == true && targetView is { })
            {
                targetView.HasFocusChanged -= OnTargetHasFocusChanged;
            }

            _targetCommandBridge?.Dispose ();
            _targetCommandBridge = null;
            _target = null;

            _contentCommandBridge?.Dispose ();
            _contentCommandBridge = null;
        }

        base.Dispose (disposing);
    }
}
