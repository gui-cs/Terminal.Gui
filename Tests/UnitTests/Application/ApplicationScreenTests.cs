using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

public class ApplicationScreenTests
{
    public ApplicationScreenTests (ITestOutputHelper output)
    {
        ConsoleDriver.RunningUnitTests = true;
    }


    [Fact]
    public void ClearScreenNextIteration_Resets_To_False_After_LayoutAndDraw ()
    {
        // Arrange
        Application.ResetState (true);
        Application.Init ();

        // Act
        Application.ClearScreenNextIteration = true;
        Application.LayoutAndDraw ();

        // Assert
        Assert.False (Application.ClearScreenNextIteration);

        // Cleanup
        Application.ResetState (true);
    }

    [Fact]
    public void ClearContents_Called_When_Top_Frame_Changes ()
    {
        // Arrange
        Application.Init (new FakeDriver ());
        Application.Top = new ();
        Application.TopLevels.Push (Application.Top);

        var clearedContentsRaised = 0;


        Application.Driver!.ClearedContents += OnClearedContents;

        // Act
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (1, clearedContentsRaised);

        // Act
        Application.Top.SetNeedsLayout ();
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (1, clearedContentsRaised);

        // Act
        Application.Top.X = 1;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (2, clearedContentsRaised);

        // Act
        Application.Top.Width = 10;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (3, clearedContentsRaised);

        // Cleanup
        Application.Top.Dispose ();
        Application.Top = null;
        Application.Driver!.ClearedContents -= OnClearedContents;
        Application.Shutdown ();
        Application.ResetState (true);

        return;

        void OnClearedContents (object e, EventArgs a) { clearedContentsRaised++; }
    }

    [Fact]
    public void Screen_Changes_OnSizeChanged_Without_Call_Application_Init ()
    {
        // Arrange
        Application.ResetState (true);
        Assert.Null (Application.Driver);
        Application.Driver = new FakeDriver { Rows = 25, Cols = 25 };
        Application.SubscribeDriverEvents ();
        Assert.Equal (new (0, 0, 25, 25), Application.Screen);

        // Act
        ((FakeDriver)Application.Driver)!.SetBufferSize (120, 30);

        // Assert
        Assert.Equal (new (0, 0, 120, 30), Application.Screen);

        // Cleanup
        Application.ResetState (true);
    }
}
