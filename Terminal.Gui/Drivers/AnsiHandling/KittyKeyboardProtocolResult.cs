namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes the kitty keyboard protocol state discovered from the active terminal.
/// </summary>
public class KittyKeyboardProtocolResult
{
    /// <summary>
    ///     Gets or sets whether the active terminal responded to the kitty keyboard protocol query.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    ///     Gets or sets the kitty keyboard flags reported by the terminal.
    /// </summary>
    public KittyKeyboardFlags SupportedFlags { get; set; }

    /// <summary>
    ///     Gets or sets the kitty keyboard flags Terminal.Gui intends to enable.
    /// </summary>
    public KittyKeyboardFlags EnabledFlags { get; set; }
}
