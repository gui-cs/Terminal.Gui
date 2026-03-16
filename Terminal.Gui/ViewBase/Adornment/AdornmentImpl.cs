namespace Terminal.Gui.ViewBase;

/// <summary>
///     Lightweight base class for adornment settings.
///     Holds <see cref="Thickness"/> and optional <see cref="Parent"/> reference.
///     The full <see cref="AdornmentView"/> is created lazily via <see cref="EnsureView"/>
///     only when needed.
/// </summary>
public abstract class AdornmentImpl : IAdornment
{
    /// <summary>
    ///     The <see cref="View"/> this adornment surrounds. Not on <see cref="IAdornment"/> — callers that need
    ///     the parent already have the <see cref="View"/> reference they used to access this adornment.
    ///     Set by <see cref="View.SetupAdornments"/> and used for geometry math and
    ///     <see cref="EnsureView"/> creation.
    /// </summary>
    public View? Parent { get; set; }

    #region Thickness

    /// <inheritdoc/>
    public Thickness Thickness
    {
        get;
        set
        {
            Thickness current = field;
            field = value;

            if (current == field)
            {
                return;
            }

            // AdornmentView.Thickness delegates back here via the IAdornment back-reference,
            // so no sync is needed — AdornmentImpl is the single authoritative owner.
            // Just invalidate the View if one exists.
            if (View is { })
            {
                View.SetNeedsLayout ();
                View.SetNeedsDraw ();
            }

            Parent?.SetAdornmentFrames ();

            // CWP: work (above) → virtual OnThicknessChanged (empty, for subclass override) → raise event
            OnThicknessChanged ();
            ThicknessChanged?.Invoke (this, EventArgs.Empty);
        }
    } = Thickness.Empty;

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
    public AdornmentView? View { get; private set; }

    /// <summary>
    ///     Explicit <see cref="IAdornment"/> implementation — exposes <see cref="View"/> as
    ///     <see cref="IAdornmentView"/> so the interface contract does not reference the concrete class.
    /// </summary>
    IAdornmentView? IAdornment.View => View;

    /// <summary>
    ///     Returns the existing <see cref="AdornmentView"/>, creating it if not yet allocated.
    ///     Calls <see cref="View.BeginInit"/> and/or <see cref="View.EndInit"/> on the new view
    ///     to match the parent's current initialization state.
    /// </summary>
    /// <remarks>Must be called on the UI thread. Internal to prevent eager allocation by consumers.</remarks>
    internal AdornmentView EnsureView ()
    {
        if (View is { })
        {
            return View;
        }
        View = CreateView ();

        // Synchronize init state with the parent.
        if (Parent?.IsInitialized == true)
        {
            View.BeginInit ();
            View.EndInit ();
        }

        Parent?.SetAdornmentFrames ();

        return View;
    }

    /// <summary>Factory method — subclasses return their specific <see cref="AdornmentView"/> subclass.</summary>
    protected abstract AdornmentView CreateView ();

    #endregion View

    #region Coordinator methods

    /// <inheritdoc/>
    public Point ViewportToScreen (in Point location) => View is { } v ? v.ViewportToScreen (location) : ComputeViewportToScreen (location);

    /// <inheritdoc/>
    public Rectangle FrameToScreen () => View is { } v ? v.FrameToScreen () : ComputeFrameToScreen ();

    /// <inheritdoc/>
    public Point ScreenToFrame (in Point location) => View is { } v ? v.ScreenToFrame (location) : ComputeScreenToFrame (location);

    private Point ComputeViewportToScreen (in Point location)
    {
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new Point (parentScreen.X + Frame.X + location.X, parentScreen.Y + Frame.Y + location.Y);
    }

    private Rectangle ComputeFrameToScreen ()
    {
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new Rectangle (new Point (parentScreen.X + Frame.X, parentScreen.Y + Frame.Y), Frame.Size);
    }

    private Point ComputeScreenToFrame (in Point location)
    {
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new Point (location.X - parentScreen.X - Frame.X, location.Y - parentScreen.Y - Frame.Y);
    }

    #endregion Coordinator methods

    #region Convenience pass-throughs

    /// <summary>Gets or sets the text displayed in this adornment's <see cref="View"/>.</summary>
    public string Text
    {
        get => View?.Text ?? field;
        set
        {
            field = value;

            if (View is { } v)
            {
                v.Text = value;
            }
        }
    } = string.Empty;

