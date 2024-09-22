using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Allow AOT and self-contained single file applications with the <see cref="System.Text.Json.Serialization"/>
/// </summary>
[JsonSerializable (typeof (Attribute))]
[JsonSerializable (typeof (Color))]
[JsonSerializable (typeof (AppScope))]
[JsonSerializable (typeof (SettingsScope))]
[JsonSerializable (typeof (Key))]
[JsonSerializable (typeof (GlyphDefinitions))]
[JsonSerializable (typeof (Alignment))]
[JsonSerializable (typeof (AlignmentModes))]
[JsonSerializable (typeof (LineStyle))]
[JsonSerializable (typeof (ShadowStyle))]
[JsonSerializable (typeof (HighlightStyle))]
[JsonSerializable (typeof (bool?))]
[JsonSerializable (typeof (Dictionary<ColorName, string>))]
[JsonSerializable (typeof (Dictionary<string, ThemeScope>))]
[JsonSerializable (typeof (Dictionary<string, ColorScheme>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{ }
