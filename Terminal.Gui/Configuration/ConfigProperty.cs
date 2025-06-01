#nullable enable
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Holds a property's value and the <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> to
///     retrieve and apply the property's value.
/// </summary>
/// <remarks>
///     Configuration properties must be <see langword="public"/>/<see langword="internal"/> and <see langword="static"/> and have the
///     <see cref="ConfigurationPropertyAttribute"/> attribute. If the type of the property requires specialized JSON
///     serialization, a <see cref="JsonConverter"/> must be provided using the <see cref="JsonConverterAttribute"/>
///     attribute.
/// </remarks>
public class ConfigProperty
{
    /// <summary>Describes the property.</summary>
    public PropertyInfo? PropertyInfo { get; set; }

    /// <summary>INTERNAL: Cached value of ConfigurationPropertyAttribute.OmitClassName; makes more AOT friendly.</summary>
    internal bool OmitClassName { get; set; }

    /// <summary>INTERNAL: Cached value of ConfigurationPropertyAttribute.Scope; makes more AOT friendly.</summary>
    internal string? ScopeType { get; set; }

    private object? _propertyValue;

    /// <summary>
    ///     Holds the property's value as it was either read from the class's implementation or from a config file. If the
    ///     property has not been set (e.g. because no configuration file specified a value), <see cref="HasValue"/> will be <see langword="false"/>.
    /// </summary>
    public object? PropertyValue
    {
        get => _propertyValue;
        set
        {
            if (Immutable)
            {
                throw new InvalidOperationException ($"Property {PropertyInfo?.Name} is immutable and cannot be set.");
            }

            // TODO: Verify value is correct type?

            _propertyValue = value;
            HasValue = true;
        }
    }

    /// <summary>
    ///     Gets or sets whether this config property has a value. This is set to <see langword="true"/> when <see cref="PropertyValue"/> is set.
    /// </summary>
    public bool HasValue { get; set; }

    /// <summary>
    ///     INTERNAL: Gets or sets whether this property is immutable. If <see langword="true"/>, the property cannot be changed.
    /// </summary>
    internal bool Immutable { get; set; }

