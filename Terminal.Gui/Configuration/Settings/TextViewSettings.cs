namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.TextView"/> defaults (ThemeScope).
/// </summary>
public class TextViewSettings
{
    /// <summary>Gets or sets the default cursor style for text views.</summary>
    public CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBar;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static TextViewSettings Defaults { get; set; } = new ();
}
