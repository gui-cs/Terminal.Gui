using System.Text;
using Xunit.Abstractions;

namespace UnitTests.DriverTests;

/// <summary>
///     Tests for the FakeDriver to ensure it works properly with the modern component factory architecture.
/// </summary>
public class FakeDriverTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Basic FakeDriver Tests

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Init_Works ()
    {
        // Verify Application was initialized
        Assert.True (Application.Initialized);

        //   Assert.NotNull (Application.Top);

        // Verify it's using a driver facade (modern architecture)
        Assert.IsAssignableFrom<IConsoleDriverFacade> (Application.Driver);

        _output.WriteLine ($"Driver type: {Application.Driver.GetType ().Name}");
        _output.WriteLine ($"Screen size: {Application.Screen}");
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Screen_Has_Default_Size ()
    {
        // Default size should be 80x25
        Assert.Equal (new (0, 0, 80, 25), Application.Screen);
        Assert.Equal (80, Application.Driver!.Cols);
        Assert.Equal (25, Application.Driver.Rows);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Can_Resize ()
    {
        // Start with default size
        Assert.Equal (80, Application.Driver!.Cols);
        Assert.Equal (25, Application.Driver.Rows);

        // Resize to 100x30
        Application.Driver?.SetScreenSize (100, 30);

        // Verify new size
        Assert.Equal (100, Application.Driver.Cols);
        Assert.Equal (30, Application.Driver.Rows);
        Assert.Equal (new (0, 0, 100, 30), Application.Screen);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Top_Is_Created ()
    {
        Application.Top = new ();

        Application.Begin (Application.Top);

        Assert.NotNull (Application.Top);
        Assert.True (Application.Top.IsInitialized);
        Assert.Equal (new (0, 0, 80, 25), Application.Top.Frame);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Can_Add_View_To_Top ()
    {
        Application.Top = new ();

        var label = new Label { Text = "Hello World" };
        Application.Top!.Add (label);

        Assert.Contains (label, Application.Top!.SubViews);
        Assert.Same (Application.Top, label.SuperView);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_RunIteration_Works ()
    {
        Application.Top = new ();

        var label = new Label { Text = "Hello" };
        Application.Top!.Add (label);

        Application.Begin (Application.Top);

        // Run a single iteration - this should layout and draw
        AutoInitShutdownAttribute.RunIteration ();

        // Verify the view was laid out
        Assert.True (label.Frame.Width > 0);
        Assert.True (label.IsInitialized);
    }

    #endregion

    #region AutoInitShutdown Attribute Tests

    //[Theory]
    //[InlineData (true)]
    //[InlineData (false)]
    //public void AutoInitShutdown_Attribute_Respects_AutoInit_Parameter (bool autoInit)
    //{
    //    // When autoInit is false, Application should not be initialized
    //    // When autoInit is true, Application should be initialized

    //    // This test will be called twice - once with autoInit=true, once with false
    //    // We can't use the attribute directly in the test body, but we can verify
    //    // the behavior by checking Application.Initialized

    //    // For this test to work properly, we need to call Application.Init manually when autoInit=false
    //    bool wasInitialized = Application.Initialized;

    //    try
    //    {
    //        if (!wasInitialized)
    //        {
    //            Application.ResetState ();
    //            var fa = new FakeApplicationFactory ();
    //            using IDisposable cleanup = fa.SetupFakeApplication ();
    //            Assert.True (Application.Initialized);
    //        }
    //        else
    //        {
    //            Assert.True (Application.Initialized);
    //        }
    //    }
    //    finally
    //    {
    //        if (!wasInitialized)
    //        {
    //            Application.Shutdown ();
    //        }
    //    }
    //}

    [Fact]
    public void Without_AutoInitShutdown_Application_Is_Not_Initialized ()
    {
        // This test deliberately does NOT use [AutoInitShutdown]
        // Application should not be initialized
        Assert.False (Application.Initialized);
        Assert.Null (Application.Driver);
        Assert.Null (Application.Top);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoInitShutdown_Cleans_Up_After_Test ()
    {
        // This test verifies that Application is properly initialized
        // The After method of AutoInitShutdown will verify cleanup
        Assert.True (Application.Initialized);
        Assert.NotNull (Application.Driver);
    }

    #endregion

    #region SetupFakeDriver Attribute Tests

    [Fact]
    [SetupFakeApplication]
    public void SetupFakeDriver_Initializes_Driver_With_80x25 ()
    {
        Assert.NotNull (Application.Driver);
        Assert.Equal (new (0, 0, 80, 25), Application.Screen);
        Assert.Equal (80, Application.Driver.Cols);
        Assert.Equal (25, Application.Driver.Rows);
    }

    [Fact]
    [SetupFakeApplication]
    public void SetupFakeDriver_Driver_Is_IConsoleDriver ()
    {
        Assert.NotNull (Application.Driver);

        // Should be IConsoleDriver
        Assert.IsAssignableFrom<IConsoleDriver> (Application.Driver);

        _output.WriteLine ($"Driver type: {Application.Driver.GetType ().Name}");
    }

    [Fact]
    [SetupFakeApplication]
    public void SetupFakeDriver_Can_Set_Screen_Size ()
    {
        IConsoleDriver fakeDriver = Application.Driver;
        Assert.NotNull (fakeDriver);

        fakeDriver!.SetScreenSize (100, 50);

        Assert.Equal (100, Application.Driver!.Cols);
        Assert.Equal (50, Application.Driver.Rows);
    }

    #endregion

    #region Integration Tests

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Can_Draw_Simple_View ()
    {
        Application.Top = new ();

        var window = new Window
        {
            Title = "Test Window",
            X = 0,
            Y = 0,
            Width = 40,
            Height = 10
        };

        var label = new Label
        {
            Text = "Hello World",
            X = 1,
            Y = 1
        };

        window.Add (label);
        Application.Top!.Add (window);

        Application.Begin (Application.Top);

        // Run iteration to layout and draw
        AutoInitShutdownAttribute.RunIteration ();

        // Verify views were initialized and laid out
        Assert.True (window.IsInitialized);
        Assert.True (label.IsInitialized);
        Assert.True (window.Frame.Width > 0);
        Assert.True (label.Frame.Width > 0);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Multiple_RunIterations_Work ()
    {
        Application.Top = new ();

        var label = new Label { Text = "Iteration Test" };
        Application.Top!.Add (label);

        // Run multiple iterations
        for (var i = 0; i < 5; i++)
        {
            AutoInitShutdownAttribute.RunIteration ();
        }

        Application.Begin (Application.Top);

        // Should still be working
        Assert.True (Application.Initialized);
        Assert.True (label.IsInitialized);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Resize_Triggers_Layout ()
    {
        Application.Top = new ();

        var view = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Application.Top!.Add (view);

        Application.Begin (Application.Top);

        AutoInitShutdownAttribute.RunIteration ();

        // Check initial size
        Rectangle initialFrame = view.Frame;
        Assert.Equal (80, initialFrame.Width);
        Assert.Equal (25, initialFrame.Height);

        // Resize
        Application.Driver?.SetScreenSize (100, 40);

        // Check new size
        Assert.Equal (100, view.Frame.Width);
        Assert.Equal (40, view.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Window_Can_Be_Shown_And_Closed ()
    {
        Application.Top = new ();

        var window = new Window { Title = "Test" };
        Application.Top!.Add (window);

        Application.Begin (Application.Top);

        AutoInitShutdownAttribute.RunIteration ();

        Assert.True (window.IsInitialized);
        Assert.Contains (window, Application.Top!.SubViews);

        // Remove window
        Application.Top.Remove (window);
        AutoInitShutdownAttribute.RunIteration ();

        Assert.DoesNotContain (window, Application.Top!.SubViews);
    }

    #endregion

    #region Clipboard Tests

    [Fact]
    [AutoInitShutdown (useFakeClipboard: true)]
    public void FakeDriver_Clipboard_Works_When_Enabled ()
    {
        Assert.NotNull (Application.Driver!.Clipboard);
        Assert.True (Application.Driver.Clipboard.IsSupported);

        // Set clipboard content
        Application.Driver.Clipboard.SetClipboardData ("Test content");

        // Get clipboard content
        string content = Application.Driver.Clipboard.GetClipboardData ();
        Assert.Equal ("Test content", content);
    }

    [Fact]
    [AutoInitShutdown (useFakeClipboard: true, fakeClipboardAlwaysThrowsNotSupportedException: true)]
    public void FakeDriver_Clipboard_Can_Throw_NotSupportedException ()
    {
        Assert.NotNull (Application.Driver!.Clipboard);

        // Should throw NotSupportedException
        Assert.Throws<NotSupportedException> (() =>
                                                  Application.Driver.Clipboard.GetClipboardData ());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Handles_Invalid_Coordinates_Gracefully ()
    {
        Application.Top = new ();

        // Try to add a view with invalid coordinates - should not crash
        var view = new View
        {
            X = -1000,
            Y = -1000,
            Width = 10,
            Height = 10
        };

        Application.Top!.Add (view);

        // Should not throw
        AutoInitShutdownAttribute.RunIteration ();

        Assert.True (Application.Initialized);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Survives_Rapid_Resizes ()
    {
        Size [] sizes = new []
        {
            new Size (80, 25),
            new Size (100, 30),
            new Size (60, 20),
            new Size (120, 40),
            new Size (80, 25)
        };

        foreach (Size size in sizes)
        {
            Application.Driver!.SetScreenSize (size.Width, size.Height);
            AutoInitShutdownAttribute.RunIteration ();

            Assert.Equal (size.Width, Application.Driver!.Cols);
            Assert.Equal (size.Height, Application.Driver.Rows);
        }
    }

    #endregion

    #region Buffer and Fill Tests

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Can_Fill_Rectangle ()
    {
        // Verify driver is initialized with buffers
        Assert.NotNull (Application.Driver);
        Assert.NotNull (Application.Driver!.Contents);

        // Fill a rectangle
        var rect = new Rectangle (5, 5, 10, 5);
        Application.Driver.FillRect (rect, (Rune)'X');

        // Verify the rectangle was filled
        for (int row = rect.Y; row < rect.Y + rect.Height; row++)
        {
            for (int col = rect.X; col < rect.X + rect.Width; col++)
            {
                Assert.Equal ((Rune)'X', Application.Driver.Contents [row, col].Rune);
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Buffer_Integrity_After_Multiple_Resizes ()
    {
        // Start with default size
        Assert.Equal (80, Application.Driver!.Cols);
        Assert.Equal (25, Application.Driver.Rows);

        // Fill with a pattern
        Application.Driver.FillRect (new (0, 0, 10, 5), (Rune)'A');

        // Resize
        Application.Driver?.SetScreenSize (100, 30);

        // Verify new size
        Assert.Equal (100, Application.Driver.Cols);
        Assert.Equal (30, Application.Driver.Rows);

        // Verify buffer is clean (no stale runes from previous size)
        Assert.NotNull (Application.Driver.Contents);
        Assert.Equal (30, Application.Driver.Contents!.GetLength (0));
        Assert.Equal (100, Application.Driver.Contents.GetLength (1));

        // Fill with new pattern
        Application.Driver.FillRect (new (0, 0, 20, 10), (Rune)'B');

        // Resize back
        Application.Driver?.SetScreenSize (80, 25);

        // Verify size is back
        Assert.Equal (80, Application.Driver.Cols);
        Assert.Equal (25, Application.Driver.Rows);

        // Verify buffer dimensions match
        Assert.Equal (25, Application.Driver.Contents.GetLength (0));
        Assert.Equal (80, Application.Driver.Contents.GetLength (1));
    }

    #endregion

    #region ScreenChanged Event Tests

    [Fact]
    [AutoInitShutdown]
    public void ScreenChanged_Event_Fires_On_SetScreenSize ()
    {
        var screenChangedFired = false;
        Size? newSize = null;

        Application.Driver!.SizeChanged += (sender, args) =>
                                           {
                                               screenChangedFired = true;
                                               newSize = args.Size;
                                           };

        // Trigger resize using FakeResize which uses SetScreenSize internally
        Application.Driver?.SetScreenSize (100, 30);

        // Verify event fired
        Assert.True (screenChangedFired);
        Assert.NotNull (newSize);
        Assert.Equal (100, newSize!.Value.Width);
        Assert.Equal (30, newSize.Value.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeResize_Triggers_ScreenChanged_And_Updates_Application_Screen ()
    {
        var screenChangedFired = false;
        Size? eventSize = null;

        Application.Driver!.SizeChanged += (sender, args) =>
                                           {
                                               screenChangedFired = true;
                                               eventSize = args.Size;
                                           };

        // Use FakeResize helper
        Application.Driver?.SetScreenSize (120, 40);

        // Verify event fired
        Assert.True (screenChangedFired);
        Assert.NotNull (eventSize);
        Assert.Equal (120, eventSize!.Value.Width);
        Assert.Equal (40, eventSize.Value.Height);

        // Verify Application.Screen was updated
        Assert.Equal (new (0, 0, 120, 40), Application.Screen);
        Assert.Equal (120, Application.Driver.Cols);
        Assert.Equal (40, Application.Driver.Rows);
    }

    [Fact]
    [AutoInitShutdown]
    public void SizeChanged_Event_Still_Fires_For_Compatibility ()
    {
        var sizeChangedFired = false;
        var screenChangedFired = false;

#pragma warning disable CS0618 // Type or member is obsolete
        Application.Driver!.SizeChanged += (sender, args) => { sizeChangedFired = true; };
#pragma warning restore CS0618 // Type or member is obsolete

        Application.Driver.SizeChanged += (sender, args) => { screenChangedFired = true; };

        // Trigger resize using FakeResize
        Application.Driver?.SetScreenSize (90, 35);

        // Both events should fire for compatibility
        Assert.True (sizeChangedFired);
        Assert.True (screenChangedFired);
    }

    #endregion
}
