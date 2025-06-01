#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define
///     scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
///     <para>
///         When constructed, the dictionary will be populated with uninitialized configuration properties for the
///         scope (<see cref="ConfigProperty.HasValue"/> will be <see langword="false"/>).
///     </para>
/// </summary>
public class Scope<T> : ConcurrentDictionary<string, ConfigProperty>
{
    /// <summary>
    ///     Creates a new instance. The dictionary will be populated with uninitialized (<see cref="ConfigProperty.HasValue"/>
    ///     will be <see langword="false"/>).
    /// </summary>
    [RequiresUnreferencedCode (
        "Uses cached configuration properties filtered by type T. This is AOT-safe as long as T is one of the known scope types (SettingsScope, ThemeScope, AppSettingsScope).")]
    public Scope () : base (StringComparer.InvariantCultureIgnoreCase)
    {
    }

    /// <summary>
    ///     INTERNAL: Adds a new ConfigProperty given a <paramref name="value"/>. Determines the correct PropertyInfo etc... by retrieving the
    ///     hard coded value for <paramref name="name"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    internal void AddValue (string name, object? value)
    {
        ConfigProperty? configProperty = GetHardCodedProperty (name);

        if (configProperty is null)
        {
            throw new InvalidOperationException ($@"{name} is not a hard coded property.");
        }

        TryAdd (name, ConfigProperty.CreateCopy (configProperty));
        this [name].PropertyValue = configProperty.PropertyValue;

    }

    internal ConfigProperty? GetHardCodedProperty (string name)
    {
        ConfigProperty? configProperty = ConfigurationManager.GetHardCodedConfigPropertiesByScope (typeof (T).Name)!
                                                             .FirstOrDefault (hardCodedKeyValuePair => hardCodedKeyValuePair.Key == name).Value;

        if (configProperty is null)
        {
            return null;
        }

        ConfigProperty copy = ConfigProperty.CreateCopy (configProperty);
        copy.PropertyValue = configProperty.PropertyValue;

        return copy;
    }

    internal ConfigProperty GetUninitializedProperty (string name)
    {
        ConfigProperty? configProperty = ConfigurationManager.GetUninitializedConfigPropertiesByScope (typeof (T).Name)!
                                                             .FirstOrDefault (hardCodedKeyValuePair => hardCodedKeyValuePair.Key == name).Value;

        if (configProperty is null)
        {
            throw new InvalidOperationException ($@"{name} is not a ConfigProperty.");
        }
        ConfigProperty  copy = ConfigProperty.CreateCopy (configProperty);
        copy.PropertyValue = configProperty.PropertyValue;

        return copy;

    }

    /// <summary>
    ///     INTERNAL: Updates the values of the properties of this scope to their corresponding static
    ///     <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    [RequiresDynamicCode ("Uses reflection to retrieve property values")]
    internal void LoadCurrentValues ()
    {
        foreach (KeyValuePair<string, ConfigProperty> validProperties in this.Where (cp => cp.Value.PropertyInfo is { }))
        {
            validProperties.Value.UpdateToCurrentValue ();
        }
    }

    /// <summary>
    ///     INTERNAL: Updates the values of all properties of this scope to their corresponding hard-coded original values.
    /// </summary>
    internal void LoadHardCodedDefaults ()
    {
        foreach (KeyValuePair<string, ConfigProperty> hardCodedKeyValuePair in ConfigurationManager.GetHardCodedConfigPropertiesByScope (typeof (T).Name)!)
        {
            ConfigProperty copy = ConfigProperty.CreateCopy (hardCodedKeyValuePair.Value);
            TryAdd (hardCodedKeyValuePair.Key, copy);
            this [hardCodedKeyValuePair.Key].PropertyValue = hardCodedKeyValuePair.Value.PropertyValue;
        }
    }

    /// <summary>
    ///     INTERNAL: Updates this scope with the values in <paramref name="scope"/> using a deep clone.
    /// </summary>
    /// <param name="scope"></param>
    /// <returns>The updated scope (this).</returns>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    internal Scope<T>? UpdateFrom (Scope<T> scope)
    {
        foreach (KeyValuePair<string, ConfigProperty> prop in scope)
        {
            if (!prop.Value.HasValue)
            {
                continue;
            }

            if (!ContainsKey (prop.Key))
            {
                if (!prop.Value.HasValue)
                {
                    continue;
                }

                // Add an empty (HasValue = false) property to this scope
                ConfigProperty copy = ConfigProperty.CreateCopy (prop.Value);
                copy.PropertyValue = prop.Value.PropertyValue;
                TryAdd (prop.Key, copy);
            }

            // Update the property value
            this [prop.Key].UpdateFrom (prop.Value.PropertyValue);
        }

        return this;
    }

    /// <summary>
    ///     INTERNAL: Applies the values of the properties of this scope to their corresponding
    ///     <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    /// <returns><see langword="true"/> if one or more property value was applied; <see langword="false"/> otherwise.</returns>
    [RequiresDynamicCode ("Uses reflection to get and set property values")]
    [RequiresUnreferencedCode ("Calls Terminal.Gui.DeepCloner.DeepClone<T>(T)")]
    internal bool Apply ()
    {
        if (!ConfigurationManager.IsEnabled)
        {
            Logging.Warning ("Apply called when CM is not Enabled. This should only be done from unit tests where side-effects are managed.");
        }

        var set = false;

        foreach (KeyValuePair<string, ConfigProperty> propWithValue in this.Where (t => t.Value.HasValue))
        {
            if (propWithValue.Value.PropertyInfo != null)
            {
                object? currentValue = propWithValue.Value.PropertyInfo.GetValue (null);

                // QUESTION: Should we avoid setting if currentValue == newValue?

                if (propWithValue.Value.PropertyValue is Scope<T> scopeSource && currentValue is Scope<T> scopeDest)
                {
                    propWithValue.Value.PropertyInfo.SetValue (null, scopeDest.UpdateFrom (scopeSource));
                }
                else
                {
                    // Use DeepCloner to create a deep copy of the property value
                    object? val = DeepCloner.DeepClone (propWithValue.Value.PropertyValue);
                    propWithValue.Value.PropertyInfo.SetValue (null, val);
                }

                set = true;
            }
        }

        return set;
    }

    internal virtual void Validate ()
    {
        if (IsEmpty)
        {
            //throw new JsonException ($@"Empty!");
        }
    }
}
