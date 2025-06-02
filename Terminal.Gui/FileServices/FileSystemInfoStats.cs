using System.Globalization;
using System.IO.Abstractions;

namespace Terminal.Gui.FileServices;

/// <summary>
///     Wrapper for <see cref="FileSystemInfo"/> that contains additional information (e.g. <see cref="IsParent"/>)
///     and helper methods.
/// </summary>
internal class FileSystemInfoStats
{
    /* ---- Colors used by the ls command line tool ----
     *
     * Blue: Directory
     * Green: Executable or recognized data file
     * Cyan (Sky Blue): Symbolic link file
     * Yellow with black background: Device
     * Magenta (Pink): Graphic image file
     * Red: Archive file
     * Red with black background: Broken link
     */
    private const long ByteConversion = 1024;
    private static readonly List<string> ExecutableExtensions = new () { ".EXE", ".BAT" };

    private static readonly List<string> ImageExtensions = new ()
    {
        ".JPG",
        ".JPEG",
        ".JPE",
        ".BMP",
        ".GIF",
        ".PNG"
    };

    private static readonly string [] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

    /// <summary>Initializes a new instance of the <see cref="FileSystemInfoStats"/> class.</summary>
    /// <param name="fsi">The directory of path to wrap.</param>
    /// <param name="culture"></param>
    public FileSystemInfoStats (IFileSystemInfo fsi, CultureInfo culture)
    {
        FileSystemInfo = fsi;
        LastWriteTime = fsi.LastWriteTime;

        if (fsi is IFileInfo fi)
        {
            MachineReadableLength = fi.Length;
            HumanReadableLength = GetHumanReadableFileSize (MachineReadableLength, culture);
            Type = fi.Extension;
        }
        else
        {
            HumanReadableLength = string.Empty;
            Type = $"<{Strings.fdDirectory}>";
            IsDir = true;
        }
    }

    /// <summary>Gets the wrapped <see cref="FileSystemInfo"/> (directory or file).</summary>
    public IFileSystemInfo FileSystemInfo { get; }

    public string HumanReadableLength { get; }
    public bool IsDir { get; }

    /// <summary>Gets or Sets a value indicating whether this instance represents the parent of the current state (i.e. "..").</summary>
    public bool IsParent { get; internal set; }

    public DateTime? LastWriteTime { get; }
    public long MachineReadableLength { get; }
    public string Name => IsParent ? ".." : FileSystemInfo.Name;
    public string Type { get; }

    public bool IsExecutable ()
    {
        // TODO: handle linux executable status
        return FileSystemInfo is IFileSystemInfo f
               && ExecutableExtensions.Contains (
                                                 f.Extension,
                                                 StringComparer.InvariantCultureIgnoreCase
                                                );
    }

    public bool IsImage ()
    {
        return FileSystemInfo is IFileSystemInfo f
               && ImageExtensions.Contains (
                                            f.Extension,
                                            StringComparer.InvariantCultureIgnoreCase
                                           );
    }

    private static string GetHumanReadableFileSize (long value, CultureInfo culture)
    {
        if (value < 0)
        {
            return "-" + GetHumanReadableFileSize (-value, culture);
        }

        if (value == 0)
        {
            return "0.0 B";
        }

        var mag = (int)Math.Log (value, ByteConversion);
        double adjustedSize = value / Math.Pow (1000, mag);

        return string.Format (culture.NumberFormat, "{0:n2} {1}", adjustedSize, SizeSuffixes [mag]);
    }
}
