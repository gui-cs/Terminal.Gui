using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;


namespace Terminal.Gui.Configuration {
	public class ConfigurationMangerTests {

		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter ()
				}
		};

		public ConfigurationMangerTests ()
		{
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
		public void TestConfigurationManagerMultiThemes ()
		{
			var configuration = new Configuration ();
			configuration.GetAllHardCodedDefaults ();
			var newTheme = new Theme ();
			newTheme.ColorSchemes.Add ("NewScheme", new ColorScheme () { Normal = Attribute.Make (Color.White, Color.Red) });

			configuration.Themes.ThemeDefinitions.Add ("NewTheme", newTheme);
			configuration.Themes.SelectedTheme = "NewTheme";
			var json = ConfigurationManager.ToJson (configuration);

			var readConfig = ConfigurationManager.LoadFromJson (json);
			File.WriteAllText ("config-newtheme.json", json);

			ConfigurationManager.Config.ApplyAll ();

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

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerToJson ()
		{
			var configuration = new Configuration ();
			configuration.GetAllHardCodedDefaults ();
			var json = ConfigurationManager.ToJson (configuration);

			var readConfig = ConfigurationManager.LoadFromJson (json);

			//Assert.Equal (Colors.Base.Normal, readConfig.ColorSchemes ["Base"].Normal);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerInitDriver ()
		{
			var configuration = new Configuration ();
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

			Assert.Equal (Key.Q | Key.CtrlMask, configuration.Settings.QuitKey);

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
	}

	public class ColorJsonConverterTests {

		[Theory]
		[InlineData ("Black", Color.Black)]
		[InlineData ("Blue", Color.Blue)]
		[InlineData ("BrightBlue", Color.BrightBlue)]
		[InlineData ("BrightCyan", Color.BrightCyan)]
		[InlineData ("BrightGreen", Color.BrightGreen)]
		[InlineData ("BrightMagenta", Color.BrightMagenta)]
		[InlineData ("BrightRed", Color.BrightRed)]
		[InlineData ("BrightYellow", Color.BrightYellow)]
		[InlineData ("Brown", Color.Brown)]
		[InlineData ("Cyan", Color.Cyan)]
		[InlineData ("DarkGray", Color.DarkGray)]
		[InlineData ("Gray", Color.Gray)]
		[InlineData ("Green", Color.Green)]
		[InlineData ("Magenta", Color.Magenta)]
		[InlineData ("Red", Color.Red)]
		[InlineData ("White", Color.White)]
		public void TestColorDeserializationFromHumanReadableColorNames (string colorName, Color expectedColor)
		{
			// Arrange
			string json = $"\"{colorName}\"";

			// Act
			Color actualColor = JsonSerializer.Deserialize<Color> (json, ConfigurationMangerTests._jsonOptions);

			// Assert
			Assert.Equal (expectedColor, actualColor);
		}


		[Theory]
		[InlineData (Color.Black, "Black")]
		[InlineData (Color.Blue, "Blue")]
		[InlineData (Color.Green, "Green")]
		[InlineData (Color.Cyan, "Cyan")]
		[InlineData (Color.Gray, "Gray")]
		[InlineData (Color.Red, "Red")]
		[InlineData (Color.Magenta, "Magenta")]
		[InlineData (Color.Brown, "Brown")]
		[InlineData (Color.DarkGray, "DarkGray")]
		[InlineData (Color.BrightBlue, "BrightBlue")]
		[InlineData (Color.BrightGreen, "BrightGreen")]
		[InlineData (Color.BrightCyan, "BrightCyan")]
		[InlineData (Color.BrightRed, "BrightRed")]
		[InlineData (Color.BrightMagenta, "BrightMagenta")]
		[InlineData (Color.BrightYellow, "BrightYellow")]
		[InlineData (Color.White, "White")]
		public void SerializesEnumValuesAsStrings (Color color, string expectedJson)
		{
			var converter = new ColorJsonConverter ();
			var options = new JsonSerializerOptions { Converters = { converter } };

			var serialized = JsonSerializer.Serialize (color, options);

			Assert.Equal ($"\"{expectedJson}\"", serialized);
		}

		[Fact]
		public void TestSerializeColor_Black ()
		{
			// Arrange
			var color = Color.Black;
			var expectedJson = "\"Black\"";

			// Act
			var json = JsonSerializer.Serialize (color, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedJson, json);
		}

		[Fact]
		public void TestSerializeColor_BrightRed ()
		{
			// Arrange
			var color = Color.BrightRed;
			var expectedJson = "\"BrightRed\"";

			// Act
			var json = JsonSerializer.Serialize (color, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedJson, json);
		}

		[Fact]
		public void TestDeserializeColor_Black ()
		{
			// Arrange
			var json = "\"Black\"";
			var expectedColor = Color.Black;

			// Act
			var color = JsonSerializer.Deserialize<Color> (json, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedColor, color);
		}

		[Fact]
		public void TestDeserializeColor_BrightRed ()
		{
			// Arrange
			var json = "\"BrightRed\"";
			var expectedColor = Color.BrightRed;

			// Act
			var color = JsonSerializer.Deserialize<Color> (json, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedColor, color);
		}
	}

	public class AttributeJsonConverterTests {
		[Fact, AutoInitShutdown]
		public void TestDeserialize ()
		{
			// Test deserializing from human-readable color names
			var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\"}";
			var attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationMangerTests._jsonOptions);
			Assert.Equal (Color.Blue, attribute.Foreground);
			Assert.Equal (Color.Green, attribute.Background);

			// Test deserializing from RGB values
			json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
			attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationMangerTests._jsonOptions);
			Assert.Equal (Color.BrightRed, attribute.Foreground);
			Assert.Equal (Color.BrightGreen, attribute.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestSerialize ()
		{
			// Test serializing to human-readable color names
			var attribute = new Attribute (Color.Blue, Color.Green);
			var json = JsonSerializer.Serialize<Attribute> (attribute, ConfigurationMangerTests._jsonOptions);
			Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\"}", json);
		}
	}

	public class ColorSchemeJsonConverterTests {
		//string json = @"
		//	{
		//	""ColorSchemes"": {
		//		""Base"": {
		//			""normal"": {
		//				""foreground"": ""White"",
		//				""background"": ""Blue""
		//   		            },
		//			""focus"": {
		//				""foreground"": ""Black"",
		//				""background"": ""Gray""
		//			    },
		//			""hotNormal"": {
		//				""foreground"": ""BrightCyan"",
		//				""background"": ""Blue""
		//			    },
		//			""hotFocus"": {
		//				""foreground"": ""BrightBlue"",
		//				""background"": ""Gray""
		//			    },
		//			""disabled"": {
		//				""foreground"": ""DarkGray"",
		//				""background"": ""Blue""
		//			    }
		//		}
		//		}
		//	}";
		[Fact, AutoInitShutdown]
		public void TestColorSchemeSerialization ()
		{
			// Arrange
			var expectedColorScheme = new ColorScheme {
				Normal = Attribute.Make (Color.White, Color.Blue),
				Focus = Attribute.Make (Color.Black, Color.Gray),
				HotNormal = Attribute.Make (Color.BrightCyan, Color.Blue),
				HotFocus = Attribute.Make (Color.BrightBlue, Color.Gray),
				Disabled = Attribute.Make (Color.DarkGray, Color.Blue)
			};
			var serializedColorScheme = JsonSerializer.Serialize<ColorScheme> (expectedColorScheme, ConfigurationMangerTests._jsonOptions);

			// Act
			var actualColorScheme = JsonSerializer.Deserialize<ColorScheme> (serializedColorScheme, ConfigurationMangerTests._jsonOptions);

			// Assert
			Assert.Equal (expectedColorScheme, actualColorScheme);
		}
	}

	public class KeyJsonConverterTests {
		[Theory, AutoInitShutdown]
		[InlineData (Key.A, "A")]
		[InlineData (Key.a | Key.ShiftMask, "a, ShiftMask")]
		[InlineData (Key.A | Key.CtrlMask, "A, CtrlMask")]
		[InlineData (Key.a | Key.AltMask | Key.CtrlMask, "a, CtrlMask, AltMask")]
		[InlineData (Key.Delete | Key.AltMask | Key.CtrlMask, "Delete, CtrlMask, AltMask")]
		[InlineData (Key.D4, "D4")]
		[InlineData (Key.Esc, "Esc")]
		public void TestKeyRoundTripConversion (Key key, string expectedStringTo)
		{
			// Arrange
			var options = new JsonSerializerOptions ();
			options.Converters.Add (new KeyJsonConverter ());

			// Act
			var json = JsonSerializer.Serialize (key, options);
			var deserializedKey = JsonSerializer.Deserialize<Key> (json, options);

			// Assert
			Assert.Equal (expectedStringTo, deserializedKey.ToString());
		}
	}
}

