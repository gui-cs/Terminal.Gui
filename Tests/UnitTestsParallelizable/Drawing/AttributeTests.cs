// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.DrawingTests;

public class AttributeTests
{
    [Fact]
    public void Attribute_Is_Value_Type ()
    {
        // prove that Color is a value type
        Assert.True (typeof (Attribute).IsValueType);
    }

    [Fact]
    public void ColorAndColorNamesConstructor ()
    {
        // Arrange & Act
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColorName = ColorName16.Black;
        var attribute = new Attribute (foregroundColor, backgroundColorName);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (new Color (backgroundColorName), attribute.Background);
    }

    [Fact]
    public void ColorConstructor ()
    {
        // Arrange & Act
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColor = new Color (255, 255, 255);
        var attribute = new Attribute (foregroundColor, backgroundColor);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void ColorNamesAndColorConstructor ()
    {
        // Arrange & Act
        var foregroundColorName = ColorName16.BrightYellow;
        var backgroundColor = new Color (128, 128, 128);
        var attribute = new Attribute (foregroundColorName, backgroundColor);

        // Assert
        Assert.Equal (new Color (foregroundColorName), attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void ColorNamesConstructor ()
    {
        // Arrange & Act
        var attribute = new Attribute (ColorName16.Blue);

        // Assert
        Assert.Equal (new Color (Color.Blue), attribute.Foreground);
        Assert.Equal (new Color (Color.Blue), attribute.Background);
    }

    [Fact]
    public void Constructors_Construct ()
    {
        var driver = new FakeDriver ();
        driver.Init ();

        // Test parameterless constructor
        var attr = new Attribute ();

        Assert.Equal (-1, attr.PlatformColor);
        Assert.Equal (new Color (Color.White), attr.Foreground);
        Assert.Equal (new Color (Color.Black), attr.Background);

        // Test foreground, background
        var fg = new Color ();
        fg = new Color (Color.Red);

        var bg = new Color ();
        bg = new Color (Color.Blue);

        attr = new Attribute (fg, bg);

        //Assert.True (attr.Initialized);
        //Assert.True (attr.HasValidColors);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (bg, attr.Background);

        attr = new Attribute (fg);

        //Assert.True (attr.Initialized);
        //Assert.True (attr.HasValidColors);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (fg, attr.Background);

        attr = new Attribute (bg);

        //Assert.True (attr.Initialized);
        //Assert.True (attr.HasValidColors);
        Assert.Equal (bg, attr.Foreground);
        Assert.Equal (bg, attr.Background);

        driver.End ();
    }

    [Fact]
    public void DefaultConstructor ()
    {
        // Arrange & Act
        var attribute = new Attribute ();

        // Assert
        //Assert.False (attribute.Initialized);
        Assert.Equal (-1, attribute.PlatformColor);
        Assert.Equal (new Color (Color.White), attribute.Foreground);
        Assert.Equal (new Color (Color.Black), attribute.Background);
    }

    [Fact]
    public void EqualityOperator_ShouldReturnFalseForDifferentAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Blue, Color.Black);

        // Act & Assert
        Assert.False (attribute1 == attribute2);
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrueForEqualAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Red, Color.Black);

        // Act & Assert
        Assert.True (attribute1 == attribute2);
    }

    [Fact]
    public void Equals_Initialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Red, Color.Green);

        Assert.True (attr1.Equals (attr2));
        Assert.True (attr2.Equals (attr1));
    }

    [Fact]
    public void Equals_NotInitialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Red, Color.Green);

        Assert.True (attr1.Equals (attr2));
        Assert.True (attr2.Equals (attr1));
    }

    [Fact]
    public void Implicit_Assign ()
    {
        var driver = new FakeDriver ();
        driver.Init ();

        var attr = new Attribute ();

        var value = 42;
        var fg = new Color ();
        fg = new Color (Color.Red);

        var bg = new Color ();
        bg = new Color (Color.Blue);

        // Test conversion to int
        attr = new Attribute (value, fg, bg);
        int value_implicit = attr.PlatformColor;
        Assert.Equal (value, value_implicit);

        Assert.Equal (value, attr.PlatformColor);

        driver.End ();
    }

    [Fact]
    public void InequalityOperator_ShouldReturnFalseForEqualAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Red, Color.Black);

        // Act & Assert
        Assert.False (attribute1 != attribute2);
    }

    [Fact]
    public void InequalityOperator_ShouldReturnTrueForDifferentAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Blue, Color.Black);

        // Act & Assert
        Assert.True (attribute1 != attribute2);
    }

    [Fact]
    public void Make_Creates ()
    {
        var driver = new FakeDriver ();
        driver.Init ();

        var fg = new Color ();
        fg = new Color (Color.Red);

        var bg = new Color ();
        bg = new Color (Color.Blue);

        var attr = new Attribute (fg, bg);

        //Assert.True (attr.Initialized);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (bg, attr.Background);

        driver.End ();
    }

    [Fact]
    public void Make_Creates_NoDriver ()
    {
        var fg = new Color ();
        fg = new Color (Color.Red);

        var bg = new Color ();
        bg = new Color (Color.Blue);

        var attr = new Attribute (fg, bg);

        //Assert.False (attr.Initialized);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (bg, attr.Background);
    }

    [Fact]
    public void Make_SetsNotInitialized_NoDriver ()
    {
        var fg = new Color ();
        fg = new Color (Color.Red);

        var bg = new Color ();
        bg = new Color (Color.Blue);

        var a = new Attribute (fg, bg);

        //Assert.False (a.Initialized);
    }

    [Fact]
    public void MakeColorAndColor_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColor = new Color (255, 255, 255);

        // Act
        var attribute = new Attribute (foregroundColor, backgroundColor);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void MakeColorAndColorNames_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0);
        var backgroundColorName = ColorName16.White;

        // Act
        var attribute = new Attribute (foregroundColor, backgroundColorName);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (new Color (backgroundColorName), attribute.Background);
    }

    [Fact]
    public void MakeColorNamesAndColor_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColorName = ColorName16.Green;
        var backgroundColor = new Color (128, 128, 128);

        // Act
        var attribute = new Attribute (foregroundColorName, backgroundColor);

        // Assert
        Assert.Equal (new Color (foregroundColorName), attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void MakeColorNamesAndColorNames_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColorName = ColorName16.BrightYellow;
        var backgroundColorName = ColorName16.Black;

        // Act
        var attribute = new Attribute (foregroundColorName, backgroundColorName);

        // Assert
        Assert.Equal (new Color (foregroundColorName), attribute.Foreground);
        Assert.Equal (new Color (backgroundColorName), attribute.Background);
    }

    [Fact]
    public void NotEquals_Initialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Green, Color.Red);

        Assert.False (attr1.Equals (attr2));
        Assert.False (attr2.Equals (attr1));
    }

    [Fact]
    public void NotEquals_NotInitialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Green, Color.Red);

        Assert.False (attr1.Equals (attr2));
        Assert.False (attr2.Equals (attr1));
    }

    [Fact]
    public void ToString_ShouldReturnFormattedStringWithForegroundAndBackground ()
    {
        // Arrange
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColor = new Color (255, 255, 255);
        var expectedString = $"[{foregroundColor},{backgroundColor}]";

        // Act
        var attribute = new Attribute (foregroundColor, backgroundColor);
        var attributeString = attribute.ToString ();

        // Assert
        Assert.Equal (expectedString, attributeString);
    }
}
