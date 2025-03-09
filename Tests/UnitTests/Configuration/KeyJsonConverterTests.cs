using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Terminal.Gui.ConfigurationTests;

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
    [InlineData ((KeyCode)'+' | KeyCode.AltMask | KeyCode.CtrlMask, "\"Ctrl+Alt++\"")]
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
    [InlineData ((KeyCode)'+' | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt++")]
    public void TestKeyRoundTripConversion (KeyCode key, string expectedStringTo)
    {
        // Arrange

        // Act
        string json = JsonSerializer.Serialize ((Key)key, ConfigurationManager.SerializerOptions);
        var deserializedKey = JsonSerializer.Deserialize<Key> (json, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal (expectedStringTo, deserializedKey.ToString ());
    }

    [Fact]
    public void Deserialized_Key_Equals ()
    {
        // Arrange
        Key key = Key.Q.WithCtrl;

        // Act
        string json = "\"Ctrl+Q\"";
        Key deserializedKey = JsonSerializer.Deserialize<Key> (json, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal (key, deserializedKey);

    }
    [Fact]
    public void Separator_Property_Serializes_As_Glyph ()
    {
        // Act
        string json = JsonSerializer.Serialize (Key.Separator, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal ($"\"{Key.Separator}\"", json);
    }

    [Fact]
    public void Separator_Property_Set_Changes_Serialization_Format ()
    {
        Rune savedSeparator = Key.Separator;

        try
        {
            // Act
            Key.Separator = (Rune)'*';
            string json = JsonSerializer.Serialize (Key.Separator, ConfigurationManager.SerializerOptions);

            // Assert
            Assert.Equal ("\"*\"", json);
        }
        finally
        {
            Key.Separator = savedSeparator;
        }

        Key.Separator = savedSeparator;
    }

    [Theory]
    [InlineData ('A', '+', "\"Ctrl+Alt+A\"")]
    [InlineData ('A', '-', "\"Ctrl+Alt+A\"")]
    [InlineData ('A', '*', "\"Ctrl+Alt+A\"")]
    [InlineData ('A', '@', "\"Ctrl+Alt+A\"")]
    [InlineData ('A', '+', "\"Ctrl@Alt@A\"")]
    [InlineData ('A', '-', "\"Ctrl@Alt@A\"")]
    [InlineData ('A', '*', "\"Ctrl@Alt@A\"")]
    [InlineData ('A', '@', "\"Ctrl@Alt@A\"")]
    [InlineData ('+', '+', "\"Ctrl+Alt++\"")]
    [InlineData ('+', '-', "\"Ctrl+Alt++\"")]
    [InlineData ('+', '*', "\"Ctrl+Alt++\"")]
    [InlineData ('+', '@', "\"Ctrl+Alt++\"")]
    [InlineData ('+', '+', "\"Ctrl@Alt@+\"")]
    [InlineData ('+', '-', "\"Ctrl@Alt@+\"")]
    [InlineData ('+', '*', "\"Ctrl@Alt@+\"")]
    [InlineData ('+', '@', "\"Ctrl@Alt@+\"")]
    public void Separator_Property_Set_Deserialization_Works_With_Any (char keyChar, char separator, string json)
    {
        Rune savedSeparator = Key.Separator;

        try
        {
            // Act
            Key.Separator = (Rune)separator;

            Key deserializedKey = JsonSerializer.Deserialize<Key> (json, ConfigurationManager.SerializerOptions);

            Key expectedKey = new Key ((KeyCode)keyChar).WithCtrl.WithAlt;
            // Assert
            Assert.Equal (expectedKey, deserializedKey);
        }
        finally
        {
            Key.Separator = savedSeparator;
        }

        Key.Separator = savedSeparator;
    }
}