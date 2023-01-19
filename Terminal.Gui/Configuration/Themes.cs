using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration {

	/// <summary>
	/// A Theme is a set of settings.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Theme is a collection of settings that are named. 
	/// </para>
	/// </remarks>
	/// <example><code>
	/// 	"Default": {
	/// 		"ColorSchemes": [
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
	/// 			"Foreground": "Brown",
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
	/// </code></example> 
	public class Theme : Config<Theme> {
		/// <summary>
		/// The ColorScheme for the Theme
		/// </summary>
		[JsonConverter (typeof (DictionaryConverter<ColorScheme>))]
		public Dictionary<string, ColorScheme> ColorSchemes { get; set; } = new Dictionary<string, ColorScheme> ();

		/// <summary>
		/// 
		/// </summary>
		/// 
		[JsonConverter (typeof (JsonStringEnumConverter))]
		public BorderStyle? DefaultWindowBorderStyle { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// 
		[JsonConverter (typeof (JsonStringEnumConverter))]
		public BorderStyle? DefaultFrameViewBorderStyle { get; set; }

		/// <inheritdoc/>
		public override void Apply ()
		{
			if (ColorSchemes != null) {
				// ColorSchemes
				foreach (var scheme in ColorSchemes) {
					Colors.ColorSchemes [scheme.Key] = scheme.Value;
				}
			}

			if (DefaultWindowBorderStyle.HasValue) {
				Window.DefaultBorderStyle = DefaultWindowBorderStyle.Value;
			}

			if (DefaultFrameViewBorderStyle.HasValue) {
				FrameView.DefaultBorderStyle = DefaultFrameViewBorderStyle.Value;
			}

		}

		/// <inheritdoc/>
		public override void GetHardCodedDefaults ()
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Performs a sparse copy of a <see cref="Theme"/> (only copies ColorSchemes that are valid/set in the source;
		/// leveraging the fact that <see cref="Attribute.Make(Color, Color)"/> only copies Colors that are not
		/// <see cref="Color.Invalid"/>).
		/// </summary>
		/// <param name="theme"></param>
		public override void CopyUpdatedProperitesFrom (Theme theme)
		{
			if (theme == null) {
				return;
			}
			foreach (var updatedScheme in theme.ColorSchemes) {
				ColorSchemes [updatedScheme.Key] = updatedScheme.Value;
			}

			if (theme.DefaultWindowBorderStyle.HasValue) {
				DefaultWindowBorderStyle = theme.DefaultWindowBorderStyle.Value;
			}

			if (theme.DefaultFrameViewBorderStyle.HasValue) {
				DefaultWindowBorderStyle = theme.DefaultFrameViewBorderStyle.Value;
			}

		}
	}

	/// <summary>
	/// Defines the Themes for a Terminal.Gui application.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Theme is a collection of settings that are named. The default theme is named "Default".
	/// </para>
	/// <para>
	/// The <c>SelectedTheme</c> property is used to detemrine the currently active theme. 
	/// </para>
	/// </remarks>
	/// <example><code>
	/// "Themes": {
	/// 	"SelectedTheme" : "Default",
	/// 	"ThemeDefinitions": [
	/// 	{
	/// 		"Default": {
	/// 			"ColorSchemes": [
	/// 			{
	/// 			"TopLevel": {
	/// 			"Normal": {
	/// 				"Foreground": "BrightGreen",
	/// 				"Background": "Black"
	/// 			},
	/// 			"Focus": {
	/// 			"Foreground": "White",
	/// 				"Background": "Cyan"
	/// 
	/// 			},
	/// 			"HotNormal": {
	/// 				"Foreground": "Brown",
	/// 				"Background": "Black"
	/// 
	/// 			},
	/// 			"HotFocus": {
	/// 				"Foreground": "Blue",
	/// 				"Background": "Cyan"
	/// 			},
	/// 			"Disabled": {
	/// 				"Foreground": "DarkGray",
	/// 				"Background": "Black"
	/// 
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code></example> 
	public class Themes : Config<Themes> {
		/// <summary>
		/// The currenlty selected theme. 
		/// </summary>
		[JsonInclude]
		public string SelectedTheme = string.Empty;

		/// <summary>
		/// The <see cref="Theme"/> definitions. 
		/// </summary>
		[JsonInclude]
		[JsonConverter (typeof (DictionaryConverter<Theme>))]
		public Dictionary<string, Theme> ThemeDefinitions { get; set; } = new Dictionary<string, Theme> ();

		/// <inheritdoc/>
		public override void GetHardCodedDefaults ()
		{
			SelectedTheme = "Default";
			var defaultTheme = new Theme () { };
			foreach (var scheme in Colors.ColorSchemes) {
				defaultTheme.ColorSchemes.Add (scheme.Key, scheme.Value);
			}
			defaultTheme.DefaultFrameViewBorderStyle = FrameView.DefaultBorderStyle;
			defaultTheme.DefaultWindowBorderStyle = Window.DefaultBorderStyle;
			ThemeDefinitions.Add (SelectedTheme, defaultTheme);
		}

		/// <inheritdoc/>
		public override void Apply ()
		{
			if (ThemeDefinitions != null && ThemeDefinitions.ContainsKey (SelectedTheme)) {
				ThemeDefinitions [SelectedTheme].Apply ();
			}

		}

		/// <inheritdoc/>
		public override void CopyUpdatedProperitesFrom (Themes updatedThemes)
		{
			if (ThemeDefinitions != null && updatedThemes != null) {
				foreach (var theme in updatedThemes.ThemeDefinitions) {
					if (ThemeDefinitions.ContainsKey (theme.Key)) {
						ThemeDefinitions [theme.Key].CopyUpdatedProperitesFrom (theme.Value);
					} else {
						ThemeDefinitions.Add (theme.Key, theme.Value);
					}
				}
			}

			if (!string.IsNullOrEmpty (updatedThemes.SelectedTheme)) {
				SelectedTheme = updatedThemes.SelectedTheme;
			}
		}

	}
}
