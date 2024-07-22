using Moq;

namespace Terminal.Gui.ViewTests.OrientationTests;

public class OrientationHelperTests
{
    [Fact]
    public void Orientation_Set_NewValue_InvokesChangingAndChangedEvents ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var orientationHelper = new OrientationHelper (mockIOrientation.Object);
        var changingEventInvoked = false;
        var changedEventInvoked = false;

        orientationHelper.OrientationChanging += (sender, e) => { changingEventInvoked = true; };
        orientationHelper.OrientationChanged += (sender, e) => { changedEventInvoked = true; };

        // Act
        orientationHelper.Orientation = Orientation.Horizontal;

        // Assert
        Assert.True (changingEventInvoked, "OrientationChanging event was not invoked.");
        Assert.True (changedEventInvoked, "OrientationChanged event was not invoked.");
    }

    [Fact]
    public void Orientation_Set_NewValue_InvokesOnChangingAndOnChangedOverrides ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var onChangingOverrideCalled = false;
        var onChangedOverrideCalled = false;

        mockIOrientation.Setup (x => x.OnOrientationChanging (It.IsAny<Orientation> (), It.IsAny<Orientation> ()))
                        .Callback (() => onChangingOverrideCalled = true)
                        .Returns (false); // Ensure it doesn't cancel the change

        mockIOrientation.Setup (x => x.OnOrientationChanged (It.IsAny<Orientation> (), It.IsAny<Orientation> ()))
                        .Callback (() => onChangedOverrideCalled = true);

        var orientationHelper = new OrientationHelper (mockIOrientation.Object);

        // Act
        orientationHelper.Orientation = Orientation.Horizontal;

        // Assert
        Assert.True (onChangingOverrideCalled, "OnOrientationChanging override was not called.");
        Assert.True (onChangedOverrideCalled, "OnOrientationChanged override was not called.");
    }

    [Fact]
    public void Orientation_Set_SameValue_DoesNotInvokeChangingOrChangedEvents ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var orientationHelper = new OrientationHelper (mockIOrientation.Object);
        orientationHelper.Orientation = Orientation.Vertical; // Set initial orientation
        var changingEventInvoked = false;
        var changedEventInvoked = false;

        orientationHelper.OrientationChanging += (sender, e) => { changingEventInvoked = true; };
        orientationHelper.OrientationChanged += (sender, e) => { changedEventInvoked = true; };

        // Act
        orientationHelper.Orientation = Orientation.Vertical; // Set to the same value

        // Assert
        Assert.False (changingEventInvoked, "OrientationChanging event was invoked.");
        Assert.False (changedEventInvoked, "OrientationChanged event was invoked.");
    }

    [Fact]
    public void Orientation_Set_NewValue_OrientationChanging_CancellationPreventsChange ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var orientationHelper = new OrientationHelper (mockIOrientation.Object);
        orientationHelper.OrientationChanging += (sender, e) => { e.Cancel = true; }; // Cancel the change

        // Act
        orientationHelper.Orientation = Orientation.Horizontal;

        // Assert
        Assert.Equal (Orientation.Vertical, orientationHelper.Orientation); // Initial orientation is Vertical
    }

    [Fact]
    public void Orientation_Set_NewValue_OnOrientationChanging_CancelsChange ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();

        mockIOrientation.Setup (x => x.OnOrientationChanging (It.IsAny<Orientation> (), It.IsAny<Orientation> ()))
                        .Returns (true); // Override to return true, cancelling the change

        var orientationHelper = new OrientationHelper (mockIOrientation.Object);

        // Act
        orientationHelper.Orientation = Orientation.Horizontal;

        // Assert
        Assert.Equal (
                      Orientation.Vertical,
                      orientationHelper.Orientation); // Initial orientation is Vertical, and it should remain unchanged due to cancellation
    }
}
