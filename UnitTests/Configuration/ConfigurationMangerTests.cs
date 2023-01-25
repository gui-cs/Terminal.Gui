using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using Terminal.Gui.Configuration;
using Xunit;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests {
	public class ConfigurationMangerTests {

		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter (),
				}
		};

		[Fact ()]
		public void DeepMemberwiseCopyTest ()
		{
			// Value types
			var stringDest = "Destination";
			var stringSrc = "Source";
			var stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			stringDest = "Destination";
			stringSrc = "Destination";
			stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			stringDest = "Destination";
			stringSrc = null;
			stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			stringDest = "Destination";
			stringSrc = string.Empty;
			stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			var boolDest = true;
			var boolSrc = false;
			var boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			boolDest = false;
			boolSrc = true;
			boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			boolDest = true;
			boolSrc = true;
			boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			boolDest = false;
			boolSrc = false;
			boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			// Structs
			var attrDest = new Attribute (1);
			var attrSrc = new Attribute (2);
			var attrCopy = DeepMemberwiseCopy (attrSrc, attrDest);
			Assert.Equal (attrSrc, attrCopy);

			// Classes
			var colorschemeDest = new ColorScheme () { Disabled = new Attribute (1) };
			var colorschemeSrc = new ColorScheme () { Disabled = new Attribute (2) };
			var colorschemeCopy = DeepMemberwiseCopy (colorschemeSrc, colorschemeDest);
			Assert.Equal (colorschemeSrc, colorschemeCopy);

			// Dictionaries
			var dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) } };
			var dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (2) } };
			var dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (dictSrc, dictCopy);

			dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) } };
			dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (2) }, { "Normal", new Attribute (3) } };
			dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (dictSrc, dictCopy);

			// src adds an item
			dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) } };
			dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (2) }, { "Normal", new Attribute (3) } };
			dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (2, dictCopy.Count);
			Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
			Assert.Equal (dictSrc ["Normal"], dictCopy ["Normal"]);

			// src updates only one item
			dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) }, { "Normal", new Attribute (2) } };
			dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (3) } };
			dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (2, dictCopy.Count);
			Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
			Assert.Equal (dictDest ["Normal"], dictCopy ["Normal"]);


		}

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
			Assert.NotEmpty (ConfigurationManager.Settings);
			// test that all ConfigProperites have our attribute
			Assert.All (ConfigurationManager.Settings, item => Assert.NotEmpty (item.Value.PropertyInfo.CustomAttributes.Where (a => a.AttributeType == typeof (SerializableConfigurationProperty))));
			Assert.Empty (ConfigurationManager.Settings.Where (cp => cp.Value.PropertyInfo.GetCustomAttribute (typeof (SerializableConfigurationProperty)) == null));

			// Application is a static class
			PropertyInfo pi = typeof (Application).GetProperty ("UseSystemConsole");
			Assert.Equal (pi, ConfigurationManager.Settings ["Application.UseSystemConsole"].PropertyInfo);

			// FrameView is not a static class and DefaultBorderStyle is Scope.Scheme
			pi = typeof (FrameView).GetProperty ("DefaultBorderStyle");
			Assert.False (ConfigurationManager.Settings.ContainsKey ("FrameView.DefaultBorderStyle"));
			Assert.True (ThemeManager.Themes ["Default"].Properties.ContainsKey ("FrameView.DefaultBorderStyle"));
		}

		[Fact]
		public void TestConfigPropertyOmitClassName ()
		{
			// Color.ColorShemes is serialzied as "ColorSchemes", not "Colors.ColorSchemes"
			PropertyInfo pi = typeof (Colors).GetProperty ("ColorSchemes");
			var scp = ((SerializableConfigurationProperty)pi.GetCustomAttribute (typeof (SerializableConfigurationProperty)));
			Assert.True (scp.Scope == typeof (ThemeManager.ThemeScope));
			Assert.True (scp.OmitClassName);
			Assert.Equal (pi, ThemeManager.Themes ["Default"].Properties ["ColorSchemes"].PropertyInfo);

		}

		/// <summary>
		/// Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.
		/// </summary>
		[Fact]
		public void TestConfigurationManagerSaveHardCodedDefaults ()
		{
			ConfigurationManager.Locations = ConfigLocations.LibraryResources;

			Application.Init (new FakeDriver ());

			// Get the hard coded settings
			ConfigurationManager.GetHardCodedDefaults ();

			// Serialize to a JSON string
			string json = ConfigurationManager.ToJson ();

			// Write the JSON string to the file specified by filePath
			File.WriteAllText ("config.json", json);


			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerToJson ()
		{
			ConfigurationManager.GetHardCodedDefaults ();
			var json = ConfigurationManager.ToJson ();

			ConfigurationManager.LoadFromJson (json);

			//Assert.Equal (Colors.Base.Normal, readConfig.ColorSchemes ["Base"].Normal);
		}

		[Fact]
		public void TestValueChanges ()
		{
			ConfigurationManager.Locations = ConfigLocations.LibraryResources;

			Application.Init (new FakeDriver ());

			ConfigurationManager.GetHardCodedDefaults ();
			ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue = true;
			Assert.True ((bool)ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue);
			var json = ConfigurationManager.ToJson ();

			ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue = false;
			Assert.False ((bool)ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue);
			ConfigurationManager.LoadFromJson (json);
			Assert.True ((bool)ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue);

			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerInitDriver ()
		{
			ConfigurationManager.GetHardCodedDefaults ();

			// Change Base
			var json = ConfigurationManager.ToJson ();

			ConfigurationManager.LoadFromJson (json);

			var colorSchemes = ((Dictionary<string, ColorScheme>)ThemeManager.Themes [ThemeManager.Theme].Properties ["ColorSchemes"].PropertyValue);
			Assert.Equal (Colors.Base, colorSchemes ["Base"]);
			Assert.Equal (Colors.TopLevel, colorSchemes ["TopLevel"]);
			Assert.Equal (Colors.Error, colorSchemes ["Error"]);
			Assert.Equal (Colors.Dialog, colorSchemes ["Dialog"]);
			Assert.Equal (Colors.Menu, colorSchemes ["Menu"]);

			Colors.Base = colorSchemes ["Base"];
			Colors.TopLevel = colorSchemes ["TopLevel"];
			Colors.Error = colorSchemes ["Error"];
			Colors.Dialog = colorSchemes ["Dialog"];
			Colors.Menu = colorSchemes ["Menu"];

			Assert.Equal (colorSchemes ["Base"], Colors.Base);
			Assert.Equal (colorSchemes ["TopLevel"], Colors.TopLevel);
			Assert.Equal (colorSchemes ["Error"], Colors.Error);
			Assert.Equal (colorSchemes ["Dialog"], Colors.Dialog);
			Assert.Equal (colorSchemes ["Menu"], Colors.Menu);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerLoadsFromJson ()
		{
			// Arrange
			string json = @"
{
  ""$schema"": ""https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"",
  ""Application.QuitKey"": {
    ""Key"": ""Q"",
    ""Modifiers"": [
      ""Ctrl""
    ]
  },
  ""Theme"": ""Default"",
  ""Themes"": [
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
        ],
        ""Dialog.DefaultBorderStyle"": ""Single"",
        ""Dialog.DefaultButtonAlignment"": ""Center"",
        ""Dialog.DefaultEffect3D"": true,
        ""FrameView.DefaultBorderStyle"": ""Single"",
        ""Window.DefaultBorderStyle"": ""Single""
      }
    }
  ]
}					
			";

			ConfigurationManager.LoadFromJson (json);

			Assert.Equal (Key.Q | Key.CtrlMask, ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);

			Assert.Equal ("Default", ThemeManager.Theme);

			var colorSchemes = ((Dictionary<string, ColorScheme>)ThemeManager.Themes [ThemeManager.Theme].Properties ["ColorSchemes"].PropertyValue);
			Assert.Equal (Color.White, colorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, colorSchemes ["Base"].Normal.Background);
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
			ConfigurationManager.GetHardCodedDefaults ();
			
			// Apply default styles
			ConfigurationManager.Apply ();

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
			//ConfigurationManager.Locations = ConfigurationManager.ConfigLocation.All;
			//ConfigurationManager.Load ();

			//// Assert
			//// Check that the settings from the highest precedence source are loaded
			//Assert.Equal ("AppSpecific", ConfigurationManager.Config.Settings.TestSetting);
		}
	}
}

