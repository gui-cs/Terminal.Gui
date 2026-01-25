using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace DriverTests;

[Collection ("Driver Tests")]
public class PlatformDetectionTests (ITestOutputHelper output)
{
    [Fact]
    public void DetectPlatform_BasedOnOSDescription ()
    {
        string osDesc = RuntimeInformation.OSDescription.ToLowerInvariant ();
        bool isWSLExpected = false;

        if (osDesc.Contains ("linux"))
        {
            // Simple heuristic for WSL
            // Many WSL distributions include "microsoft" in uname -a
            // Since we cannot execute Bash in a unit test reliably, we simulate here
            isWSLExpected = osDesc.Contains ("microsoft") || PlatformDetection.IsWSL ();
        }

        switch (osDesc)
        {
            case var desc when desc.Contains ("windows"):
                Assert.True (PlatformDetection.IsWindows ());
                Assert.False (PlatformDetection.IsUnixLike ());
                Assert.False (PlatformDetection.IsLinux ());
                Assert.False (PlatformDetection.IsMac ());
                Assert.False (PlatformDetection.IsWSL ());
                break;

            case var desc when desc.Contains ("linux"):
                Assert.False (PlatformDetection.IsWindows ());
                Assert.True (PlatformDetection.IsUnixLike ());
                Assert.True (PlatformDetection.IsLinux ());
                Assert.False (PlatformDetection.IsMac ());
                Assert.Equal (isWSLExpected, PlatformDetection.IsWSL ());
                break;

            case var desc when desc.Contains ("darwin") || desc.Contains ("macos"):
                Assert.False (PlatformDetection.IsWindows ());
                Assert.True (PlatformDetection.IsUnixLike ());
                Assert.False (PlatformDetection.IsLinux ());
                Assert.True (PlatformDetection.IsMac ());
                Assert.False (PlatformDetection.IsWSL ());
                break;

            case var desc when desc.Contains ("freebsd"):
                Assert.False (PlatformDetection.IsWindows ());
                Assert.True (PlatformDetection.IsUnixLike ());
                Assert.False (PlatformDetection.IsLinux ());
                Assert.True (PlatformDetection.IsMac ());
                Assert.False (PlatformDetection.IsWSL ());
                break;

            default:
                // Fallback for other Unix-like or unknown systems
                Assert.False (PlatformDetection.IsWindows ());
                Assert.True (PlatformDetection.IsUnixLike ());
                Assert.False (PlatformDetection.IsLinux ());
                Assert.True (PlatformDetection.IsMac ());
                Assert.False (PlatformDetection.IsWSL ());
                output.WriteLine ($"Unknown OS Description: {osDesc}");
                break;
        }
    }
}