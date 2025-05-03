#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
///     Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define
///     scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
/// </summary>
public class Scope<T> : Dictionary<string, ConfigProperty>
{
    /// <summary>Crates a new instance.</summary>
    [RequiresUnreferencedCode ("AOT")]
    public Scope () : base (StringComparer.InvariantCultureIgnoreCase)
    {
        foreach (KeyValuePair<string, ConfigProperty> p in GetConfigPropertiesByScope (typeof (T)))
        {
            Add (p.Key, new () { PropertyInfo = p.Value.PropertyInfo, PropertyValue = null });
        }
    }

    /// <summary>
    ///     Retrieves the values of the properties of this scope from their corresponding static
    ///     <see cref="SerializableConfigurationProperty"/> properties.
    /// </summary>
    public void RetrieveValues ()
    {
        foreach (KeyValuePair<string, ConfigProperty> p in this.Where (cp => cp.Value.PropertyInfo is { }))
        {
            p.Value.RetrieveValue ();
        }
    }

    /// <summary>Updates this instance from the specified source scope.</summary>
    /// <param name="scope"></param>
    /// <returns>The updated scope (this).</returns>
    public Scope<T>? Update (Scope<T> scope)
    {
        foreach (KeyValuePair<string, ConfigProperty> prop in scope)
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

    /// <summary>
    ///     Applies the values of the properties of this scope to their corresponding static properties.
    /// </summary>
    /// <returns></returns>
    internal bool Apply ()
    {
        var set = false;

        foreach (KeyValuePair<string, ConfigProperty> p in this.Where (t => t.Value is { PropertyValue: { } }))
        {
            if (p.Value.PropertyInfo != null)
            {
                object? currentValue = p.Value.PropertyInfo.GetValue (null);

                if (p.Value.PropertyValue is Scope<T> scopeSource && currentValue is Scope<T> scopeDest)
                {
                    p.Value.PropertyInfo.SetValue (null, scopeDest.Update (scopeSource));
                }
                else
                {
                    // Fallback to generic deep copy
                    object? val = ScopeExtensions.DeepMemberWiseCopy (p.Value.PropertyValue, currentValue);
                    p.Value.PropertyInfo.SetValue (null, val);
                }

                set = true;
            }
        }

        return set;
    }
}
