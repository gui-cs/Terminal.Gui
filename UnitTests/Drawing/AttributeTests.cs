using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DrawingTests;
public class AttributeTests {
	[Fact]
	public void DefaultConstructor ()
	{
		// Arrange & Act
		var attribute = new Attribute ();

		// Assert
		Assert.False (attribute.Initialized);
		Assert.Equal (-1, attribute.Value);
		Assert.Equal ((Color)Color.White, attribute.Foreground);
		Assert.Equal ((Color)Color.Black, attribute.Background);

	}

	[Fact]
	public void PlatformColorConstructor ()
	{
		// Arrange & Act
		var attribute = new Attribute (42);

		// Assert
		Assert.True (attribute.Initialized);
		Assert.Equal (42, attribute.Value);
		Assert.Equal ((Color)Color.White, attribute.Foreground);
		Assert.Equal ((Color)Color.Black, attribute.Background);
	}

	[Fact]
	public void ColorNamesConstructor ()
	{
		// Arrange & Act
		var attribute = new Attribute (ColorNames.Blue);

		// Assert
		Assert.Equal ((Color)Color.Blue, attribute.Foreground);
		Assert.Equal ((Color)Color.Blue, attribute.Background);
	}

	[Fact, AutoInitShutdown]
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

	[Fact, AutoInitShutdown]
	public void ColorAndColorNamesConstructor ()
	{
		// Arrange & Act
		var foregroundColor = new Color (0, 0, 255);
		var backgroundColorName = ColorNames.Black;
		var attribute = new Attribute (foregroundColor, backgroundColorName);

		// Assert
		Assert.Equal (foregroundColor, attribute.Foreground);
		Assert.Equal ((Color)backgroundColorName, attribute.Background);
	}

	[Fact]
	public void ColorNamesAndColorConstructor ()
	{
		// Arrange & Act
		var foregroundColorName = ColorNames.BrightYellow;
		var backgroundColor = new Color (128, 128, 128);
		var attribute = new Attribute (foregroundColorName, backgroundColor);

		// Assert
		Assert.Equal ((Color)foregroundColorName, attribute.Foreground);
		Assert.Equal (backgroundColor, attribute.Background);
	}

