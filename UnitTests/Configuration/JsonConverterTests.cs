using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Terminal.Gui.ConfigurationTests;

public class ColorJsonConverterTests
{
    [Theory]
    [InlineData ("\"#000000\"", 0, 0, 0)]
    public void DeserializesFromHexCode (string hexCode, int r, int g, int b)
    {
        // Arrange
        var expected = new Color (r, g, b);

        // Act
        var actual = JsonSerializer.Deserialize<Color> (
                                                        hexCode,
                                                        new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                       );

        //Assert
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("\"rgb(0,0,0)\"", 0, 0, 0)]
    public void DeserializesFromRgb (string rgb, int r, int g, int b)
    {
        // Arrange
        var expected = new Color (r, g, b);

        // Act
        var actual = JsonSerializer.Deserialize<Color> (
                                                        rgb,
                                                        new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                       );

        //Assert
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (ColorName.Black, "Black")]
    [InlineData (ColorName.Blue, "Blue")]
    [InlineData (ColorName.Green, "Green")]
    [InlineData (ColorName.Cyan, "Cyan")]
    [InlineData (ColorName.Gray, "Gray")]
    [InlineData (ColorName.Red, "Red")]
    [InlineData (ColorName.Magenta, "Magenta")]
    [InlineData (ColorName.Yellow, "Yellow")]
    [InlineData (ColorName.DarkGray, "DarkGray")]
    [InlineData (ColorName.BrightBlue, "BrightBlue")]
    [InlineData (ColorName.BrightGreen, "BrightGreen")]
    [InlineData (ColorName.BrightCyan, "BrightCyan")]
    [InlineData (ColorName.BrightRed, "BrightRed")]
    [InlineData (ColorName.BrightMagenta, "BrightMagenta")]
    [InlineData (ColorName.BrightYellow, "BrightYellow")]
    [InlineData (ColorName.White, "White")]
    public void SerializesEnumValuesAsStrings (ColorName colorName, string expectedJson)
    {
        var converter = new ColorJsonConverter ();
        var options = new JsonSerializerOptions { Converters = { converter } };

        string serialized = JsonSerializer.Serialize (new Color (colorName), options);

        Assert.Equal ($"\"{expectedJson}\"", serialized);
    }

    [Theory]
    [InlineData (0, 0, 0, "\"#000000\"")]
    [InlineData (0, 0, 1, "\"#000001\"")]
    public void SerializesToHexCode (int r, int g, int b, string expected)
    {
        // Arrange

        // Act
        string actual = JsonSerializer.Serialize (
                                                  new Color (r, g, b),
                                                  new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                 );

        //Assert
        Assert.Equal (expected, actual);
    }

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
    public void TestColorDeserializationFromHumanReadableColorNames (string colorName, ColorName expectedColor)
    {
        // Arrange
        var json = $"\"{colorName}\"";

        // Act
        var actualColor = JsonSerializer.Deserialize<Color> (json, ConfigurationManagerTests._jsonOptions);

        // Assert
        Assert.Equal (new Color (expectedColor), actualColor);
    }

    [Fact]
    public void TestDeserializeColor_Black ()
    {
        // Arrange
        var json = "\"Black\"";
        var expectedColor = new Color (ColorName.Black);

        // Act
        var color = JsonSerializer.Deserialize<Color> (
                                                       json,
                                                       new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                      );

        // Assert
        Assert.Equal (expectedColor, color);
    }

    [Fact]
    public void TestDeserializeColor_BrightRed ()
    {
        // Arrange
        var json = "\"BrightRed\"";
        var expectedColor = new Color (ColorName.BrightRed);

        // Act
        var color = JsonSerializer.Deserialize<Color> (
                                                       json,
                                                       new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                      );

        // Assert
        Assert.Equal (expectedColor, color);
    }

    [Fact]
    public void TestSerializeColor_Black ()
    {
        // Arrange
        var expectedJson = "\"Black\"";

        // Act
        string json = JsonSerializer.Serialize (
                                                new Color (Color.Black),
                                                new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                               );

        // Assert
        Assert.Equal (expectedJson, json);
    }

    [Fact]
    public void TestSerializeColor_BrightRed ()
    {
        // Arrange
        var expectedJson = "\"BrightRed\"";

        // Act
        string json = JsonSerializer.Serialize (
                                                new Color (Color.BrightRed),
                                                new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                               );

        // Assert
        Assert.Equal (expectedJson, json);
    }
}

public class AttributeJsonConverterTests
{
    [Fact]
    public void TestDeserialize ()
    {
        // Test deserializing from human-readable color names
        var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\"}";
        var attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
        Assert.Equal (Color.Blue, attribute.Foreground.GetClosestNamedColor ());
        Assert.Equal (Color.Green, attribute.Background.GetClosestNamedColor ());

        // Test deserializing from RGB values
        json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
        attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
        Assert.Equal (Color.Red, attribute.Foreground.GetClosestNamedColor ());
        Assert.Equal (Color.BrightGreen, attribute.Background.GetClosestNamedColor ());
    }

