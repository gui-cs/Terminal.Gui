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

    /// <inheritdoc/>
    public override string ToString ()
    {
        List<string> parts = [];

        if (All is { Length: > 0 })
        {
            parts.Add ($"All=[{string.Join (", ", All)}]");
        }

        if (Windows is { Length: > 0 })
        {
            parts.Add ($"Win=[{string.Join (", ", Windows)}]");
        }

        if (Linux is { Length: > 0 })
        {
            parts.Add ($"Linux=[{string.Join (", ", Linux)}]");
        }

        if (Macos is { Length: > 0 })
        {
            parts.Add ($"Mac=[{string.Join (", ", Macos)}]");
        }

        return parts.Count > 0 ? string.Join ("; ", parts) : "(none)";
    }

    /// <summary>
    ///     Returns the key strings applicable to the current operating system.
    ///     Yields all <see cref="All"/> keys followed by the platform-specific keys.
    /// </summary>
    public IEnumerable<string> GetCurrentPlatformKeys ()
    {
        if (All is { })
        {
            foreach (string k in All)
            {
                yield return k;
            }
        }

        string []? platKeys = PlatformDetection.GetCurrentPlatform () switch
                              {
                                  TuiPlatform.Windows => Windows,
                                  TuiPlatform.Linux => Linux,
                                  TuiPlatform.Macos => Macos,
                                  _ => null
                              };

        if (platKeys is null)
        {
            yield break;
        }

        foreach (string k in platKeys)
        {
            yield return k;
        }
    }
}
