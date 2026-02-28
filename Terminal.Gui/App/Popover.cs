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
///         <item>Target view weak reference for command bubbling and focus-loss auto-close</item>
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
    private CommandBridge? _targetCommandBridge;
    private bool _isOpen;
    private TResult? _result;

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
    ///     <para>
    ///         If set to <see langword="null"/>, a default <typeparamref name="TView"/> instance is created
    ///         automatically. This ensures <see cref="ContentView"/> is never <see langword="null"/> after
    ///         construction.
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

            Trace.Command (this, "ContentViewSet", $"Old={_contentView?.ToIdentifyingString ()} New={value?.ToIdentifyingString ()}");

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
            Trace.Command (this, "BridgeCreate", $"Bridging Activate from {_contentView.ToIdentifyingString ()}");
        }
    }

    /// <summary>
    ///     Gets or sets a weak reference to the target view that initiated this popover.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, the popover subscribes to the target's <see cref="View.HasFocusChanged"/> event.
    ///         If the target loses focus, the popover is automatically closed (<see cref="IsOpen"/> set to
    ///         <see langword="false"/>).
    ///     </para>
    ///     <para>
    ///         A <see cref="CommandBridge"/> is created from the popover to the target, relaying
    ///         <see cref="Command.Activate"/> so that activations propagate from ContentView through
    ///         the popover to the target.
    ///     </para>
    ///     <para>
    ///         The target is stored as a <see cref="WeakReference{T}"/> to prevent memory leaks.
    ///         This property is typically set by the view that owns the popover (e.g., a MenuBarItem).
    ///     </para>
    /// </remarks>
    public WeakReference<View?>? Target
    {
        get;
        set
        {
            // Unsubscribe from old target
            if (field?.TryGetTarget (out View? oldTarget) == true && oldTarget is { })
            {
                oldTarget.HasFocusChanged -= OnTargetHasFocusChanged;
            }

            _targetCommandBridge?.Dispose ();
            _targetCommandBridge = null;

            field = value;

            // Subscribe to new target and create bridge
            if (field?.TryGetTarget (out View? newTarget) == true && newTarget is { })
            {
                Trace.Command (this, "TargetSet", $"Target={newTarget.ToIdentifyingString ()}");
                newTarget.HasFocusChanged += OnTargetHasFocusChanged;

                // Bridge Activate from Popover → Target across the non-containment boundary.
                _targetCommandBridge = CommandBridge.Connect (newTarget, this, Command.Activate);
                Trace.Command (this, "TargetBridgeCreate", $"Bridging Activate to {newTarget.ToIdentifyingString ()}");
            }
        }
    }

    /// <summary>
    ///     Handles the target view losing focus by closing the popover.
    /// </summary>
    private void OnTargetHasFocusChanged (object? sender, HasFocusEventArgs e)
    {
        Trace.Command (this, "TargetFocusChanged", $"NewHasFocus={e.NewValue} IsOpen={_isOpen}");

        if (!e.NewValue)
        {
            IsOpen = false;
        }
    }

    /// <summary>
    ///     Gets or sets whether the popover is open and visible.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a CWP (Cancellable Workflow Property) property. Setting it to <see langword="true"/>
    ///         calls <see cref="MakeVisible"/>; setting to <see langword="false"/> hides the popover.
    ///     </para>
    ///     <para>
    ///         The <see cref="IsOpenChanging"/> event can be used to cancel the change before it takes effect.
    ///         The <see cref="IsOpenChanged"/> event fires after the change is complete.
    ///     </para>
    ///     <para>
    ///         This property is automatically synchronized with <see cref="View.Visible"/> — setting
    ///         <c>Visible = false</c> directly will also update <c>IsOpen</c> to <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen == value)
            {
                return;
            }

            Trace.Command (this, "IsOpenSet", $"Current={_isOpen} New={value}");

            CWPPropertyHelper.ChangeProperty (this,
                                              ref _isOpen,
                                              value,
                                              OnIsOpenChanging,
                                              IsOpenChanging,
                                              newValue =>
                                              {
                                                  _isOpen = newValue;
                                                  Trace.Command (this, "IsOpenDoWork", $"IsOpen={_isOpen}");

                                                  if (_isOpen)
                                                  {
                                                      // Use Anchor for positioning if available
                                                      Rectangle? anchorRect = Anchor?.Invoke ();
                                                      MakeVisible (anchor: anchorRect);
                                                  }
                                                  else
                                                  {
                                                      Visible = false;
                                                  }
                                              },
                                              OnIsOpenChanged,
                                              IsOpenChanged,
                                              out _);
        }
    }

    /// <summary>
    ///     Raised when <see cref="IsOpen"/> is about to change. Set <see cref="CancelEventArgs{T}.Handled"/>
    ///     to <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? IsOpenChanging;

    /// <summary>
    ///     Raised when <see cref="IsOpen"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? IsOpenChanged;

    /// <summary>
    ///     Gets or sets a function that returns the screen-relative anchor rectangle for popover positioning.
    /// </summary>
    /// <remarks>
    ///     When set, the popover is positioned relative to the rectangle returned by this function.
    ///     The content view is placed below the anchor if possible, otherwise above it.
    ///     If not set, the popover is positioned at the mouse cursor location.
    /// </remarks>
    public Func<Rectangle>? Anchor { get; set; }

    /// <summary>
    ///     Gets or sets a function that extracts a result from the content view.
    /// </summary>
    /// <remarks>
    ///     When the popover is hidden, this function is called to extract the result.
    ///     If not set, the result is extracted from <see cref="IValue{TResult}"/> if the content view implements it.
    /// </remarks>
    public Func<TView, TResult?>? ResultExtractor { get; set; }

    /// <summary>
    ///     Gets or sets the result extracted from the content view.
    /// </summary>
    /// <remarks>
    ///     The result is automatically extracted when the popover is hidden (via <see cref="ResultExtractor"/>
    ///     or <see cref="IValue{TResult}"/>). It can also be set manually.
    /// </remarks>
    public TResult? Result
    {
        get => _result;
        protected set
        {
            if (EqualityComparer<TResult?>.Default.Equals (_result, value))
            {
                return;
            }

            TResult? oldResult = _result;
            _result = value;
            ResultChanged?.Invoke (this, new ValueChangedEventArgs<TResult?> (oldResult, _result));
        }
    }

    /// <summary>
    ///     Raised when <see cref="Result"/> changes.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<TResult?>>? ResultChanged;

    /// <summary>
    ///     Makes the popover visible and positions it. If the popover is not yet registered
    ///     with <see cref="Application.Popover"/>, it will be automatically registered.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position. If <see langword="null"/>, the anchor or mouse position is used.
    /// </param>
    /// <param name="anchor">
    ///     Optional anchor rectangle for positioning. Overrides the <see cref="Anchor"/> property.
    /// </param>
    public virtual void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        Trace.Command (this, "Entry", $"Visible={Visible} App={App is not null} Registered={App?.Popovers?.IsRegistered (this)}");

        if (Visible)
        {
            return;
        }

        // Auto-register if not already registered
        if (App is { Popovers: { } popovers } && !popovers.IsRegistered (this))
        {
            Trace.Command (this, "AutoRegister", "Registering with Application.Popovers");
            popovers.Register (this);
        }

        // Ensure the Popover is sized correctly
        Layout ();

        SetPosition (idealScreenPosition, anchor);

        // Show the popover
        App?.Popovers?.Show (this);
    }

    /// <summary>
    ///     Sets the position of the popover content view.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position. If <see langword="null"/>, uses anchor or mouse position.
    /// </param>
    /// <param name="anchor">
    ///     Optional anchor rectangle. If <see langword="null"/>, uses the <see cref="Anchor"/> property.
    /// </param>
    public virtual void SetPosition (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        anchor ??= Anchor?.Invoke ();
        idealScreenPosition ??= anchor is { } a ? new Point (a.X, a.Bottom) : App?.Mouse.LastMousePosition;

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

        pos = GetAnchoredPosition (ContentView, pos);

        ContentView.X = pos.X;
        ContentView.Y = pos.Y;
    }

    /// <summary>
    ///     Calculates the best position for the content view, ensuring it stays fully visible on screen.
    /// </summary>
    /// <param name="view">The view to position.</param>
    /// <param name="idealLocation">The ideal location.</param>
    /// <returns>The adjusted position.</returns>
    internal Point GetAnchoredPosition (View view, Point idealLocation)
    {
        GetLocationEnsuringFullVisibility (view, idealLocation.X, idealLocation.Y, out int nx, out int ny);

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
        Trace.Command (this, "Entry", $"Visible={Visible} IsOpen={_isOpen}");
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
    ///     Called when the ContentView becomes invisible, to hide the Popover.
    /// </summary>
    private void ContentViewOnVisibleChanged (object? sender, EventArgs e)
    {
        if (sender is View { Visible: false } view && view == ContentView && Visible)
        {
            Trace.Command (this, "ContentViewHidden", "ContentView became invisible — hiding Popover");
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

            // Unsubscribe from target focus tracking
            if (Target?.TryGetTarget (out View? targetView) == true && targetView is { })
            {
                targetView.HasFocusChanged -= OnTargetHasFocusChanged;
            }

            _targetCommandBridge?.Dispose ();
            _targetCommandBridge = null;

            _contentCommandBridge?.Dispose ();
            _contentCommandBridge = null;
        }

        base.Dispose (disposing);
    }
}
