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
    ///     Crates a new instance. The dictionary will be populated with uninitialized (<see cref="ConfigProperty.HasValue"/> will be <see langword="false"/>).
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    public Scope () : base (StringComparer.InvariantCultureIgnoreCase)
    {
        // Populate the dictionary with uninitialized, mutable, properties
        foreach (KeyValuePair<string, ConfigProperty> p in GetConfigPropertiesByScope (typeof (T))!)
        {
            Add (p.Key, new ()
            {
                // Copy just the PropertyInfo, NOT PropertyValue
                PropertyInfo = p.Value.PropertyInfo,
                Immutable = false
            });
        }
    }


    /// <summary>
    ///     Retrieves the values of the properties of this scope from their corresponding static
    ///     <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    public void RetrieveValues ()
    {
        foreach (KeyValuePair<string, ConfigProperty> p in this.Where (cp => cp.Value.PropertyInfo is { }))
        {
            p.Value.RetrieveValue ();
        }
    }

    // TODO: Should this take a Dictionary<string, ConfigProperty> instead of a Scope<T>?
    /// <summary>Updates this instance from the specified source scope.</summary>
    /// <param name="scope"></param>
    /// <returns>The updated scope (this).</returns>
    public Scope<T>? Update (Scope<T> scope)
    {
        Debug.Assert(Locations != ConfigLocations.HardCoded);
        foreach (KeyValuePair<string, ConfigProperty> prop in scope)
        {
            if (ContainsKey (prop.Key))
            {
                this [prop.Key].PropertyValue = this [prop.Key].UpdateValueFrom (prop.Value.PropertyValue!);
            }
            else
            {
                // Add the property to this scope
                Add (prop.Key, new ());
                this [prop.Key].PropertyValue = prop.Value.PropertyValue;
            }
        }

        return this;
    }
    /// <summary>
    ///     Applies the values of the properties of this scope to their corresponding <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    /// <returns><see langword="true"/> if one or more property value was applied; <see langword="false"/> otherwise.</returns>
    internal bool Apply ()
    {
        if (!IsEnabled)
        {
            return false;
        }

        //Debug.Assert(Locations != ConfigLocations.HardCoded);
        var set = false;

        foreach (KeyValuePair<string, ConfigProperty> p in this.Where (t => t.Value is { PropertyValue: { } }))
        {
            if (!p.Value.HasValue)
            {
                continue;
                //throw new ArgumentException($"Property {p.Key} has no value.");
            }

            if (p.Value.PropertyInfo != null)
            {
                object? currentValue = p.Value.PropertyInfo.GetValue (null);

                if (p.Value.PropertyValue is Scope<T> scopeSource && currentValue is Scope<T> scopeDest)
                {
                    p.Value.PropertyInfo.SetValue (null, scopeDest.Update (scopeSource));
                }
                else
                {
                    // Use DeepCloner to create a deep copy of the property value
                    object? val = DeepCloner.DeepClone (p.Value.PropertyValue);
                    p.Value.PropertyInfo.SetValue (null, val);
                }

                set = true;
            }
        }

        return set;
    }
}
