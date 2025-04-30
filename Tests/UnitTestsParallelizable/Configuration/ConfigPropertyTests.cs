
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
        var value = configProperty.RetrieveValue ();

        // Assert
        Assert.Equal ("CurrentValue", value);
        Assert.Equal ("CurrentValue", configProperty.PropertyValue);
    }

    [Fact]
    public void UpdateValueFrom_Updates_String_Property_Value ()
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
        var updatedValue = configProperty.UpdateValueFrom ("NewValue");

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
        var jsonPropertyName = ConfigProperty.GetJsonPropertyName (propertyInfo);

        // Assert
        Assert.Equal ("TestStringProperty", jsonPropertyName);
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


    public class DeepCopyTest ()
    {
        public static Key key = Key.Esc;
    }

    [Fact]
    public void Illustrate_DeepMemberWiseCopy_Breaks_Dictionary ()
    {
        Assert.Equal (Key.Esc, DeepCopyTest.key);

        Dictionary<Key, string> dict = new Dictionary<Key, string> (new KeyEqualityComparer ());
        dict.Add (new (DeepCopyTest.key), "Esc");
        Assert.Contains (Key.Esc, dict);

        DeepCopyTest.key = (Key)ConfigProperty.DeepMemberWiseCopy (Key.Q.WithCtrl, DeepCopyTest.key);

        Assert.Equal (Key.Q.WithCtrl, DeepCopyTest.key);
        Assert.Equal (Key.Esc, dict.Keys.ToArray () [0]);

        var eq = new KeyEqualityComparer ();
        Assert.True (eq.Equals (Key.Q.WithCtrl, DeepCopyTest.key));
        Assert.Equal (Key.Q.WithCtrl.GetHashCode (), DeepCopyTest.key.GetHashCode ());
        Assert.Equal (eq.GetHashCode (Key.Q.WithCtrl), eq.GetHashCode (DeepCopyTest.key));
        Assert.Equal (Key.Q.WithCtrl.GetHashCode (), eq.GetHashCode (DeepCopyTest.key));
        Assert.True (dict.ContainsKey (Key.Esc));

        dict.Remove (Key.Esc);
        dict.Add (new (DeepCopyTest.key), "Ctrl+Q");
        Assert.True (dict.ContainsKey (Key.Q.WithCtrl));
    }


    [Fact]
    public void DeepMemberWiseCopyTest ()
    {
        // Value types
        var stringDest = "Destination";
        var stringSrc = "Source";
        object stringCopy = ConfigProperty.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = "Destination";
        stringCopy = ConfigProperty.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = null;
        stringCopy = ConfigProperty.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = string.Empty;
        stringCopy = ConfigProperty.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        var boolDest = true;
        var boolSrc = false;
        object boolCopy = ConfigProperty.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = false;
        boolSrc = true;
        boolCopy = ConfigProperty.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = true;
        boolSrc = true;
        boolCopy = ConfigProperty.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = false;
        boolSrc = false;
        boolCopy = ConfigProperty.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        // Structs
        var attrDest = new Attribute (Color.Black);
        var attrSrc = new Attribute (Color.White);
        object attrCopy = ConfigProperty.DeepMemberWiseCopy (attrSrc, attrDest);
        Assert.Equal (attrSrc, attrCopy);

        // Classes
        var colorschemeDest = new Scheme { Disabled = new Attribute (Color.Black) };
        var colorschemeSrc = new Scheme { Disabled = new Attribute (Color.White) };
        object colorschemeCopy = ConfigProperty.DeepMemberWiseCopy (colorschemeSrc, colorschemeDest);
        Assert.Equal (colorschemeSrc, colorschemeCopy);

        // Dictionaries
        Dictionary<string, Attribute> dictDest = new () { { "Disabled", new Attribute (Color.Black) } };
        Dictionary<string, Attribute> dictSrc = new () { { "Disabled", new Attribute (Color.White) } };
        Dictionary<string, Attribute> dictCopy = (Dictionary<string, Attribute>)ConfigProperty.DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (dictSrc, dictCopy);

        dictDest = new Dictionary<string, Attribute> { { "Disabled", new Attribute (Color.Black) } };

        dictSrc = new Dictionary<string, Attribute>
        {
            { "Disabled", new Attribute (Color.White) }, { "Normal", new Attribute (Color.Blue) }
        };
        dictCopy = (Dictionary<string, Attribute>)ConfigProperty.DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (dictSrc, dictCopy);

        // src adds an item
        dictDest = new Dictionary<string, Attribute> { { "Disabled", new Attribute (Color.Black) } };

        dictSrc = new Dictionary<string, Attribute>
        {
            { "Disabled", new Attribute (Color.White) }, { "Normal", new Attribute (Color.Blue) }
        };
        dictCopy = (Dictionary<string, Attribute>)ConfigProperty.DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (2, dictCopy!.Count);
        Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
        Assert.Equal (dictSrc ["Normal"], dictCopy ["Normal"]);

        // src updates only one item
        dictDest = new Dictionary<string, Attribute>
        {
            { "Disabled", new Attribute (Color.Black) }, { "Normal", new Attribute (Color.White) }
        };
        dictSrc = new Dictionary<string, Attribute> { { "Disabled", new Attribute (Color.White) } };
        dictCopy = (Dictionary<string, Attribute>)ConfigProperty.DeepMemberWiseCopy (dictSrc, dictDest);
        Assert.Equal (2, dictCopy!.Count);
        Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
        Assert.Equal (dictDest ["Normal"], dictCopy ["Normal"]);
    }
}
