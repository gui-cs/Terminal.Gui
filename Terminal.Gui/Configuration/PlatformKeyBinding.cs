namespace Terminal.Gui;

/// <summary>
///     Defines the key strings for a single command, optionally varying by platform.
///     Keys are additive — for example, on Linux both <see cref="All"/> and <see cref="Linux"/> keys apply.
/// </summary>
public record PlatformKeyBinding
{
    /// <summary>Gets or sets keys that apply on all platforms.</summary>
    public string []? All { get; init; }

    /// <summary>Gets or sets additional keys for Windows only.</summary>
    public string []? Windows { get; init; }

    /// <summary>Gets or sets additional keys for Linux only.</summary>
    public string []? Linux { get; init; }

    /// <summary>Gets or sets additional keys for macOS only.</summary>
    public string []? Macos { get; init; }
}
