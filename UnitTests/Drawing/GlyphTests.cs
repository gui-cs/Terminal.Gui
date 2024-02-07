using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Terminal.Gui.DrawingTests;

public class GlyphTests {
    [Fact]
    public void Default_GlyphDefinitions_Deserialize () {
        var defs = new GlyphDefinitions ();

        // enumerate all properties in GlyphDefinitions
        foreach (PropertyInfo prop in typeof (GlyphDefinitions).GetProperties ()) {
            if (prop.PropertyType == typeof (Rune)) {
                // Act
                var rune = (Rune)prop.GetValue (defs);
                string json = JsonSerializer.Serialize (rune, ConfigurationManager._serializerOptions);
                var deserialized = JsonSerializer.Deserialize<Rune> (json, ConfigurationManager._serializerOptions);

                // Assert
                Assert.Equal (((Rune)prop.GetValue (defs)).Value, deserialized.Value);
            }
        }
    }
}
