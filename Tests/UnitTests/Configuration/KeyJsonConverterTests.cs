using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Terminal.Gui.ConfigurationTests;

public class KeyJsonConverterTests
{

    [Fact]
    public void Separator_Property_Set_Changes_Serialization_Format ()
    {
        Rune savedSeparator = Key.Separator;

        // Act
        // NOTE: This means this test can't be parallelized
        Key.Separator = (Rune)'*';
        string json = JsonSerializer.Serialize (Key.Separator, ConfigurationManager.SerializerContext.Options);

        // Assert
        Assert.Equal ("\"*\"", json);
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

        // Act
        // NOTE: This means this test can't be parallelized
        Key.Separator = (Rune)separator;

        Key deserializedKey = JsonSerializer.Deserialize<Key> (json, ConfigurationManager.SerializerContext.Options);

        Key expectedKey = new Key ((KeyCode)keyChar).WithCtrl.WithAlt;
        // Assert
        Assert.Equal (expectedKey, deserializedKey);
        Key.Separator = savedSeparator;
    }
}