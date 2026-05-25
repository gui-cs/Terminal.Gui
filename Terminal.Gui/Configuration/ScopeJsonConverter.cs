using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

#pragma warning disable CS0618 // Obsolete - ScopeJsonConverter still uses ConfigurationManager internally during transition

namespace Terminal.Gui.Configuration;

/// <summary>
///     Converts <see cref="Scope{T}"/> instances to/from JSON. Does all the heavy lifting of reading/writing config
///     data to/from <see cref="ConfigurationManager"/> JSON documents.
/// </summary>
/// <typeparam name="TScopeT"></typeparam>
internal class ScopeJsonConverter<
    [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                                 | DynamicallyAccessedMemberTypes.PublicProperties)]
    TScopeT> : JsonConverter<TScopeT> where TScopeT : Scope<TScopeT>
{
    [UnconditionalSuppressMessage ("AOT",
                                   "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                   Justification =
                                       "Arbitrary property-level converter fallback is guarded by RuntimeFeature.IsDynamicCodeSupported and is unreachable under NativeAOT.")]
    [UnconditionalSuppressMessage ("Trimming",
                                   "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                                   Justification =
                                       "Arbitrary property-level converter fallback is only used when a consumer opts into a custom property-level JsonConverter on JIT-supported runtimes.")]
    public override TScopeT Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException ($$"""Expected a JSON object ("{ "propName" : ... }"), but got "{{reader.TokenType}}".""");
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
                    if (reader.TokenType == JsonTokenType.Null && CanAcceptNullValue (propertyType!))
                    {
                        scope! [propertyName].PropertyValue = null;

                        continue;
                    }

                    if (TryReadWithKnownConverter (ref reader, propertyType!, converterType, options, out object? convertedValue))
                    {
                        scope! [propertyName].PropertyValue = convertedValue;

                        continue;
                    }

                    if (TryReadWithDynamicConverter (ref reader, propertyType!, converterType, options, out convertedValue))
                    {
                        scope! [propertyName].PropertyValue = convertedValue;

                        continue;
                    }

                    throw new JsonException ($"{
                        propertyName
                    }: Unsupported configuration converter type \"{
                        converterType.FullName
                    }\" when dynamic code is unavailable.");
                }
                scope! [propertyName].PropertyValue = DeserializePropertyValue (ref reader, propertyType!, options);

                // Logging.Warning ($"{propertyName} = {scope! [propertyName].PropertyValue}");
            }
            else
            {
                // It is not a config property. Maybe it's just a property on the Scope with [JsonInclude]
                // like ScopeSettings.$schema.
                // If so, don't add it to the dictionary but apply it to the underlying property on
                // the scopeT.
                // BUGBUG: This is terrible design. The only time it's used is for $schema though.
                PropertyInfo? property = typeof (TScopeT).GetProperties ()
                                                         .Where (p =>
                                                                 {
                                                                     if (p.GetCustomAttribute (typeof (JsonIncludeAttribute)) is JsonIncludeAttribute { } jia)
                                                                     {
                                                                         var jsonPropertyNameAttribute =
                                                                             p.GetCustomAttribute (typeof (JsonPropertyNameAttribute)) as
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
                                                                 })
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
                    throw new JsonException ($"{propertyName}: Unknown property name.");
                }
            }
        }

        throw new JsonException ($"{propertyName}: Json error in ScopeJsonConverter");
    }

    [UnconditionalSuppressMessage ("AOT",
                                   "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                   Justification =
                                       "Arbitrary property-level converter fallback is guarded by RuntimeFeature.IsDynamicCodeSupported and is unreachable under NativeAOT.")]
    [UnconditionalSuppressMessage ("Trimming",
                                   "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                                   Justification =
                                       "Arbitrary property-level converter fallback is only used when a consumer opts into a custom property-level JsonConverter on JIT-supported runtimes.")]
    public override void Write (Utf8JsonWriter writer, TScopeT scope, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();

        IEnumerable<PropertyInfo> properties = typeof (TScopeT).GetProperties ().Where (p => p.GetCustomAttribute (typeof (JsonIncludeAttribute)) != null);

        foreach (PropertyInfo p in properties)
        {
            writer.WritePropertyName (ConfigProperty.GetJsonPropertyName (p));
            object? prop = p.GetValue (scope);
            JsonSerializer.Serialize (writer, prop, p.PropertyType, ConfigurationManager.SerializerContext);
        }

        foreach (KeyValuePair<string, ConfigProperty> p in from p in scope.Where (cp => cp.Value.ScopeType == typeof (TScopeT).Name)
                                                           where p.Value.HasValue
                                                           select p)
        {
            Type? propertyType = p.Value.PropertyInfo?.PropertyType;
            object? propertyValue = p.Value.PropertyValue;

            if (ShouldSkipNullPropertyValue (propertyType, propertyValue))
            {
                continue;
            }

            writer.WritePropertyName (p.Key);

            if (propertyType != null && p.Value.ConverterType is { } converterType)
            {
                if (propertyValue is null)
                {
                    writer.WriteNullValue ();

                    continue;
                }

                if (TryWriteWithKnownConverter (writer, propertyType, converterType, propertyValue, options))
                {
                    continue;
                }

                if (TryWriteWithDynamicConverter (writer, propertyType, converterType, propertyValue, options))
                {
                    continue;
                }

                throw new JsonException ($"{p.Key}: Unsupported configuration converter type \"{converterType.FullName}\" when dynamic code is unavailable.");
            }
            object? prop = propertyValue;

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

        writer.WriteEndObject ();
    }

    private static bool CanAcceptNullValue (Type propertyType) => !propertyType.IsValueType || Nullable.GetUnderlyingType (propertyType) is { };

    private static object? DeserializePropertyValue (ref Utf8JsonReader reader, Type propertyType, JsonSerializerOptions options)
    {
        Type? nullableType = Nullable.GetUnderlyingType (propertyType);
        Type enumType = nullableType ?? propertyType;

        if (enumType.IsEnum)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return Enum.Parse (enumType, reader.GetString ()!, true);
            }

            if (reader.TokenType == JsonTokenType.Null && nullableType is { })
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

    private static bool ShouldSkipNullPropertyValue (Type? propertyType, object? propertyValue)
    {
        if (propertyValue is { })
        {
            return false;
        }

        if (propertyType is null)
        {
            return false;
        }

        return propertyType.IsValueType && Nullable.GetUnderlyingType (propertyType) is null;
    }

    [RequiresDynamicCode ("Instantiates arbitrary property-level JsonConverter types for JIT-only configuration paths.")]
    [RequiresUnreferencedCode ("Arbitrary property-level JsonConverter types may access unreferenced members.")]
    private static bool TryReadWithDynamicConverter (ref Utf8JsonReader reader,
                                                     Type propertyType,
                                                     Type converterType,
                                                     JsonSerializerOptions options,
                                                     out object? value)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            value = null;

            return false;
        }

        object converter = Activator.CreateInstance (converterType)!;

        if (converter is JsonConverterFactory factory && factory.CanConvert (propertyType))
        {
            converter = factory.CreateConverter (propertyType, options)!;
        }

        try
        {
            Type helperType = typeof (ReadHelper<>).MakeGenericType (typeof (TScopeT), propertyType);
            var readHelper = (ReadHelper)Activator.CreateInstance (helperType, converter)!;
            value = readHelper.Read (ref reader, propertyType, options);

            return true;
        }
        catch (NotSupportedException e)
        {
            throw new JsonException ($"{propertyType.Name}: Error reading property with converter \"{converterType.FullName}\".", e);
        }
        catch (TargetInvocationException)
        {
            value = JsonSerializer.Deserialize (ref reader, propertyType, options);

            return true;
        }
    }

    private static bool TryReadWithKnownConverter (ref Utf8JsonReader reader,
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

    [RequiresDynamicCode ("Instantiates arbitrary property-level JsonConverter types for JIT-only configuration paths.")]
    [RequiresUnreferencedCode ("Arbitrary property-level JsonConverter types may access unreferenced members.")]
    private static bool TryWriteWithDynamicConverter (Utf8JsonWriter writer, Type propertyType, Type converterType, object value, JsonSerializerOptions options)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return false;
        }

        object converter = Activator.CreateInstance (converterType)!;

        if (converter is JsonConverterFactory factory && factory.CanConvert (propertyType))
        {
            converter = factory.CreateConverter (propertyType, options)!;
        }

        MethodInfo? writeMethod = converter.GetType ().GetMethod (nameof (Write), [typeof (Utf8JsonWriter), propertyType, typeof (JsonSerializerOptions)]);

        if (writeMethod is null)
        {
            throw new JsonException ($"{propertyType.Name}: Converter \"{converterType.FullName}\" does not expose a compatible Write method.");
        }

        try
        {
            writeMethod.Invoke (converter, [writer, value, options]);

            return true;
        }
        catch (TargetInvocationException e) when (e.InnerException is { })
        {
            throw new JsonException ($"{propertyType.Name}: Error writing property with converter \"{converterType.FullName}\".", e.InnerException);
        }
    }

    private static bool TryWriteWithKnownConverter (Utf8JsonWriter writer, Type propertyType, Type converterType, object? value, JsonSerializerOptions options)
    {
        if (converterType == typeof (ConcurrentDictionaryJsonConverter<ThemeScope>))
        {
            new ConcurrentDictionaryJsonConverter<ThemeScope> ().Write (writer, (ConcurrentDictionary<string, ThemeScope>)value!, options);

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

    internal abstract class ReadHelper
    {
        public abstract object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
    }

    [method: RequiresUnreferencedCode ("Creates delegates for arbitrary property-level JsonConverter.Read methods on JIT-only paths.")]
    internal class ReadHelper<TValue> (object converter) : ReadHelper
    {
        private readonly ReadDelegate _readDelegate = (ReadDelegate)Delegate.CreateDelegate (typeof (ReadDelegate), converter, "Read");

        public override object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => _readDelegate.Invoke (ref reader, type, options);

        private delegate TValue ReadDelegate (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
    }
}
