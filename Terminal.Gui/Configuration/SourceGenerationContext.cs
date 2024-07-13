using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Allow AOT and self-contained single file applications with the <see cref="System.Text.Json.Serialization"/>
/// </summary>
[JsonSerializable (typeof (Attribute))]
[JsonSerializable (typeof (Color))]
[JsonSerializable (typeof (ThemeScope))]
[JsonSerializable (typeof (ColorScheme))]
[JsonSerializable (typeof (SettingsScope))]
[JsonSerializable (typeof (AppScope))]
[JsonSerializable (typeof (Key))]
[JsonSerializable (typeof (GlyphDefinitions))]
[JsonSerializable (typeof (ConfigProperty))]
[JsonSerializable (typeof (ShadowStyle))]
[JsonSerializable (typeof (string))]
[JsonSerializable (typeof (bool))]
[JsonSerializable (typeof (bool?))]
[JsonSerializable (typeof (Dictionary<ColorName, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{ }
