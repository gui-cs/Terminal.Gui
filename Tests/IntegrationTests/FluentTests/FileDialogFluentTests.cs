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

    private Toplevel NewSaveDialog (out SaveDialog sd, bool modal = true)
    {
        return NewSaveDialog (out sd, out _, modal);
    }

    private Toplevel NewSaveDialog (out SaveDialog sd, out MockFileSystem fs, bool modal = true)
    {
        fs = CreateExampleFileSystem ();
        sd = new SaveDialog (fs) { Modal = modal };
        return sd;
    }


    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void CancelFileDialog_QuitKey_Quits (TestDriver d)
    {
        SaveDialog? sd = null;
        using var c = With.A (() => NewSaveDialog (out sd), 100, 20, d)
            .ScreenShot ("Save dialog", _out)
            .EnqueueKeyEvent (Application.QuitKey)
            .AssertTrue (sd!.Canceled)
            .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_TabThenEnter (TestDriver d)
    {
        SaveDialog? sd = null;
        using var c = With.A (() => NewSaveDialog (out sd, modal: false), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Focus<Button> (b => b.Text == "_Cancel")
                          .AssertTrue (sd!.Canceled)
                          .EnqueueKeyEvent (Key.Enter)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_LeftClickButton (TestDriver d)
    {
        SaveDialog? sd = null;
        using var c = With.A (() => NewSaveDialog (out sd), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .LeftClick<Button> (b => b.Text == "_Cancel")
                          .WriteOutLogs (_out)
                          .AssertTrue (sd!.Canceled)
                          .Stop ();
    }
    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void CancelFileDialog_UsingCancelButton_AltC (TestDriver d)
    {
        SaveDialog? sd = null;
        using var c = With.A (() => NewSaveDialog (out sd), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .EnqueueKeyEvent (Key.C.WithAlt)
                          .WriteOutLogs (_out)
                          .AssertTrue (sd!.Canceled)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void SaveFileDialog_UsingOkButton_Enter (TestDriver d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .LeftClick<Button> (b => b.Text == "_Save")
                          .WaitIteration ()
                          .AssertFalse (sd!.Canceled)
                          .AssertEqual (GetFileSystemRoot (fs!), sd!.FileName)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void SaveFileDialog_UsingOkButton_AltS (TestDriver d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .EnqueueKeyEvent (Key.S.WithAlt)
                          .WriteOutLogs (_out)
                          .AssertFalse (sd!.Canceled)
                          .AssertEqual (GetFileSystemRoot (fs!), sd!.FileName)
                          .Stop ();

    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void SaveFileDialog_UsingOkButton_TabEnter (TestDriver d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs, modal: false), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .Focus<Button> (b => b.Text == "_Save")
                          .EnqueueKeyEvent (Key.Enter)
                          .WriteOutLogs (_out)
                          .AssertFalse (sd!.Canceled)
                          .AssertEqual (GetFileSystemRoot (fs!), sd!.FileName)
                          .Stop ();
    }

    private string GetFileSystemRoot (IFileSystem fs)
    {
        return RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ?
                $@"C:{fs.Path.DirectorySeparatorChar}" :
                "/";
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void SaveFileDialog_PressingPopTree_ShouldNotChangeCancel (TestDriver d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs, modal: false), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .AssertTrue (sd!.Canceled)
                          .Focus<Button> (b => b.Text == "►_Tree")
                          .EnqueueKeyEvent (Key.Enter)
                          .ScreenShot ("After pop tree", _out)
                          .WriteOutLogs (_out)
                          .AssertTrue (sd!.Canceled)
                          .Stop ();

    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void SaveFileDialog_PopTree_AndNavigate (TestDriver d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs, modal: false), 100, 20, d)
                          .ScreenShot ("Save dialog", _out)
                          .AssertTrue (sd!.Canceled)
                          .LeftClick<Button> (b => b.Text == "►_Tree")
                          .ScreenShot ("After pop tree", _out)
                          .Focus<TreeView<IFileSystemInfo>> (_ => true)
                          .EnqueueKeyEvent (Key.CursorRight)
                          .ScreenShot ("After expand tree", _out)
                          .EnqueueKeyEvent (Key.CursorDown)
                          .ScreenShot ("After navigate down in tree", _out)
                          .EnqueueKeyEvent (Key.Enter)
                          .WaitIteration ()
                          .AssertFalse (sd!.Canceled)
                          .AssertContains ("empty-dir", sd!.FileName)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void SaveFileDialog_PopTree_AndNavigate_PreserveFilenameOnDirectoryChanges_True (TestDriver d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs, modal: false), 100, 20, d)
                          .Then (() => sd!.Style.PreserveFilenameOnDirectoryChanges = true)
                          .ScreenShot ("Save dialog", _out)
                          .AssertTrue (sd!.Canceled)
                          .Focus<TextField> (_ => true)
                          // Clear selection by pressing right in 'file path' text box
                          .EnqueueKeyEvent (Key.CursorRight)
                          .AssertIsType<TextField> (sd!.Focused)
                          // Type a filename into the dialog
                          .EnqueueKeyEvent (Key.H)
                          .EnqueueKeyEvent (Key.E)
                          .EnqueueKeyEvent (Key.L)
                          .EnqueueKeyEvent (Key.L)
                          .EnqueueKeyEvent (Key.O)
                          .WaitIteration ()
                          .ScreenShot ("After typing filename 'hello'", _out)
                          .AssertEndsWith ("hello", sd!.Path)
                          .LeftClick<Button> (b => b.Text == "►_Tree")
                          .ScreenShot ("After pop tree", _out)
                          .Focus<TreeView<IFileSystemInfo>> (_ => true)
                          .EnqueueKeyEvent (Key.CursorRight)
                          .ScreenShot ("After expand tree", _out)
                          // Because of PreserveFilenameOnDirectoryChanges we should select the new dir but keep the filename
                          .AssertEndsWith ("hello", sd!.Path)
                          .EnqueueKeyEvent (Key.CursorDown)
                          .ScreenShot ("After navigate down in tree", _out)
                          // Because of PreserveFilenameOnDirectoryChanges we should select the new dir but keep the filename
                          .AssertContains ("empty-dir", sd!.Path)
                          .AssertEndsWith ("hello", sd!.Path)
                          .EnqueueKeyEvent (Key.Enter)
                          .WaitIteration ()
                          .AssertFalse (sd!.Canceled)
                          .AssertContains ("empty-dir", sd!.FileName)
                          .WriteOutLogs (_out)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void SaveFileDialog_PopTree_AndNavigate_PreserveFilenameOnDirectoryChanges_False (TestDriver d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs, modal: false), 100, 20, d)
                          .Then (() => sd!.Style.PreserveFilenameOnDirectoryChanges = false)
                          .ScreenShot ("Save dialog", _out)
                          .AssertTrue (sd!.Canceled)
                          .Focus<TextField> (_ => true)
                          // Clear selection by pressing right in 'file path' text box
                          .EnqueueKeyEvent (Key.CursorRight)
                          .AssertIsType<TextField> (sd!.Focused)
                          // Type a filename into the dialog
                          .EnqueueKeyEvent (Key.H)
                          .EnqueueKeyEvent (Key.E)
                          .EnqueueKeyEvent (Key.L)
                          .EnqueueKeyEvent (Key.L)
                          .EnqueueKeyEvent (Key.O)
                          .WaitIteration ()
                          .ScreenShot ("After typing filename 'hello'", _out)
                          .AssertEndsWith ("hello", sd!.Path)
                          .LeftClick<Button> (b => b.Text == "►_Tree")
                          .ScreenShot ("After pop tree", _out)
                          .Focus<TreeView<IFileSystemInfo>> (_ => true)
                          .EnqueueKeyEvent (Key.CursorRight)
                          .ScreenShot ("After expand tree", _out)
                          .EnqueueKeyEvent (Key.CursorDown)
                          .ScreenShot ("After navigate down in tree", _out)
                          // PreserveFilenameOnDirectoryChanges is false so just select new path
                          .AssertEndsWith ("empty-dir", sd!.Path)
                          .AssertDoesNotContain ("hello", sd!.Path)
                          .EnqueueKeyEvent (Key.Enter)
                          .WaitIteration ()
                          .AssertFalse (sd!.Canceled)
                          .AssertContains ("empty-dir", sd!.FileName)
                          .WriteOutLogs (_out)
                          .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers_WithTrueFalseParameter))]
    public void SaveFileDialog_TableView_UpDown_PreserveFilenameOnDirectoryChanges_True (TestDriver d, bool preserve)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;
        using var c = With.A (() => NewSaveDialog (out sd, out fs, modal: false), 100, 20, d)
                          .Then (() => sd!.Style.PreserveFilenameOnDirectoryChanges = preserve)
                          .ScreenShot ("Save dialog", _out)
                          .AssertTrue (sd!.Canceled)
                          .Focus<TextField> (_ => true)
                          // Clear selection by pressing right in 'file path' text box
                          .EnqueueKeyEvent (Key.CursorRight)
                          .AssertIsType<TextField> (sd!.Focused)
                          // Type a filename into the dialog
                          .EnqueueKeyEvent (Key.H)
                          .EnqueueKeyEvent (Key.E)
                          .EnqueueKeyEvent (Key.L)
                          .EnqueueKeyEvent (Key.L)
                          .EnqueueKeyEvent (Key.O)
                          .WaitIteration ()
                          .ScreenShot ("After typing filename 'hello'", _out)
                          .AssertEndsWith ("hello", sd!.Path)
                          .Focus<TableView> (_ => true)
                          .ScreenShot ("After focus table", _out)
                          .EnqueueKeyEvent (Key.CursorDown)
                          .ScreenShot ("After down in table", _out);

        if (preserve)
        {
            c.AssertContains ("logs", sd!.Path)
             .AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("logs", sd!.Path)
             .AssertDoesNotContain ("hello", sd!.Path);
        }

        c.EnqueueKeyEvent (Key.CursorUp).ScreenShot ("After up in table", _out);

        if (preserve)
        {
            c.AssertContains ("empty-dir", sd!.Path)
             .AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd!.Path)
             .AssertDoesNotContain ("hello", sd!.Path);
        }

        c.EnqueueKeyEvent (Key.Enter)
         .ScreenShot ("After enter in table", _out); ;


        if (preserve)
        {
            c.AssertContains ("empty-dir", sd!.Path)
             .AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd!.Path)
             .AssertDoesNotContain ("hello", sd!.Path);
        }

        c.LeftClick<Button> (b => b.Text == "_Save");
        c.WaitIteration ();
        c.AssertFalse (sd!.Canceled);

        if (preserve)
        {
            c.AssertContains ("empty-dir", sd!.Path)
             .AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd!.Path)
             .AssertDoesNotContain ("hello", sd!.Path);
        }

        c.WriteOutLogs (_out);
        c.WaitIteration ();
        c.Stop ();
    }
}
