using Xunit;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.Drivers;

/// <summary>
/// Tests for FakeDriver functionality including basic driver operations.
/// These tests prove that FakeDriver can be used independently for testing Terminal.Gui applications.
/// </summary>
public class FakeDriverRenderingTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Basic Driver Tests

    [Fact]
    public void FakeDriver_Can_Write_To_Contents_Buffer ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act - Write directly to driver
        driver.Move (0, 0);
        driver.AddStr ("Hello World");

        // Assert - Verify text was written to driver contents
        Assert.NotNull (driver.Contents);
        
        // Check that "Hello World" is in the first row
        string firstRow = "";
        for (int col = 0; col < Math.Min (11, driver.Cols); col++)
        {
            firstRow += (char)driver.Contents [0, col].Rune.Value;
        }
        Assert.Equal ("Hello World", firstRow);

        driver.End ();
    }

    [Fact]
    public void FakeDriver_Can_Set_Attributes ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();
        var attr = new Attribute (Color.Red, Color.Blue);

        // Act
        driver.Move (5, 5);
        driver.SetAttribute (attr);
        driver.AddRune ('X');

        // Assert - Verify attribute was set
        Assert.NotNull (driver.Contents);
        Assert.Equal ('X', (char)driver.Contents [5, 5].Rune.Value);
        Assert.Equal (attr, driver.Contents [5, 5].Attribute);

        driver.End ();
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

    [Fact]
    public void FakeDriver_Can_Fill_Rectangle ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act
        driver.FillRect (new Rectangle (0, 0, 5, 3), '*');

        // Assert - Verify rectangle was filled
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                Assert.Equal ('*', (char)driver.Contents [row, col].Rune.Value);
            }
        }

        driver.End ();
    }

    [Fact]
    public void FakeDriver_Tracks_Cursor_Position ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act
        driver.Move (10, 5);

        // Assert
        Assert.Equal (10, driver.Col);
        Assert.Equal (5, driver.Row);

        driver.End ();
    }

    #endregion
}
