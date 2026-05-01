using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>Json converter from the <see cref="Attribute"/> class.</summary>
internal class AttributeJsonConverter : JsonConverter<Attribute>
{
    private static AttributeJsonConverter? _instance;

    /// <summary></summary>
    public static AttributeJsonConverter Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new AttributeJsonConverter ();
            }

            return _instance;
        }
    }

    public override Attribute Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException ($"Unexpected StartObject token when parsing Attribute: {reader.TokenType}.");
        }

        var attribute = new Attribute ();
        Color? foreground = null;
        Color? background = null;
        TextStyle? style = null;

        string propertyName = string.Empty;

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (foreground is null || background is null)
                {
                    throw new JsonException ($"{propertyName}: Both Foreground and Background colors must be provided.");
                }

                if (style.HasValue)
                {
                    return new Attribute (foreground.Value, background.Value, style.Value);
                }
                else
                {
                    return new Attribute (foreground.Value, background.Value);
                }
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException ($"{propertyName}: Unexpected token when parsing Attribute: {reader.TokenType}.");
            }

            propertyName = reader.GetString ()!;
            reader.Read ();
            string property = reader.TokenType == JsonTokenType.String
                                  ? $"\"{reader.GetString ()}\""
                                  : $"<{reader.TokenType}>";

            try
            {
                switch (propertyName?.ToLower ())
                {
                    case "foreground":
                        foreground = JsonSerializer.Deserialize (ref reader, ConfigurationManager.SerializerContext.Color);

                        break;
                    case "background":
                        background = JsonSerializer.Deserialize (ref reader, ConfigurationManager.SerializerContext.Color);

                        break;
                    case "style":
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new JsonException ($"{propertyName}: Expected a string value.");
                        }

                        string styleValue = reader.GetString ()!;

                        if (!TryParseNamedTextStyle (styleValue, out TextStyle parsedStyle))
                        {
                            throw new JsonException ("Expected a valid text style value.");
                        }

                        style = parsedStyle;

                        break;

                    default:
                        throw new JsonException ($"{propertyName}: Unknown Attribute property .");
                }
            }
            catch (JsonException ex)
            {
                throw new JsonException ($"{propertyName}: \"{property}\" - {ex.Message}");
            }
        }

        throw new JsonException ($"{propertyName}: Bad Attribute.");
    }

    public override void Write (Utf8JsonWriter writer, Attribute value, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();
        writer.WritePropertyName (nameof (Attribute.Foreground));
        ColorJsonConverter.Instance.Write (writer, value.Foreground, options);
        writer.WritePropertyName (nameof (Attribute.Background));
        ColorJsonConverter.Instance.Write (writer, value.Background, options);
        if (value.Style != TextStyle.None)
        {
            writer.WritePropertyName (nameof (Attribute.Style));
            writer.WriteStringValue (value.Style.ToString ());
        }

        writer.WriteEndObject ();
    }

    private static bool TryParseNamedTextStyle (string styleValue, out TextStyle parsedStyle)
    {
        parsedStyle = TextStyle.None;
        string[] parts = styleValue.Split (',', StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return false;
        }

        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace (part))
            {
                return false;
            }

            if (part [0] == '+' || part [0] == '-' || char.IsDigit (part [0]))
            {
                return false;
            }

            if (!Enum.TryParse (part, ignoreCase: true, out TextStyle parsedPart))
            {
                return false;
            }

            parsedStyle |= parsedPart;
        }

        return true;
    }
}
