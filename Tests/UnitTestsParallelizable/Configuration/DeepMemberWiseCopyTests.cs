#nullable enable
using System.Diagnostics;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class DeepMemberWiseCopyTests
{
    public class DeepCopyTest
    {
        public static Key? Key { get; set; } = Key.Esc;
    }

    [Fact]
    public void Illustrate_Breaks_Dictionary ()
    {
        Assert.Equal (Key.Esc, DeepCopyTest.Key);

        Dictionary<Key, string> dict = new (new KeyEqualityComparer ())
        {
            {
                new (DeepCopyTest.Key!), "Esc"
            }
        };
        Assert.Contains (Key.Esc, dict);

        DeepCopyTest.Key = (Key)ScopeExtensions.DeepMemberWiseCopy (Key.Q.WithCtrl, DeepCopyTest.Key)!;

        Assert.Equal (Key.Q.WithCtrl, DeepCopyTest.Key);
        Assert.Equal (Key.Esc, dict.Keys.ToArray () [0]);

        var eq = new KeyEqualityComparer ();
        Assert.True (eq.Equals (Key.Q.WithCtrl, DeepCopyTest.Key));
        Assert.Equal (Key.Q.WithCtrl.GetHashCode (), DeepCopyTest.Key.GetHashCode ());
        Assert.Equal (eq.GetHashCode (Key.Q.WithCtrl), eq.GetHashCode (DeepCopyTest.Key));
        Assert.Equal (Key.Q.WithCtrl.GetHashCode (), eq.GetHashCode (DeepCopyTest.Key));
        Assert.True (dict.ContainsKey (Key.Esc));

        dict.Remove (Key.Esc);
        dict.Add (new (DeepCopyTest.Key), "Ctrl+Q");
        Assert.True (dict.ContainsKey (Key.Q.WithCtrl));
    }

    [Fact]
    public void DeepMemberWiseCopyTest ()
    {
        // Value types
        var stringDest = "Destination";
        var stringSrc = "Source";
        object? stringCopy = ScopeExtensions.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = "Destination";
        stringCopy = ScopeExtensions.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = null;
        stringCopy = ScopeExtensions.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        stringDest = "Destination";
        stringSrc = string.Empty;
        stringCopy = ScopeExtensions.DeepMemberWiseCopy (stringSrc, stringDest);
        Assert.Equal (stringSrc, stringCopy);

        var boolDest = true;
        var boolSrc = false;
        object? boolCopy = ScopeExtensions.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = false;
        boolSrc = true;
        boolCopy = ScopeExtensions.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = true;
        boolSrc = true;
        boolCopy = ScopeExtensions.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        boolDest = false;
        boolSrc = false;
        boolCopy = ScopeExtensions.DeepMemberWiseCopy (boolSrc, boolDest);
        Assert.Equal (boolSrc, boolCopy);

        // Structs
        var attrDest = new Attribute (Color.Black);
        var attrSrc = new Attribute (Color.White);
        object? attrCopy = ScopeExtensions.DeepMemberWiseCopy (attrSrc, attrDest);
        Assert.Equal (attrSrc, attrCopy);

        // Classes
        var colorschemeDest = new Scheme { Disabled = new (Color.Black) };
        var colorschemeSrc = new Scheme { Disabled = new (Color.White) };
        object? colorschemeCopy = ScopeExtensions.DeepMemberWiseCopy (colorschemeSrc, colorschemeDest);
        Assert.Equal (colorschemeSrc, colorschemeCopy);

        // Dictionaries
        Dictionary<string, Attribute> dictDest = new () { { "Disabled", new (Color.Black) } };
        Dictionary<string, Attribute> dictSrc = new () { { "Disabled", new (Color.White) } };
        Dictionary<string, Attribute> dictCopy = (Dictionary<string, Attribute>)ScopeExtensions.DeepMemberWiseCopy (dictSrc, dictDest)!;
        Assert.Equal (dictSrc, dictCopy);

        dictDest = new () { { "Disabled", new (Color.Black) } };

        dictSrc = new ()
        {
            { "Disabled", new (Color.White) }, { "Normal", new (Color.Blue) }
        };
        dictCopy = (Dictionary<string, Attribute>)ScopeExtensions.DeepMemberWiseCopy (dictSrc, dictDest)!;
        Assert.Equal (dictSrc, dictCopy);

        // src adds an item
        dictDest = new () { { "Disabled", new (Color.Black) } };

        dictSrc = new ()
        {
            { "Disabled", new (Color.White) }, { "Normal", new (Color.Blue) }
        };
        dictCopy = (Dictionary<string, Attribute>)ScopeExtensions.DeepMemberWiseCopy (dictSrc, dictDest)!;
        Assert.Equal (2, dictCopy!.Count);
        Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
        Assert.Equal (dictSrc ["Normal"], dictCopy ["Normal"]);

        // src updates only one item
        dictDest = new()
        {
            { "Disabled", new (Color.Black) }, { "Normal", new (Color.White) }
        };
        dictSrc = new() { { "Disabled", new (Color.White) } };
        dictCopy = (Dictionary<string, Attribute>)ScopeExtensions.DeepMemberWiseCopy (dictSrc, dictDest)!;
        Assert.Equal (2, dictCopy!.Count);
        Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
        Assert.Equal (dictDest ["Normal"], dictCopy ["Normal"]);
    }

    [Fact]
    public void SourceIsNull_ReturnsNull ()
    {
        // Arrange
        object? source = null;
        var destination = new object ();

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void DestinationIsNull_ThrowsArgumentNullException ()
    {
        // Arrange
        var source = new object ();
        object? destination = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => ScopeExtensions.DeepMemberWiseCopy (source, destination));
    }

    public class CircularReference
    {
        public CircularReference? Child { get; set; }
    }

    [Fact (Skip = "AI Generated test")]
    public void CircularReferences_DoesNotThrow ()
    {
        // Arrange
        var source = new CircularReference ();
        source.Child = source;

        var destination = new CircularReference ();

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.NotNull (result);
        Assert.Same (result, ((CircularReference)result).Child);
    }

    [Fact (Skip = "AI Generated test")]
    public void List_CopiesElements ()
    {
        // Arrange
        List<int> source = new() { 1, 2, 3 };
        List<int> destination = new ();

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Equal (source, result);
    }

    [Fact]
    public void Array_CopiesElements ()
    {
        // Arrange
        var source = new [] { 1, 2, 3 };
        var destination = new int [3];

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Equal (source, result);
    }

    public class NestedObject
    {
        public string? Name { get; set; }
        public NestedObject? Child { get; set; }
    }

    [Fact]
    public void NestedObjects_CopiesAllLevels ()
    {
        // Arrange
        var source = new NestedObject
        {
            Name = "Parent",
            Child = new() { Name = "Child" }
        };
        var destination = new NestedObject ();

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Equal (source.Name, ((NestedObject)result!).Name);
        Assert.Equal (source.Child?.Name, ((NestedObject)result!).Child?.Name);
    }

    public class ComplexKey
    {
        public int Id { get; set; }
        public override bool Equals (object? obj) { return obj is ComplexKey key && Id == key.Id; }

        public override int GetHashCode () { return Id.GetHashCode (); }
    }

    [Fact]
    public void DictionaryWithComplexKeys_CopiesCorrectly ()
    {
        // Arrange
        Dictionary<ComplexKey, string> source = new ()
        {
            { new() { Id = 1 }, "Value1" }
        };
        Dictionary<ComplexKey, string> destination = new ();

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Equal (source.Keys.First ().Id, ((Dictionary<ComplexKey, string>)result!)!.Keys.First ().Id);
        Assert.Equal (source.Values.First (), ((Dictionary<ComplexKey, string>)result).Values.First ());
    }

    [Fact (Skip = "AI Generated test")]
    public void UnsupportedType_ThrowsException ()
    {
        // Arrange
        var source = new StreamReader (Stream.Null);
        var destination = new StreamReader (Stream.Null);

        // Act & Assert
        Assert.Throws<JsonException> (() => ScopeExtensions.DeepMemberWiseCopy (source, destination));
    }

    [Fact]
    public void ImmutableObject_ReturnsSource ()
    {
        // Arrange
        var source = "ImmutableString";
        var destination = "AnotherString";

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Equal (source, result);
    }

    [Fact (Skip = "AI Generated test")]
    public void LargeObject_PerformsWithinLimit ()
    {
        // Arrange
        List<int> source = new (Enumerable.Range (1, 10000));
        List<int> destination = new ();

        // Act
        var stopwatch = Stopwatch.StartNew ();
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);
        stopwatch.Stop ();

        // Assert
        Assert.Equal (source, result);
        Assert.True (stopwatch.ElapsedMilliseconds < 1000); // Ensure it completes within 1 second
    }

    public class CustomType
    {
        public int Value { get; set; }
        public override bool Equals (object? obj) { return obj is CustomType other && Value == other.Value; }

        public override int GetHashCode () { return Value.GetHashCode (); }
    }

    [Fact]
    public void CustomType_CopiesCorrectly ()
    {
        // Arrange
        var source = new CustomType { Value = 42 };
        var destination = new CustomType ();

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Equal (source.Value, ((CustomType)result!)!.Value);
    }

    public class MixedObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
        public NestedObject? Child { get; set; }
    }

    [Fact]
    public void MixedObject_CopiesAllProperties ()
    {
        // Arrange
        var source = new MixedObject
        {
            Id = 1,
            Name = "Test",
            Tags = new() { "Tag1", "Tag2" },
            Child = new() { Name = "Child" }
        };
        var destination = new MixedObject ();

        // Act
        object? result = ScopeExtensions.DeepMemberWiseCopy (source, destination);

        // Assert
        Assert.Equal (source.Id, ((MixedObject)result!)!.Id);
        Assert.Equal (source.Name, ((MixedObject)result).Name);
        Assert.Equal (source.Tags, ((MixedObject)result).Tags);
        Assert.Equal (source.Child?.Name, ((MixedObject)result).Child?.Name);
    }
}
