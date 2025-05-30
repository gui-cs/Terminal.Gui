#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

// ReSharper disable StringLiteralTypo
/// <summary>Implements a JSON converter for <see cref="Drawing.Scheme"/>.</summary>
[RequiresUnreferencedCode ("AOT")]
internal class SchemeJsonConverter : JsonConverter<Scheme>
{
    /// <inheritdoc/>
    public override Scheme Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException ($"Unexpected StartObject token when parsing Scheme: {reader.TokenType}.");
        }

        var scheme = new Scheme ();
        var propertyName = string.Empty;

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return scheme;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException ($"After {propertyName}: Expected PropertyName but got another token when parsing Attribute: {reader.TokenType}.");
            }

            propertyName = reader.GetString ();
            reader.Read ();

            // Make sure attributes are marked as explicitly set when deserialized
            object? attrObj = JsonSerializer.Deserialize (ref reader, ConfigurationManager.SerializerContext.Attribute);

            if (attrObj is not Attribute attribute)
            {
                throw new JsonException ($"After {propertyName}: Expected Attribute but got {attrObj?.GetType ().Name ?? "null"} when parsing Scheme.");
            }
            if (propertyName is { })
            {
                scheme = propertyName.ToLowerInvariant () switch
                         {
                             "normal" => scheme with { Normal = attribute },
                             "hotnormal" => scheme with { HotNormal = attribute },
                             "focus" => scheme with { Focus = attribute },
                             "hotfocus" => scheme with { HotFocus = attribute },
                             "active" => scheme with { Active = attribute },
                             "hotactive" => scheme with { HotActive = attribute },
                             "highlight" => scheme with { Highlight = attribute },
                             "editable" => scheme with { Editable = attribute },
                             "readonly" => scheme with { ReadOnly = attribute },
                             "disabled" => scheme with { Disabled = attribute },
                             _ => throw new JsonException ($"{propertyName}: Unrecognized Scheme Attribute name.")
                         };
            }
            else
            {
                throw new JsonException ("null property name.");
            }
        }

        throw new JsonException ($"After {propertyName}: Invalid Json.");
    }

    /// <inheritdoc/>
    public override void Write (Utf8JsonWriter writer, Scheme value, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();

        foreach (VisualRole role in Enum.GetValues<VisualRole>())
        {
            // Get the attribute for the role

            if (!value.TryGetExplicitlySetAttributeForRole (role, out Attribute? attribute))
            {
                // Skip attributes that are not explicitly set
                continue;
            }
            writer.WritePropertyName (role.ToString ());
            // Write the attribute using the AttributeJsonConverter
            AttributeJsonConverter.Instance.Write (writer, attribute!.Value, options);
        }

        writer.WriteEndObject ();
    }
}
