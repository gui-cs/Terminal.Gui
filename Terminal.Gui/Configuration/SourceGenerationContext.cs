using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Allow AOT and self-contained single file applications with the <see cref="System.Text.Json.Serialization"/>.
///     <para>
///         The SourceGenerationContext class leverages the System.Text.Json source generation feature to pre-generate
///         serialization metadata for specific types. This approach avoids runtime reflection, which is problematic in AOT
///         scenarios where metadata might be stripped, and improves performance by generating serialization code at
///         compile time.
///     </para>
/// </summary>
[JsonSerializable (typeof (bool?))]
[JsonSerializable (typeof (Dictionary<string, object>))]
[JsonSerializable (typeof (List<string>))]

[JsonSerializable (typeof (Attribute))]
[JsonSerializable (typeof (Color))]
[JsonSerializable (typeof (Key))]
[JsonSerializable (typeof (Glyphs))]
[JsonSerializable (typeof (Alignment))]
[JsonSerializable (typeof (AlignmentModes))]
[JsonSerializable (typeof (LineStyle))]
[JsonSerializable (typeof (ShadowStyle))]
[JsonSerializable (typeof (MouseState))]
[JsonSerializable (typeof (TextStyle))]
[JsonSerializable (typeof (Dictionary<ColorName16, string>))]
[JsonSerializable (typeof (Dictionary<string, Color>))]

[JsonSerializable (typeof (Dictionary<string, ConfigProperty>))]
[JsonSerializable (typeof (ConcurrentDictionary<string, ConfigProperty>))]
[JsonSerializable (typeof (ConfigProperty))]

[JsonSerializable (typeof (Scope<string>))]
[JsonSerializable (typeof (AppSettingsScope))]
[JsonSerializable (typeof (SettingsScope))]
[JsonSerializable (typeof (ThemeScope))]
[JsonSerializable (typeof (Scope<ThemeScope>))]
[JsonSerializable (typeof (Scope<AppSettingsScope>))]
[JsonSerializable (typeof (Scope<SettingsScope>))]
[JsonSerializable (typeof (ConcurrentDictionary<string, ThemeScope>))]
[JsonSerializable (typeof (Dictionary<string, Scheme>))]

internal partial class SourceGenerationContext : JsonSerializerContext
{ }
