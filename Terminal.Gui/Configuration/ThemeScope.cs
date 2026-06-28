using System.Text.Json.Serialization;

#pragma warning disable CS0618 // Obsolete - ThemeScope references Scope internally

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
/// 		"Accent": {
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
[Obsolete ("Being replaced by Microsoft.Extensions.Configuration. Will be removed in a future version.")]
public class ThemeScope : Scope<ThemeScope>
{
}


