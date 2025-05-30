#nullable enable
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
/// 		"TopLevel": {
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
[JsonConverter (typeof (ScopeJsonConverter<ThemeScope>))]
public class ThemeScope : Scope<ThemeScope>
{
}


