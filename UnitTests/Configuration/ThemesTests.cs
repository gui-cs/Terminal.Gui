using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Terminal.Gui.Configuration {
	public class ThemesTests {
		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter ()
				}
		};

		[Fact, AutoInitShutdown]
		public void AllThemesPresent()
		{
			ConfigurationManager.Config.Themes.ThemeDefinitions.Clear ();
			Assert.Empty (ConfigurationManager.Config.Themes.ThemeDefinitions);
			ConfigurationManager.LoadConfigurationFromLibraryResource ();
			Assert.True (ConfigurationManager.Config.Themes.ThemeDefinitions.ContainsKey ("Default"));
			Assert.True (ConfigurationManager.Config.Themes.ThemeDefinitions.ContainsKey ("Dark"));
			Assert.True (ConfigurationManager.Config.Themes.ThemeDefinitions.ContainsKey ("Light"));
		}

		[Fact, AutoInitShutdown]
		public void GetHardCodedDefaults_ShouldSetProperties ()
		{
			var themes = new Themes ();
			themes.GetHardCodedDefaults ();
			Assert.NotNull (themes.ThemeDefinitions);
			Assert.Equal ("Default", themes.SelectedTheme);
			Assert.NotEmpty (themes.ThemeDefinitions);
		}

		[Fact, AutoInitShutdown]
		public void Apply_ShouldApplyProperties ()
		{
			var themes = new Themes ();
			themes.GetHardCodedDefaults ();
			themes.Apply ();
			Assert.Equal ("Default", themes.SelectedTheme);
		}

		[Fact, AutoInitShutdown]
		public void CopyUpdatedProperitesFrom_ShouldCopyChangedProperties ()
		{
			var themes = new Themes ();
			var updatedThemes = new Themes ();
			updatedThemes.SelectedTheme = "Custom";
			var theme = new Theme ();
			theme.ColorSchemes = new Dictionary<string, ColorScheme> {
				{ "TopLevel", new ColorScheme () }
			};
			updatedThemes.ThemeDefinitions.Add ("Custom", theme);

			themes.CopyUpdatedProperitesFrom (updatedThemes);
			Assert.Equal ("Custom", themes.SelectedTheme);
			Assert.NotNull (themes.ThemeDefinitions);
			Assert.NotEmpty (themes.ThemeDefinitions);
			Assert.True (themes.ThemeDefinitions.ContainsKey ("Custom"));
		}

		[Fact, AutoInitShutdown]
		public void CopyUpdatedProperitesFrom_ShouldNotCopyUnchangedProperties ()
		{
			var themes = new Themes ();
			Assert.Empty (themes.ThemeDefinitions);
			themes.ThemeDefinitions.Add ("Default", new Theme ());
			themes.ThemeDefinitions ["Default"].ColorSchemes = new Dictionary<string, ColorScheme> () {
					{
						"TopLevel",
						new ColorScheme () {
							Normal = new Attribute (Color.Blue, Color.White)
						}
					},
					{
						"Error",
						new ColorScheme () {
							Normal = new Attribute (Color.Gray, Color.DarkGray)
					}
				}
			};
			
			themes.ThemeDefinitions.Add ("Custom", new Theme ());
			themes.SelectedTheme = "Custom";
			themes.ThemeDefinitions ["Custom"].ColorSchemes = new Dictionary<string, ColorScheme> () {
					{
						"TopLevel",
						new ColorScheme () {
							Normal = new Attribute (Color.Blue, Color.White)
						}
					},
					{
						"Error",
						new ColorScheme () {
							Normal = new Attribute (Color.Gray, Color.DarkGray)
					}
				}
			};

			var updatedThemes = new Themes ();
			var theme = new Theme ();
			theme.ColorSchemes = new Dictionary<string, ColorScheme> {
				{
					"TopLevel",
					new ColorScheme () {
						HotNormal = new Attribute (Color.Red, Color.BrightRed)
					}
				},
				{
					"Base",
					new ColorScheme () {
						HotNormal = new Attribute (Color.Brown, Color.BrightYellow)
					}
				}
			};
			updatedThemes.ThemeDefinitions.Add ("Custom", theme);

			themes.CopyUpdatedProperitesFrom (updatedThemes);
			
			Assert.Equal ("Custom", themes.SelectedTheme);
			Assert.NotEmpty (themes.ThemeDefinitions);

			Assert.True (themes.ThemeDefinitions ["Default"].ColorSchemes.ContainsKey ("Error"));
			Assert.True (themes.ThemeDefinitions ["Custom"].ColorSchemes.ContainsKey ("TopLevel"));

			// Prove Default was not changed
			Assert.True (themes.ThemeDefinitions.ContainsKey ("Default"));
			Assert.True (themes.ThemeDefinitions ["Default"].ColorSchemes.ContainsKey ("TopLevel"));
			Assert.Equal (Color.Blue, themes.ThemeDefinitions ["Default"].ColorSchemes ["TopLevel"].Normal.Foreground);
			Assert.Equal (Color.DarkGray, themes.ThemeDefinitions ["Default"].ColorSchemes ["Error"].Normal.Background);

			// Prove Custom properties that weren't suposed to be changed weren't changed
			Assert.Equal (Color.Gray, themes.ThemeDefinitions ["Custom"].ColorSchemes ["Error"].Normal.Foreground);
			Assert.Equal (Color.DarkGray, themes.ThemeDefinitions ["Custom"].ColorSchemes ["Error"].Normal.Background);

			// Prove Custom was changed (where it should have been)
			Assert.Equal (Color.Red, themes.ThemeDefinitions ["Custom"].ColorSchemes ["TopLevel"].HotNormal.Foreground);
			Assert.Equal (Color.BrightRed, themes.ThemeDefinitions ["Custom"].ColorSchemes ["TopLevel"].HotNormal.Background);
			
			// Prove Custom has new stuff
			Assert.True (themes.ThemeDefinitions ["Custom"].ColorSchemes.ContainsKey ("Base"));
			Assert.Equal (Color.Brown, themes.ThemeDefinitions ["Custom"].ColorSchemes ["Base"].HotNormal.Foreground);
			Assert.Equal (Color.BrightYellow, themes.ThemeDefinitions ["Custom"].ColorSchemes ["Base"].HotNormal.Background);

		}

		[Fact, AutoInitShutdown]
		public void TestRoundTrip ()
		{
			var theme = new Themes ();
			var serialized = JsonSerializer.Serialize<Themes> (theme, _jsonOptions);
			var deserialized = JsonSerializer.Deserialize<Themes> (serialized, _jsonOptions);

			Assert.Equal (deserialized.SelectedTheme, theme.SelectedTheme);
			Assert.Equal (deserialized.ThemeDefinitions, theme.ThemeDefinitions);
		}
	}
}