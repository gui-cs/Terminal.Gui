using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Provides deep cloning functionality for Terminal.Gui configuration objects.
///     Creates a deep copy of an object by recursively cloning public properties,
///     handling collections, arrays, dictionaries, and circular references.
/// </summary>
/// <remarks>
///     This class does not use <see cref="ICloneable"/> because it does not guarantee deep cloning,
///     may not handle circular references, and is not widely implemented in modern .NET types.
///     Instead, it uses reflection to ensure consistent deep cloning behavior across all types.
///     <para>
///         Limitations:
///         - Types without a parameterless constructor (and not handled as simple types, arrays, dictionaries, or
///         collections) may be instantiated using uninitialized objects, which could lead to runtime errors if not
///         properly handled.
///         - Immutable collections (e.g., <see cref="System.Collections.Immutable.ImmutableDictionary{TKey,TValue}"/>) are
///         not supported and will throw a <see cref="NotSupportedException"/>.
///         - Only public, writable properties are cloned; private fields, read-only properties, and non-public members are
///         ignored.
///     </para>
/// </remarks>
public static class DeepCloner
{
    /// <summary>
    ///     Creates a deep copy of the specified object.
    /// </summary>
    /// <typeparam name="T">The type of the object to clone.</typeparam>
    /// <param name="source">The object to clone.</param>
    /// <returns>A deep copy of the source object, or default if source is null.</returns>
    public static T? DeepClone<T> (T? source)
    {
        if (source is null)
        {
            return default (T?);
        }
        ConcurrentDictionary<object, object> visited = new (ReferenceEqualityComparer.Instance);

        return (T?)DeepCloneInternal (source, visited);
    }

    [UnconditionalSuppressMessage ("Trimming", "IL2075", Justification = "DeepCloner reflects over writable properties of runtime configuration/test types to preserve object graphs, circular references, and non-public setters.")]
    private static object? DeepCloneInternal (object? source, ConcurrentDictionary<object, object> visited)
    {
        if (source is null)
        {
            return null;
        }

        if (source is Drawing.Attribute attribute)
        {
            return attribute;
        }

        if (source is Drawing.Scheme scheme)
        {
            return new Drawing.Scheme (scheme);
        }

        if (source is ThemeScope themeScope)
        {
            return CloneScope (themeScope, visited);
        }

        if (source is AppSettingsScope appSettingsScope)
        {
            return CloneScope (appSettingsScope, visited);
        }

        if (source is SettingsScope settingsScope)
        {
            return CloneScope (settingsScope, visited);
        }

        // Handle already visited objects to avoid circular references
        if (visited.TryGetValue (source, out object? existingClone))
        {
            return existingClone;
        }

        Type type = source.GetType ();

        // Handle immutable or simple types
        if (IsSimpleType (type))
        {
            return source;
        }

        // Handle arrays
        if (type.IsArray)
        {
            return CloneArray (source, visited);
        }

        // Handle dictionaries
        if (source is IDictionary)
        {
            return CloneDictionary (source, visited);
        }

        // Handle collections
        if (typeof (ICollection).IsAssignableFrom (type))
        {
            return CloneCollection (source, visited);
        }

        // Create new instance
        object clone = CreateInstance (type);

        // Add to visited before cloning properties
        visited.TryAdd (source, clone);

        // Clone writable public and internal properties
        foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                          .Where (p => !p.IsSpecialName) // exclude private backing fields or compiler-generated props
                                          .Where (p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters ().Length == 0))
        {
            object? value = prop.GetValue (source);
            object? clonedValue = DeepCloneInternal (value, visited);
            prop.SetValue (clone, clonedValue);
        }

