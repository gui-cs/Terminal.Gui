namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class AnsiMouseParserTests
{
    private readonly AnsiMouseParser _parser = new ();

    // Consolidated test for all mouse events: button press/release, wheel scroll, position, modifiers
    [Theory]
    [InlineData ("\u001b[<0;100;200M", 99, 199, MouseFlags.LeftButtonPressed)] // Button 1 Pressed
    [InlineData ("\u001b[<0;150;250m", 149, 249, MouseFlags.LeftButtonReleased)] // Button 1 Released
    [InlineData ("\u001b[<1;120;220M", 119, 219, MouseFlags.MiddleButtonPressed)] // Button 2 Pressed
    [InlineData ("\u001b[<1;180;280m", 179, 279, MouseFlags.MiddleButtonReleased)] // Button 2 Released
    [InlineData ("\u001b[<2;200;300M", 199, 299, MouseFlags.RightButtonPressed)] // Button 3 Pressed
    [InlineData ("\u001b[<2;250;350m", 249, 349, MouseFlags.RightButtonReleased)] // Button 3 Released
    [InlineData ("\u001b[<64;100;200M", 99, 199, MouseFlags.WheeledUp)] // Wheel Scroll Up
    [InlineData ("\u001b[<65;150;250m", 149, 249, MouseFlags.WheeledDown)] // Wheel Scroll Down
    [InlineData ("\u001b[<39;100;200m", 99, 199, MouseFlags.Shift | MouseFlags.PositionReport)] // Mouse Position (No Button)
    [InlineData ("\u001b[<43;120;240m", 119, 239, MouseFlags.Alt | MouseFlags.PositionReport)] // Mouse Position (No Button)
    [InlineData ("\u001b[<8;100;200M", 99, 199, MouseFlags.LeftButtonPressed | MouseFlags.Alt)] // Button 1 Pressed + Alt
    [InlineData ("\u001b[<invalid;100;200M", 0, 0, MouseFlags.None)] // Invalid Input (Expecting null)
    [InlineData ("\u001b[<100;200;300Z", 0, 0, MouseFlags.None)] // Invalid Input (Expecting null)
    [InlineData ("\u001b[<invalidInput>", 0, 0, MouseFlags.None)] // Invalid Input (Expecting null)
    public void ProcessMouseInput_ReturnsCorrectFlags (string input, int expectedX, int expectedY, MouseFlags expectedFlags)
    {
        // Act
        Mouse? result = _parser.ProcessMouseInput (input);

        // Assert
        if (expectedFlags == MouseFlags.None)
        {
            Assert.Null (result); // Expect null for invalid inputs
        }
        else
        {
            Assert.NotNull (result); // Expect non-null result for valid inputs
            Assert.NotNull (result.Timestamp);
            Assert.Equal (new (expectedX, expectedY), result!.ScreenPosition); // Verify position
            Assert.Equal (expectedFlags, result.Flags); // Verify flags
        }
    }

    /// <summary>
    /// Tests that ProcessMouseInput sets ScreenPosition and NOT Position.
    /// Position is View-relative and should only be set by ApplicationMouse or View.Mouse code.
    /// </summary>
    [Theory]
    [InlineData ("\u001b[<0;10;20M", 9, 19)] // Button 1 Pressed at screen (9, 19) 
    [InlineData ("\u001b[<64;50;75M", 49, 74)] // Wheel up at screen (49, 74)
    [InlineData ("\u001b[<35;1;1m", 0, 0)] // Mouse move at screen (0, 0)
    public void ProcessMouseInput_SetsScreenPosition_NotPosition (string input, int expectedX, int expectedY)
    {
        // Act
        Mouse? result = _parser.ProcessMouseInput (input);

        // Assert
        Assert.NotNull (result);

        // ScreenPosition should be set to the parsed coordinates (0-based)
        Assert.Equal (new Point (expectedX, expectedY), result!.ScreenPosition);
        Assert.NotNull (result.Timestamp);

        // Position should NEVER be set by parsers; it's View-relative and set by ApplicationMouse/View.Mouse
        Assert.Null (result.Position);
    }
}
