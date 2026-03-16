namespace Terminal.Gui.ViewBase;

/// <summary>
///     The lightweight Border settings for a <see cref="View"/>. Accessed via <see cref="View.Border"/>.
///     Stores <see cref="Thickness"/>, <see cref="LineStyle"/>, and <see cref="Settings"/> without creating a full View
///     unless rendering, arrangement, or SubViews require it.
/// </summary>
/// <remarks>
///     <para>
///         Renders a border around the view with the <see cref="View.Title"/>. A border using <see cref="LineStyle"/>
///         will be drawn on the sides of <see cref="Drawing.Thickness"/> that are greater than zero.
///     </para>
///     <para>
///         The Border provides keyboard and mouse support for moving and resizing the View. See
///         <see cref="ViewArrangement"/>.
///     </para>
/// </remarks>
public class Border : AdornmentImpl
{
    /// <inheritdoc/>
    protected override AdornmentView CreateView ()
    {
        BorderView bv = new (Parent, this) { LineStyle = _lineStyle ?? LineStyle.None, Settings = Settings };

        return bv;
    }

    private LineStyle? _lineStyle;

    /// <summary>
    ///     Sets the style of the border by changing the <see cref="Thickness"/>. This is a helper API for setting the
    ///     <see cref="Thickness"/> to <c>(1,1,1,1)</c> and setting the line style of the views that comprise the border. If
    ///     set to <see cref="LineStyle.None"/> no border will be drawn.
    /// </summary>
    public LineStyle LineStyle
    {
        get => _lineStyle ?? Parent?.SuperView?.BorderStyle ?? LineStyle.None;
        set
        {
            _lineStyle = value;

            if (View is BorderView bv)
            {
                bv.LineStyle = value;
            }
        }
    }

    private BorderSettings _settings = BorderSettings.Title;

    /// <summary>
    ///     Gets or sets the settings for the border.
    /// </summary>
    public BorderSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value;

            if (View is BorderView bv)
            {
                bv.Settings = value;
            }
        }
    }

    /// <summary>
    ///     Computes the border rectangle in screen coordinates.
    ///     Delegates to <see cref="BorderView.GetBorderRectangle"/> when a View exists.
    /// </summary>
    public Rectangle GetBorderRectangle ()
    {
        if (View is BorderView bv)
        {
            return bv.GetBorderRectangle ();
        }

        // Compute without a View
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (parentScreen.X + Math.Max (0, Thickness.Left - 1),
                    parentScreen.Y + Math.Max (0, Thickness.Top - 1),
                    Math.Max (0, parentScreen.Width - Math.Max (0, Thickness.Left - 1) - Math.Max (0, Thickness.Right - 1)),
                    Math.Max (0, parentScreen.Height - Math.Max (0, Thickness.Top - 1) - Math.Max (0, Thickness.Bottom - 1)));
    }

    /// <summary>
    ///     The view-arrangement controller. Only exists when a <see cref="BorderView"/> is present.
    /// </summary>
    internal Arranger? Arranger => (View as BorderView)?.Arranger;

    /// <summary>
    ///     Gets the subview used to render <see cref="ViewDiagnosticFlags.DrawIndicator"/>.
    /// </summary>
    public SpinnerView? DrawIndicator => (View as BorderView)?.DrawIndicator;

    /// <summary>
    ///     Advances the draw indicator animation. No-op if no <see cref="BorderView"/> exists.
    /// </summary>
    internal void AdvanceDrawIndicator () => (View as BorderView)?.AdvanceDrawIndicator ();
}
