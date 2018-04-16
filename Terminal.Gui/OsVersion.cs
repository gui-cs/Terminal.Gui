#if netstandard1_6

using System;
using System.IO;

namespace Terminal.Gui
{
    internal enum OsPlatform
    {
        Windows,
        MacOsX,
        Linux,
        Unknown
    }

    /// <summary>
    /// To add support for NetStandard use a managed code approach until
    /// System.Environment.OSVersion.Platform is available in NetStandard
    /// </summary>
    internal class OsVersion
    {
        private static OsVersion _current;

        public static OsVersion Current => _current = _current = new OsVersion();

        public OsVersion()
        {
            Platform = GetPlatform();
        }

        public OsVersion(OsPlatform platform)
        {
            Platform = platform;
        }

        public OsPlatform Platform { get; }

        /// <summary>
        /// a solution based off of 
        /// https://stackoverflow.com/questions/38790802/determine-operating-system-in-net-core?#answer-38795621
        /// </summary>
        /// <returns></returns>
        private OsPlatform GetPlatform()
        {
            const string linuxFile = @"/proc/sys/kernel/ostype";

            var windowsDirectory = Environment.GetEnvironmentVariable("windir");
            
            if (!string.IsNullOrEmpty(windowsDirectory) && windowsDirectory.Contains(@"\") && Directory.Exists(windowsDirectory))
            {
                return OsPlatform.Windows;
            }
            else if (File.Exists(linuxFile))
            {
                var osType = File.ReadAllText(linuxFile);

                if (osType.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
                {
                    // Note: Android gets here too
                    return OsPlatform.Linux;
                }

                // log this
                //throw new UnsupportedPlatformException(osType);
            }
            else if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist"))
            {
                // Note: iOS gets here too
                return OsPlatform.MacOsX;
            }

            return OsPlatform.Unknown;
        }
    }
}

#endif