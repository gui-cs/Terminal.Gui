namespace Terminal.Gui.ViewsTests;

public class ScrollSliderTests
{
    // Test for GetPositionFromSliderLocation to GetSliderLocationDimensionFromPosition
    [Theory]
    [InlineData (Orientation.Vertical, 26, 236, -1, 0)]
    [InlineData (Orientation.Vertical, 26, 236, 0, 0)]
    [InlineData (Orientation.Vertical, 26, 236, 5, 46)]
    [InlineData (Orientation.Vertical, 26, 236, 10, 91)]
    [InlineData (Orientation.Vertical, 26, 236, 15, 137)]
    [InlineData (Orientation.Vertical, 26, 236, 20, 182)]
    [InlineData (Orientation.Vertical, 26, 236, 26, 210)]
    [InlineData (Orientation.Vertical, 26, 236, 27, 210)]
    [InlineData (Orientation.Vertical, 37, 236, 2, 13)]
    [InlineData (Orientation.Vertical, 42, 236, 29, 164)]
    public void Test_Position_Location_Consistency_KeepContentInAllViewport_True (Orientation orientation, int scrollLength, int size, int location, int expectedPosition)
    {
        // Arrange
        Scroll scroll = new ()
        {
            Orientation = orientation,
            Width = orientation == Orientation.Vertical ? 1 : scrollLength,
            Height = orientation == Orientation.Vertical ? scrollLength : 1,
            Size = size,
            KeepContentInAllViewport = true
        };

        scroll.BeginInit ();
        scroll.EndInit ();

        // Act
        scroll.Position = scroll._slider.GetPositionFromSliderLocation (location);
        (int calculatedLocation, int calculatedDimension) = scroll._slider.GetSliderLocationDimensionFromPosition ();
        int calculatedPosition = scroll._slider.GetPositionFromSliderLocation (calculatedLocation);

        // Assert
        AssertLocation (scrollLength, location, calculatedLocation, calculatedDimension);

        Assert.Equal (scroll.Position, expectedPosition);
        Assert.Equal (calculatedPosition, expectedPosition);
    }

    // Randomized Test for more extensive testing
    [Theory]
    [InlineData (Orientation.Vertical, true, 26, 236, 5)]
    [InlineData (Orientation.Vertical, false, 26, 236, 5)]
    public void Test_Position_Location_Consistency_Random (Orientation orientation, bool keepContentInAllViewport, int scrollLength, int size, int testCount)
    {
        var random = new Random ();

        Scroll scroll = new ()
        {
            Orientation = orientation,
            Width = orientation == Orientation.Vertical ? 1 : scrollLength,
            Height = orientation == Orientation.Vertical ? scrollLength : 1,
            Size = size,
            KeepContentInAllViewport = keepContentInAllViewport
        };

        scroll.BeginInit ();
        scroll.EndInit ();

        // Number of random tests to run
        for (var i = 0; i < testCount; i++)
        {
            // Arrange
            int randomScrollLength = random.Next (0, 60); // Random content size length
            int randomLocation = random.Next (0, randomScrollLength); // Random location

            scroll.Width = scroll.Orientation == Orientation.Vertical ? 1 : randomScrollLength;
            scroll.Height = scroll.Orientation == Orientation.Vertical ? randomScrollLength : 1;

            // Slider may have changed content size
            scroll.LayoutSubviews ();

            // Act
            scroll.Position = scroll._slider.GetPositionFromSliderLocation (randomLocation);
            (int calculatedLocation, int calculatedDimension) = scroll._slider.GetSliderLocationDimensionFromPosition ();
            int calculatedPosition = scroll._slider.GetPositionFromSliderLocation (calculatedLocation);

            // Assert
            AssertLocation (randomScrollLength, randomLocation, calculatedLocation, calculatedDimension);

            Assert.Equal (scroll.Position, calculatedPosition);
        }
    }

    private static void AssertLocation (int scrollLength, int location, int calculatedLocation, int calculatedDimension)
    {
        if (location < 0)
        {
            Assert.Equal (0, calculatedLocation);
        }
        else if (location + calculatedDimension >= scrollLength)
        {
            Assert.Equal (scrollLength - calculatedDimension, calculatedLocation);
        }
        else
        {
            Assert.Equal (location, calculatedLocation);
        }
    }
}
