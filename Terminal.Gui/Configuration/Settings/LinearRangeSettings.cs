namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.LinearRangeDefaults"/> defaults (ThemeScope).
/// </summary>
public class LinearRangeSettings
{
    /// <summary>Gets or sets the default cursor style for linear range views.</summary>
    public CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBlock;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static LinearRangeSettings Defaults { get; set; } = new ();
}
