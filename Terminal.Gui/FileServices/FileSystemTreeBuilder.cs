#nullable disable
using System.IO.Abstractions;

namespace Terminal.Gui.FileServices;

/// <summary>TreeView builder for creating file system based trees.</summary>
public class FileSystemTreeBuilder : ITreeBuilder<IFileSystemInfo>, IComparer<IFileSystemInfo>
{
    /// <summary>Creates a new instance of the <see cref="FileSystemTreeBuilder"/> class.</summary>
    public FileSystemTreeBuilder () => Sorter = this;

    /// <summary>Gets or sets a flag indicating whether to show files as leaf elements in the tree. Defaults to true.</summary>
    public bool IncludeFiles { get; set; } = true;

    /// <summary>Gets or sets the order of directory children.  Defaults to <see langword="this"/>.</summary>
    public IComparer<IFileSystemInfo> Sorter { get; set; }

    /// <inheritdoc/>
    public int Compare (IFileSystemInfo x, IFileSystemInfo y)
    {
        if (x is IDirectoryInfo && y is not IDirectoryInfo)
        {
            return -1;
        }

        if (x is not IDirectoryInfo && y is IDirectoryInfo)
        {
            return 1;
        }

        if (x is { } && y is { })
        {
            return string.Compare (x.Name, y.Name, StringComparison.Ordinal);
        }

        return 0;
    }

    /// <inheritdoc/>
    public bool SupportsCanExpand => true;

    /// <inheritdoc/>
    public bool CanExpand (IFileSystemInfo toExpand)
    {
        if (toExpand is IFileInfo)
        {
            return false;
        }

        if (IsReparsePoint (toExpand))
        {
            return false;
        }

        return TryGetChildren (toExpand).Any ();
    }

    /// <inheritdoc/>
    public IEnumerable<IFileSystemInfo> GetChildren (IFileSystemInfo forObject) => TryGetChildren (forObject).OrderBy (k => k, Sorter);

    private IEnumerable<IFileSystemInfo> TryGetChildren (IFileSystemInfo entry)
    {
        if (entry is IFileInfo)
        {
            return Enumerable.Empty<IFileSystemInfo> ();
        }

        // Prevent traversal cycles through symlinks/junctions/mount points.
        if (IsReparsePoint (entry))
        {
            return Enumerable.Empty<IFileSystemInfo> ();
        }

        var dir = (IDirectoryInfo)entry;

        try
        {
            return dir.GetFileSystemInfos ().Where (e => IncludeFiles || e is IDirectoryInfo);
        }
        catch (Exception)
        {
            return Enumerable.Empty<IFileSystemInfo> ();
        }
    }

    private static bool IsReparsePoint (IFileSystemInfo entry) => (entry.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
}
