using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Text;
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
        Debug.Assert (!IsEnabled);
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
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Copies ()
    {
        Enable ();
        ResetToHardCodedDefaults ();
        Assert.Equal (Key.Esc, Application.QuitKey);

        ConfigProperty fromSettings = Settings! ["Application.QuitKey"];

        FrozenDictionary<string, ConfigProperty> initialCache = GetHardCodedConfigPropertyCache ();
        Assert.NotNull (initialCache);

        ConfigProperty fromCache = initialCache ["Application.QuitKey"];

        // Assert
        Assert.NotEqual (fromCache, fromSettings);
        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Immutable ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable ();
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

            Application.QuitKey = Key.K;

            // Assert
            FrozenDictionary<string, ConfigProperty> cache = GetHardCodedConfigPropertyCache ();
            Assert.Equal (initialCache, cache);
            Assert.True (initialCache ["Application.QuitKey"].Immutable);
            Assert.Equal (Key.Esc, (Key)initialCache ["Application.QuitKey"].PropertyValue);
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }

    [Fact]
    public void Disable_Settings_Is_NotNull ()
    {
        Disable ();
        Assert.NotNull (Settings);
    }

    [Fact]
    public void Enable_Settings_Is_Valid ()
    {
        Disable ();
        Enable ();

        Assert.NotNull (Settings);

        Disable ();
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
        Enable ();
        ResetToCurrentValues ();
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
        ResetToCurrentValues ();
        Disable ();
    }

    [Fact]
    public void Load_Raises_Updated ()
    {
        var fired = false;
        Enable ();
        ResetToCurrentValues ();

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
        ResetToHardCodedDefaults ();
        Disable ();

        return;
        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj) { fired = true; }
    }

    [Fact]
    public void Load_And_Apply_Performance_Check ()
    {
        Enable ();
        ResetToHardCodedDefaults ();

        try
        {
            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch ();
            stopwatch.Start ();

            // Act
            Load (ConfigLocations.All);
            Apply ();

            // Stop stopwatch
            stopwatch.Stop ();

            // Assert
            _output.WriteLine ($"Load took {stopwatch.ElapsedMilliseconds} ms");

            // Ensure load time is reasonable (adjust threshold as needed)
            Assert.True (stopwatch.ElapsedMilliseconds < 1000,
                         $"Loading configuration took {stopwatch.ElapsedMilliseconds}ms, which exceeds reasonable threshold");
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }


    [Fact]
    public void Load_Loads_Custom_Json ()
    {
        try
        {
            Enable ();

            ResetToCurrentValues ();
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
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }

    [Fact (Skip = "Events disabled")]
    public void ResetToCurrentValues_Raises_Updated ()
    {
        var fired = false;

        try
        {
            Enable ();
            ResetToHardCodedDefaults ();

            ResetToCurrentValues ();

            Settings! ["Application.QuitKey"].PropertyValue = Key.Q;

            Updated += ConfigurationManagerUpdated;

            // Act
            ResetToCurrentValues ();

            // assert
            Assert.True (fired);
        }
        finally
        {
            Updated -= ConfigurationManagerUpdated;

            Disable ();
            ResetToHardCodedDefaults ();
        }

        return;

        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj)
        {
            fired = true;
        }
    }

    [Fact]
    public void ResetToHardCodedDefaults_and_Load_LibraryResourcesOnly_are_same ()
    {
        try
        {
            // arrange
            Enable ();
            ResetToHardCodedDefaults ();

            Settings! ["Application.QuitKey"].PropertyValue = Key.Q;
            Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
            Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;
            Settings.Apply ();

            // assert apply worked
            Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
            Assert.Equal (KeyCode.F, Application.NextTabGroupKey.KeyCode);
            Assert.Equal (KeyCode.B, Application.PrevTabGroupKey.KeyCode);

            //act
            ResetToHardCodedDefaults ();

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

            // act
            Load (ConfigLocations.LibraryResources);
            Apply ();

            // assert
            Assert.NotEmpty (ThemeManager.Themes);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
            Assert.Equal (Key.F6, Application.NextTabGroupKey);
            Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }

    [Fact]
    public void ResetToCurrentValues_Throws_If_Not_Enabled ()
    {
        Assert.False (IsEnabled);

        // Act
        Assert.Throws<ConfigurationManagerNotEnabledException> (ResetToCurrentValues);
    }

    [Fact]
    public void ResetToCurrentValues_Enabled_Resets ()
    {
        // Act
        Enable ();
        ResetToHardCodedDefaults ();

        Application.QuitKey = Key.A;

        ResetToCurrentValues ();

        Assert.Equal (Key.A, (Key)Settings! ["Application.QuitKey"].PropertyValue);
        Assert.NotNull (Settings);
        Assert.NotNull (AppSettings);
        Assert.NotNull (ThemeManager.Themes);

        // Default Theme should be "Default"
        Assert.Single (ThemeManager.Themes);
        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);

        ResetToHardCodedDefaults ();
        Assert.Equal (Key.Esc, (Key)Settings! ["Application.QuitKey"].PropertyValue);
        Disable ();
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

            ResetToCurrentValues ();
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
            ResetToCurrentValues ();
            Disable ();
        }
    }

    /// <summary>Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.</summary>
    /// <remarks>
    ///     IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties are all
    ///     static and thus can be overwritten by other tests.
    /// </remarks>
    [Fact]
    public void Save_HardCodedDefaults_To_config_json ()
    {
        try
        {
            Enable ();
            // Get the hard coded settings
            ResetToHardCodedDefaults ();

            // Serialize to a JSON string
            string json = CM.SourcesManager?.ToJson (Settings);

            // Write the JSON string to the file
            File.WriteAllText ("config.json", json);

            // Verify the file was created
            Assert.True (File.Exists ("config.json"), "Failed to create config.json file");
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }

    }

    [Fact]
    public void TestConfigProperties ()
    {
        try
        {
            Enable ();
            ResetToHardCodedDefaults ();

            Assert.NotEmpty (Settings!);

            // test that all ConfigProperties have our attribute
            Assert.All (
                        Settings,
                        item => Assert.NotEmpty (
                                                 item.Value.PropertyInfo!.CustomAttributes.Where (
                                                                                                  a => a.AttributeType
                                                                                                       == typeof (ConfigurationPropertyAttribute)
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
            Assert.True (ThemeManager.GetCurrentTheme ().ContainsKey ("FrameView.DefaultBorderStyle"));
            Assert.Equal (pi, ThemeManager.GetCurrentTheme () ["FrameView.DefaultBorderStyle"].PropertyInfo);
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }


    //[Fact]
    //public void InitDriver ()
    //{
    //    Assert.Equal ("Default", ThemeManager.Theme);

    //    Assert.Equal (new Color (Color.White), SchemeManager.Schemes ["Base"]!.Normal.Foreground);
    //    Assert.Equal (new Color (Color.Blue), SchemeManager.Schemes ["Base"].Normal.Background);

    //    // Change Base
    //    Stream json = CM.SourcesManager?.ToStream (Settings);

    //    CM.SourcesManager?.Load (Settings, json, "InitDriver", ConfigLocations.Runtime);

    //    Dictionary<string, Scheme> schemes = (Dictionary<string, Scheme>)ThemeManager.Themes [ThemeManager.Theme] ["Schemes"].PropertyValue;
    //    Assert.Equal (SchemeManager.Schemes ["Base"], schemes! ["Base"]);
    //    Assert.Equal (SchemeManager.Schemes ["TopLevel"], schemes ["TopLevel"]);
    //    Assert.Equal (SchemeManager.Schemes ["Error"], schemes ["Error"]);
    //    Assert.Equal (SchemeManager.Schemes ["Dialog"], schemes ["Dialog"]);
    //    Assert.Equal (SchemeManager.Schemes ["Menu"], schemes ["Menu"]);

    //    SchemeManager.Schemes ["Base"] = schemes ["Base"];
    //    SchemeManager.Schemes ["TopLevel"] = schemes ["TopLevel"];
    //    SchemeManager.Schemes ["Error"] = schemes ["Error"];
    //    SchemeManager.Schemes ["Dialog"] = schemes ["Dialog"];
    //    SchemeManager.Schemes ["Menu"] = schemes ["Menu"];

    //    Assert.Equal (schemes ["Base"], SchemeManager.Schemes ["Base"]);
    //    Assert.Equal (schemes ["TopLevel"], SchemeManager.Schemes ["TopLevel"]);
    //    Assert.Equal (schemes ["Error"], SchemeManager.Schemes ["Error"]);
    //    Assert.Equal (schemes ["Dialog"], SchemeManager.Schemes ["Dialog"]);
    //    Assert.Equal (schemes ["Menu"], SchemeManager.Schemes ["Menu"]);
    //}

    [Fact]
    public void Load_And_Apply_HardCoded ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Spot check by setting some of the config properties
            Application.QuitKey = Key.X.WithCtrl;
            FileDialog.MaxSearchResults = 1;

            Enable ();
            Load (ConfigLocations.HardCoded);

            // Spot check
            Assert.Equal (Key.Esc, Settings ["Application.QuitKey"].PropertyValue as Key);
            Assert.Equal (10000, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Assert.Single (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.NotEmpty (ThemeManager.Themes [ThemeManager.Theme]);

            // Verify schemes are properly initialized
            Assert.NotNull (SchemeManager.Schemes);
            Assert.NotEmpty (SchemeManager.Schemes);

            // Veriify "Base" has correct values
            Assert.Equal (Color.White, SchemeManager.Schemes ["Base"]!.Normal.Foreground);
            Assert.Equal (Color.Blue, SchemeManager.Schemes ["Base"].Normal.Background);

            Apply ();
            Assert.Equal (Key.Esc, Application.QuitKey);
            Assert.Equal (10000, FileDialog.MaxSearchResults);
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }

    [Fact]
    public void Load_And_Apply_LibraryResources ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Spot check by setting some of the config properties
            Application.QuitKey = Key.X.WithCtrl;
            FileDialog.MaxSearchResults = 1;

            Enable ();
            Load (ConfigLocations.LibraryResources);

            // Spot check
            Assert.Equal (Key.Esc, Settings ["Application.QuitKey"].PropertyValue as Key);
            Assert.Equal (10000, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Assert.NotEmpty (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.NotEmpty (ThemeManager.Themes [ThemeManager.Theme]);

            // Verify schemes are properly initialized
            Assert.NotNull (SchemeManager.Schemes);
            Assert.NotEmpty (SchemeManager.Schemes);

            // Veriify "Base" has correct values
            Assert.Equal (Color.White, SchemeManager.Schemes ["Base"]!.Normal.Foreground);
            Assert.Equal (Color.Blue, SchemeManager.Schemes ["Base"].Normal.Background);

            Apply ();
            Assert.Equal (Key.Esc, Application.QuitKey);
            Assert.Equal (10000, FileDialog.MaxSearchResults);
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }

    [Fact]
    public void Load_And_Apply_RuntimeConfig ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Spot check by setting some of the config properties
            Application.QuitKey = Key.X.WithCtrl;
            FileDialog.MaxSearchResults = 1;
            Glyphs.Apple = new Rune ('z');

            ThrowOnJsonErrors = true;
            Enable ();

            RuntimeConfig = """
                            {
                                "Application.QuitKey": "Alt-Q",
                                "FileDialog.MaxSearchResults":9,
                                "Themes" : [
                                    {
                                        "Default" : {
                                            "Glyphs.Apple": "a"
                                        }
                                    }
                                ]
                            }
                            """;
            Load (ConfigLocations.Runtime);

            Assert.Equal (Key.Q.WithAlt, Settings! ["Application.QuitKey"].PropertyValue as Key);
            Assert.Equal (9, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);
            Assert.Equal (new Rune ('a'), ThemeManager.GetCurrentTheme () ["Glyphs.Apple"].PropertyValue);

            Apply ();
            Assert.Equal (Key.Q.WithAlt, Application.QuitKey);
            Assert.Equal (9, FileDialog.MaxSearchResults);
            Assert.Equal (new Rune ('a'), Glyphs.Apple);
        }
        finally
        {
            ResetToHardCodedDefaults ();
            Disable ();
        }
    }

    [Fact]
    public void InvalidJsonLogs ()
    {
        Enable ();

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

        ThrowOnJsonErrors = false;

        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void InvalidJsonThrows ()
    {
        Assert.False (IsEnabled);
        Enable ();
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

        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void UpdateFromJson ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable ();
            ResetToHardCodedDefaults ();

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

            ResetToCurrentValues ();
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
            ResetToCurrentValues ();
            Disable ();
        }
    }

    [Fact]
    public void Initialize_Throws_If_Called_Explicitly ()
    {
        Assert.Throws<InvalidOperationException> (Initialize);
    }
}
