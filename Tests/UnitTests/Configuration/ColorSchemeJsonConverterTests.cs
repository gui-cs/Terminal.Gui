using System.Text.Json;
using UnitTests;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeJsonConverterTests
{
    //string json = @"
    //	{
    //	""Schemes"": {
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
    public void TestSchemesSerialization ()
    {
        // Arrange
        var expectedScheme = new Scheme
        {
            Normal = new Attribute (Color.White, Color.Blue),
            Focus = new Attribute (Color.Black, Color.Gray),
            HotNormal = new Attribute (Color.BrightCyan, Color.Blue),
            HotFocus = new Attribute (Color.BrightBlue, Color.Gray),
            Disabled = new Attribute (Color.DarkGray, Color.Blue)
        };

        string serializedScheme =
            JsonSerializer.Serialize (expectedScheme, ConfigurationManagerTests._jsonOptions);

        // Act
        var actualScheme =
            JsonSerializer.Deserialize<Scheme> (serializedScheme, ConfigurationManagerTests._jsonOptions);

        // Assert
        Assert.Equal (expectedScheme, actualScheme);
    }
}