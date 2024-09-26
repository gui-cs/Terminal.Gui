using System.Reflection;
using System.Text.Json;
using Xunit.Abstractions;
using static Terminal.Gui.ConfigurationManager;
#pragma warning disable IDE1006

namespace Terminal.Gui.ConfigurationTests;

public class ConfigurationManagerTests
{
    private readonly ITestOutputHelper _output;

    public ConfigurationManagerTests (ITestOutputHelper output)
    {
        _output = output;
    }

    public static readonly JsonSerializerOptions _jsonOptions = new ()
    {
        Converters = { new AttributeJsonConverter (), new ColorJsonConverter () }
    };

    [Fact]
    public void Apply_FiresApplied ()
    {
        Reset ();
        Applied += ConfigurationManager_Applied;
        var fired = false;

        void ConfigurationManager_Applied (object sender, ConfigurationManagerEventArgs obj)
        {
            fired = true;

            // assert
            Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
            Assert.Equal (KeyCode.F, Application.NextTabGroupKey.KeyCode);
            Assert.Equal (KeyCode.B, Application.PrevTabGroupKey.KeyCode);
        }

        // act
        Settings! ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;

        Apply ();

        // assert
        Assert.True (fired);

        Applied -= ConfigurationManager_Applied;
        Reset ();
    }

    [Fact]
    public void DeepMemberWiseCopyTest ()
    {
        // Value types
        var stringDest = "Destination";
        var stringSrc = "Source";
        object stringCopy = DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = "Destination";
        stringCopy = DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = null;
        stringCopy = DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = string.Empty;
        stringCopy = DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        var boolDest = true;
        var boolSrc = false;
        object boolCopy = DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = false;
        boolSrc = true;
        boolCopy = DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = true;
        boolSrc = true;
        boolCopy = DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = false;
        boolSrc = false;
        boolCopy = DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        // Structs
        var attrDest = new Attribute (Color.Black);
        var attrSrc = new Attribute (Color.White);
        object attrCopy = DeepMemberWiseCopy (attrSrc, attrDest);
        Assert.Equal (attrSrc, attrCopy);

        // Classes
        var colorschemeDest = new ColorScheme { Disabled = new Attribute (Color.Black) };
        var colorschemeSrc = new ColorScheme { Disabled = new Attribute (Color.White) };
        object colorschemeCopy = DeepMemberWiseCopy (colorschemeSrc, colorschemeDest);
        Assert.Equal (colorschemeSrc, colorschemeCopy);

        // Dictionaries
        Dictionary<string, Attribute> dictDest = new () { { "Disabled", new Attribute (Color.Black) } };
        Dictionary<string, Attribute> dictSrc = new () { { "Disabled", new Attribute (Color.White) } };
        Dictionary<string, Attribute> dictCopy = (Dictionary<string, Attribute>)DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (dictSrc, dictCopy);

        dictDest = new Dictionary<string, Attribute> { { "Disabled", new Attribute (Color.Black) } };

        dictSrc = new Dictionary<string, Attribute>
        {
            { "Disabled", new Attribute (Color.White) }, { "Normal", new Attribute (Color.Blue) }
        };
        dictCopy = (Dictionary<string, Attribute>)DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (dictSrc, dictCopy);

        // src adds an item
        dictDest = new Dictionary<string, Attribute> { { "Disabled", new Attribute (Color.Black) } };

        dictSrc = new Dictionary<string, Attribute>
        {
            { "Disabled", new Attribute (Color.White) }, { "Normal", new Attribute (Color.Blue) }
        };
        dictCopy = (Dictionary<string, Attribute>)DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (2, dictCopy!.Count);
        Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
        Assert.Equal (dictSrc ["Normal"], dictCopy ["Normal"]);

        // src updates only one item
        dictDest = new Dictionary<string, Attribute>
        {
            { "Disabled", new Attribute (Color.Black) }, { "Normal", new Attribute (Color.White) }
        };
        dictSrc = new Dictionary<string, Attribute> { { "Disabled", new Attribute (Color.White) } };
        dictCopy = (Dictionary<string, Attribute>)DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (2, dictCopy!.Count);
        Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
        Assert.Equal (dictDest ["Normal"], dictCopy ["Normal"]);
    }