        return clone;
    }

    private static bool IsSimpleType (Type type)
    {
        if (type.IsPrimitive
            || type.IsEnum
            || type.IsValueType
            || type == typeof (decimal)
            || type == typeof (DateTime)
            || type == typeof (DateTimeOffset)
            || type == typeof (TimeSpan)
            || type == typeof (Guid)
            || type == typeof (Rune)
            || type == typeof (string))
        {
            return true;
        }

        // Treat Type and PropertyInfo as simple metadata objects that should not be cloned.
        if (typeof (Type).IsAssignableFrom (type) || typeof (PropertyInfo).IsAssignableFrom (type))
        {
            return true;
        }

        return false;
    }

    [UnconditionalSuppressMessage ("Trimming", "IL2067", Justification = "DeepCloner creates only runtime configuration/test types with parameterless constructors, and this path is covered by unit tests and NativeAOT validation.")]
    [UnconditionalSuppressMessage ("Trimming", "IL2070", Justification = "DeepCloner probes for a public parameterless constructor on runtime configuration/test types before instantiating them.")]
    private static object CreateInstance (Type type)
    {
        try
        {
            // Try parameterless constructor
            if (type.GetConstructor (Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance (type)!;
            }

            // In AOT, try using the JsonSerializer if available
            if (IsAotEnvironment ())
            {
                JsonTypeInfo? jsonTypeInfo = TuiSerializerContext.Instance.GetTypeInfo (type);

                if (jsonTypeInfo is not null)
                {
                    return JsonSerializer.Deserialize ("{}", jsonTypeInfo)!;
                }
            }

            throw new InvalidOperationException ($"Cannot create instance of type {type.FullName}. No parameterless constructor or clone method found.");
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException (
                                                 $"Cannot create instance of type {type.FullName} in AOT context. Consider adding this type to your SourceGenerationContext.");
        }
    }

    private static object CloneArray (object source, ConcurrentDictionary<object, object> visited)
    {
        Array array = (Array)source;
        Array newArray = (Array)array.Clone ();
        visited.TryAdd (source, newArray);

        for (var i = 0; i < array.Length; i++)
        {
            object? value = array.GetValue (i);
            object? clonedValue = DeepCloneInternal (value, visited);
            newArray.SetValue (clonedValue, i);
        }

        return newArray;
    }

    [UnconditionalSuppressMessage ("Trimming", "IL2072", Justification = "Collection cloning only instantiates the runtime collection type after filtering to supported IList implementations.")]
    private static object CloneCollection (object source, ConcurrentDictionary<object, object> visited)
    {
        Type type = source.GetType ();

        // Check for immutable collections and throw if found
        if (type.IsGenericType)
        {
            Type genericTypeDef = type.GetGenericTypeDefinition ();

            if (genericTypeDef.FullName != null && genericTypeDef.FullName.StartsWith ("System.Collections.Immutable"))
            {
                throw new NotSupportedException ($"Cloning of immutable collections like {type.Name} is not supported.");
            }
        }

        if (source is not IList)
        {
            throw new NotSupportedException ($"Cloning of collection type {type.Name} is not supported unless it implements IList.");
        }

        if (Activator.CreateInstance (type) is not IList tempList)
        {
            throw new NotSupportedException ($"Cloning of collection type {type.Name} is not supported without a parameterless constructor.");
        }

        // Add to visited before cloning contents to prevent circular reference issues
        visited.TryAdd (source, tempList);

        foreach (object? item in (IEnumerable)source)
        {
            object? clonedItem = DeepCloneInternal (item, visited);
            tempList.Add (clonedItem);
        }

        return tempList;
    }

    #region Dictionary Support

    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Dictionary cloning constructs supported runtime dictionary shapes (Dictionary<,> and ConcurrentDictionary<,>) via MakeGenericType, which is validated by NativeAOT publish tests.")]
    [UnconditionalSuppressMessage ("Trimming", "IL2075", Justification = "Dictionary cloning reads the runtime dictionary comparer from supported dictionary types to preserve comparer semantics.")]
    private static object CloneDictionary (object source, ConcurrentDictionary<object, object> visited)
    {
        if (source is ConcurrentDictionary<string, ThemeScope> themeSource)
        {
            ConcurrentDictionary<string, ThemeScope> clonedThemes = new (StringComparer.InvariantCultureIgnoreCase);
            visited.TryAdd (source, clonedThemes);

            foreach (KeyValuePair<string, ThemeScope> kvp in themeSource)
            {
                object? clonedThemeObject = DeepCloneInternal (kvp.Value, visited);

                if (clonedThemeObject is not ThemeScope clonedTheme)
                {
                    throw new InvalidOperationException (
                                                         $"Expected cloned theme scope to be {typeof (ThemeScope).FullName}, but got {clonedThemeObject?.GetType ().FullName ?? "<null>"}.");
                }

                clonedThemes [kvp.Key] = clonedTheme;
            }

            return clonedThemes;
        }

        if (source is Dictionary<string, Scheme?> schemeSource)
        {
            Dictionary<string, Scheme?> clonedSchemes = new (schemeSource.Comparer);
            visited.TryAdd (source, clonedSchemes);

            foreach (KeyValuePair<string, Scheme?> kvp in schemeSource)
            {
                clonedSchemes [kvp.Key] = (Scheme?)DeepCloneInternal (kvp.Value, visited);
            }

            return clonedSchemes;
        }

        IDictionary sourceDict = (IDictionary)source;
        Type type = source.GetType ();

        // Check for frozen or immutable dictionaries and throw if found
        if (type.IsGenericType)
        {
            CheckForUnsupportedDictionaryTypes (type);
        }

        Type [] genericArgs = type.GetGenericArguments ();
        Type dictType;

        if (genericArgs.Length == 2)
        {
            if (type.GetGenericTypeDefinition () == typeof (Dictionary<,>))
            {
                dictType = typeof (Dictionary<,>).MakeGenericType (genericArgs);
            }
            else if (type.GetGenericTypeDefinition () == typeof (ConcurrentDictionary<,>))
            {
                dictType = typeof (ConcurrentDictionary<,>).MakeGenericType (genericArgs);
            }
            else
            {
                throw new InvalidOperationException (
                                                     $"Unsupported dictionary type: {type}. Only Dictionary<,> and ConcurrentDictionary<,> are supported.");
            }
        }
        else
        {
            dictType = typeof (Dictionary<object, object>);
        }

        object? comparer = type.GetProperty ("Comparer")?.GetValue (source);

        IDictionary tempDict = CreateDictionaryInstance (dictType, comparer);
        visited.TryAdd (source, tempDict);

        object? lastKey = null;

        try
        {
            // Clone all key-value pairs
            foreach (object? key in sourceDict.Keys)
            {
                lastKey = key;
                object? clonedKey = DeepCloneInternal (key, visited);
                object? clonedValue = DeepCloneInternal (sourceDict [key], visited);

                if (tempDict is ConcurrentDictionary<string, ThemeScope> themeDict)
                {
                    themeDict [(string)clonedKey!] = (ThemeScope)clonedValue!;

                    continue;
                }

                if (tempDict is Dictionary<string, Scheme?> schemeDict)
                {
                    schemeDict [(string)clonedKey!] = (Scheme?)clonedValue;

                    continue;
                }

                if (tempDict.Contains (clonedKey!))
                {
                    tempDict [clonedKey!] = clonedValue;
                }
                else
                {
                    tempDict.Add (clonedKey!, clonedValue);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            // Handle cases where the dictionary is modified during enumeration
            throw new InvalidOperationException (
                                                 $"Error cloning dictionary ({source}) (last key was \"{lastKey}\"). Ensure the source dictionary is not modified during cloning.",
                                                 ex);
        }

        return tempDict;
    }

    [UnconditionalSuppressMessage ("Trimming", "IL2067", Justification = "Dictionary cloning only instantiates supported dictionary runtime types and falls back safely when comparer constructors are unavailable.")]
    private static IDictionary CreateDictionaryInstance (Type dictType, object? comparer)
    {
        // Typed paths for dictionary types that require custom comparers.
        if (dictType == typeof (ConcurrentDictionary<string, ThemeScope>))
        {
            if (comparer is IEqualityComparer<string> stringComparer)
            {
                return new ConcurrentDictionary<string, ThemeScope> (stringComparer);
            }

            return new ConcurrentDictionary<string, ThemeScope> ();
        }

        if (dictType == typeof (Dictionary<string, Scheme?>))
        {
            if (comparer is IEqualityComparer<string> stringComparer)
            {
                return new Dictionary<string, Scheme?> (stringComparer);
            }

            return new Dictionary<string, Scheme?> ();
        }

        if (dictType == typeof (Dictionary<Command, PlatformKeyBinding>))
        {
            return new Dictionary<Command, PlatformKeyBinding> ();
        }

        if (dictType == typeof (Dictionary<string, Dictionary<Command, PlatformKeyBinding>>))
        {
            if (comparer is IEqualityComparer<string> stringComparer)
            {
                return new Dictionary<string, Dictionary<Command, PlatformKeyBinding>> (stringComparer);
            }

            return new Dictionary<string, Dictionary<Command, PlatformKeyBinding>> ();
        }

        // AOT-safe: use the source-generated JSON serializer to create empty dictionary instances.
        // This avoids Activator.CreateInstance, whose target constructors are trimmed by the AOT linker
        // for closed generic dictionary types not otherwise statically reachable.
        // Always attempted regardless of comparer — in AOT, a default-comparer dictionary is preferable
        // to a crash from a trimmed constructor.
        try
        {
            JsonTypeInfo? jsonTypeInfo = TuiSerializerContext.Instance.GetTypeInfo (dictType);

            if (jsonTypeInfo is not null)
            {
                IDictionary? result = JsonSerializer.Deserialize ("{}", jsonTypeInfo) as IDictionary;

                if (result is not null)
                {
                    return result;
                }
            }
        }
        catch (InvalidOperationException)
        {
            // JSON serializer context may not be initialized — fall through to reflective construction.
        }

        try
        {
            // Try to create the dictionary with the comparer
            return comparer != null
                       ? (IDictionary)Activator.CreateInstance (dictType, comparer)!
                       : (IDictionary)Activator.CreateInstance (dictType)!;
        }
        catch (MissingMethodException)
        {
            // Fallback to parameterless constructor if comparer constructor is not available
            return (IDictionary)Activator.CreateInstance (dictType)!;
        }
    }

    private static void CheckForUnsupportedDictionaryTypes (Type type)
    {
        Type? currentType = type;

        while (currentType != null && currentType != typeof (object))
        {
            if (currentType.IsGenericType)
            {
                string? genericTypeName = currentType.GetGenericTypeDefinition ().FullName;

                if (genericTypeName != null
                    && (genericTypeName.StartsWith ("System.Collections.Frozen") || genericTypeName.StartsWith ("System.Collections.Immutable")))
                {
                    throw new NotSupportedException ($"Cloning of frozen or immutable dictionaries like {type.Name} is not supported.");
                }
            }

            currentType = currentType.BaseType;
        }
    }

    #endregion Dictionary Support

    #region AOT Support

    private static TScopeT CloneScope<TScopeT> (TScopeT scope, ConcurrentDictionary<object, object> visited)
        where TScopeT : Scope<TScopeT>, new ()
    {
        TScopeT clonedScope = new ();
        visited.TryAdd (scope, clonedScope);

        foreach (KeyValuePair<string, ConfigProperty> kvp in scope)
        {
            ConfigProperty clonedProperty = ConfigProperty.CreateCopy (kvp.Value);
            clonedProperty.Immutable = kvp.Value.Immutable;

            if (kvp.Value.HasValue)
            {
                clonedProperty.PropertyValue = DeepCloneInternal (kvp.Value.PropertyValue, visited);
            }

            clonedScope.TryAdd (kvp.Key, clonedProperty);
        }

        return clonedScope;
    }

    private static bool IsAotEnvironment () =>

        // Check if running in an AOT environment
        Type.GetType ("System.Runtime.CompilerServices.RuntimeFeature")?.GetProperty ("IsDynamicCodeSupported")?.GetValue (null) is bool and false;

    #endregion AOT Support
}
