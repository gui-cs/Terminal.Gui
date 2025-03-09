using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.ConfigurationTests;

public class ConfigPropertyTests
{
    [Fact]
    public void Apply_PropertyValueIsAppliedToStatic_String_Property()
    {
        // Arrange
        TestConfiguration.Reset ();
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "UpdatedValue"
        };

        // Act
        var result = configProperty.Apply();

        // Assert
        Assert.Equal (1, TestConfiguration.TestStringPropertySetCount);
        Assert.True(result);
        Assert.Equal("UpdatedValue", TestConfiguration.TestStringProperty);
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
        Assert.Equal(1, TestConfiguration.TestKeyPropertySetCount);
        Assert.True (result);
        Assert.Equal (Key.Q.WithCtrl, TestConfiguration.TestKeyProperty);
        TestConfiguration.Reset ();
    }

    [Fact]
    public void RetrieveValue_GetsCurrentValueOfStaticProperty()
    {
        // Arrange
        TestConfiguration.TestStringProperty = "CurrentValue";
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo
        };

        // Act
        var value = configProperty.RetrieveValue();

        // Assert
        Assert.Equal("CurrentValue", value);
        Assert.Equal("CurrentValue", configProperty.PropertyValue);
    }

    [Fact]
    public void UpdateValueFrom_Updates_String_Property_Value ()
    {
        // Arrange
        TestConfiguration.Reset ();
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = "InitialValue"
        };

        // Act
        var updatedValue = configProperty.UpdateValueFrom("NewValue");

        // Assert
        Assert.Equal (0, TestConfiguration.TestStringPropertySetCount);
        Assert.Equal("NewValue", updatedValue);
        Assert.Equal("NewValue", configProperty.PropertyValue);
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
    public void Apply_TargetInvocationException_ThrowsJsonException()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));
        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = null // This will cause ArgumentNullException in the set accessor
        };

        // Act & Assert
        var exception = Assert.Throws<JsonException> (() => configProperty.Apply());
    }

    [Fact]
    public void GetJsonPropertyName_ReturnsJsonPropertyNameAttributeValue()
    {
        // Arrange
        var propertyInfo = typeof(TestConfiguration).GetProperty(nameof(TestConfiguration.TestStringProperty));

        // Act
        var jsonPropertyName = ConfigProperty.GetJsonPropertyName(propertyInfo);

        // Assert
        Assert.Equal("TestStringProperty", jsonPropertyName);
    }
}

public class TestConfiguration
{
    private static string _testStringProperty = "Default";
    public static int TestStringPropertySetCount { get; set; }

    [SerializableConfigurationProperty]
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

    [SerializableConfigurationProperty]
    public static Key TestKeyProperty
    {
        get => _testKeyProperty;
        set
        {
            TestKeyPropertySetCount++;
            _testKeyProperty = value ?? throw new ArgumentNullException (nameof (value));
        }
    }

    public static void Reset ()
    {
        TestStringPropertySetCount = 0;
        TestKeyPropertySetCount = 0;
    }
}
