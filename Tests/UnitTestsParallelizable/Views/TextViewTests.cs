using System.Text;

namespace Terminal.Gui.ViewsTests;

public class TextViewTests
{
    [Fact]
    public void CloseFile_Throws_If_FilePath_Is_Null ()
    {
        var tv = new TextView ();
        Assert.Throws<ArgumentNullException> (() => tv.CloseFile ());
    }

    [Fact]
    public void ContentsChanged_Event_Fires_ClearHistoryChanges ()
    {
        var eventcount = 0;

        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 50, Height = 10, Text = text };
        tv.ContentsChanged += (s, e) => { eventcount++; };

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal (
                      $"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);

        var expectedEventCount = 1; // for ENTER key
        Assert.Equal (expectedEventCount, eventcount);

        tv.ClearHistoryChanges ();
        expectedEventCount = 2;
        Assert.Equal (expectedEventCount, eventcount);
    }

    [Fact]
    public void ContentsChanged_Event_Fires_LoadStream_By_Calling_HistoryText_Clear ()
    {
        var eventcount = 0;

        var tv = new TextView { Width = 50, Height = 10 };
        tv.ContentsChanged += (s, e) => { eventcount++; };

        var text = "This is the first line.\r\nThis is the second line.\r\n";
        tv.Load (new MemoryStream (Encoding.ASCII.GetBytes (text)));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );

