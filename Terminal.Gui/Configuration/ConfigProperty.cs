#nullable enable
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Holds a property's value and the <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> to
///     get and set the property's value.
/// </summary>
/// <remarks>
///     Configuration properties must be <see langword="public"/> and <see langword="static"/> and have the
///     <see cref="SerializableConfigurationProperty"/> attribute. If the type of the property requires specialized JSON
///     serialization, a <see cref="JsonConverter"/> must be provided using the <see cref="JsonConverterAttribute"/>
///     attribute.
/// </remarks>
public class ConfigProperty
{
    /// <summary>Describes the property.</summary>
    public PropertyInfo? PropertyInfo { get; set; }

    private object? _propertyValue;

    /// <summary>
    ///     Holds the property's value as it was either read from the class's implementation or from a config file. If the
    ///     property has not been set (e.g. because no configuration file specified a value), this will be
    ///     <see langword="null"/>.
    /// </summary>
    /// <remarks>
    ///     On <see langword="set"/>, performs a sparse-copy of the new value to the existing value (only copies elements
    ///     of the object that are non-null).
    /// </remarks>
    public object? PropertyValue
    {
        get => _propertyValue;
        set
        {
            _propertyValue = value;
            HasValue = true;
        }
    }

    public bool HasValue { get; set; }

    /// <summary>Applies the <see cref="PropertyValue"/> to the static property described by <see cref="PropertyInfo"/>.</summary>
    /// <returns></returns>
    public bool Apply ()
    {
        try
        {
            if (PropertyInfo?.GetValue (null) is { })
            {
                var val = ScopeExtensions.DeepMemberWiseCopy (PropertyValue, PropertyInfo?.GetValue (null));
                PropertyInfo?.SetValue (null, val);
            }
        }
        catch (TargetInvocationException tie)
        {
            // Check if there is an inner exception
            if (tie.InnerException is { })
            {
                // Handle the inner exception separately without catching the outer exception
                Exception? innerException = tie.InnerException;

                // Handle the inner exception here
                throw new JsonException (
                                         $"Error Applying Configuration Change: {innerException.Message}",
                                         innerException
                                        );
            }

            // Handle the outer exception or rethrow it if needed
            throw new JsonException ($"Error Applying Configuration Change: {tie.Message}", tie);
        }
        catch (ArgumentException ae)
        {
            throw new JsonException (
                                     $"Error Applying Configuration Change ({PropertyInfo?.Name}): {ae.Message}",
                                     ae
                                    );
        }

        return PropertyValue != null;
    }

    /// <summary>
    ///     Helper to get either the Json property named (specified by [JsonPropertyName(name)] or the actual property
    ///     name.
    /// </summary>
    /// <param name="pi"></param>
    /// <returns></returns>
    public static string GetJsonPropertyName (PropertyInfo pi)
    {
        var attr = pi.GetCustomAttribute (typeof (JsonPropertyNameAttribute)) as JsonPropertyNameAttribute;

        return attr?.Name ?? pi.Name;
    }

    /// <summary>
    ///     Retrieves (using reflection) the value of the static  <see cref="SerializableConfigurationProperty"/>
    ///     property described in <see cref="PropertyInfo"/> into
    ///     <see cref="PropertyValue"/>.
    /// </summary>
    /// <returns></returns>
    public object? RetrieveValue () { return PropertyValue = PropertyInfo!.GetValue (null); }

    /// <summary>
    ///     Updates (using reflection) <see cref="PropertyValue"/> with the value in <paramref name="source"/> using a deep memberwise copy.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal object? UpdateValueFrom (object source)
    {
        if (source is null)
        {
            return PropertyValue;
        }

        Type? ut = Nullable.GetUnderlyingType (PropertyInfo!.PropertyType);

        if (source.GetType () != PropertyInfo!.PropertyType && ut is { } && source.GetType () != ut)
        {
            throw new ArgumentException (
                                         $"The source object ({PropertyInfo!.DeclaringType}.{PropertyInfo!.Name}) is not of type {PropertyInfo!.PropertyType}."
                                        );
        }

        if (PropertyValue is { })
        {
            PropertyValue = ScopeExtensions.DeepMemberWiseCopy (source, PropertyValue);
        }
        else
        {
            PropertyValue = source;
        }

        return PropertyValue;
    }

    /// <summary>
    ///     A cache of all classes that have properties decorated with the <see cref="SerializableConfigurationProperty"/>.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static ImmutableSortedDictionary<string, Type>? _classesWithConfigProps;

    /// <summary>
    /// Retrieves a dictionary of classes with properties annotated with see <see cref="SerializableConfigurationProperty"/>.
    /// The dictionary is case-insensitive and contains the class name as the key and the type as the value.
    /// To be called from the <see cref="ModuleInitializers.InitializeConfigurationManager"/>..
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    internal static void Initialize ()
    {
        if (_classesWithConfigProps is { })
        {
            return;
        }

        Dictionary<string, Type> dict = new (StringComparer.InvariantCultureIgnoreCase);

        IEnumerable<Type> types = from assembly in AppDomain.CurrentDomain.GetAssemblies ()
                                  from type in assembly.GetTypes ()
                                  where type.GetProperties ()
                                            .Any (prop => prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) != null)
                                  select type;

        foreach (Type classWithConfig in types)
        {
            dict.Add (classWithConfig.Name, classWithConfig);
        }

        _classesWithConfigProps = dict.ToImmutableSortedDictionary ();
    }

    /// <summary>
    ///   Uninitializes the <see cref="_classesWithConfigProps"/> dictionary. For unit testing.
    /// </summary>
    internal static void UnInitialize ()
    {
        _classesWithConfigProps = null;
    }


    /// <summary>
    /// Retrieves a dictionary of all properties annotated with <see cref="SerializableConfigurationProperty"/> from the classes
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    internal static ImmutableSortedDictionary<string, ConfigProperty> GetAllConfigProperties ()
    {
        if (_classesWithConfigProps is null)
        {
            throw new InvalidOperationException ("Initialize has not been called.");
        }

        var allConfigProperties = new Dictionary<string, ConfigProperty> (StringComparer.InvariantCultureIgnoreCase);

        foreach (var property in from c in _classesWithConfigProps
                                 let props = c.Value.GetProperties (
                                                                    BindingFlags.Instance |
                                                                    BindingFlags.Static |
                                                                    BindingFlags.NonPublic |
                                                                    BindingFlags.Public)
                                              .Where (prop => prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty)
                                 from property in props
                                 select property)
        {
            if (property.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is not SerializableConfigurationProperty scp)
            {
                continue;
            }

            // This code is disabled to call out that internal is explicitly supported.
            //if (!property.GetGetMethod (true)!.IsPublic)
            //{
            //    throw new InvalidOperationException (
            //                                         $"Property {property.Name} in class {property.DeclaringType?.Name} is not public. SerializableConfigurationProperty properties must be public.");

            //}

            if (property.GetGetMethod (true)!.IsStatic)
            {
                var key = scp.OmitClassName
                              ? ConfigProperty.GetJsonPropertyName (property)
                              : $"{property.DeclaringType?.Name}.{property.Name}";

                allConfigProperties.Add (key, new ConfigProperty
                {
                    PropertyInfo = property,
                    PropertyValue = null
                });
            }
            else
            {
                throw new InvalidOperationException (
                                                     $"Property {property.Name} in class {property.DeclaringType?.Name} is not static. SerializableConfigurationProperty properties must be static.");
            }
        }

        // Sort the properties
        return allConfigProperties.ToImmutableSortedDictionary (StringComparer.InvariantCultureIgnoreCase);
    }

}
