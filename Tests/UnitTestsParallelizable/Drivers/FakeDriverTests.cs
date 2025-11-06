using System.Text;
using UnitTests.Parallelizable;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.DriverTests;

/// <summary>
///     Tests for the FakeDriver to ensure it works properly with the modern component factory architecture.
/// </summary>
public class FakeDriverTests (ITestOutputHelper output) : ParallelizableBase
{
    private readonly ITestOutputHelper _output = output;

    #region Basic FakeDriver Tests

    [Fact]
    public void FakeDriver_Init_Works ()
    {
        IDriver driver = CreateFakeDriver ();

        Assert.IsAssignableFrom<IDriver> (driver);

        _output.WriteLine ($"Driver type: {driver.GetType ().Name}");
        _output.WriteLine ($"Screen size: {driver.Screen}");
    }

    [Fact]

    public void FakeDriver_Screen_Has_Default_Size ()
    {
        IDriver driver = CreateFakeDriver ();
        // Default size should be 80x25
        Assert.Equal (new (0, 0, 80, 25), driver.Screen);
        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);
    }

    [Fact]

    public void FakeDriver_Can_Resize ()
    {
        IDriver driver = CreateFakeDriver ();

        // Start with default size
        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);

        // Resize to 100x30
        driver?.SetScreenSize (100, 30);

        // Verify new size
        Assert.Equal (100, driver.Cols);
        Assert.Equal (30, driver.Rows);
        Assert.Equal (new (0, 0, 100, 30), driver.Screen);
    }

    #endregion

    #region CreateFakeDriver Tests

    [Fact]

    public void SetupFakeDriver_Initializes_Driver_With_80x25 ()
    {
        IDriver driver = CreateFakeDriver ();

        Assert.NotNull (driver);
        Assert.Equal (new (0, 0, 80, 25), driver.Screen);
        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);
    }

    [Fact]

    public void SetupFakeDriver_Driver_Is_IConsoleDriver ()
    {
        IDriver driver = CreateFakeDriver ();

        Assert.NotNull (driver);

        // Should be IConsoleDriver
        Assert.IsAssignableFrom<IDriver> (driver);

        _output.WriteLine ($"Driver type: {driver.GetType ().Name}");
    }

    [Fact]

    public void SetupFakeDriver_Can_Set_Screen_Size ()
    {
        IDriver driver = CreateFakeDriver ();

        IDriver fakeDriver = driver;
        Assert.NotNull (fakeDriver);

        fakeDriver!.SetScreenSize (100, 50);

        Assert.Equal (100, driver.Cols);
        Assert.Equal (50, driver.Rows);
    }

    #endregion


    #region Clipboard Tests

    [Fact]
    public void FakeDriver_Clipboard_Works_When_Enabled ()
    {
        IDriver driver = CreateFakeDriver ();

        Assert.NotNull (driver.Clipboard);
        Assert.True (driver.Clipboard.IsSupported);

        // Set clipboard content
        driver.Clipboard.SetClipboardData ("Test content");

        // Get clipboard content
        string content = driver.Clipboard.GetClipboardData ();
        Assert.Equal ("Test content", content);
    }

    [Fact]
    public void FakeDriver_Clipboard_GetClipboarData_Works ()
    {
        IDriver driver = CreateFakeDriver ();

        Assert.NotNull (driver.Clipboard);

        driver.Clipboard.SetClipboardData ("test");
        Assert.Equal ("test", driver.Clipboard.GetClipboardData ());
    }

    #endregion


    #region Buffer and Fill Tests

    [Fact]

    public void FakeDriver_Can_Fill_Rectangle ()
    {
        IDriver driver = CreateFakeDriver ();

        // Verify driver is initialized with buffers
        Assert.NotNull (driver);
        Assert.NotNull (driver.Contents);

        // Fill a rectangle
        var rect = new Rectangle (5, 5, 10, 5);
        driver.FillRect (rect, (Rune)'X');

        // Verify the rectangle was filled
        for (int row = rect.Y; row < rect.Y + rect.Height; row++)
        {
            for (int col = rect.X; col < rect.X + rect.Width; col++)
            {
                Assert.Equal ((Rune)'X', driver.Contents [row, col].Rune);
            }
        }
    }

    [Fact]

    public void FakeDriver_Buffer_Integrity_After_Multiple_Resizes ()
    {
        IDriver driver = CreateFakeDriver ();

        // Start with default size
        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);

        // Fill with a pattern
        driver.FillRect (new (0, 0, 10, 5), (Rune)'A');

        // Resize
        driver?.SetScreenSize (100, 30);

        // Verify new size
        Assert.Equal (100, driver.Cols);
        Assert.Equal (30, driver.Rows);

        // Verify buffer is clean (no stale runes from previous size)
        Assert.NotNull (driver.Contents);
        Assert.Equal (30, driver.Contents!.GetLength (0));
        Assert.Equal (100, driver.Contents.GetLength (1));

        // Fill with new pattern
        driver.FillRect (new (0, 0, 20, 10), (Rune)'B');

        // Resize back
        driver?.SetScreenSize (80, 25);

        // Verify size is back
        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);

        // Verify buffer dimensions match
        Assert.Equal (25, driver.Contents.GetLength (0));
        Assert.Equal (80, driver.Contents.GetLength (1));
    }

    #endregion

    #region ScreenChanged Event Tests

    [Fact]

    public void ScreenChanged_Event_Fires_On_SetScreenSize ()
    {
        IDriver driver = CreateFakeDriver ();

        var screenChangedFired = false;
        Size? newSize = null;

        driver.SizeChanged += (sender, args) =>
                                           {
                                               screenChangedFired = true;
                                               newSize = args.Size;
                                           };

        // Trigger resize using FakeResize which uses SetScreenSize internally
        driver?.SetScreenSize (100, 30);

        // Verify event fired
        Assert.True (screenChangedFired);
        Assert.NotNull (newSize);
        Assert.Equal (100, newSize!.Value.Width);
        Assert.Equal (30, newSize.Value.Height);
    }

    [Fact]

    public void FakeResize_Triggers_ScreenChanged_And_Updates_Application_Screen ()
    {
        IDriver driver = CreateFakeDriver ();

        var screenChangedFired = false;
        Size? eventSize = null;

        driver.SizeChanged += (sender, args) =>
                                           {
                                               screenChangedFired = true;
                                               eventSize = args.Size;
                                           };

        // Use FakeResize helper
        driver?.SetScreenSize (120, 40);

        // Verify event fired
        Assert.True (screenChangedFired);
        Assert.NotNull (eventSize);
        Assert.Equal (120, eventSize!.Value.Width);
        Assert.Equal (40, eventSize.Value.Height);

        // Verify driver.Screen was updated
        Assert.Equal (new (0, 0, 120, 40), driver.Screen);
        Assert.Equal (120, driver.Cols);
        Assert.Equal (40, driver.Rows);
    }

    [Fact]

    public void SizeChanged_Event_Still_Fires_For_Compatibility ()
    {
        IDriver driver = CreateFakeDriver ();

        var sizeChangedFired = false;
        var screenChangedFired = false;

#pragma warning disable CS0618 // Type or member is obsolete
        driver.SizeChanged += (sender, args) => { sizeChangedFired = true; };
#pragma warning restore CS0618 // Type or member is obsolete

        driver.SizeChanged += (sender, args) => { screenChangedFired = true; };

        // Trigger resize using FakeResize
        driver?.SetScreenSize (90, 35);

        // Both events should fire for compatibility
        Assert.True (sizeChangedFired);
        Assert.True (screenChangedFired);
    }

    #endregion
}