        Assert.Equal (1, eventcount);
    }

    [Fact]
    public void ContentsChanged_Event_Fires_On_LoadFile_By_Calling_HistoryText_Clear ()
    {
        var eventcount = 0;

        var tv = new TextView { Width = 50, Height = 10 };
        tv.BeginInit ();
        tv.EndInit ();

        tv.ContentsChanged += (s, e) => { eventcount++; };

        var fileName = "textview.txt";
        File.WriteAllText (fileName, "This is the first line.\r\nThis is the second line.\r\n");

        tv.Load (fileName);
        Assert.Equal (1, eventcount);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );
    }

    [Fact]
    public void GetRegion_StringFromRunes_Environment_NewLine ()
    {
        var tv = new TextView { Text = $"1{Environment.NewLine}2" };

        Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
        Assert.Equal ("", tv.SelectedText);

        tv.SelectAll ();
        Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
        Assert.Equal ($"1{Environment.NewLine}2", tv.SelectedText);
    }

    [Fact]
    public void HistoryText_ClearHistoryChanges ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal (
                      $"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        tv.ClearHistoryChanges ();

        Assert.Equal (
                      $"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Exceptions ()
    {
        var ht = new HistoryText ();

        foreach (object ls in Enum.GetValues (typeof (TextEditingLineStatus)))
        {
            if ((TextEditingLineStatus)ls != TextEditingLineStatus.Original)
            {
                Assert.Throws<ArgumentException> (
                                                  () => ht.Add (
                                                                new List<List<Cell>> (),
                                                                Point.Empty,
                                                                (TextEditingLineStatus)ls
                                                               )
                                                 );
            }
        }

        Assert.Null (Record.Exception (() => ht.Add (new () { new () }, Point.Empty)));
    }

    [Fact]
    public void HistoryText_IsDirty_HasHistoryChanges ()
    {
        var tv = new TextView ();

        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"1{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ($"1{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        // IsDirty cannot be based on HasHistoryChanges because HasHistoryChanges is greater than 0
        // The only way is comparing from the original text
        Assert.False (tv.IsDirty);

        // Still true because HasHistoryChanges is greater than 0
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Changing_On_Middle_Clear_History_Forwards ()
    {
        var tv = new TextView ();

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ("12", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.D3));
        Assert.Equal ("123", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("12", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.D4));
        Assert.Equal ("124", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.CursorPosition);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("124", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Disabled_On_WordWrap ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
        var tv = new TextView { Width = 80, Height = 5, Text = text };

        Assert.False (tv.WordWrap);
        tv.WordWrap = true;

        tv.SelectionStartColumn = 12;
        tv.CursorPosition = new (12, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Ending_With_Newline_Multi_Line_Selected_Almost_All_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.CursorPosition = new (12, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (12, 2), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (12, 2), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_First_Line_Selected_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.CursorPosition = new (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.A));

        Assert.Equal (
                      $"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 0), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 0), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_KillWordBackward ()
    {
        var text = "First line.\nSecond line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (12, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (11, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (7, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (11, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (10, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (6, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (6, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (10, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (11, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (7, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (11, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (12, 1), tv.CursorPosition);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (11, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (7, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (11, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (10, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (6, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_KillWordForward ()
    {
        var text = "First line.\nSecond line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_All_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 2), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 2), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 2), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharLeft_All ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.SelectedText
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 2), tv.CursorPosition);
        Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 2), tv.CursorPosition);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharRight_All ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.SelectedText
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 2), tv.CursorPosition);
        Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 2), tv.CursorPosition);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multiline_Selected_Tab_BackTab ()
    {
        var text = "First line.\nSecond line.\nThird line.";
        var tv = new TextView { Width = 80, Height = 5, Text = text };

        tv.SelectionStartColumn = 6;
        tv.CursorPosition = new (6, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Tab));
        Assert.Equal ("First \tline.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (7, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (6, 0), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First \tline.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (7, 0), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (6, 2), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("First \tline.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (7, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (6, 0), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multiline_Simples_Tab_BackTab ()
    {
        var text = "First line.\nSecond line.\nThird line.";
        var tv = new TextView { Width = 80, Height = 5, Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.Tab));

        Assert.Equal (
                      $"\tFirst line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"\tFirst line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"\tFirst line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 0), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.CursorPosition = new (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 0), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 0), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.SelectionStartRow = 1;
        tv.CursorPosition = new (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.SelectionStartRow = 1;
        tv.CursorPosition = new (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.A));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (1, 2), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (1, 2), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 2), tv.CursorPosition);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (1, 2), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Three_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.CursorPosition = new (17, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 2), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 2), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Two_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.CursorPosition = new (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.CursorPosition);
    }

    [Fact]
    public void HistoryText_Undo_Redo_ApplyCellsAttribute ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.CursorPosition = new (18, 1);

        if (Environment.NewLine.Length == 2)
        {
            Assert.Equal (31, tv.SelectedLength);
        }
        else
        {
            Assert.Equal (30, tv.SelectedLength);
        }

        Assert.Equal ($"first line.{Environment.NewLine}This is the second", tv.SelectedText);
        Assert.Equal ($"first line.{Environment.NewLine}This is the second", Cell.ToString (tv.SelectedCellsList));
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        AssertNullAttribute ();

        tv.ApplyCellsAttribute (new (Color.Red, Color.Green));

        AssertRedGreenAttribute ();

        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ($"first line.{Environment.NewLine}This is the second", Cell.ToString (tv.SelectedCellsList));
        Assert.Equal (new (18, 1), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        AssertNullAttribute ();

        Assert.Equal (12, tv.SelectionStartColumn);
        Assert.Equal (0, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Empty (tv.SelectedCellsList);
        Assert.Equal (new (12, 0), tv.CursorPosition);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        AssertRedGreenAttribute ();

        Assert.Equal (12, tv.SelectionStartColumn);
        Assert.Equal (0, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Empty (tv.SelectedCellsList);
        Assert.Equal (new (12, 0), tv.CursorPosition);
        Assert.True (tv.IsDirty);

        void AssertNullAttribute ()
        {
            tv.GetRegion (out List<List<Cell>> region, 0, 12, 1, 18);

            foreach (List<Cell> cells in region)
            {
                foreach (Cell cell in cells)
                {
                    Assert.Null (cell.Attribute);
                }
            }
        }

        void AssertRedGreenAttribute ()
        {
            tv.GetRegion (out List<List<Cell>> region, 0, 12, 1, 18);

            foreach (List<Cell> cells in region)
            {
                foreach (Cell cell in cells)
                {
                    Assert.Equal ("[Red,Green,None]", cell.Attribute.ToString ());
                }
            }
        }
    }

    [Fact]
    public void Internal_Tests ()
    {
        var txt = "This is a text.";
        List<Cell> txtRunes = Cell.StringToCells (txt);
        Assert.Equal (txt.Length, txtRunes.Count);
        Assert.Equal ('T', txtRunes [0].Rune.Value);
        Assert.Equal ('h', txtRunes [1].Rune.Value);
        Assert.Equal ('i', txtRunes [2].Rune.Value);
        Assert.Equal ('s', txtRunes [3].Rune.Value);
        Assert.Equal (' ', txtRunes [4].Rune.Value);
        Assert.Equal ('i', txtRunes [5].Rune.Value);
        Assert.Equal ('s', txtRunes [6].Rune.Value);
        Assert.Equal (' ', txtRunes [7].Rune.Value);
        Assert.Equal ('a', txtRunes [8].Rune.Value);
        Assert.Equal (' ', txtRunes [9].Rune.Value);
        Assert.Equal ('t', txtRunes [10].Rune.Value);
        Assert.Equal ('e', txtRunes [11].Rune.Value);
        Assert.Equal ('x', txtRunes [12].Rune.Value);
        Assert.Equal ('t', txtRunes [13].Rune.Value);
        Assert.Equal ('.', txtRunes [^1].Rune.Value);

        var col = 0;
        Assert.True (TextModel.SetCol (ref col, 80, 79));
        Assert.False (TextModel.SetCol (ref col, 80, 80));
        Assert.Equal (79, col);

        var start = 0;
        var x = 8;
        Assert.Equal (8, TextModel.GetColFromX (txtRunes, start, x));
        Assert.Equal ('a', txtRunes [start + x].Rune.Value);
        start = 1;
        x = 7;
        Assert.Equal (7, TextModel.GetColFromX (txtRunes, start, x));
        Assert.Equal ('a', txtRunes [start + x].Rune.Value);

        Assert.Equal ((15, 15), TextModel.DisplaySize (txtRunes));
        Assert.Equal ((6, 6), TextModel.DisplaySize (txtRunes, 1, 7));

        Assert.Equal (0, TextModel.CalculateLeftColumn (txtRunes, 0, 7, 8));
        Assert.Equal (1, TextModel.CalculateLeftColumn (txtRunes, 0, 8, 8));
        Assert.Equal (2, TextModel.CalculateLeftColumn (txtRunes, 0, 9, 8));

        var tm = new TextModel ();
        tm.AddLine (0, Cell.StringToCells ("This is first line."));
        tm.AddLine (1, Cell.StringToCells ("This is last line."));
        Assert.Equal ((new Point (2, 0), true), tm.FindNextText ("is", out bool gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 0), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 1), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 1), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 0), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.True (gaveFullTurn);
        tm.ResetContinuousFind (Point.Empty);
        Assert.Equal ((new Point (5, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 0), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 0), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.True (gaveFullTurn);

        Assert.Equal ((new Point (9, 1), true), tm.ReplaceAllText ("is", false, false, "really"));
        Assert.Equal (Cell.StringToCells ("Threally really first line."), tm.GetLine (0));
        Assert.Equal (Cell.StringToCells ("Threally really last line."), tm.GetLine (1));
        tm = new ();
        tm.AddLine (0, Cell.StringToCells ("This is first line."));
        tm.AddLine (1, Cell.StringToCells ("This is last line."));
        Assert.Equal ((new Point (5, 1), true), tm.ReplaceAllText ("is", false, true, "really"));
        Assert.Equal (Cell.StringToCells ("This really first line."), tm.GetLine (0));
        Assert.Equal (Cell.StringToCells ("This really last line."), tm.GetLine (1));
    }

    [Fact]
    public void LeftColumn_Add_One_If_Text_Length_Is_Equal_To_Width ()
    {
        var tv = new TextView { Width = 10, Text = "1234567890" };

        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.Equal (0, tv.LeftColumn);

        tv.CursorPosition = new (9, 0);
        Assert.Equal (new (9, 0), tv.CursorPosition);
        Assert.Equal (0, tv.LeftColumn);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        tv.CursorPosition = new (10, 0);
        Assert.Equal (new (10, 0), tv.CursorPosition);
        Assert.Equal (1, tv.LeftColumn);
    }

    [Fact]
    public void LoadFile_Throws_If_File_Is_Empty ()
    {
        var result = false;
        var tv = new TextView ();
        Assert.Throws<ArgumentException> (() => result = tv.Load (""));
        Assert.False (result);
    }

    [Fact]
    public void LoadFile_Throws_If_File_Is_Null ()
    {
        var result = false;
        var tv = new TextView ();
        Assert.Throws<ArgumentNullException> (() => result = tv.Load ((string)null));
        Assert.False (result);
    }

    [Fact]
    public void LoadFile_Throws_If_File_Not_Exist ()
    {
        var result = false;
        var tv = new TextView ();
        Assert.Throws<FileNotFoundException> (() => result = tv.Load ("blabla"));
        Assert.False (result);
    }

    [Fact]
    public void LoadStream_CRLF ()
    {
        var text = "This is the first line.\r\nThis is the second line.\r\n";
        var tv = new TextView ();
        tv.Load (new MemoryStream (Encoding.ASCII.GetBytes (text)));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );
    }

    [Fact]
    public void LoadStream_IsDirty ()
    {
        var text = "Testing";

        using (var stream = new MemoryStream ())
        {
            var writer = new StreamWriter (stream);
            writer.Write (text);
            writer.Flush ();
            stream.Position = 0;

            var tv = new TextView ();
            tv.Load (stream);

            Assert.Equal (7, text.Length);
            Assert.Equal (text.Length, tv.Text.Length);
            Assert.Equal (text, tv.Text);
            Assert.False (tv.IsDirty);
        }
    }

    [Fact]
    public void LoadStream_IsDirty_With_Null_On_The_Text ()
    {
        var text = "Test\0ing";

        using (var stream = new MemoryStream ())
        {
            var writer = new StreamWriter (stream);
            writer.Write (text);
            writer.Flush ();
            stream.Position = 0;

            var tv = new TextView ();
            tv.Load (stream);

            Assert.Equal (8, text.Length);
            Assert.Equal (text.Length, tv.Text.Length);
            Assert.Equal (8, text.Length);
            Assert.Equal (8, tv.Text.Length);
            Assert.Equal (text, tv.Text);
            Assert.False (tv.IsDirty);
            Assert.Equal ((Rune)'\u2400', ((Rune)tv.Text [4]).MakePrintable ());
        }
    }

    [Fact]
    public void LoadStream_LF ()
    {
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView ();
        tv.Load (new MemoryStream (Encoding.ASCII.GetBytes (text)));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );
    }

    [Fact]
    public void LoadStream_Stream_Is_Empty ()
    {
        var tv = new TextView ();
        tv.Load (new MemoryStream ());
        Assert.Equal ("", tv.Text);
    }

    [Fact]
    public void LoadStream_Throws_If_Stream_Is_Null ()
    {
        var tv = new TextView ();
        Assert.Throws<ArgumentNullException> (() => tv.Load ((Stream)null));
    }

    [Fact]
    public void ReplaceAllText_Does_Not_Throw_Exception ()
    {
        var textToFind = "hello! hello!";
        var textToReplace = "hello!";
        var tv = new TextView { Width = 20, Height = 3, Text = textToFind };

        Exception exception = Record.Exception (() => tv.ReplaceAllText (textToFind, false, false, textToReplace));
        Assert.Null (exception);
        Assert.Equal (textToReplace, tv.Text);
    }

    [Fact]
    public void StringToRunes_Slipts_CRLF ()
    {
        var text = "This is the first line.\r\nThis is the second line.\r\n";
        var tv = new TextView ();
        tv.Text = text;

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );
    }

    [Fact]
    public void StringToRunes_Slipts_LF ()
    {
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView ();
        tv.Text = text;

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );
    }

    [Fact]
    public void TextView_MultiLine_But_Without_Tabs ()
    {
        var view = new TextView ();

        // the default for TextView
        Assert.True (view.Multiline);

        view.AllowsTab = false;
        Assert.False (view.AllowsTab);

        Assert.True (view.Multiline);
    }

    [Fact]
    public void WordBackward_WordForward_Limits_Return_Null ()
    {
        var model = new TextModel ();
        model.LoadString ("Test");
        (int col, int row)? newPos = model.WordBackward (0, 0, false);
        Assert.Null (newPos);
        newPos = model.WordForward (4, 0, false);
        Assert.Null (newPos);
    }

    [Fact]
    public void WordWrap_Gets_Sets ()
    {
        var tv = new TextView { WordWrap = true };
        Assert.True (tv.WordWrap);
        tv.WordWrap = false;
        Assert.False (tv.WordWrap);
    }

    [Fact]
    public void WordWrap_True_Text_Always_Returns_Unwrapped ()
    {
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView { Width = 10 };
        tv.Text = text;

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );
        tv.WordWrap = true;

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}",
                      tv.Text
                     );
    }

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new TextView ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var view = new TextView ();
        var accepted = false;
        view.Accepting += OnAccept;
        view.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 1)]
    public void Accepted_Command_Raises_Accepted_Regardles_Of_AllowsReturn (bool allowsReturn, int expectedAcceptEvents)
    {
        var view = new TextView
        {
            AllowsReturn = allowsReturn
        };

        var acceptedEvents = 0;
        view.Accepting += Accept;
        view.InvokeCommand (Command.Accept);
        Assert.Equal (expectedAcceptEvents, acceptedEvents);

        return;

        void Accept (object sender, CommandEventArgs e) { acceptedEvents++; }
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 0)]
    public void Enter_Key_Fires_Accepted_BasedOn_AllowsReturn (bool allowsReturn, int expectedAccepts)
    {
        var view = new TextView
        {
            Multiline = allowsReturn
        };

        var accepted = 0;
        view.Accepting += Accept;
        view.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, accepted);

        return;

        void Accept (object sender, CommandEventArgs e) { accepted++; }
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 0)]
    public void Enter_Key_Fires_Accepted_BasedOn_Multiline (bool multiline, int expectedAccepts)
    {
        var view = new TextView
        {
            Multiline = multiline
        };

        var accepted = 0;
        view.Accepting += Accept;
        view.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, accepted);

        return;

        void Accept (object sender, CommandEventArgs e) { accepted++; }
    }

    [Fact]
    public void Space_Key_Types_Space ()
    {
        var view = new TextView ();

        view.NewKeyDownEvent (Key.Space);

        Assert.Equal (" ", view.Text);
    }

    [Theory]
    [InlineData (false, false, 1, 1)]
    [InlineData (false, true, 1, 0)]
    [InlineData (true, false, 0, 0)]
    [InlineData (true, true, 0, 0)]
    public void Accepted_Event_Handled_Prevents_Default_Button_Accept (bool multiline, bool handleAccept, int expectedAccepts, int expectedButtonAccepts)
    {
        var superView = new Window ();

        var tv = new TextView
        {
            Multiline = multiline
        };

        var button = new Button
        {
            IsDefault = true
        };

        superView.Add (tv, button);

        var buttonAccept = 0;
        button.Accepting += ButtonAccept;

        var textViewAccept = 0;
        tv.Accepting += TextViewAccept;

        tv.SetFocus ();
        Assert.True (tv.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, textViewAccept);
        Assert.Equal (expectedButtonAccepts, buttonAccept);

        button.SetFocus ();
        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, textViewAccept);
        Assert.Equal (expectedButtonAccepts + 1, buttonAccept);

        return;

        void TextViewAccept (object sender, CommandEventArgs e)
        {
            textViewAccept++;
            e.Handled = handleAccept;
        }

        void ButtonAccept (object sender, CommandEventArgs e) { buttonAccept++; }
    }

    [Theory]
    [InlineData (true, 0)]
    [InlineData (false, 1)]
    public void Accepted_No_Handler_Enables_Default_Button_Accept (bool multiline, int expectedButtonAccept)
    {
        var superView = new Window ();

        var tv = new TextView
        {
            Multiline = multiline
        };

        var button = new Button
        {
            IsDefault = true
        };

        superView.Add (tv, button);

        var buttonAccept = 0;
        button.Accepting += ButtonAccept;

        tv.SetFocus ();
        Assert.True (tv.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedButtonAccept, buttonAccept);

        button.SetFocus ();
        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedButtonAccept + 1, buttonAccept);

        return;

        void ButtonAccept (object sender, CommandEventArgs e) { buttonAccept++; }
    }

    [Fact]
    public void Autocomplete_Popup_Added_To_SuperView_On_Init ()
    {
        View superView = new ()
        {
            CanFocus = true
        };

        TextView t = new ();

        superView.Add (t);
        Assert.Single (superView.SubViews);

        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Autocomplete__Added_To_SuperView_On_Add ()
    {
        View superView = new ()
        {
            CanFocus = true,
            Id = "superView"
        };

        superView.BeginInit ();
        superView.EndInit ();
        Assert.Empty (superView.SubViews);

        TextView t = new ()
        {
            Id = "t"
        };

        superView.Add (t);

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Autocomplete_Visible_False_By_Default ()
    {
        View superView = new ()
        {
            CanFocus = true
        };

        TextView t = new ();

        superView.Add (t);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);

        Assert.True (t.Visible);
        Assert.False (t.Autocomplete.Visible);
    }

    [Fact]
    public void Right_CursorAtEnd_WithSelection_ShouldClearSelection ()
    {
        var tv = new TextView
        {
            Text = "Hello"
        };
        tv.SetFocus ();

        tv.NewKeyDownEvent (Key.End.WithShift);
        Assert.Equal (5, tv.CursorPosition.X);

        // When there is selected text and the cursor is at the end of the text field
        Assert.Equal ("Hello", tv.SelectedText);

        // Pressing right should not move focus, instead it should clear selection
        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Empty (tv.SelectedText);

        // Now that the selection is cleared another right keypress should move focus
        Assert.False (tv.NewKeyDownEvent (Key.CursorRight));
    }

    [Fact]
    public void Left_CursorAtStart_WithSelection_ShouldClearSelection ()
    {
        var tv = new TextView
        {
            Text = "Hello"
        };
        tv.SetFocus ();

        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorRight);

        Assert.Equal (2, tv.CursorPosition.X);

        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithShift));

        // When there is selected text and the cursor is at the start of the text field
        Assert.Equal ("He", tv.SelectedText);

        // Pressing left should not move focus, instead it should clear selection
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Empty (tv.SelectedText);

        // When clearing selected text with left the cursor should be at the start of the selection
        Assert.Equal (0, tv.CursorPosition.X);

        // Now that the selection is cleared another left keypress should move focus
        Assert.False (tv.NewKeyDownEvent (Key.CursorLeft));
    }

    [Fact]
    public void Equals_True ()
    {
        var c1 = new Cell ();
        var c2 = new Cell ();
        Assert.True (c1.Equals (c2));
        Assert.True (c2.Equals (c1));

        c1.Rune = new ('a');
        c1.Attribute = new ();
        c2.Rune = new ('a');
        c2.Attribute = new ();
        Assert.True (c1.Equals (c2));
        Assert.True (c2.Equals (c1));
    }

    [Fact]
    public void Cell_LoadCells_Without_Scheme_Is_Never_Null ()
    {
        List<Cell> cells = new ()
        {
            new () { Rune = new ('T') },
            new () { Rune = new ('e') },
            new () { Rune = new ('s') },
            new () { Rune = new ('t') }
        };
        TextView tv = CreateTextView ();
        var top = new Toplevel ();
        top.Add (tv);
        tv.Load (cells);

        for (var i = 0; i < tv.Lines; i++)
        {
            List<Cell> line = tv.GetLine (i);

            foreach (Cell c in line)
            {
                Assert.NotNull (c.Attribute);
            }
        }
    }

    private TextView CreateTextView () { return new () { Width = 30, Height = 10 }; }
}
