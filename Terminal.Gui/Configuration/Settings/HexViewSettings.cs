namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.HexView"/> defaults (ThemeScope).
/// </summary>
public class HexViewSettings
{
    /// <summary>Gets or sets the default cursor style for hex views.</summary>
    public CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBlock;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static HexViewSettings Defaults { get; set; } = new ();
}