    [Fact]
    [AutoInitShutdown]
    public void TestSerialize ()
    {
        // Test serializing to human-readable color names
        var attribute = new Attribute (Color.Blue, Color.Green);
        string json = JsonSerializer.Serialize (attribute, ConfigurationManagerTests._jsonOptions);
        Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\"}", json);
    }
}

public class ColorSchemeJsonConverterTests
{
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
    [Fact]
    [AutoInitShutdown]
    public void TestColorSchemesSerialization ()
    {
        // Arrange
        var expectedColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.White, Color.Blue),
            Focus = new Attribute (Color.Black, Color.Gray),
            HotNormal = new Attribute (Color.BrightCyan, Color.Blue),
            HotFocus = new Attribute (Color.BrightBlue, Color.Gray),
            Disabled = new Attribute (Color.DarkGray, Color.Blue)
        };

        string serializedColorScheme =
            JsonSerializer.Serialize (expectedColorScheme, ConfigurationManagerTests._jsonOptions);

        // Act
        var actualColorScheme =
            JsonSerializer.Deserialize<ColorScheme> (serializedColorScheme, ConfigurationManagerTests._jsonOptions);

        // Assert
        Assert.Equal (expectedColorScheme, actualColorScheme);
    }
}

public class KeyCodeJsonConverterTests
{
    [Theory]
    [InlineData (KeyCode.A, "A")]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, "A, ShiftMask")]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, "A, CtrlMask")]
    [InlineData (KeyCode.A | KeyCode.AltMask | KeyCode.CtrlMask, "A, CtrlMask, AltMask")]
    [InlineData ((KeyCode)'a' | KeyCode.AltMask | KeyCode.CtrlMask, "Space, A, CtrlMask, AltMask")]
    [InlineData ((KeyCode)'a' | KeyCode.ShiftMask, "Space, A, ShiftMask")]
    [InlineData (KeyCode.Delete | KeyCode.AltMask | KeyCode.CtrlMask, "Delete, CtrlMask, AltMask")]
    [InlineData (KeyCode.D4, "D4")]
    [InlineData (KeyCode.Esc, "Esc")]
    public void TestKeyRoundTripConversion (KeyCode key, string expectedStringTo)
    {
        // Arrange
        var options = new JsonSerializerOptions ();
        options.Converters.Add (new KeyCodeJsonConverter ());

        // Act
        string json = JsonSerializer.Serialize (key, options);
        var deserializedKey = JsonSerializer.Deserialize<KeyCode> (json, options);

        // Assert
        Assert.Equal (expectedStringTo, deserializedKey.ToString ());
    }
}

public class KeyJsonConverterTests
{
    [Theory]
    [InlineData (KeyCode.A, "\"a\"")]
    [InlineData ((KeyCode)'â', "\"â\"")]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, "\"A\"")]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, "\"Ctrl+A\"")]
    [InlineData (KeyCode.A | KeyCode.AltMask | KeyCode.CtrlMask, "\"Ctrl+Alt+A\"")]
    [InlineData (KeyCode.Delete | KeyCode.AltMask | KeyCode.CtrlMask, "\"Ctrl+Alt+Delete\"")]
    [InlineData (KeyCode.D4, "\"4\"")]
    [InlineData (KeyCode.Esc, "\"Esc\"")]
    public void TestKey_Serialize (KeyCode key, string expected)
    {
        // Arrange
        var options = new JsonSerializerOptions ();
        options.Converters.Add (new KeyJsonConverter ());
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        // Act
        string json = JsonSerializer.Serialize ((Key)key, options);

        // Assert
        Assert.Equal (expected, json);
    }

    [Theory]
    [InlineData (KeyCode.A, "a")]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, "A")]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, "Ctrl+A")]
    [InlineData (KeyCode.A | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt+A")]
    [InlineData (KeyCode.Delete | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt+Delete")]
    [InlineData (KeyCode.D4, "4")]
    [InlineData (KeyCode.Esc, "Esc")]
    public void TestKeyRoundTripConversion (KeyCode key, string expectedStringTo)
    {
        // Arrange
        var options = new JsonSerializerOptions ();
        options.Converters.Add (new KeyJsonConverter ());
        var encoderSettings = new TextEncoderSettings ();
        encoderSettings.AllowCharacters ('+', '-');
        encoderSettings.AllowRange (UnicodeRanges.BasicLatin);
        options.Encoder = JavaScriptEncoder.Create (encoderSettings);

        // Act
        string json = JsonSerializer.Serialize ((Key)key, options);
        var deserializedKey = JsonSerializer.Deserialize<Key> (json, options);

        // Assert
        Assert.Equal (expectedStringTo, deserializedKey.ToString ());
    }
}
