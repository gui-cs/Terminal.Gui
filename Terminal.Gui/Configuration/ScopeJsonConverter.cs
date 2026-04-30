using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Terminal.Gui.Drawing;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Converts <see cref="Scope{T}"/> instances to/from JSON. Does all the heavy lifting of reading/writing config
///     data to/from <see cref="ConfigurationManager"/> JSON documents.
/// </summary>
/// <typeparam name="TScopeT"></typeparam>
internal class ScopeJsonConverter<
    [DynamicallyAccessedMembers (
                                    DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                                    | DynamicallyAccessedMemberTypes.PublicProperties)]
    TScopeT> : JsonConverter<TScopeT>
    where TScopeT : Scope<TScopeT>
{
    public override TScopeT Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException (
                                     $$"""Expected a JSON object ("{ "propName" : ... }"), but got "{{reader.TokenType}}"."""
                                    );
        }

        var scope = (TScopeT)Activator.CreateInstance (typeof (TScopeT))!;
        var propertyName = string.Empty;

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return scope!;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException ($"After {propertyName}: Expected a JSON property name, but got \"{reader.TokenType}\"");
            }

            propertyName = reader.GetString ();
            reader.Read ();

            // Get the hardcoded property from the TscopeT (e.g. ThemeScope.GetHardCodedProperty)
            ConfigProperty? configProperty = scope.GetHardCodedProperty (propertyName!);

            if (propertyName is { } && configProperty is { })
            {
                // This property name was found in the cached hard-coded scope dict.

                // Add it, with no value
                configProperty.HasValue = false;
                configProperty.PropertyValue = null;
                scope.TryAdd (propertyName, configProperty);

                // Figure out if it needs a JsonConverter and if so, create one
                Type? propertyType = configProperty?.PropertyInfo?.PropertyType!;

                if (configProperty?.ConverterType is { } converterType)
                {
                    if (TryReadWithKnownConverter (ref reader, propertyType!, converterType, options, out object? convertedValue))
                    {
                        scope! [propertyName].PropertyValue = convertedValue;

                        continue;
                    }

                    throw new JsonException (
                                             $"{propertyName}: Unsupported configuration converter type \"{converterType.FullName}\"."
                                            );
                }
                else
                {
                    scope! [propertyName].PropertyValue = DeserializePropertyValue (ref reader, propertyType!, options);
                }

                //Logging.Warning ($"{propertyName} = {scope! [propertyName].PropertyValue}");
            }
            else
            {
                // It is not a config property. Maybe it's just a property on the Scope with [JsonInclude]
                // like ScopeSettings.$schema.
                // If so, don't add it to the dictionary but apply it to the underlying property on
                // the scopeT.
                // BUGBUG: This is terrible design. The only time it's used is for $schema though.
                PropertyInfo? property = typeof (TScopeT)
                                               .GetProperties ()
                                               .Where (p =>
                                                       {
                                                            if (p.GetCustomAttribute (typeof (JsonIncludeAttribute)) is JsonIncludeAttribute { } jia)
                                                           {
                                                               var jsonPropertyNameAttribute =
                                                                   p.GetCustomAttribute (
                                                                                         typeof (JsonPropertyNameAttribute)
                                                                                        ) as
                                                                       JsonPropertyNameAttribute;

                                                               if (jsonPropertyNameAttribute?.Name == propertyName)
                                                               {
                                                                   // Bit of a hack, modifying propertyName in an enumerator...
                                                                   propertyName = p.Name;

                                                                   return true;
                                                               }

                                                               return p.Name == propertyName;
                                                           }

                                                           return false;
                                                       }
                                                      )
                                               .FirstOrDefault ();

                if (property is { })
                {
                    // Set the value of propertyName on the scopeT.
                    PropertyInfo prop = typeof (TScopeT).GetProperty (propertyName!)!;

                    prop.SetValue (scope, JsonSerializer.Deserialize (ref reader, prop.PropertyType, ConfigurationManager.SerializerContext));
                }
                else
                {
                    // Unknown property
                    // TODO: To support forward compatibility, we should just ignore unknown properties?
                    // TODO: Eg if we read an unknown property, it's possible that the property was added in a later version
                    throw new JsonException ($"{propertyName}: Unknown property name.");
                }
            }
        }

        throw new JsonException ($"{propertyName}: Json error in ScopeJsonConverter");
    }

    public override void Write (Utf8JsonWriter writer, TScopeT scope, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();

        IEnumerable<PropertyInfo> properties = typeof (TScopeT)
                                                     .GetProperties ()
                                                     .Where (p => p.GetCustomAttribute (typeof (JsonIncludeAttribute))
                                                                  != null
                                                            );

        foreach (PropertyInfo p in properties)
        {
            writer.WritePropertyName (ConfigProperty.GetJsonPropertyName (p));
            object? prop = typeof (TScopeT).GetProperty (p.Name)?.GetValue (scope);
            JsonSerializer.Serialize (writer, prop, prop!.GetType (), ConfigurationManager.SerializerContext);
        }

        foreach (KeyValuePair<string, ConfigProperty> p in from p in scope
                                                               .Where (cp => cp.Value.ScopeType == typeof (TScopeT).Name)
                                                             where p.Value.HasValue
                                                             select p)
        {
            writer.WritePropertyName (p.Key);
            Type? propertyType = p.Value.PropertyInfo?.PropertyType;

            if (propertyType != null
                && p.Value.ConverterType is { } converterType)
            {
                if (p.Value.PropertyValue is null)
                {
                    writer.WriteNullValue ();

                    continue;
                }

                if (TryWriteWithKnownConverter (writer, propertyType, converterType, p.Value.PropertyValue, options))
                {
                    continue;
                }

                throw new JsonException (
                                         $"{p.Key}: Unsupported configuration converter type \"{converterType.FullName}\"."
                                        );
            }
            else
            {
                object? prop = p.Value.PropertyValue;

                if (prop == null)
                {
                    writer.WriteNullValue ();
                }
                else
                {
                    if (TryWriteEnumValue (writer, prop.GetType (), prop))
                    {
                        continue;
                    }

                    JsonTypeInfo? jsonTypeInfo = ConfigurationManager.SerializerContext.GetTypeInfo (prop.GetType ());

                    if (jsonTypeInfo is null)
                    {
                        throw new JsonException ($"{p.Key}: No source-generated JsonTypeInfo is registered for {prop.GetType ().FullName}.");
                    }

                    JsonSerializer.Serialize (writer, prop, jsonTypeInfo);
                }
            }
        }

        writer.WriteEndObject ();
    }

    private static bool TryReadWithKnownConverter (
        ref Utf8JsonReader reader,
        Type propertyType,
        Type converterType,
        JsonSerializerOptions options,
        out object? value)
    {
        if (converterType == typeof (ConcurrentDictionaryJsonConverter<ThemeScope>))
        {
            value = new ConcurrentDictionaryJsonConverter<ThemeScope> ().Read (ref reader, propertyType, options);

            return true;
        }

        if (converterType == typeof (DictionaryJsonConverter<Scheme?>))
        {
            value = new DictionaryJsonConverter<Scheme?> ().Read (ref reader, propertyType, options);

            return true;
        }

        if (converterType == typeof (TraceCategoryJsonConverter))
        {
            value = new TraceCategoryJsonConverter ().Read (ref reader, propertyType, options);

            return true;
        }

        value = null;

        return false;
    }

    private static bool TryWriteWithKnownConverter (
        Utf8JsonWriter writer,
        Type propertyType,
        Type converterType,
        object? value,
        JsonSerializerOptions options)
    {
        if (converterType == typeof (ConcurrentDictionaryJsonConverter<ThemeScope>))
        {
            new ConcurrentDictionaryJsonConverter<ThemeScope> ().Write (
                                                                   writer,
                                                                   (ConcurrentDictionary<string, ThemeScope>)value!,
                                                                   options
                                                                  );

            return true;
        }

        if (converterType == typeof (DictionaryJsonConverter<Scheme?>))
        {
            new DictionaryJsonConverter<Scheme?> ().Write (writer, (Dictionary<string, Scheme?>)value!, options);

            return true;
        }

        if (converterType == typeof (TraceCategoryJsonConverter))
        {
            new TraceCategoryJsonConverter ().Write (writer, (TraceCategory)value!, options);

            return true;
        }

        return false;
    }

    private static object? DeserializePropertyValue (ref Utf8JsonReader reader, Type propertyType, JsonSerializerOptions options)
    {
        Type? nullableType = Nullable.GetUnderlyingType (propertyType);
        Type enumType = nullableType ?? propertyType;

        if (enumType.IsEnum)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return Enum.Parse (enumType, reader.GetString ()!, ignoreCase: true);
            }

            if (reader.TokenType == JsonTokenType.Null && nullableType is not null)
            {
                return null;
            }
        }

        JsonTypeInfo? jsonTypeInfo = ConfigurationManager.SerializerContext.GetTypeInfo (propertyType);

        if (jsonTypeInfo is null)
        {
            throw new JsonException ($"{propertyType.FullName}: No source-generated JsonTypeInfo is registered for this configuration property type.");
        }

        return JsonSerializer.Deserialize (ref reader, jsonTypeInfo);
    }

    private static bool TryWriteEnumValue (Utf8JsonWriter writer, Type propertyType, object value)
    {
        Type enumType = Nullable.GetUnderlyingType (propertyType) ?? propertyType;

        if (!enumType.IsEnum)
        {
            return false;
        }

        writer.WriteStringValue (value.ToString ());

        return true;
    }
}
