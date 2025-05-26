#nullable enable
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class ConfigPropertyTests
{
    [Fact]
    public void Apply_PropertyValueIsAppliedToStatic_String_Property ()
    {
        // Arrange
        TestConfiguration.Reset ();
        var propertyInfo = typeof (TestConfiguration).GetProperty (nameof (TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "UpdatedValue"
        };

        // Act
        var result = configProperty.Apply ();

        // Assert
        Assert.Equal (1, TestConfiguration.TestStringPropertySetCount);
        Assert.True (result);
        Assert.Equal ("UpdatedValue", TestConfiguration.TestStringProperty);
        TestConfiguration.Reset ();
    }

    [Fact]
    public void Apply_PropertyValueIsAppliedToStatic_Key_Property ()
    {
        // Arrange
        TestConfiguration.Reset ();
        var propertyInfo = typeof (TestConfiguration).GetProperty (nameof (TestConfiguration.TestKeyProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = Key.Q.WithCtrl
        };

        // Act
        var result = configProperty.Apply ();

        // Assert
        Assert.Equal (1, TestConfiguration.TestKeyPropertySetCount);
        Assert.True (result);
        Assert.Equal (Key.Q.WithCtrl, TestConfiguration.TestKeyProperty);
        TestConfiguration.Reset ();
    }

    [Fact]
    public void RetrieveValue_GetsCurrentValueOfStaticProperty ()
    {
        // Arrange
        TestConfiguration.TestStringProperty = "CurrentValue";
        var propertyInfo = typeof (TestConfiguration).GetProperty (nameof (TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo
        };

        // Act
        var value = configProperty.UpdateToCurrentValue ();

        // Assert
        Assert.Equal ("CurrentValue", value);
        Assert.Equal ("CurrentValue", configProperty.PropertyValue);
    }

    [Fact]
    public void DeepCloneFrom_Updates_String_Property_Value ()
    {
        // Arrange
        TestConfiguration.Reset ();
        var propertyInfo = typeof (TestConfiguration).GetProperty (nameof (TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "InitialValue"
        };

        // Act
        var updatedValue = configProperty.UpdateFrom ("NewValue");

        // Assert
        Assert.Equal (0, TestConfiguration.TestStringPropertySetCount);
        Assert.Equal ("NewValue", updatedValue);
        Assert.Equal ("NewValue", configProperty.PropertyValue);
        TestConfiguration.Reset ();
    }

    //[Fact]
    //public void UpdateValueFrom_InvalidType_ThrowsArgumentException()
    //{
    //    // Arrange
    //    var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
    //    var configProperty = new ConfigProperty
    //    {
    //        PropertyInfo = propertyInfo
    //    };

    //    // Act & Assert
    //    Assert.Throws<ArgumentException>(() => configProperty.UpdateValueFrom(123));
    //}

    [Fact]
    public void Apply_TargetInvocationException_ThrowsJsonException ()
    {
        // Arrange
        var propertyInfo = typeof (TestConfiguration).GetProperty (nameof (TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = null // This will cause ArgumentNullException in the set accessor
        };

        // Act & Assert
        var exception = Assert.Throws<JsonException> (() => configProperty.Apply ());
    }

    [Fact]
    public void GetJsonPropertyName_ReturnsJsonPropertyNameAttributeValue ()
    {
        // Arrange
        var propertyInfo = typeof (TestConfiguration).GetProperty (nameof (TestConfiguration.TestStringProperty));

        // Act
        var jsonPropertyName = ConfigProperty.GetJsonPropertyName (propertyInfo!);

        // Assert
        Assert.Equal ("TestStringProperty", jsonPropertyName);
    }
    [Fact]
    public void UpdateFrom_NullSource_ReturnsExistingValue()
    {
        // Arrange
        TestConfiguration.TestStringProperty = "CurrentValue";
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "ExistingValue"
        };

        // Act
        var result = configProperty.UpdateFrom(null);

        // Assert
        Assert.Equal("ExistingValue", result);
        Assert.Equal("ExistingValue", configProperty.PropertyValue);
    }

    [Fact]
    public void UpdateFrom_InvalidType_ThrowsArgumentException()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "ExistingValue"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => configProperty.UpdateFrom(123));
    }

    [Fact]
    public void UpdateFrom_ConfigPropertySource_CopiesValue()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "ExistingValue"
        };

        var sourceConfigProperty = new ConfigProperty
        {
            PropertyValue = "SourceValue",
            HasValue = true
        };

        // Act
        var result = configProperty.UpdateFrom(sourceConfigProperty);

        // Assert
        Assert.Equal("SourceValue", result);
        Assert.Equal("SourceValue", configProperty.PropertyValue);
    }

    [Fact]
    public void UpdateFrom_ConfigPropertySource_WithoutValue_KeepsExistingValue()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "ExistingValue"
        };

        var sourceConfigProperty = new ConfigProperty
        {
            HasValue = false
        };

        // Act
        var result = configProperty.UpdateFrom(sourceConfigProperty);

        // Assert
        Assert.Equal("ExistingValue", result);
        Assert.Equal("ExistingValue", configProperty.PropertyValue);
    }

    [Fact]
    public void UpdateFrom_ConcurrentDictionaryOfThemeScopes_UpdatesValues()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestDictionaryProperty));

        // Create a destination dictionary
        var destinationDict = new ConcurrentDictionary<string, ThemeScope>(StringComparer.InvariantCultureIgnoreCase);
        destinationDict.TryAdd("theme1", new ThemeScope());

        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = destinationDict
        };

        // Create a source dictionary with one matching key and one new key
        var sourceDict = new ConcurrentDictionary<string, ThemeScope>(StringComparer.InvariantCultureIgnoreCase);
        var sourceTheme1 = new ThemeScope();
        var sourceTheme2 = new ThemeScope();

        // Add a property to sourceTheme1 to verify it gets updated
        var keyProperty = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestKeyProperty));
        if (sourceTheme1.TryAdd("TestKey", new ConfigProperty { PropertyInfo = keyProperty, PropertyValue = Key.A.WithCtrl, HasValue = true }))
        {
            // Successfully added
        }

        sourceDict.TryAdd("theme1", sourceTheme1);
        sourceDict.TryAdd("theme2", sourceTheme2);

        // Act
        var result = configProperty.UpdateFrom(sourceDict);

        // Assert
        var resultDict = result as ConcurrentDictionary<string, ThemeScope>;
        Assert.NotNull(resultDict);
        Assert.Equal(2, resultDict.Count);
        Assert.True(resultDict.ContainsKey("theme1"));
        Assert.True(resultDict.ContainsKey("theme2"));

        // Verify the theme1 was updated with the property
        Assert.True(resultDict["theme1"].ContainsKey("TestKey"));
        Assert.Equal(Key.A.WithCtrl, resultDict["theme1"]["TestKey"].PropertyValue);
    }

    [Fact]
    public void UpdateFrom_ConcurrentDictionaryOfConfigProperties_UpdatesValues()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestConfigDictionaryProperty));

        // Create a destination dictionary
        var destinationDict = new ConcurrentDictionary<string, ConfigProperty>(StringComparer.InvariantCultureIgnoreCase);
        destinationDict.TryAdd("prop1", new ConfigProperty { PropertyValue = "Original", HasValue = true });

        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = destinationDict
        };

        // Create a source dictionary with one matching key and one new key
        var sourceDict = new ConcurrentDictionary<string, ConfigProperty>(StringComparer.InvariantCultureIgnoreCase);
        sourceDict.TryAdd("prop1", new ConfigProperty { PropertyValue = "Updated", HasValue = true });
        sourceDict.TryAdd("prop2", new ConfigProperty { PropertyValue = "New", HasValue = true });

        // Act
        var result = configProperty.UpdateFrom(sourceDict);

        // Assert
        var resultDict = result as ConcurrentDictionary<string, ConfigProperty>;
        Assert.NotNull(resultDict);
        Assert.Equal(2, resultDict.Count);
        Assert.True(resultDict.ContainsKey("prop1"));
        Assert.True(resultDict.ContainsKey("prop2"));
        Assert.Equal("Updated", resultDict["prop1"].PropertyValue);
        Assert.Equal("New", resultDict["prop2"].PropertyValue);
    }

    [Fact]
    public void UpdateFrom_DictionaryOfConfigProperties_UpdatesValues()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestConfigDictionaryProperty));

        // Create a destination dictionary
        var destinationDict = new Dictionary<string, ConfigProperty>(StringComparer.InvariantCultureIgnoreCase);
        destinationDict.Add("prop1", new ConfigProperty { PropertyValue = "Original", HasValue = true });

        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = destinationDict
        };

        // Create a source dictionary with one matching key and one new key
        var sourceDict = new Dictionary<string, ConfigProperty>(StringComparer.InvariantCultureIgnoreCase);
        sourceDict.Add("prop1", new ConfigProperty { PropertyValue = "Updated", HasValue = true });
        sourceDict.Add("prop2", new ConfigProperty { PropertyValue = "New", HasValue = true });

        // Act
        var result = configProperty.UpdateFrom(sourceDict);

        // Assert
        var resultDict = result as Dictionary<string, ConfigProperty>;
        Assert.NotNull(resultDict);
        Assert.Equal(2, resultDict.Count);
        Assert.True(resultDict.ContainsKey("prop1"));
        Assert.True(resultDict.ContainsKey("prop2"));
        Assert.Equal("Updated", resultDict["prop1"].PropertyValue);
        Assert.Equal("New", resultDict["prop2"].PropertyValue);
    }

    [Fact]
    public void PropertyValue_SetWhenImmutable_ThrowsException()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            Immutable = true
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => configProperty.PropertyValue = "New Value");
        Assert.Contains("immutable", exception.Message);
    }

    [Fact]
    public void CreateWithAttributeInfo_ReturnsConfigPropertyWithCorrectValues()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));

        // Act
        var configProperty = ConfigProperty.CreateImmutableWithAttributeInfo(propertyInfo!);

        // Assert
        Assert.Equal(propertyInfo, configProperty.PropertyInfo);
        Assert.False(configProperty.OmitClassName);
        Assert.True(configProperty.Immutable);
        Assert.Null(configProperty.PropertyValue);
        Assert.False(configProperty.HasValue);
    }

    [Fact]
    public void HasConfigurationPropertyAttribute_ReturnsTrue_ForDecoratedProperty()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));

        // Act
        var result = ConfigProperty.HasConfigurationPropertyAttribute(propertyInfo!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasConfigurationPropertyAttribute_ReturnsFalse_ForNonDecoratedProperty()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringPropertySetCount));

        // Act
        var result = ConfigProperty.HasConfigurationPropertyAttribute(propertyInfo!);

        // Assert
        Assert.False(result);
    }
    public class TestConfiguration
    {
        private static string _testStringProperty = "Default";
        public static int TestStringPropertySetCount { get; set; }

        [ConfigurationProperty]
        public static string TestStringProperty
        {
            get => _testStringProperty;
            set
            {
                TestStringPropertySetCount++;
                _testStringProperty = value ?? throw new ArgumentNullException (nameof (value));
            }
        }

        private static Key _testKeyProperty = Key.Esc;
        public static int TestKeyPropertySetCount { get; set; }

        [ConfigurationProperty]
        public static Key TestKeyProperty
        {
            get => _testKeyProperty;
            set
            {
                TestKeyPropertySetCount++;
                _testKeyProperty = value ?? throw new ArgumentNullException (nameof (value));
            }
        }

        // Add these new properties for testing dictionaries
        [ConfigurationProperty]
        public static ConcurrentDictionary<string, ThemeScope>? TestDictionaryProperty { get; set; }

        [ConfigurationProperty]
        public static Dictionary<string, ConfigProperty>? TestRegularDictionaryProperty { get; set; }

        [ConfigurationProperty]
        public static ConcurrentDictionary<string, ConfigProperty>? TestConfigDictionaryProperty { get; set; }

        [ConfigurationProperty]
        public static Scheme? TestSchemeProperty { get; set; }


        public static void Reset ()
        {
            TestStringPropertySetCount = 0;
            TestKeyPropertySetCount = 0;
            TestDictionaryProperty = null;
            TestRegularDictionaryProperty = null;
            TestConfigDictionaryProperty = null;
            TestSchemeProperty = null;
        }
    }

    [Fact]
    public void UpdateFrom_SchemeSource_UpdatesValue ()
    {
        // Arrange
        PropertyInfo? propertyInfo = typeof (TestConfiguration).GetProperty (nameof (TestConfiguration.TestSchemeProperty));
        Scheme sourceScheme = new (new Attribute (Color.Red, Color.Blue, TextStyle.Bold));

        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo!,
            PropertyValue = new Scheme (new Attribute (Color.White, Color.Black, TextStyle.None))
        };

        // Act
        object? result = configProperty.UpdateFrom (sourceScheme);

        // Assert
        Assert.Equal (sourceScheme, result);
        Assert.Equal (sourceScheme, configProperty.PropertyValue);
        Assert.NotSame (sourceScheme, configProperty.PropertyValue); // Prove it's a clone, not a ref
    }
}
