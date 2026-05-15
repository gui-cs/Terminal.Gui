namespace Terminal.Gui.App;

/// <summary>
///     Provides access to the terminal status line or title area.
/// </summary>
/// <remarks>
///     Status line output uses the driver's best available ANSI mechanism. The initial implementation uses OSC 0/1/2
///     title sequences, which are ignored by legacy consoles and unsupported terminals.
/// </remarks>
public sealed class StatusLine
{
    private readonly Func<IDriver?> _driverGetter;
    private bool _hasPendingWrite;

    internal StatusLine (Func<IDriver?> driverGetter) => _driverGetter = driverGetter;

    /// <summary>
    ///     Gets the last text requested for the status line.
    /// </summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the last OSC title selector requested.
    /// </summary>
    public int Mode { get; private set; } = 2;

    /// <summary>
    ///     Gets whether the current driver can emit ANSI status line output.
    /// </summary>
    public bool IsSupported => _driverGetter () is { IsLegacyConsole: false };

    /// <summary>
    ///     Clears the terminal status line or title area.
    /// </summary>
    public void Clear () => SetText (string.Empty);

    /// <summary>
    ///     Writes <paramref name="text"/> to the terminal status line or title area.
    /// </summary>
    /// <param name="text">The text to show. <see langword="null"/> is treated as an empty string.</param>
    /// <param name="mode">
    ///     The OSC title selector: 0 = icon and window title, 1 = icon title, 2 = window title.
    /// </param>
    public void SetText (string? text, int mode = 2)
    {
        Text = text ?? string.Empty;
        Mode = Math.Clamp (mode, 0, 2);
        _hasPendingWrite = true;
        Flush ();
    }

    internal void Flush ()
    {
        if (!_hasPendingWrite)
        {
            return;
        }

        IDriver? driver = _driverGetter ();

        if (driver is null || driver.IsLegacyConsole)
        {
            return;
        }

        driver.SetTerminalTitle (Text, Mode);
    }

    internal void Reset ()
    {
        Text = string.Empty;
        Mode = 2;
        _hasPendingWrite = false;
    }
}
