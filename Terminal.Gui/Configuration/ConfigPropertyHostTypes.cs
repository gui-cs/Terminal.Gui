using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS0618 // Obsolete - ConfigPropertyHostTypes references ConfigurationPropertyAttribute during transition

namespace Terminal.Gui.Configuration;

/// <summary>
///     INTERNAL: The statically-known set of types that own
///     <see cref="ConfigurationPropertyAttribute"/>-decorated properties.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ConfigurationManager"/> used to discover these types by reflecting over every loaded
///         assembly at module-init time. That scan was not trim-safe: with <c>PublishTrimmed=true</c>, types
///         not otherwise referenced by the consuming application were stripped and
///         <c>Scope&lt;T&gt;.GetUninitializedProperty</c> would throw at startup.
///         See <see href="https://github.com/gui-cs/Terminal.Gui/issues/5069"/>.
///     </para>
///     <para>
///         The per-type <see cref="DynamicDependencyAttribute"/> entries on <see cref="GetTypes"/> root
///         each host and preserve its <see cref="DynamicallyAccessedMemberTypes.PublicProperties"/> so the
///         trimmer does not remove the properties the scan depends on.
///     </para>
///     <para>
///         A unit test verifies this list stays in sync with the types that actually carry
///         <see cref="ConfigurationPropertyAttribute"/> so drift is caught at build time, not in AOT
///         consumer apps.
///     </para>
/// </remarks>
[Obsolete ("Being replaced by Microsoft.Extensions.Configuration. Will be removed in a future version.")]
internal static class ConfigPropertyHostTypes
{
    private const DynamicallyAccessedMemberTypes PRESERVED_MEMBERS = DynamicallyAccessedMemberTypes.PublicProperties;

    private static readonly Type [] _types =
    [
        typeof (Application),
        typeof (ConfigurationManager),
        typeof (SchemeManager),
        typeof (ThemeManager),
        typeof (Color),
        typeof (View)
    ];

    [DynamicDependency (PRESERVED_MEMBERS, typeof (Application))]
    [DynamicDependency (PRESERVED_MEMBERS, typeof (ConfigurationManager))]
    [DynamicDependency (PRESERVED_MEMBERS, typeof (SchemeManager))]
    [DynamicDependency (PRESERVED_MEMBERS, typeof (ThemeManager))]
    [DynamicDependency (PRESERVED_MEMBERS, typeof (Color))]
    [DynamicDependency (PRESERVED_MEMBERS, typeof (View))]
    internal static Type [] GetTypes () => _types;
}
