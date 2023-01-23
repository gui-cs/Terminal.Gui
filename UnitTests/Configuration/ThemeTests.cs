using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests {
#if false       
	public class ThemeTests {
		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter ()
				}
		};

		[Fact,AutoInitShutdown]
		public void TestApply_UpdatesColors ()
		{
			// Arrange
			var theme = new Theme ();
			var colorScheme = new ColorScheme { Normal = new Attribute (Color.Red, Color.Green) };
			theme.ColorSchemes ["test"] = colorScheme;
			Colors.ColorSchemes ["test"] = new ColorScheme ();

			// Act
			theme.Apply ();

			// Assert
			var updatedScheme = Colors.ColorSchemes ["test"];
			Assert.Equal (Color.Red, updatedScheme.Normal.Foreground);
			Assert.Equal (Color.Green, updatedScheme.Normal.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestApply ()
		{
			var theme = new Theme ();
			var colorScheme = new ColorScheme ();
			colorScheme.Normal = new Attribute (Color.Red, Color.Green);
			theme.ColorSchemes.Add ("Test", colorScheme);
			theme.Apply ();
			Assert.Equal (Colors.ColorSchemes ["Test"].Normal, colorScheme.Normal);
		}

		[Fact, AutoInitShutdown]
		public void TestCopyUpdatedProperties ()
		{
			var theme = new Theme ();
			var colorScheme = new ColorScheme ();
			colorScheme.Normal = new Attribute (Color.Red, Color.Green);
			theme.ColorSchemes.Add ("Test", colorScheme);

			var updatedTheme = new Theme ();
			var updatedColorScheme = new ColorScheme ();
			updatedColorScheme.Normal = new Attribute (Color.Blue, Color.BrightBlue);
			updatedColorScheme.Focus = new Attribute (Color.Cyan, Color.BrightCyan);
			updatedTheme.ColorSchemes.Add ("Test", updatedColorScheme);

			theme.CopyUpdatedProperitesFrom (updatedTheme);

			Assert.Equal (theme.ColorSchemes ["Test"].Normal, updatedColorScheme.Normal);
			Assert.Equal (theme.ColorSchemes ["Test"].Focus, updatedColorScheme.Focus);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerApplyPartialColorScheme ()
		{
			ConfigurationManager.Config.GetAllHardCodedDefaults ();

			// Apply default theme
			ConfigurationManager.Config.Themes.Apply ();

			// Prove Base is defaults (White, Blue)
			Assert.Equal (Color.White, Colors.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].Normal.Background);

			// Prove Error's Normal is default (Red, White)
			Assert.Equal (Color.Red, Colors.ColorSchemes ["Error"].Normal.Foreground);
			Assert.Equal (Color.White, Colors.ColorSchemes ["Error"].Normal.Background);

			// Modify Error's Normal
			string json = @"
			{
				""Themes"" : {
					""ThemeDefinitions"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""Error"": {
									""Normal"": {
										""foreground"": ""Gray"",
										""background"": ""DarkGray""
									}
								}
							}
							]
						}
					}
					]
				}
			}";

			ConfigurationManager.UpdateConfiguration (json);
			ConfigurationManager.Config.Themes.Apply ();

			// Prove Base didn't change from defaults (White, Blue)
			Assert.Equal (Color.White, Colors.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].Normal.Background);

			// Prove Error's Normal changed
			Assert.Equal (Color.Gray, Colors.ColorSchemes ["Error"].Normal.Foreground);
			Assert.Equal (Color.DarkGray, Colors.ColorSchemes ["Error"].Normal.Background);

		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerMultiThemes ()
		{
			var configuration = new ConfigRoot ();
			configuration.GetAllHardCodedDefaults ();
			var newTheme = new Theme ();
			newTheme.ColorSchemes.Add ("NewScheme", new ColorScheme () { Normal = Attribute.Make (Color.White, Color.Red) });

			configuration.Themes.ThemeDefinitions.Add ("NewTheme", newTheme);
			configuration.Themes.SelectedTheme = "NewTheme";
			var json = ConfigurationManager.ToJson (configuration);

			var readConfig = ConfigurationManager.LoadFromJson (json);
			File.WriteAllText ("config-newtheme.json", json);

			ConfigurationManager.Apply ();

			Assert.Equal ("NewTheme", configuration.Themes.SelectedTheme);
			Assert.Equal (Color.White, configuration.Themes.ThemeDefinitions [configuration.Themes.SelectedTheme].ColorSchemes ["NewScheme"].Normal.Foreground);
			Assert.Equal (Color.Red, configuration.Themes.ThemeDefinitions [configuration.Themes.SelectedTheme].ColorSchemes ["NewScheme"].Normal.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestColorSchemeRoundTrip ()
		{
			var serializedColors = JsonSerializer.Serialize (Colors.Base, _jsonOptions);
			var deserializedColors = JsonSerializer.Deserialize<ColorScheme> (serializedColors, _jsonOptions);

			Assert.Equal (Colors.Base.Normal, deserializedColors.Normal);
			Assert.Equal (Colors.Base.Focus, deserializedColors.Focus);
			Assert.Equal (Colors.Base.HotNormal, deserializedColors.HotNormal);
			Assert.Equal (Colors.Base.HotFocus, deserializedColors.HotFocus);
			Assert.Equal (Colors.Base.Disabled, deserializedColors.Disabled);
		}
	}
#endif
}