#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace Terminal.Gui;


/// <summary>
///     Provides deep cloning functionality for Terminal.Gui configuration objects.
///     Creates a deep copy of an object by recursively cloning public properties,
///     handling collections, arrays, dictionaries, and circular references.
/// </summary>
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
            return default;
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

        // Validate that the type can be cloned
        if (type.GetConstructor (Type.EmptyTypes) == null && type.GetMethod ("<Clone>$") == null)
        {
            throw new ArgumentException ($"Type '{type.Name}' cannot be cloned because it lacks a parameterless constructor and is not a supported type (simple type, array, dictionary, or collection).");
        }

        // Create new instance
        object clone = CreateInstance (type);

        // Add to visited before cloning properties
        visited.TryAdd (source, clone);

        // Clone writable public properties
        foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
                                          .Where (p => p.CanRead && p.CanWrite && p.GetIndexParameters ().Length == 0))
        {
            object? value = prop.GetValue (source);
            object? clonedValue = DeepCloneInternal (value, visited);
            prop.SetValue (clone, clonedValue);
        }

        return clone;
    }

    private static bool IsSimpleType (Type type)
    {
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof (decimal) ||
            type == typeof (DateTime) ||
            type == typeof (DateTimeOffset) ||
            type == typeof (TimeSpan) ||
            type == typeof (Guid) ||
            type == typeof (Rune) ||
            type == typeof (string))
        {
            return true;
        }

        // Treat structs with no writable public properties as simple types (immutable structs)
        if (type.IsValueType) // Structs are value types
        {
            var writableProperties = type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
                                         .Where (p => p.CanRead && p.CanWrite && p.GetIndexParameters ().Length == 0);
            return !writableProperties.Any ();
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
        return System.Runtime.Serialization.FormatterServices.GetUninitializedObject (type);
    }

    private static object CloneArray (object source, ConcurrentDictionary<object, object> visited)
    {
        Array array = (Array)source;
        Type elementType = array.GetType ().GetElementType ()!;
        Array newArray = Array.CreateInstance (elementType, array.Length);
        visited.TryAdd (source, newArray);

        for (int i = 0; i < array.Length; i++)
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
        IList tempList = (IList)Activator.CreateInstance (listType)!;

        foreach (object? item in (IEnumerable)source)
        {
            object? clonedItem = DeepCloneInternal (item, visited);
            tempList.Add (clonedItem);
        }

        // Try to create the original collection type if possible
        if (type != listType && type.GetConstructor (new [] { listType }) != null)
        {
            object result = Activator.CreateInstance (type, tempList)!;
            visited.TryAdd (source, result);
            return result;
        }

        visited.TryAdd (source, tempList);
        return tempList;
    }

    private static object CloneDictionary (object source, ConcurrentDictionary<object, object> visited)
    {
        IDictionary sourceDict = (IDictionary)source;
        Type type = source.GetType ();

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
        IDictionary tempDict = (IDictionary)Activator.CreateInstance (dictType)!;

        // Add to visited before cloning contents to prevent circular reference issues
        visited.TryAdd (source, tempDict);

        // Clone all key-value pairs
        foreach (object? key in sourceDict.Keys)
        {
            object? value = sourceDict [key];
            object? clonedKey = DeepCloneInternal (key, visited);
            object? clonedValue = DeepCloneInternal (value, visited);

            // Handle duplicate keys by updating the value (last value wins, aligning with layering)
            if (tempDict.Contains (clonedKey))
            {
                tempDict [clonedKey] = clonedValue;
            }
            else
            {
                tempDict.Add (clonedKey, clonedValue);
            }
        }

        // Handle ImmutableDictionary specifically
        if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (ImmutableDictionary<,>))
        {
            // Use ToImmutableDictionary to create a new instance with cloned key-value pairs
            var keyValuePairType = typeof (KeyValuePair<,>).MakeGenericType (genericArgs);
            var enumerableType = typeof (IEnumerable<>).MakeGenericType (keyValuePairType);
            var toImmutableDictMethod = typeof (ImmutableDictionary)
                                        .GetMethods ()
                                        .Where (m => m.Name == "ToImmutableDictionary" && m.GetParameters ().Length == 3)
                                        .First (m => m.GetParameters () [0].ParameterType == enumerableType)
                                        .MakeGenericMethod (genericArgs [0], genericArgs [1]);

            // Convert tempDict to an IEnumerable<KeyValuePair<TKey, TValue>>
            var keyValueList = (IEnumerable)tempDict;

            // Create the new ImmutableDictionary
            object immutableDict = toImmutableDictMethod.Invoke (null, new object [] { keyValueList, null, null })!;

            // Update visited to point to the final object
            visited [source] = immutableDict;
            return immutableDict;
        }

        // Try to create an instance of the original dictionary type if it has a parameterless constructor
        if (type.GetConstructor (Type.EmptyTypes) != null)
        {
            IDictionary newDict = (IDictionary)Activator.CreateInstance (type)!;
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
                                             .Where (p => p.CanRead && p.CanWrite && p.GetIndexParameters ().Length == 0))
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
