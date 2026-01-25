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
    ///     Determines whether the current operating system is a Unix-like platform.
    /// </summary>
    /// <remarks>
    ///     Unix-like platforms include operating systems that derive from or
    ///     closely follow traditional UNIX and POSIX design principles.
    ///     On .NET, this currently includes Linux, macOS (Darwin), and FreeBSD.
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the operating system is Linux, macOS, or FreeBSD;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsUnixLike () =>
        RuntimeInformation.IsOSPlatform (OSPlatform.Linux)
        || RuntimeInformation.IsOSPlatform (OSPlatform.OSX)
        || RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD);

    /// <summary>
    ///     Determines whether the current operating system is Linux.
    /// </summary>
    /// <remarks>
    ///     This method returns <see langword="true"/> only when running on a Linux
    ///     distribution. Other Unix-like platforms such as macOS and FreeBSD
    ///     return <see langword="false"/>.
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the operating system is Linux;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsLinux () => RuntimeInformation.IsOSPlatform (OSPlatform.Linux);

    /// <summary>
    ///     Determines whether the current operating system is macOS.
    /// </summary>
    /// <returns>true if the current operating system is macOS; otherwise, false.</returns>
    public static bool IsMac () => RuntimeInformation.IsOSPlatform (OSPlatform.OSX) || RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD);
}
