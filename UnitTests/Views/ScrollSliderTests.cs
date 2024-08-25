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
    public void Test_Position_Location_Consistency (Orientation orientation, int scrollLength, int size, int location, int expectedPosition)
    {
        // Arrange
        Scroll host = new ()
        {
            Orientation = orientation,
            Width = orientation == Orientation.Vertical ? 1 : scrollLength,
            Height = orientation == Orientation.Vertical ? scrollLength : 1,
            Size = size
        };

        host.BeginInit ();
        host.EndInit ();

        // Act
        host.Position = host._slider.GetPositionFromSliderLocation (location);
        (int calculatedLocation, int calculatedDimension) = host._slider.GetSliderLocationDimensionFromPosition ();
        int calculatedPosition = host._slider.GetPositionFromSliderLocation (calculatedLocation);

        // Assert
        AssertLocation (scrollLength, location, calculatedLocation, calculatedDimension);

        Assert.Equal (host.Position, expectedPosition);
        Assert.Equal (calculatedPosition, expectedPosition);
    }

    // Randomized Test for more extensive testing
    [Theory]
    [InlineData (Orientation.Vertical, 26, 236, 5)]
    public void Test_Position_Location_Consistency_Random (Orientation orientation, int scrollLength, int size, int testCount)
    {
        var random = new Random ();

        Scroll host = new ()
        {
            Orientation = orientation,
            Width = orientation == Orientation.Vertical ? 1 : scrollLength,
            Height = orientation == Orientation.Vertical ? scrollLength : 1,
            Size = size
        };

        host.BeginInit ();
        host.EndInit ();

        // Number of random tests to run
        for (var i = 0; i < testCount; i++)
        {
            // Arrange
            int randomScrollLength = random.Next (0, 60); // Random content size length
            int randomLocation = random.Next (0, randomScrollLength); // Random location

            host.Width = host.Orientation == Orientation.Vertical ? 1 : randomScrollLength;
            host.Height = host.Orientation == Orientation.Vertical ? randomScrollLength : 1;

            // Slider may have changed content size
            host.LayoutSubviews ();

            // Act
            host.Position = host._slider.GetPositionFromSliderLocation (randomLocation);
            (int calculatedLocation, int calculatedDimension) = host._slider.GetSliderLocationDimensionFromPosition ();
            int calculatedPosition = host._slider.GetPositionFromSliderLocation (calculatedLocation);

            // Assert
            AssertLocation (randomScrollLength, randomLocation, calculatedLocation, calculatedDimension);

            Assert.Equal (host.Position, calculatedPosition);
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
