namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Text.NerdFonts"/> defaults (ThemeScope).
/// </summary>
public class NerdFontsSettings
{
    /// <summary>Gets or sets whether Nerd Fonts glyphs are enabled.</summary>
    public bool Enable { get; set; } = false;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static NerdFontsSettings Defaults { get; set; } = new ();
}
