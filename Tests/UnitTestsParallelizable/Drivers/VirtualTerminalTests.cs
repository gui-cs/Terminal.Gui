#nullable enable
using UnitTests;

namespace DriverTests;

public class VirtualTerminalTests : FakeDriverBase
{
    [Fact]
    public void IsVirtualTerminal_Returns_Expected_Values ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);

        driver.IsVirtualTerminal = false;
        Assert.False (driver.IsVirtualTerminal);
    }

    [Fact]
    public void IsVirtualTerminal_True_Force16Colors_True_False ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);
        Assert.False (driver.Force16Colors);

        driver.Force16Colors = true;
        Assert.True (driver.IsVirtualTerminal);
        Assert.True (driver.Force16Colors);
    }

    [Fact]
    public void IsVirtualTerminal_False_Force16Colors_Is_Always_True ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);
        Assert.False (driver.Force16Colors);

        driver.IsVirtualTerminal = false;
        Assert.True (driver.Force16Colors);

        driver.Force16Colors = false;
        Assert.True (driver.Force16Colors);
    }

    [Fact]
    public void IsVirtualTerminal_True_False_SupportsTrueColor_Is_Always_True_False ()
    {
        DriverImpl? driver = CreateFakeDriver () as DriverImpl;
        Assert.NotNull (driver?.IsVirtualTerminal);
        Assert.True (driver.IsVirtualTerminal);
        Assert.True (driver.SupportsTrueColor);

        driver.IsVirtualTerminal = false;
        Assert.False (driver.SupportsTrueColor);
    }
}
