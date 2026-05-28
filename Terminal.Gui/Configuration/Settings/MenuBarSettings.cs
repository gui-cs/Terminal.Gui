namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.MenuBar"/> defaults.
/// </summary>
public class MenuBarSettings
{
    /// <summary>Gets or sets the default border style for menu bars.</summary>
    public LineStyle DefaultBorderStyle { get; set; } = LineStyle.None;

    /// <summary>Gets or sets the default activation key for menu bars.</summary>
    public Key DefaultKey { get; set; } = Key.F10;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static MenuBarSettings Defaults { get; set; } = new ();
}
