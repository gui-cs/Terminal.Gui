using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests {
	public class ThemeManagerTests {
		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				//new AttributeJsonConverter (),
				//new ColorJsonConverter ()
				}
		};


		[Fact]
		public void ThemeManager_ClassMethodsWork ()
		{
			ConfigurationManager.Reset ();
			Assert.Equal (ConfigurationManager.ThemeManager.Instance, ConfigurationManager.Themes);
			Assert.NotEmpty (ConfigurationManager.ThemeManager.Themes);

			ConfigurationManager.ThemeManager.SelectedTheme = "foo";
			Assert.Equal ("foo", ConfigurationManager.ThemeManager.SelectedTheme);
			ConfigurationManager.ThemeManager.Reset ();
			Assert.Equal (string.Empty, ConfigurationManager.ThemeManager.SelectedTheme);

			Assert.Empty (ConfigurationManager.ThemeManager.Themes);
		}

		[Fact]
		public void AllThemesPresent()
		{
			ConfigurationManager.Reset ();
			Assert.True (ConfigurationManager.Themes.ContainsKey ("Default"));
			Assert.True (ConfigurationManager.Themes.ContainsKey ("Dark"));
			Assert.True (ConfigurationManager.Themes.ContainsKey ("Light"));
		}

		[Fact]
		public void GetHardCodedDefaults_ShouldSetProperties ()
		{
			ConfigurationManager.Reset ();
			ConfigurationManager.GetHardCodedDefaults ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);
		}

		[Fact, AutoInitShutdown]
		public void Apply_ShouldApplyUpdatedProperties ()
		{
			ConfigurationManager.Reset ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal (Dialog.ButtonAlignments.Center, Dialog.DefaultButtonAlignment);

			ConfigurationManager.Themes ["Default"] ["Dialog.DefaultButtonAlignment"].PropertyValue = Dialog.ButtonAlignments.Right;

			// ConfigurationManager.Settings should NOT apply theme settings
			ConfigurationManager.Settings.Apply ();
			Assert.Equal (Dialog.ButtonAlignments.Center, Dialog.DefaultButtonAlignment);

			// ConfigurationManager.Settings should NOT apply theme settings
			ConfigurationManager.Apply ();
			Assert.Equal (Dialog.ButtonAlignments.Center, Dialog.DefaultButtonAlignment);

			ConfigurationManager.Themes.Apply ();
			Assert.Equal (Dialog.ButtonAlignments.Right, Dialog.DefaultButtonAlignment);
		}

		[Fact]
		public void UpdateFrom_ShouldCopyChangedProperties ()
		{

		}

#if false

		[Fact, AutoInitShutdown]
		public void CopyUpdatedProperitesFrom_ShouldNotCopyUnchangedProperties ()
		{
			var themes = new Themes ();
			Assert.Empty (themes.ThemeDefinitions);
			themes.Add ("Default", new Theme ());
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
			
			themes.Add ("Custom", new Theme ());
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
			updatedThemes.Add ("Custom", theme);

			themes.CopyUpdatedProperitesFrom (updatedThemes);
			
			Assert.Equal ("Custom", themes.SelectedTheme);
			Assert.NotEmpty (themes.ThemeDefinitions);

			Assert.True (themes.ThemeDefinitions ["Default"].ColorSchemes.ContainsKey ("Error"));
			Assert.True (themes.ThemeDefinitions ["Custom"].ColorSchemes.ContainsKey ("TopLevel"));

			// Prove Default was not changed
			Assert.True (themes.ContainsKey ("Default"));
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
		
#endif
	}
}