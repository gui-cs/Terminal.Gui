using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ScrollSliderTests (ITestOutputHelper output)
{
    [Theory]
    [SetupFakeDriver]
    [InlineData (
                    3,
                    10,
                    1,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
└───┘")]
    [InlineData (
                    10,
                    1,
                    3,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│███       │
└──────────┘")]
    [InlineData (
                    3,
                    10,
                    3,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│   │
│   │
│   │
│   │
│   │
│   │
│   │
└───┘")]



    [InlineData (
                    3,
                    10,
                    5,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│   │
│   │
│   │
│   │
│   │
└───┘")]

    [InlineData (
                    3,
                    10,
                    5,
                    1,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│███│
│███│
│███│
│███│
│███│
│   │
│   │
│   │
│   │
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    4,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
│   │
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    5,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    6,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    3,
                    10,
                    10,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    3,
                    10,
                    10,
                    5,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]
    [InlineData (
                    3,
                    10,
                    11,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    10,
                    3,
                    5,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│█████     │
│█████     │
│█████     │
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    5,
                    1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│ █████    │
│ █████    │
│ █████    │
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    4,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│    █████ │
│    █████ │
│    █████ │
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│     █████│
│     █████│
│     █████│
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    6,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│     █████│
│     █████│
│     █████│
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    10,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    10,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    11,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]
    public void Draws_Correctly (int superViewportWidth, int superViewportHeight, int sliderSize, int position, Orientation orientation, string expected)
    {
        var super = new Window
        {
            Id = "super",
            Width = superViewportWidth + 2,
            Height = superViewportHeight + 2
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            Size = sliderSize,
            //Position = position,
        };
        Assert.Equal (sliderSize, scrollSlider.Size);
        super.Add (scrollSlider);
        scrollSlider.Position = position;

        super.Layout ();
        super.Draw ();

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
    }
}
