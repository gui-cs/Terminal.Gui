
namespace Terminal.Gui.DrawingTests;

public class GradientTests
{
    // Static method to provide all enum values
    public static IEnumerable<object []> GradientDirectionValues ()
    {
        return typeof (GradientDirection).GetEnumValues ()
            .Cast<GradientDirection> ()
            .Select (direction => new object [] { direction });
    }

    [Theory]
    [MemberData (nameof (GradientDirectionValues))]
    public void GradientIsInclusive_2_by_2 (GradientDirection direction)
    {
        // Define the colors of the gradient stops
        var stops = new List<Color>
            {
                new Color(255, 0, 0),    // Red
                new Color(0, 0, 255)     // Blue
            };

        // Define the number of steps between each color
        var steps = new List<int> { 10 }; // 10 steps between Red -> Blue

        var g = new Gradient (stops, steps, loop: false);
        Assert.Equal (4, g.BuildCoordinateColorMapping (1, 1, direction).Count);
    }

    [Theory]
    [MemberData (nameof (GradientDirectionValues))]
    public void GradientIsInclusive_1_by_1 (GradientDirection direction)
    {
        // Define the colors of the gradient stops
        var stops = new List<Color>
            {
                new Color(255, 0, 0),    // Red
                new Color(0, 0, 255)     // Blue
            };

        // Define the number of steps between each color
        var steps = new List<int> { 10 }; // 10 steps between Red -> Blue

        var g = new Gradient (stops, steps, loop: false);

        // Note that
        var c = Assert.Single (g.BuildCoordinateColorMapping (0, 0, direction));
        Assert.Equal (c.Key, new Point(0,0));
        Assert.Equal (c.Value, new Color (0, 0, 255));
    }
}