    /// <summary>Applies the <see cref="PropertyValue"/> to the static property described by <see cref="PropertyInfo"/>.</summary>
    /// <returns></returns>
    [RequiresDynamicCode ("Uses reflection to get and set property values")]
    [RequiresUnreferencedCode ("Uses DeepCloner which requires types to be registered in SourceGenerationContext")]
    public bool Apply ()
    {
        try
        {
            if (PropertyInfo?.GetValue (null) is { })
            {
                // Use DeepCloner to create a deep copy of PropertyValue
                object? val = DeepCloner.DeepClone (PropertyValue);
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
    /// INTERNAL: Creates a copy of a ConfigProperty with the same metadata but no value.
    /// </summary>
    /// <param name="source">The source ConfigProperty.</param>
    /// <returns>A new ConfigProperty instance.</returns>
    internal static ConfigProperty CreateCopy (ConfigProperty source)
    {
        return new ConfigProperty
        {
            Immutable = false,
            PropertyInfo = source.PropertyInfo,
            OmitClassName = source.OmitClassName,
            ScopeType = source.ScopeType,
            HasValue = false
        };
    }

    /// <summary>
    /// INTERNAL: Create an immutable ConfigProperty with cached attribute information
    /// </summary>
    /// <param name="propertyInfo">The PropertyInfo to create from</param>
    /// <returns>A new ConfigProperty with attribute data cached</returns>
    [RequiresDynamicCode ("Uses reflection to access custom attributes")]
    internal static ConfigProperty CreateImmutableWithAttributeInfo (PropertyInfo propertyInfo)
    {
        var attr = propertyInfo.GetCustomAttribute (typeof (ConfigurationPropertyAttribute)) as ConfigurationPropertyAttribute;

        return new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            OmitClassName = attr?.OmitClassName ?? false,
            ScopeType = attr?.Scope!.Name,
            // By default, properties are immutable
            Immutable = true
        };
    }

    /// <summary>
    /// INTERNAL: Helper method to get the ConfigurationPropertyAttribute for a PropertyInfo
    /// </summary>
    /// <param name="propertyInfo">The PropertyInfo to get the attribute from</param>
    /// <returns>The ConfigurationPropertyAttribute if found; otherwise, null</returns>
    [RequiresDynamicCode ("Uses reflection to access custom attributes")]
    internal static ConfigurationPropertyAttribute? GetConfigurationPropertyAttribute (PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttribute (typeof (ConfigurationPropertyAttribute)) as ConfigurationPropertyAttribute;
    }

    /// <summary>
    /// INTERNAL: Helper method to check if a PropertyInfo has a ConfigurationPropertyAttribute
    /// </summary>
    /// <param name="propertyInfo">The PropertyInfo to check</param>
    /// <returns>True if the PropertyInfo has a ConfigurationPropertyAttribute; otherwise, false</returns>
    [RequiresDynamicCode ("Uses reflection to access custom attributes")]
    internal static bool HasConfigurationPropertyAttribute (PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttribute (typeof (ConfigurationPropertyAttribute)) != null;
    }

    /// <summary>
    ///     INTERNAL: Helper to get either the Json property named (specified by [JsonPropertyName(name)] or the actual property
    ///     name.
    /// </summary>
    /// <param name="pi"></param>
    /// <returns></returns>
    [RequiresDynamicCode ("Uses reflection to access custom attributes")]
    internal static string GetJsonPropertyName (PropertyInfo pi)
    {
        var attr = pi.GetCustomAttribute (typeof (JsonPropertyNameAttribute)) as JsonPropertyNameAttribute;

        return attr?.Name ?? pi.Name;
    }

    /// <summary>
    ///     Updates (using reflection) the <see cref="PropertyValue"/> from the static  <see cref="ConfigurationPropertyAttribute"/>
    ///     property described in <see cref="PropertyInfo"/>.
    /// </summary>
    /// <returns></returns>
    [RequiresDynamicCode ("Uses reflection to retrieve property values")]
    public object? UpdateToCurrentValue ()
    {
        return PropertyValue = PropertyInfo!.GetValue (null);
    }

    /// <summary>
    ///     INTERNAL: Updates <see cref="PropertyValue"/> with the value in <paramref name="source"/> using a deep memberwise copy that
    ///     copies only the values that <see cref="HasValue"/>.
    /// </summary>
    /// <param name="source">The source object to copy values from.</param>
    /// <returns>The updated property value.</returns>
    /// <exception cref="ArgumentException">Thrown when the source type doesn't match the property type.</exception>
    [RequiresUnreferencedCode ("Uses DeepCloner which requires types to be registered in SourceGenerationContext")]
    [RequiresDynamicCode ("Calls Terminal.Gui.DeepCloner.DeepClone<T>(T)")]
    internal object? UpdateFrom (object? source)
    {
        // If the source (higher-priority layer) doesn't provide a value, keep the existing value
        // In the context of layering, a null source means the higher-priority layer doesn't specify a value,
        // so we should retain the value from the lower-priority layer.
        if (source is null)
        {
            return PropertyValue;
        }

        // Process the source based on its type
        if (source is ConcurrentDictionary<string, ThemeScope> themeDictSource &&
            PropertyValue is ConcurrentDictionary<string, ThemeScope> themeDictDest)
        {
            UpdateThemeScopeDictionary (themeDictSource, themeDictDest);
        }
        else if (source is ConcurrentDictionary<string, ConfigProperty> concurrentDictSource &&
                 PropertyValue is ConcurrentDictionary<string, ConfigProperty> concurrentDictDest)
        {
            UpdateConfigPropertyConcurrentDictionary (concurrentDictSource, concurrentDictDest);
        }
        else if (source is Dictionary<string, ConfigProperty> dictSource &&
                 PropertyValue is Dictionary<string, ConfigProperty> dictDest)
        {
            UpdateConfigPropertyDictionary (dictSource, dictDest);
        }
        else if (source is ConfigProperty configProperty)
        {
            if (configProperty.HasValue)
            {
                PropertyValue = DeepCloner.DeepClone (configProperty.PropertyValue);
            }
        }
        else if (source is Dictionary<string, Scheme> dictSchemeSource &&
                 PropertyValue is Dictionary<string, Scheme> dictSchemesDest)
        {
            UpdateSchemeDictionary (dictSchemeSource, dictSchemesDest);
        }
        else if (source is Scheme scheme)
        {
            PropertyValue = new Scheme (scheme); // use copy constructor
        }

        else
        {
            // Validate type compatibility for non-dictionary types
            ValidateTypeCompatibility (source);

            // For non-scope types, perform a deep copy of the source value to ensure immutability
            PropertyValue = DeepCloner.DeepClone (source);
        }

        return PropertyValue;
    }

    /// <summary>
    /// Validates that the source type is compatible with the property type.
    /// </summary>
    /// <param name="source">The source object to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the source type doesn't match the property type.</exception>
    private void ValidateTypeCompatibility (object source)
    {
        Type? underlyingType = Nullable.GetUnderlyingType (PropertyInfo!.PropertyType);

        bool isCompatibleType = source.GetType () == PropertyInfo.PropertyType ||
                               (underlyingType is { } && source.GetType () == underlyingType);

        if (!isCompatibleType)
        {
            throw new ArgumentException (
                $"The source object ({PropertyInfo.DeclaringType}.{PropertyInfo.Name}) is not of type {PropertyInfo.PropertyType}."
            );
        }
    }


    /// <summary>
    /// Updates a Scheme object by selectively applying explicitly set attributes from the source.
    /// </summary>
    /// <param name="sourceScheme">The source Scheme.</param>
    /// <param name="destScheme">The destination Scheme to update.</param>
    private void UpdateScheme (Scheme sourceScheme, Scheme destScheme)
    {
        // We can't modify properties of a record directly, so we need to create a new one
        // First, create a clone of the destination to preserve any values
        var updatedScheme = new Scheme (destScheme);

        //// Use with expressions to update only explicitly set attributes
        //// For each role, check if the source has an explicitly set attribute
        //if (sourceScheme.Normal.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { Normal = sourceScheme.Normal };
        //}

        //if (sourceScheme.HotNormal.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { HotNormal = sourceScheme.HotNormal };
        //}

        //if (sourceScheme.Focus.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { Focus = sourceScheme.Focus };
        //}

        //if (sourceScheme.HotFocus.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { HotFocus = sourceScheme.HotFocus };
        //}

        //if (sourceScheme.Active.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { Active = sourceScheme.Active };
        //}

        //if (sourceScheme.HotActive.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { HotActive = sourceScheme.HotActive };
        //}

        //if (sourceScheme.Highlight.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { Highlight = sourceScheme.Highlight };
        //}

        //if (sourceScheme.Editable.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { Editable = sourceScheme.Editable };
        //}

        //if (sourceScheme.ReadOnly.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { ReadOnly = sourceScheme.ReadOnly };
        //}

        //if (sourceScheme.Disabled.IsExplicitlySet)
        //{
        //    updatedScheme = updatedScheme with { Disabled = sourceScheme.Disabled };
        //}

        // Update the PropertyValue with the merged scheme
        PropertyValue = updatedScheme;
    }

    /// <summary>
    /// Updates a ThemeScope dictionary with values from a source dictionary.
    /// </summary>
    /// <param name="source">The source ThemeScope dictionary.</param>
    /// <param name="destination">The destination ThemeScope dictionary.</param>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.Scope<T>.UpdateFrom(Scope<T>)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Scope<T>.UpdateFrom(Scope<T>)")]
    private static void UpdateThemeScopeDictionary (
        ConcurrentDictionary<string, ThemeScope> source,
        ConcurrentDictionary<string, ThemeScope> destination)
    {
        foreach (KeyValuePair<string, ThemeScope> scope in source)
        {
            if (!destination.ContainsKey (scope.Key))
            {
                destination.TryAdd (scope.Key, scope.Value);
                continue;
            }

            destination [scope.Key].UpdateFrom (scope.Value);
        }
    }

    /// <summary>
    /// Updates a ConfigProperty dictionary with values from a source dictionary.
    /// </summary>
    /// <param name="source">The source ConfigProperty dictionary.</param>
    /// <param name="destination">The destination ConfigProperty dictionary.</param>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    private static void UpdateConfigPropertyConcurrentDictionary (
        ConcurrentDictionary<string, ConfigProperty> source,
        ConcurrentDictionary<string, ConfigProperty> destination)
    {
        foreach (KeyValuePair<string, ConfigProperty> sourceProp in source)
        {
            // Skip properties without values
            if (!sourceProp.Value.HasValue)
            {
                continue;
            }

            if (!destination.ContainsKey (sourceProp.Key))
            {
                // Add the property to the destination
                var copy = CreateCopy (sourceProp.Value);
                destination.TryAdd (sourceProp.Key, copy);
            }

            // Update the value in the destination
            destination [sourceProp.Key].UpdateFrom (sourceProp.Value);
        }
    }

    /// <summary>
    /// Updates a ConfigProperty dictionary with values from a source dictionary.
    /// </summary>
    /// <param name="source">The source ConfigProperty dictionary.</param>
    /// <param name="destination">The destination ConfigProperty dictionary.</param>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    private static void UpdateConfigPropertyDictionary (
        Dictionary<string, ConfigProperty> source,
        Dictionary<string, ConfigProperty> destination)
    {
        foreach (KeyValuePair<string, ConfigProperty> sourceProp in source)
        {
            // Skip properties without values
            if (!sourceProp.Value.HasValue)
            {
                continue;
            }

            if (!destination.ContainsKey (sourceProp.Key))
            {
                // Add the property to the destination
                var copy = CreateCopy (sourceProp.Value);
                destination.Add (sourceProp.Key, copy);
            }

            // Update the value in the destination
            destination [sourceProp.Key].UpdateFrom (sourceProp.Value);
        }
    }

