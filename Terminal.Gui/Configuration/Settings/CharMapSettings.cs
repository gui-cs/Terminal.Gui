namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.CharMap"/> defaults (ThemeScope).
/// </summary>
public class CharMapSettings
{
    /// <summary>Gets or sets the default cursor style for character map views.</summary>
    public CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBlock;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static CharMapSettings Defaults { get; set; } = new ();
}
