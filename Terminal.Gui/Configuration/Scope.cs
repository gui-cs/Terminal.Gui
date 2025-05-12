#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
///     Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define
///     scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
///     <para>
///         When constructed, the dictionary will be populated with the uninitialized configuration properties for the scope (<see cref="ConfigProperty.HasValue"/> will be <see langword="false"/>).
///     </para>
///     <para>
///     </para>
/// </summary>
public class Scope<T> : Dictionary<string, ConfigProperty>
{
    /// <summary>
    ///     Creates a new instance. The dictionary will be populated with uninitialized (<see cref="ConfigProperty.HasValue"/> will be <see langword="false"/>).
    /// </summary>
    [RequiresUnreferencedCode ("Uses cached configuration properties filtered by type T. This is AOT-safe as long as T is one of the known scope types (SettingsScope, ThemeScope, AppSettingsScope).")]
    public Scope () : base (StringComparer.InvariantCultureIgnoreCase)
    {
        // Populate the dictionary with uninitialized, mutable, properties
        foreach (KeyValuePair<string, ConfigProperty> p in GetConfigPropertiesByScope (typeof (T).Name)!)
        {
            Add (p.Key, new ()
            {
                // Copy just the PropertyInfo, NOT PropertyValue
                PropertyInfo = p.Value.PropertyInfo,
                OmitClassName = p.Value.OmitClassName,
                ScopeType = p.Value.ScopeType,
                Immutable = false
            });
        }
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
    ///     INTERNAL: Updates the values of the properties of this scope to their corresponding hard-coded original values.
    /// </summary>
    internal void LoadHardCodedDefaults ()
    {
        foreach (KeyValuePair<string, ConfigProperty> hardCodedKeyValuePair in GetHardCodedConfigPropertiesByScope (typeof (T).Name)!)
        {
            if (!ContainsKey (hardCodedKeyValuePair.Key))
            {
                continue;
            }
            this [hardCodedKeyValuePair.Key].PropertyValue = hardCodedKeyValuePair.Value.PropertyValue;
        }
    }

    /// <summary>
    ///     INTERNAL: Updates this scope with the values in <paramref name="scope"/> using a deep clone.
    /// </summary>
    /// <param name="scope"></param>
    /// <returns>The updated scope (this).</returns>
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
                // Add the property to this scope
                ConfigProperty? copy = new ConfigProperty ()
                {
                    Immutable = false,
                    PropertyInfo = prop.Value.PropertyInfo,
                    OmitClassName = prop.Value.OmitClassName,
                    ScopeType = prop.Value.ScopeType,
                    HasValue = false
                };
                Add (prop.Key, copy);
                this [prop.Key].UpdateFrom (prop.Value.PropertyValue);
            }
            this [prop.Key].UpdateFrom (prop.Value.PropertyValue);
        }

        return this;
    }
    /// <summary>
    ///     INTERNAL: Applies the values of the properties of this scope to their corresponding <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    /// <returns><see langword="true"/> if one or more property value was applied; <see langword="false"/> otherwise.</returns>
    [RequiresDynamicCode ("Uses reflection to get and set property values")]
    internal bool Apply ()
    {
        if (!IsEnabled)
        {
            Logging.Warning($"Apply called when CM is not Enabled. This should only be done from unit tests where side-effects are managed.");
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
}
