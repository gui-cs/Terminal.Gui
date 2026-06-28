namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.TextField"/> defaults (ThemeScope).
/// </summary>
public class TextFieldSettings
{
    /// <summary>Gets or sets the default cursor style for text fields.</summary>
    public CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBar;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static TextFieldSettings Defaults { get; set; } = new ();
}