    /// <summary>
    /// Updates a ConfigProperty dictionary with values from a source dictionary.
    /// </summary>
    /// <param name="source">The source ConfigProperty dictionary.</param>
    /// <param name="destination">The destination ConfigProperty dictionary.</param>
    private static void UpdateSchemeDictionary (
        Dictionary<string, Scheme> source,
        Dictionary<string, Scheme> destination)
    {
        foreach (KeyValuePair<string, Scheme> sourceProp in source)
        {
            if (!destination.ContainsKey (sourceProp.Key))
            {
                // Add the property to the destination
                // Schemes are structs are passed by val
                destination.Add (sourceProp.Key, sourceProp.Value);
            }

            // Update the value in the destination
            // Schemes are structs are passed by val
            destination [sourceProp.Key] = sourceProp.Value;
        }
    }

    #region Initialization

    /// <summary>
    ///     INTERNAL: A cache of all classes that have properties decorated with the <see cref="ConfigurationPropertyAttribute"/>.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    private static ImmutableSortedDictionary<string, Type>? _classesWithConfigProps;

    /// <summary>
    /// INTERNAL: Called from the <see cref="ModuleInitializers.InitializeConfigurationManager"/> method to initialize the
    /// _classesWithConfigProps dictionary.
    /// </summary>
    [RequiresDynamicCode ("Uses reflection to scan assemblies for configuration properties. " +
                        "Only called during initialization and not needed during normal operation. " +
                        "In AOT environments, ensure all types with ConfigurationPropertyAttribute are preserved.")]
    [RequiresUnreferencedCode ("Reflection requires all types with ConfigurationPropertyAttribute to be preserved in AOT. " +
                             "Use the SourceGenerationContext to register all configuration property types.")]
    internal static void Initialize ()
    {
        if (_classesWithConfigProps is { })
        {
            return;
        }

        Dictionary<string, Type> dict = new (StringComparer.InvariantCultureIgnoreCase);

        // Process assemblies directly to avoid LINQ overhead
        Assembly [] assemblies = AppDomain.CurrentDomain.GetAssemblies ();
        foreach (Assembly assembly in assemblies)
        {
            try
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                foreach (Type type in assembly.GetTypes ())
                {
                    PropertyInfo [] properties = type.GetProperties ();

                    // Check if any property has the ConfigurationPropertyAttribute
                    var hasConfigProp = false;
                    foreach (PropertyInfo prop in properties)
                    {
                        if (HasConfigurationPropertyAttribute (prop))
                        {
                            hasConfigProp = true;
                            break;
                        }
                    }

                    if (hasConfigProp)
                    {
                        dict [type.Name] = type;
                    }
                }
            }
            // Skip problematic assemblies that can't be loaded or analyzed
            catch (ReflectionTypeLoadException)
            {
                continue;
            }
            catch (BadImageFormatException)
            {
                continue;
            }
        }

