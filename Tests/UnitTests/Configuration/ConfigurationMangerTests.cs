using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ColorHelper;
using Xunit.Abstractions;
using static Terminal.Gui.Configuration.ConfigurationManager;
using File = System.IO.File;

#pragma warning disable IDE1006

namespace Terminal.Gui.ConfigurationTests;

public class ConfigurationManagerTests (ITestOutputHelper output)
{
    [Fact]
    public void ModuleInitializer_Was_Called ()
    {
        Assert.False (IsEnabled);
        Assert.True (IsInitialized ());
    }

    [Fact]
    public void Initialize_Throws_If_Called_Explicitly ()
    {
        Assert.False (IsEnabled);

        Assert.Throws<InvalidOperationException> (Initialize);
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Copies ()
    {
        Assert.False (IsEnabled);
        Enable (ConfigLocations.HardCoded);

        Assert.Equal (Key.Esc, Application.QuitKey);

        ConfigProperty fromSettings = Settings! ["Application.QuitKey"];

        FrozenDictionary<string, ConfigProperty> initialCache = GetHardCodedConfigPropertyCache ();
        Assert.NotNull (initialCache);

        ConfigProperty fromCache = initialCache ["Application.QuitKey"];

        // Assert
        Assert.NotEqual (fromCache, fromSettings);
        Disable (true);
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Immutable ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);
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
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Disable_Settings_Is_NotNull ()
    {
        Assert.False (IsEnabled);

        Disable ();
        Assert.NotNull (Settings);
    }


    [Fact]
    public void Disable_With_ResetToHardCodedDefaults_True_Works_When_Disabled ()
    {
        Assert.False (ConfigurationManager.IsEnabled);
        ConfigurationManager.Disable (true);
    }

    [Fact]
    public void Enable_Settings_Is_Valid ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        Assert.NotNull (Settings);

        Disable ();
    }


    [Fact]
    public void Apply_Applies_Theme ()
    {
        Assert.False (IsEnabled);
        Enable (ConfigLocations.HardCoded);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
        Assert.NotEmpty (theme);

        Assert.True (ThemeManager.Themes!.TryAdd ("testTheme", theme));
        Assert.Equal (2, ThemeManager.Themes.Count);

        Assert.Equal (LineStyle.Rounded, FrameView.DefaultBorderStyle);
        theme ["FrameView.DefaultBorderStyle"].PropertyValue = LineStyle.Double;

        ThemeManager.Theme = "testTheme";
        ConfigurationManager.Apply ();

        Assert.Equal (LineStyle.Double, FrameView.DefaultBorderStyle);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void Apply_Raises_Applied ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

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

        Disable (resetToHardCodedDefaults: true);
        Application.ResetState (true);
    }

