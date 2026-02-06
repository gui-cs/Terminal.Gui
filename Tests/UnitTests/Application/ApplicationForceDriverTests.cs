using UnitTests;

namespace UnitTests.ApplicationTests;

public class ApplicationForceDriverTests : FakeDriverBase
{
    [Fact (Skip = "Bogus test now that config properties are handled correctly")]
    public void ForceDriver_Does_Not_Changes_If_It_Has_Valid_Value ()
    {
        Assert.False (Application.Initialized);
        Assert.Null (Application.Driver);
        Assert.Equal (string.Empty, Application.ForceDriver);

        Application.ForceDriver = "fake";
        Assert.Equal ("fake", Application.ForceDriver);

        Application.ForceDriver = "dotnet";
        Assert.Equal ("fake", Application.ForceDriver);
    }

    [Fact (Skip = "Bogus test now that config properties are handled correctly")]
    public void ForceDriver_Throws_If_Initialized_Changed_To_Another_Value ()
    {
        IDriver driver = CreateFakeDriver ();

        Assert.False (Application.Initialized);
        Assert.Null (Application.Driver);
        Assert.Equal (string.Empty, Application.ForceDriver);

        Application.Init (driverName: "fake");
        Assert.True (Application.Initialized);
        Assert.NotNull (Application.Driver);
        Assert.Equal ("fake", Application.Driver.GetName ());
        Assert.Equal (string.Empty, Application.ForceDriver);

        Assert.Throws<InvalidOperationException> (() => Application.ForceDriver = "dotnet");

        Application.ForceDriver = "fake";
        Assert.Equal ("fake", Application.ForceDriver);
    }
}
