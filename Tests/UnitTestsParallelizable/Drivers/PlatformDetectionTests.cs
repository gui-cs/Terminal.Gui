using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace DriverTests;

[Collection ("Driver Tests")]
public class PlatformDetectionTests (ITestOutputHelper output)
{
    [Fact]
    public void DetectPlatform_BasedOnOSDescription ()
    {
        bool isWSLExpected = PlatformDetection.IsWSL ();

        if (OperatingSystem.IsWindows ())
        {
            Assert.False (isWSLExpected);
            Assert.True (PlatformDetection.IsWindows ());
            Assert.False (PlatformDetection.IsUnixLike ());
            Assert.False (PlatformDetection.IsLinux ());
            Assert.False (PlatformDetection.IsMac ());
        }
        else if (OperatingSystem.IsLinux ())
        {
            Assert.Equal (isWSLExpected, PlatformDetection.IsWSL ());
            Assert.False (PlatformDetection.IsWindows ());
            Assert.True (PlatformDetection.IsUnixLike ());
            Assert.True (PlatformDetection.IsLinux ());
            Assert.False (PlatformDetection.IsMac ());
        }
        else if (OperatingSystem.IsMacOS ())
        {
            Assert.False (isWSLExpected);
            Assert.False (PlatformDetection.IsWindows ());
            Assert.True (PlatformDetection.IsUnixLike ());
            Assert.False (PlatformDetection.IsLinux ());
            Assert.True (PlatformDetection.IsMac ());
        }
        else
        {
            // Fallback for other Unix-like or unknown systems
            Assert.False (isWSLExpected);
            Assert.False (PlatformDetection.IsWindows ());
            Assert.True (PlatformDetection.IsUnixLike ());
            Assert.False (PlatformDetection.IsLinux ());
            Assert.True (PlatformDetection.IsMac ());
            output.WriteLine ($"Unknown OS Description: {RuntimeInformation.OSDescription}");
        }
    }
}
