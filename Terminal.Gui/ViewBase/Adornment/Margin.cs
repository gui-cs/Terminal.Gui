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
        MarginView mv = new (this);

        //if (ShadowStyle != ViewBase.ShadowStyle.None)
        //{
        //    mv.ShadowStyle = ShadowStyle;
        //}

        //if (ShadowSize != Size.Empty)
        //{
        //    mv.ShadowSize = ShadowSize;
        //}

        return mv;
    }

    /// <inheritdoc/>
    public override Rectangle GetFrame () => Parent is { } ? Parent.Frame with { Location = Point.Empty } : Rectangle.Empty;

    /// <inheritdoc/>
    protected override void OnThicknessChanged ()
    {
        base.OnThicknessChanged ();

        if (Thickness == Thickness.Empty)
        {
            return;
        }

        if (ShadowStyle is null)
        {
            return;
        }

        EnsureView ();
    }

    /// <summary>
    ///     Shadow effect. Setting to anything other than <see cref="ShadowStyles.None"/> forces a <see cref="MarginView"/>
    ///     to be created so the shadow sub-views can be hosted.
    /// </summary>
    public ShadowStyles? ShadowStyle
    {
        get => field ?? Parent?.SuperView?.ShadowStyle ?? null;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;

            if (field is null)
            {
                // null means no shadow and no thickness
                (View as MarginView)?.SetShadow (null);

                return;
            }

            if (EnsureView () is not MarginView marginView)
            {
                return;
            }

            switch (field)
            {
                case ShadowStyles.Opaque:
                case ShadowStyles.Transparent when marginView.ShadowSize.Width == 0 || marginView.ShadowSize.Height == 0:
                {
                    if (marginView.ShadowSize.Width != 1)
                    {
                        marginView.ShadowSize = marginView.ShadowSize with { Width = 1 };
                    }

                    if (marginView.ShadowSize.Height != 1)
                    {
                        marginView.ShadowSize = marginView.ShadowSize with { Height = 1 };
                    }

                    break;
                }
            }

            // Always call SetShadow to update thickness and shadow views
            marginView.SetShadow (field);
        }
    }

    // --- Internal clip-cache methods (delegated to MarginView) ---

    internal void CacheClip () => (View as MarginView)?.CacheClip ();
    internal Region? GetCachedClip () => (View as MarginView)?.GetCachedClip ();
    internal void ClearCachedClip () => (View as MarginView)?.ClearCachedClip ();
}
