#nullable enable
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

#region API Docs

/// <summary>
///     View is the base class all visible elements. View can render itself and
///     contains zero or more nested views, called SubViews. View provides basic functionality for layout, arrangement, and
///     drawing. In addition, View provides keyboard and mouse event handling.
///     <para>
///         See the
///         <see href="../docs/view.md">
///             View
///             Deep Dive
///         </see>
///         for more.
///     </para>
/// </summary>

#endregion API Docs

public partial class View : IDisposable, ISupportInitializeNotification
{
    private bool _disposedValue;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resource.</summary>
    public void Dispose ()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Disposing?.Invoke (this, EventArgs.Empty);

        Dispose (true);
        GC.SuppressFinalize (this);

#if DEBUG_IDISPOSABLE
        WasDisposed = true;
        // Safely remove any disposed views from the Instances list
        List<View> itemsToKeep = Instances.Where (view => !view.WasDisposed).ToList ();
        Instances = new ConcurrentBag<View> (itemsToKeep);
#endif
    }

    /// <summary>
    ///     Riased when the <see cref="View"/> is being disposed.
    /// </summary>
    public event EventHandler? Disposing;

    /// <summary>Pretty prints the View</summary>
    /// <returns></returns>
    public override string ToString () { return $"{GetType ().Name}({Id}){Frame}"; }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    /// <remarks>
    ///     If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and
    ///     unmanaged resources can be disposed. If disposing equals false, the method has been called by the runtime from
    ///     inside the finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
    /// </remarks>
    /// <param name="disposing"></param>
    protected virtual void Dispose (bool disposing)
    {
        if (disposing)
        {
            LineCanvas.Dispose ();

            DisposeMouse ();
            DisposeKeyboard ();
            DisposeAdornments ();
            DisposeScrollBars ();

            if (Application.MouseGrabHandler.MouseGrabView == this)
            {
                Application.MouseGrabHandler.UngrabMouse ();
            }

            for (int i = InternalSubViews.Count - 1; i >= 0; i--)
            {
                View subview = InternalSubViews [i];
                Remove (subview);
                subview.Dispose ();
            }

            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                _disposedValue = true;
            }

            Debug.Assert (InternalSubViews.Count == 0);
        }
    }

    #region Constructors and Initialization

    /// <summary>Gets or sets arbitrary data for the view.</summary>
    /// <remarks>This property is not used internally.</remarks>
    public object? Data { get; set; }

    /// <summary>Gets or sets an identifier for the view;</summary>
    /// <value>The identifier.</value>
    /// <remarks>The id should be unique across all Views that share a SuperView.</remarks>
    public string Id { get; set; } = "";

    private IConsoleDriver? _driver = null;
    /// <summary>
    ///     INTERNAL: Use <see cref="Application.Driver"/> instead. Points to the current driver in use by the view, it is a convenience property for simplifying the development
    ///     of new views.
    /// </summary>
    internal IConsoleDriver? Driver
    {
        get
        {
            if (_driver is { })
            {
                return _driver;
            }
            return Application.Driver;
        }
        set => _driver = value;
    }

    /// <summary>Initializes a new instance of <see cref="View"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically
    ///         control the size and location of the view.
    ///     </para>
    /// </remarks>
    public View ()
    {
#if DEBUG_IDISPOSABLE
        Instances.Add (this);
#endif

        SetupAdornments ();

        SetupCommands ();

        SetupKeyboard ();

        SetupMouse ();

        SetupText ();

        SetupScrollBars ();
    }

    /// <summary>
    ///     Raised once when the <see cref="View"/> is being initialized for the first time. Allows
    ///     configurations and assignments to be performed before the <see cref="View"/> being shown.
    ///     View implements <see cref="ISupportInitializeNotification"/> to allow for more sophisticated initialization.
    /// </summary>
    public event EventHandler? Initialized;

    /// <summary>
    ///     Get or sets if  the <see cref="View"/> has been initialized (via <see cref="ISupportInitialize.BeginInit"/>
    ///     and <see cref="ISupportInitialize.EndInit"/>).
    /// </summary>
    /// <para>
    ///     If first-run-only initialization is preferred, overrides to
    ///     <see cref="ISupportInitializeNotification.IsInitialized"/> can be implemented, in which case the
    ///     <see cref="ISupportInitialize"/> methods will only be called if
    ///     <see cref="ISupportInitializeNotification.IsInitialized"/> is <see langword="false"/>. This allows proper
    ///     <see cref="View"/> inheritance hierarchies to override base class layout code optimally by doing so only on first
    ///     run, instead of on every run.
    /// </para>
    public virtual bool IsInitialized { get; set; }

    /// <summary>Signals the View that initialization is starting. See <see cref="ISupportInitialize"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         Views can opt-in to more sophisticated initialization by implementing overrides to
    ///         <see cref="ISupportInitialize.BeginInit"/> and <see cref="ISupportInitialize.EndInit"/> which will be called
    ///         when the <see cref="SuperView"/> is initialized.
    ///     </para>
    ///     <para>
    ///         If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification"/> can
    ///         be implemented too, in which case the <see cref="ISupportInitialize"/> methods will only be called if
    ///         <see cref="ISupportInitializeNotification.IsInitialized"/> is <see langword="false"/>. This allows proper
    ///         <see cref="View"/> inheritance hierarchies to override base class layout code optimally by doing so only on
    ///         first run, instead of on every run.
    ///     </para>
    /// </remarks>
    public virtual void BeginInit ()
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException ("The view is already initialized.");
        }
