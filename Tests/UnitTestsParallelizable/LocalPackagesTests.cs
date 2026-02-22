namespace BuildAndDeployTests;

public class LocalPackagesTests
{
    private readonly string _localPackagesPath = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..", "..", "..", "local_packages");

    // Define the local_packages path relative to the solution directory

    [Fact]
    public void LocalPackagesFolderExists () =>
        Assert.True (Directory.Exists (_localPackagesPath), $"The local_packages folder does not exist: {_localPackagesPath}");

    [Fact]
    public void NupkgFilesExist ()
    {
        string [] nupkgFiles = Directory.GetFiles (_localPackagesPath, "*.nupkg");
        Assert.NotEmpty (nupkgFiles);
    }

    [Fact]
    public void SnupkgFilesExist ()
    {
        string [] snupkgFiles = Directory.GetFiles (_localPackagesPath, "*.snupkg");
        Assert.NotEmpty (snupkgFiles);
    }
}
