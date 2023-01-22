using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using Terminal.Gui.Configuration;
using Xunit;
using static System.Formats.Asn1.AsnWriter;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests {
	public class ConfigurationMangerTests {

		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter (),
				}
		};

		//[Fact ()]
		//public void LoadFromJsonTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void ToJsonTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void UpdateConfigurationTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void UpdateConfigurationFromFileTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void SaveHardCodedDefaultsTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadGlobalFromLibraryResourceTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadGlobalFromAppDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadGlobalFromHomeDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadAppFromAppResourcesTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadAppFromAppDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadAppFromHomeDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		[Fact]
		public void TestConfigProperties ()
		{
			Assert.NotEmpty (ConfigProperties);
			// test that all ConfigProperites have our attribute
			Assert.All (ConfigProperties, item => Assert.NotEmpty (item.Value.PropertyInfo.CustomAttributes.Where (a => a.AttributeType == typeof (SerializableConfigurationProperty))));
			Assert.Empty (ConfigProperties.Where (cp => cp.Value.PropertyInfo.GetCustomAttribute (typeof (SerializableConfigurationProperty)) == null));

			// Application is a static class
			PropertyInfo pi = typeof (Application).GetProperty ("UseSystemConsole");
			Assert.Equal (pi, ConfigProperties ["Application.UseSystemConsole"].PropertyInfo);

			// FrameView is not a static class
			pi = typeof (FrameView).GetProperty ("DefaultBorderStyle");
			Assert.Equal (pi, ConfigProperties ["FrameView.DefaultBorderStyle"].PropertyInfo);

		}

		[Fact]
		public void TestConfigPropertyOmitClassName ()
		{
			// Color.ColorShemes is serialzied as "ColorSchemes", not "Colors.ColorSchemes"
			PropertyInfo pi = typeof (Colors).GetProperty ("ColorSchemes");
			var scp = ((SerializableConfigurationProperty)pi.GetCustomAttribute (typeof (SerializableConfigurationProperty)));
			Assert.True (scp.Scope == SerializableConfigurationProperty.Scopes.Theme);
			Assert.True (scp.OmitClassName);
			Assert.Equal (pi, ConfigProperties ["ColorSchemes"].PropertyInfo);

		}

		/// <summary>
		/// Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.
		/// </summary>
		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerSaveHardCodedDefaults ()
		{
			ConfigurationManager.SaveHardCodedDefaults ("config.json");
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerToJson ()
		{
			var configuration = new ConfigRoot ();
			configuration.GetAllHardCodedDefaults ();
			var json = ConfigurationManager.ToJson (configuration);

			var readConfig = ConfigurationManager.LoadFromJson (json);

			//Assert.Equal (Colors.Base.Normal, readConfig.ColorSchemes ["Base"].Normal);
		}

		[Fact,AutoInitShutdown]
		public void Prototype ()
		{
			var configuration = new ConfigRoot ();
			configuration.GetAllHardCodedDefaults ();
			ConfigurationManager.ConfigProperties ["Application.HeightAsBuffer"].PropertyValue = true;
			var json = ConfigurationManager.ToJson (configuration);

			ConfigurationManager.ConfigProperties ["Application.HeightAsBuffer"].PropertyValue = false;
			Assert.False ((bool)ConfigurationManager.ConfigProperties ["Application.HeightAsBuffer"].PropertyValue);
			var readConfig = ConfigurationManager.LoadFromJson (json);
			Assert.True ((bool)ConfigurationManager.ConfigProperties ["Application.HeightAsBuffer"].PropertyValue);

			//Assert.Equal (Colors.Base, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Base"]);
			//Assert.Equal (Colors.TopLevel, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["TopLevel"]);
			//Assert.Equal (Colors.Error, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Error"]);
			//Assert.Equal (Colors.Dialog, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Dialog"]);
			//Assert.Equal (Colors.Menu, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Menu"]);

			//Assert.Equal (Colors.Base.Normal, readConfig.ColorSchemes ["Base"].Normal);
		}
#if false
		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerInitDriver ()
		{
			var configuration = new ConfigRoot ();
			configuration.GetAllHardCodedDefaults ();

			// Change Base
			var json = ConfigurationManager.ToJson (configuration);

			var readConfig = ConfigurationManager.LoadFromJson (json);
			Assert.Equal (Colors.Base, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Base"]);
			Assert.Equal (Colors.TopLevel, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["TopLevel"]);
			Assert.Equal (Colors.Error, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Error"]);
			Assert.Equal (Colors.Dialog, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Dialog"]);
			Assert.Equal (Colors.Menu, readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Menu"]);

			Colors.Base = readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Base"];
			Colors.TopLevel = readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["TopLevel"];
			Colors.Error = readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Error"];
			Colors.Dialog = readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Dialog"];
			Colors.Menu = readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Menu"];

			Assert.Equal (readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Base"], Colors.Base);
			Assert.Equal (readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["TopLevel"], Colors.TopLevel);
			Assert.Equal (readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Error"], Colors.Error);
			Assert.Equal (readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Dialog"], Colors.Dialog);
			Assert.Equal (readConfig.Themes.ThemeDefinitions [readConfig.Themes.SelectedTheme].ColorSchemes ["Menu"], Colors.Menu);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerLoadsFromJson ()
		{
			// Arrange
			string json = @"
			{
			  ""$schema"": ""https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"",
			  ""Settings"": {
			    ""QuitKey"": {
			      ""Key"": ""Q"",
			      ""Modifiers"": [
				""Ctrl""
			      ]
			    },
			    ""AlternateForwardKey"": {
			      ""Key"": ""PageDown"",
			      ""Modifiers"": [
				""Ctrl""
			      ]
			    },
			    ""AlternateBackwardKey"": {
			      ""Key"": ""PageUp"",
			      ""Modifiers"": [
				""Ctrl""
			      ]
			    },
			    ""UseSystemConsole"": false,
			    ""IsMouseDisabled"": false,
			    ""HeightAsBuffer"": false
			  },
			  ""Themes"": {
			    ""ThemeDefinitions"": [
			      {
				""Default"": {
				  ""ColorSchemes"": [
				    {
				      ""TopLevel"": {
					""Normal"": {
					  ""Foreground"": ""BrightGreen"",
					  ""Background"": ""Black""
					},
					""Focus"": {
					  ""Foreground"": ""White"",
					  ""Background"": ""Cyan""
					},
					""HotNormal"": {
					  ""Foreground"": ""Brown"",
					  ""Background"": ""Black""
					},
					""HotFocus"": {
					  ""Foreground"": ""Blue"",
					  ""Background"": ""Cyan""
					},
					""Disabled"": {
					  ""Foreground"": ""DarkGray"",
					  ""Background"": ""Black""
					}
				      }
				    },
				    {
				      ""Base"": {
					""Normal"": {
					  ""Foreground"": ""White"",
					  ""Background"": ""Blue""
					},
					""Focus"": {
					  ""Foreground"": ""Black"",
					  ""Background"": ""Gray""
					},
					""HotNormal"": {
					  ""Foreground"": ""BrightCyan"",
					  ""Background"": ""Blue""
					},
					""HotFocus"": {
					  ""Foreground"": ""BrightBlue"",
					  ""Background"": ""Gray""
					},
					""Disabled"": {
					  ""Foreground"": ""DarkGray"",
					  ""Background"": ""Blue""
					}
				      }
				    },
				    {
				      ""Dialog"": {
					""Normal"": {
					  ""Foreground"": ""Black"",
					  ""Background"": ""Gray""
					},
					""Focus"": {
					  ""Foreground"": ""White"",
					  ""Background"": ""DarkGray""
					},
					""HotNormal"": {
					  ""Foreground"": ""Blue"",
					  ""Background"": ""Gray""
					},
					""HotFocus"": {
					  ""Foreground"": ""BrightYellow"",
					  ""Background"": ""DarkGray""
					},
					""Disabled"": {
					  ""Foreground"": ""Gray"",
					  ""Background"": ""DarkGray""
					}
				      }
				    },
				    {
				      ""Menu"": {
					""Normal"": {
					  ""Foreground"": ""White"",
					  ""Background"": ""DarkGray""
					},
					""Focus"": {
					  ""Foreground"": ""White"",
					  ""Background"": ""Black""
					},
					""HotNormal"": {
					  ""Foreground"": ""BrightYellow"",
					  ""Background"": ""DarkGray""
					},
					""HotFocus"": {
					  ""Foreground"": ""BrightYellow"",
					  ""Background"": ""Black""
					},
					""Disabled"": {
					  ""Foreground"": ""Gray"",
					  ""Background"": ""DarkGray""
					}
				      }
				    },
				    {
				      ""Error"": {
					""Normal"": {
					  ""Foreground"": ""Red"",
					  ""Background"": ""White""
					},
					""Focus"": {
					  ""Foreground"": ""Black"",
					  ""Background"": ""BrightRed""
					},
					""HotNormal"": {
					  ""Foreground"": ""Black"",
					  ""Background"": ""White""
					},
					""HotFocus"": {
					  ""Foreground"": ""White"",
					  ""Background"": ""BrightRed""
					},
					""Disabled"": {
					  ""Foreground"": ""DarkGray"",
					  ""Background"": ""White""
					}
				      }
				    }
				  ]
				}
			      }
			    ],
			    ""SelectedTheme"": ""Default""
			  }
			}			
			";

			var configuration = ConfigurationManager.LoadFromJson (json);

			Assert.Equal (Key.Q | Key.CtrlMask, ConfigurationManager.ConfigProperties ["Application.QuitKey"].PropertyValue);

			Assert.Equal ("Default", configuration.Themes.SelectedTheme);
			Assert.Equal (Color.White, configuration.Themes.ThemeDefinitions [configuration.Themes.SelectedTheme].ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, configuration.Themes.ThemeDefinitions [configuration.Themes.SelectedTheme].ColorSchemes ["Base"].Normal.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerLoadInvalidJsonAsserts ()
		{
			// "yellow" is not a color
			string json = @"
			{
				""Themes"" : {
					""ThemeDefinitions"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""yellow"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
					]
				}
			}";

			JsonException jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.LoadFromJson (json));
			Assert.Equal ("Invalid color string: 'yellow'", jsonException.Message);

			// AbNormal is not a ColorScheme attribute
			json = @"
			{
				""Themes"" : {
					""ThemeDefinitions"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""AbNormal"": {
										""foreground"": ""green"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
					]
				}
			}";

			jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.LoadFromJson (json));
			Assert.Equal ("Unrecognized property name: AbNormal", jsonException.Message);

			// Modify hotNormal background only 
			json = @"
			{
				""Themes"" : {
					""ThemeDefinitions"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""background"": ""cyan""
									}
								}
							}
							]
						}
					}
					]
				}
			}";

			jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.LoadFromJson (json));
			Assert.Equal ("Both Foreground and Background colors must be provided.", jsonException.Message);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerAllHardCodedDefaults ()
		{
			ConfigurationManager.Config.GetAllHardCodedDefaults ();
			
			// Apply default styles
			ConfigurationManager.Config.ApplyAll ();

			Assert.Equal (Color.White, Colors.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].Normal.Background);

			Assert.Equal (Color.Black, Colors.ColorSchemes ["Base"].Focus.Foreground);
			Assert.Equal (Color.Gray, Colors.ColorSchemes ["Base"].Focus.Background);

			Assert.Equal (Color.BrightCyan, Colors.ColorSchemes ["Base"].HotNormal.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].HotNormal.Background);

			Assert.Equal (Color.BrightBlue, Colors.ColorSchemes ["Base"].HotFocus.Foreground);
			Assert.Equal (Color.Gray, Colors.ColorSchemes ["Base"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, Colors.ColorSchemes ["Base"].Disabled.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].Disabled.Background);

			Assert.Equal (Color.BrightGreen, Colors.ColorSchemes ["TopLevel"].Normal.Foreground);
			Assert.Equal (Color.Black, Colors.ColorSchemes ["TopLevel"].Normal.Background);

			Assert.Equal (Color.White, Colors.ColorSchemes ["TopLevel"].Focus.Foreground);
			Assert.Equal (Color.Cyan, Colors.ColorSchemes ["TopLevel"].Focus.Background);

			Assert.Equal (Color.Brown, Colors.ColorSchemes ["TopLevel"].HotNormal.Foreground);
			Assert.Equal (Color.Black, Colors.ColorSchemes ["TopLevel"].HotNormal.Background);

			Assert.Equal (Color.Blue, Colors.ColorSchemes ["TopLevel"].HotFocus.Foreground);
			Assert.Equal (Color.Cyan, Colors.ColorSchemes ["TopLevel"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, Colors.ColorSchemes ["TopLevel"].Disabled.Foreground);
			Assert.Equal (Color.Black, Colors.ColorSchemes ["TopLevel"].Disabled.Background);
		}

		[Fact,AutoInitShutdown]
		public void LoadConfigurationFromAllSources_ShouldLoadSettingsFromAllSources ()
		{
			//var _configFilename = "config.json";
			//// Arrange
			//// Create a mock of the configuration files in all sources
			//// Home directory
			//string homeDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".tui");
			//if (!Directory.Exists (homeDir)) {
			//	Directory.CreateDirectory (homeDir);
			//}
			//string globalConfigFile = Path.Combine (homeDir, _configFilename);
			//string appSpecificConfigFile = Path.Combine (homeDir, "appname.config.json");
			//File.WriteAllText (globalConfigFile, "{\"Settings\": {\"TestSetting\":\"Global\"}}");
			//File.WriteAllText (appSpecificConfigFile, "{\"Settings\": {\"TestSetting\":\"AppSpecific\"}}");

			//// App directory
			//string appDir = Directory.GetCurrentDirectory ();
			//string appDirGlobalConfigFile = Path.Combine (appDir, _configFilename);
			//string appDirAppSpecificConfigFile = Path.Combine (appDir, "appname.config.json");
			//File.WriteAllText (appDirGlobalConfigFile, "{\"Settings\": {\"TestSetting\":\"GlobalAppDir\"}}");
			//File.WriteAllText (appDirAppSpecificConfigFile, "{\"Settings\": {\"TestSetting\":\"AppSpecificAppDir\"}}");

			//// App resources
			//// ...

			//// Act
			//ConfigurationManager.LoadConfigurationFromAllSources ();

			//// Assert
			//// Check that the settings from the highest precedence source are loaded
			//Assert.Equal ("AppSpecific", ConfigurationManager.Config.Settings.TestSetting);
		}
#endif 
	}
}

