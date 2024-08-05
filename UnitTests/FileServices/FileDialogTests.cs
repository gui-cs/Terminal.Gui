using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Terminal.Gui.FileServicesTests;

public class FileDialogTests (ITestOutputHelper output)
{
    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    [AutoInitShutdown]
    public void CancelSelection (bool cancel)
    {
        FileDialog dlg = GetInitializedFileDialog ();
        string openIn = Path.Combine (Environment.CurrentDirectory, "zz");
        Directory.CreateDirectory (openIn);
        dlg.Path = openIn + Path.DirectorySeparatorChar;

        dlg.FilesSelected += (s, e) => e.Cancel = cancel;

        //pressing enter will complete the current selection
        // unless the event cancels the confirm
        Send ('\n', ConsoleKey.Enter);

        Assert.Equal (cancel, dlg.Canceled);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DirectTyping_Allowed ()
    {
        FileDialog dlg = GetInitializedFileDialog ();
        TextField tf = dlg.Subviews.OfType<TextField> ().First (t => t.HasFocus);
        tf.ClearAllSelection ();
        tf.CursorPosition = tf.Text.Length;
        Assert.True (tf.HasFocus);

        SendSlash ();

        Assert.Equal (
                      new DirectoryInfo (Environment.CurrentDirectory + Path.DirectorySeparatorChar).FullName,
                      new DirectoryInfo (dlg.Path + Path.DirectorySeparatorChar).FullName
                     );

        // continue typing the rest of the path
        Send ("BOB");
        Send ('.', ConsoleKey.OemPeriod);
        Send ("CSV");

        Assert.True (dlg.Canceled);

        Send ('\n', ConsoleKey.Enter);
        Assert.False (dlg.Canceled);
        Assert.Equal ("bob.csv", Path.GetFileName (dlg.Path));
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DirectTyping_AutoComplete ()
    {
        FileDialog dlg = GetInitializedFileDialog ();
        string openIn = Path.Combine (Environment.CurrentDirectory, "zz");

        Directory.CreateDirectory (openIn);

        string expectedDest = Path.Combine (openIn, "xx");
        Directory.CreateDirectory (expectedDest);

        dlg.Path = openIn + Path.DirectorySeparatorChar;

        Send ("X");

        // nothing selected yet
        Assert.True (dlg.Canceled);
        Assert.Equal ("x", Path.GetFileName (dlg.Path));

        // complete auto typing
        Send ('\t', ConsoleKey.Tab);

        // but do not close dialog
        Assert.True (dlg.Canceled);
        Assert.EndsWith ("xx" + Path.DirectorySeparatorChar, dlg.Path);

        // press enter again to confirm the dialog
        Send ('\n', ConsoleKey.Enter);
        Assert.False (dlg.Canceled);
        Assert.EndsWith ("xx" + Path.DirectorySeparatorChar, dlg.Path);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DoNotConfirmSelectionWhenFindFocused ()
    {
        FileDialog dlg = GetInitializedFileDialog ();
        string openIn = Path.Combine (Environment.CurrentDirectory, "zz");
        Directory.CreateDirectory (openIn);
        dlg.Path = openIn + Path.DirectorySeparatorChar;
#if BROKE_IN_2927
        Send ('f', ConsoleKey.F, false, true, false);
#else
        Application.OnKeyDown (Key.Tab);
        Application.OnKeyDown (Key.Tab);
        Application.OnKeyDown (Key.Tab);
#endif

        Assert.IsType<TextField> (dlg.MostFocused);
        var tf = (TextField)dlg.MostFocused;
        Assert.Equal ("Enter Search", tf.Caption);

        // Dialog has not yet been confirmed with a choice
        Assert.True (dlg.Canceled);

        //pressing enter while search focused should not confirm path
        Send ('\n', ConsoleKey.Enter);

        Assert.True (dlg.Canceled);

        // tabbing out of search 
        Send ('\t', ConsoleKey.Tab);

        //should allow enter to confirm path
        Send ('\n', ConsoleKey.Enter);

        // Dialog has not yet been confirmed with a choice
        Assert.False (dlg.Canceled);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DotDot_MovesToRoot_ThenPressBack ()
    {
        FileDialog dlg = GetDialog ();
        dlg.OpenMode = OpenMode.Directory;
        dlg.AllowsMultipleSelection = true;
        var selected = false;
        dlg.FilesSelected += (s, e) => { selected = true; };

        AssertIsTheStartingDirectory (dlg.Path);

        Assert.IsType<TextField> (dlg.MostFocused);
        Send ('v', ConsoleKey.DownArrow);
        Assert.IsType<TableView> (dlg.MostFocused);

        // ".." should be the first thing selected
        // ".." should not mess with the displayed path
        AssertIsTheStartingDirectory (dlg.Path);

        // Accept navigation up a directory
        Send ('\n', ConsoleKey.Enter);

        AssertIsTheRootDirectory (dlg.Path);

        Assert.True (dlg.Canceled);
        Assert.False (selected);

        // Now press the back button (in table view)
        Send ('<', ConsoleKey.Backspace);

        // Should move us back to the root
        AssertIsTheStartingDirectory (dlg.Path);

        Assert.True (dlg.Canceled);
        Assert.False (selected);
        dlg.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void MultiSelectDirectory_CannotToggleDotDot (bool acceptWithEnter)
    {
        FileDialog dlg = GetDialog ();
        dlg.OpenMode = OpenMode.Directory;
        dlg.AllowsMultipleSelection = true;
        IReadOnlyCollection<string> eventMultiSelected = null;
        dlg.FilesSelected += (s, e) => { eventMultiSelected = e.Dialog.MultiSelected; };

        Assert.IsType<TextField> (dlg.MostFocused);
        Send ('v', ConsoleKey.DownArrow);
        Assert.IsType<TableView> (dlg.MostFocused);

        // Try to toggle '..'
        Send (' ', ConsoleKey.Spacebar);
        Send ('v', ConsoleKey.DownArrow);

        // Toggle subfolder
        Send (' ', ConsoleKey.Spacebar);

        Assert.True (dlg.Canceled);

        if (acceptWithEnter)
        {
            Send ('\n', ConsoleKey.Enter);
        }
        else
        {
            Send ('O', ConsoleKey.O, false, true);
        }

        Assert.False (dlg.Canceled);

        Assert.Multiple (
                         () =>
                         {
                             // Only the subfolder should be selected
                             Assert.Single (dlg.MultiSelected);
                             AssertIsTheSubfolder (dlg.Path);
                             AssertIsTheSubfolder (dlg.MultiSelected.Single ());
                         },
                         () =>
                         {
                             // Event should also agree with the final state
                             Assert.NotNull (eventMultiSelected);
                             Assert.Single (eventMultiSelected);
                             AssertIsTheSubfolder (eventMultiSelected.Single ());
                         }
                        );
        dlg.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void MultiSelectDirectory_CanToggleThenAccept (bool acceptWithEnter)
    {
        FileDialog dlg = GetDialog ();
        dlg.OpenMode = OpenMode.Directory;
        dlg.AllowsMultipleSelection = true;
        IReadOnlyCollection<string> eventMultiSelected = null;
        dlg.FilesSelected += (s, e) => { eventMultiSelected = e.Dialog.MultiSelected; };

        Assert.IsType<TextField> (dlg.MostFocused);
        Send ('v', ConsoleKey.DownArrow);
        Assert.IsType<TableView> (dlg.MostFocused);

        // Move selection to subfolder
        Send ('v', ConsoleKey.DownArrow);

        // Toggle subfolder
        Send (' ', ConsoleKey.Spacebar);

        Assert.True (dlg.Canceled);

        if (acceptWithEnter)
        {
            Send ('\n', ConsoleKey.Enter);
        }
        else
        {
            Send ('O', ConsoleKey.O, false, true);
        }

        Assert.False (dlg.Canceled);

        Assert.Multiple (
                         () =>
                         {
                             // Only the subfolder should be selected
                             Assert.Single (dlg.MultiSelected);
                             AssertIsTheSubfolder (dlg.Path);
                             AssertIsTheSubfolder (dlg.MultiSelected.Single ());
                         },
                         () =>
                         {
                             // Event should also agree with the final state
                             Assert.NotNull (eventMultiSelected);
                             Assert.Single (eventMultiSelected);
                             AssertIsTheSubfolder (eventMultiSelected.Single ());
                         }
                        );
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MultiSelectDirectory_EnterOpensFolder ()
    {
        FileDialog dlg = GetDialog ();
        dlg.OpenMode = OpenMode.Directory;
        dlg.AllowsMultipleSelection = true;
        IReadOnlyCollection<string> eventMultiSelected = null;
        dlg.FilesSelected += (s, e) => { eventMultiSelected = e.Dialog.MultiSelected; };

        Assert.IsType<TextField> (dlg.MostFocused);
        Send ('v', ConsoleKey.DownArrow);
        Assert.IsType<TableView> (dlg.MostFocused);

        // Move selection to subfolder
        Send ('v', ConsoleKey.DownArrow);

        Send ('\n', ConsoleKey.Enter);

        // Path should update to the newly opened folder
        AssertIsTheSubfolder (dlg.Path);

        // No selection will have been confirmed
        Assert.True (dlg.Canceled);
        Assert.Empty (dlg.MultiSelected);
        Assert.Null (eventMultiSelected);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void OnLoad_TextBoxIsFocused ()
    {
        FileDialog dlg = GetInitializedFileDialog ();

        View tf = dlg.Subviews.FirstOrDefault (t => t.HasFocus);
        Assert.NotNull (tf);
        Assert.IsType<TextField> (tf);
        dlg.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true, true)]
    [InlineData (true, false)]
    [InlineData (false, true)]
    [InlineData (false, false)]
    public void PickDirectory_ArrowNavigation (bool openModeMixed, bool multiple)
    {
        FileDialog dlg = GetDialog ();
        dlg.OpenMode = openModeMixed ? OpenMode.Mixed : OpenMode.Directory;
        dlg.AllowsMultipleSelection = multiple;

        Assert.IsType<TextField> (dlg.MostFocused);
        Send ('v', ConsoleKey.DownArrow);
        Assert.IsType<TableView> (dlg.MostFocused);

        // Should be selecting ..
        Send ('v', ConsoleKey.DownArrow);

        // Down to the directory
        Assert.True (dlg.Canceled);

        // Alt+O to open (enter would just navigate into the child dir)
        Send ('O', ConsoleKey.O, false, true);
        Assert.False (dlg.Canceled);

        AssertIsTheSubfolder (dlg.Path);
        dlg.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true, true)]
    [InlineData (true, false)]
    [InlineData (false, true)]
    [InlineData (false, false)]
    public void PickDirectory_DirectTyping (bool openModeMixed, bool multiple)
    {
        FileDialog dlg = GetDialog ();
        dlg.OpenMode = openModeMixed ? OpenMode.Mixed : OpenMode.Directory;
        dlg.AllowsMultipleSelection = multiple;

        // whe first opening the text field will have select all on
        // so to add to current path user must press End or right
        Send ('>', ConsoleKey.LeftArrow);
        Send ('>', ConsoleKey.RightArrow);

        Send ("SUBFOLDER");

        // Dialog has not yet been confirmed with a choice
        Assert.True (dlg.Canceled);

        // Now it has
        Send ('\n', ConsoleKey.Enter);
        Assert.False (dlg.Canceled);
        AssertIsTheSubfolder (dlg.Path);
        dlg.Dispose ();
    }

    [Theory]
    [InlineData (".csv", null, false)]
    [InlineData (".csv", "", false)]
    [InlineData (".csv", "c:\\MyFile.csv", true)]
    [InlineData (".csv", "c:\\MyFile.CSV", true)]
    [InlineData (".csv", "c:\\MyFile.csv.bak", false)]
    public void TestAllowedType_Basic (string allowed, string candidate, bool expected)
    {
        Assert.Equal (expected, new AllowedType ("Test", allowed).IsAllowed (candidate));
    }

    [Theory]
    [InlineData (".Designer.cs", "c:\\MyView.Designer.cs", true)]
    [InlineData (".Designer.cs", "c:\\temp/MyView.Designer.cs", true)]
    [InlineData (".Designer.cs", "MyView.Designer.cs", true)]
    [InlineData (".Designer.cs", "c:\\MyView.DESIGNER.CS", true)]
    [InlineData (".Designer.cs", "MyView.cs", false)]
    public void TestAllowedType_DoubleBarreled (string allowed, string candidate, bool expected)
    {
        Assert.Equal (expected, new AllowedType ("Test", allowed).IsAllowed (candidate));
    }

    [Theory]
    [InlineData ("Dockerfile", "c:\\temp\\Dockerfile", true)]
    [InlineData ("Dockerfile", "Dockerfile", true)]
    [InlineData ("Dockerfile", "someimg.Dockerfile", true)]
    public void TestAllowedType_SpecificFile (string allowed, string candidate, bool expected)
    {
        Assert.Equal (expected, new AllowedType ("Test", allowed).IsAllowed (candidate));
    }

    [Fact]
    [AutoInitShutdown]
    public void TestDirectoryContents_Linux ()
    {
        if (IsWindows ())
        {
            return;
        }

        FileDialog fd = GetLinuxDialog ();
        fd.Title = string.Empty;

        fd.Style.Culture = new ("en-US");

        fd.Draw ();

        var expected =
            @$"
┌─────────────────────────────────────────────────────────────────────────┐
│/demo/                                                                   │
│{
    CM.Glyphs.LeftBracket
}▲{
    CM.Glyphs.RightBracket
}                                                                      │
│┌────────────┬──────────┬──────────────────────────────┬────────────────┐│
││Filename (▲)│Size      │Modified                      │Type            ││
│├────────────┼──────────┼──────────────────────────────┼────────────────┤│
││..          │          │                              │<Directory>     ││
││/subfolder  │          │2002-01-01T22:42:10           │<Directory>     ││
││image.gif   │4.00 B    │2002-01-01T22:42:10           │.gif            ││
││jQuery.js   │7.00 B    │2001-01-01T11:44:42           │.js             ││
│                                                                         │
│                                                                         │
│                                                                         │
│{
    CM.Glyphs.LeftBracket
} ►► {
    CM.Glyphs.RightBracket
} Enter Search                                 {
    CM.Glyphs.LeftBracket
}{
    CM.Glyphs.LeftDefaultIndicator
} OK {
    CM.Glyphs.RightDefaultIndicator
}{
    CM.Glyphs.RightBracket
} {
    CM.Glyphs.LeftBracket
} Cancel {
    CM.Glyphs.RightBracket
}  │
└─────────────────────────────────────────────────────────────────────────┘
";
        TestHelpers.AssertDriverContentsAre (expected, output, ignoreLeadingWhitespace: true);
        fd.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestDirectoryContents_Windows ()
    {
        if (!IsWindows ())
        {
            return;
        }

        FileDialog fd = GetWindowsDialog ();
        fd.Title = string.Empty;

        fd.Style.Culture = new ("en-US");

        fd.Draw ();

        var expected =
            @$"
┌─────────────────────────────────────────────────────────────────────────┐
│c:\demo\                                                                 │
│{
    CM.Glyphs.LeftBracket
}▲{
    CM.Glyphs.RightBracket
}                                                                      │
│┌────────────┬──────────┬──────────────────────────────┬────────────────┐│
││Filename (▲)│Size      │Modified                      │Type            ││
│├────────────┼──────────┼──────────────────────────────┼────────────────┤│
││..          │          │                              │<Directory>     ││
││\subfolder  │          │2002-01-01T22:42:10           │<Directory>     ││
││image.gif   │4.00 B    │2002-01-01T22:42:10           │.gif            ││
││jQuery.js   │7.00 B    │2001-01-01T11:44:42           │.js             ││
││mybinary.exe│7.00 B    │2001-01-01T11:44:42           │.exe            ││
│                                                                         │
│                                                                         │
│{
    CM.Glyphs.LeftBracket
} ►► {
    CM.Glyphs.RightBracket
} Enter Search                                 {
    CM.Glyphs.LeftBracket
}{
    CM.Glyphs.LeftDefaultIndicator
} OK {
    CM.Glyphs.RightDefaultIndicator
}{
    CM.Glyphs.RightBracket
} {
    CM.Glyphs.LeftBracket
} Cancel {
    CM.Glyphs.RightBracket
}  │
└─────────────────────────────────────────────────────────────────────────┘
";
        TestHelpers.AssertDriverContentsAre (expected, output, ignoreLeadingWhitespace: true);
        fd.Dispose ();
    }

    private void AssertIsTheRootDirectory (string path)
    {
        if (IsWindows ())
        {
            Assert.Equal (@"c:\", path);
        }
        else
        {
            Assert.Equal ("/", path);
        }
    }

    private void AssertIsTheStartingDirectory (string path)
    {
        if (IsWindows ())
        {
            Assert.Equal (@"c:\demo\", path);
        }
        else
        {
            Assert.Equal ("/demo/", path);
        }
    }

    private void AssertIsTheSubfolder (string path)
    {
        if (IsWindows ())
        {
            Assert.Equal (@"c:\demo\subfolder", path);
        }
        else
        {
            Assert.Equal ("/demo/subfolder", path);
        }
    }

    private void Begin (FileDialog dlg)
    {
        dlg.BeginInit ();
        dlg.EndInit ();
        Application.Begin (dlg);
    }

    private FileDialog GetDialog () { return IsWindows () ? GetWindowsDialog () : GetLinuxDialog (); }

    private FileDialog GetInitializedFileDialog ()
    {

        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;

        var dlg = new FileDialog ();
        Begin (dlg);

        return dlg;
    }

    private FileDialog GetLinuxDialog ()
    {
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;

        // Arrange
        var fileSystem = new MockFileSystem (new Dictionary<string, MockFileData> (), "/");
        fileSystem.MockTime (() => new (2010, 01, 01, 11, 12, 43));

        fileSystem.AddFile (
                            @"/myfile.txt",
                            new ("Testing is meh.") { LastWriteTime = new DateTime (2001, 01, 01, 11, 12, 11) }
                           );

        fileSystem.AddFile (
                            @"/demo/jQuery.js",
                            new ("some js") { LastWriteTime = new DateTime (2001, 01, 01, 11, 44, 42) }
                           );

        fileSystem.AddFile (
                            @"/demo/image.gif",
                            new (new byte [] { 0x12, 0x34, 0x56, 0xd2 })
                            {
                                LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10)
                            }
                           );

        var m = (MockDirectoryInfo)fileSystem.DirectoryInfo.New (@"/demo/subfolder");
        m.Create ();
        m.LastWriteTime = new (2002, 01, 01, 22, 42, 10);

        fileSystem.AddFile (
                            @"/demo/subfolder/image2.gif",
                            new (new byte [] { 0x12, 0x34, 0x56, 0xd2 })
                            {
                                LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10)
                            }
                           );

        var fd = new FileDialog (fileSystem) { Height = 15, Width = 75 };
        fd.Path = @"/demo/";
        Begin (fd);

        return fd;
    }

    private FileDialog GetWindowsDialog ()
    {
        // Override CM
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;

        // Arrange
        var fileSystem = new MockFileSystem (new Dictionary<string, MockFileData> (), @"c:\");
        fileSystem.MockTime (() => new (2010, 01, 01, 11, 12, 43));

        fileSystem.AddFile (
                            @"c:\myfile.txt",
                            new ("Testing is meh.") { LastWriteTime = new DateTime (2001, 01, 01, 11, 12, 11) }
                           );

        fileSystem.AddFile (
                            @"c:\demo\jQuery.js",
                            new ("some js") { LastWriteTime = new DateTime (2001, 01, 01, 11, 44, 42) }
                           );

        fileSystem.AddFile (
                            @"c:\demo\mybinary.exe",
                            new ("some js") { LastWriteTime = new DateTime (2001, 01, 01, 11, 44, 42) }
                           );

        fileSystem.AddFile (
                            @"c:\demo\image.gif",
                            new (new byte [] { 0x12, 0x34, 0x56, 0xd2 })
                            {
                                LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10)
                            }
                           );

        var m = (MockDirectoryInfo)fileSystem.DirectoryInfo.New (@"c:\demo\subfolder");
        m.Create ();
        m.LastWriteTime = new (2002, 01, 01, 22, 42, 10);

        fileSystem.AddFile (
                            @"c:\demo\subfolder\image2.gif",
                            new (new byte [] { 0x12, 0x34, 0x56, 0xd2 })
                            {
                                LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10)
                            }
                           );

        var fd = new FileDialog (fileSystem) { Height = 15, Width = 75 };
        fd.Path = @"c:\demo\";
        Begin (fd);

        return fd;
    }
    /*
            [Fact, AutoInitShutdown]
            public void Autocomplete_NoSuggestion_WhenTextMatchesExactly ()
            {
                var tb = new TextFieldWithAppendAutocomplete ();
                ForceFocus (tb);

                tb.Text = "/bob/fish";
                tb.CursorPosition = tb.Text.Length;
                tb.GenerateSuggestions (null, "fish", "fishes");

                // should not report success for autocompletion because we already have that exact
                // string
                Assert.False (tb.AcceptSelectionIfAny ());
            }

            [Fact, AutoInitShutdown]
            public void Autocomplete_AcceptSuggstion ()
            {
                var tb = new TextFieldWithAppendAutocomplete ();
                ForceFocus (tb);

                tb.Text = @"/bob/fi";
                tb.CursorPosition = tb.Text.Length;
                tb.GenerateSuggestions (null, "fish", "fishes");

                Assert.True (tb.AcceptSelectionIfAny ());
                Assert.Equal (@"/bob/fish", tb.Text);
            }*/

    private bool IsWindows () { return RuntimeInformation.IsOSPlatform (OSPlatform.Windows); }

    private void Send (char ch, ConsoleKey ck, bool shift = false, bool alt = false, bool control = false)
    {
        Application.Driver?.SendKeys (ch, ck, shift, alt, control);
    }

    private void Send (string chars)
    {
        foreach (char ch in chars)
        {
            Application.Driver?.SendKeys (ch, ConsoleKey.NoName, false, false, false);
        }
    }

    private void SendSlash ()
    {
        if (Path.DirectorySeparatorChar == '/')
        {
            Send ('/', ConsoleKey.Separator);
        }
        else
        {
            Send ('\\', ConsoleKey.Separator);
        }
    }
}
