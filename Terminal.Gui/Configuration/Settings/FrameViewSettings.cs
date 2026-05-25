namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.FrameView"/> defaults (ThemeScope).
/// </summary>
public class FrameViewSettings
{
    /// <summary>Gets or sets the default border style for frame views.</summary>
    public LineStyle DefaultBorderStyle { get; set; } = LineStyle.Rounded;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static FrameViewSettings Defaults { get; set; } = new ();
}
