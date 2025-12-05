// Alias Console to MockConsole so we don't accidentally use Console

using UnitTests;

namespace DriverTests;

public class DriverColorTests : FakeDriverBase
{
    [Fact]
    public void Force16Colors_Sets ()
    {
        // Set the static property before creating the driver
        Terminal.Gui.Drivers.Driver.Force16Colors = true;
        IDriver driver = CreateFakeDriver ();

        Assert.True (driver.Force16Colors);
        Assert.True (driver.GetForce16Colors ());

        driver.End ();
        
        // Reset for other tests
        Terminal.Gui.Drivers.Driver.Force16Colors = false;
    }
}
