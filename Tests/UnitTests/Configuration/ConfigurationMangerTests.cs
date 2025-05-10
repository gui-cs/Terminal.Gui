using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using UnitTests;
using Xunit.Abstractions;
using static System.Net.WebRequestMethods;
using static Terminal.Gui.ConfigurationManager;
using File = System.IO.File;

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
    public void ModuleInitializer_Was_Called ()
    {
        Assert.True (IsInitialized ());
        Assert.True (IsInitialized ());
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Copies ()
    {
        ResetToHardCodedDefaults ();
        Assert.Equal (Key.Esc, Application.QuitKey);

        ConfigProperty fromSettings = Settings! ["Application.QuitKey"];

        FrozenDictionary<string, ConfigProperty> initialCache = GetHardCodedConfigPropertyCache ();
        Assert.NotNull (initialCache);

        ConfigProperty fromCache = initialCache ["Application.QuitKey"];

        // Assert
        Assert.NotEqual (fromCache, fromSettings);
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Immutable ()
    {
        ResetToHardCodedDefaults ();


        Assert.Equal (Key.Esc, Application.QuitKey);

        FrozenDictionary<string, ConfigProperty> initialCache = GetHardCodedConfigPropertyCache ();
        Assert.NotNull (initialCache);
        Assert.Equal (Key.Esc, (Key)initialCache ["Application.QuitKey"].PropertyValue);


        // Act
        Settings! ["Application.QuitKey"].PropertyValue = Key.Q;
        Assert.Equal (Key.Q, (Key)Settings ["Application.QuitKey"].PropertyValue);

        Settings ["Application.QuitKey"].Apply ();
        Assert.Equal (Key.Q, Application.QuitKey);

        //Apply ();

        //Application.QuitKey = Key.K;

        // Assert
        FrozenDictionary<string, ConfigProperty> cache = GetHardCodedConfigPropertyCache ();
        Assert.Equal (initialCache, cache);
        Assert.True (initialCache ["Application.QuitKey"].Immutable);
        Assert.Equal (Key.Esc, (Key)initialCache ["Application.QuitKey"].PropertyValue);
    }

    [Fact]
    public void Disable_Settings_Is_Null ()
    {
        Disable ();


        Assert.Null (Settings);
    }

    [Fact]
    public void Enable_Settings_Is_Valid ()
    {
        Disable ();

        Enable ();

        Assert.NotNull (Settings);
    }

    //[Fact]
    //public void Disabled_Loads_Only_HardCoded_Values ()
    //{
    //    try
    //    {
    //        CM.Enable ();
    //        Assert.NotEmpty (ThemeManager.Themes!);

    //        ThemeManager.Reset ();

    //        Assert.NotEmpty (ThemeManager.Themes!);

    //        // Default theme exists
    //        Assert.NotNull (ThemeManager.Themes? ["Default"]);

    //        //// Schemes exists, but is not initialized
    //        //Assert.Null (manager ["Default"].);

    //        //manager.RetrieveValues ();

    //        //Assert.NotEmpty (manager);

    //        //// Schemes exists, and has correct # of eleements
    //        //var schemes = manager ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
    //        //Assert.NotNull (schemes);
    //        //Assert.Equal (5, schemes!.Count);

    //        //// Base has correct values
    //        //var baseSchemee = schemes ["Base"];
    //        //Assert.Equal (new Attribute (Color.White, Color.Blue), baseSchemee.Normal);

    //    }
    //    finally
    //    {
    //        CM.Reset ();
    //    }

    //}

    //[Fact]
    //public void Enabled_ ()
    //{
    //    try
    //    {
    //        CM.Enable ();
    //        Assert.NotEmpty (ThemeManager.Themes!);

    //        ThemeManager.Reset ();

    //        Assert.NotEmpty (ThemeManager.Themes!);

    //        // Default theme exists
    //        Assert.NotNull (ThemeManager.Themes? ["Default"]);

    //        //// Schemes exists, but is not initialized
    //        //Assert.Null (manager ["Default"].);

    //        //manager.RetrieveValues ();

    //        //Assert.NotEmpty (manager);

    //        //// Schemes exists, and has correct # of eleements
    //        //var schemes = manager ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
    //        //Assert.NotNull (schemes);
    //        //Assert.Equal (5, schemes!.Count);

    //        //// Base has correct values
    //        //var baseSchemee = schemes ["Base"];
    //        //Assert.Equal (new Attribute (Color.White, Color.Blue), baseSchemee.Normal);

    //    }
    //    finally
    //    {
    //        CM.Reset ();
    //    }

    //}

    [Fact]
    public void Apply_Raises_Applied ()
    {
        Reset ();
        Applied += ConfigurationManagerApplied;
        var fired = false;

        void ConfigurationManagerApplied (object sender, ConfigurationManagerEventArgs obj)
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

        Applied -= ConfigurationManagerApplied;
        Reset ();
    }

    [Fact]
    public void Load_Raises_Updated ()
    {
        var fired = false;
        Enable ();
        Reset ();

        ThrowOnJsonErrors = true;
        Assert.Equal (Key.Esc, (((Key)Settings! ["Application.QuitKey"].PropertyValue)!).KeyCode);

        Updated += ConfigurationManagerUpdated;

        // Act
        // Only select locations under test control
        Load (ConfigLocations.LibraryResources | ConfigLocations.AppResources | ConfigLocations.Runtime);

        // assert
        Assert.True (fired);

        // clean up
        Updated -= ConfigurationManagerUpdated;
        Reset ();
        Disable ();

        return;
        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj) { fired = true; }

    }

    [Fact]
    public void Load_Performance_Check ()
    {
        Enable ();
        Reset ();

        // Start stopwatch
        Stopwatch stopwatch = new Stopwatch ();
        stopwatch.Start ();

        // Act
        Load (ConfigLocations.All);
        Apply ();

        // Stop stopwatch
        stopwatch.Stop ();

        Disable ();

        // Assert
        _output.WriteLine ($"Load took {stopwatch.ElapsedMilliseconds} ms");

        // Ensure load time is reasonable (adjust threshold as needed)
        Assert.True (stopwatch.ElapsedMilliseconds < 1000,
            $"Loading configuration took {stopwatch.ElapsedMilliseconds}ms, which exceeds reasonable threshold");
    }

    [Fact]
    public void Load_Loads_Custom_Json ()
    {
        try
        {
            Enable ();

            Reset ();
            ThrowOnJsonErrors = true;

            Assert.Equal (Key.Esc, (Key)Settings! ["Application.QuitKey"].PropertyValue);

            // act
            RuntimeConfig = """
                            
                                    {
                                          "Application.QuitKey": "Ctrl-Q"
                                    }
                            """;
            Load (ConfigLocations.Runtime);

            // assert
            Assert.Equal (Key.Q.WithCtrl, (Key)Settings ["Application.QuitKey"].PropertyValue);

        }
        finally
        {

            // clean up
//            Locations = ConfigLocations.LibraryResources;
            Reset ();
            Disable ();
        }
    }

    [Fact (Skip = "AI Generated")]
    public void Load_With_MultipleKeyBindings_MergesCorrectly ()
    {
        // arrange
        //  Locations = ConfigLocations.Runtime | ConfigLocations.LibraryResources;
        Reset ();
        ThrowOnJsonErrors = true;

        // act - set multiple key bindings in different configs
        RuntimeConfig = """
                   {
                        "Application.QuitKey": "Ctrl-Q",
                        "Application.NextTabGroupKey": "Ctrl-Tab"
                   }
                   """;

        var oldSource = """
                   {
                        "Application.PrevTabGroupKey": "Ctrl-Shift-Tab"
                   }
                   """;

        // Update with both configs
        Load (ConfigLocations.Runtime);
        CM.SourcesManager?.Load (Settings, oldSource, "older-config", ConfigLocations.LibraryResources);

        // assert - all settings should be merged
        Assert.Equal (Key.Q.WithCtrl, (Key)Settings! ["Application.QuitKey"].PropertyValue);
        Assert.Equal (Key.Tab.WithCtrl, (Key)Settings ["Application.NextTabGroupKey"].PropertyValue);
        Assert.Equal (Key.Tab.WithCtrl.WithShift, (Key)Settings ["Application.PrevTabGroupKey"].PropertyValue);

        // clean up
        ConfigurationManager.Disable ();
        ConfigurationManager.ResetToHardCodedDefaults ();
    }

    [Fact]
    public void ResetAllSettings_Raises_Updated ()
    {
        var fired = false;

        try
        {
            Enable ();
            // Only select locations under test control
            //Locations = ConfigLocations.LibraryResources | ConfigLocations.AppResources | ConfigLocations.Runtime;

            Reset ();

            Settings! ["Application.QuitKey"].PropertyValue = Key.Q;

            Updated += ConfigurationManagerUpdated;

            // Act
            Reset ();

            // assert
            Assert.True (fired);
        }
        finally
        {
            Updated -= ConfigurationManagerUpdated;
            Reset ();
            ConfigurationManager.Disable ();
            ConfigurationManager.ResetToHardCodedDefaults ();
        }

        return;

        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj)
        {
            fired = true;
        }
    }

    [Fact]
    public void ResetAllSettings_and_ResetLoadWithLibraryResourcesOnly_are_same ()
    {
        try
        {
            Enable ();
           // Locations = ConfigLocations.LibraryResources;

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
            Assert.NotEmpty (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (Key.Esc, Application.QuitKey);
            Assert.Equal (Key.F6, Application.NextTabGroupKey);
            Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);

            // arrange
            Settings ["Application.QuitKey"].PropertyValue = Key.Q;
            Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
            Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;
            Settings.Apply ();

            //Locations = ConfigLocations.LibraryResources;

            // act
            Reset ();
            Load (ConfigLocations.LibraryResources);

            // assert
            Assert.NotEmpty (ThemeManager.Themes);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
            Assert.Equal (Key.F6, Application.NextTabGroupKey);
            Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
        }
        finally
        {
            ConfigurationManager.Disable ();
            ConfigurationManager.ResetToHardCodedDefaults ();
        }
    }

    [Fact]
    public void ResetAllSettings_Resets ()
    {
        // Act
        Reset ();

        Assert.NotNull (Settings);
        Assert.NotNull (AppSettings);
        Assert.NotNull (ThemeManager.Themes);

        // Default Theme should be "Default"
        Assert.Single (ThemeManager.Themes);
        Assert.Equal ("Default", ThemeManager.Theme);
    }

    [Fact]
    public void ConfigurationManager_DefaultPrecedence_IsRespected ()
    {

        try
        {
            // arrange
            Enable ();

            // Only select locations under test control
            //Locations = ConfigLocations.LibraryResources | ConfigLocations.AppResources | ConfigLocations.Runtime;

            Reset ();
            ThrowOnJsonErrors = true;

            // Setup multiple configurations with the same setting
            // with different precedence levels
            RuntimeConfig = """
                            {
                                 "Application.QuitKey": "Alt+Q"  
                            }
                            """;

            var defaultConfig = """
                                {
                                     "Application.QuitKey": "Ctrl+X"
                                }
                                """;

            // Update default config first (lower precedence)
            CM.SourcesManager?.Load (Settings, defaultConfig, "default-test", ConfigLocations.LibraryResources);

            // Then load runtime config, which should override default
            Load (ConfigLocations.Runtime);

            // Assert - the runtime config should win due to precedence
            Assert.Equal (Key.Q.WithAlt, (Key)Settings! ["Application.QuitKey"].PropertyValue);

            // clean up
            //Locations = ConfigLocations.LibraryResources;
        }
        finally
        {
            Reset ();
            Disable ();
        }
    }

    /// <summary>Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.</summary>
    /// <remarks>
    ///     IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties are all
    ///     static and thus can be overwritten by other tests.
    /// </remarks>
    [Fact]
    public void SaveDefaults ()
    {
        // Get the hard coded settings
        ResetToHardCodedDefaults();

        // Serialize to a JSON string
        string json = CM.SourcesManager?.ToJson (Settings);

        // Write the JSON string to the file
        File.WriteAllText ("config.json", json);

        // Verify the file was created
        Assert.True (File.Exists ("config.json"), "Failed to create config.json file");
    }

    [Fact]
    public void TestConfigProperties ()
    {
        // Only select locations under test control
       // Locations = ConfigLocations.LibraryResources | ConfigLocations.AppResources | ConfigLocations.Runtime;

        Reset ();

        Assert.NotEmpty (Settings!);

        // test that all ConfigProperties have our attribute
        Assert.All (
                    Settings,
                    item => Assert.NotEmpty (
                                             item.Value.PropertyInfo!.CustomAttributes.Where (
                                                                                              a => a.AttributeType == typeof (ConfigurationPropertyAttribute)
                                                                                             )
                                            )
                   );

#pragma warning disable xUnit2030
        Assert.Empty (
                      Settings.Where (
                                      cp => cp.Value.PropertyInfo!.GetCustomAttribute (
                                                                                       typeof (ConfigurationPropertyAttribute)
                                                                                      )
                                            == null
                                     )
                     );
#pragma warning restore xUnit2030

        // Application is a static class
        PropertyInfo pi = typeof (Application).GetProperty ("QuitKey");
        Assert.Equal (pi, Settings ["Application.QuitKey"].PropertyInfo);

        // FrameView is not a static class and DefaultBorderStyle is Scope.Scheme
        pi = typeof (FrameView).GetProperty ("DefaultBorderStyle");
        Assert.False (Settings.ContainsKey ("FrameView.DefaultBorderStyle"));
        Assert.True (ThemeManager.Themes! ["Default"].ContainsKey ("FrameView.DefaultBorderStyle"));
        Assert.Equal (pi, ThemeManager.Themes! ["Default"] ["FrameView.DefaultBorderStyle"].PropertyInfo);
    }

    [Fact]
    public void TestConfigPropertyOmitClassName ()
    {
        //ConfigLocations savedLocations = Locations;
        // Only select locations under test control
       // Locations = ConfigLocations.LibraryResources | ConfigLocations.AppResources | ConfigLocations.Runtime;

        // Color.Schemes is serialized as "Schemes", not "Colors.Schemes"
        PropertyInfo pi = typeof (SchemeManager).GetProperty ("Schemes");
        var scp = (ConfigurationPropertyAttribute)pi!.GetCustomAttribute (typeof (ConfigurationPropertyAttribute));
        Assert.True (scp!.Scope == typeof (ThemeScope));
        Assert.True (scp.OmitClassName);

        Reset ();
        Assert.Equal (pi, ThemeManager.Themes! ["Default"] ["Schemes"].PropertyInfo);

       // Locations = savedLocations;
    }

    [Fact]
    [AutoInitShutdown]
    public void InitDriver ()
    {
        Assert.Equal ("Default", ThemeManager.Theme);

        Assert.Equal (new Color (Color.White), SchemeManager.Schemes ["Base"]!.Normal.Foreground);
        Assert.Equal (new Color (Color.Blue), SchemeManager.Schemes ["Base"].Normal.Background);

        // Change Base
        Stream json = CM.SourcesManager?.ToStream (Settings);

        CM.SourcesManager?.Load (Settings, json, "InitDriver", ConfigLocations.Runtime);

        Dictionary<string, Scheme> schemes =
            (Dictionary<string, Scheme>)ThemeManager.Themes [ThemeManager.Theme] ["Schemes"].PropertyValue;
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
    public void InitDriver_NoLocations ()
    {
        // If ConfigLocations.None, then no config files are loaded
        // and Settings is populated with the hard coded values found in the sourcecode
        try
        {
//            Locations = ConfigLocations.HardCoded;

            // Spot check by setting some of the config properties
            Application.QuitKey = Key.X.WithCtrl;
            FileDialog.MaxSearchResults = 1;

            Application.Init (new FakeDriver ());

            // Verify Settings exists and values were set
            Assert.NotEmpty (Settings!);
            Assert.All (
                        Settings,
                        item =>
                        {
                            Assert.True (item.Value.HasValue);
                        });

            // Spot check
            Assert.Equal ("Ctrl+X", Settings ["Application.QuitKey"].PropertyValue as Key);
            Assert.Equal (1, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Verify AppSettings exists and values were set
            Assert.NotEmpty (AppSettings!);
            Assert.All (
                        AppSettings,
                        item =>
                        {
                            Assert.True (item.Value.HasValue);
                        });


            Assert.Equal ("Default", ThemeManager.Theme);

            Assert.Single (ThemeManager.Themes!);
            Assert.NotEmpty (ThemeManager.Themes [ThemeManager.Theme]);

            // Verify schemes are properly initialized
            Assert.NotNull (SchemeManager.Schemes);
            Assert.NotEmpty (SchemeManager.Schemes);

            // Veriify "Base" has correct values
            Assert.Equal (Color.White, SchemeManager.Schemes ["Base"]!.Normal.Foreground);
            Assert.Equal (Color.Blue, SchemeManager.Schemes ["Base"].Normal.Background);
        }
        finally
        {
            Reset ();
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Theme_Reload_Consistency ()
    {
        try
        {
            Enable ();

            // First load with a custom theme
          //  Locations = ConfigLocations.Runtime;
            Reset ();

            // Create a test theme
            RuntimeConfig = """
                   {
                        "Theme": "TestTheme",
                        "Themes": [
                          {
                            "TestTheme": {
                              "Schemes": []
                            }
                          }
                        ]
                   }
                   """;

            // Load the test theme
            Load (ConfigLocations.Runtime);
            Assert.Equal ("TestTheme", ThemeManager.Theme);

            // Now reset everything and reload
          //  Locations = ConfigLocations.HardCoded;
            Reset ();

            // Verify we're back to default
            Assert.Equal ("Default", ThemeManager.Theme);
        }
        finally
        {
            Reset ();
            Disable ();
        }
    }

    [Fact]
    public void InvalidJsonLogs ()
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

        CM.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

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

        CM.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

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

        CM.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

        CM.SourcesManager?.Load (Settings, "{}}", "test", ConfigLocations.Runtime);

        Assert.NotEqual (0, _jsonErrors.Length);

        Application.Shutdown ();

        ThrowOnJsonErrors = false;
    }

    [Fact]
    [AutoInitShutdown]
    public void InvalidJsonThrows ()
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

        var jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
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

        jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
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

        jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.Equal ("Both Foreground and Background colors must be provided.", jsonException.Message);

        // Unknown property
        json = @"
			{
				""Unknown"" : ""Not known""
			}";

        jsonException = Assert.Throws<JsonException> (() => CM.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("Unknown property", jsonException.Message);

        Assert.Equal (0, _jsonErrors.Length);

        ThrowOnJsonErrors = false;
    }

    [Fact]
    [AutoInitShutdown]
    public void ToJson ()
    {
        Reset ();
        Stream stream = CM.SourcesManager?.ToStream (Settings);

        CM.SourcesManager?.Load (Settings, stream, "ToJson", ConfigLocations.Runtime);

        // TODO: What does this test?
    }

    [Fact]
    public void UpdateFromJson ()
    {
        try
        {
            Enable ();

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

            Reset ();
            ThrowOnJsonErrors = true;

            CM.SourcesManager?.Load (Settings, json, "UpdateFromJson", ConfigLocations.Runtime);

            Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
            Assert.Equal (KeyCode.Z | KeyCode.AltMask, ((Key)Settings ["Application.QuitKey"].PropertyValue)!.KeyCode);

            Assert.Equal ("Default", ThemeManager.Theme);

            Assert.Equal (new Color (Color.White), SchemeManager.Schemes ["Base"]!.Normal.Foreground);
            Assert.Equal (new Color (Color.Blue), SchemeManager.Schemes ["Base"].Normal.Background);

            Dictionary<string, Scheme> schemes =
                (Dictionary<string, Scheme>)ThemeManager.Themes.First ().Value ["Schemes"].PropertyValue;
            Assert.Equal (new Color (Color.White), schemes! ["Base"].Normal.Foreground);
            Assert.Equal (new Color (Color.Blue), schemes ["Base"].Normal.Background);

            // Now re-apply
            Apply ();

            Assert.Equal (KeyCode.Z | KeyCode.AltMask, Application.QuitKey.KeyCode);
            Assert.Equal ("Default", ThemeManager.Theme);

            Assert.Equal (new Color (Color.White), SchemeManager.Schemes ["Base"].Normal.Foreground);
            Assert.Equal (new Color (Color.Blue), SchemeManager.Schemes ["Base"].Normal.Background);
        }
        finally
        {
            Reset ();
            Disable ();
        }
    }

    [Fact]
    public void Initialize_Throws_If_Called_Explicitly ()
    {
        Assert.Throws<InvalidOperationException> (Initialize);
    }
}
