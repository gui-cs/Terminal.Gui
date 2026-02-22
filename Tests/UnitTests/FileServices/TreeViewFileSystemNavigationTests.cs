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
}
