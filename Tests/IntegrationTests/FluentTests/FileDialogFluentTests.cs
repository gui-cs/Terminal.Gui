using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using Terminal.Gui;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;
public class FileDialogFluentTests
{
    private readonly TextWriter _out;

    public FileDialogFluentTests (ITestOutputHelper outputHelper) { _out = new TestOutputWriter (outputHelper); }

    private MockFileSystem CreateExampleFileSystem ()
    {

        // Optional: use Ordinal to simulate Linux-style case sensitivity
        var mockFileSystem = new MockFileSystem (new Dictionary<string, MockFileData> ());

        string testDir = mockFileSystem.Path.Combine ("test-dir");
        string subDir = mockFileSystem.Path.Combine (testDir, "sub-dir");
        string logsDir = "logs";
        string emptyDir = "empty-dir";

        // Add files
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (testDir, "file1.txt"), new MockFileData ("Hello, this is file 1."));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (testDir, "file2.txt"), new MockFileData ("Hello, this is file 2."));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (subDir, "nested-file.txt"), new MockFileData ("This is a nested file."));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (logsDir, "log1.log"), new MockFileData ("Log entry 1"));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (logsDir, "log2.log"), new MockFileData ("Log entry 2"));

        // Create an empty directory
        mockFileSystem.AddDirectory (emptyDir);

        return mockFileSystem;
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void CancelFileDialog_UsingEscape (V2TestDriver d)
    {
        var sd = new SaveDialog ( CreateExampleFileSystem ());
        using var c = With.A (sd, 100, 20, d)
            .ScreenShot ("Save dialog",_out)
            .Escape()
            .Stop ();

        Assert.True (sd.Canceled);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_TabThenEnter (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ());
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Focus <Button>(b=> b.Text == "_Cancel")
                          .Enter ()
                          .Stop ();

        Assert.True (sd.Canceled);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_LeftClickButton (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ());
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .LeftClick <Button> (b => b.Text == "_Cancel")
                          .Stop ()
                          .WriteOutLogs (_out);

        Assert.True (sd.Canceled);
    }
    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_AltC (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ());
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Send (Key.C.WithAlt)
                          .WriteOutLogs (_out)
                          .Stop ();

        Assert.True (sd.Canceled);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_UsingOkButton_Enter (V2TestDriver d)
    {
        var fs = CreateExampleFileSystem ();
        var sd = new SaveDialog (fs);
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .LeftClick<Button> (b => b.Text == "_Save")
                          .WriteOutLogs (_out)
                          .Stop ();

        Assert.False (sd.Canceled);
        AssertIsFileSystemRoot (fs, sd);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_UsingOkButton_AltS (V2TestDriver d)
    {
        var fs = CreateExampleFileSystem ();
        var sd = new SaveDialog (fs);
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Send (Key.S.WithAlt)
                          .WriteOutLogs (_out)
                          .Stop ();

        Assert.False (sd.Canceled);
        AssertIsFileSystemRoot (fs, sd);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_UsingOkButton_TabEnter (V2TestDriver d)
    {
        var fs = CreateExampleFileSystem ();
        var sd = new SaveDialog (fs);
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Focus <Button> (b => b.Text == "_Save")
                          .Enter ()
                          .WriteOutLogs (_out)
                          .Stop ();

        Assert.False (sd.Canceled);
        AssertIsFileSystemRoot (fs,sd);
    }

    private void AssertIsFileSystemRoot (IFileSystem fs, SaveDialog sd)
    {
        var expectedPath =
            RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ?
                $@"C:{fs.Path.DirectorySeparatorChar}" :
                "/";

        Assert.Equal (expectedPath, sd.FileName);

    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_PressingPopTree_ShouldNotChangeCancel (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = true };
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .AssertTrue (sd.Canceled)
                          .Focus<Button> (b => b.Text == "►►")
                          .Enter ()
                          .ScreenShot ("After pop tree", _out)
                          .AssertTrue (sd.Canceled)
                          .WriteOutLogs (_out)
                          .Stop ();

        Assert.True(sd.Canceled);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_PopTree_AndNavigate (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = true };

        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .AssertTrue (sd.Canceled)
                          .LeftClick <Button> (b => b.Text == "►►")
                          .ScreenShot ("After pop tree", _out)
                          .Focus <TreeView<IFileSystemInfo>> (_ => true)
                          .Right ()
                          .ScreenShot ("After expand tree", _out)
                          .Down ()
                          .ScreenShot ("After navigate down in tree", _out)
                          .Enter ()
                          .WaitIteration ()
                          .AssertFalse (sd.Canceled)
                          .AssertContains ("empty-dir", sd.FileName)
                          .WriteOutLogs (_out)
                          .Stop ();

        Assert.False (sd.Canceled);
    }
}
