#nullable enable

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Terminal.Gui.ConfigurationTests;

public class ConfigurationPropertyAttributeTests
{

    /// <summary>
    ///     If this test fails, you need to add a new property to <see cref="SourceGenerationContext"/> to support serialization of the new property type.
    /// </summary>
    [Fact]
    public void Verify_Types_Added_To_JsonSerializerContext ()
    {
        // The assembly containing the types to inspect
        var assembly = Assembly.GetAssembly (typeof (SourceGenerationContext));

        // Get all types from the assembly
        var types = assembly!.GetTypes ();

        // Find all properties with the [ConfigurationProperty] attribute
        var properties = new List<PropertyInfo> ();
        foreach (var type in types)
        {
            properties.AddRange (type.GetProperties ().Where (p =>
                p.GetCustomAttributes (typeof (ConfigurationPropertyAttribute), false).Any ()));
        }

        // Get the types of the properties
        var propertyTypes = properties.Select (p => p.PropertyType).Distinct ();

        // Get the types registered in the JsonSerializerContext derived class
        var contextType = typeof (SourceGenerationContext);
        var contextTypes = GetRegisteredTypes (contextType);

        // Ensure all property types are included in the JsonSerializerContext derived class
        IEnumerable<Type> collection = contextTypes as Type [] ?? contextTypes.ToArray ();

        foreach (var type in propertyTypes)
        {
            Assert.Contains (type, collection);
        }

        // Ensure no property has the generic JsonStringEnumConverter<>
        EnsureNoSpecifiedConverters (properties, new [] { typeof (JsonStringEnumConverter<>) });
        // Ensure no property has the type RuneJsonConverter
        EnsureNoSpecifiedConverters (properties, new [] { typeof (RuneJsonConverter) });
        // Ensure no property has the type KeyJsonConverter
        EnsureNoSpecifiedConverters (properties, new [] { typeof (KeyJsonConverter) });

        // Find all classes with the JsonConverter attribute of type ScopeJsonConverter<>
        var classesWithScopeJsonConverter = types.Where (t =>
            t.GetCustomAttributes (typeof (JsonConverterAttribute), false)
            .Any (attr => ((JsonConverterAttribute)attr).ConverterType!.IsGenericType &&
                         ((JsonConverterAttribute)attr).ConverterType!.GetGenericTypeDefinition () == typeof (ScopeJsonConverter<>)));

        // Ensure all these classes are included in the JsonSerializerContext derived class
        foreach (var type in classesWithScopeJsonConverter)
        {
            Assert.Contains (type, collection);
        }
    }


    [Fact]
    public void OmitClassName_Omits ()
    {
        // Color.Schemes is serialized as "Schemes", not "Colors.Schemes"
        PropertyInfo pi = typeof (SchemeManager).GetProperty ("Schemes")!;
        var scp = (ConfigurationPropertyAttribute)pi!.GetCustomAttribute (typeof (ConfigurationPropertyAttribute))!;
        Assert.True (scp!.Scope == typeof (ThemeScope));
        Assert.True (scp.OmitClassName);
    }

    private static IEnumerable<Type> GetRegisteredTypes (Type contextType)
    {
        // Use reflection to find which types are registered in the JsonSerializerContext
        var registeredTypes = new List<Type> ();

        var properties = contextType.GetProperties (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition () == typeof (JsonTypeInfo<>))
            {
                registeredTypes.Add (property.PropertyType.GetGenericArguments () [0]);
            }
        }

        return registeredTypes.Distinct ();
    }

    private static void EnsureNoSpecifiedConverters (List<PropertyInfo> properties, IEnumerable<Type> converterTypes)
    {
        // Ensure no property has any of the specified converter types
        foreach (var property in properties)
        {
            var jsonConverterAttributes = property.GetCustomAttributes (typeof (JsonConverterAttribute), false)
                                                  .Cast<JsonConverterAttribute> ();

            foreach (var attribute in jsonConverterAttributes)
            {
                foreach (var converterType in converterTypes)
                {
                    if (attribute.ConverterType!.IsGenericType &&
                        attribute.ConverterType.GetGenericTypeDefinition () == converterType)
                    {
                        Assert.Fail ($"Property '{property.Name}' should not use the converter '{converterType.Name}'.");
                    }

                    if (!attribute.ConverterType!.IsGenericType &&
                        attribute.ConverterType == converterType)
                    {
                        Assert.Fail ($"Property '{property.Name}' should not use the converter '{converterType.Name}'.");
                    }
                }
            }
        }
    }
}
