namespace Terminal.Gui.ViewBase;

/// <summary>
///     Lightweight base class for adornment settings.
///     Holds <see cref="Thickness"/> and optional <see cref="Parent"/> reference.
///     The full <see cref="AdornmentView"/> is created lazily via <see cref="EnsureView"/>
///     only when needed.
/// </summary>
public abstract class AdornmentImpl : IAdornment
{
    private AdornmentView? _view;

    /// <summary>
    ///     The <see cref="View"/> this adornment surrounds. Not on <see cref="IAdornment"/> — callers that need
    ///     the parent already have the <see cref="View"/> reference they used to access this adornment.
    ///     Set by <see cref="View.SetupAdornments"/> and used for geometry math and
    ///     <see cref="EnsureView"/> creation.
    /// </summary>
    public View? Parent { get; set; }

    #region Thickness

    private Thickness _thickness = Thickness.Empty;

    /// <inheritdoc/>
    public Thickness Thickness
    {
        get => _thickness;
        set
        {
            Thickness current = _thickness;
            _thickness = value;

            if (current != _thickness)
            {
                // AdornmentView.Thickness delegates back here via the IAdornment back-reference,
                // so no sync is needed — AdornmentImpl is the single authoritative owner.
                // Just invalidate the View if one exists.
                if (_view is { })
                {
                    _view.SetNeedsLayout ();
                    _view.SetNeedsDraw ();
                }

                Parent?.SetAdornmentFrames ();

                // CWP: work (above) → virtual OnThicknessChanged (empty, for subclass override) → raise event
                OnThicknessChanged ();
                ThicknessChanged?.Invoke (this, EventArgs.Empty);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler? ThicknessChanged;

    /// <summary>Called when <see cref="Thickness"/> changes. Override in subclasses to react; base is empty.</summary>
    protected virtual void OnThicknessChanged () { }

    #endregion Thickness

    #region Frame

    /// <inheritdoc/>
    public Rectangle Frame { get; internal set; }

    #endregion Frame

    #region View

    /// <summary>
    ///     The backing <see cref="AdornmentView"/> — <see langword="null"/> until demanded.
    ///     Returns the concrete <see cref="AdornmentView"/> for callers within this assembly.
    /// </summary>
    public AdornmentView? View => _view;

    /// <summary>
    ///     Explicit <see cref="IAdornment"/> implementation — exposes <see cref="View"/> as
    ///     <see cref="IAdornmentView"/> so the interface contract does not reference the concrete class.
    /// </summary>
    IAdornmentView? IAdornment.View => _view;

    /// <summary>
    ///     Returns the existing <see cref="AdornmentView"/>, creating it if not yet allocated.
    ///     Calls <see cref="View.BeginInit"/> and/or <see cref="View.EndInit"/> on the new view
    ///     to match the parent's current initialization state.
    /// </summary>
    /// <remarks>Must be called on the UI thread. Internal to prevent eager allocation by consumers.</remarks>
    internal AdornmentView EnsureView ()
    {
        if (_view is null)
        {
            _view = CreateView ();
            _view.Adornment = this;
            _view.Parent = Parent;

            // Synchronize init state with the parent.
            if (Parent?.IsInitialized == true)
            {
                _view.BeginInit ();
                _view.EndInit ();
            }

            Parent?.SetAdornmentFrames ();
        }

        return _view;
    }

    /// <summary>Factory method — subclasses return their specific <see cref="AdornmentView"/> subclass.</summary>
    protected abstract AdornmentView CreateView ();

    #endregion View

    #region Coordinator methods

    /// <inheritdoc/>
    public Point ViewportToScreen (in Point location)
        => View is { } v ? v.ViewportToScreen (location) : computeViewportToScreen (location);

    /// <inheritdoc/>
    public Rectangle FrameToScreen ()
        => View is { } v ? v.FrameToScreen () : computeFrameToScreen ();

    /// <inheritdoc/>
    public Point ScreenToFrame (in Point location)
        => View is { } v ? v.ScreenToFrame (location) : computeScreenToFrame (location);

    private Point computeViewportToScreen (in Point location)
    {
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (parentScreen.X + Frame.X + location.X,
                    parentScreen.Y + Frame.Y + location.Y);
    }

    private Rectangle computeFrameToScreen ()
    {
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (new (parentScreen.X + Frame.X, parentScreen.Y + Frame.Y), Frame.Size);
    }

    private Point computeScreenToFrame (in Point location)
    {
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (location.X - parentScreen.X - Frame.X,
                    location.Y - parentScreen.Y - Frame.Y);
    }

    #endregion Coordinator methods

    #region Convenience pass-throughs

    /// <summary>Gets or sets the text displayed in this adornment's <see cref="View"/>.</summary>
    public string Text
    {
        get => View?.Text ?? _text;
        set
        {
            _text = value;

            if (View is { } v)
            {
                v.Text = value;
            }
        }
    }
    private string _text = string.Empty;

    /// <summary>Gets or sets arbitrary data associated with this adornment.</summary>
    public object? Data
    {
        get => View?.Data ?? _data;
        set
        {
            _data = value;

            if (View is { } v)
            {
                v.Data = value;
            }
        }
    }
    private object? _data;

    /// <summary>Marks the backing <see cref="View"/> as needing to be redrawn. No-op if no View exists.</summary>
    public void SetNeedsDraw () => View?.SetNeedsDraw ();

    /// <summary>Marks the backing <see cref="View"/> as needing layout. No-op if no View exists.</summary>
    public void SetNeedsLayout () => View?.SetNeedsLayout ();

    /// <summary>Sets the <see cref="Scheme"/> on the backing <see cref="View"/>. No-op if no View exists.</summary>
    public void SetScheme (Scheme scheme) => View?.SetScheme (scheme);

    /// <summary>Gets the <see cref="Scheme"/> from the backing <see cref="View"/>.</summary>
    public Scheme? GetScheme () => View?.GetScheme ();

    /// <summary>Gets or sets whether the backing <see cref="View"/> can receive focus.</summary>
    public bool CanFocus
    {
        get => View?.CanFocus ?? false;
        set
        {
            if (View is { } v)
            {
                v.CanFocus = value;
            }
        }
    }

    /// <summary>Gets or sets diagnostic flags on the backing <see cref="View"/>.</summary>
    public ViewDiagnosticFlags Diagnostics
    {
        get => View?.Diagnostics ?? ViewDiagnosticFlags.Off;
        set
        {
            if (View is { } v)
            {
                v.Diagnostics = value;
            }
        }
    }

    /// <summary>Gets the SubViews of the backing <see cref="View"/>. Returns empty if no View exists.</summary>
    public IEnumerable<View> SubViews => View?.SubViews ?? [];

    /// <summary>Adds a SubView to the backing <see cref="View"/>. Forces View creation via <see cref="EnsureView"/>.</summary>
    public virtual void Add (View subView) => EnsureView ().Add (subView);

    /// <summary>Gets whether the backing <see cref="View"/> needs to be redrawn. <see langword="false"/> if no View exists.</summary>
    public bool NeedsDraw => View?.NeedsDraw ?? false;

    /// <summary>Gets whether the backing <see cref="View"/> needs layout. <see langword="false"/> if no View exists.</summary>
    public bool NeedsLayout => View?.NeedsLayout ?? false;

    /// <summary>Gets whether the backing <see cref="View"/> has focus. <see langword="false"/> if no View exists.</summary>
    public bool HasFocus => View?.HasFocus ?? false;

    /// <summary>Gets or sets the viewport settings flags on the backing <see cref="View"/>.</summary>
    public ViewportSettingsFlags ViewportSettings
    {
        get => View?.ViewportSettings ?? _viewportSettings;
        set
        {
            _viewportSettings = value;

            if (View is { } v)
            {
                v.ViewportSettings = value;
            }
        }
    }
    private ViewportSettingsFlags _viewportSettings;

    /// <summary>Gets or sets visibility of the backing <see cref="View"/>.</summary>
    public bool Visible
    {
        get => View?.Visible ?? true;
        set
        {
            if (View is { } v)
            {
                v.Visible = value;
            }
        }
    }

    /// <summary>
    ///     Indicates whether the specified SuperView-relative coordinates are within this adornment's
    ///     <see cref="Thickness"/>. Works even when no <see cref="View"/> has been created.
    /// </summary>
    public bool Contains (in Point location)
    {
        if (View is { } v)
        {
            return v.Contains (location);
        }

        if (Parent is null)
        {
            return false;
        }

        Rectangle outside = Frame;
        outside.Offset (Parent.Frame.Location);

        return Thickness.Contains (outside, location);
    }

    /// <summary>Calls <see cref="View.BeginInit"/> on the backing View. No-op if no View exists.</summary>
    public void BeginInit () => View?.BeginInit ();

    /// <summary>Calls <see cref="View.EndInit"/> on the backing View. No-op if no View exists.</summary>
    public void EndInit () => View?.EndInit ();

    /// <summary>Calls <see cref="View.LayoutSubViews"/> on the backing View. No-op if no View exists.</summary>
    public void LayoutSubViews () => View?.LayoutSubViews ();

    /// <summary>Disposes the backing <see cref="View"/> (if any) and clears references.</summary>
    public void Dispose () => DisposeView ();

    #endregion Convenience pass-throughs

    #region Drawing

    /// <summary>
    ///     Draws the adornment content if a <see cref="View"/> exists.
    /// </summary>
    internal virtual void Draw () => View?.Draw ();

    #endregion Drawing

    #region Disposal

    /// <summary>Propagates disposal to the <see cref="View"/> if it exists.</summary>
    internal void DisposeView ()
    {
        View?.Dispose ();
        _view = null;
        Parent = null;
    }

    #endregion Disposal
}