    [Fact]
    public void Load_Raises_Updated ()
    {
        Assert.False (IsEnabled);

        var fired = false;
        Enable (ConfigLocations.HardCoded);

        ThrowOnJsonErrors = true;
        Assert.Equal (Key.Esc, ((Key)Settings! ["Application.QuitKey"].PropertyValue)!.KeyCode);

        Updated += ConfigurationManagerUpdated;

        // Act
        // Only select locations under test control
        Load (ConfigLocations.LibraryResources | ConfigLocations.AppResources | ConfigLocations.Runtime);

        // assert
        Assert.True (fired);

        // clean up
        Updated -= ConfigurationManagerUpdated;
        Disable (true);
        Application.ResetState (true);

        return;

        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj) { fired = true; }
    }

    [Fact]
    public void Load_And_Apply_Performance_Check ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        try
        {
            // Start stopwatch
            var stopwatch = new Stopwatch ();
            stopwatch.Start ();

            // Act
            Load (ConfigLocations.All);
            Apply ();

            // Stop stopwatch
            stopwatch.Stop ();

            // Assert
            output.WriteLine ($"Load took {stopwatch.ElapsedMilliseconds} ms");

            // Ensure load time is reasonable (adjust threshold as needed)
            Assert.True (
                         stopwatch.ElapsedMilliseconds < 1000,
                         $"Loading configuration took {stopwatch.ElapsedMilliseconds}ms, which exceeds reasonable threshold");
        }
        finally
        {
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Load_Loads_Custom_Json ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);
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
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact (Skip = "Events disabled")]
    public void ResetToCurrentValues_Raises_Updated ()
    {
        Assert.False (IsEnabled);

        var fired = false;

        try
        {
            Enable (ConfigLocations.HardCoded);

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

            Disable (true);
            Application.ResetState (true);
        }

        return;

        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj) { fired = true; }
    }

    [Fact]
    public void ResetToHardCodedDefaults_and_Load_LibraryResourcesOnly_are_same ()
    {
        Assert.False (IsEnabled);

        try
        {
            // arrange
            Enable (ConfigLocations.HardCoded);

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
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void ResetToCurrentValues_Enabled_Resets ()
    {
        Assert.False (IsEnabled);

        // Act
        Enable (ConfigLocations.HardCoded);

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
        Application.ResetState (true);
    }

    [Fact]
    public void ConfigurationManager_DefaultPrecedence_IsRespected ()
    {
        Assert.False (IsEnabled);

        try
        {
            // arrange
            Enable (ConfigLocations.HardCoded);

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
            ConfigurationManager.SourcesManager?.Load (Settings, defaultConfig, "default-test", ConfigLocations.LibraryResources);

            // Then load runtime config, which should override default
            Load (ConfigLocations.Runtime);

            // Assert - the runtime config should win due to precedence
            Assert.Equal (Key.Q.WithAlt, (Key)Settings! ["Application.QuitKey"].PropertyValue);

        }
        finally
        {
            Disable (true);
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
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            // Get the hard coded settings
            ResetToHardCodedDefaults ();

            // Serialize to a JSON string
            string json = ConfigurationManager.SourcesManager?.ToJson (Settings);

            // Write the JSON string to the file
            File.WriteAllText ("hard_coded_defaults_config.json", json);

            // Verify the file was created
            Assert.True (File.Exists ("hard_coded_defaults_config.json"), "Failed to create config.json file");
        }
        finally
        {
            Disable (true);
        }
    }

    /// <summary>Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.</summary>
    /// <remarks>
    ///     IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties are all
    ///     static and thus can be overwritten by other tests.
    /// </remarks>
    [Fact]
    public void Save_Library_Defaults_To_config_json ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.LibraryResources);

            // Serialize to a JSON string
            string json = ConfigurationManager.SourcesManager?.ToJson (Settings);

            // Write the JSON string to the file
            File.WriteAllText ("library_defaults_config.json", json);

            // Verify the file was created
            Assert.True (File.Exists ("library_defaults_config.json"), "Failed to create config.json file");
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void TestConfigProperties ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            Assert.NotEmpty (Settings!);

            // test that all ConfigProperties have our attribute
            Assert.All (
                        Settings,
                        item => Assert.Contains (item.Value.PropertyInfo!.CustomAttributes, a => a.AttributeType
                                                                                                       == typeof (ConfigurationPropertyAttribute)
));

#pragma warning disable xUnit2030
            Assert.DoesNotContain (Settings, cp => cp.Value.PropertyInfo!.GetCustomAttribute (
                                                                                           typeof (ConfigurationPropertyAttribute)
                                                                                          )
                                                == null
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
            Disable (true);
        }
    }

    [Fact]
    public void Load_And_Apply_HardCoded ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Spot check by setting some of the config properties
            Application.QuitKey = Key.X.WithCtrl;
            FileDialog.MaxSearchResults = 1;

            Enable (ConfigLocations.HardCoded);
            Load (ConfigLocations.HardCoded);

            // Spot check
            Assert.Equal (Key.Esc, Settings ["Application.QuitKey"].PropertyValue as Key);
            Assert.Equal (10000, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Assert.Single (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.NotEmpty (ThemeManager.Themes [ThemeManager.Theme]);

            // Verify schemes are properly initialized
            Assert.NotNull (SchemeManager.GetSchemes ());
            Assert.NotEmpty (SchemeManager.GetSchemes ());

            // Verify "Base" has correct values
            //Assert.Equal (Color.White, SchemeManager.GetSchemes () ["Base"]!.Normal.Foreground);
            //Assert.Equal (Color.Blue, SchemeManager.GetSchemes () ["Base"].Normal.Background);

            Apply ();
            Assert.Equal (Key.Esc, Application.QuitKey);
            Assert.Equal (10000, FileDialog.MaxSearchResults);
        }
        finally
        {
            Disable (true);
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

            Enable (ConfigLocations.HardCoded);
            Load (ConfigLocations.LibraryResources);

            // Spot check
            Assert.Equal (Key.Esc, Settings! ["Application.QuitKey"].PropertyValue as Key);
            Assert.Equal (10000, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Assert.NotEmpty (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.NotEmpty (ThemeManager.Themes [ThemeManager.Theme]);

            // Verify schemes are properly initialized
            Assert.NotNull (SchemeManager.GetSchemes ());
            Assert.NotEmpty (SchemeManager.GetSchemes ());

            // This is too fragile as the default scheme may change
            // Verify "Base" has correct values
            //Assert.Equal (Color.White, SchemeManager.Schemes ["Base"]!.Normal.Foreground);
            //Assert.Equal (Color.Blue, SchemeManager.Schemes ["Base"].Normal.Background);

            Apply ();
            Assert.Equal (Key.Esc, Application.QuitKey);
            Assert.Equal (10000, FileDialog.MaxSearchResults);
        }
        finally
        {
            Disable (true);
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
            Glyphs.Apple = new ('z');

            ThrowOnJsonErrors = true;
            Enable (ConfigLocations.HardCoded);

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
            Assert.Equal (new ('a'), Glyphs.Apple);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void InvalidJsonLogs ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

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

        ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

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

        ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

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

        ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

        ConfigurationManager.SourcesManager?.Load (Settings, "{}}", "test", ConfigLocations.Runtime);

        Assert.NotEqual (0, _jsonErrors.Length);

        ThrowOnJsonErrors = false;

        Disable (true);
    }

    [Fact]
    public void InvalidJsonThrows ()
    {
        Assert.False (IsEnabled);
        Enable (ConfigLocations.HardCoded);

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

        var jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("foreground: \"\"brownish\"\"", jsonException.Message);

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

        jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("AbNormal:", jsonException.Message);

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

        jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("background:", jsonException.Message);

        // Unknown property
        json = @"
			{
				""Unknown"" : ""Not known""
			}";

        jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("Unknown:", jsonException.Message);

        Assert.Equal (0, _jsonErrors.Length);

        ThrowOnJsonErrors = false;

        Disable (true);
    }

    [Fact]
    public void UpdateFromJson ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            // Arrange
            var json = @"
{
  ""$schema"": ""https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"",
  ""Application.QuitKey"": ""Alt-Z"",
  ""Theme"": ""Default"",
  ""Themes"": [
    {
      ""Default"": {
        ""MessageBox.DefaultButtonAlignment"": ""End"",
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

            ConfigurationManager.SourcesManager?.Load (Settings, json, "UpdateFromJson", ConfigLocations.Runtime);

            Assert.Equal ("Default", ThemeManager.Theme);

            Assert.Equal (KeyCode.Esc, Application.QuitKey.KeyCode);
            Assert.Equal (KeyCode.Z | KeyCode.AltMask, ((Key)Settings! ["Application.QuitKey"].PropertyValue)!.KeyCode);
            Assert.Equal (Alignment.Center, MessageBox.DefaultButtonAlignment);

            // Now re-apply
            Apply ();

            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (KeyCode.Z | KeyCode.AltMask, Application.QuitKey.KeyCode);
            Assert.Equal (Alignment.End, MessageBox.DefaultButtonAlignment);

            Assert.Equal (Color.White, SchemeManager.GetSchemes ()! ["Base"].Normal.Foreground);
            Assert.Equal (Color.Blue, SchemeManager.GetSchemes ()! ["Base"].Normal.Background);
        }
        finally
        {
            Disable (resetToHardCodedDefaults: true);

        }
    }

}
