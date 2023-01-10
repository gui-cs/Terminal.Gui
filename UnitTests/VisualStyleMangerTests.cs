using System;
using System.Linq;
using System.Text.Json;
using Xunit;


namespace Terminal.Gui.Core {
	public class VisualStyleMangerTests {

		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter ()
				}
		};

		public VisualStyleMangerTests ()
		{
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
			configuration.VisualStyles.ColorSchemes = Colors.ColorSchemes;
			var json = ConfigurationManager.ToJson (configuration);

			var readConfig = ConfigurationManager.LoadFromJson (json);

			Assert.Equal (Colors.Base.Normal, readConfig.VisualStyles.ColorSchemes ["Base"].Normal);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerInitDriver ()
		{
			var configuration = new Configuration ();
			configuration.VisualStyles.ColorSchemes = Colors.ColorSchemes;

			// Change Base
			var json = ConfigurationManager.ToJson (configuration);

			var readConfig = ConfigurationManager.LoadFromJson (json);
			Assert.Equal (Colors.Base, readConfig.VisualStyles.ColorSchemes ["Base"]);
			Assert.Equal (Colors.TopLevel, readConfig.VisualStyles.ColorSchemes ["TopLevel"]);
			Assert.Equal (Colors.Error, readConfig.VisualStyles.ColorSchemes ["Error"]);
			Assert.Equal (Colors.Dialog, readConfig.VisualStyles.ColorSchemes ["Dialog"]);
			Assert.Equal (Colors.Menu, readConfig.VisualStyles.ColorSchemes ["Menu"]);

			Colors.Base = readConfig.VisualStyles.ColorSchemes ["Base"];
			Colors.TopLevel = readConfig.VisualStyles.ColorSchemes ["TopLevel"];
			Colors.Error = readConfig.VisualStyles.ColorSchemes ["Error"];
			Colors.Dialog = readConfig.VisualStyles.ColorSchemes ["Dialog"];
			Colors.Menu = readConfig.VisualStyles.ColorSchemes ["Menu"];

			Assert.Equal (readConfig.VisualStyles.ColorSchemes ["Base"], Colors.Base);
			Assert.Equal (readConfig.VisualStyles.ColorSchemes ["TopLevel"], Colors.TopLevel);
			Assert.Equal (readConfig.VisualStyles.ColorSchemes ["Error"], Colors.Error);
			Assert.Equal (readConfig.VisualStyles.ColorSchemes ["Dialog"], Colors.Dialog);
			Assert.Equal (readConfig.VisualStyles.ColorSchemes ["Menu"], Colors.Menu);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerLoadsFromJson ()
		{
			// Arrange
			string json = @"
			{
			""VisualStyles"": {
			""ColorSchemes"": {
				""Base"": {
					""NORMAL"": {
						""FOREGROUND"": ""WHITE"",
						""background"": ""blue""
	    					    },
					""focus"": {
						""Foreground"": ""bLAck"",
						""Background"": ""Gray""
					    },
					""hotNormal"": {
						""foreground"": ""BrightCyan"",
						""background"": ""Blue""
					    },
					""HotFocus"": {
						""foreground"": ""BrightBlue"",
						""background"": ""Gray""
					    },
					""disabled"": {
						""foreground"": ""DarkGray"",
						""background"": ""Blue""
					    }
				},
				""TopLevel"": {
					""NORMAL"": {
						""FOREGROUND"": ""WHITE"",
						""background"": ""blue""
	    					    },
					""focus"": {
						""Foreground"": ""bLAck"",
						""Background"": ""Gray""
					    },
					""hotNormal"": {
						""foreground"": ""BrightCyan"",
						""background"": ""Blue""
					    },
					""HotFocus"": {
						""foreground"": ""BrightBlue"",
						""background"": ""Gray""
					    },
					""disabled"": {
						""foreground"": ""DarkGray"",
						""background"": ""Blue""
					    }
				},
				""UserDefined"": {
					""NORMAL"": {
						""FOREGROUND"": ""red"",
						""background"": ""white""
	    					    },
					""focus"": {
						""Foreground"": ""blue"",
						""Background"": ""green""
					    },
					""hotNormal"": {
						""foreground"": ""brightyellow"",
						""background"": ""gray""
					    },
					""HotFocus"": {
						""foreground"": ""darkgray"",
						""background"": ""black""
					    },
					""disabled"": {
						""foreground"": ""cyan"",
						""background"": ""BrightCyan""
					    }
				}
				}
				}
			}";

			var configuration = ConfigurationManager.LoadFromJson (json);

			Assert.Equal (Color.White, configuration.VisualStyles.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, configuration.VisualStyles.ColorSchemes ["Base"].Normal.Background);

			Assert.Equal (Color.Black, configuration.VisualStyles.ColorSchemes ["Base"].Focus.Foreground);
			Assert.Equal (Color.Gray, configuration.VisualStyles.ColorSchemes ["Base"].Focus.Background);

			Assert.Equal (Color.BrightCyan, configuration.VisualStyles.ColorSchemes ["Base"].HotNormal.Foreground);
			Assert.Equal (Color.Blue, configuration.VisualStyles.ColorSchemes ["Base"].HotNormal.Background);

			Assert.Equal (Color.BrightBlue, configuration.VisualStyles.ColorSchemes ["Base"].HotFocus.Foreground);
			Assert.Equal (Color.Gray, configuration.VisualStyles.ColorSchemes ["Base"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, configuration.VisualStyles.ColorSchemes ["Base"].Disabled.Foreground);
			Assert.Equal (Color.Blue, configuration.VisualStyles.ColorSchemes ["Base"].Disabled.Background);

			Assert.Equal (Color.White, configuration.VisualStyles.ColorSchemes ["TopLevel"].Normal.Foreground);
			Assert.Equal (Color.Blue, configuration.VisualStyles.ColorSchemes ["TopLevel"].Normal.Background);

			Assert.Equal (Color.Black, configuration.VisualStyles.ColorSchemes ["TopLevel"].Focus.Foreground);
			Assert.Equal (Color.Gray, configuration.VisualStyles.ColorSchemes ["TopLevel"].Focus.Background);

			Assert.Equal (Color.BrightCyan, configuration.VisualStyles.ColorSchemes ["TopLevel"].HotNormal.Foreground);
			Assert.Equal (Color.Blue, configuration.VisualStyles.ColorSchemes ["TopLevel"].HotNormal.Background);

			Assert.Equal (Color.BrightBlue, configuration.VisualStyles.ColorSchemes ["TopLevel"].HotFocus.Foreground);
			Assert.Equal (Color.Gray, configuration.VisualStyles.ColorSchemes ["TopLevel"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, configuration.VisualStyles.ColorSchemes ["TopLevel"].Disabled.Foreground);
			Assert.Equal (Color.Blue, configuration.VisualStyles.ColorSchemes ["TopLevel"].Disabled.Background);

			// User defined color scheme
			Assert.Equal (Color.Red, configuration.VisualStyles.ColorSchemes ["UserDefined"].Normal.Foreground);
			Assert.Equal (Color.White, configuration.VisualStyles.ColorSchemes ["UserDefined"].Normal.Background);

			Assert.Equal (Color.Blue, configuration.VisualStyles.ColorSchemes ["UserDefined"].Focus.Foreground);
			Assert.Equal (Color.Green, configuration.VisualStyles.ColorSchemes ["UserDefined"].Focus.Background);

			Assert.Equal (Color.BrightYellow, configuration.VisualStyles.ColorSchemes ["UserDefined"].HotNormal.Foreground);
			Assert.Equal (Color.Gray, configuration.VisualStyles.ColorSchemes ["UserDefined"].HotNormal.Background);

			Assert.Equal (Color.DarkGray, configuration.VisualStyles.ColorSchemes ["UserDefined"].HotFocus.Foreground);
			Assert.Equal (Color.Black, configuration.VisualStyles.ColorSchemes ["UserDefined"].HotFocus.Background);

			Assert.Equal (Color.Cyan, configuration.VisualStyles.ColorSchemes ["UserDefined"].Disabled.Foreground);
			Assert.Equal (Color.BrightCyan, configuration.VisualStyles.ColorSchemes ["UserDefined"].Disabled.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerLoadInvalidJsonAsserts ()
		{
			// Arrange
			string json = @"
			{
			""VisualStyles"": {
			""ColorSchemes"": {
				""UserDefined"": {
					""hotNormal"": {
						""foreground"": ""yellow"",
						""background"": ""1234""
					    }
				}
				}
				}
			}";

			JsonException jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.LoadFromJson (json));
			Assert.Equal ("Invalid color string: 'yellow'", jsonException.Message);

			json = @"
			{
			""VisualStyles"": {
			""ColorSchemes"": {
				""UserDefined"": {
					""AbNormal"": {
						""FOREGROUND"": ""red"",
						""background"": ""white""
	    					    }
				}
				}
				}
			}";

			jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.LoadFromJson (json));
			Assert.Equal ("Unrecognized property name: AbNormal", jsonException.Message);

			// Modify hotNormal background only 
			json = @"
			{
			""VisualStyles"": {
			""ColorSchemes"": {
				""Error"": {
					""Normal"": {
						""background"": ""Cyan""
					    }
				}
				}
				}
			}";

			jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.LoadFromJson (json));
			Assert.Equal ("Both Foreground and Background colors must be provided.", jsonException.Message);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerSaveDefaults ()
		{
			ConfigurationManager.SaveHardCodedConfig ("config.json");

			// Verify the saved file matches

		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerLoadDefaults ()
		{
			ConfigurationManager.LoadDefaults ();
			Assert.NotNull (ConfigurationManager.Config);

			// Apply default styles
			ConfigurationManager.Config.VisualStyles.Apply ();

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
			ConfigurationManager.LoadDefaults ();
			Assert.NotNull (ConfigurationManager.Config);

			// Apply default styles
			ConfigurationManager.Config.VisualStyles.Apply ();

			// Prove Base is defaults (White, Blue)
			Assert.Equal (Color.White, ConfigurationManager.Config.VisualStyles.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, ConfigurationManager.Config.VisualStyles.ColorSchemes ["Base"].Normal.Background);

			// Modify hotNormal
			string json = @"
			{
			""VisualStyles"": {
				""ColorSchemes"": {
						""Error"": {
							""Normal"": {
								""foreground"": ""gray"",
								""background"": ""DarkGray""
								}
						}
					}
				}
			}";

			ConfigurationManager.UpdateConfiguration (json);
			ConfigurationManager.Config.VisualStyles.Apply ();

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
			Color actualColor = JsonSerializer.Deserialize<Color> (json, VisualStyleMangerTests._jsonOptions);

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
			var attribute = JsonSerializer.Deserialize<Attribute> (json, VisualStyleMangerTests._jsonOptions);
			Assert.Equal (Color.Blue, attribute.Foreground);
			Assert.Equal (Color.Green, attribute.Background);

			// Test deserializing from RGB values
			json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
			attribute = JsonSerializer.Deserialize<Attribute> (json, VisualStyleMangerTests._jsonOptions);
			Assert.Equal (Color.BrightRed, attribute.Foreground);
			Assert.Equal (Color.BrightGreen, attribute.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestSerialize ()
		{
			// Test serializing to human-readable color names
			var attribute = new Attribute (Color.Blue, Color.Green);
			var json = JsonSerializer.Serialize<Attribute> (attribute, VisualStyleMangerTests._jsonOptions);
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
			var serializedColorScheme = JsonSerializer.Serialize<ColorScheme> (expectedColorScheme, VisualStyleMangerTests._jsonOptions);

			// Act
			var actualColorScheme = JsonSerializer.Deserialize<ColorScheme> (serializedColorScheme, VisualStyleMangerTests._jsonOptions);

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

