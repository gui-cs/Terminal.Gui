using System.Text.Json;
using UnitTests;

namespace Terminal.Gui.ConfigurationTests;

public class ColorSchemeJsonConverterTests
{
    //string json = @"
    //	{
    //	""ColorSchemes"": {
    //		""Base"": {
    //			""normal"": {
    //				""foreground"": ""White"",
    //				""background"": ""Blue""
    //   		            },
    //			""focus"": {
    //				""foreground"": ""Black"",
    //				""background"": ""Gray""
    //			    },
    //			""hotNormal"": {
    //				""foreground"": ""BrightCyan"",
    //				""background"": ""Blue""
    //			    },
    //			""hotFocus"": {
    //				""foreground"": ""BrightBlue"",
    //				""background"": ""Gray""
    //			    },
    //			""disabled"": {
    //				""foreground"": ""DarkGray"",
    //				""background"": ""Blue""
    //			    }
    //		}
    //		}
    //	}";
    [Fact]
    [AutoInitShutdown]
    public void TestColorSchemesSerialization ()
    {
        // Arrange
        var expectedColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.White, Color.Blue),
            Focus = new Attribute (Color.Black, Color.Gray),
            HotNormal = new Attribute (Color.BrightCyan, Color.Blue),
            HotFocus = new Attribute (Color.BrightBlue, Color.Gray),
            Disabled = new Attribute (Color.DarkGray, Color.Blue)
        };

        string serializedColorScheme =
            JsonSerializer.Serialize (expectedColorScheme, ConfigurationManagerTests._jsonOptions);

        // Act
        var actualColorScheme =
            JsonSerializer.Deserialize<ColorScheme> (serializedColorScheme, ConfigurationManagerTests._jsonOptions);

        // Assert
        Assert.Equal (expectedColorScheme, actualColorScheme);
    }
}