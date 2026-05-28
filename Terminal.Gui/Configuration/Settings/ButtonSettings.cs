namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.Button"/> visual defaults (ThemeScope).
/// </summary>
public class ButtonSettings
{
    /// <summary>Gets or sets the default shadow style for buttons.</summary>
    public ShadowStyles DefaultShadow { get; set; } = ShadowStyles.Opaque;

    /// <summary>Gets or sets the default mouse highlight states for buttons.</summary>
    public MouseState DefaultMouseHighlightStates { get; set; } = MouseState.In | MouseState.Pressed | MouseState.PressedOutside;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static ButtonSettings Defaults { get; set; } = new ();
}
