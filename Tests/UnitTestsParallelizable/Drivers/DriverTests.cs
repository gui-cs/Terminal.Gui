using UnitTests;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.DriverTests;

public class DriverTests : FakeDriverBase
{
    [Theory]
    [InlineData (null, true)]
    [InlineData ("", true)]
    [InlineData ("a", true)]
    [InlineData ("👩‍❤️‍💋‍👨", false)]
    public void IsValidLocation (string text, bool positive)
    {
        IDriver driver = CreateFakeDriver ();
        driver.SetScreenSize (10, 10);

        // positive
        Assert.True (driver.IsValidLocation (text, 0, 0));
        Assert.True (driver.IsValidLocation (text, 1, 1));
        Assert.Equal (positive, driver.IsValidLocation (text, driver.Cols - 1, driver.Rows - 1));

        // negative
        Assert.False (driver.IsValidLocation (text, -1, 0));
        Assert.False (driver.IsValidLocation (text, 0, -1));
        Assert.False (driver.IsValidLocation (text, -1, -1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows));

        // Define a clip rectangle
        driver.Clip = new (new Rectangle (5, 5, 5, 5));

        // positive
        Assert.True (driver.IsValidLocation (text, 5, 5));
        Assert.Equal (positive, driver.IsValidLocation (text, 9, 9));

        // negative
        Assert.False (driver.IsValidLocation (text, 4, 5));
        Assert.False (driver.IsValidLocation (text, 5, 4));
        Assert.False (driver.IsValidLocation (text, 10, 9));
        Assert.False (driver.IsValidLocation (text, 9, 10));
        Assert.False (driver.IsValidLocation (text, -1, 0));
        Assert.False (driver.IsValidLocation (text, 0, -1));
        Assert.False (driver.IsValidLocation (text, -1, -1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows));
    }
}
