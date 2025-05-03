#nullable enable
using System.Collections;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
///     Extends <see cref="Scope{T}"/>.
/// </summary>
public static class ScopeExtensions
{
    /// <summary>
    ///     Performs a deep, member-wise copy of the source object to the destination object.
    /// </summary>
    /// <param name="source">The source object to copy from.</param>
    /// <param name="destination">The destination object to copy to.</param>
    /// <returns>The updated destination object.</returns>
    public static object? DeepMemberWiseCopy (object? source, object? destination)
    {
        ArgumentNullException.ThrowIfNull (destination);

        if (source is null)
        {
            return null!;
        }

        // Handle value types and strings
        if (source.GetType ().IsValueType || source is string)
        {
            return source;
        }

        // Handle arrays
        if (source is Array sourceArray && destination is Array destinationArray)
        {
            if (sourceArray.Length != destinationArray.Length)
            {
                throw new ArgumentException ("Source and destination arrays must have the same length.");
            }

            for (int i = 0; i < sourceArray.Length; i++)
            {
                object? sourceElement = sourceArray.GetValue (i);
                object? destinationElement = destinationArray.GetValue (i);

                destinationArray.SetValue (DeepMemberWiseCopy (sourceElement, destinationElement), i);
            }

            return destinationArray;
        }

        // Handle dictionaries
        if (source is IDictionary sourceDict && destination is IDictionary destDict)
        {
            foreach (var key in sourceDict.Keys)
            {
                if (destDict.Contains (key))
                {
                    destDict [key] = DeepMemberWiseCopy (sourceDict [key], destDict [key]);
                }
                else
                {
                    destDict.Add (key, sourceDict [key]);
                }
            }

            return destination;
        }

        // Handle other object types using reflection (fallback)
        Type sourceType = source.GetType ();
        Type destinationType = destination.GetType ();

        if (sourceType == destinationType)
        {
            foreach (PropertyInfo property in sourceType.GetProperties (BindingFlags.Public | BindingFlags.Instance)
                                                        .Where (p => p.CanRead && p.CanWrite))
            {
                object? sourceValue = property.GetValue (source);
                object? destinationValue = property.GetValue (destination);

                if (sourceValue is { })
                {
                    property.SetValue (destination, destinationValue is { }
                        ? DeepMemberWiseCopy (sourceValue, destinationValue)
                        : sourceValue);
                }
            }
        }

        return destination;
    }

}