	[Fact]
	public void Constuctors_Constuct ()
	{
		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		// Test parameterless constructor
		var attr = new Attribute ();

		Assert.Equal (-1, attr.Value);
		Assert.Equal ((Color)Color.White, attr.Foreground);
		Assert.Equal ((Color)Color.Black, attr.Background);

		// Test foreground, background
		var fg = new Color ();
		fg = (Color)Color.Red;

		var bg = new Color ();
		bg = (Color)Color.Blue;

		attr = new Attribute (fg, bg);

		Assert.True (attr.Initialized);
		Assert.True (attr.HasValidColors);
		Assert.Equal (fg, attr.Foreground);
		Assert.Equal (bg, attr.Background);

		attr = new Attribute (fg);
		Assert.True (attr.Initialized);
		Assert.True (attr.HasValidColors);
		Assert.Equal (fg, attr.Foreground);
		Assert.Equal (fg, attr.Background);

		attr = new Attribute (bg);
		Assert.True (attr.Initialized);
		Assert.True (attr.HasValidColors);
		Assert.Equal (bg, attr.Foreground);
		Assert.Equal (bg, attr.Background);

		driver.End ();
		Application.Shutdown ();
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
	public void MakeColorNamesAndColorNames_ForegroundAndBackgroundShouldMatchInput ()
	{
		// Arrange
		var foregroundColorName = ColorNames.BrightYellow;
		var backgroundColorName = ColorNames.Black;

		// Act
		var attribute = new Attribute (foregroundColorName, backgroundColorName);

		// Assert
		Assert.Equal ((Color)foregroundColorName, attribute.Foreground);
		Assert.Equal ((Color)backgroundColorName, attribute.Background);
	}

	[Fact]
	public void MakeColorNamesAndColor_ForegroundAndBackgroundShouldMatchInput ()
	{
		// Arrange
		var foregroundColorName = ColorNames.Green;
		var backgroundColor = new Color (128, 128, 128);

		// Act
		var attribute = new Attribute (foregroundColorName, backgroundColor);

		// Assert
		Assert.Equal ((Color)foregroundColorName, attribute.Foreground);
		Assert.Equal (backgroundColor, attribute.Background);
	}

	[Fact]
	public void MakeColorAndColorNames_ForegroundAndBackgroundShouldMatchInput ()
	{
		// Arrange
		var foregroundColor = new Color (255, 0, 0);
		var backgroundColorName = ColorNames.White;

		// Act
		var attribute = new Attribute (foregroundColor, backgroundColorName);

		// Assert
		Assert.Equal (foregroundColor, attribute.Foreground);
		Assert.Equal ((Color)backgroundColorName, attribute.Background);
	}



	[Fact]
	public void Implicit_Assign ()
	{
		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		var attr = new Attribute ();

		var value = 42;
		var fg = new Color ();
		fg = (Color)Color.Red;

		var bg = new Color ();
		bg = (Color)Color.Blue;

		// Test conversion to int
		attr = new Attribute (value, fg, bg);
		int value_implicit = attr.Value;
		Assert.Equal (value, value_implicit);

		Assert.Equal (value, attr.Value);

		driver.End ();
		Application.Shutdown ();
	}

	[Fact]
	public void Make_SetsNotInitialized_NoDriver ()
	{
		var fg = new Color ();
		fg = (Color)Color.Red;

		var bg = new Color ();
		bg = (Color)Color.Blue;

		var a = new Attribute (fg, bg);

		Assert.False (a.Initialized);
	}

	[Fact]
	public void Make_Creates ()
	{
		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		var fg = new Color ();
		fg = (Color)Color.Red;

		var bg = new Color ();
		bg = (Color)Color.Blue;

		var attr = new Attribute (fg, bg);
		Assert.True (attr.Initialized);
		Assert.Equal (fg, attr.Foreground);
		Assert.Equal (bg, attr.Background);

		driver.End ();
		Application.Shutdown ();
	}

	[Fact]
	public void Make_Creates_NoDriver ()
	{

		var fg = new Color ();
		fg = (Color)Color.Red;

		var bg = new Color ();
		bg = (Color)Color.Blue;

		var attr = new Attribute (fg, bg);
		Assert.False (attr.Initialized);
		Assert.Equal (fg, attr.Foreground);
		Assert.Equal (bg, attr.Background);
	}

	[Fact]
	public void Get_Asserts_NoDriver ()
	{
		Assert.Throws<InvalidOperationException> (() => Attribute.Get ());
	}

	[Fact]
	public void Get_Gets ()
	{
		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		var value = 42;
		var fg = new Color ();
		fg = (Color)Color.Red;

		var bg = new Color ();
		bg = (Color)Color.Blue;

		var attr = new Attribute (value, fg, bg);

		driver.SetAttribute (attr);

		var ret_attr = Attribute.Get ();

		Assert.Equal (value, ret_attr.Value);
		Assert.Equal (fg, ret_attr.Foreground);
		Assert.Equal (bg, ret_attr.Background);

		driver.End ();
		Application.Shutdown ();
	}

	[Fact]
	[AutoInitShutdown]
	public void GetColors_Based_On_Value ()
	{
		var driver = Application.Driver;
		var attrValue = new Attribute (Color.Red, Color.Green).Value;
		driver.GetColors (attrValue, out ColorNames fg, out ColorNames bg);

		Assert.Equal ((Color)Color.Red, (Color)fg);
		Assert.Equal ((Color)Color.Green, (Color)bg);
	}

	[Fact]
	public void IsValid_Tests ()
	{
		var attr = new Attribute ();
		Assert.True (attr.HasValidColors);

		attr = new Attribute (Color.Red, Color.Green);
		Assert.True (attr.HasValidColors);

		//attr = new Attribute (Color.Red, (Color)(-1));
		//Assert.False (attr.HasValidColors);

		//attr = new Attribute ((Color)(-1), Color.Green);
		//Assert.False (attr.HasValidColors);

		//attr = new Attribute ((Color)(-1), (Color)(-1));
		//Assert.False (attr.HasValidColors);
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
	public void NotEquals_NotInitialized ()
	{
		var attr1 = new Attribute (Color.Red, Color.Green);
		var attr2 = new Attribute (Color.Green, Color.Red);

		Assert.False (attr1.Equals (attr2));
		Assert.False (attr2.Equals (attr1));
	}

	[Fact, AutoInitShutdown]
	public void Equals_Initialized ()
	{
		Assert.NotNull (Application.Driver);

		var attr1 = new Attribute (Color.Red, Color.Green);
		var attr2 = new Attribute (Color.Red, Color.Green);

		Assert.True (attr1.Equals (attr2));
		Assert.True (attr2.Equals (attr1));
	}

	[Fact, AutoInitShutdown]
	public void NotEquals_Initialized ()
	{
		var attr1 = new Attribute (Color.Red, Color.Green);
		var attr2 = new Attribute (Color.Green, Color.Red);

		Assert.False (attr1.Equals (attr2));
		Assert.False (attr2.Equals (attr1));
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
	public void EqualityOperator_ShouldReturnFalseForDifferentAttributes ()
	{
		// Arrange
		var attribute1 = new Attribute (Color.Red, Color.Black);
		var attribute2 = new Attribute (Color.Blue, Color.Black);

		// Act & Assert
		Assert.False (attribute1 == attribute2);
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
	public void InequalityOperator_ShouldReturnFalseForEqualAttributes ()
	{
		// Arrange
		var attribute1 = new Attribute (Color.Red, Color.Black);
		var attribute2 = new Attribute (Color.Red, Color.Black);

		// Act & Assert
		Assert.False (attribute1 != attribute2);
	}


	[Fact]
	public void ToString_ShouldReturnFormattedStringWithForegroundAndBackground ()
	{
		// Arrange
		var foregroundColor = new Color (0, 0, 255);
		var backgroundColor = new Color (255, 255, 255);
		var expectedString = $"{foregroundColor},{backgroundColor}";

		// Act
		var attribute = new Attribute (foregroundColor, backgroundColor);
		var attributeString = attribute.ToString ();

		// Assert
		Assert.Equal (expectedString, attributeString);
	}


}
