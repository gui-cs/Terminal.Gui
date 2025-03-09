using System.Text.Json;
using UnitTests;

namespace Terminal.Gui.ConfigurationTests;

public class AttributeJsonConverterTests
{
    [Fact]
    public void TestDeserialize ()
    {
        // Test deserializing from human-readable color names
        var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\"}";
        var attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
        Assert.Equal (Color.Blue, attribute.Foreground.GetClosestNamedColor16 ());
        Assert.Equal (Color.Green, attribute.Background.GetClosestNamedColor16 ());

        // Test deserializing from RGB values
        json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
        attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
        Assert.Equal (Color.Red, attribute.Foreground.GetClosestNamedColor16 ());
        Assert.Equal (Color.BrightGreen, attribute.Background.GetClosestNamedColor16 ());
    }

    [Fact]
    [AutoInitShutdown]
    public void TestSerialize ()
    {
        // Test serializing to human-readable color names
        var attribute = new Attribute (Color.Blue, Color.Green);
        string json = JsonSerializer.Serialize (attribute, ConfigurationManagerTests._jsonOptions);
        Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\"}", json);
    }
}