        _classesWithConfigProps = dict.ToImmutableSortedDictionary ();
    }

    /// <summary>
    /// INTERNAL: Retrieves a dictionary of all properties annotated with <see cref="ConfigurationPropertyAttribute"/> from the classes in the module.
    /// The dictionary case-insensitive and sorted.
    /// The <see cref="ConfigProperty"/> items have <see cref="PropertyInfo"/> set, but not <see cref="PropertyValue"/>. 
    /// <see cref="Immutable"/> is set to <see langword="true"/>.
    /// </summary>
    [RequiresDynamicCode ("Uses reflection to scan assemblies for configuration properties. " +
                         "Only called during initialization and not needed during normal operation. " +
                         "In AOT environments, ensure all types with ConfigurationPropertyAttribute are preserved.")]
    [RequiresUnreferencedCode ("Reflection requires all types with ConfigurationPropertyAttribute to be preserved in AOT. " +
                              "Use the SourceGenerationContext to register all configuration property types.")]
    internal static ImmutableSortedDictionary<string, ConfigProperty> GetAllConfigProperties ()
    {
        if (_classesWithConfigProps is null)
        {
            throw new InvalidOperationException ("Initialize has not been called.");
        }

        // Estimate capacity to reduce resizing operations
        int estimatedCapacity = _classesWithConfigProps.Count * 5; // Assume ~5 properties per class
        Dictionary<string, ConfigProperty> allConfigProperties = new (estimatedCapacity, StringComparer.InvariantCultureIgnoreCase);

        // Process each class with direct iteration instead of LINQ
        foreach (KeyValuePair<string, Type> classEntry in _classesWithConfigProps)
        {
            Type type = classEntry.Value;

            // Get all public static/instance properties
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            PropertyInfo [] properties = type.GetProperties (bindingFlags);

            foreach (PropertyInfo propertyInfo in properties)
            {
                // Skip properties without our attribute
                if (!HasConfigurationPropertyAttribute (propertyInfo))
                {
                    continue;
                }

                // Verify the property is static
                if (!propertyInfo.GetGetMethod (true)!.IsStatic)
                {
                    throw new InvalidOperationException (
                        $"Property {propertyInfo.Name} in class {propertyInfo.DeclaringType?.Name} is not static. " +
                        "[ConfigurationProperty] properties must be static.");
                }

                // Create config property with cached attribute data
                ConfigProperty configProperty = CreateImmutableWithAttributeInfo (propertyInfo);

                // Use cached attribute data to determine the key
                string key = configProperty.OmitClassName
                    ? GetJsonPropertyName (propertyInfo)
                    : $"{propertyInfo.DeclaringType?.Name}.{propertyInfo.Name}";

                allConfigProperties.Add (key, configProperty);
            }
        }

        return allConfigProperties.ToImmutableSortedDictionary (StringComparer.InvariantCultureIgnoreCase);
    }


    #endregion Initialization
}
