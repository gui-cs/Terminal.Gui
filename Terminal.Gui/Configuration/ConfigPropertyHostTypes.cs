using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Text;
using Terminal.Gui.Tracing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

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
internal static class ConfigPropertyHostTypes
{
    private const DynamicallyAccessedMemberTypes PreservedMembers = DynamicallyAccessedMemberTypes.PublicProperties;

    private static readonly Type [] _types =
    [
        typeof (Application),
        typeof (ConfigurationManager),
        typeof (SchemeManager),
        typeof (ThemeManager),
        typeof (Color),
        typeof (Glyphs),
        typeof (Driver),
        typeof (Key),
        typeof (NerdFonts),
        typeof (Trace),
        typeof (View),
        typeof (Button),
        typeof (CharMap),
        typeof (CheckBox),
        typeof (Dialog),
        typeof (FileDialog),
        typeof (FileDialogStyle),
        typeof (FrameView),
        typeof (HexView),
        typeof (LinearRange),
        typeof (Menu),
        typeof (MenuBar),
        typeof (MessageBox),
        typeof (PopoverMenu),
        typeof (SelectorBase),
        typeof (StatusBar),
        typeof (TextField),
        typeof (TextView),
        typeof (Window)
    ];

    [DynamicDependency (PreservedMembers, typeof (Application))]
    [DynamicDependency (PreservedMembers, typeof (ConfigurationManager))]
    [DynamicDependency (PreservedMembers, typeof (SchemeManager))]
    [DynamicDependency (PreservedMembers, typeof (ThemeManager))]
    [DynamicDependency (PreservedMembers, typeof (Color))]
    [DynamicDependency (PreservedMembers, typeof (Glyphs))]
    [DynamicDependency (PreservedMembers, typeof (Driver))]
    [DynamicDependency (PreservedMembers, typeof (Key))]
    [DynamicDependency (PreservedMembers, typeof (NerdFonts))]
    [DynamicDependency (PreservedMembers, typeof (Trace))]
    [DynamicDependency (PreservedMembers, typeof (View))]
    [DynamicDependency (PreservedMembers, typeof (Button))]
    [DynamicDependency (PreservedMembers, typeof (CharMap))]
    [DynamicDependency (PreservedMembers, typeof (CheckBox))]
    [DynamicDependency (PreservedMembers, typeof (Dialog))]
    [DynamicDependency (PreservedMembers, typeof (FileDialog))]
    [DynamicDependency (PreservedMembers, typeof (FileDialogStyle))]
    [DynamicDependency (PreservedMembers, typeof (FrameView))]
    [DynamicDependency (PreservedMembers, typeof (HexView))]
    [DynamicDependency (PreservedMembers, typeof (LinearRange))]
    [DynamicDependency (PreservedMembers, typeof (Menu))]
    [DynamicDependency (PreservedMembers, typeof (MenuBar))]
    [DynamicDependency (PreservedMembers, typeof (MessageBox))]
    [DynamicDependency (PreservedMembers, typeof (PopoverMenu))]
    [DynamicDependency (PreservedMembers, typeof (SelectorBase))]
    [DynamicDependency (PreservedMembers, typeof (StatusBar))]
    [DynamicDependency (PreservedMembers, typeof (TextField))]
    [DynamicDependency (PreservedMembers, typeof (TextView))]
    [DynamicDependency (PreservedMembers, typeof (Window))]
    internal static Type [] GetTypes () => _types;
}