    [Fact]
    public void Load_FiresUpdated ()
    {
        ConfigLocations savedLocations = Locations;
        Locations = ConfigLocations.All;
        Reset ();

        Settings! ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;

        Updated += ConfigurationManager_Updated;
        var fired = false;

        void ConfigurationManager_Updated (object sender, ConfigurationManagerEventArgs obj)
        {
            fired = true;

            // assert
            Assert.Equal (Key.Esc, (((Key)Settings! ["Application.QuitKey"].PropertyValue)!).KeyCode);

            Assert.Equal (
                          KeyCode.F6,
                          (((Key)Settings ["Application.NextTabGroupKey"].PropertyValue)!).KeyCode
                         );

            Assert.Equal (
                          KeyCode.F6 | KeyCode.ShiftMask,
                          (((Key)Settings ["Application.PrevTabGroupKey"].PropertyValue)!).KeyCode
                         );
        }

        Load (true);

        // assert
        Assert.True (fired);

        Updated -= ConfigurationManager_Updated;
        Reset ();
        Locations = savedLocations;
    }

    [Fact]
    [AutoInitShutdown]
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
    public void Reset_and_ResetLoadWithLibraryResourcesOnly_are_same ()
    {
        Locations = ConfigLocations.DefaultOnly;

        // arrange
        Reset ();
        Settings! ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;
        Settings.Apply ();

        // assert apply worked
        Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
        Assert.Equal (KeyCode.F, Application.NextTabGroupKey.KeyCode);
        Assert.Equal (KeyCode.B, Application.PrevTabGroupKey.KeyCode);

        //act
        Reset ();

        // assert
        Assert.NotEmpty (Themes!);
        Assert.Equal ("Default", Themes.Theme);
        Assert.Equal (Key.Esc, Application.QuitKey);
        Assert.Equal (Key.F6, Application.NextTabGroupKey);
        Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);

        // arrange
        Settings ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;
        Settings.Apply ();

        Locations = ConfigLocations.DefaultOnly;

        // act
        Reset ();
        Load ();

