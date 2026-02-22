using System.IO.Abstractions;

namespace Terminal.Gui.Views;

internal class FileSystemCollectionNavigationMatcher : DefaultCollectionNavigatorMatcher
{

    public override bool IsMatch (string search, object? value)
    {
        if(value is IFileSystemInfo fsi)
        {
            return fsi.Name.StartsWith (search, Comparer);
        }

        return base.IsMatch (search, value);
    }
}