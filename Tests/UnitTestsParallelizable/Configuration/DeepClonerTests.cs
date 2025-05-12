#nullable enable

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
        public bool Flag { get; set; }
    }

    private class SimpleReferenceType
    {
        public string? Name { get; set; }
        public int Count { get; set; }

        public override bool Equals (object? obj) { return obj is SimpleReferenceType other && Name == other.Name && Count == other.Count; }

        public override int GetHashCode () { return HashCode.Combine (Name, Count); }
    }

    private class CollectionContainer
    {
        public List<string>? Strings { get; set; }
        public Dictionary<string, int>? Counts { get; set; }
        public int []? Numbers { get; set; }
    }

    private class NestedObject
    {
        public SimpleReferenceType? Inner { get; set; }
        public List<SimpleValueType>? Values { get; set; }
    }

    private class CircularReference
    {
        public CircularReference? Self { get; set; }
        public string? Name { get; set; }
    }

    private class ConfigPropertyMock
    {
        public object? PropertyValue { get; set; }
        public bool Immutable { get; set; }
    }

    private class SettingsScopeMock : Dictionary<string, ConfigPropertyMock>
    {
        public string? Theme { get; set; }
    }

    private class ComplexKey
    {
        public int Id { get; set; }
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
        Assert.Equal (source ["Disabled"], result ["Disabled"]);
        Assert.Equal (source ["Normal"], result ["Normal"]);
    }

    [Fact]
    public void Dictionary_SourceUpdatesOneItem_ClonesCorrectly ()
    {
        Dictionary<string, Attribute>? source = new () { { "Disabled", new (Color.White) } };
        Dictionary<string, Attribute>? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (1, result.Count);
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
            Counts = new() { { "X", 1 }, { "Y", 2 } },
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
            Inner = new() { Name = "Inner", Count = 5 },
            Values = new()
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
    public void SettingsScopeMockWithKey_CreatesDeepCopy ()
    {
        SettingsScopeMock? source = new ()
        {
            Theme = "Dark",
            ["KeyBinding"] = new() { PropertyValue = new Key (KeyCode.A) { Handled = true } },
            ["Counts"] = new() { PropertyValue = new Dictionary<string, int> { { "X", 1 } } }
        };
        SettingsScopeMock? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source.Theme, result!.Theme);
        Assert.NotSame (source ["KeyBinding"], result ["KeyBinding"]);
        Assert.NotSame (source ["Counts"], result ["Counts"]);

        ConfigPropertyMock clonedKeyProp = result ["KeyBinding"];
        var clonedKey = (Key)clonedKeyProp.PropertyValue!;
        Assert.NotSame (source ["KeyBinding"].PropertyValue, clonedKey);
        Assert.Equal (((Key)source ["KeyBinding"].PropertyValue!).KeyCode, clonedKey.KeyCode);
        Assert.Equal (((Key)source ["KeyBinding"].PropertyValue!).Handled, clonedKey.Handled);

        Assert.Equal ((Dictionary<string, int>)source ["Counts"].PropertyValue!, (Dictionary<string, int>)result ["Counts"].PropertyValue!);

        // Modify result, ensure source unchanged
        result.Theme = "Light";
        clonedKey.Handled = false;
        ((Dictionary<string, int>)result ["Counts"].PropertyValue!).Add ("Y", 2);
        Assert.Equal ("Dark", source.Theme);
        Assert.True (((Key)source ["KeyBinding"].PropertyValue!).Handled);
        Assert.Equal (1, ((Dictionary<string, int>)source ["Counts"].PropertyValue!).Count);
    }

    [Fact]
    public void ThemeScopeList_WithThemes_ClonesSuccessfully ()
    {
        // Arrange: Create a ThemeScope and verify a property exists
        var defaultThemeScope = new ThemeScope ();
        Assert.True (defaultThemeScope.ContainsKey ("Button.DefaultHighlightStyle"));

        var darkThemeScope = new ThemeScope ();
        Assert.True (darkThemeScope.ContainsKey ("Button.DefaultHighlightStyle"));

        // Create a Themes list with two themes
        List<Dictionary<string, ThemeScope>> themesList = new()
        {
            new() { { "Default", defaultThemeScope } },
            new() { { "Dark", darkThemeScope } }
        };

        // Create a SettingsScope and set the Themes property
        var settingsScope = new SettingsScope ();
        Assert.True (settingsScope.ContainsKey ("Themes"));
        settingsScope ["Themes"].PropertyValue = themesList;

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);
        var resultScope = (SettingsScope)result;
        Assert.True (resultScope.ContainsKey ("Themes"));

        Assert.NotNull (resultScope ["Themes"].PropertyValue);

        List<Dictionary<string, ThemeScope>> clonedThemes = (List<Dictionary<string, ThemeScope>>)resultScope ["Themes"].PropertyValue!;
        Assert.Equal (2, clonedThemes.Count);
    }

    [Fact]
    public void Empty_SettingsScope_ClonesSuccessfully ()
    {
        // Arrange: Create a SettingsScope 
        var settingsScope = new SettingsScope ();
        Assert.True (settingsScope.ContainsKey ("Themes"));

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);

        // There are no HasValue properties, so DeepClone...
        Assert.False (result.ContainsKey ("Themes"));
    }

    [Fact]
    public void SettingsScope_With_Themes_Set_ClonesSuccessfully ()
    {
        // Arrange: Create a SettingsScope 
        var settingsScope = new SettingsScope ();
        Assert.True (settingsScope.ContainsKey ("Themes"));

        settingsScope ["Themes"].PropertyValue = new List<Dictionary<string, ThemeScope>>
        {
            new() { { "Default", new () } },
            new() { { "Dark", new () } }
        };

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);
        Assert.True (result.ContainsKey ("Themes"));
        Assert.NotNull (result ["Themes"].PropertyValue);
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