#if AUTO_CANFOCUS
        _oldCanFocus = CanFocus;
        _oldTabIndex = _tabIndex;
#endif

        BeginInitAdornments ();

        if (InternalSubViews?.Count > 0)
        {
            foreach (View view in InternalSubViews)
            {
                if (!view.IsInitialized)
                {
                    view.BeginInit ();
                }
            }
        }
    }

    // TODO: Implement logic that allows EndInit to throw if BeginInit has not been called
    // TODO: See EndInit_Called_Without_BeginInit_Throws test.

    /// <summary>Signals the View that initialization is ending. See <see cref="ISupportInitialize"/>.</summary>
    /// <remarks>
    ///     <para>Initializes all SubViews and Invokes the <see cref="Initialized"/> event.</para>
    /// </remarks>
    public virtual void EndInit ()
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException ("The view is already initialized.");
        }

        IsInitialized = true;

        EndInitAdornments ();

        // TODO: Move these into ViewText.cs as EndInit_Text() to consolidate.
        // TODO: Verify UpdateTextDirection really needs to be called here.
        // These calls were moved from BeginInit as they access Viewport which is indeterminate until EndInit is called.
        UpdateTextDirection (TextDirection);
        UpdateTextFormatterText ();

        foreach (View view in InternalSubViews)
        {
            if (!view.IsInitialized)
            {
                view.EndInit ();
            }
        }

        // Force a layout each time a View is initialized
        // See: https://github.com/gui-cs/Terminal.Gui/issues/3951
        // See: https://github.com/gui-cs/Terminal.Gui/issues/4204
        Layout (); // the EventLog in AllViewsTester fails to layout correctly if this is not here (convoluted Dim.Fill(Func)).

        // Complex layout scenarios (e.g. DimAuto and PosAlign) may require multiple layouts to be performed.
        // Thus, we call SetNeedsLayout() to ensure that the layout is performed at least once.
        SetNeedsLayout ();

        Initialized?.Invoke (this, EventArgs.Empty);
    }

    #endregion Constructors and Initialization

    #region Visibility

    private bool _enabled = true;

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> can respond to user interaction.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;

            if (!_enabled && HasFocus)
            {
                HasFocus = false;
            }

            if (_enabled
                && CanFocus
                && Visible
                && !HasFocus
                && SuperView is null or { HasFocus: true, Visible: true, Enabled: true, Focused: null })
            {
                SetFocus ();
            }

            OnEnabledChanged ();
            SetNeedsDraw ();

            if (Border is { })
            {
                Border.Enabled = _enabled;
            }

            foreach (View view in InternalSubViews)
            {
                view.Enabled = Enabled;
            }
        }
    }

    /// <summary>Raised when the <see cref="Enabled"/> value is being changed.</summary>
    public event EventHandler? EnabledChanged;

    // TODO: Change this event to match the standard TG event model.
    /// <summary>Invoked when the <see cref="Enabled"/> property from a view is changed.</summary>
    public virtual void OnEnabledChanged () { EnabledChanged?.Invoke (this, EventArgs.Empty); }

    private bool _visible = true;

    // TODO: Remove virtual once Menu/MenuBar are removed. MenuBar is the only override.
    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> is visible.</summary>
    public virtual bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
            {
                return;
            }

            if (OnVisibleChanging ())
            {
                return;
            }

            CancelEventArgs<bool> args = new (in _visible, ref value);
            VisibleChanging?.Invoke (this, args);

            if (args.Cancel)
            {
                return;
            }

            _visible = value;

            if (!_visible)
            {
                // BUGBUG: Ideally we'd reset _previouslyFocused to the first focusable subview
                _previouslyFocused = SubViews.FirstOrDefault (v => v.CanFocus);
                if (HasFocus)
                {
                    HasFocus = false;
                }
            }

            if (_visible
                && CanFocus
                && Enabled
                && !HasFocus
                && SuperView is null or { HasFocus: true, Visible: true, Enabled: true, Focused: null })
            {
                SetFocus ();
            }

            OnVisibleChanged ();
            VisibleChanged?.Invoke (this, EventArgs.Empty);

            SetNeedsLayout ();
            SuperView?.SetNeedsLayout ();
            SetNeedsDraw ();

            if (SuperView is { })
            {
                SuperView?.SetNeedsDraw ();
            }
            else
            {
                Application.ClearScreenNextIteration = true;
            }
        }
    }

    /// <summary>Called when <see cref="Visible"/> is changing. Can be cancelled by returning <see langword="true"/>.</summary>
    protected virtual bool OnVisibleChanging () { return false; }

    /// <summary>
    ///     Raised when the <see cref="Visible"/> value is being changed. Can be cancelled by setting Cancel to
    ///     <see langword="true"/>.
    /// </summary>
    public event EventHandler<CancelEventArgs<bool>>? VisibleChanging;

    /// <summary>Called when <see cref="Visible"/> has changed.</summary>
    protected virtual void OnVisibleChanged () { }

    /// <summary>Raised when <see cref="Visible"/> has changed.</summary>
    public event EventHandler? VisibleChanged;

    /// <summary>
    ///     INTERNAL Indicates whether all views up the Superview hierarchy are visible.
    /// </summary>
    /// <param name="view">The view to test.</param>
    /// <returns>
    ///     <see langword="false"/> if `view.Visible` is  <see langword="false"/> or any Superview is not visible,
    ///     <see langword="true"/> otherwise.
    /// </returns>
    internal static bool CanBeVisible (View view)
    {
        if (!view.Visible)
        {
            return false;
        }

        for (View? c = view.SuperView; c != null; c = c.SuperView)
        {
            if (!c.Visible)
            {
                return false;
            }
        }

        return true;
    }

    #endregion Visibility

    #region Title

    private string _title = string.Empty;

    /// <summary>Gets the <see cref="Text.TextFormatter"/> used to format <see cref="Title"/>.</summary>
    internal TextFormatter TitleTextFormatter { get; init; } = new ();

    /// <summary>
    ///     The title to be displayed for this <see cref="View"/>. The title will be displayed if <see cref="Border"/>.
    ///     <see cref="Thickness.Top"/> is greater than 0. The title can be used to set the <see cref="HotKey"/>
    ///     for the view by prefixing character with <see cref="HotKeySpecifier"/> (e.g. <c>"T_itle"</c>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable Title-based hotkey support set
    ///         <see cref="HotKeySpecifier"/> to <c>(Rune)0xffff</c>.
    ///     </para>
    ///     <para>
    ///         Only the first HotKey specifier found in <see cref="Title"/> is supported.
    ///     </para>
    ///     <para>
    ///         To cause the hotkey to be rendered with <see cref="Text"/>,
    ///         set <c>View.</c><see cref="TextFormatter.HotKeySpecifier"/> to the desired character.
    ///     </para>
    /// </remarks>
    /// <value>The title.</value>
    public string Title
    {
        get
        {
            return _title;
        }
        set
        {
#if DEBUG_IDISPOSABLE
            if (EnableDebugIDisposableAsserts && WasDisposed)
            {
                throw new ObjectDisposedException (GetType ().FullName);
            }
#endif
            if (value == _title)
            {
                return;
            }

            if (!OnTitleChanging (ref value))
            {
                string old = _title;
                _title = value;
                TitleTextFormatter.Text = _title;

                SetTitleTextFormatterSize ();
                SetHotKeyFromTitle ();
                SetNeedsDraw ();
                OnTitleChanged ();
            }
        }
    }

    private void SetTitleTextFormatterSize ()
    {
        TitleTextFormatter.ConstrainToSize = new (
                                                  TextFormatter.GetWidestLineLength (TitleTextFormatter.Text)
                                                  - (TitleTextFormatter.Text?.Contains ((char)HotKeySpecifier.Value) == true
                                                         ? Math.Max (HotKeySpecifier.GetColumns (), 0)
                                                         : 0),
                                                  1);
    }

    // TODO: Change this event to match the standard TG event model.
    /// <summary>Called when the <see cref="View.Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.</summary>
    protected void OnTitleChanged () { TitleChanged?.Invoke (this, new (in _title)); }

    /// <summary>
    ///     Called before the <see cref="View.Title"/> changes. Invokes the <see cref="TitleChanging"/> event, which can
    ///     be cancelled.
    /// </summary>
    /// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
    /// <returns>`true` if an event handler canceled the Title change.</returns>
    protected bool OnTitleChanging (ref string newTitle)
    {
        CancelEventArgs<string> args = new (ref _title, ref newTitle);
        TitleChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>Raised after the <see cref="View.Title"/> has been changed.</summary>
    public event EventHandler<EventArgs<string>>? TitleChanged;

    /// <summary>
    ///     Raised when the <see cref="View.Title"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to `true`
    ///     to cancel the Title change.
    /// </summary>
    public event EventHandler<CancelEventArgs<string>>? TitleChanging;

    #endregion

#if DEBUG_IDISPOSABLE
    /// <summary>
    ///     Gets or sets whether failure to appropriately call Dispose() on a View will result in an Assert.
    ///     The default is <see langword="true"/>.
    ///     Note, this is a static property and will affect all Views.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public static bool EnableDebugIDisposableAsserts { get; set; } = true;

    /// <summary>
    ///     Gets whether <see cref="Dispose"/> was called on this view or not.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public bool WasDisposed { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="Dispose"/> was called on this view.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public int DisposedCount { get; private set; } = 0;

    /// <summary>
    ///     Gets the list of Views that have been created and not yet disposed.
    ///     Note, this is a static property and will affect all Views.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public static ConcurrentBag<View> Instances { get; private set; } = [];
#endif
}
