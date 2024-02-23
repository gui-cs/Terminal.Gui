namespace Terminal.Gui;

/// <summary>The Padding for a <see cref="View"/>.</summary>
/// <remarks>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Padding : Adornment
{
    /// <inheritdoc/>
    public Padding ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Padding (View parent) : base (parent)
    {
        /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */
    }

    /// <summary>
    ///     The color scheme for the Padding. If set to <see langword="null"/>, gets the <see cref="Adornment.Parent"/>
    ///     scheme. color scheme.
    /// </summary>
    public override ColorScheme ColorScheme
    {
        get
        {
            if (base.ColorScheme is { })
            {
                return base.ColorScheme;
            }

            return Parent?.ColorScheme;
        }
        set
        {
            base.ColorScheme = value;
            Parent?.SetNeedsDisplay ();
        }
    }

    /// <inheritdoc/>
    public override Rectangle FrameToScreen ()
    {
        Rectangle ret = base.FrameToScreen ();

        ret.X += Parent != null ? Parent.Margin.Thickness.Left + Parent.Border.Thickness.Left : 0;
        ret.Y += Parent != null ? Parent.Margin.Thickness.Top + Parent.Border.Thickness.Top : 0;

        return ret;
    }
}
