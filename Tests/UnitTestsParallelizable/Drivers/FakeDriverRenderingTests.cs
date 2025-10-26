using Xunit;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.Drivers;

/// <summary>
/// Tests for FakeDriver functionality including rendering and basic driver operations.
/// These tests prove that FakeDriver can be used independently for testing Terminal.Gui applications.
/// </summary>
public class FakeDriverRenderingTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region View Rendering Tests

    [Fact]
    public void FakeDriver_Can_Render_Simple_Label ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        var label = new Label { Text = "Hello World", X = 0, Y = 0 };
        label.Driver = driver;
        label.BeginInit ();
        label.EndInit ();

        // Act
        label.SetNeedsDraw ();
        label.Draw ();

        // Assert
        Assert.NotNull (driver.Contents);
        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);

        driver.End ();
        label.Dispose ();
    }

    [Fact]
    public void FakeDriver_Can_Render_View_With_Border ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        var window = new Window
        {
            Title = "Test Window",
            X = 0,
            Y = 0,
            Width = 40,
            Height = 10,
            BorderStyle = LineStyle.Single
        };
        window.Driver = driver;
        window.BeginInit ();
        window.EndInit ();

        // Act
        window.SetNeedsDraw ();
        window.Draw ();

        // Assert - Check that contents buffer was written to
        Assert.NotNull (driver.Contents);
        
        driver.End ();
        window.Dispose ();
    }

    [Fact]
    public void FakeDriver_Default_Screen_Size ()
    {
        // Arrange & Act
        var driver = new FakeDriver ();
        driver.Init ();

        // Assert
        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);

        driver.End ();
    }

    [Fact]
    public void FakeDriver_Can_Change_Screen_Size ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act
        driver.SetBufferSize (120, 40);

        // Assert
        Assert.Equal (120, driver.Cols);
        Assert.Equal (40, driver.Rows);

        driver.End ();
    }

    #endregion
}
