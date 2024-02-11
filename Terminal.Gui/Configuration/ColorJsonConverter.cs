using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Json converter for the <see cref="Color"/> class.</summary>
internal class ColorJsonConverter : JsonConverter<Color>
{
    private static ColorJsonConverter _instance;

    /// <summary>Singleton</summary>
    public static ColorJsonConverter Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ColorJsonConverter ();
            }

            return _instance;
        }
    }

    public override Color Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Check if the value is a string
        if (reader.TokenType == JsonTokenType.String)
        {
            // Get the color string
            ReadOnlySpan<char> colorString = reader.GetString ();

            // Check if the color string is a color name
            if (Enum.TryParse (colorString, true, out ColorName color))
            {
                // Return the parsed color
                return new Color (in color);
            }

            if (Color.TryParse (colorString, null, out Color parsedColor))
            {
                return parsedColor;
            }

            throw new JsonException ($"Unexpected color name: {colorString}.");
        }

        throw new JsonException ($"Unexpected token when parsing Color: {reader.TokenType}");
    }

    public override void Write (Utf8JsonWriter writer, Color value, JsonSerializerOptions options) { writer.WriteStringValue (value.ToString ()); }
}
