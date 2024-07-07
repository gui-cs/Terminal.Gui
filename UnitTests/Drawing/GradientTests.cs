
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

    [Fact]
    public void SingleColorStop ()
    {
        var stops = new List<Color> { new Color (255, 0, 0) }; // Red
        var steps = new List<int> { };

        var g = new Gradient (stops, steps, loop: false);
        Assert.All (g.Spectrum, color => Assert.Equal (new Color (255, 0, 0), color));
    }

    [Fact]
    public void LoopingGradient_CorrectColors ()
    {
        var stops = new List<Color>
            {
                new Color(255, 0, 0),    // Red
                new Color(0, 0, 255)     // Blue
            };

        var steps = new List<int> { 10 };

        var g = new Gradient (stops, steps, loop: true);
        Assert.Equal (new Color (255, 0, 0), g.Spectrum.First ());
        Assert.Equal (new Color (255, 0, 0), g.Spectrum.Last ());
    }

    [Fact]
    public void DifferentStepSizes ()
    {
        var stops = new List<Color>
            {
                new Color(255, 0, 0),    // Red
                new Color(0, 255, 0),    // Green
                new Color(0, 0, 255)     // Blue
            };

        var steps = new List<int> { 5, 15 }; // Different steps

        var g = new Gradient (stops, steps, loop: false);
        Assert.Equal (22, g.Spectrum.Count);
    }

    [Fact]
    public void FractionOutOfRange_ThrowsException ()
    {
        var stops = new List<Color>
            {
                new Color(255, 0, 0),    // Red
                new Color(0, 0, 255)     // Blue
            };

        var steps = new List<int> { 10 };

        var g = new Gradient (stops, steps, loop: false);

        Assert.Throws<ArgumentOutOfRangeException> (() => g.GetColorAtFraction (-0.1));
        Assert.Throws<ArgumentOutOfRangeException> (() => g.GetColorAtFraction (1.1));
    }

    [Fact]
    public void NaNFraction_ReturnsLastColor ()
    {
        var stops = new List<Color>
            {
                new Color(255, 0, 0),    // Red
                new Color(0, 0, 255)     // Blue
            };

        var steps = new List<int> { 10 };

        var g = new Gradient (stops, steps, loop: false);
        Assert.Equal (new Color (0, 0, 255), g.GetColorAtFraction (double.NaN));
    }

    [Fact]
    public void Constructor_SingleStepProvided_ReplicatesForAllPairs ()
    {
        var stops = new List<Color>
    {
        new Color(255, 0, 0),    // Red
        new Color(0, 255, 0),    // Green
        new Color(0, 0, 255)     // Blue
    };

        var singleStep = new List<int> { 5 }; // Single step provided
        var gradient = new Gradient (stops, singleStep, loop: false);

        Assert.NotNull (gradient.Spectrum);
        Assert.Equal (12, gradient.Spectrum.Count); // 5 steps Red -> Green + 5 steps Green -> Blue + 2 end colors
    }

    [Fact]
    public void Constructor_InvalidStepsLength_ThrowsArgumentException ()
    {
        var stops = new List<Color>
    {
        new Color(255, 0, 0),    // Red
        new Color(0, 0, 255)     // Blue
    };

        var invalidSteps = new List<int> { 5, 5 }; // Invalid length (N-1 expected)
        Assert.Throws<ArgumentException> (() => new Gradient (stops, invalidSteps, loop: false));
    }

    [Fact]
    public void Constructor_ValidStepsLength_DoesNotThrow ()
    {
        var stops = new List<Color>
    {
        new Color(255, 0, 0),    // Red
        new Color(0, 255, 0),    // Green
        new Color(0, 0, 255)     // Blue
    };

        var validSteps = new List<int> { 5, 5 }; // Valid length (N-1)
        var gradient = new Gradient (stops, validSteps, loop: false);

        Assert.NotNull (gradient.Spectrum);
        Assert.Equal (12, gradient.Spectrum.Count); // 5 steps Red -> Green + 5 steps Green -> Blue + 2 end colors
    }

}