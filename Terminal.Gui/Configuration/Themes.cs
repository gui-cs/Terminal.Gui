using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;

#nullable enable

namespace Terminal.Gui.Configuration {

	public static partial class ConfigurationManager {
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
			/// <summary>
			/// Event arguments for the <see cref="ThemeManager"/> events.
			/// </summary>
			public class EventArgs : System.EventArgs {
				/// <summary>
				/// The name of the new active theme..
				/// </summary>
				public string NewTheme { get; set; } = string.Empty;

				/// <summary>
				/// Initializes a new instance of <see cref="EventArgs"/>
				/// </summary>
				public EventArgs (string newTheme)
				{
					NewTheme = newTheme;
				}
			}
		}

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
		public class ThemeManager : IDictionary<string, ThemeScope> {
			private static ThemeManager _instance = new ThemeManager ();
			static ThemeManager () { } // Make sure it's truly lazy
			private ThemeManager () { } // Prevent instantiation outside

			/// <summary>
			/// Class is a singleton...
			/// </summary>
			public static ThemeManager Instance { get { return _instance; } }

			private static string theme = "Default";
			private static Dictionary<string, ThemeScope>? themes;

			/// <summary>
			/// The currently selected theme. 
			/// </summary>
			[JsonInclude, SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true), JsonPropertyName ("Theme")]
			internal static string ThemeProperty {
				get => theme;
				set {
					var oldTheme = theme;
					theme = value;
					if (oldTheme != theme && themes != null && themes.ContainsKey (theme)) {
						Instance.OnThemeChanged (oldTheme);
						Instance.Apply ();
					}
				}
			}

			/// <summary>
			/// 
			/// </summary>
			[JsonIgnore]
			public string Theme {
				get => ThemeProperty;
				set {
					ThemeProperty = value;
				}
			}

			/// <summary>
			/// Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.
			/// </summary>
			public void OnThemeChanged (string theme)
			{
				Debug.WriteLine ($"Themes.OnThemeChanged({theme}) -> {Theme}");
				ThemeChanged?.Invoke (new ThemeScope.EventArgs (theme));
			}

			/// <summary>
			/// Event fired he selected theme has changed.
			/// application.
			/// </summary>
			public event Action<ThemeScope.EventArgs>? ThemeChanged;


			/// <summary>
			/// Holds the <see cref="ThemeScope"/> definitions. 
			/// </summary>
			[JsonInclude, JsonConverter (typeof (DictionaryConverter<ThemeScope>))]
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
			public void Apply ()
			{
				if (Themes.ContainsKey (ThemeProperty)) {
					foreach (var prop in Themes [ThemeProperty].Where (t => t.Value != null && t.Value.PropertyValue != null)) {
						if (prop.Key == "Dialog.DefaultBorder") {
							Debug.WriteLine (prop.Value);
							if (((Border) prop.Value.PropertyValue).BorderStyle == BorderStyle.Double) {
								throw new Exception ("Fuck");
							}
						}
						prop.Value.PropertyInfo?.SetValue (null, DeepMemberwiseCopy (prop.Value.PropertyValue, prop.Value.PropertyInfo?.GetValue (null)));
					}
					Application.Driver?.InitalizeColorSchemes ();
					OnApplied ();
				}
			}

			/// <summary>
			/// Called when an updated theme has been applied to the  
			/// application. Fires the <see cref="Applied"/> event.
			/// </summary>
			public void OnApplied ()
			{
				Debug.WriteLine ($"Themes.OnApplied() -> {Theme}");
				Applied?.Invoke (new ThemeScope.EventArgs (Theme));
			}

			/// <summary>
			/// Event fired when an updated theme has been applied to the  
			/// application.
			/// </summary>
			public event Action<ThemeScope.EventArgs>? Applied;

			internal void GetHardCodedDefaults ()
			{
				Debug.WriteLine ($"Themes.GetHardCodedDefaults()");
				Themes?.Clear ();
				var theme = new ThemeScope ();
				foreach (var p in theme.Where (cp => cp.Value.PropertyInfo != null)) {
					theme [p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
				}
				Themes = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase) { { "Default", theme } };
			}

			#region IDictionary
			public ICollection<string> Keys => ((IDictionary<string, ThemeScope>)Themes).Keys;
			/// <inheritdoc/>
			public ICollection<ThemeScope> Values => ((IDictionary<string, ThemeScope>)Themes).Values;
			/// <inheritdoc/>
			public int Count => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Count;
			/// <inheritdoc/>
			public bool IsReadOnly => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).IsReadOnly;
			/// <inheritdoc/>
			public ThemeScope this [string key] { get => ((IDictionary<string, ThemeScope>)Themes) [key]; set => ((IDictionary<string, ThemeScope>)Themes) [key] = value; }
			/// <inheritdoc/>
			public void Add (string key, ThemeScope value)
			{
				((IDictionary<string, ThemeScope>)Themes).Add (key, value);
			}
			/// <inheritdoc/>
			public bool ContainsKey (string key)
			{
				return ((IDictionary<string, ThemeScope>)Themes).ContainsKey (key);
			}
			/// <inheritdoc/>
			public bool Remove (string key)
			{
				return ((IDictionary<string, ThemeScope>)Themes).Remove (key);
			}
			/// <inheritdoc/>
			public bool TryGetValue (string key, out ThemeScope value)
			{
				return ((IDictionary<string, ThemeScope>)Themes).TryGetValue (key, out value!);
			}
			/// <inheritdoc/>
			public void Add (KeyValuePair<string, ThemeScope> item)
			{
				((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Add (item);
			}
			/// <inheritdoc/>
			public void Clear ()
			{
				((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Clear ();
			}
			/// <inheritdoc/>
			public bool Contains (KeyValuePair<string, ThemeScope> item)
			{
				return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Contains (item);
			}
			/// <inheritdoc/>
			public void CopyTo (KeyValuePair<string, ThemeScope> [] array, int arrayIndex)
			{
				((ICollection<KeyValuePair<string, ThemeScope>>)Themes).CopyTo (array, arrayIndex);
			}
			/// <inheritdoc/>
			public bool Remove (KeyValuePair<string, ThemeScope> item)
			{
				return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Remove (item);
			}
			/// <inheritdoc/>
			public IEnumerator<KeyValuePair<string, ThemeScope>> GetEnumerator ()
			{
				return ((IEnumerable<KeyValuePair<string, ThemeScope>>)Themes).GetEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return ((IEnumerable)Themes).GetEnumerator ();
			}
			#endregion
		}
	}
}