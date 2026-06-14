namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.CheckBox"/> visual defaults (ThemeScope).
/// </summary>
public class CheckBoxSettings
{
    /// <summary>Gets or sets the default mouse highlight states for checkboxes.</summary>
    public MouseState DefaultMouseHighlightStates { get; set; } = MouseState.PressedOutside | MouseState.Pressed | MouseState.In;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static CheckBoxSettings Defaults { get; set; } = new ();
}
