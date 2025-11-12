using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ApplicationTests;

public class ApplicationScreenTests
{
    public ApplicationScreenTests (ITestOutputHelper output)
    {
    }


    [Fact]
    public void ClearScreenNextIteration_Resets_To_False_After_LayoutAndDraw ()
    {
        // Arrange
        Application.ResetState (true);
        Application.Init (null, "fake");

        // Act
        Application.ClearScreenNextIteration = true;
        Application.LayoutAndDraw ();

        // Assert
        Assert.False (Application.ClearScreenNextIteration);

        // Cleanup
        Application.ResetState (true);
    }

    [Fact]
    [AutoInitShutdown]
    public void ClearContents_Called_When_Top_Frame_Changes ()
    {
        Toplevel top = new Toplevel ();
        SessionToken rs = Application.Begin (top);
        // Arrange
        var clearedContentsRaised = 0;

        Application.Driver!.ClearedContents += OnClearedContents;

        // Act
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (0, clearedContentsRaised);

        // Act
        Application.Current!.SetNeedsLayout ();
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (0, clearedContentsRaised);

        // Act
        Application.Current.X = 1;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (1, clearedContentsRaised);

        // Act
        Application.Current.Width = 10;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (2, clearedContentsRaised);

        // Act
        Application.Current.Y = 1;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (3, clearedContentsRaised);

        // Act
        Application.Current.Height = 10;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (4, clearedContentsRaised);

        Application.Driver!.ClearedContents -= OnClearedContents;

        Application.End (rs);

        return;

        void OnClearedContents (object e, EventArgs a) { clearedContentsRaised++; }
    }

    [Fact]
    [SetupFakeApplication]
    public void Screen_Changes_OnScreenChanged_Without_Call_Application_Init ()
    {
        Assert.Equal (new (0, 0, 80, 25), Application.Screen);

        // Act
        Application.Driver!.SetScreenSize (120, 30);

        // Assert
        Assert.Equal (new (0, 0, 120, 30), Application.Screen);
    }
}
