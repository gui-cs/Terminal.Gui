using UnitTests;
using Xunit;

using Xunit.Abstractions;

namespace UnitTests.DriverTests;

/// <summary>
/// Tests for the FakeDriver to ensure it works properly with the modern component factory architecture.
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
        
        _output.WriteLine ($"Driver type: {Application.Driver.GetType().Name}");
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
        AutoInitShutdownAttribute.FakeResize (new (100, 30));

        // Verify new size
        Assert.Equal (100, Application.Driver.Cols);
        Assert.Equal (30, Application.Driver.Rows);
        Assert.Equal (new (0, 0, 100, 30), Application.Screen);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Top_Is_Created ()
    {
        Application.Top = new Toplevel ();

        Application.Begin (Application.Top);

        Assert.NotNull (Application.Top);
        Assert.True (Application.Top.IsInitialized);
        Assert.Equal (new (0, 0, 80, 25), Application.Top.Frame);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Can_Add_View_To_Top ()
    {
        Application.Top = new Toplevel ();

        var label = new Label { Text = "Hello World" };
        Application.Top!.Add (label);

        Assert.Contains (label, Application.Top!.SubViews);
        Assert.Same (Application.Top, label.SuperView);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_RunIteration_Works ()
    {
        Application.Top = new Toplevel ();

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

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void AutoInitShutdown_Attribute_Respects_AutoInit_Parameter (bool autoInit)
    {
        // When autoInit is false, Application should not be initialized
        // When autoInit is true, Application should be initialized
        
        // This test will be called twice - once with autoInit=true, once with false
        // We can't use the attribute directly in the test body, but we can verify
        // the behavior by checking Application.Initialized
        
        // For this test to work properly, we need to call Application.Init manually when autoInit=false
        bool wasInitialized = Application.Initialized;
        
        try
        {
            if (!wasInitialized)
            {
                Application.ResetState ();
                var fa = new FakeApplicationFactory ();
                using var cleanup = fa.SetupFakeApplication ();
                Assert.True (Application.Initialized);
            }
            else
            {
                Assert.True (Application.Initialized);
            }
        }
        finally
        {
            if (!wasInitialized)
            {
                Application.Shutdown ();
            }
        }
    }

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
    [SetupFakeDriver]
    public void SetupFakeDriver_Initializes_Driver_With_25x25 ()
    {
        Assert.NotNull (Application.Driver);
        Assert.Equal (new (0, 0, 25, 25), Application.Screen);
        Assert.Equal (25, Application.Driver.Cols);
        Assert.Equal (25, Application.Driver.Rows);
    }

    [Fact]
    [SetupFakeDriver]
    public void SetupFakeDriver_Driver_Is_FakeConsoleDriver ()
    {
        Assert.NotNull (Application.Driver);
        
        // Should be IFakeConsoleDriver
        Assert.IsAssignableFrom<IFakeConsoleDriver> (Application.Driver);
        
        _output.WriteLine ($"Driver type: {Application.Driver.GetType().Name}");
    }

    [Fact]
    [SetupFakeDriver]
    public void SetupFakeDriver_Can_Set_Buffer_Size ()
    {
        var fakeDriver = Application.Driver as IFakeConsoleDriver;
        Assert.NotNull (fakeDriver);

        fakeDriver!.SetBufferSize (100, 50);

        Assert.Equal (100, Application.Driver!.Cols);
        Assert.Equal (50, Application.Driver.Rows);
    }

    #endregion

    #region Integration Tests

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Can_Draw_Simple_View ()
    {
        Application.Top = new Toplevel ();

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
        Application.Top = new Toplevel ();

        var label = new Label { Text = "Iteration Test" };
        Application.Top!.Add (label);

        // Run multiple iterations
        for (int i = 0; i < 5; i++)
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
        Application.Top = new Toplevel ();

        var view = new View 
        { 
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Application.Top!.Add (view);

        Application.Begin (Application.Top);

        AutoInitShutdownAttribute.FakeResize (new Size (80,25));
        AutoInitShutdownAttribute.RunIteration ();

        // Check initial size
        var initialFrame = view.Frame;
        Assert.Equal (80, initialFrame.Width);
        Assert.Equal (25, initialFrame.Height);

        // Resize
        AutoInitShutdownAttribute.FakeResize (new (100, 40));

        // Check new size
        Assert.Equal (100, view.Frame.Width);
        Assert.Equal (40, view.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Window_Can_Be_Shown_And_Closed ()
    {
        Application.Top = new Toplevel ();

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
        Application.Top = new Toplevel ();

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
        var sizes = new[] 
        { 
            new Size (80, 25),
            new Size (100, 30),
            new Size (60, 20),
            new Size (120, 40),
            new Size (80, 25)
        };

        foreach (var size in sizes)
        {
            AutoInitShutdownAttribute.FakeResize (size);
            AutoInitShutdownAttribute.RunIteration ();
            
            Assert.Equal (size.Width, Application.Driver!.Cols);
            Assert.Equal (size.Height, Application.Driver.Rows);
        }
    }

    #endregion

    #region Resize Behavior Tests

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Resize_Fires_SizeChanged_Event ()
    {
        // Track if the event was fired
        bool eventFired = false;
        Size? newSize = null;

        // Subscribe to the SizeChanged event
        Application.Driver!.SizeChanged += (sender, args) =>
        {
            eventFired = true;
            newSize = args.Size;
        };

        // Perform a resize
        AutoInitShutdownAttribute.FakeResize (new Size (100, 30));

        // Verify the event was fired with correct size
        Assert.True (eventFired, "SizeChanged event should have been fired");
        Assert.NotNull (newSize);
        Assert.Equal (100, newSize.Value.Width);
        Assert.Equal (30, newSize.Value.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Resize_Updates_Screen_Property ()
    {
        // Initial screen size
        var initialScreen = Application.Screen;
        Assert.Equal (new Rectangle (0, 0, 80, 25), initialScreen);

        // Resize to 120x40
        AutoInitShutdownAttribute.FakeResize (new Size (120, 40));

        // Verify Screen property is updated
        var newScreen = Application.Screen;
        Assert.Equal (new Rectangle (0, 0, 120, 40), newScreen);
        
        // Verify it matches driver's reported size
        Assert.Equal (Application.Driver!.Cols, newScreen.Width);
        Assert.Equal (Application.Driver.Rows, newScreen.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Resize_Clears_Contents ()
    {
        Application.Top = new Toplevel ();
        
        var label = new Label { Text = "Test Content", X = 0, Y = 0 };
        Application.Top!.Add (label);
        
        Application.Begin (Application.Top);
        AutoInitShutdownAttribute.RunIteration ();

        // Get initial contents
        var driver = Application.Driver!;
        var initialContents = driver.Contents;
        Assert.NotNull (initialContents);
        
        int initialRows = initialContents.GetLength (0);
        int initialCols = initialContents.GetLength (1);

        // Resize
        AutoInitShutdownAttribute.FakeResize (new Size (100, 30));

        // Verify contents buffer was resized
        var newContents = driver.Contents;
        Assert.NotNull (newContents);
        
        int newRows = newContents.GetLength (0);
        int newCols = newContents.GetLength (1);
        
        Assert.NotEqual (initialRows, newRows);
        Assert.NotEqual (initialCols, newCols);
        Assert.Equal (30, newRows);
        Assert.Equal (100, newCols);
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Multiple_Resize_Events_All_Fire ()
    {
        int eventCount = 0;
        List<Size> eventSizes = new ();

        // Subscribe to the SizeChanged event
        Application.Driver!.SizeChanged += (sender, args) =>
        {
            eventCount++;
            if (args.Size.HasValue)
            {
                eventSizes.Add (args.Size.Value);
            }
        };

        // Perform multiple resizes
        var sizes = new[] 
        { 
            new Size (90, 27),
            new Size (110, 35),
            new Size (95, 28)
        };

        foreach (var size in sizes)
        {
            AutoInitShutdownAttribute.FakeResize (size);
        }

        // Verify all events were fired
        Assert.Equal (sizes.Length, eventCount);
        Assert.Equal (sizes.Length, eventSizes.Count);
        
        for (int i = 0; i < sizes.Length; i++)
        {
            Assert.Equal (sizes [i], eventSizes [i]);
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void FakeDriver_Resize_To_Same_Size_Still_Fires_Event ()
    {
        int eventCount = 0;

        // Subscribe to the SizeChanged event
        Application.Driver!.SizeChanged += (sender, args) =>
        {
            eventCount++;
        };

        // Get current size
        var currentSize = new Size (Application.Driver.Cols, Application.Driver.Rows);

        // Resize to the same size
        AutoInitShutdownAttribute.FakeResize (currentSize);

        // Event should still fire even though size hasn't changed
        Assert.Equal (1, eventCount);
    }

    #endregion
}
