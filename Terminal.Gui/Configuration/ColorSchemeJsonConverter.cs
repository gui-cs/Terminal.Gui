using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Implements a JSON converter for <see cref="ColorScheme"/>.</summary>
internal class ColorSchemeJsonConverter : JsonConverter<ColorScheme>
{
    private static ColorSchemeJsonConverter instance;

    /// <summary>Singleton</summary>
    public static ColorSchemeJsonConverter Instance
    {
        get
        {
            if (instance is null)
            {
                instance = new ColorSchemeJsonConverter ();
            }

            return instance;
        }
    }

    /// <inheritdoc/>
    public override ColorScheme Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException ($"Unexpected StartObject token when parsing ColorScheme: {reader.TokenType}.");
        }

        var normal = Attribute.Default;
        var focus = Attribute.Default;
        var hotNormal = Attribute.Default;
        var hotFocus = Attribute.Default;
        var disabled = Attribute.Default;

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                var colorScheme = new ColorScheme
                {
                    Normal = normal,
                    Focus = focus,
                    HotNormal = hotNormal,
                    HotFocus = hotFocus,
                    Disabled = disabled
                };

                return colorScheme;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException ($"Unexpected token when parsing Attribute: {reader.TokenType}.");
            }

            string propertyName = reader.GetString ();
            reader.Read ();
            var attribute = JsonSerializer.Deserialize (ref reader, SerializerContext.Attribute);

            switch (propertyName.ToLower ())
            {
                case "normal":
                    normal = attribute;

                    break;
                case "focus":
                    focus = attribute;

                    break;
                case "hotnormal":
                    hotNormal = attribute;

                    break;
                case "hotfocus":
                    hotFocus = attribute;

                    break;
                case "disabled":
                    disabled = attribute;

                    break;
                default:
                    throw new JsonException ($"Unrecognized ColorScheme Attribute name: {propertyName}.");
            }
        }

        throw new JsonException ();
    }

    /// <inheritdoc/>
    public override void Write (Utf8JsonWriter writer, ColorScheme value, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();

        writer.WritePropertyName ("Normal");
        AttributeJsonConverter.Instance.Write (writer, value.Normal, options);
        writer.WritePropertyName ("Focus");
        AttributeJsonConverter.Instance.Write (writer, value.Focus, options);
        writer.WritePropertyName ("HotNormal");
        AttributeJsonConverter.Instance.Write (writer, value.HotNormal, options);
        writer.WritePropertyName ("HotFocus");
        AttributeJsonConverter.Instance.Write (writer, value.HotFocus, options);
        writer.WritePropertyName ("Disabled");
        AttributeJsonConverter.Instance.Write (writer, value.Disabled, options);

        writer.WriteEndObject ();
    }
}
