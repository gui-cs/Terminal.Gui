using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using UnitTests;

namespace UnitTests.FileServicesTests;

public class FileSystemCollectionNavigationMatcherTests
{
    [Fact]
    [AutoInitShutdown]
    public void TreeView_LetterBasedNavigation_WorksWithAspectGetter ()
    {
        // Arrange - Create a mock file system with 3 files
        var mockFileSystem = new MockFileSystem (new Dictionary<string, MockFileData> ());
        mockFileSystem.AddFile ("apple.csv", new MockFileData (""));
        mockFileSystem.AddFile ("banana.csv", new MockFileData (""));
        mockFileSystem.AddFile ("cherry.csv", new MockFileData (""));

        var apple = mockFileSystem.FileInfo.New ("apple.csv");
        var banana = mockFileSystem.FileInfo.New ("banana.csv");
        var cherry = mockFileSystem.FileInfo.New ("cherry.csv");

        // Create TreeView with files
        var treeView = new TreeView<IFileSystemInfo> { Width = 20, Height = 10 };
        treeView.TreeBuilder = new DelegateTreeBuilder<IFileSystemInfo> (_ => null); // No children
        
        // AspectGetter returns "[ICON] Name" format
        treeView.AspectGetter = fsi => $"[ICON] {fsi.Name}";
        
        // Use FileSystemCollectionNavigationMatcher which matches on Name property
        treeView.KeystrokeNavigator.Matcher = new FileSystemCollectionNavigationMatcher ();

        treeView.AddObject (apple);
        treeView.AddObject (banana);
        treeView.AddObject (cherry);


        // Act & Assert
        // Select apple
        treeView.SelectedObject = apple;
        Assert.Equal (apple, treeView.SelectedObject);

        // Press 'b' - should navigate to banana (matches on Name, not AspectGetter result)
        treeView.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, treeView.SelectedObject);

        // Press 'c' - should navigate to cherry
        treeView.NewKeyDownEvent (Key.C);
        Assert.Equal (cherry, treeView.SelectedObject);

        // Press 'a' - should cycle back to apple
        treeView.NewKeyDownEvent (Key.A);
        Assert.Equal (apple, treeView.SelectedObject);

        treeView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TreeView_LetterBasedNavigation_WithAspectGetter_NavigatesFromNonMatching ()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem (new Dictionary<string, MockFileData> ());
        mockFileSystem.AddFile ("apple.csv", new MockFileData (""));
        mockFileSystem.AddFile ("banana.csv", new MockFileData (""));
        mockFileSystem.AddFile ("cherry.csv", new MockFileData (""));
        mockFileSystem.AddFile ("date.csv", new MockFileData (""));

        var apple = mockFileSystem.FileInfo.New ("apple.csv");
        var banana = mockFileSystem.FileInfo.New ("banana.csv");
        var cherry = mockFileSystem.FileInfo.New ("cherry.csv");
        var date = mockFileSystem.FileInfo.New ("date.csv");

        var treeView = new TreeView<IFileSystemInfo> { Width = 20, Height = 10 };
        treeView.TreeBuilder = new DelegateTreeBuilder<IFileSystemInfo> (_ => null);

        // Even when the tree view has prefixes added by the AspectGetter
        treeView.AspectGetter = fsi => $"[ICON] {fsi.Name}";

        // The matcher should be able to access the model and make its own mind up if it matches
        treeView.KeystrokeNavigator.Matcher = new FileSystemCollectionNavigationMatcher ();

        treeView.AddObject (apple);
        treeView.AddObject (banana);
        treeView.AddObject (cherry);
        treeView.AddObject (date);

        // Act & Assert
        // Select cherry (starts with 'c')
        treeView.SelectedObject = cherry;
        Assert.Equal (cherry, treeView.SelectedObject);

        // Press 'a' - should navigate to next 'a' item (wrapping around to apple)
        treeView.NewKeyDownEvent (Key.A);
        Assert.Equal (apple, treeView.SelectedObject);

        // Press 'c' - should navigate to next 'c' item (cherry)
        treeView.NewKeyDownEvent (Key.C);
        Assert.Equal (cherry, treeView.SelectedObject);

