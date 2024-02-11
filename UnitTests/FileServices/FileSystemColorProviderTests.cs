namespace Terminal.Gui.FileServicesTests;

public class FileSystemColorProviderTests
{
    [Fact]
    public void CanConstruct ()
    {
        var prov = new FileSystemColorProvider ();
        Assert.NotNull (prov);
    }
}
