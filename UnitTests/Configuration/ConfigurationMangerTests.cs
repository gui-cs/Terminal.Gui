using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests; 

public class ConfigurationManagerTests {
	public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions {
		Converters = {
			new AttributeJsonConverter (),
			new ColorJsonConverter ()
		}
	};

	[Fact ()]
	public void DeepMemberwiseCopyTest ()
	{
		// Value types
		string stringDest = "Destination";
		string stringSrc = "Source";
		object stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
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

		bool boolDest = true;
		bool boolSrc = false;
		object boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
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
		var attrDest = new Attribute (Color.Black);
		var attrSrc = new Attribute (Color.White);
		object attrCopy = DeepMemberwiseCopy (attrSrc, attrDest);
		Assert.Equal (attrSrc, attrCopy);

		// Classes
		var colorschemeDest = new ColorScheme () { Disabled = new Attribute (Color.Black) };
		var colorschemeSrc = new ColorScheme () { Disabled = new Attribute (Color.White) };
		object colorschemeCopy = DeepMemberwiseCopy (colorschemeSrc, colorschemeDest);
		Assert.Equal (colorschemeSrc, colorschemeCopy);

		// Dictionaries
		var dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.Black) } };
		var dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.White) } };
		var dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
		Assert.Equal (dictSrc, dictCopy);

		dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.Black) } };
		dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.White) }, { "Normal", new Attribute (Color.Blue) } };
		dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
		Assert.Equal (dictSrc, dictCopy);

		// src adds an item
		dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.Black) } };
		dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.White) }, { "Normal", new Attribute (Color.Blue) } };
		dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
		Assert.Equal (2, dictCopy.Count);
		Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
		Assert.Equal (dictSrc ["Normal"], dictCopy ["Normal"]);

		// src updates only one item
		dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.Black) }, { "Normal", new Attribute (Color.White) } };
		dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (Color.White) } };
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

	/// <summary>
	/// Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties
	/// are all static and thus can be overwritten by other tests.</remarks>
	[Fact]
	public void SaveDefaults ()
	{
		Initialize ();
		Reset ();

		// Get the hard coded settings
		GetHardCodedDefaults ();

		// Serialize to a JSON string
		string json = ToJson ();

		// Write the JSON string to the file 
		File.WriteAllText ("config.json", json);
	}

	[Fact]
	public void UseWithoutResetAsserts ()
	{
		Initialize ();
		Assert.Throws<InvalidOperationException> (() => _ = Settings);
	}

	[Fact]
	public void Reset_Resets ()
	{
		Locations = ConfigLocations.DefaultOnly;
		Reset ();
		Assert.NotEmpty (Themes);
		Assert.Equal ("Default", Themes.Theme);
	}

	[Fact]
	public void Reset_and_ResetLoadWithLibraryResourcesOnly_are_same ()
	{
		Locations = ConfigLocations.DefaultOnly;
		// arrange
		Reset ();
		Settings ["Application.QuitKey"].PropertyValue = new Key (KeyCode.Q);
		Settings ["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
		Settings ["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
		Settings ["Application.IsMouseDisabled"].PropertyValue = true;
		Settings.Apply ();

		// assert apply worked
		Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
		Assert.Equal (KeyCode.F, Application.AlternateForwardKey.KeyCode);
		Assert.Equal (KeyCode.B, Application.AlternateBackwardKey.KeyCode);
		Assert.True (Application.IsMouseDisabled);

		//act
		Reset ();

		// assert
		Assert.NotEmpty (Themes);
		Assert.Equal ("Default", Themes.Theme);
		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey.KeyCode);
		Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, Application.AlternateForwardKey.KeyCode);
		Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, Application.AlternateBackwardKey.KeyCode);
		Assert.False (Application.IsMouseDisabled);

		// arrange
		Settings ["Application.QuitKey"].PropertyValue = new Key (KeyCode.Q);
		Settings ["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
		Settings ["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
		Settings ["Application.IsMouseDisabled"].PropertyValue = true;
		Settings.Apply ();

		Locations = ConfigLocations.DefaultOnly;

		// act
		Reset ();
		Load ();

		// assert
		Assert.NotEmpty (Themes);
		Assert.Equal ("Default", Themes.Theme);
		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey.KeyCode);
		Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, Application.AlternateForwardKey.KeyCode);
		Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, Application.AlternateBackwardKey.KeyCode);
		Assert.False (Application.IsMouseDisabled);

	}

	[Fact]
	public void TestConfigProperties ()
	{
		Locations = ConfigLocations.All;
		Reset ();

		Assert.NotEmpty (Settings);
		// test that all ConfigProperites have our attribute
		Assert.All (Settings, item => Assert.NotEmpty (item.Value.PropertyInfo.CustomAttributes.Where (a => a.AttributeType == typeof (SerializableConfigurationProperty))));
		Assert.Empty (Settings.Where (cp => cp.Value.PropertyInfo.GetCustomAttribute (typeof (SerializableConfigurationProperty)) == null));

		// Application is a static class
		var pi = typeof (Application).GetProperty ("QuitKey");
		Assert.Equal (pi, Settings ["Application.QuitKey"].PropertyInfo);

		// FrameView is not a static class and DefaultBorderStyle is Scope.Scheme
		pi = typeof (FrameView).GetProperty ("DefaultBorderStyle");
		Assert.False (Settings.ContainsKey ("FrameView.DefaultBorderStyle"));
		Assert.True (Themes ["Default"].ContainsKey ("FrameView.DefaultBorderStyle"));
	}

	[Fact]
	public void TestConfigPropertyOmitClassName ()
	{
		// Color.ColorShemes is serialzied as "ColorSchemes", not "Colors.ColorSchemes"
		var pi = typeof (Colors).GetProperty ("ColorSchemes");
		var scp = (SerializableConfigurationProperty)pi.GetCustomAttribute (typeof (SerializableConfigurationProperty));
		Assert.True (scp.Scope == typeof (ThemeScope));
		Assert.True (scp.OmitClassName);

		Reset ();
		Assert.Equal (pi, Themes ["Default"] ["ColorSchemes"].PropertyInfo);
	}

	[Fact, AutoInitShutdown]
	public void TestConfigurationManagerToJson ()
	{
		Reset ();
		GetHardCodedDefaults ();
		var stream = ToStream ();

		Settings.Update (stream, "TestConfigurationManagerToJson");
	}

	[Fact, AutoInitShutdown (configLocation: ConfigLocations.None)]
	public void TestConfigurationManagerInitDriver_NoLocations ()
	{


	}

	[Fact, AutoInitShutdown (configLocation: ConfigLocations.DefaultOnly)]
	public void TestConfigurationManagerInitDriver ()
	{
		Assert.Equal ("Default", Themes.Theme);
		Assert.True (Themes.ContainsKey ("Default"));

		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey.KeyCode);

		Assert.Equal (new Color (Color.White), Colors.ColorSchemes ["Base"].Normal.Foreground);
		Assert.Equal (new Color (Color.Blue), Colors.ColorSchemes ["Base"].Normal.Background);

		// Change Base
		var json = ToStream ();

		Settings.Update (json, "TestConfigurationManagerInitDriver");

		var colorSchemes = (Dictionary<string, ColorScheme>)Themes [Themes.Theme] ["ColorSchemes"].PropertyValue;
		Assert.Equal (Colors.ColorSchemes ["Base"], colorSchemes ["Base"]);
		Assert.Equal (Colors.ColorSchemes ["TopLevel"], colorSchemes ["TopLevel"]);
		Assert.Equal (Colors.ColorSchemes ["Error"], colorSchemes ["Error"]);
		Assert.Equal (Colors.ColorSchemes ["Dialog"], colorSchemes ["Dialog"]);
		Assert.Equal (Colors.ColorSchemes ["Menu"], colorSchemes ["Menu"]);

		Colors.ColorSchemes ["Base"] = colorSchemes ["Base"];
		Colors.ColorSchemes ["TopLevel"] = colorSchemes ["TopLevel"];
		Colors.ColorSchemes ["Error"] = colorSchemes ["Error"];
		Colors.ColorSchemes ["Dialog"] = colorSchemes ["Dialog"];
		Colors.ColorSchemes ["Menu"] = colorSchemes ["Menu"];

		Assert.Equal (colorSchemes ["Base"], Colors.ColorSchemes ["Base"]);
		Assert.Equal (colorSchemes ["TopLevel"], Colors.ColorSchemes ["TopLevel"]);
		Assert.Equal (colorSchemes ["Error"], Colors.ColorSchemes ["Error"]);
		Assert.Equal (colorSchemes ["Dialog"], Colors.ColorSchemes ["Dialog"]);
		Assert.Equal (colorSchemes ["Menu"], Colors.ColorSchemes ["Menu"]);
	}

	[Fact]
	public void TestConfigurationManagerUpdateFromJson ()
	{
		// Arrange
		string json = @"
{
  ""$schema"": ""https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"",
  ""Application.QuitKey"": ""Alt-Z"",
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
                ""Foreground"": ""Yellow"",
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
        ""Dialog.DefaultButtonAlignment"": ""Center""
      }
    }
  ]
}					
			";

		Reset ();
		ThrowOnJsonErrors = true;

		Settings.Update (json, "TestConfigurationManagerUpdateFromJson");

		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey.KeyCode);
		Assert.Equal (KeyCode.Z | KeyCode.AltMask, ((Key)Settings ["Application.QuitKey"].PropertyValue).KeyCode);

		Assert.Equal ("Default", Themes.Theme);

		Assert.Equal (new Color (Color.White), Colors.ColorSchemes ["Base"].Normal.Foreground);
		Assert.Equal (new Color (Color.Blue), Colors.ColorSchemes ["Base"].Normal.Background);

		var colorSchemes = (Dictionary<string, ColorScheme>)Themes.First ().Value ["ColorSchemes"].PropertyValue;
		Assert.Equal (new Color (Color.White), colorSchemes ["Base"].Normal.Foreground);
		Assert.Equal (new Color (Color.Blue), colorSchemes ["Base"].Normal.Background);

		// Now re-apply
		Apply ();

		Assert.Equal (KeyCode.Z | KeyCode.AltMask, Application.QuitKey.KeyCode);
		Assert.Equal ("Default", Themes.Theme);

		Assert.Equal (new Color (Color.White), Colors.ColorSchemes ["Base"].Normal.Foreground);
		Assert.Equal (new Color (Color.Blue), Colors.ColorSchemes ["Base"].Normal.Background);
	}

	[Fact, AutoInitShutdown]
	public void TestConfigurationManagerInvalidJsonThrows ()
	{
		ThrowOnJsonErrors = true;
		// "yellow" is not a color
		string json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""brown"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				]
			}";

		var jsonException = Assert.Throws<JsonException> (() => Settings.Update (json, "test"));
		Assert.Equal ("Unexpected color name: brown.", jsonException.Message);

		// AbNormal is not a ColorScheme attribute
		json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""AbNormal"": {
										""foreground"": ""green"",
										""background"": ""black""
									}
								}
							}
							]
						}
					}
				]
			}";

		jsonException = Assert.Throws<JsonException> (() => Settings.Update (json, "test"));
		Assert.Equal ("Unrecognized ColorScheme Attribute name: AbNormal.", jsonException.Message);

		// Modify hotNormal background only 
		json = @"
			{
				""Themes"" : [ 
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
			}";

		jsonException = Assert.Throws<JsonException> (() => Settings.Update (json, "test"));
		Assert.Equal ("Both Foreground and Background colors must be provided.", jsonException.Message);

		// Unknown proeprty
		json = @"
			{
				""Unknown"" : ""Not known""
			}";

		jsonException = Assert.Throws<JsonException> (() => Settings.Update (json, "test"));
		Assert.StartsWith ("Unknown property", jsonException.Message);

		Assert.Equal (0, jsonErrors.Length);

		ThrowOnJsonErrors = false;
	}

	[Fact]
	public void TestConfigurationManagerInvalidJsonLogs ()
	{
		Application.Init (new FakeDriver ());

		ThrowOnJsonErrors = false;
		// "brown" is not a color
		string json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""brown"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				}
			}";

		Settings.Update (json, "test");

		// AbNormal is not a ColorScheme attribute
		json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""AbNormal"": {
										""foreground"": ""green"",
										""background"": ""black""
									}
								}
							}
							]
						}
					}
				}
			}";

		Settings.Update (json, "test");

		// Modify hotNormal background only 
		json = @"
			{
				""Themes"" :  [ 
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
				}
			}";

		Settings.Update (json, "test");

		Settings.Update ("{}}", "test");

		Assert.NotEqual (0, jsonErrors.Length);

		Application.Shutdown ();

		ThrowOnJsonErrors = false;
	}

	[Fact, AutoInitShutdown]
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

	[Fact]
	public void Load_FiresUpdated ()
	{
		Reset ();

		Settings ["Application.QuitKey"].PropertyValue = new Key (KeyCode.Q);
		Settings ["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
		Settings ["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
		Settings ["Application.IsMouseDisabled"].PropertyValue = true;

		Updated += ConfigurationManager_Updated;
		bool fired = false;
		void ConfigurationManager_Updated (object sender, ConfigurationManagerEventArgs obj)
		{
			fired = true;
			// assert
			Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, ((Key)Settings ["Application.QuitKey"].PropertyValue).KeyCode);
			Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, ((Key)Settings ["Application.AlternateForwardKey"].PropertyValue).KeyCode);
			Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, ((Key)Settings ["Application.AlternateBackwardKey"].PropertyValue).KeyCode);
			Assert.False ((bool)Settings ["Application.IsMouseDisabled"].PropertyValue);
		}

		Load (true);

		// assert
		Assert.True (fired);

		Updated -= ConfigurationManager_Updated;
	}

	[Fact]
	public void Apply_FiresApplied ()
	{
		Reset ();
		Applied += ConfigurationManager_Applied;
		bool fired = false;
		void ConfigurationManager_Applied (object sender, ConfigurationManagerEventArgs obj)
		{
			fired = true;
			// assert
			Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
			Assert.Equal (KeyCode.F, Application.AlternateForwardKey.KeyCode);
			Assert.Equal (KeyCode.B, Application.AlternateBackwardKey.KeyCode);
			Assert.True (Application.IsMouseDisabled);
		}

		// act
		Settings ["Application.QuitKey"].PropertyValue = new Key (KeyCode.Q);
		Settings ["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
		Settings ["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
		Settings ["Application.IsMouseDisabled"].PropertyValue = true;

		Apply ();

		// assert
		Assert.True (fired);

		Applied -= ConfigurationManager_Applied;
	}
}