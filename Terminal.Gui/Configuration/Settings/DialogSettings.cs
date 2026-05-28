namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.Dialog"/> visual defaults (ThemeScope).
/// </summary>
public class DialogSettings
{
    /// <summary>Gets or sets the default shadow style for dialogs.</summary>
    public ShadowStyles DefaultShadow { get; set; } = ShadowStyles.Transparent;

    /// <summary>Gets or sets the default border style for dialogs.</summary>
    public LineStyle DefaultBorderStyle { get; set; } = LineStyle.Heavy;

    /// <summary>Gets or sets the default button alignment for dialogs.</summary>
    public Alignment DefaultButtonAlignment { get; set; } = Alignment.End;

    /// <summary>Gets or sets the default button alignment modes for dialogs.</summary>
    public AlignmentModes DefaultButtonAlignmentModes { get; set; } = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static DialogSettings Defaults { get; set; } = new ();
}
