using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;
public class FileDialogFluentTests
{
    private readonly TextWriter _out;

    public FileDialogFluentTests (ITestOutputHelper outputHelper)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _out = new TestOutputWriter (outputHelper);
    }

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
        var sd = new SaveDialog (CreateExampleFileSystem ());
        using var c = With.A (sd, 100, 20, d)
            .ScreenShot ("Save dialog", _out)
            .Escape ()
            .Then (() => Assert.True (sd.Canceled))
            .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_TabThenEnter (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = false };
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Focus<Button> (b => b.Text == "_Cancel")
                          .Then (() => Assert.True (sd.Canceled))
                          .Enter ()
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_LeftClickButton (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ());

        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .LeftClick<Button> (b => b.Text == "_Cancel")
                          .WriteOutLogs (_out)
                          .Then (() => Assert.True (sd.Canceled))
                          .Stop ();
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
                          .Then (() => Assert.True (sd.Canceled))
                          .Stop ();
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
                          .Then (() => Assert.False (sd.Canceled))
                          .Then (() => AssertIsFileSystemRoot (fs, sd))
                          .Stop ();
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
                          .Then (() => Assert.False (sd.Canceled))
                          .Then (() => AssertIsFileSystemRoot (fs, sd))
                          .Stop ();

    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_UsingOkButton_TabEnter (V2TestDriver d)
    {
        var fs = CreateExampleFileSystem ();
        var sd = new SaveDialog (fs) { Modal = false };
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Focus<Button> (b => b.Text == "_Save")
                          .Enter ()
                          .WriteOutLogs (_out)
                          .Then (() => Assert.False (sd.Canceled))
                          .Then (() => AssertIsFileSystemRoot (fs, sd))
                          .Stop ();
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
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = false };
        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Then (() => Assert.True (sd.Canceled))
                          .Focus<Button> (b => b.Text == "►►")
                          .Enter ()
                          .ScreenShot ("After pop tree", _out)
                          .WriteOutLogs (_out)
                          .Then (() => Assert.True (sd.Canceled))
                          .Stop ();

    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_PopTree_AndNavigate (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = false };

        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Then (() => Assert.True (sd.Canceled))
                          .LeftClick<Button> (b => b.Text == "►►")
                          .ScreenShot ("After pop tree", _out)
                          .Focus<TreeView<IFileSystemInfo>> (_ => true)
                          .Right ()
                          .ScreenShot ("After expand tree", _out)
                          .Down ()
                          .ScreenShot ("After navigate down in tree", _out)
                          .Enter ()
                          .WaitIteration ()
                          .Then (() => Assert.False (sd.Canceled))
                          .AssertContains ("empty-dir", sd.FileName)
                          .WriteOutLogs (_out)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_PopTree_AndNavigate_PreserveFilenameOnDirectoryChanges_True (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = false };
        sd.Style.PreserveFilenameOnDirectoryChanges = true;

        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Then (() => Assert.True (sd.Canceled))
                          .Focus<TextField> (_=>true)
                          // Clear selection by pressing right in 'file path' text box
                          .RaiseKeyDownEvent (Key.CursorRight)
                          .AssertIsType <TextField>(sd.Focused)
                          // Type a filename into the dialog
                          .RaiseKeyDownEvent (Key.H)
                          .RaiseKeyDownEvent (Key.E)
                          .RaiseKeyDownEvent (Key.L)
                          .RaiseKeyDownEvent (Key.L)
                          .RaiseKeyDownEvent (Key.O)
                          .WaitIteration ()
                          .ScreenShot ("After typing filename 'hello'", _out)
                          .AssertEndsWith ("hello", sd.Path)
                          .LeftClick<Button> (b => b.Text == "►►")
                          .ScreenShot ("After pop tree", _out)
                          .Focus<TreeView<IFileSystemInfo>> (_ => true)
                          .Right ()
                          .ScreenShot ("After expand tree", _out)
                          // Because of PreserveFilenameOnDirectoryChanges we should select the new dir but keep the filename
                          .AssertEndsWith ("hello", sd.Path)
                          .Down ()
                          .ScreenShot ("After navigate down in tree", _out)
                          // Because of PreserveFilenameOnDirectoryChanges we should select the new dir but keep the filename
                          .AssertContains ("empty-dir",sd.Path)
                          .AssertEndsWith ("hello", sd.Path)
                          .Enter ()
                          .WaitIteration ()
                          .Then (() => Assert.False (sd.Canceled))
                          .AssertContains ("empty-dir", sd.FileName)
                          .WriteOutLogs (_out)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void SaveFileDialog_PopTree_AndNavigate_PreserveFilenameOnDirectoryChanges_False (V2TestDriver d)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = false };
        sd.Style.PreserveFilenameOnDirectoryChanges = false;

        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Then (() => Assert.True (sd.Canceled))
                          .Focus<TextField> (_ => true)
                          // Clear selection by pressing right in 'file path' text box
                          .RaiseKeyDownEvent (Key.CursorRight)
                          .AssertIsType<TextField> (sd.Focused)
                          // Type a filename into the dialog
                          .RaiseKeyDownEvent (Key.H)
                          .RaiseKeyDownEvent (Key.E)
                          .RaiseKeyDownEvent (Key.L)
                          .RaiseKeyDownEvent (Key.L)
                          .RaiseKeyDownEvent (Key.O)
                          .WaitIteration ()
                          .ScreenShot ("After typing filename 'hello'", _out)
                          .AssertEndsWith ("hello", sd.Path)
                          .LeftClick<Button> (b => b.Text == "►►")
                          .ScreenShot ("After pop tree", _out)
                          .Focus<TreeView<IFileSystemInfo>> (_ => true)
                          .Right ()
                          .ScreenShot ("After expand tree", _out)
                          .Down ()
                          .ScreenShot ("After navigate down in tree", _out)
                          // PreserveFilenameOnDirectoryChanges is false so just select new path
                          .AssertEndsWith ("empty-dir", sd.Path)
                          .AssertDoesNotContain ("hello", sd.Path)
                          .Enter ()
                          .WaitIteration ()
                          .Then (() => Assert.False (sd.Canceled))
                          .AssertContains ("empty-dir", sd.FileName)
                          .WriteOutLogs (_out)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers_WithTrueFalseParameter))]
    public void SaveFileDialog_TableView_UpDown_PreserveFilenameOnDirectoryChanges_True (V2TestDriver d, bool preserve)
    {
        var sd = new SaveDialog (CreateExampleFileSystem ()) { Modal = false };
        sd.Style.PreserveFilenameOnDirectoryChanges = preserve;

        using var c = With.A (sd, 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Then (() => Assert.True (sd.Canceled))
                          .Focus<TextField> (_ => true)
                          // Clear selection by pressing right in 'file path' text box
                          .RaiseKeyDownEvent (Key.CursorRight)
                          .AssertIsType<TextField> (sd.Focused)
                          // Type a filename into the dialog
                          .RaiseKeyDownEvent (Key.H)
                          .RaiseKeyDownEvent (Key.E)
                          .RaiseKeyDownEvent (Key.L)
                          .RaiseKeyDownEvent (Key.L)
                          .RaiseKeyDownEvent (Key.O)
                          .WaitIteration ()
                          .ScreenShot ("After typing filename 'hello'", _out)
                          .AssertEndsWith ("hello", sd.Path)
                          .Focus<TableView> (_ => true)
                          .ScreenShot ("After focus table", _out)
                          .Down ()
                          .ScreenShot ("After down in table", _out);

        if (preserve)
        {
            c.AssertContains ("logs", sd.Path)
             .AssertEndsWith ("hello", sd.Path);
        }
        else
        {
            c.AssertContains ("logs", sd.Path)
             .AssertDoesNotContain ("hello", sd.Path);
        }

        c.Up ()
         .ScreenShot ("After up in table", _out);

        if (preserve)
        {
            c.AssertContains ("empty-dir", sd.Path)
             .AssertEndsWith ("hello", sd.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd.Path)
             .AssertDoesNotContain ("hello", sd.Path);
        }

        c.Enter ()
         .ScreenShot ("After enter in table", _out); ;


        if (preserve)
        {
            c.AssertContains ("empty-dir", sd.Path)
             .AssertEndsWith ("hello", sd.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd.Path)
             .AssertDoesNotContain ("hello", sd.Path);
        }

        c.LeftClick<Button> (b => b.Text == "_Save");
        c.AssertFalse (sd.Canceled);

        if (preserve)
        {
            c.AssertContains ("empty-dir", sd.Path)
             .AssertEndsWith ("hello", sd.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd.Path)
             .AssertDoesNotContain ("hello", sd.Path);
        }

        c.WriteOutLogs (_out)
         .Stop ();
    }
}
