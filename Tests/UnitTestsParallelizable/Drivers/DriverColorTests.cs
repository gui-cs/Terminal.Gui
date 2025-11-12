// Alias Console to MockConsole so we don't accidentally use Console

using UnitTests;

namespace UnitTests_Parallelizable.DriverTests;

public class DriverColorTests : FakeDriverBase
{
    [Fact]
    public void Force16Colors_Sets ()
    {
        IDriver driver = CreateFakeDriver ();

        driver.Force16Colors = true;
        Assert.True (driver.Force16Colors);

        driver.End ();
    }
}
