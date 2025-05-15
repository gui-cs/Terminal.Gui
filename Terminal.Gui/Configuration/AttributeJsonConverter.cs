using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Json converter from the <see cref="Attribute"/> class.</summary>
[RequiresUnreferencedCode ("AOT")]

internal class AttributeJsonConverter : JsonConverter<Attribute>
{
    private static AttributeJsonConverter _instance;

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

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (foreground is null || background is null)
                {
                    throw new JsonException ("Both Foreground and Background colors must be provided.");
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
                throw new JsonException ($"Unexpected token when parsing Attribute: {reader.TokenType}.");
            }

            string propertyName = reader.GetString ();
            reader.Read ();
            var property = $"\"{reader.GetString ()}\"";

            switch (propertyName?.ToLower ())
            {
                case "foreground":
                    foreground = JsonSerializer.Deserialize (property, ConfigurationManager.SerializerContext.Color);

                    break;
                case "background":
                    background = JsonSerializer.Deserialize (property, ConfigurationManager.SerializerContext.Color);

                    break;
                case "style":
                    style = JsonSerializer.Deserialize (property, ConfigurationManager.SerializerContext.TextStyle);

                    break;
                //case "bright":
                //case "bold":
                //	attribute.Bright = reader.GetBoolean ();
                //	break;
                //case "dim":
                //	attribute.Dim = reader.GetBoolean ();
                //	break;
                //case "underline":
                //	attribute.Underline = reader.GetBoolean ();
                //	break;
                //case "blink":
                //	attribute.Blink = reader.GetBoolean ();
                //	break;
                //case "reverse":
                //	attribute.Reverse = reader.GetBoolean ();
                //	break;
                //case "hidden":
                //	attribute.Hidden = reader.GetBoolean ();
                //	break;
                //case "strike-through":
                //	attribute.StrikeThrough = reader.GetBoolean ();
                //	break;				
                default:
                    throw new JsonException ($"Unknown Attribute property {propertyName}.");
            }
        }

        throw new JsonException ("Attribute");
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
            JsonSerializer.Serialize (writer, value.Style, ConfigurationManager.SerializerContext.TextStyle);
        }

        writer.WriteEndObject ();
    }
}
