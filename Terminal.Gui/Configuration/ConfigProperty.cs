#nullable enable
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
    ///     property has not been set (e.g. because no configuration file specified a value), <see cref="HasValue"/> will be <see langword="false"/>.
    /// </summary>
    /// <remarks>
    ///  // BUGBUG: This is not true. Has not been true for a long time???
    ///     On <see langword="set"/>, performs a sparse-copy of the new value to the existing value (only copies elements
    ///     of the object that are non-null).
    /// </remarks>
    public object? PropertyValue
    {
        get => _propertyValue;
        set
        {
            if (Immutable)
            {
                throw new InvalidOperationException ($"Property {PropertyInfo?.Name} is immutable and cannot be set.");
            }
            _propertyValue = value;
            HasValue = true;
        }
    }

    /// <summary>
    ///     Gets or sets whether this config property has a value. This is set to <see langword="true"/> when <see cref="PropertyValue"/> is set.
    /// </summary>
    public bool HasValue { get; set; }

    /// <summary>
    ///     Gets or sets whether this property is immutable. If <see langword="true"/>, the property cannot be changed.
    /// </summary>
    public bool Immutable { get; set; }

    /// <summary>Applies the <see cref="PropertyValue"/> to the static property described by <see cref="PropertyInfo"/>.</summary>
    /// <returns></returns>
    public bool Apply ()
    {
        try
        {
            if (PropertyInfo?.GetValue (null) is { })
            {
                object? currentValue = PropertyInfo.GetValue (null);

                // generic deep copy
                var val = ScopeExtensions.DeepMemberWiseCopy (PropertyValue, currentValue);
                PropertyInfo.SetValue (null, val);

            }
        }
        catch (TargetInvocationException tie)
        {
            if (tie.InnerException is { })
            {
                throw new JsonException (
                                         $"Error Applying Configuration Change: {tie.InnerException.Message}",
                                         tie.InnerException
                                        );
            }

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
    public object? RetrieveValue ()
    {
        return PropertyValue = PropertyInfo!.GetValue (null);
    }

    /// <summary>
    ///     Updates (using reflection) <see cref="PropertyValue"/> with the value in <paramref name="source"/> using a deep memberwise copy.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal object? UpdateValueFrom (object? source)
    {
        if (source is null)
        {
            return PropertyValue;
        }

        Type? underlyingType = Nullable.GetUnderlyingType (PropertyInfo!.PropertyType);

        if (source.GetType () != PropertyInfo.PropertyType && underlyingType is { } && source.GetType () != underlyingType)
        {
            throw new ArgumentException (
                                         $"The source object ({PropertyInfo.DeclaringType}.{PropertyInfo.Name}) is not of type {PropertyInfo.PropertyType}."
                                        );
        }

        if (PropertyValue is { })
        {
            // Handle Scope<T>-specific logic
            if (source is SettingsScope settingsSource && PropertyValue is SettingsScope settingsDest)
            {
                PropertyValue = settingsDest.Update (settingsSource);
            }
            else if (source is ThemeScope themeSource && PropertyValue is ThemeScope themeDest)
            {
                PropertyValue = themeDest.Update (themeSource);
            }
            else if (source is AppScope appSource && PropertyValue is AppScope appDest)
            {
                PropertyValue = appDest.Update (appSource);
            }
            else
            {
                // Fallback to generic deep copy
                PropertyValue = ScopeExtensions.DeepMemberWiseCopy (source, PropertyValue);
            }
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
    internal static ImmutableSortedDictionary<string, Type>? _classesWithConfigProps;

    /// <summary>
    /// To be called from the <see cref="ModuleInitializers.InitializeConfigurationManager"/> method to initialize the
    /// _classesWithConfigProps dictionary.
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
    /// Retrieves a dictionary of all properties annotated with <see cref="SerializableConfigurationProperty"/> from the classes in the module.
    /// THe dictionary case-insensitive and sorted.
    /// The <see cref="ConfigProperty"/> items have <see cref="PropertyInfo"/> set, but not <see cref="PropertyValue"/>. 
    /// <see cref="Immutable"/> is set to <see langword="true"/>.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    internal static ImmutableSortedDictionary<string, ConfigProperty> GetAllConfigProperties ()
    {
        if (_classesWithConfigProps is null)
        {
            throw new InvalidOperationException ("Initialize has not been called.");
        }

        var allConfigProperties = new Dictionary<string, ConfigProperty> (StringComparer.InvariantCultureIgnoreCase);

        foreach (PropertyInfo? propertyInfo in from c in _classesWithConfigProps
                                           let props = c.Value.GetProperties (
                                                                              BindingFlags.Instance |
                                                                              BindingFlags.Static |
                                                                              BindingFlags.NonPublic |
                                                                              BindingFlags.Public)
                                                        .Where (prop => prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty)
                                           from property in props
                                           select property)
        {
            if (propertyInfo.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is not SerializableConfigurationProperty scp)
            {
                continue;
            }

            // This code is disabled to call out that internal is explicitly supported.
            //if (!property.GetGetMethod (true)!.IsPublic)
            //{
            //    throw new InvalidOperationException (
            //                                         $"Property {property.Name} in class {property.DeclaringType?.Name} is not public. SerializableConfigurationProperty properties must be public.");

            //}

            if (propertyInfo.GetGetMethod (true)!.IsStatic)
            {
                var key = scp.OmitClassName
                              ? ConfigProperty.GetJsonPropertyName (propertyInfo)
                              : $"{propertyInfo.DeclaringType?.Name}.{propertyInfo.Name}";

                allConfigProperties.Add (key, new ConfigProperty
                {
                    // Set onlly PropertyInfo. Do not set PropertyValue (or HasValue will be set)
                    PropertyInfo = propertyInfo,
                    // By default, properties are immutable
                    Immutable = true
                });
            }
            else
            {
                throw new InvalidOperationException (
                                                     $"Property {propertyInfo.Name} in class {propertyInfo.DeclaringType?.Name} is not static. SerializableConfigurationProperty properties must be static.");
            }
        }

        // Sort the properties
        return allConfigProperties.ToImmutableSortedDictionary (StringComparer.InvariantCultureIgnoreCase);
    }
}
