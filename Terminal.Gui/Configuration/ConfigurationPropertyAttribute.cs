#nullable enable

namespace Terminal.Gui.Configuration;

/// <summary>An attribute indicating a property is managed by <see cref="ConfigurationManager"/>.</summary>
/// <example>
///     [ConfigurationProperty(Scope = typeof(AppSettingsScope))] public static string? MyProperty { get; set; } = "MyValue";
/// </example>
[AttributeUsage (AttributeTargets.Property)]
public class ConfigurationPropertyAttribute : System.Attribute
{
    /// <summary>
    ///     If <see langword="true"/>, the property will be serialized to the configuration file using only the property
    ///     name as the key. If <see langword="false"/>, the property will be serialized to the configuration file using the
    ///     property name pre-pended with the classname (e.g. <c>Application.UseSystemConsole</c>).
    /// </summary>
    public bool OmitClassName { get; set; }

    private Type? _scope;

    /// <summary>Specifies the scope of the property. If <see langword="null"/> then <see cref="AppSettingsScope"/> will be used.</summary>
    public Type? Scope
    {
        get
        {
            if (_scope is { })
            {
                return _scope;
            }
            return typeof (AppSettingsScope);
        }
        set
        {
            if (value == typeof (AppSettingsScope) && OmitClassName)
            {
                throw new ArgumentException ("OmitClassName is not allowed when Scope is AppSettingsScope to ensure property names are globally unique.");
            }
            _scope = value;
        }
    }
}
