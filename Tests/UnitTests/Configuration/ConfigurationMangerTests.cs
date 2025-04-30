using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Terminal.Gui.Configuration;
using UnitTests;
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
    public void Initialize_Sets_Statics ()
    {
        Assert.Null (_classesWithConfigProps);
        Assert.Null (_allConfigProperties);

        ConfigurationManager.Initialize ();

        Assert.NotNull (_classesWithConfigProps);
        Assert.NotNull (_allConfigProperties);
    }

    [Fact]
    public void Reset_Clears_Statics ()
    {
        UnInitialize ();

        Assert.Null (_classesWithConfigProps);
        Assert.Null (_allConfigProperties);

        Initialize ();

        Assert.NotNull (_classesWithConfigProps);
        Assert.NotNull (_allConfigProperties);

        UnInitialize ();

        Assert.Null (_classesWithConfigProps);
        Assert.Null (_allConfigProperties);
        Assert.Null (_settings);
        Assert.Null (_themeManager);
        Assert.Null (_appSettings);
        Assert.Equal (ConfigLocations.All, Locations);
        //        Assert.Null (RuntimeConfig);
        //Assert.Null (ThrowOnJsonErrors);


    }

    [Fact]
    public void Apply_Raises_Applied ()
    {
        ResetAllSettings ();
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
        ResetAllSettings ();
    }


    [Fact]
    public void Load_Raises_Updated ()
    {
        ThrowOnJsonErrors = true;
        Locations = ConfigLocations.All;
        ResetAllSettings ();
        Assert.Equal (Key.Esc, (((Key)Settings! ["Application.QuitKey"].PropertyValue)!).KeyCode);

        Updated += ConfigurationManager_Updated;
        var fired = false;

        void ConfigurationManager_Updated (object sender, ConfigurationManagerEventArgs obj)
        {
            fired = true;
        }

        // Act
        // Reset to cause load to raise event
        Load (true);

        // assert
        Assert.True (fired);

        Updated -= ConfigurationManager_Updated;

        // clean up
        Locations = ConfigLocations.Default;
        ResetAllSettings ();
    }

    [Fact]
    public void Load_Performance_Check ()
    {
        Locations = ConfigLocations.All;
        ResetAllSettings ();

        // Start stopwatch
        Stopwatch stopwatch = new Stopwatch ();
        stopwatch.Start ();

        // Act
        Load (true);
        Apply ();

        // Stop stopwatch
        stopwatch.Stop ();

        // Assert
        _output.WriteLine ($"Load took {stopwatch.ElapsedMilliseconds} ms");
    }


    [Fact]
    public void Load_Loads_Custom_Json ()
    {
        // arrange
        Locations = ConfigLocations.Runtime | ConfigLocations.Default;
        ResetAllSettings ();
        ThrowOnJsonErrors = true;

        Assert.Equal (Key.Esc, (Key)Settings! ["Application.QuitKey"].PropertyValue);

        // act
        RuntimeConfig = """
                   
                           {
                                 "Application.QuitKey": "Ctrl-Q"
                           }
                   """;
        Load (false);

        // assert
        Assert.Equal (Key.Q.WithCtrl, (Key)Settings ["Application.QuitKey"].PropertyValue);

        // clean up
        Locations = ConfigLocations.Default;
        ResetAllSettings ();
    }

    //[Fact]
    //[AutoInitShutdown]
    //public void LoadConfigurationFromAllSources_ShouldLoadSettingsFromAllSources ()
    //{
    //    //var _configFilename = "config.json";
    //    //// Arrange
    //    //// Create a mock of the configuration files in all sources
    //    //// Home directory
    //    //string homeDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".tui");
    //    //if (!Directory.Exists (homeDir)) {
    //    //	Directory.CreateDirectory (homeDir);
    //    //}
    //    //string globalConfigFile = Path.Combine (homeDir, _configFilename);
    //    //string appSpecificConfigFile = Path.Combine (homeDir, "appname.config.json");
    //    //File.WriteAllText (globalConfigFile, "{\"Settings\": {\"TestSetting\":\"Global\"}}");
    //    //File.WriteAllText (appSpecificConfigFile, "{\"Settings\": {\"TestSetting\":\"AppSpecific\"}}");

    //    //// App directory
    //    //string appDir = Directory.GetCurrentDirectory ();
    //    //string appDirGlobalConfigFile = Path.Combine (appDir, _configFilename);
    //    //string appDirAppSpecificConfigFile = Path.Combine (appDir, "appname.config.json");
    //    //File.WriteAllText (appDirGlobalConfigFile, "{\"Settings\": {\"TestSetting\":\"GlobalAppDir\"}}");
    //    //File.WriteAllText (appDirAppSpecificConfigFile, "{\"Settings\": {\"TestSetting\":\"AppSpecificAppDir\"}}");

    //    //// App resources
    //    //// ...

    //    //// Act
    //    //ConfigurationManager.Locations = ConfigurationManager.ConfigLocation.All;
    //    //ConfigurationManager.Load ();

    //    //// Assert
    //    //// Check that the settings from the highest precedence source are loaded
    //    //Assert.Equal ("AppSpecific", ConfigurationManager.Config.Settings.TestSetting);
    //}


    [Fact]
    public void ResetAllSettings_Raises_Updated ()
    {
        ConfigLocations savedLocations = Locations;
        Locations = ConfigLocations.All;
        ResetAllSettings ();

        Settings! ["Application.QuitKey"].PropertyValue = Key.Q;

        Updated += ConfigurationManager_Updated;
        var fired = false;

        void ConfigurationManager_Updated (object sender, ConfigurationManagerEventArgs obj)
        {
            fired = true;
        }

        // Act
        ResetAllSettings ();

        // assert
        Assert.True (fired);

        Updated -= ConfigurationManager_Updated;
        ResetAllSettings ();
        Locations = savedLocations;
    }


    [Fact]
    public void ResetAllSettings_and_ResetLoadWithLibraryResourcesOnly_are_same ()
    {
        Locations = ConfigLocations.Default;

        // arrange
        ResetAllSettings ();
        Settings! ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;
        Settings.Apply ();

        // assert apply worked
        Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
        Assert.Equal (KeyCode.F, Application.NextTabGroupKey.KeyCode);
        Assert.Equal (KeyCode.B, Application.PrevTabGroupKey.KeyCode);

        //act
        ResetAllSettings ();

        // assert
        Assert.NotEmpty (ConfigurationManager.ThemeManager!);
        Assert.Equal ("Default", ConfigurationManager.ThemeManager.Theme);
        Assert.Equal (Key.Esc, Application.QuitKey);
        Assert.Equal (Key.F6, Application.NextTabGroupKey);
        Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);

        // arrange
        Settings ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;
        Settings.Apply ();

        Locations = ConfigLocations.Default;

        // act
        ResetAllSettings ();
        Load ();

        // assert
        Assert.NotEmpty (ConfigurationManager.ThemeManager);
        Assert.Equal ("Default", ConfigurationManager.ThemeManager.Theme);
        Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
        Assert.Equal (Key.F6, Application.NextTabGroupKey);
        Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
        ResetAllSettings ();
    }

    [Fact]
    public void ResetAllSettings_Resets ()
    {
        Initialize ();

        Locations = ConfigLocations.None;

        // Act
        ResetAllSettings ();

        Assert.NotNull (Settings);
        Assert.NotNull (AppSettings);
        Assert.NotNull (ConfigurationManager.ThemeManager);

        // Default Theme should be "Default"
        Assert.Equal (0, ConfigurationManager.ThemeManager.Keys.Count);
        Assert.Equal ("Default", ConfigurationManager.ThemeManager.Theme);

        //Assert.Equal();
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
        ResetAllSettings ();

        // Get the hard coded settings
        ResetToCurrentValues ();

        // Serialize to a JSON string
        string json = CM.SourcesManager?.ToJson (Settings);

        // Write the JSON string to the file
        File.WriteAllText ("config.json", json);
    }

    [Fact]
    public void TestConfigProperties ()
    {
        Locations = ConfigLocations.All;
        ResetAllSettings ();

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
        Assert.True (ConfigurationManager.ThemeManager! ["Default"].ContainsKey ("FrameView.DefaultBorderStyle"));
        Assert.Equal (pi, ConfigurationManager.ThemeManager! ["Default"] ["FrameView.DefaultBorderStyle"].PropertyInfo);
    }

    [Fact]
    public void TestConfigPropertyOmitClassName ()
    {
        ConfigLocations savedLocations = Locations;
        Locations = ConfigLocations.All;

        // Color.Schemes is serialized as "Schemes", not "Colors.Schemes"
        PropertyInfo pi = typeof (SchemeManager).GetProperty ("Schemes");
        var scp = (SerializableConfigurationProperty)pi!.GetCustomAttribute (typeof (SerializableConfigurationProperty));
        Assert.True (scp!.Scope == typeof (ThemeScope));
        Assert.True (scp.OmitClassName);

        ResetAllSettings ();
        Assert.Equal (pi, ConfigurationManager.ThemeManager! ["Default"] ["Schemes"].PropertyInfo);

        Locations = savedLocations;
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.Default)]
    public void TestConfigurationManagerInitDriver ()
    {
        Assert.Equal ("Default", ConfigurationManager.ThemeManager!.Theme);

        Assert.Equal (new Color (Color.White), SchemeManager.Schemes ["Base"]!.Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), SchemeManager.Schemes ["Base"].Normal.Background);

        // Change Base
        Stream json = CM.SourcesManager?.ToStream(Settings);

        CM.SourcesManager?.Update (Settings, json, "TestConfigurationManagerInitDriver", ConfigLocations.Runtime);

        Dictionary<string, Scheme> schemes =
            (Dictionary<string, Scheme>)ConfigurationManager.ThemeManager [ConfigurationManager.ThemeManager.Theme] ["Schemes"].PropertyValue;
        Assert.Equal (SchemeManager.Schemes ["Base"], schemes! ["Base"]);
        Assert.Equal (SchemeManager.Schemes ["TopLevel"], schemes ["TopLevel"]);
        Assert.Equal (SchemeManager.Schemes ["Error"], schemes ["Error"]);
        Assert.Equal (SchemeManager.Schemes ["Dialog"], schemes ["Dialog"]);
        Assert.Equal (SchemeManager.Schemes ["Menu"], schemes ["Menu"]);

        SchemeManager.Schemes ["Base"] = schemes ["Base"];
        SchemeManager.Schemes ["TopLevel"] = schemes ["TopLevel"];
        SchemeManager.Schemes ["Error"] = schemes ["Error"];
        SchemeManager.Schemes ["Dialog"] = schemes ["Dialog"];
        SchemeManager.Schemes ["Menu"] = schemes ["Menu"];

        Assert.Equal (schemes ["Base"], SchemeManager.Schemes ["Base"]);
        Assert.Equal (schemes ["TopLevel"], SchemeManager.Schemes ["TopLevel"]);
        Assert.Equal (schemes ["Error"], SchemeManager.Schemes ["Error"]);
        Assert.Equal (schemes ["Dialog"], SchemeManager.Schemes ["Dialog"]);
        Assert.Equal (schemes ["Menu"], SchemeManager.Schemes ["Menu"]);
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.None)]
    public void TestConfigurationManagerInitDriver_NoLocations ()
    {
        // TODO: Write this test
    }

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
							""Schemes"": [
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

        CM.SourcesManager?.Update (Settings, json, "test", ConfigLocations.Runtime);

        // AbNormal is not a Scheme attribute
        json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""Schemes"": [
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

        CM.SourcesManager?.Update (Settings, json, "test", ConfigLocations.Runtime);

        // Modify hotNormal background only
        json = @"
			{
				""Themes"" :  [ 
                                        {
						""Default"" : {
							""Schemes"": [
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

        CM.SourcesManager?.Update(Settings, json, "test", ConfigLocations.Runtime);

        CM.SourcesManager?.Update(Settings, "{}}", "test", ConfigLocations.Runtime);

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
							""Schemes"": [
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

        var jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Update(Settings, json, "test", ConfigLocations.Runtime));
        Assert.Equal ("Unexpected color name: brownish.", jsonException.Message);

        // AbNormal is not a Scheme attribute
        json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""Schemes"": [
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

        jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Update(Settings, json, "test", ConfigLocations.Runtime));
        Assert.Equal ("Unrecognized Scheme Attribute name: AbNormal.", jsonException.Message);

        // Modify hotNormal background only
        json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""Schemes"": [
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

        jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Update(Settings, json, "test", ConfigLocations.Runtime));
        Assert.Equal ("Both Foreground and Background colors must be provided.", jsonException.Message);

        // Unknown property
        json = @"
			{
				""Unknown"" : ""Not known""
			}";

        jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Update(Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("Unknown property", jsonException.Message);

        Assert.Equal (0, _jsonErrors.Length);

        ThrowOnJsonErrors = false;
    }

    [Fact]
    [AutoInitShutdown]
    public void TestConfigurationManagerToJson ()
    {
        ResetAllSettings ();
        ResetToCurrentValues ();
        Stream stream = CM.SourcesManager?.ToStream (Settings);

        CM.SourcesManager?.Update(Settings, stream, "TestConfigurationManagerToJson", ConfigLocations.Runtime);
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
        ""Schemes"": [
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

        ResetAllSettings ();
        ThrowOnJsonErrors = true;

        CM.SourcesManager?.Update(Settings, json, "TestConfigurationManagerUpdateFromJson", ConfigLocations.Runtime);

        Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
        Assert.Equal (KeyCode.Z | KeyCode.AltMask, ((Key)Settings ["Application.QuitKey"].PropertyValue)!.KeyCode);

        Assert.Equal ("Default", ConfigurationManager.ThemeManager!.Theme);

        Assert.Equal (new Color (Color.White), SchemeManager.Schemes ["Base"]!.Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), SchemeManager.Schemes ["Base"].Normal.Background);

        Dictionary<string, Scheme> schemes =
            (Dictionary<string, Scheme>)ConfigurationManager.ThemeManager.First ().Value ["Schemes"].PropertyValue;
        Assert.Equal (new Color (Color.White), schemes! ["Base"].Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), schemes ["Base"].Normal.Background);

        // Now re-apply
        Apply ();

        Assert.Equal (KeyCode.Z | KeyCode.AltMask, Application.QuitKey.KeyCode);
        Assert.Equal ("Default", ConfigurationManager.ThemeManager.Theme);

        Assert.Equal (new Color (Color.White), SchemeManager.Schemes ["Base"].Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), SchemeManager.Schemes ["Base"].Normal.Background);
        ResetAllSettings ();

        Locations = savedLocations;
    }

    [Fact]
    public void UseWithoutResetDoesNotThrow ()
    {
        Initialize ();
        _ = Settings;
    }
}
