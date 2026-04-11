namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes the kitty keyboard protocol capabilities discovered from the active terminal.
/// </summary>
public class KittyKeyboardCapabilities
{
    /// <summary>
    ///     Gets or sets whether the active terminal responded to the kitty keyboard protocol query.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    ///     Gets or sets the kitty keyboard flags reported by the terminal as enabled. If <see cref="IsSupported"/>
    ///     is <see langword="true"/>, <see cref="KittyKeyboardFlags.None"/> indicates a response to
    ///     <see cref="EscSeqUtils.CSI_QueryKittyKeyboardFlags"/>
    ///     had not yet been received.
    /// </summary>
    public KittyKeyboardFlags Flags { get; set; }
}
