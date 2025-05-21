using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeJsonConverterTests
{
    public static readonly JsonSerializerOptions JsonOptions = new ()
    {
        Converters = { new AttributeJsonConverter (), new ColorJsonConverter (), new SchemeJsonConverter () }
    };

    //string json = @"
    //	{
    //	""Schemes"": {
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
    public void TestSchemesSerialization_Equality ()
    {
        // Arrange
        var expectedScheme = new Scheme
        {
            Normal = new Attribute (Color.White, Color.Blue),
            Focus = new Attribute (Color.Black, Color.Gray),
            HotNormal = new Attribute (Color.BrightCyan, Color.Blue),
            HotFocus = new Attribute (Color.BrightBlue, Color.Gray),
            Disabled = new Attribute (Color.DarkGray, Color.Blue)
        };

        string serializedScheme =
            JsonSerializer.Serialize (expectedScheme, JsonOptions);

        // Act
        var actualScheme =
            JsonSerializer.Deserialize<Scheme> (serializedScheme, JsonOptions);

        // Assert
        Assert.Equal (expectedScheme, actualScheme);
    }

    [Fact]
    public void TestSchemesSerialization ()
    {
        var expected = new Scheme
        {
            Normal = new (Color.White, Color.Blue),
            Focus = new (Color.Black, Color.Gray),
            HotNormal = new (Color.BrightCyan, Color.Blue),
            HotFocus = new (Color.BrightBlue, Color.Gray),
            Disabled = new (Color.DarkGray, Color.Blue)
        };

        string json = JsonSerializer.Serialize (expected, JsonOptions);
        Scheme actual = JsonSerializer.Deserialize<Scheme> (json, JsonOptions);

        Assert.NotNull (actual);

        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            Attribute expectedAttr = expected.GetAttributeForRole (role);
            Attribute actualAttr = actual.GetAttributeForRole (role);

            Assert.Equal (expectedAttr, actualAttr);
        }
    }

    [Fact]
    public void Deserialized_Attributes_AreExplicitlySet ()
    {
        const string json = """
                            {
                              "Normal": { "Foreground": "White", "Background": "Blue" },
                              "Focus": { "Foreground": "Black", "Background": "Gray" },
                              "HotNormal": { "Foreground": "BrightCyan", "Background": "Blue" },
                              "HotFocus": { "Foreground": "BrightBlue", "Background": "Gray" },
                              "Disabled": { "Foreground": "DarkGray", "Background": "Blue" }
                            }
                            """;

        Scheme scheme = JsonSerializer.Deserialize<Scheme> (json, JsonOptions)!;

        Assert.True (scheme.Normal.IsExplicitlySet, "Normal should be explicitly set");
        Assert.True (scheme.Focus.IsExplicitlySet, "Focus should be explicitly set");
        Assert.True (scheme.HotNormal.IsExplicitlySet, "HotNormal should be explicitly set");
        Assert.True (scheme.HotFocus.IsExplicitlySet, "HotFocus should be explicitly set");
        Assert.True (scheme.Disabled.IsExplicitlySet, "Disabled should be explicitly set");
    }

    [Fact]
    public void Deserialized_Attributes_NotSpecified_AreImplicit ()
    {
        const string json = """
                            {
                              "Normal": { "Foreground": "White", "Background": "Black" }
                            }
                            """;

        Scheme scheme = JsonSerializer.Deserialize<Scheme> (json, JsonOptions)!;

        // explicitly set
        Assert.True (scheme.Normal.IsExplicitlySet);

        // derived from Normal
        Assert.False (scheme.Focus.IsExplicitlySet);
        Assert.False (scheme.HotNormal.IsExplicitlySet);
        Assert.False (scheme.HotFocus.IsExplicitlySet);
        Assert.False (scheme.Disabled.IsExplicitlySet);
    }

}