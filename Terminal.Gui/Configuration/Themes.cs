using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;

#nullable enable

namespace Terminal.Gui.Configuration {
	/// <summary>
	/// Contains a dictionary of the <see cref="ThemeManager.Theme"/>s for a Terminal.Gui application.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Theme is a collection of settings that are named. The default theme is named "Default".
	/// </para>
	/// <para>
	/// The <see cref="ThemeManager.Theme"/> property is used to detemrine the currently active theme. 
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

		/// <summary>
		/// The root object for a Theme. A Theme is a set of settings that are applied to the running <see cref="Application"/>
		/// as a group.
		/// </summary>
		/// <remarks>
		/// <para>
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
		[JsonConverter (typeof (ConfigScopeConverter<ThemeScope>))]
		public class ThemeScope : Scope {
		}

		private static string theme = "Default";
		private static Dictionary<string, ThemeScope>? themes = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase) { { "Default", new ThemeScope () } };

		/// <summary>
		/// The currently selected theme. 
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof(SettingsScope), OmitClassName = true)]
		public static string Theme {
			get => theme;
			set {
				var oldTheme = theme;
				theme = value;
				if (oldTheme != theme && themes != null && themes.ContainsKey(theme)) {
					OnThemeChanged (oldTheme);
					Apply ();
				}
			}
		}

		/// <summary>
		/// Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.
		/// </summary>
		public static void OnThemeChanged (string theme)
		{
			ThemeChanged?.Invoke (new ThemeScopeEventArgs (theme));
		}

		/// <summary>
		/// Event fired he selected theme has changed.
		/// application.
		/// </summary>
		public static event Action<ThemeScopeEventArgs>? ThemeChanged;

		/// <summary>
		/// Event arguments for the <see cref="ThemeManager"/> events.
		/// </summary>
		public class ThemeScopeEventArgs {
			/// <summary>
			/// The name of the new active theme..
			/// </summary>
			public string NewTheme { get; set; } = string.Empty;

			/// <summary>
			/// Initializes a new instance of <see cref="ThemeScopeEventArgs"/>
			/// </summary>
			public ThemeScopeEventArgs (string newTheme)
			{
				NewTheme = newTheme;
			}
		}

		/// <summary>
		/// Holds the <see cref="ThemeScope"/> definitions. 
		/// </summary>
		[JsonInclude]
		[JsonConverter (typeof (DictionaryConverter<ThemeScope>))]
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
		public static Dictionary<string, ThemeScope> Themes {
			get => themes!;
			set {
				if (themes == null || value == null) {
					themes = value;
				} else {
					themes = (Dictionary<string, ThemeScope>)DeepMemberwiseCopy (value, themes);
				}
			}
		}

		/// <summary>
		/// Applies the theme settings in the current <see cref="ThemeScope"/> (determiend by <see cref="Theme"/>
		/// to the running <see cref="Application"/> instance.
		/// </summary>
		/// <remarks>
		/// This only applies <see cref="ThemeScope"/> properites. Use <see cref="ConfigurationManager.Apply"/> to apply the 
		/// global settings.</remarks>
		public static void Apply ()
		{
			if (Themes.ContainsKey (Theme)) {
				foreach (var theme in Themes [Theme].Properties.Where (t => t.Value != null && t.Value.PropertyValue != null)) {
					theme.Value.PropertyInfo?.SetValue (null, DeepMemberwiseCopy (theme.Value.PropertyValue, theme.Value.PropertyInfo?.GetValue (null)));
				}
				Application.Driver?.InitalizeColorSchemes ();
				OnApplied ();
			}
		}

		/// <summary>
		/// Called when an updated theme has been applied to the  
		/// application. Fires the <see cref="Applied"/> event.
		/// </summary>
		public static void OnApplied ()
		{
			Applied?.Invoke (new ThemeScopeEventArgs (Theme));
		}

		/// <summary>
		/// Event fired when an updated theme has been applied to the  
		/// application.
		/// </summary>
		public static event Action<ThemeScopeEventArgs>? Applied;

		internal static void GetHardCodedDefaults ()
		{
			Themes?.Clear ();
			var theme = new ThemeScope ();
			foreach (var p in theme.Properties.Where (cp => cp.Value.PropertyInfo != null)) {
				theme.Properties [p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
			}
			Themes = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase) { { "Default", theme} };
		}
	}
}
