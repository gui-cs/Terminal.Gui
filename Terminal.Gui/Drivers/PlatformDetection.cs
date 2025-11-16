using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
/// Helper class for detecting platform-specific features.
/// </summary>
internal static class PlatformDetection
{
    /// <summary>
    /// Determines if the current platform is WSL (Windows Subsystem for Linux).
    /// </summary>
    /// <returns>True if running on WSL, false otherwise.</returns>
    public static bool IsWSLPlatform ()
    {
        // xclip does not work on WSL, so we need to use the Windows clipboard via Powershell
        (int exitCode, string result) = ClipboardProcessRunner.Bash ("uname -a", waitForOutput: true);

        if (exitCode == 0 && result.Contains ("microsoft") && result.Contains ("WSL"))
        {
            return true;
        }

        return false;
    }
}
