namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.Menu"/> defaults (ThemeScope).
/// </summary>
public class MenuSettings
{
    /// <summary>Gets or sets the default border style for menus.</summary>
    public LineStyle DefaultBorderStyle { get; set; } = LineStyle.None;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static MenuSettings Defaults { get; set; } = new ();
}