    /// <summary>Gets or sets arbitrary data associated with this adornment.</summary>
    public object? Data
    {
        get => View?.Data ?? field;
        set
        {
            field = value;

            if (View is { } v)
            {
                v.Data = value;
            }
        }
    }

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
    public IReadOnlyCollection<View> SubViews => View?.SubViews ?? _emptySubViews;

    private static readonly IReadOnlyCollection<View> _emptySubViews = Array.Empty<View> ();

    /// <summary>Adds a SubView to the backing <see cref="View"/>. Forces View creation via <see cref="EnsureView"/>.</summary>
    public virtual void Add (View subView) => EnsureView ().Add (subView);

    /// <summary>Gets whether the backing <see cref="View"/> needs to be redrawn. <see langword="false"/> if no View exists.</summary>
    public bool NeedsDraw => View?.NeedsDraw ?? false;

    /// <summary>Gets or sets whether the backing <see cref="View"/> needs layout. <see langword="false"/> if no View exists.</summary>
    public bool NeedsLayout
    {
        get => View?.NeedsLayout ?? false;
        set
        {
            if (View is { } v)
            {
                v.NeedsLayout = value;
            }
        }
    }

    /// <summary>Gets whether the backing <see cref="View"/> has focus. <see langword="false"/> if no View exists.</summary>
    public bool HasFocus => View?.HasFocus ?? false;

    /// <summary>Gets or sets the viewport settings flags on the backing <see cref="View"/>.</summary>
    public ViewportSettingsFlags ViewportSettings
    {
        get => View?.ViewportSettings ?? field;
        set
        {
            field = value;

            if (View is { } v)
            {
                v.ViewportSettings = value;
            }
        }
    }

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

    /// <summary>Clears the needs-draw state on the backing <see cref="View"/>. No-op if no View exists.</summary>
    public void ClearNeedsDraw () => View?.ClearNeedsDraw ();

    /// <summary>Gets or sets the Enabled state on the backing <see cref="View"/>.</summary>
    public bool Enabled
    {
        get => View?.Enabled ?? true;
        set
        {
            if (View is { } v)
            {
                v.Enabled = value;
            }
        }
    }

    /// <summary>Gets or sets the TabStop behavior on the backing <see cref="View"/>.</summary>
    public TabBehavior? TabStop
    {
        get => View?.TabStop;
        set
        {
            if (View is { } v)
            {
                v.TabStop = value;
            }
        }
    }

    /// <summary>Removes a SubView from the backing <see cref="View"/>. No-op if no View exists.</summary>
    public void Remove (View subView) => View?.Remove (subView);

    /// <summary>Gets or sets the Id on the backing <see cref="View"/>.</summary>
    public string Id
    {
        get => View?.Id ?? field;
        set
        {
            field = value;

            if (View is { } v)
            {
                v.Id = value;
            }
        }
    } = string.Empty;

    /// <summary>Gets or sets the SchemeName on the backing <see cref="View"/>.</summary>
    public string? SchemeName
    {
        get => View?.SchemeName ?? field;
        set
        {
            field = value;

            if (View is { } v)
            {
                v.SchemeName = value;
            }
        }
    }

    /// <summary>Gets whether the backing <see cref="View"/> is initialized.</summary>
    public bool IsInitialized => View?.IsInitialized ?? false;

    /// <summary>
    ///     Delegates to <see cref="View.GetAttributeForRole"/> on the backing <see cref="View"/>.
    ///     Falls back to the <see cref="Parent"/>'s attribute when no View exists.
    /// </summary>
    public Attribute GetAttributeForRole (VisualRole role) => View?.GetAttributeForRole (role) ?? Parent?.GetAttributeForRole (role) ?? default (Attribute);

    /// <summary>Calls <see cref="View.AddFrameToClip"/> on the backing <see cref="View"/>. Returns null if no View exists.</summary>
    internal Region? AddFrameToClip () => View?.AddFrameToClip ();

    /// <summary>Calls <see cref="View.DoDrawSubViews"/> on the backing <see cref="View"/>. No-op if no View exists.</summary>
    internal void DoDrawSubViews () => View?.DoDrawSubViews ();

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
        View = null;
        Parent = null;
    }

    #endregion Disposal
}
