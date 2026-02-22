using UnitTests;

namespace UnitTests.ViewsTests;

public class TreeViewCollectionNavigatorMatcherTests
{
    [Fact]
    public void IsMatch_UsesAspectGetter_NotToString ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "Apple" };
        
        // Act & Assert - Should match based on Name (AspectGetter), not Id (ToString)
        Assert.True (matcher.IsMatch ("A", item));
        Assert.True (matcher.IsMatch ("App", item));
        Assert.True (matcher.IsMatch ("Apple", item));
        
        // Should NOT match ToString value
        Assert.False (matcher.IsMatch ("1", item));
    }

    [Fact]
    public void IsMatch_IsCaseInsensitive ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "Apple" };
        
        // Act & Assert - Should be case insensitive
        Assert.True (matcher.IsMatch ("a", item));
        Assert.True (matcher.IsMatch ("A", item));
        Assert.True (matcher.IsMatch ("APPLE", item));
        Assert.True (matcher.IsMatch ("apple", item));
        Assert.True (matcher.IsMatch ("ApPlE", item));
    }

    [Fact]
    public void IsMatch_MatchesPrefix ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "Banana" };
        
        // Act & Assert - Should match prefix only
        Assert.True (matcher.IsMatch ("B", item));
        Assert.True (matcher.IsMatch ("Ban", item));
        Assert.True (matcher.IsMatch ("Banana", item));
        
        // Should NOT match substring that's not at start
        Assert.False (matcher.IsMatch ("ana", item));
        Assert.False (matcher.IsMatch ("nana", item));
    }

    [Fact]
    public void IsMatch_WithNullValue_ReturnsFalse ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        
        // Act & Assert
        Assert.False (matcher.IsMatch ("A", null));
    }

    [Fact]
    public void IsMatch_WithEmptySearch_ReturnsFalse ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "Apple" };
        
        // Act & Assert
        Assert.False (matcher.IsMatch ("", item));
        Assert.False (matcher.IsMatch (null, item));
    }

    [Fact]
    public void IsMatch_AspectGetterReturnsNull_ReturnsFalse ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => null; // AspectGetter returns null
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "Apple" };
        
        // Act & Assert
        Assert.False (matcher.IsMatch ("A", item));
    }

    [Fact]
    public void IsMatch_AspectGetterReturnsEmptyString_ReturnsFalse ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => string.Empty;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "Apple" };
        
        // Act & Assert
        Assert.False (matcher.IsMatch ("A", item));
    }

    [Fact]
    public void IsMatch_WithFormatting_MatchesAspectValue ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => $"[{item.Id}] {item.Name}";
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 42, Name = "Cherry" };
        
        // Act & Assert - Should match the formatted string
        Assert.True (matcher.IsMatch ("[", item));
        Assert.True (matcher.IsMatch ("[42", item));
        Assert.True (matcher.IsMatch ("[42] C", item));
        
        // Should NOT match just the name
        Assert.False (matcher.IsMatch ("Cherry", item));
        Assert.False (matcher.IsMatch ("C", item));
    }

    [Fact]
    public void IsMatch_ChangingAspectGetter_UsesNewAspectGetter ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "Apple" };
        
        // Act & Assert - Initially matches on Name
        Assert.True (matcher.IsMatch ("A", item));
        Assert.False (matcher.IsMatch ("1", item));
        
        // Change AspectGetter to return Id
        treeView.AspectGetter = item => item.Id.ToString ();
        
        // Should now match on Id, not Name
        Assert.True (matcher.IsMatch ("1", item));
        Assert.False (matcher.IsMatch ("A", item));
    }

    [Fact]
    public void IsMatch_WithWrongType_FallsBackToBaseImplementation ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        
        // Pass a string instead of TestItem
        var stringValue = "Apple";
        
        // Act & Assert - Should fall back to base implementation (ToString)
        Assert.True (matcher.IsMatch ("A", stringValue));
        Assert.True (matcher.IsMatch ("App", stringValue));
    }

    [Fact]
    public void IsMatch_WithSpecialCharacters ()
    {
        // Arrange
        var treeView = new TreeView<TestItem> ();
        treeView.AspectGetter = item => item.Name;
        
        var matcher = new TreeViewCollectionNavigatorMatcher<TestItem> (treeView);
        var item = new TestItem { Id = 1, Name = "@Special-File_Name.txt" };
        
        // Act & Assert
        Assert.True (matcher.IsMatch ("@", item));
        Assert.True (matcher.IsMatch ("@S", item));
        Assert.True (matcher.IsMatch ("@Special", item));
        Assert.True (matcher.IsMatch ("@Special-File", item));
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString () => Id.ToString ();
    }
}
