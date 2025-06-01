#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    [RequiresUnreferencedCode ("Deep cloning may use reflection which might be incompatible with AOT compilation if types aren't registered in SourceGenerationContext")]
    [RequiresDynamicCode ("Deep cloning may use reflection that requires runtime code generation if source generation fails")]

    public static T? DeepClone<T> (T? source)
    {
        if (source is null)
        {
            return default (T?);
        }
        //// For AOT environments, use source generation exclusively
        //if (IsAotEnvironment ())
        //{
        //    if (TryUseSourceGeneratedCloner<T> (source, out T? result))
        //    {
        //        return result;
        //    }

        //    // If in AOT but source generation failed, throw an exception
        //    // instead of silently falling back to reflection
        //    //throw new InvalidOperationException (
        //    //                                     $"Type {typeof (T).FullName} is not properly registered in SourceGenerationContext " +
        //    //                                     $"for AOT-compatible cloning.");
        //    Logging.Error ($"Type {typeof (T).FullName} is not properly registered in SourceGenerationContext " +
        //                  $"for AOT-compatible cloning.");
        //}

        // Use reflection-based approach, which should have better performance in non-AOT environments
        ConcurrentDictionary<object, object> visited = new (ReferenceEqualityComparer.Instance);

        return (T?)DeepCloneInternal (source, visited);
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.DeepCloner.CreateInstance(Type)")]
    [UnconditionalSuppressMessage ("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static object? DeepCloneInternal (object? source, ConcurrentDictionary<object, object> visited)
    {
        if (source is null)
        {
            return null;
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

        // Handle strings explicitly
        if (type == typeof (string))
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

    private static bool IsSimpleType ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (type.IsPrimitive
            || type.IsEnum
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

        // Treat structs with no writable public properties as simple types (immutable structs)
        if (type.IsValueType)
        {
            IEnumerable<PropertyInfo> writableProperties = type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
                                                               .Where (p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters ().Length == 0);

            return !writableProperties.Any ();
        }

        // Treat PropertyInfo (e.g., RuntimePropertyInfo) as a simple type since it's metadata and shouldn't be cloned
        if (typeof (PropertyInfo).IsAssignableFrom (type))
        {
            return true;
        }

        return false;
    }

    [RequiresUnreferencedCode ("Calls System.Text.Json.JsonSerializer.Deserialize(String, Type, JsonSerializerOptions)")]
    [RequiresDynamicCode ("Calls System.Text.Json.JsonSerializer.Deserialize(String, Type, JsonSerializerOptions)")]
    private static object CreateInstance ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        try
        {
            // Try parameterless constructor
            if (type.GetConstructor (Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance (type)!;
            }

            // Record support
            if (type.GetMethod ("<Clone>$") != null)
            {
                return Activator.CreateInstance (type)!;
            }

            // In AOT, try using the JsonSerializer if available
            if (IsAotEnvironment () && CanSerializeWithJson (type))
            {
                return JsonSerializer.Deserialize (JsonSerializer.Serialize (new object (), type), type, ConfigurationManager.SerializerContext.Options)!;
            }

            throw new InvalidOperationException ($"Cannot create instance of type {type.FullName}. No parameterless constructor or clone method found.");
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException ($"Cannot create instance of type {type.FullName} in AOT context. Consider adding this type to your SourceGenerationContext.");
        }
    }

    [RequiresDynamicCode ("Calls System.Array.CreateInstance(Type, Int32)")]
    [RequiresUnreferencedCode ("Calls Terminal.Gui.DeepCloner.DeepCloneInternal(Object, ConcurrentDictionary<Object, Object>)")]
    private static object CloneArray (object source, ConcurrentDictionary<object, object> visited)
    {
        var array = (Array)source;
        Type elementType = array.GetType ().GetElementType ()!;
        var newArray = Array.CreateInstance (elementType, array.Length);
        visited.TryAdd (source, newArray);

        for (var i = 0; i < array.Length; i++)
        {
            object? value = array.GetValue (i);
            object? clonedValue = DeepCloneInternal (value, visited);
            newArray.SetValue (clonedValue, i);
        }

        return newArray;
    }

    [RequiresDynamicCode ("Calls System.Type.MakeGenericType(params Type[])")]
    [RequiresUnreferencedCode ("Calls Terminal.Gui.DeepCloner.DeepCloneInternal(Object, ConcurrentDictionary<Object, Object>)")]
    private static object CloneCollection (object source, ConcurrentDictionary<object, object> visited)
    {
        Type type = source.GetType ();
        Type elementType = type.GetGenericArguments ().FirstOrDefault () ?? typeof (object);

        // Check for immutable collections and throw if found
        if (type.IsGenericType)
        {
            Type genericTypeDef = type.GetGenericTypeDefinition ();

            if (genericTypeDef.FullName != null && genericTypeDef.FullName.StartsWith ("System.Collections.Immutable"))
            {
                throw new NotSupportedException ($"Cloning of immutable collections like {type.Name} is not supported.");
            }
        }

        Type listType = typeof (List<>).MakeGenericType (elementType);
        var tempList = (IList)Activator.CreateInstance (listType)!;

        // Add to visited before cloning contents to prevent circular reference issues
        visited.TryAdd (source, tempList);

        foreach (object? item in (IEnumerable)source)
        {
            object? clonedItem = DeepCloneInternal (item, visited);
            tempList.Add (clonedItem);
        }

        // Try to create the original collection type if possible
        if (type != listType && type.GetConstructor ([listType]) != null)
        {
            object result = Activator.CreateInstance (type, tempList)!;
            visited [source] = result;

            return result;
        }

        return tempList;
    }

    #region Dictionary Support

    [RequiresDynamicCode ("Calls System.Type.MakeGenericType(params Type[])")]
    [RequiresUnreferencedCode ("Calls Terminal.Gui.DeepCloner.DeepCloneInternal(Object, ConcurrentDictionary<Object, Object>)")]
    private static object CloneDictionary (object source, ConcurrentDictionary<object, object> visited)
    {
        var sourceDict = (IDictionary)source;
        Type type = source.GetType ();

        // Check for frozen or immutable dictionaries and throw if found
        if (type.IsGenericType)
        {
            CheckForUnsupportedDictionaryTypes (type);
        }

        // Determine dictionary type and comparer
        Type [] genericArgs = type.GetGenericArguments ();
        Type dictType = genericArgs.Length == 2
            ? typeof (Dictionary<,>).MakeGenericType (genericArgs)
            : typeof (Dictionary<object, object>);
        object? comparer = type.GetProperty ("Comparer")?.GetValue (source);

        // Create a temporary dictionary to hold cloned key-value pairs
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
            throw new InvalidOperationException ($"Error cloning dictionary ({source}) (last key was \"{lastKey}\"). Ensure the source dictionary is not modified during cloning.", ex);
        }

        // If the original dictionary type has a parameterless constructor, create a new instance
        if (type.GetConstructor (Type.EmptyTypes) != null)
        {
            return CreateFinalDictionary (type, comparer, tempDict, source, visited);
        }

        return tempDict;
    }

    private static IDictionary CreateDictionaryInstance ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type dictType, object? comparer)
    {
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

                if (genericTypeName != null &&
                    (genericTypeName.StartsWith ("System.Collections.Frozen") ||
                     genericTypeName.StartsWith ("System.Collections.Immutable")))
                {
                    throw new NotSupportedException ($"Cloning of frozen or immutable dictionaries like {type.Name} is not supported.");
                }
            }

            currentType = currentType.BaseType;
        }
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.DeepCloner.DeepCloneInternal(Object, ConcurrentDictionary<Object, Object>)")]
    private static object CreateFinalDictionary (
        [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
        object? comparer,
        IDictionary tempDict,
        object source,
        ConcurrentDictionary<object, object> visited)
    {
        IDictionary newDict;

        try
        {
            // Try to create the dictionary with the comparer
            newDict = comparer != null
                ? (IDictionary)Activator.CreateInstance (type, comparer)!
                : (IDictionary)Activator.CreateInstance (type)!;
        }
        catch (MissingMethodException)
        {
            // Fallback to parameterless constructor if comparer constructor is not available
            newDict = (IDictionary)Activator.CreateInstance (type)!;
        }

        newDict.Clear ();
        visited [source] = newDict;

        // Copy cloned key-value pairs to the new dictionary
        foreach (object? key in tempDict.Keys)
        {
            if (newDict.Contains (key))
            {
                newDict [key] = tempDict [key];
            }
            else
            {
                newDict.Add (key, tempDict [key]);
            }
        }

        // Clone additional properties of the derived dictionary type
        foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
                                          .Where (p => p.CanRead && p.CanWrite && p.GetIndexParameters ().Length == 0))
        {
            object? value = prop.GetValue (source);
            object? clonedValue = DeepCloneInternal (value, visited);
            prop.SetValue (newDict, clonedValue);
        }

        return newDict;
    }

    #endregion Dictionary Support

    #region AOT Support

    /// <summary>
    /// Determines if a type can be serialized using System.Text.Json based on the types 
    /// registered in the SourceGenerationContext.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type can be serialized using System.Text.Json; otherwise, false.</returns>
    private static bool CanSerializeWithJson (Type type)
    {
        // Check if the type or any of its base types is registered in SourceGenerationContext
        return ConfigurationManager.SerializerContext.GetType ()
                                   .GetProperties (BindingFlags.Public | BindingFlags.Static)
                                   .Any (p => p.PropertyType.IsGenericType &&
                                              p.PropertyType.GetGenericTypeDefinition () == typeof (JsonTypeInfo<>) &&
                                              (p.PropertyType.GetGenericArguments () [0] == type ||
                                               p.PropertyType.GetGenericArguments () [0].IsAssignableFrom (type)));
    }

    private static bool IsAotEnvironment () =>
        // Check if running in an AOT environment
        Type.GetType ("System.Runtime.CompilerServices.RuntimeFeature")?.GetProperty ("IsDynamicCodeSupported")?.GetValue (null) is bool isDynamicCodeSupported && !isDynamicCodeSupported;

    /// <summary>
    /// Attempts to clone an object using source-generated serialization from System.Text.Json.
    /// This provides an AOT-compatible alternative to reflection-based deep cloning.
    /// </summary>
    /// <typeparam name="T">The type of the object to clone</typeparam>
    /// <param name="source">The source object to clone</param>
    /// <param name="result">The cloned result, if successful</param>
    /// <returns>True if cloning succeeded using source generation; otherwise, false</returns>
    private static bool TryUseSourceGeneratedCloner<T> (T source, [NotNullWhen (true)] out T? result)
    {
        result = default;

        try
        {
            // Check if the type has a JsonTypeInfo in our SourceGenerationContext
            JsonTypeInfo<T>? jsonTypeInfo = GetJsonTypeInfo<T> ();

            if (jsonTypeInfo != null)
            {
                // Use JSON serialization for deep cloning
                string json = JsonSerializer.Serialize (source, jsonTypeInfo);
                result = JsonSerializer.Deserialize<T> (json, jsonTypeInfo);
                return result != null;
            }

            return false;
        }
        catch
        {
            // If any exception occurs during serialization/deserialization,
            // return false to fall back to reflection-based approach
            return false;
        }
    }

    /// <summary>
    /// Gets JsonTypeInfo for a type from the SourceGenerationContext, if available.
    /// </summary>
    /// <typeparam name="T">The type to get JsonTypeInfo for</typeparam>
    /// <returns>JsonTypeInfo if found; otherwise, null</returns>
    private static JsonTypeInfo<T>? GetJsonTypeInfo<T> ()
    {
        // Try to find a matching JsonTypeInfo property in the SourceGenerationContext
        var contextType = ConfigurationManager.SerializerContext.GetType ();

        // First try for an exact type match
        var exactProperty = contextType.GetProperty (typeof (T).Name);

        if (exactProperty != null &&
            exactProperty.PropertyType.IsGenericType &&
            exactProperty.PropertyType.GetGenericTypeDefinition () == typeof (JsonTypeInfo<>) &&
            exactProperty.PropertyType.GetGenericArguments () [0] == typeof (T))
        {
            return (JsonTypeInfo<T>?)exactProperty.GetValue (null);
        }

        // Then look for any compatible JsonTypeInfo
        foreach (var prop in contextType.GetProperties (BindingFlags.Public | BindingFlags.Static))
        {
            if (prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition () == typeof (JsonTypeInfo<>) &&
                prop.PropertyType.GetGenericArguments () [0].IsAssignableFrom (typeof (T)))
            {
                // This is a bit tricky - we've found a compatible type but need to cast it
                // Warning: This might not work for all types and is a bit of a hack
                return (JsonTypeInfo<T>?)prop.GetValue (null);
            }
        }

        return null;
    }


    #endregion AOT Support
}
