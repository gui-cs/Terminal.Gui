namespace Terminal.Gui.ViewBase;

/// <summary>
///     The lightweight Margin settings for a <see cref="View"/>. Accessed via <see cref="View.Margin"/>.
///     The underlying <see cref="MarginView"/> is created lazily when shadows, sub-views, or other
///     View-level functionality is needed.
/// </summary>
/// <remarks>
///     <para>
///         The Margin is transparent by default. This can be overridden by explicitly setting
///         <see cref="AdornmentImpl.SetScheme"/>.
///     </para>
///     <para>
///         Margins are drawn after all other Views in the application View hierarchy are drawn.
///     </para>
///     <para>
///         Margins have <see cref="ViewportSettingsFlags.TransparentMouse"/> enabled by default and are thus
///         transparent to the mouse.
///     </para>
/// </remarks>
public class Margin : AdornmentImpl
{
    /// <inheritdoc/>
    protected override AdornmentView CreateView ()
    {
        MarginView mv = new (Parent, this);

        if (_shadowStyle != ShadowStyle.None)
        {
            mv.ShadowStyle = _shadowStyle;
        }

        if (_shadowSize != Size.Empty)
        {
            mv.ShadowSize = _shadowSize;
        }

        return mv;
    }

    private ShadowStyle _shadowStyle;

    /// <summary>
    ///     Shadow effect. Setting to anything other than <see cref="ShadowStyle.None"/> forces a <see cref="MarginView"/>
    ///     to be created so the shadow sub-views can be hosted.
    /// </summary>
    public ShadowStyle ShadowStyle
    {
        get => _shadowStyle;
        set
        {
            _shadowStyle = value;

            if (View is MarginView mv)
            {
                mv.ShadowStyle = value;
            }
            else if (value != ShadowStyle.None)
            {
                ((MarginView)EnsureView ()).ShadowStyle = value;
            }
        }
    }

    private Size _shadowSize;

    /// <summary>Gets or sets the size of the shadow effect.</summary>
    public Size ShadowSize
    {
        get => View is MarginView mv ? mv.ShadowSize : _shadowSize;
        set
        {
            _shadowSize = value;

            if (View is MarginView mv)
            {
                mv.ShadowSize = value;
            }
        }
    }

    // --- Internal clip-cache methods (delegated to MarginView) ---

    internal void CacheClip () => (View as MarginView)?.CacheClip ();
    internal Region? GetCachedClip () => (View as MarginView)?.GetCachedClip ();
    internal void ClearCachedClip () => (View as MarginView)?.ClearCachedClip ();

    /// <summary>
    ///     Gets the border rectangle from the parent's border, if available.
    /// </summary>
    internal Rectangle GetBorderRectangle () => Parent?.Border?.GetBorderRectangle () ?? Rectangle.Empty;
}
