// Copilot

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
