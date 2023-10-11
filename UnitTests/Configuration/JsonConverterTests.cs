using System.Text.Json;
using Xunit;

namespace Terminal.Gui.ConfigurationTests {
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
		[InlineData ("Yellow", Color.Yellow)]
		[InlineData ("Cyan", Color.Cyan)]
		[InlineData ("DarkGray", Color.DarkGray)]
		[InlineData ("Gray", Color.Gray)]
		[InlineData ("Green", Color.Green)]
		[InlineData ("Magenta", Color.Magenta)]
		[InlineData ("Red", Color.Red)]
		[InlineData ("White", Color.White)]
		public void TestColorDeserializationFromHumanReadableColorNames (string colorName, ColorNames expectedColor)
		{
			// Arrange
			string json = $"\"{colorName}\"";

			// Act
			Color actualColor = JsonSerializer.Deserialize<Color> (json, ConfigurationManagerTests._jsonOptions);

			// Assert
			Assert.Equal ((Color)expectedColor, actualColor);
		}

		[Theory]
		[InlineData (ColorNames.Black, "Black")]
		[InlineData (ColorNames.Blue, "Blue")]
		[InlineData (ColorNames.Green, "Green")]
		[InlineData (ColorNames.Cyan, "Cyan")]
		[InlineData (ColorNames.Gray, "Gray")]
		[InlineData (ColorNames.Red, "Red")]
		[InlineData (ColorNames.Magenta, "Magenta")]
		[InlineData (ColorNames.Yellow, "Yellow")]
		[InlineData (ColorNames.DarkGray, "DarkGray")]
		[InlineData (ColorNames.BrightBlue, "BrightBlue")]
		[InlineData (ColorNames.BrightGreen, "BrightGreen")]
		[InlineData (ColorNames.BrightCyan, "BrightCyan")]
		[InlineData (ColorNames.BrightRed, "BrightRed")]
		[InlineData (ColorNames.BrightMagenta, "BrightMagenta")]
		[InlineData (ColorNames.BrightYellow, "BrightYellow")]
		[InlineData (ColorNames.White, "White")]
		public void SerializesEnumValuesAsStrings (ColorNames colorName, string expectedJson)
		{
			var converter = new ColorJsonConverter ();
			var options = new JsonSerializerOptions { Converters = { converter } };

			var serialized = JsonSerializer.Serialize<Color> ((Color)colorName, options);

			Assert.Equal ($"\"{expectedJson}\"", serialized);
		}

		[Fact]
		public void TestSerializeColor_Black ()
		{
			// Arrange
			var expectedJson = "\"Black\"";

			// Act
			var json = JsonSerializer.Serialize<Color> ((Color)Color.Black, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedJson, json);
		}

		[Fact]
		public void TestSerializeColor_BrightRed ()
		{
			// Arrange
			var expectedJson = "\"BrightRed\"";

			// Act
			var json = JsonSerializer.Serialize<Color> ((Color)Color.BrightRed, new JsonSerializerOptions {
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
			var expectedColor = new Color (ColorNames.Black);

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
			var expectedColor = new Color (ColorNames.BrightRed);

			// Act
			var color = JsonSerializer.Deserialize<Color> (json, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedColor, color);
		}

		[Theory]
		[InlineData (0, 0, 0, "\"#000000\"")]
		[InlineData (0, 0, 1, "\"#000001\"")]
		public void SerializesToHexCode (int r, int g, int b, string expected)
		{
			// Arrange

			// Act
			var actual = JsonSerializer.Serialize (new Color (r, g, b), new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			//Assert
			Assert.Equal (expected, actual);

		}

		[Theory]
		[InlineData ("\"#000000\"", 0, 0, 0)]
		public void DeserializesFromHexCode (string hexCode, int r, int g, int b)
		{
			// Arrange
			Color expected = new Color (r, g, b);

			// Act
			var actual = JsonSerializer.Deserialize<Color> (hexCode, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			//Assert
			Assert.Equal (expected, actual);
		}

		[Theory]
		[InlineData ("\"rgb(0,0,0)\"", 0, 0, 0)]
		public void DeserializesFromRgb (string rgb, int r, int g, int b)
		{
			// Arrange
			Color expected = new Color (r, g, b);

			// Act
			var actual = JsonSerializer.Deserialize<Color> (rgb, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			//Assert
			Assert.Equal (expected, actual);
		}
	}

	public class AttributeJsonConverterTests {
		[Fact, AutoInitShutdown]
		public void TestDeserialize ()
		{
			// Test deserializing from human-readable color names
			var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\"}";
			var attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
			Assert.Equal (Color.Blue, attribute.Foreground.ColorName);
			Assert.Equal (Color.Green, attribute.Background.ColorName);

			// Test deserializing from RGB values
			json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
			attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
			Assert.Equal (Color.Red, attribute.Foreground.ColorName);
			Assert.Equal (Color.BrightGreen, attribute.Background.ColorName);
		}

		[Fact, AutoInitShutdown]
		public void TestSerialize ()
		{
			// Test serializing to human-readable color names
			var attribute = new Attribute (Color.Blue, Color.Green);
			var json = JsonSerializer.Serialize<Attribute> (attribute, ConfigurationManagerTests._jsonOptions);
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
		public void TestColorSchemesSerialization ()
		{
			// Arrange
			var expectedColorScheme = new ColorScheme {
				Normal = new Attribute (Color.White, Color.Blue),
				Focus = new Attribute (Color.Black, Color.Gray),
				HotNormal = new Attribute (Color.BrightCyan, Color.Blue),
				HotFocus = new Attribute (Color.BrightBlue, Color.Gray),
				Disabled = new Attribute (Color.DarkGray, Color.Blue)
			};
			var serializedColorScheme = JsonSerializer.Serialize<ColorScheme> (expectedColorScheme, ConfigurationManagerTests._jsonOptions);

			// Act
			var actualColorScheme = JsonSerializer.Deserialize<ColorScheme> (serializedColorScheme, ConfigurationManagerTests._jsonOptions);

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
			Assert.Equal (expectedStringTo, deserializedKey.ToString ());
		}
	}
}