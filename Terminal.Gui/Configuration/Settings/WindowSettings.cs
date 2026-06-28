namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.Window"/> visual defaults (ThemeScope).
/// </summary>
public class WindowSettings
{
    /// <summary>Gets or sets the default shadow style for windows.</summary>
    public ShadowStyles DefaultShadow { get; set; } = ShadowStyles.None;

    /// <summary>Gets or sets the default border style for windows.</summary>
    public LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static WindowSettings Defaults { get; set; } = new ();
}
