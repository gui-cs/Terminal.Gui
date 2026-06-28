namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.SelectorBase"/> defaults (ThemeScope).
/// </summary>
public class SelectorBaseSettings
{
    /// <summary>Gets or sets the default mouse highlight states for selectors.</summary>
    public MouseState DefaultMouseHighlightStates { get; set; } = MouseState.In;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static SelectorBaseSettings Defaults { get; set; } = new ();
}
