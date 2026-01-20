using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for detecting platform-specific features.
/// </summary>
public static class PlatformDetection
{
    /// <summary>
    ///     Determines if the current platform is WSL (Windows Subsystem for Linux).
    /// </summary>
    /// <returns>True if running on WSL, false otherwise.</returns>
    public static bool IsWSL ()
    {
        // xclip does not work on WSL, so we need to use the Windows clipboard via Powershell
        (int exitCode, string result) = ClipboardProcessRunner.Bash ("uname -a", waitForOutput: true);

        return exitCode == 0 && result.Contains ("microsoft") && result.Contains ("WSL");
    }

    /// <summary>
    ///     Determines if the current platform is Windows.
    /// </summary>
    /// <returns></returns>
    public static bool IsWindows () => RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

    /// <summary>
    ///     Determines whether the current operating system is a Unix-like platform, excluding macOS and FreeBSD.
    /// </summary>
    /// <remarks>
    ///     This method is useful for distinguishing Linux environments from other Unix-like systems such
    ///     as macOS and FreeBSD. It can be used to enable platform-specific behavior in cross-platform
    ///     applications.
    /// </remarks>
    /// <returns>true if the operating system is Linux and not macOS or FreeBSD; otherwise, false.</returns>
    public static bool IsUnixLike () =>
        RuntimeInformation.IsOSPlatform (OSPlatform.Linux)
        && !RuntimeInformation.IsOSPlatform (OSPlatform.OSX)
        && !RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD);

    /// <summary>
    ///     Determines whether the current operating system is macOS.
    /// </summary>
    /// <returns>true if the current operating system is macOS; otherwise, false.</returns>
    public static bool IsMac () => RuntimeInformation.IsOSPlatform (OSPlatform.OSX);
}
