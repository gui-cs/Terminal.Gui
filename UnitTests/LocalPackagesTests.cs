namespace Terminal.Gui;

public class LocalPackagesTests
{
    private readonly string _localPackagesPath;

    public LocalPackagesTests ()
    {
        // Define the local_packages path relative to the solution directory
        _localPackagesPath = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..", "..", "local_packages");
    }

    [Fact]
    public void LocalPackagesFolderExists ()
    {
        Assert.True (Directory.Exists (_localPackagesPath),
                     $"The local_packages folder does not exist: {_localPackagesPath}");
    }

    [Fact]
    public void NupkgFilesExist ()
    {
        var nupkgFiles = Directory.GetFiles (_localPackagesPath, "*.nupkg");
        Assert.NotEmpty (nupkgFiles);
    }

    [Fact]
    public void SnupkgFilesExist ()
    {
        var snupkgFiles = Directory.GetFiles (_localPackagesPath, "*.snupkg");
        Assert.NotEmpty (snupkgFiles);
    }
}