namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.PopoverMenu"/> defaults (SettingsScope).
/// </summary>
public class PopoverMenuSettings
{
    /// <summary>Gets or sets the default activation key for popover menus.</summary>
    public Key DefaultKey { get; set; } = Key.F10.WithShift;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static PopoverMenuSettings Defaults { get; set; } = new ();
}
