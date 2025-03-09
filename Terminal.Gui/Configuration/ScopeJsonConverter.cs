#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Converts <see cref="Scope{T}"/> instances to/from JSON. Does all the heavy lifting of reading/writing config
///     data to/from <see cref="ConfigurationManager"/> JSON documents.
/// </summary>
/// <typeparam name="scopeT"></typeparam>
internal class ScopeJsonConverter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] scopeT> : JsonConverter<scopeT> where scopeT : Scope<scopeT>
{
    [RequiresDynamicCode ("Calls System.Type.MakeGenericType(params Type[])")]
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public override scopeT Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException (
                                     $"Expected a JSON object (\"{{ \"propName\" : ... }}\"), but got \"{reader.TokenType}\"."
                                    );
        }

        var scope = (scopeT)Activator.CreateInstance (typeof (scopeT))!;

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return scope!;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException ($"Expected a JSON property name, but got \"{reader.TokenType}\".");
            }

            string? propertyName = reader.GetString ();
            reader.Read ();

            if (propertyName is { } && scope!.TryGetValue (propertyName, out ConfigProperty? configProp))
            {
                // This property name was found in the Scope's ScopeProperties dictionary
                // Figure out if it needs a JsonConverter and if so, create one
                Type? propertyType = configProp?.PropertyInfo?.PropertyType!;

                if (configProp?.PropertyInfo?.GetCustomAttribute (typeof (JsonConverterAttribute)) is
                    JsonConverterAttribute jca)
                {
                    object? converter = Activator.CreateInstance (jca.ConverterType!)!;

                    if (converter.GetType ().BaseType == typeof (JsonConverterFactory))
                    {
                        var factory = (JsonConverterFactory)converter;

                        if (propertyType is { } && factory.CanConvert (propertyType))
                        {
                            converter = factory.CreateConverter (propertyType, options);
                        }
                    }

                    var readHelper = Activator.CreateInstance (
                                                               (Type?)typeof (ReadHelper<>).MakeGenericType (
                                                                    typeof (scopeT),
                                                                    propertyType!
                                                                   )!,
                                                               converter
                                                              ) as ReadHelper;

                    try
                    {
                        scope! [propertyName].PropertyValue = readHelper?.Read (ref reader, propertyType!, options);
                    }
                    catch (NotSupportedException e)
                    {
                        throw new JsonException (
                                                 $"Error reading property \"{propertyName}\" of type \"{propertyType?.Name}\".",
                                                 e
                                                );
                    }
                }
                else
                {
                    try
                    {
                        scope! [propertyName].PropertyValue =
                            JsonSerializer.Deserialize (ref reader, propertyType!, SerializerContext);
                    }
                    catch (Exception)
                    {
                       // Logging.Trace ($"scopeT Read: {ex}");
                    }
                }
            }
            else
            {
                // It is not a config property. Maybe it's just a property on the Scope with [JsonInclude]
                // like ScopeSettings.$schema...
                PropertyInfo? property = scope!.GetType ()
                                               .GetProperties ()
                                               .Where (
                                                       p =>
                                                       {
                                                           var jia =
                                                               p.GetCustomAttribute (typeof (JsonIncludeAttribute)) as
                                                                   JsonIncludeAttribute;

                                                           if (jia is { })
                                                           {
                                                               var jpna =
                                                                   p.GetCustomAttribute (
                                                                                         typeof (JsonPropertyNameAttribute)
                                                                                        ) as
                                                                       JsonPropertyNameAttribute;

                                                               if (jpna?.Name == propertyName)
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
                    PropertyInfo prop = scope.GetType ().GetProperty (propertyName!)!;
                    prop.SetValue (scope, JsonSerializer.Deserialize (ref reader, prop.PropertyType, SerializerContext));
                }
                else
                {
                    // Unknown property
                    throw new JsonException ($"Unknown property name \"{propertyName}\".");
                }
            }
        }

        throw new JsonException ();
    }

    public override void Write (Utf8JsonWriter writer, scopeT scope, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();

        IEnumerable<PropertyInfo> properties = scope!.GetType ()
                                                     .GetProperties ()
                                                     .Where (
                                                             p => p.GetCustomAttribute (typeof (JsonIncludeAttribute))
                                                                  != null
                                                            );

        foreach (PropertyInfo p in properties)
        {
            writer.WritePropertyName (ConfigProperty.GetJsonPropertyName (p));
            object? prop = scope.GetType ().GetProperty (p.Name)?.GetValue (scope);
            JsonSerializer.Serialize (writer, prop, prop!.GetType (), SerializerContext);
        }

        foreach (KeyValuePair<string, ConfigProperty> p in from p in scope
                                                               .Where (
                                                                       cp =>
                                                                           cp.Value.PropertyInfo?.GetCustomAttribute (
                                                                                    typeof (
                                                                                        SerializableConfigurationProperty)
                                                                                   )
                                                                               is
                                                                               SerializableConfigurationProperty scp
                                                                           && scp?.Scope == typeof (scopeT)
                                                                      )
                                                           where p.Value.PropertyValue != null
                                                           select p)
        {
            writer.WritePropertyName (p.Key);
            Type? propertyType = p.Value.PropertyInfo?.PropertyType;

            if (propertyType != null
                && p.Value.PropertyInfo?.GetCustomAttribute (typeof (JsonConverterAttribute)) is JsonConverterAttribute
                    jca)
            {
                object converter = Activator.CreateInstance (jca.ConverterType!)!;

                if (converter.GetType ().BaseType == typeof (JsonConverterFactory))
                {
                    var factory = (JsonConverterFactory)converter;

                    if (factory.CanConvert (propertyType))
                    {
                        converter = factory.CreateConverter (propertyType, options)!;
                    }
                }

                if (p.Value.PropertyValue is { })
                {
                    converter.GetType ()
                             .GetMethod ("Write")
                             ?.Invoke (converter, new [] { writer, p.Value.PropertyValue, options });
                }
            }
            else
            {
                object? prop = p.Value.PropertyValue;
                JsonSerializer.Serialize (writer, prop, prop!.GetType (), SerializerContext);
            }
        }

        writer.WriteEndObject ();
    }

    // See: https://stackoverflow.com/questions/60830084/how-to-pass-an-argument-by-reference-using-reflection
    internal abstract class ReadHelper
    {
        public abstract object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
    }

    internal class ReadHelper<converterT> : ReadHelper
    {
        private readonly ReadDelegate _readDelegate;

        [RequiresUnreferencedCode ("Calls System.Delegate.CreateDelegate(Type, Object, String)")]
        public ReadHelper (object converter) { _readDelegate = (ReadDelegate)Delegate.CreateDelegate (typeof (ReadDelegate), converter, "Read"); }

        public override object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return _readDelegate.Invoke (ref reader, type, options);
        }

        private delegate converterT ReadDelegate (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
    }
}
