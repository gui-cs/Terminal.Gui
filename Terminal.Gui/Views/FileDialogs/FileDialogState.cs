using System.IO.Abstractions;

namespace Terminal.Gui.Views;

internal class FileDialogState
{
    public FileDialogState (IDirectoryInfo dir, FileDialog parent)
    {
        Parent = parent;
        Directory = dir;
        Children = GetChildren (Directory).ToArray ();
        Path = parent.Path;
    }

    protected FileDialog Parent { get; }

    public FileSystemInfoStats [] Children { get; internal set; }
    public IDirectoryInfo Directory { get; }

    /// <summary>Gets what was entered in the path text box of the dialog when the state was active.</summary>
    public string Path { get; }

    public FileSystemInfoStats? Selected { get; set; }

    protected IEnumerable<FileSystemInfoStats> GetChildren (IDirectoryInfo dir)
    {
        try
        {
            List<FileSystemInfoStats> children;

            // if directories only
            if (Parent.OpenMode == OpenMode.Directory)
            {
                children = dir.GetDirectories ().Select (e => new FileSystemInfoStats (e, Parent.Style.Culture)).ToList ();
            }
            else
            {
                children = dir.GetFileSystemInfos ().Select (e => new FileSystemInfoStats (e, Parent.Style.Culture)).ToList ();
            }

            // if only allowing specific file types
            if (Parent.AllowedTypes.Count > 0 && Parent.OpenMode == OpenMode.File)
            {
                children = children.Where (c => c.IsDir || (c.FileSystemInfo is IFileInfo f && Parent.IsCompatibleWithAllowedExtensions (f))).ToList ();
            }

            // if there's a UI filter in place too
            if (Parent.CurrentFilter is { })
            {
                children = children.Where (MatchesApiFilter).ToList ();
            }

            // allow navigating up as '..'
            if (dir.Parent is { })
            {
                children.Add (new FileSystemInfoStats (dir.Parent, Parent.Style.Culture) { IsParent = true });
            }

            return children;
        }
        catch (Exception)
        {
            // Access permissions Exceptions, Dir not exists etc
            return [];
        }
    }

    protected bool MatchesApiFilter (FileSystemInfoStats arg) =>
        Parent.CurrentFilter is { } && (arg.IsDir || (arg.FileSystemInfo is IFileInfo f && Parent.CurrentFilter.IsAllowed (f.FullName)));

    internal virtual void RefreshChildren () => Children = GetChildren (Directory).ToArray ();
}
