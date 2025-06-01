#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Converts <see cref="Scope{T}"/> instances to/from JSON. Does all the heavy lifting of reading/writing config
///     data to/from <see cref="ConfigurationManager"/> JSON documents.
/// </summary>
/// <typeparam name="TScopeT"></typeparam>
[RequiresUnreferencedCode ("AOT")]
internal class ScopeJsonConverter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TScopeT> : JsonConverter<TScopeT>
    where TScopeT : Scope<TScopeT>
{
    [RequiresDynamicCode ("Calls System.Type.MakeGenericType(params Type[])")]
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public override TScopeT Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
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

                if (configProperty?.PropertyInfo?.GetCustomAttribute (typeof (JsonConverterAttribute)) is
                    JsonConverterAttribute jca)
                {
                    object? converter = Activator.CreateInstance (jca.ConverterType!)!;

                    if (converter.GetType ().BaseType == typeof (JsonConverterFactory))
                    {
                        var factory = (JsonConverterFactory)converter;

                        if (factory.CanConvert (propertyType))
                        {
                            converter = factory.CreateConverter (propertyType, options);
                        }
                    }

                    try
                    {
                        var type = (Type?)typeof (ReadHelper<>).MakeGenericType (typeof (TScopeT), propertyType!);
                        var readHelper = Activator.CreateInstance (type!, converter) as ReadHelper;

                        scope! [propertyName].PropertyValue = readHelper?.Read (ref reader, propertyType!, options);
                    }
                    catch (NotSupportedException e)
                    {
                        throw new JsonException (
                                                 $"{propertyName}: Error reading property of type \"{propertyType?.Name}\".",
                                                 e
                                                );
                    }
                    catch (TargetInvocationException)
                    {
                        // QUESTION: Should we try/catch here?
                        scope! [propertyName].PropertyValue = JsonSerializer.Deserialize (ref reader, propertyType!, options);
                    }
                }
                else
                {
                    // QUESTION: Should we try/catch here?
                    scope! [propertyName].PropertyValue = JsonSerializer.Deserialize (ref reader, propertyType!, ConfigurationManager.SerializerContext);
                }

                //Logging.Warning ($"{propertyName} = {scope! [propertyName].PropertyValue}");
            }
            else
            {
                // It is not a config property. Maybe it's just a property on the Scope with [JsonInclude]
                // like ScopeSettings.$schema.
                // If so, don't add it to the dictionary but apply it to the underlying property on 
                // the scopeT. 
                // BUGBUG: This is a really bad design. The only time it's used is for $schema though.
                PropertyInfo? property = scope!.GetType ()
                                               .GetProperties ()
                                               .Where (
                                                       p =>
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
                    PropertyInfo prop = scope.GetType ().GetProperty (propertyName!)!;
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

    [UnconditionalSuppressMessage (
                                      "AOT",
                                      "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                      Justification = "<Pending>")]
    public override void Write (Utf8JsonWriter writer, TScopeT scope, JsonSerializerOptions options)
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
            JsonSerializer.Serialize (writer, prop, prop!.GetType (), ConfigurationManager.SerializerContext);
        }

        foreach (KeyValuePair<string, ConfigProperty> p in from p in scope
                                                               .Where (
                                                                       cp =>
                                                                           cp.Value.PropertyInfo?.GetCustomAttribute (
                                                                                    typeof (
                                                                                        ConfigurationPropertyAttribute)
                                                                                   )
                                                                               is
                                                                               ConfigurationPropertyAttribute scp
                                                                           && scp?.Scope == typeof (TScopeT)
                                                                      )
                                                           where p.Value.HasValue
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
                             ?.Invoke (converter, [writer, p.Value.PropertyValue, options]);
                }
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
                    JsonSerializer.Serialize (writer, prop, prop.GetType (), ConfigurationManager.SerializerContext);
                }
            }
        }

        writer.WriteEndObject ();
    }

    // See: https://stackoverflow.com/questions/60830084/how-to-pass-an-argument-by-reference-using-reflection
    internal abstract class ReadHelper
    {
        public abstract object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
    }

    [method: RequiresUnreferencedCode ("Calls System.Delegate.CreateDelegate(Type, Object, String)")]
    internal class ReadHelper<TConverter> (object converter) : ReadHelper
    {
        private readonly ReadDelegate _readDelegate = (ReadDelegate)Delegate.CreateDelegate (typeof (ReadDelegate), converter, "Read");

        public override object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return _readDelegate.Invoke (ref reader, type, options);
        }

        private delegate TConverter ReadDelegate (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
    }
}
