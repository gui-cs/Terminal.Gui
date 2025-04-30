#nullable enable
using System.Collections;
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

    /// <summary>
    ///     Holds the property's value as it was either read from the class's implementation or from a config file. If the
    ///     property has not been set (e.g. because no configuration file specified a value), this will be
    ///     <see langword="null"/>.
    /// </summary>
    /// <remarks>
    ///     On <see langword="set"/>, performs a sparse-copy of the new value to the existing value (only copies elements
    ///     of the object that are non-null).
    /// </remarks>
    public object? PropertyValue { get; set; }

    /// <summary>Applies the <see cref="PropertyValue"/> to the static property described by <see cref="PropertyInfo"/>.</summary>
    /// <returns></returns>
    public bool Apply ()
    {
        try
        {
            if (PropertyInfo?.GetValue (null) is { })
            {
                var val = DeepMemberWiseCopy (PropertyValue, PropertyInfo?.GetValue (null));
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
                                         $"The source object ({
                                             PropertyInfo!.DeclaringType
                                         }.{
                                             PropertyInfo!.Name
                                         }) is not of type {
                                             PropertyInfo!.PropertyType
                                         }."
                                        );
        }

        if (PropertyValue is { })
        {
            PropertyValue = DeepMemberWiseCopy (source, PropertyValue);
        }
        else
        {
            PropertyValue = source;
        }

        return PropertyValue;
    }


    /// <summary>
    ///     System.Text.Json does not support copying a deserialized object to an existing instance. To work around this,
    ///     we implement a 'deep, member-wise copy' method.
    /// </summary>
    /// <remarks>TOOD: When System.Text.Json implements `PopulateObject` revisit https://github.com/dotnet/corefx/issues/37627</remarks>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns><paramref name="destination"/> updated from <paramref name="source"/></returns>
    internal static object? DeepMemberWiseCopy (object? source, object? destination)
    {
        ArgumentNullException.ThrowIfNull (destination);

        if (source is null)
        {
            return null!;
        }

        if (source.GetType () == typeof (SettingsScope))
        {
            return ((SettingsScope)destination).Update ((SettingsScope)source);
        }

        if (source.GetType () == typeof (ThemeScope))
        {
            return ((ThemeScope)destination).Update ((ThemeScope)source);
        }

        if (source.GetType () == typeof (AppScope))
        {
            return ((AppScope)destination).Update ((AppScope)source);
        }

        // If value type, just use copy constructor.
        if (source.GetType ().IsValueType || source is string)
        {
            return source;
        }

        // HACK: Key is a class, but we want to treat it as a value type so just _keyCode gets copied.
        if (source.GetType () == typeof (Key))
        {
            return source;
        }

        // Dictionary
        if (source.GetType ().IsGenericType
            && source.GetType ().GetGenericTypeDefinition ().IsAssignableFrom (typeof (Dictionary<,>)))
        {
            foreach (object? srcKey in ((IDictionary)source).Keys)
            {
                if (((IDictionary)destination).Contains (srcKey))
                {
                    ((IDictionary)destination) [srcKey] =
                        DeepMemberWiseCopy (((IDictionary)source) [srcKey], ((IDictionary)destination) [srcKey]);
                }
                else
                {
                    ((IDictionary)destination).Add (srcKey, ((IDictionary)source) [srcKey]);
                }
            }

            return destination;
        }

        // ALl other object types
        List<PropertyInfo>? sourceProps = source?.GetType ().GetProperties ().Where (x => x.CanRead).ToList ();
        List<PropertyInfo>? destProps = destination?.GetType ().GetProperties ().Where (x => x.CanWrite).ToList ()!;

        foreach ((PropertyInfo? sourceProp, PropertyInfo? destProp) in
                 from sourceProp in sourceProps
                 where destProps.Any (x => x.Name == sourceProp.Name)
                 let destProp = destProps.First (x => x.Name == sourceProp.Name)
                 where destProp.CanWrite
                 select (sourceProp, destProp))
        {
            object? sourceVal = sourceProp.GetValue (source);
            object? destVal = destProp.GetValue (destination);

            if (sourceVal is { })
            {
                try
                {
                    if (destVal is { })
                    {
                        // Recurse
                        destProp.SetValue (destination, DeepMemberWiseCopy (sourceVal, destVal));
                    }
                    else
                    {
                        destProp.SetValue (destination, sourceVal);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new JsonException ($"Error Applying Configuration Change: {e.Message}", e);
                }
            }
        }

        return destination;
    }

}
