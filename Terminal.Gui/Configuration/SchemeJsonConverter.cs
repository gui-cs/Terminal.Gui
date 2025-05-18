using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

// ReSharper disable StringLiteralTypo
/// <summary>Implements a JSON converter for <see cref="Scheme"/>.</summary>
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

        // Create a default scheme with all attributes marked as implicit
        var scheme = new Scheme (Attribute.Default.AsImplicit ());
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
            var attribute = JsonSerializer.Deserialize (ref reader, ConfigurationManager.SerializerContext.Attribute)
                                          .AsExplicitlySet ();

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

        // Always write Normal
        writer.WritePropertyName ("Normal");
        AttributeJsonConverter.Instance.Write (writer, value.Normal, options);

        // Only write explicitly set attributes
        if (value.HotNormal.IsExplicitlySet)
        {
            writer.WritePropertyName ("HotNormal");
            AttributeJsonConverter.Instance.Write (writer, value.HotNormal, options);
        }

        if (value.Focus.IsExplicitlySet)
        {
            writer.WritePropertyName ("Focus");
            AttributeJsonConverter.Instance.Write (writer, value.Focus, options);
        }

        if (value.HotFocus.IsExplicitlySet)
        {
            writer.WritePropertyName ("HotFocus");
            AttributeJsonConverter.Instance.Write (writer, value.HotFocus, options);
        }

        if (value.Active.IsExplicitlySet)
        {
            writer.WritePropertyName ("Active");
            AttributeJsonConverter.Instance.Write (writer, value.Active, options);
        }

        if (value.HotActive.IsExplicitlySet)
        {
            writer.WritePropertyName ("HotActive");
            AttributeJsonConverter.Instance.Write (writer, value.HotActive, options);
        }

        if (value.Highlight.IsExplicitlySet)
        {
            writer.WritePropertyName ("Highlight");
            AttributeJsonConverter.Instance.Write (writer, value.Highlight, options);
        }

        if (value.Editable.IsExplicitlySet)
        {
            writer.WritePropertyName ("Editable");
            AttributeJsonConverter.Instance.Write (writer, value.Editable, options);
        }

        if (value.ReadOnly.IsExplicitlySet)
        {
            writer.WritePropertyName ("ReadOnly");
            AttributeJsonConverter.Instance.Write (writer, value.ReadOnly, options);
        }

        if (value.Disabled.IsExplicitlySet)
        {
            writer.WritePropertyName ("Disabled");
            AttributeJsonConverter.Instance.Write (writer, value.Disabled, options);
        }

        writer.WriteEndObject ();
    }
}
