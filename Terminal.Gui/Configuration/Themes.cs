using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;

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
	public class Theme : Config {
		private Dictionary<string, ColorScheme> colorSchemes;

		/// <summary>
		/// The ColorScheme for the Theme
		/// </summary>
		[JsonConverter (typeof (DictionaryConverter<ColorScheme>))]
		public Dictionary<string, ColorScheme> ColorSchemes {
			get => colorSchemes;
			set {
				if (colorSchemes == null) {
					colorSchemes = new Dictionary<string, ColorScheme> ();
				}
				// TODO: If a Theme doesn't specify the built-in ColorSchemes (e.g. Base)
				// the behavior is indeterminte. The commented code below is one solution; 
				// it makes the ne Theme "inherit" from Default (assuming default) exists.
				//
				if (ThemeManager.Themes != null && ThemeManager.Themes.ContainsKey ("Default")) {
					foreach (var s in ThemeManager.Themes ["Default"].ColorSchemes) {
						colorSchemes [s.Key] = s.Value;
					}
				}
				//
				// Another solution is to have a Theme that doesn't specify all core Schemes 
				// "inherit" from whatever ConsoleDriver sets (which is currenlty Black/White for all Schemes).
				//
				//foreach (var s in Colors.ColorSchemes) {
				//	colorSchemes [s.Key] = s.Value;
				//}

				foreach (var newScheme in value) {
					colorSchemes [newScheme.Key] = newScheme.Value;
				}
			}
		}

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
				// If a driver is set, call CreateColors to ensure the attributes in the loaded
				// scheme are set.
				Application.Driver?.InitalizeColorSchemes ();
			}

			if (DefaultWindowBorderStyle.HasValue) {
				Window.DefaultBorderStyle = DefaultWindowBorderStyle.Value;
			}

			if (DefaultFrameViewBorderStyle.HasValue) {
				FrameView.DefaultBorderStyle = DefaultFrameViewBorderStyle.Value;
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
	/// 	"Themes": [
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
	/// </code></example> 
	public static class ThemeManager {

		private static string theme = string.Empty;
		private static Dictionary<string, Theme> themes;// = new Dictionary<string, Theme> ();

		/// <summary>
		/// The currently selected theme. 
		/// </summary>
		[SerializableConfigurationProperty (Scope = SerializableConfigurationProperty.Scopes.Settings, OmitClassName = true)]
		public static string Theme {
			get => theme;
			set {
				theme = value;
			}
		}

		/// <summary>
		/// The <see cref="Configuration.Theme"/> definitions. 
		/// </summary>
		[JsonInclude]
		[JsonConverter (typeof (DictionaryConverter<Theme>))]
		[SerializableConfigurationProperty (Scope = SerializableConfigurationProperty.Scopes.Settings, OmitClassName = true)]
		public static Dictionary<string, Theme> Themes {
			get => themes;
			set {
				if (themes == null) {
					themes = value;
				} else {
					foreach (var theme in value.Where (t => t.Value != null)) {
						themes [theme.Key].CopyPropertiesFrom (theme.Value);
					}
				}
			}
		}

		public static void Apply ()
		{
			if (Themes != null && Themes.ContainsKey (Theme)) {
				// Reset Colors.ColorSchemes
				Colors.Init ();
				Themes [Theme].Apply ();
			}

			if (Themes != null && Themes.ContainsKey (Theme)) {
				Themes [Theme].Apply ();
			}

		}
	}
}