        // Press 'b' - from cherry (non-matching), should go to banana (next 'b' after cherry)
        treeView.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, treeView.SelectedObject);

        treeView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TreeView_LetterBasedNavigation_MixedObjectTypes_FileSystemMatcherFallsBackToToString ()
    {
        // Arrange - Mix file system objects with regular objects
        var mockFileSystem = new MockFileSystem (new Dictionary<string, MockFileData> ());
        mockFileSystem.AddFile ("apple.csv", new MockFileData (""));
        mockFileSystem.AddFile ("cherry.csv", new MockFileData (""));

        var apple = mockFileSystem.FileInfo.New ("apple.csv");
        var cherry = mockFileSystem.FileInfo.New ("cherry.csv");

        // Create TreeView that accepts any object type
        var treeView = new TreeView<object> { Width = 20, Height = 10 };
        treeView.TreeBuilder = new DelegateTreeBuilder<object> (_ => null);
        treeView.AspectGetter = obj => obj switch
        {
            IFileSystemInfo fsi => $"[FILE] {fsi.Name}",
            _ => obj.ToString ()
        };
        
        // Use FileSystemCollectionNavigationMatcher which handles both types
        treeView.KeystrokeNavigator.Matcher = new FileSystemCollectionNavigationMatcher ();

        // Add mixed objects: file, string, file
        treeView.AddObject (apple);
        treeView.AddObject ("banana"); // Regular string object
        treeView.AddObject (cherry);


        // Act & Assert
        // Select apple (file system object)
        treeView.SelectedObject = apple;
        Assert.Equal (apple, treeView.SelectedObject);

        // Press 'b' - should navigate to "banana" (string object, falls back to ToString)
        treeView.NewKeyDownEvent (Key.B);
        Assert.Equal ("banana", treeView.SelectedObject);

        // Press 'c' - should navigate to cherry (file system object)
        treeView.NewKeyDownEvent (Key.C);
        Assert.Equal (cherry, treeView.SelectedObject);

        // Press 'a' - should cycle back to apple
        treeView.NewKeyDownEvent (Key.A);
        Assert.Equal (apple, treeView.SelectedObject);

        // Press 'b' again - from apple, should go to banana
        treeView.NewKeyDownEvent (Key.B);
        Assert.Equal ("banana", treeView.SelectedObject);

        treeView.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TreeView_LetterBasedNavigation_CustomAspectGetter_SearchUsesAspectNotToString ()
    {
        // Arrange - Create objects where AspectGetter returns different values than ToString
        var item1 = new TestItem { Id = 1, Name = "Zebra" }; // ToString = "1", AspectGetter = "Zebra"
        var item2 = new TestItem { Id = 2, Name = "Apple" }; // ToString = "2", AspectGetter = "Apple"
        var item3 = new TestItem { Id = 3, Name = "Banana" }; // ToString = "3", AspectGetter = "Banana"

        var treeView = new TreeView<TestItem> { Width = 20, Height = 10 };
        treeView.TreeBuilder = new DelegateTreeBuilder<TestItem> (_ => null);
        
        // AspectGetter returns Name property
        treeView.AspectGetter = item => item.Name;
        
        // Use default matcher (not FileSystemCollectionNavigationMatcher)
        // This should use ToString() on the collection items passed to it
        // Since TreeView now passes the actual objects and uses AspectGetter to build the collection,
        // the matcher will search based on the aspect (Name), not ToString (Id)

        treeView.AddObject (item1); // Zebra
        treeView.AddObject (item2); // Apple
        treeView.AddObject (item3); // Banana

        treeView.BeginInit ();
        treeView.EndInit ();
        Application.Begin (treeView);

        // Act & Assert
        // Select Zebra (item1)
        treeView.SelectedObject = item1;
        Assert.Equal (item1, treeView.SelectedObject);

        // Press 'a' - should navigate to Apple (based on Name via AspectGetter), NOT fail
        // If it was using ToString(), 'a' wouldn't match anything (IDs are 1, 2, 3)
        Application.RaiseKeyDownEvent (Key.A);
        Assert.Equal (item2, treeView.SelectedObject); // Should go to Apple

        // Press 'b' - should navigate to Banana
        Application.RaiseKeyDownEvent (Key.B);
        Assert.Equal (item3, treeView.SelectedObject); // Should go to Banana

        // Press 'z' - should cycle to Zebra
        Application.RaiseKeyDownEvent (Key.Z);
        Assert.Equal (item1, treeView.SelectedObject); // Should go to Zebra

        // Verify that pressing '1', '2', '3' (the ToString values) does NOT work
        Application.RaiseKeyDownEvent (Key.D1);
        // Should stay on Zebra since no items start with '1' in their Name
        Assert.Equal (item1, treeView.SelectedObject);

        treeView.Dispose ();
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString () => Id.ToString ();
    }
}