        // assert
        Assert.NotEmpty (Themes);
        Assert.Equal ("Default", Themes.Theme);
        Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
        Assert.Equal (Key.F6, Application.NextTabGroupKey);
        Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
        Reset ();
    }

    [Fact]
    public void Reset_Resets ()
    {
        Locations = ConfigLocations.DefaultOnly;
        Reset ();
        Assert.NotEmpty (Themes!);
        Assert.Equal ("Default", Themes.Theme);
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

    /// <summary>Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.</summary>
    /// <remarks>
    ///     IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties are all
    ///     static and thus can be overwritten by other tests.
    /// </remarks>
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
    public void TestConfigProperties ()
    {
        Locations = ConfigLocations.All;
        Reset ();

        Assert.NotEmpty (Settings!);

        // test that all ConfigProperties have our attribute
        Assert.All (
                    Settings,
                    item => Assert.NotEmpty (
                                             item.Value.PropertyInfo!.CustomAttributes.Where (
                                                                                              a => a.AttributeType == typeof (SerializableConfigurationProperty)
                                                                                             )
                                            )
                   );

#pragma warning disable xUnit2029
        Assert.Empty (
                      Settings.Where (
                                      cp => cp.Value.PropertyInfo!.GetCustomAttribute (
                                                                                       typeof (SerializableConfigurationProperty)
                                                                                      )
                                            == null
                                     )
                     );
#pragma warning restore xUnit2029

        // Application is a static class
        PropertyInfo pi = typeof (Application).GetProperty ("QuitKey");
        Assert.Equal (pi, Settings ["Application.QuitKey"].PropertyInfo);

        // FrameView is not a static class and DefaultBorderStyle is Scope.Scheme
        pi = typeof (FrameView).GetProperty ("DefaultBorderStyle");
        Assert.False (Settings.ContainsKey ("FrameView.DefaultBorderStyle"));
        Assert.True (Themes! ["Default"].ContainsKey ("FrameView.DefaultBorderStyle"));
        Assert.Equal (pi, Themes! ["Default"] ["FrameView.DefaultBorderStyle"].PropertyInfo);
    }

    [Fact]
    public void TestConfigPropertyOmitClassName ()
    {
        ConfigLocations savedLocations = Locations;
        Locations = ConfigLocations.All;

        // Color.ColorSchemes is serialized as "ColorSchemes", not "Colors.ColorSchemes"
        PropertyInfo pi = typeof (Colors).GetProperty ("ColorSchemes");
        var scp = (SerializableConfigurationProperty)pi!.GetCustomAttribute (typeof (SerializableConfigurationProperty));
        Assert.True (scp!.Scope == typeof (ThemeScope));
        Assert.True (scp.OmitClassName);

        Reset ();
        Assert.Equal (pi, Themes! ["Default"] ["ColorSchemes"].PropertyInfo);

        Locations = savedLocations;
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.DefaultOnly)]
    public void TestConfigurationManagerInitDriver ()
    {
        Assert.Equal ("Default", Themes!.Theme);

        Assert.Equal (new Color (Color.White), Colors.ColorSchemes ["Base"]!.Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), Colors.ColorSchemes ["Base"].Normal.Background);

        // Change Base
        Stream json = ToStream ();

        Settings!.Update (json, "TestConfigurationManagerInitDriver");

        Dictionary<string, ColorScheme> colorSchemes =
            (Dictionary<string, ColorScheme>)Themes [Themes.Theme] ["ColorSchemes"].PropertyValue;
        Assert.Equal (Colors.ColorSchemes ["Base"], colorSchemes! ["Base"]);
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
    [AutoInitShutdown (configLocation: ConfigLocations.None)]
    public void TestConfigurationManagerInitDriver_NoLocations () { }

    [Fact]
    public void TestConfigurationManagerInvalidJsonLogs ()
    {
        Application.Init (new FakeDriver ());

        ThrowOnJsonErrors = false;

        // "brown" is not a color
        var json = @"
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

        Settings!.Update (json, "test");

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

        Assert.NotEqual (0, _jsonErrors.Length);

        Application.Shutdown ();

        ThrowOnJsonErrors = false;
    }

    [Fact]
    [AutoInitShutdown]
    public void TestConfigurationManagerInvalidJsonThrows ()
    {
        ThrowOnJsonErrors = true;

        // "yellow" is not a color
        var json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""brownish"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				]
			}";

        var jsonException = Assert.Throws<JsonException> (() => Settings!.Update (json, "test"));
        Assert.Equal ("Unexpected color name: brownish.", jsonException.Message);

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

        jsonException = Assert.Throws<JsonException> (() => Settings!.Update (json, "test"));
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

        jsonException = Assert.Throws<JsonException> (() => Settings!.Update (json, "test"));
        Assert.Equal ("Both Foreground and Background colors must be provided.", jsonException.Message);

        // Unknown property
        json = @"
			{
				""Unknown"" : ""Not known""
			}";

        jsonException = Assert.Throws<JsonException> (() => Settings!.Update (json, "test"));
        Assert.StartsWith ("Unknown property", jsonException.Message);

        Assert.Equal (0, _jsonErrors.Length);

        ThrowOnJsonErrors = false;
    }

    [Fact]
    [AutoInitShutdown]
    public void TestConfigurationManagerToJson ()
    {
        Reset ();
        GetHardCodedDefaults ();
        Stream stream = ToStream ();

        Settings!.Update (stream, "TestConfigurationManagerToJson");
    }

    [Fact]
    public void TestConfigurationManagerUpdateFromJson ()
    {
        ConfigLocations savedLocations = Locations;
        Locations = ConfigLocations.All;

        // Arrange
        var json = @"
{
  ""$schema"": ""https://gui-cs.github.io/Terminal.GuiV2Docs/schemas/tui-config-schema.json"",
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
        ]
      }
    }
  ]
}					
			";

        Reset ();
        ThrowOnJsonErrors = true;

        Settings!.Update (json, "TestConfigurationManagerUpdateFromJson");

        Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
        Assert.Equal (KeyCode.Z | KeyCode.AltMask, ((Key)Settings ["Application.QuitKey"].PropertyValue)!.KeyCode);

        Assert.Equal ("Default", Themes!.Theme);

        Assert.Equal (new Color (Color.White), Colors.ColorSchemes ["Base"]!.Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), Colors.ColorSchemes ["Base"].Normal.Background);

        Dictionary<string, ColorScheme> colorSchemes =
            (Dictionary<string, ColorScheme>)Themes.First ().Value ["ColorSchemes"].PropertyValue;
        Assert.Equal (new Color (Color.White), colorSchemes! ["Base"].Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), colorSchemes ["Base"].Normal.Background);

        // Now re-apply
        Apply ();

        Assert.Equal (KeyCode.Z | KeyCode.AltMask, Application.QuitKey.KeyCode);
        Assert.Equal ("Default", Themes.Theme);

        Assert.Equal (new Color (Color.White), Colors.ColorSchemes ["Base"].Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), Colors.ColorSchemes ["Base"].Normal.Background);
        Reset ();

        Locations = savedLocations;
    }

    [Fact]
    public void UseWithoutResetDoesNotThrow ()
    {
        Initialize ();
        _ = Settings;
    }
}
