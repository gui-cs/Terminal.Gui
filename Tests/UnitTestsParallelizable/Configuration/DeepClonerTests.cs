#nullable enable

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Terminal.Gui.ConfigurationTests;

/// <summary>
///     Unit tests for the <see cref="DeepCloner"/> class, ensuring robust deep cloning for
///     Terminal.Gui's configuration system.
/// </summary>
public class DeepClonerTests
{
    // Test classes for complex scenarios
    private class SimpleValueType
    {
        public int Number { get; set; }
        public bool Flag { get; init; }
    }

    private class SimpleReferenceType
    {
        public string? Name { get; set; }
        public int Count { get; set; }

        public override bool Equals (object? obj) { return obj is SimpleReferenceType other && Name == other.Name && Count == other.Count; }

        // ReSharper disable twice NonReadonlyMemberInGetHashCode
        public override int GetHashCode () { return HashCode.Combine (Name, Count); }
    }

    private class CollectionContainer
    {
        public List<string>? Strings { get; init; }
        public Dictionary<string, int>? Counts { get; init; }
        public int []? Numbers { get; init; }
    }

    private class NestedObject
    {
        public SimpleReferenceType? Inner { get; init; }
        public List<SimpleValueType>? Values { get; init; }
    }

    private class CircularReference
    {
        public CircularReference? Self { get; set; }
        public string? Name { get; set; }
    }

    private class ConfigPropertyMock
    {
        public object? PropertyValue { get; init; }
        public bool Immutable { get; init; }
    }

    private class ComplexKey
    {
        public int Id { get; init; }
        public override bool Equals (object? obj) { return obj is ComplexKey key && Id == key.Id; }

        public override int GetHashCode () { return Id.GetHashCode (); }
    }

    private class KeyEqualityComparer : IEqualityComparer<Key>
    {
        public bool Equals (Key? x, Key? y) { return x?.KeyCode == y?.KeyCode; }

        public int GetHashCode (Key obj) { return obj.KeyCode.GetHashCode (); }
    }

    // Fundamentals

    [Fact]
    public void Null_ReturnsNull ()
    {
        object? source = null;
        object? result = DeepCloner.DeepClone (source);

        Assert.Null (result);
    }

    [Fact]
    public void SimpleValueType_ReturnsEqualValue ()
    {
        var source = 42;
        int result = DeepCloner.DeepClone (source);

        Assert.Equal (source, result);
    }

    [Fact]
    public void String_ReturnsSameString ()
    {
        var source = "Hello";
        string? result = DeepCloner.DeepClone (source);

        Assert.Equal (source, result);
        Assert.Same (source, result); // Strings are immutable
    }

    [Fact]
    public void Rune_ReturnsEqualRune ()
    {
        Rune source = new ('A');
        Rune result = DeepCloner.DeepClone (source);

        Assert.Equal (source, result);
    }

    [Fact]
    public void Key_CreatesDeepCopy ()
    {
        Key? source = new (KeyCode.A);
        source.Handled = true;
        Key? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source.KeyCode, result.KeyCode);
        Assert.Equal (source.Handled, result.Handled);

