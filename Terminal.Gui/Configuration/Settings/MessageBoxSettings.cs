namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="MessageBox"/> visual defaults (ThemeScope).
/// </summary>
public class MessageBoxSettings
{
    /// <summary>Gets or sets the default border style for message boxes.</summary>
    public LineStyle DefaultBorderStyle { get; set; } = LineStyle.Heavy;

    /// <summary>Gets or sets the default button alignment for message boxes.</summary>
    public Alignment DefaultButtonAlignment { get; set; } = Alignment.Center;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static MessageBoxSettings Defaults { get; set; } = new ();
}
