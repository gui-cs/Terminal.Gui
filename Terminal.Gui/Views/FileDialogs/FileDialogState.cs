using System.IO.Abstractions;

namespace Terminal.Gui.Views;

internal class FileDialogState
{
    protected readonly FileDialog Parent;

    public FileDialogState (IDirectoryInfo dir, FileDialog parent)
    {
        Directory = dir;
        Parent = parent;
        Path = parent.Path;

        RefreshChildren ();
    }

    public FileSystemInfoStats [] Children { get; internal set; }
    public IDirectoryInfo Directory { get; }

    /// <summary>Gets what was entered in the path text box of the dialog when the state was active.</summary>
    public string Path { get; }

    public FileSystemInfoStats Selected { get; set; }

    protected virtual IEnumerable<FileSystemInfoStats> GetChildren (IDirectoryInfo dir)
    {
        try
        {
            List<FileSystemInfoStats> children;

            // if directories only
            if (Parent.OpenMode == OpenMode.Directory)
            {
                children = dir.GetDirectories ()
                              .Select (e => new FileSystemInfoStats (e, Parent.Style.Culture))
                              .ToList ();
            }
            else
            {
                children = dir.GetFileSystemInfos ()
                              .Select (e => new FileSystemInfoStats (e, Parent.Style.Culture))
                              .ToList ();
            }

            // if only allowing specific file types
            if (Parent.AllowedTypes.Any () && Parent.OpenMode == OpenMode.File)
            {
                children = children.Where (
                                           c => c.IsDir
                                                || (c.FileSystemInfo is IFileInfo f
                                                    && Parent.IsCompatibleWithAllowedExtensions (f))
                                          )
                                   .ToList ();
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
            return Enumerable.Empty<FileSystemInfoStats> ();
        }
    }

    protected bool MatchesApiFilter (FileSystemInfoStats arg)
    {
        return arg.IsDir
               || (arg.FileSystemInfo is IFileInfo f
                   && Parent.CurrentFilter.IsAllowed (f.FullName));
    }

    internal virtual void RefreshChildren ()
    {
        IDirectoryInfo dir = Directory;
        Children = GetChildren (dir).ToArray ();
    }
}
