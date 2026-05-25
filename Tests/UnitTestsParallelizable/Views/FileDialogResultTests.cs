// Copilot

using System.Reflection;
using System.IO.Abstractions.TestingHelpers;

namespace UnitTests.Views;

/// <summary>
///     Tests for <see cref="FileDialog"/> refactored to inherit from <see cref="Dialog{TResult}"/>
///     where TResult is <c>IReadOnlyList&lt;string&gt;?</c>.
/// </summary>
public class FileDialogResultTests
{
    [Fact]
    public void FileDialog_Result_IsNull_WhenNotAccepted ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        // Assert - Result should be null before any acceptance
        Assert.Null (fd.Result);
        Assert.True (fd.Canceled);
    }

    [Fact]
    public void FileDialog_Result_IsNull_BeforeAcceptance ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("hello"));
        using SaveDialog sd = new TestableSaveDialog (fs);
        sd.Path = "/testdir/file1.txt";

        // Assert - Result should be null before any acceptance
        Assert.True (sd.Canceled);
        Assert.Null (sd.Result);
    }

    [Fact]
    public void FileDialog_Canceled_IsTrue_WhenResultIsNull ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        // Assert
        Assert.True (fd.Canceled);
    }

    [Fact]
    public void FileDialog_Canceled_IsFalse_WhenAccepted_ThroughCommandPipeline ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("hello"));
        using SaveDialog sd = new TestableSaveDialog (fs);
        sd.Path = "/testdir/file1.txt";

        // Act
        bool? accepted = sd.InvokeCommand (Command.Accept);

        // Assert
        Assert.True (accepted is true);
        Assert.False (sd.Canceled);
        Assert.NotNull (sd.Result);
        Assert.Single (sd.Result);
        Assert.Equal ("/testdir/file1.txt", sd.Result [0]);
    }

    [Fact]
    public void FileDialog_InheritsFromDialogOfReadOnlyListString ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        // Assert - verify the new base type
        Assert.IsAssignableFrom<Dialog<IReadOnlyList<string>?>> (fd);
    }

    [Fact]
    public void OpenDialog_FilePaths_IsEmpty_WhenCanceled ()
    {
        // Arrange
        using OpenDialog od = new TestableOpenDialog ();

        // Assert - Result is null → Canceled → FilePaths empty
        Assert.True (od.Canceled);
        Assert.Empty (od.FilePaths);
    }

    [Fact]
    public void SaveDialog_FileName_IsNull_WhenCanceled ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using SaveDialog sd = new TestableSaveDialog (fs);

        // Assert
        Assert.True (sd.Canceled);
        Assert.Null (sd.FileName);
    }

    [Fact]
    public void FileDialog_Result_MultiSelection_IsPopulated ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("a"));
        fs.AddFile ("/testdir/file2.txt", new MockFileData ("b"));
        using FileDialog fd = new TestableFileDialog (fs);

        // Act - directly set Result as would happen after multi-select acceptance
        List<string> paths = ["/testdir/file1.txt", "/testdir/file2.txt"];
        fd.Result = paths.AsReadOnly ();

        // Assert
        Assert.False (fd.Canceled);
        Assert.Equal (2, fd.Result.Count);
        Assert.Contains ("/testdir/file1.txt", fd.Result);
        Assert.Contains ("/testdir/file2.txt", fd.Result);
    }

    [Fact]
    public void FileDialog_Cancel_ClearsResult_WhenPreviouslyAccepted ()
    {
        // Copilot
        // This test validates that canceling a dialog after a previous acceptance
        // correctly clears Result to null (so Canceled == true).
        // Without the explicit `Result = null` in the Cancel path, this test would fail
        // because Result would retain the previously accepted value.

        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("hello"));
        using SaveDialog sd = new TestableSaveDialog (fs);
        sd.Path = "/testdir/file1.txt";

        // Act - first accept the dialog to set Result
        sd.InvokeCommand (Command.Accept);
        Assert.False (sd.Canceled); // Result is set — not canceled

        // Act - now simulate pressing Cancel button
        Button cancelBtn = sd.Buttons [sd.CancelButtonIndex];
        cancelBtn.InvokeCommand (Command.Accept);

        // Assert - Result should be cleared, Canceled should be true
        Assert.Null (sd.Result);
        Assert.True (sd.Canceled);
    }

    [Fact]
    public void OpenDialog_UsesInnerTableSeparatorsWithoutOuterBorders ()
    {
        using OpenDialog od = new TestableOpenDialog ();

        FieldInfo? tableViewField = typeof (FileDialog).GetField ("_tableView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tableViewField);

        TableView tableView = Assert.IsType<TableView> (tableViewField!.GetValue (od));

        Assert.True (tableView.Style.ShowVerticalCellLines);
        Assert.True (tableView.Style.ShowVerticalHeaderLines);
        Assert.False (tableView.Style.ShowVerticalCellLineForFirstColumn);
        Assert.False (tableView.Style.ShowVerticalCellLineForLastColumn);
    }

    [Fact]
    public void FileDialog_PathField_End_MovesInsertionPointToEnd ()
    {
        // Copilot
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        FieldInfo? tbPathField = typeof (FileDialog).GetField ("_tbPath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tbPathField);

        TextField tbPath = Assert.IsType<TextField> (tbPathField!.GetValue (fd));
        tbPath.Text = "/testdir/example.txt";

        tbPath.NewKeyDownEvent (Key.Home);
        Assert.Equal (0, tbPath.InsertionPoint);

        tbPath.NewKeyDownEvent (Key.End);
        Assert.Equal (tbPath.Text.Length, tbPath.InsertionPoint);
    }

    [Theory]
    [InlineData ('"')]
    [InlineData ('<')]
    [InlineData ('>')]
    [InlineData ('|')]
    [InlineData ('*')]
    [InlineData ('?')]
    public void FileDialog_PathField_BadChars_AreSuppressed (char badChar)
    {
        // Copilot
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        FieldInfo? tbPathField = typeof (FileDialog).GetField ("_tbPath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tbPathField);

        TextField tbPath = Assert.IsType<TextField> (tbPathField!.GetValue (fd));
        tbPath.Text = "/testdir/";
        tbPath.MoveEnd ();

        int insertionPointBefore = tbPath.InsertionPoint;
        string textBefore = tbPath.Text;

        tbPath.NewKeyDownEvent (new Key (badChar));

        Assert.Equal (textBefore, tbPath.Text);
        Assert.Equal (insertionPointBefore, tbPath.InsertionPoint);
    }

    [Fact]
    public void FileDialog_Accepting_Directory_From_Table_Keeps_Path_On_Opened_Directory ()
    {
        // Copilot
        MockFileSystem fs = new ();
        fs.AddDirectory ("/UI");
        fs.AddDirectory ("/UI/Window");
        fs.AddFile ("/UI/Window/file1.txt", new MockFileData ("hello"));

        using FileDialog fd = new TestableFileDialog (fs) { OpenMode = OpenMode.File };
        fd.Path = "/UI";

        FieldInfo? tableViewField = typeof (FileDialog).GetField ("_tableView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tableViewField);

        TableView tableView = Assert.IsType<TableView> (tableViewField!.GetValue (fd));

        // Find the "Window" directory row by scanning the table source rather than hard-coding an index.
        int windowRow = FindRowByName (tableView, "Window");
        Assert.True (windowRow >= 0, "Expected to find a row named 'Window' in the table.");

        tableView.SetFocus ();
        tableView.SetSelection (0, windowRow, false);

        // Path should end with the selected directory name (platform-independent check).
        Assert.EndsWith ("Window", fd.Path);

        string pathBeforeAccept = fd.Path;

        tableView.InvokeCommand (Command.Accept);

        // After accepting a directory in File mode, the path should remain unchanged
        // (navigates into the directory rather than selecting a file).
        Assert.Equal (pathBeforeAccept, fd.Path);
    }

    /// <summary>Finds a row in a <see cref="TableView"/> whose first column contains the given name.</summary>
    private static int FindRowByName (TableView tableView, string name)
    {
        ITableSource? source = tableView.Table;

        if (source is null)
        {
            return -1;
        }

        for (var row = 0; row < source.Rows; row++)
        {
            string cellText = source [row, 0]?.ToString () ?? string.Empty;

            if (cellText.Contains (name, StringComparison.Ordinal))
            {
                return row;
            }
        }

        return -1;
    }

    /// <summary>Testable subclass that exposes the internal file-system constructor.</summary>
    private sealed class TestableFileDialog : FileDialog
    {
        public TestableFileDialog (MockFileSystem fs) : base (fs) { }
    }

    /// <summary>Testable subclass for OpenDialog.</summary>
    private sealed class TestableOpenDialog : OpenDialog
    {
        public TestableOpenDialog () { }
    }

    /// <summary>Testable subclass for SaveDialog.</summary>
    private sealed class TestableSaveDialog : SaveDialog
    {
        public TestableSaveDialog (MockFileSystem fs) : base (fs) { }
    }
}
