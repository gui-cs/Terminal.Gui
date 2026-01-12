using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

// TODO: Change to internal to prevent app usage
/// <summary>
///     The root object for a Theme. A Theme is a set of settings that are applied to the running
///     <see cref="Application"/> as a group.
/// </summary>
/// <remarks>
///     <para></para>
/// </remarks>
/// <example>
///     <code>
/// 	"Default": {
/// 		"Schemes": [
/// 		{
/// 		"Runnable": {
/// 		"Normal": {
/// 			"Foreground": "BrightGreen",
/// 			"Background": "Black"
/// 		},
/// 		"Focus": {
/// 		"Foreground": "White",
/// 			"Background": "Cyan"
/// 
/// 		},
/// 		"HotNormal": {
/// 			"Foreground": "Yellow",
/// 			"Background": "Black"
/// 
/// 		},
/// 		"HotFocus": {
/// 			"Foreground": "Blue",
/// 			"Background": "Cyan"
/// 		},
/// 		"Disabled": {
/// 			"Foreground": "DarkGray",
/// 			"Background": "Black"
/// 
/// 		}
/// 	}
/// </code>
/// </example>
#pragma warning disable IL2026 // ScopeJsonConverter and Scope<T> are AOT-compatible for known scope types
[JsonConverter (typeof (ScopeJsonConverter<ThemeScope>))]
public class ThemeScope : Scope<ThemeScope>
#pragma warning restore IL2026
{
}


