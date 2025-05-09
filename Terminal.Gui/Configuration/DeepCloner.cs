#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Terminal.Gui;

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

        // Clone writable public properties
        foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
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

    private static object CreateInstance (Type type)
    {
        // Try parameterless constructor
        if (type.GetConstructor (Type.EmptyTypes) != null)
        {
            return Activator.CreateInstance (type)!;
        }

        // Try record's clone method if it's a record
        if (type.GetMethod ("<Clone>$") != null)
        {
            return Activator.CreateInstance (type)!;
        }

        // Fallback to uninitialized object
        return FormatterServices.GetUninitializedObject (type);
    }

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

    private static object CloneDictionary (object source, ConcurrentDictionary<object, object> visited)
    {
        var sourceDict = (IDictionary)source;
        Type type = source.GetType ();

        // Check for frozen or immutable dictionaries and throw if found
        if (type.IsGenericType)
        {
            Type genericTypeDef = type.GetGenericTypeDefinition ();
            Type? currentType = type;

            while (currentType != null && currentType != typeof (object))
            {
                if (currentType.IsGenericType)
                {
                    genericTypeDef = currentType.GetGenericTypeDefinition ();

                    if (genericTypeDef.FullName != null
                        && (genericTypeDef.FullName.StartsWith ("System.Collections.Frozen")
                            || genericTypeDef.FullName.StartsWith ("System.Collections.Immutable")))
                    {
                        throw new NotSupportedException ($"Cloning of frozen or immutable dictionaries like {type.Name} is not supported.");
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        Type [] genericArgs = type.GetGenericArguments ();
        Type dictType;

        if (genericArgs.Length == 2)
        {
            dictType = typeof (Dictionary<,>).MakeGenericType (genericArgs);
        }
        else
        {
            dictType = typeof (Dictionary<object, object>);
        }

        // Create a temporary dictionary to hold the cloned key-value pairs
        var tempDict = (IDictionary)Activator.CreateInstance (dictType)!;

        // Add to visited before cloning contents to prevent circular reference issues
        visited.TryAdd (source, tempDict);

        // Clone all key-value pairs
        foreach (object? key in sourceDict.Keys)
        {
            object? value = sourceDict [key];
            object? clonedKey = DeepCloneInternal (key, visited);
            object? clonedValue = DeepCloneInternal (value, visited);

            // Handle duplicate keys by updating the value (last value wins, aligning with layering)
            if (tempDict.Contains (clonedKey!))
            {
                tempDict [clonedKey!] = clonedValue;
            }
            else
            {
                tempDict.Add (clonedKey!, clonedValue);
            }
        }

        // Try to create an instance of the original dictionary type if it has a parameterless constructor
        if (type.GetConstructor (Type.EmptyTypes) != null)
        {
            var newDict = (IDictionary)Activator.CreateInstance (type)!;

            // Clear any pre-populated keys from the constructor
            newDict.Clear ();

            // Update visited to point to the final object
            visited [source] = newDict;

            // Copy the cloned key-value pairs to the new dictionary, handling duplicates
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

            // Clone any additional properties of the derived dictionary type
            foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
                                              .Where (p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters ().Length == 0))
            {
                object? value = prop.GetValue (source);
                object? clonedValue = DeepCloneInternal (value, visited);
                prop.SetValue (newDict, clonedValue);
            }

            return newDict;
        }

        return tempDict;
    }
}