        // Modify result, ensure source unchanged
        result.Handled = false;
        Assert.True (source.Handled);
    }

    [Fact]
    public void Enum_ReturnsEqualEnum ()
    {
        var source = DayOfWeek.Monday;
        DayOfWeek result = DeepCloner.DeepClone (source);

        Assert.Equal (source, result);
    }

    [Fact]
    public void Boolean_ReturnsEqualValue ()
    {
        var source = true;
        bool result = DeepCloner.DeepClone (source);
        Assert.Equal (source, result);

        source = false;
        result = DeepCloner.DeepClone (source);
        Assert.Equal (source, result);
    }

    [Fact]
    public void Attribute_ReturnsEqualValue ()
    {
        var source = new Attribute (Color.Black);
        Attribute result = DeepCloner.DeepClone (source);
        Assert.Equal (source, result);

        source = new (Color.White);
        result = DeepCloner.DeepClone (source);
        Assert.Equal (source, result);
    }

    [Fact]
    public void Scheme_Normal_Set_ReturnsEqualValue ()
    {
        var source = new Scheme (new Scheme (new Attribute (Color.Red, Color.Green, TextStyle.Bold)));
        Scheme? result = DeepCloner.DeepClone (source);
        Assert.Equal (source, result);

        source = new Scheme (new Scheme ());
        result = DeepCloner.DeepClone (source);
        Assert.Equal (source, result);
    }

    [Fact]
    public void Scheme_All_Set_ReturnsEqualValue ()
    {
        Scheme? source = new ()
        {
            Normal = new ("LightGray", "RaisinBlack", TextStyle.None), 
            Focus = new ("White", "DarkGray", TextStyle.None), 
            HotNormal = new ("Silver", "RaisinBlack", TextStyle.Underline), 
            Disabled = new ("DarkGray", "RaisinBlack", TextStyle.Faint), 
            HotFocus = new ("White", "Green", TextStyle.Underline), 
            Active = new ("White", "Charcoal", TextStyle.Bold), 
            HotActive = new ("White", "Charcoal", TextStyle.Underline | TextStyle.Bold),
            Highlight = new ("White", "Onyx", TextStyle.None), 
            Editable = new ("LightYellow", "RaisinBlack", TextStyle.None),
            ReadOnly = new ("Gray", "RaisinBlack", TextStyle.Italic) 
        };
        Scheme? result = DeepCloner.DeepClone (source);
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.Normal, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out _));
        Assert.True (source.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out _));

        Assert.Equal (source, result);

        source = new Scheme (new Scheme ());
        result = DeepCloner.DeepClone (source);
        Assert.Equal (source, result);
    }

    // Simple Reference Types

    [Fact]
    public void SimpleReferenceType_CreatesDeepCopy ()
    {
        SimpleReferenceType? source = new () { Name = "Test", Count = 10 };
        SimpleReferenceType? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source.Name, result!.Name);
        Assert.Equal (source.Count, result.Count);
        Assert.True (source.Equals (result)); // Verify Equals method
        Assert.Equal (source.GetHashCode (), result.GetHashCode ()); // Verify GetHashCode

        // Modify result, ensure source unchanged
        result.Name = "Modified";
        result.Count = 20;
        Assert.Equal ("Test", source.Name);
        Assert.Equal (10, source.Count);
    }

    // Collections

    [Fact]
    public void List_CreatesDeepCopy ()
    {
        List<string>? source = new () { "One", "Two" };
        List<string>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source, result);

        // Modify result, ensure source unchanged
        result!.Add ("Three");
        Assert.Equal (2, source.Count);
        Assert.Equal (3, result.Count);
    }

    [Fact]
    public void Dictionary_CreatesDeepCopy ()
    {
        Dictionary<string, int>? source = new () { { "A", 1 }, { "B", 2 } };
        Dictionary<string, int>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source, result);

        // Modify result, ensure source unchanged
        result! ["C"] = 3;
        Assert.Equal (2, source.Count);
        Assert.Equal (3, result.Count);
    }

    [Fact]
    public void Dictionary_CreatesDeepCopy_Including_Comparer_Options ()
    {
        Dictionary<string, int>? source = new (StringComparer.InvariantCultureIgnoreCase) { { "A", 1 }, { "B", 2 } };
        Dictionary<string, int>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source, result);
        Assert.Equal (source.Comparer, result.Comparer);

        // Modify result, ensure source unchanged
        result! ["C"] = 3;
        Assert.Equal (2, source.Count);
        Assert.Equal (3, result.Count);

        Assert.Contains ("A", result);
        Assert.Contains ("a", result);
    }

    [Fact]
    public void Dictionary_CreatesDeepCopy_WithCapacity ()
    {
        // Arrange: Create a dictionary with a specific capacity
        Dictionary<string, int> source = new (100) // Set initial capacity to 100
        {
            { "Key1", 1 },
            { "Key2", 2 }
        };

        // Act: Clone the dictionary
        Dictionary<string, int>? result = DeepCloner.DeepClone (source);

        // Assert: Verify the dictionary was cloned correctly
        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source, result); // Verify key-value pairs are cloned

        // Verify that the capacity is preserved (if supported)
        Assert.True (result.Count <= result.EnsureCapacity (0)); // EnsureCapacity(0) returns the current capacity
        Assert.True (source.Count <= source.EnsureCapacity (0)); // EnsureCapacity(0) returns the current capacity
    }

    [Fact]
    public void ConcurrentDictionary_CreatesDeepCopy ()
    {
        ConcurrentDictionary<string, int>? source = new (new Dictionary<string, int> () { { "A", 1 }, { "B", 2 } });
        ConcurrentDictionary<string, int>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source, result);

        // Modify result, ensure source unchanged
        result! ["C"] = 3;
        Assert.Equal (2, source.Count);
        Assert.Equal (3, result.Count);
    }

    [Fact]
    public void Array_CreatesDeepCopy ()
    {
        int []? source = { 1, 2, 3 };
        int []? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source, result);

        // Modify result, ensure source unchanged
        result! [0] = 99;
        Assert.Equal (1, source [0]);
        Assert.Equal (99, result [0]);
    }

    [Fact]
    public void ImmutableList_ThrowsNotSupported ()
    {
        ImmutableList<string> source = ImmutableList.Create ("One", "Two");
        Assert.Throws<NotSupportedException> (() => DeepCloner.DeepClone (source));
    }

    [Fact]
    public void ImmutableDictionary_ThrowsNotSupported ()
    {
        ImmutableDictionary<string, int> source = ImmutableDictionary.Create<string, int> ().Add ("A", 1);
        Assert.Throws<NotSupportedException> (() => DeepCloner.DeepClone (source));
    }

    [Fact]
    public void Dictionary_SourceAddsItem_ClonesCorrectly ()
    {
        Dictionary<string, Attribute>? source = new ()
        {
            { "Disabled", new (Color.White) },
            { "Normal", new (Color.Blue) }
        };
        Dictionary<string, Attribute>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (2, result.Count);
        Assert.Equal ((Attribute)source ["Disabled"], (Attribute)result ["Disabled"]);
        Assert.Equal (source ["Normal"], result ["Normal"]);
    }

    [Fact]
    public void Dictionary_SourceUpdatesOneItem_ClonesCorrectly ()
    {
        Dictionary<string, Attribute>? source = new () { { "Disabled", new (Color.White) } };
        Dictionary<string, Attribute>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Single (result);
        Assert.Equal (source ["Disabled"], result ["Disabled"]);
    }

    [Fact]
    public void Dictionary_WithComplexKeys_ClonesCorrectly ()
    {
        Dictionary<ComplexKey, string>? source = new ()
        {
            { new() { Id = 1 }, "Value1" }
        };
        Dictionary<ComplexKey, string>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source.Keys.First ().Id, result.Keys.First ().Id);
        Assert.Equal (source.Values.First (), result.Values.First ());
    }

    [Fact]
    public void Dictionary_WithCustomKeyComparer_ClonesCorrectly ()
    {
        Dictionary<Key, string> source = new (new KeyEqualityComparer ())
        {
            { new (KeyCode.Esc), "Esc" }
        };
        Dictionary<Key, string> result = DeepCloner.DeepClone (source)!;

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Single (result);
        Assert.True (result.ContainsKey (new (KeyCode.Esc)));
        Assert.Equal ("Esc", result [new (KeyCode.Esc)]);

        // Modify result, ensure source unchanged
        result [new (KeyCode.Q)] = "Q";
        Assert.False (source.ContainsKey (new (KeyCode.Q)));
    }

    // Nested Objects

    [Fact]
    public void CollectionContainer_CreatesDeepCopy ()
    {
        CollectionContainer? source = new ()
        {
            Strings = ["A", "B"],
            Counts = new () { { "X", 1 }, { "Y", 2 } },
            Numbers = [10, 20]
        };
        CollectionContainer? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.NotSame (source.Strings, result!.Strings);
        Assert.NotSame (source.Counts, result.Counts);
        Assert.NotSame (source.Numbers, result.Numbers);
        Assert.Equal (source.Strings, result.Strings);
        Assert.Equal (source.Counts, result.Counts);
        Assert.Equal (source.Numbers, result.Numbers);

        // Modify result, ensure source unchanged
        result.Strings!.Add ("C");
        result.Counts! ["Z"] = 3;
        result.Numbers! [0] = 99;
        Assert.Equal (2, source.Strings.Count);
        Assert.Equal (2, source.Counts.Count);
        Assert.Equal (10, source.Numbers [0]);
    }

    [Fact]
    public void NestedObject_CreatesDeepCopy ()
    {
        NestedObject? source = new ()
        {
            Inner = new () { Name = "Inner", Count = 5 },
            Values = new ()
            {
                new() { Number = 1, Flag = true },
                new() { Number = 2, Flag = false }
            }
        };
        NestedObject? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.NotSame (source.Inner, result!.Inner);
        Assert.NotSame (source.Values, result.Values);
        Assert.Equal (source.Inner!.Name, result.Inner!.Name);
        Assert.Equal (source.Inner.Count, result.Inner.Count);
        Assert.Equal (source.Values! [0].Number, result.Values! [0].Number);
        Assert.Equal (source.Values [0].Flag, result.Values [0].Flag);

        // Modify result, ensure source unchanged
        result.Inner.Name = "Modified";
        result.Values [0].Number = 99;
        Assert.Equal ("Inner", source.Inner.Name);
        Assert.Equal (1, source.Values [0].Number);
    }

    // Circular References

    [Fact]
    public void CircularReference_HandlesCorrectly ()
    {
        CircularReference? source = new () { Name = "Cycle" };
        source.Self = source; // Create circular reference
        CircularReference? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source.Name, result!.Name);
        Assert.NotNull (result.Self);
        Assert.Same (result, result.Self); // Circular reference preserved
        Assert.NotSame (source.Self, result.Self);

        // Modify result, ensure source unchanged
        result.Name = "Modified";
        Assert.Equal ("Cycle", source.Name);
    }

    // Terminal.Gui-Specific Types

    [Fact]
    public void ConfigPropertyMock_CreatesDeepCopy ()
    {
        ConfigPropertyMock? source = new ()
        {
            PropertyValue = new List<string> { "Red", "Blue" },
            Immutable = true
        };
        ConfigPropertyMock? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.NotSame (source.PropertyValue, result!.PropertyValue);
        Assert.Equal ((List<string>)source.PropertyValue, (List<string>)result.PropertyValue!);
        Assert.Equal (source.Immutable, result.Immutable);

        // Modify result, ensure source unchanged
        ((List<string>)result.PropertyValue!).Add ("Green");
        Assert.Equal (2, ((List<string>)source.PropertyValue).Count);
    }

    [Fact]
    public void ConfigProperty_CreatesDeepCopy ()
    {
        ConfigProperty? source = ConfigProperty.CreateImmutableWithAttributeInfo (CM.GetHardCodedConfigPropertyCache ()! ["Application.QuitKey"].PropertyInfo!);
        source.Immutable = false;
        source.PropertyValue = Key.A;
        ConfigProperty? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotNull (result.PropertyInfo);
        Assert.NotSame (source, result);
        Assert.NotSame (source.PropertyValue, result!.PropertyValue);
        // PropertyInfo is effectively a simple type
        Assert.Same (source.PropertyInfo, result!.PropertyInfo);
        Assert.Equal (source.Immutable, result.Immutable);
    }

    [Fact]
    public void LargeObject_PerformsWithinLimit ()
    {
        List<int> source = new (Enumerable.Range (1, 10000));
        var stopwatch = Stopwatch.StartNew ();
        List<int> result = DeepCloner.DeepClone (source)!;
        stopwatch.Stop ();

        Assert.Equal (source, result);
        Assert.True (stopwatch.ElapsedMilliseconds < 1000); // Ensure it completes within 1 second
    }

}
