#nullable enable

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Terminal.Gui.ConfigurationTests;

public class SerializableConfigurationPropertyTests
{
    [Fact]
    public void Test_SerializableConfigurationProperty_Types_Added_To_JsonSerializerContext ()
    {
        // The assembly containing the types to inspect
        var assembly = Assembly.GetAssembly (typeof (SourceGenerationContext));

        // Get all types from the assembly
        var types = assembly!.GetTypes ();

        // Find all properties with the SerializableConfigurationProperty attribute
        var properties = new List<PropertyInfo> ();
        foreach (var type in types)
        {
            properties.AddRange (type.GetProperties ().Where (p =>
                p.GetCustomAttributes (typeof (SerializableConfigurationProperty), false).Any ()));
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
        foreach (var property in properties)
        {
            var jsonConverterAttributes = property.GetCustomAttributes (typeof (JsonConverterAttribute), false)
                .Cast<JsonConverterAttribute> ();

            foreach (var attribute in jsonConverterAttributes)
            {
                Assert.False (attribute.ConverterType!.IsGenericType &&
                             attribute.ConverterType.GetGenericTypeDefinition () == typeof (JsonStringEnumConverter<>));
            }
        }

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

    private IEnumerable<Type> GetRegisteredTypes (Type contextType)
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
}
