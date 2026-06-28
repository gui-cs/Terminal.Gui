namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.StatusBar"/> defaults (ThemeScope).
/// </summary>
public class StatusBarSettings
{
    /// <summary>Gets or sets the default separator line style for status bars.</summary>
    public LineStyle DefaultSeparatorLineStyle { get; set; } = LineStyle.Single;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static StatusBarSettings Defaults { get; set; } = new ();
}
