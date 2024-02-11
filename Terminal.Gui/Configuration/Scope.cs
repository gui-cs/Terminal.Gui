﻿#nullable enable
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
///     Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
/// </summary>
public class Scope<T> : Dictionary<string, ConfigProperty>
{ //, IScope<Scope<T>> {
    /// <summary>Crates a new instance.</summary>
    public Scope () : base (StringComparer.InvariantCultureIgnoreCase)
    {
        foreach (KeyValuePair<string, ConfigProperty> p in GetScopeProperties ())
        {
            Add (p.Key, new ConfigProperty { PropertyInfo = p.Value.PropertyInfo, PropertyValue = null });
        }
    }

    /// <summary>Retrieves the values of the properties of this scope from their corresponding static properties.</summary>
    public void RetrieveValues ()
    {
        foreach (KeyValuePair<string, ConfigProperty> p in this.Where (cp => cp.Value.PropertyInfo != null))
        {
            p.Value.RetrieveValue ();
        }
    }

    /// <summary>Updates this instance from the specified source scope.</summary>
    /// <param name="source"></param>
    /// <returns>The updated scope (this).</returns>
    public Scope<T>? Update (Scope<T> source)
    {
        foreach (KeyValuePair<string, ConfigProperty> prop in source)
        {
            if (ContainsKey (prop.Key))
            {
                this [prop.Key].PropertyValue = this [prop.Key].UpdateValueFrom (prop.Value.PropertyValue!);
            }
            else
            {
                this [prop.Key].PropertyValue = prop.Value.PropertyValue;
            }
        }

        return this;
    }

    /// <summary>Applies the values of the properties of this scope to their corresponding static properties.</summary>
    /// <returns></returns>
    internal virtual bool Apply ()
    {
        var set = false;

        foreach (KeyValuePair<string, ConfigProperty> p in this.Where (
                                                                       t => t.Value != null
                                                                            && t.Value.PropertyValue != null
                                                                      ))
        {
            if (p.Value.Apply ())
            {
                set = true;
            }
        }

        return set;
    }

    private IEnumerable<KeyValuePair<string, ConfigProperty>> GetScopeProperties ()
    {
        return _allConfigProperties!.Where (
                                            cp =>
                                                (cp.Value.PropertyInfo?.GetCustomAttribute (
                                                                                            typeof (SerializableConfigurationProperty)
                                                                                           )
                                                     as SerializableConfigurationProperty)?.Scope
                                                == GetType ()
                                           );
    }
}
