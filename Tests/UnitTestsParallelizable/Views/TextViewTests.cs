#nullable disable
using System.Text;

namespace ViewsTests.TextViewTests;

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

        Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
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

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);

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

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
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

        Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        tv.ClearHistoryChanges ();

        Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
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
                Assert.Throws<ArgumentException> (() => ht.Add (new List<List<Cell>> (), Point.Empty, (TextEditingLineStatus)ls));
            }
        }

        Assert.Null (Record.Exception (() => ht.Add (new List<List<Cell>> { new () }, Point.Empty)));
    }

    [Fact]
    public void HistoryText_IsDirty_HasHistoryChanges ()
    {
        var tv = new TextView ();

        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"1{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ($"1{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

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
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ("12", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.D3));
        Assert.Equal ("123", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("12", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.D4));
        Assert.Equal ("124", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("124", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Disabled_On_WordWrap ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
        var tv = new TextView { Width = 80, Height = 5, Text = text };

        Assert.False (tv.WordWrap);
        tv.WordWrap = true;

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (12, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Ending_With_Newline_Multi_Line_Selected_Almost_All_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (12, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (12, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (12, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_First_Line_Selected_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));

        Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
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
        Assert.Equal (new Point (12, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (11, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (7, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (7, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (11, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (12, 1), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (11, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (7, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
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
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_All_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharLeft_All ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharRight_All ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multiline_Selected_Tab ()
    {
        var text = $"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.";
        var tv = new TextView { Width = 80, Height = 5, Text = text };

        tv.SelectionStartColumn = 6;
        tv.InsertionPoint = new Point (6, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Tab));
        Assert.Equal ("First \tline.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (7, 0), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (text, tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (6, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First \tline.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (7, 0), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.SelectionStartRow = 1;
        tv.InsertionPoint = new Point (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.SelectionStartRow = 1;
        tv.InsertionPoint = new Point (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 2), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Three_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (17, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Two_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_ApplyCellsAttribute ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (18, 1);

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
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        AssertNullAttribute ();

        tv.ApplyCellsAttribute (new Attribute (Color.Red, Color.Green));

        AssertRedGreenAttribute ();

        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ($"first line.{Environment.NewLine}This is the second", Cell.ToString (tv.SelectedCellsList));
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        AssertNullAttribute ();

        Assert.Equal (12, tv.SelectionStartColumn);
        Assert.Equal (0, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Empty (tv.SelectedCellsList);
        Assert.Equal (new Point (12, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        AssertRedGreenAttribute ();

        Assert.Equal (12, tv.SelectionStartColumn);
        Assert.Equal (0, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Empty (tv.SelectedCellsList);
        Assert.Equal (new Point (12, 0), tv.InsertionPoint);
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
        List<Cell> txtStrings = Cell.StringToCells (txt);
        Assert.Equal (txt.Length, txtStrings.Count);
        Assert.Equal ("T", txtStrings [0].Grapheme);
        Assert.Equal ("h", txtStrings [1].Grapheme);
        Assert.Equal ("i", txtStrings [2].Grapheme);
        Assert.Equal ("s", txtStrings [3].Grapheme);
        Assert.Equal (" ", txtStrings [4].Grapheme);
        Assert.Equal ("i", txtStrings [5].Grapheme);
        Assert.Equal ("s", txtStrings [6].Grapheme);
        Assert.Equal (" ", txtStrings [7].Grapheme);
        Assert.Equal ("a", txtStrings [8].Grapheme);
        Assert.Equal (" ", txtStrings [9].Grapheme);
        Assert.Equal ("t", txtStrings [10].Grapheme);
        Assert.Equal ("e", txtStrings [11].Grapheme);
        Assert.Equal ("x", txtStrings [12].Grapheme);
        Assert.Equal ("t", txtStrings [13].Grapheme);
        Assert.Equal (".", txtStrings [^1].Grapheme);

        var col = 0;
        Assert.True (TextModel.SetCol (ref col, 80, 79));
        Assert.False (TextModel.SetCol (ref col, 80, 80));
        Assert.Equal (79, col);

        var start = 0;
        var x = 8;
        Assert.Equal (8, TextModel.GetColFromX (txtStrings, start, x));
        Assert.Equal ("a", txtStrings [start + x].Grapheme);
        start = 1;
        x = 7;
        Assert.Equal (7, TextModel.GetColFromX (txtStrings, start, x));
        Assert.Equal ("a", txtStrings [start + x].Grapheme);

        Assert.Equal ((15, 15), TextModel.DisplaySize (txtStrings));
        Assert.Equal ((6, 6), TextModel.DisplaySize (txtStrings, 1, 7));

        Assert.Equal (0, TextModel.CalculateLeftColumn (txtStrings, 0, 7, 8));
        Assert.Equal (1, TextModel.CalculateLeftColumn (txtStrings, 0, 8, 8));
        Assert.Equal (2, TextModel.CalculateLeftColumn (txtStrings, 0, 9, 8));

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
        tm = new TextModel ();
        tm.AddLine (0, Cell.StringToCells ("This is first line."));
        tm.AddLine (1, Cell.StringToCells ("This is last line."));
        Assert.Equal ((new Point (5, 1), true), tm.ReplaceAllText ("is", false, true, "really"));
        Assert.Equal (Cell.StringToCells ("This really first line."), tm.GetLine (0));
        Assert.Equal (Cell.StringToCells ("This really last line."), tm.GetLine (1));
    }

    [Fact]
    public void Viewport_X_Add_One_If_Text_Length_Is_Equal_To_Width ()
    {
        var tv = new TextView { Width = 10, Text = "1234567890" };

        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.Viewport.X);

        tv.InsertionPoint = new Point (9, 0);
        Assert.Equal (new Point (9, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.Viewport.X);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        tv.InsertionPoint = new Point (10, 0);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);
        Assert.Equal (1, tv.Viewport.X);
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

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
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

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
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

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void StringToRunes_Slipts_LF ()
    {
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView ();
        tv.Text = text;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void MultiLine_But_Without_Tabs ()
    {
        var view = new TextView ();

        // the default for TextView
        Assert.True (view.Multiline);

        view.TabKeyAddsTab = false;
        Assert.False (view.TabKeyAddsTab);

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

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
        tv.WordWrap = true;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
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

        void OnAccept (object sender, CommandEventArgs e) => accepted = true;
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 1)]
    public void Accepted_Command_Raises_Accepted_Regardless_Of_EnterKeyAddsLine (bool allowsReturn, int expectedAcceptEvents)
    {
        var view = new TextView { EnterKeyAddsLine = allowsReturn };

        var acceptedEvents = 0;
        view.Accepting += Accept;
        view.InvokeCommand (Command.Accept);
        Assert.Equal (expectedAcceptEvents, acceptedEvents);

        return;

        void Accept (object sender, CommandEventArgs e) => acceptedEvents++;
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 0)]
    public void Enter_Key_Fires_Accepted_BasedOn_AllowsReturn (bool allowsReturn, int expectedAccepts)
    {
        var view = new TextView { Multiline = allowsReturn };

        var accepted = 0;
        view.Accepting += Accept;
        view.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, accepted);

        return;

        void Accept (object sender, CommandEventArgs e) => accepted++;
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 0)]
    public void Enter_Key_Fires_Accepted_BasedOn_Multiline (bool multiline, int expectedAccepts)
    {
        var view = new TextView { Multiline = multiline };

        var accepted = 0;
        view.Accepting += Accept;
        view.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, accepted);

        return;

        void Accept (object sender, CommandEventArgs e) => accepted++;
    }

    [Fact]
    public void Space_Key_Types_Space ()
    {
        TextView view = new ();

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

        var tv = new TextView { Multiline = multiline };

        var button = new Button { IsDefault = true };

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

        void ButtonAccept (object sender, CommandEventArgs e) => buttonAccept++;
    }

    [Theory]
    [InlineData (true, 0)]
    [InlineData (false, 1)]
    public void Accepted_No_Handler_Enables_Default_Button_Accept (bool multiline, int expectedButtonAccept)
    {
        var superView = new Window ();

        var tv = new TextView { Multiline = multiline };

        var button = new Button { IsDefault = true };

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

        void ButtonAccept (object sender, CommandEventArgs e) => buttonAccept++;
    }

    [Fact]
    public void Autocomplete_Popup_Added_To_SuperView_On_Init ()
    {
        View superView = new () { CanFocus = true };

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
        View superView = new () { CanFocus = true, Id = "superView" };

        superView.BeginInit ();
        superView.EndInit ();
        Assert.Empty (superView.SubViews);

        TextView t = new () { Id = "t" };

        superView.Add (t);

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Autocomplete_Visible_False_By_Default ()
    {
        View superView = new () { CanFocus = true };

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
        var tv = new TextView { Text = "Hello" };
        tv.SetFocus ();

        tv.NewKeyDownEvent (Key.End.WithShift);
        Assert.Equal (5, tv.InsertionPoint.X);

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
        var tv = new TextView { Text = "Hello" };
        tv.SetFocus ();

        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorRight);

        Assert.Equal (2, tv.InsertionPoint.X);

        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithShift));

        // When there is selected text and the cursor is at the start of the text field
        Assert.Equal ("He", tv.SelectedText);

        // Pressing left should not move focus, instead it should clear selection
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Empty (tv.SelectedText);

        // When clearing selected text with left the cursor should be at the start of the selection
        Assert.Equal (0, tv.InsertionPoint.X);

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

        c1.Grapheme = new string ("a");
        c1.Attribute = new Attribute ();
        c2.Grapheme = new string ("a");
        c2.Attribute = new Attribute ();
        Assert.True (c1.Equals (c2));
        Assert.True (c2.Equals (c1));
    }

    [Fact]
    public void Cell_LoadCells_Without_Scheme_Is_Never_Null ()
    {
        List<Cell> cells = new ()
        {
            new Cell { Grapheme = new string ("T") },
            new Cell { Grapheme = new string ("e") },
            new Cell { Grapheme = new string ("s") },
            new Cell { Grapheme = new string ("t") }
        };
        TextView tv = CreateTextView ();
        var top = new Runnable ();
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

    [Theory]
    [InlineData ("", false, "")]
    [InlineData ("", true, "")]
    [InlineData (" ", false, "")]
    [InlineData (" ", true, "")]
    [InlineData ("  ", false, "")]
    [InlineData ("  ", true, "")]
    [InlineData ("a", false, "")]
    [InlineData ("a", true, "")]
    [InlineData ("a ", false, "")]
    [InlineData ("a ", true, "")]
    [InlineData (" a ", false, "a ", "")]
    [InlineData (" a ", true, "a ", "")]
    [InlineData ("  H1  ", false, "H1  ", "")]
    [InlineData ("  H1  ", true, "H1  ", "")]
    [InlineData ("a$", false, "$", "")]
    [InlineData ("a$", true, "$", "")]
    [InlineData ("a$#", false, "$#", "")]
    [InlineData ("a$#", true, "$#", "#", "")]
    [InlineData ("  a$#  ", false, "a$#  ", "$#  ", "")]
    [InlineData ("  a$#  ", true, "a$#  ", "$#  ", "#  ", "")]
    [InlineData ("\"$schema\"", false, "schema\"", "\"", "")]
    [InlineData ("\"$schema\"", true, "$schema\"", "schema\"", "\"", "")]
    [InlineData ("\": \"", false, "\"", "")]
    [InlineData ("\": \"", true, "\"", "")]
    [InlineData ("\"$schema\": \"", false, "schema\": \"", "\": \"", "\"", "")]
    [InlineData ("\"$schema\": \"", true, "$schema\": \"", "schema\": \"", "\": \"", "\"", "")]
    [InlineData ("1ºªA", false, "")]
    [InlineData ("1ºªA", true, "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", false, "\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", true, "\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", "|$=+´`~^<>£€¨", "")]
    [InlineData ("{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 false,
                 "\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/schemas/tui-config-schema.json\"\r\n}",
                 "schemas/tui-config-schema.json\"\r\n}",
                 "/tui-config-schema.json\"\r\n}",
                 "tui-config-schema.json\"\r\n}",
                 "-config-schema.json\"\r\n}",
                 "config-schema.json\"\r\n}",
                 "-schema.json\"\r\n}",
                 "schema.json\"\r\n}",
                 ".json\"\r\n}",
                 "json\"\r\n}",
                 "\"\r\n}",
                 "\r\n}",
                 "}",
                 "")]
    [InlineData ("{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 true,
                 "\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/schemas/tui-config-schema.json\"\r\n}",
                 "schemas/tui-config-schema.json\"\r\n}",
                 "/tui-config-schema.json\"\r\n}",
                 "tui-config-schema.json\"\r\n}",
                 "-config-schema.json\"\r\n}",
                 "config-schema.json\"\r\n}",
                 "-schema.json\"\r\n}",
                 "schema.json\"\r\n}",
                 ".json\"\r\n}",
                 "json\"\r\n}",
                 "\"\r\n}",
                 "\r\n}",
                 "}",
                 "")]
    public void WordForward_WordWrap_False_True (string text, bool useSameRuneType, params string [] expectedText)
    {
        TextView tv = CreateTextView ();
        tv.UseSameRuneTypeForWords = useSameRuneType;

        ProcessDeleteWithCtrl ();

        tv.WordWrap = true;
        ProcessDeleteWithCtrl ();

        void ProcessDeleteWithCtrl ()
        {
            tv.Text = text;
            var idx = 0;

            while (!string.IsNullOrEmpty (tv.Text))
            {
                tv.NewKeyDownEvent (Key.Delete.WithCtrl);
                Assert.Equal (expectedText [idx].Replace ("\r\n", Environment.NewLine), tv.Text);
                idx++;
            }
        }
    }

    [Theory]
    [InlineData ("", false, "")]
    [InlineData ("", true, "")]
    [InlineData (" ", false, "")]
    [InlineData (" ", true, "")]
    [InlineData ("  ", false, "")]
    [InlineData ("  ", true, "")]
    [InlineData ("a", false, "")]
    [InlineData ("a", true, "")]
    [InlineData ("a ", false, "")]
    [InlineData ("a ", true, "")]
    [InlineData (" a ", false, " ", "")]
    [InlineData (" a ", true, " ", "")]
    [InlineData ("  H1  ", false, "  ", "")]
    [InlineData ("  H1  ", true, "  ", "")]
    [InlineData ("a$", false, "a", "")]
    [InlineData ("a$", true, "a", "")]
    [InlineData ("a$#", false, "a", "")]
    [InlineData ("a$#", true, "a$", "a", "")]
    [InlineData ("  a$#  ", false, "  a", "  ", "")]
    [InlineData ("  a$#  ", true, "  a$", "  a", "  ", "")]
    [InlineData ("\"$schema\"", false, "\"$schema", "\"$", "")]
    [InlineData ("\"$schema\"", true, "\"$schema", "\"$", "\"", "")]
    [InlineData ("\"$schema\": \"", false, "\"$schema\": ", "\"$schema", "\"$", "")]
    [InlineData ("\"$schema\": \"", true, "\"$schema\": ", "\"$schema", "\"$", "\"", "")]
    [InlineData ("1ºªA", false, "")]
    [InlineData ("1ºªA", true, "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", false, "ºª", "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", true, "ºª\\!\"#%&/()?'«»*;,:._-@{[]}]", "ºª", "")]
    [InlineData ("{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 false,
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.",
                 "{\r\n  \"$schema\": \"https://gui-cs.github",
                 "{\r\n  \"$schema\": \"https://gui-cs.",
                 "{\r\n  \"$schema\": \"https://gui-cs",
                 "{\r\n  \"$schema\": \"https://gui-",
                 "{\r\n  \"$schema\": \"https://gui",
                 "{\r\n  \"$schema\": \"https://",
                 "{\r\n  \"$schema\": \"https",
                 "{\r\n  \"$schema\": \"",
                 "{\r\n  \"$schema\": ",
                 "{\r\n  \"$schema",
                 "{\r\n  \"$",
                 "{\r\n  ",
                 "{\r\n",
                 "{",
                 "")]
    [InlineData ("{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 true,
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-config",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui-",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/tui",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas/",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/schemas",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui/",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.Gui",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal.",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/Terminal",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io/",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.io",
                 "{\r\n  \"$schema\": \"https://gui-cs.github.",
                 "{\r\n  \"$schema\": \"https://gui-cs.github",
                 "{\r\n  \"$schema\": \"https://gui-cs.",
                 "{\r\n  \"$schema\": \"https://gui-cs",
                 "{\r\n  \"$schema\": \"https://gui-",
                 "{\r\n  \"$schema\": \"https://gui",
                 "{\r\n  \"$schema\": \"https://",
                 "{\r\n  \"$schema\": \"https",
                 "{\r\n  \"$schema\": \"",
                 "{\r\n  \"$schema\": ",
                 "{\r\n  \"$schema",
                 "{\r\n  \"$",
                 "{\r\n  \"",
                 "{\r\n  ",
                 "{\r\n",
                 "{",
                 "")]
    public void WordBackward_WordWrap_False_True (string text, bool useSameRuneType, params string [] expectedText)
    {
        TextView tv = CreateTextView ();
        tv.UseSameRuneTypeForWords = useSameRuneType;

        ProcessBackspaceWithCtrl ();

        tv.WordWrap = true;
        ProcessBackspaceWithCtrl ();

        void ProcessBackspaceWithCtrl ()
        {
            tv.Text = text;
            tv.MoveEnd ();
            var idx = 0;

            while (!string.IsNullOrEmpty (tv.Text))
            {
                tv.NewKeyDownEvent (Key.Backspace.WithCtrl);
                Assert.Equal (expectedText [idx].Replace ("\r\n", Environment.NewLine), tv.Text);
                idx++;
            }
        }
    }

    [Theory]
    [InlineData ("", 0, false, "")]
    [InlineData ("", 0, true, "")]
    [InlineData ("a", 0, false, "a")]
    [InlineData ("a", 0, true, "a")]
    [InlineData ("a:", 0, false, "a")]
    [InlineData ("a:", 0, true, "a")]
    [InlineData ("a:", 1, false, ":")]
    [InlineData ("a:", 1, true, ":")]
    [InlineData ("a ", 0, false, "a ")]
    [InlineData ("a ", 0, true, "a")]
    [InlineData ("a ", 1, false, "a ")]
    [InlineData ("a ", 1, true, "a")]
    [InlineData ("a b", 0, false, "a ")]
    [InlineData ("a b", 0, true, "a")]
    [InlineData ("a b", 1, false, "a ")]
    [InlineData ("a b", 1, true, "a")]
    [InlineData ("a b ", 2, false, "b ")]
    [InlineData ("a b ", 2, true, "b")]
    [InlineData ("a b ", 3, false, "b ")]
    [InlineData ("a b ", 3, true, "b")]
    [InlineData (" a b ", 0, false, " ")]
    [InlineData (" a b ", 0, true, " ")]
    [InlineData (" a  b ", 2, false, "  ")]
    [InlineData (" a  b ", 2, true, "  ")]
    [InlineData (" a  b ", 3, false, "  ")]
    [InlineData (" a  b ", 3, true, "  ")]
    [InlineData (" H1$&#2you ", 2, false, "H1")]
    [InlineData (" H1$&#2you ", 2, true, "H1")]
    [InlineData (" H1$&#2you ", 3, false, "$&#")]
    [InlineData (" H1$&#2you ", 3, true, "$&#")]
    [InlineData (" H1$&#2you ", 4, false, "$&#")]
    [InlineData (" H1$&#2you ", 4, true, "$&#")]
    [InlineData (" H1$&#2you ", 5, false, "$&#")]
    [InlineData (" H1$&#2you ", 5, true, "$&#")]
    [InlineData (" H1$&#2you ", 6, false, "2you ")]
    [InlineData (" H1$&#2you ", 6, true, "2you")]
    public void ProcessDoubleClickSelection_False_True (string text, int col, bool selectWordOnly, string expectedText)
    {
        TextView tv = CreateTextView ();
        tv.Text = text;
        tv.SelectWordOnlyOnDoubleClick = selectWordOnly;

        Assert.True (tv.NewMouseEvent (new Mouse { Position = new Point (col, 0), Flags = MouseFlags.LeftButtonDoubleClicked }));
        Assert.Equal (expectedText, tv.SelectedText);
    }

    [Fact]
    public void ReadOnly_True_Move_Right_Moves_Until_The_End_Of_Text_More_One_Column ()
    {
        TextView tv = CreateTextView ();
        tv.Text = "Hi";
        tv.ReadOnly = true;

        Assert.Equal (0, tv.CurrentColumn);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (1, tv.CurrentColumn);
        Assert.Equal ("H", tv.SelectedText);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (2, tv.CurrentColumn);
        Assert.Equal ("Hi", tv.SelectedText);
    }

    [Fact]
    public void Viewport_X_Treat_Negative_Width_As_One_Column ()
    {
        TextView tv = new () { Width = 2, Height = 1, Text = "\u001B[" };

        Assert.Equal (0, tv.Viewport.X);
        Assert.Equal (new Point (0, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (0, tv.Viewport.X);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, tv.Viewport.X);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        Assert.False (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, tv.Viewport.X);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentSize is updated when text is edited, allowing scrollbars to appear.
    ///     See https://github.com/gui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentSize_Updates_When_Text_Is_Edited ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();
        tv.SetRelativeLayout (new Size (100, 100));

        Size initialContentSize = tv.GetContentSize ();
        Assert.Equal (1, initialContentSize.Height); // Initially 1 line

        // Act: Type text that exceeds viewport height (add multiple lines)
        tv.NewKeyDownEvent (Key.A);
        tv.NewKeyDownEvent (Key.Enter);
        tv.NewKeyDownEvent (Key.B);
        tv.NewKeyDownEvent (Key.Enter);
        tv.NewKeyDownEvent (Key.C);
        tv.NewKeyDownEvent (Key.Enter);
        tv.NewKeyDownEvent (Key.D);

        // Assert: ContentSize height should reflect the number of lines (4 lines)
        Size newContentSize = tv.GetContentSize ();
        Assert.Equal (4, newContentSize.Height);

        // With height=3 and 4 lines of content, vertical scrollbar should be visible
        Assert.True (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible when content exceeds viewport height");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that horizontal scrollbar appears when text width exceeds viewport width.
    ///     See https://github.com/gui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void HorizontalScrollBar_Appears_When_Text_Width_Exceeds_Viewport ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled, WordWrap disabled
        TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill (), ScrollBars = true, WordWrap = false };
        tv.BeginInit ();
        tv.EndInit ();
        tv.SetRelativeLayout (new Size (10, 5));

        // Assert initial state
        Assert.False (tv.HorizontalScrollBar.Visible, "Horizontal scrollbar should not be visible initially");

        // Act: Type text that exceeds viewport width
        for (var i = 0; i < 20; i++)
        {
            tv.NewKeyDownEvent (Key.A);
        }

        // Assert: ContentSize width should reflect the text width
        Size newContentSize = tv.GetContentSize ();
        Assert.True (newContentSize.Width >= 20, "Content width should reflect the typed text length");

        // With width=10 and 20 characters, horizontal scrollbar should be visible
        Assert.True (tv.HorizontalScrollBar.Visible, "Horizontal scrollbar should be visible when content exceeds viewport width");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentSize is updated when Text property is set programmatically.
    ///     See https://github.com/gui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentSize_Updates_When_Text_Property_Is_Set ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();
        tv.SetRelativeLayout (new Size (100, 100));

        Size initialContentSize = tv.GetContentSize ();
        Assert.Equal (1, initialContentSize.Height);

        // Act: Set Text property programmatically with multiple lines
        tv.Text = "Line1\nLine2\nLine3\nLine4";

        // Assert: After setting Text programmatically with multiple lines,
        // the content size should reflect the new number of lines
        Size contentSizeAfterTextSet = tv.GetContentSize ();
        Assert.Equal (4, contentSizeAfterTextSet.Height);

        // And scrollbar should be visible since content (4 lines) > viewport (3 lines)
        Assert.True (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible after setting multi-line text");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentsChanged event is fired and content size is updated appropriately.
    ///     This test verifies the behavior described in issue #4660.
    ///     See https://github.com/gui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentsChanged_Fires_And_ContentSize_Is_Correct_After_Typing ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();
        tv.SetRelativeLayout (new Size (100, 100));

        // Track ContentsChanged events and content size at each event
        var contentsChangedCount = 0;
        var contentSizeAtLastContentsChanged = Size.Empty;

        tv.ContentsChanged += (_, _) =>
                              {
                                  contentsChangedCount++;
                                  contentSizeAtLastContentsChanged = tv.GetContentSize ();
                              };

        // Act: Type text that exceeds viewport height
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (1, contentsChangedCount);

        tv.NewKeyDownEvent (Key.Enter);
        Assert.Equal (2, contentsChangedCount);

        // After pressing Enter, we have 2 lines
        Assert.Equal (2, contentSizeAtLastContentsChanged.Height);

        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (3, contentsChangedCount);

        tv.NewKeyDownEvent (Key.Enter);
        Assert.Equal (4, contentsChangedCount);

        // After 2nd Enter, we have 3 lines
        Assert.Equal (3, contentSizeAtLastContentsChanged.Height);

        tv.NewKeyDownEvent (Key.C);
        Assert.Equal (5, contentsChangedCount);

        tv.NewKeyDownEvent (Key.Enter);
        Assert.Equal (6, contentsChangedCount);

        // After 3rd Enter, we have 4 lines
        Assert.Equal (4, contentSizeAtLastContentsChanged.Height);

        tv.NewKeyDownEvent (Key.D);
        Assert.Equal (7, contentsChangedCount);

        // Final content size should be 4 lines
        Assert.Equal (4, contentSizeAtLastContentsChanged.Height);

        // Assert: Vertical scrollbar should be visible since content (4 lines) > viewport (3 lines)
        Assert.True (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible when content exceeds viewport height");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentSize is updated *during* the ContentsChanged event, not just after.
    ///     This test specifically verifies the bug where UpdateContentSize() needs to be called
    ///     in OnContentsChanged() so that the content size is correct at the time the event fires.
    ///     See https://github.com/gui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentSize_Is_Updated_During_ContentsChanged_Event ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();
        tv.SetRelativeLayout (new Size (100, 100));

        // Track whether content size is correct INSIDE the ContentsChanged handler
        // The bug is that UpdateContentSize() isn't called in OnContentsChanged(),
        // so the content size may be stale when the event fires.
        var contentSizeWasCorrectInHandler = true;

        tv.ContentsChanged += (_, _) =>
                              {
                                  Size contentSize = tv.GetContentSize ();
                                  int expectedHeight = tv.Lines;

                                  if (contentSize.Height != expectedHeight)
                                  {
                                      contentSizeWasCorrectInHandler = false;
                                  }
                              };

        // Act: Type text that creates multiple lines
        tv.NewKeyDownEvent (Key.A); // Line 1
        tv.NewKeyDownEvent (Key.Enter); // Creates line 2
        tv.NewKeyDownEvent (Key.B); // Line 2 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 3
        tv.NewKeyDownEvent (Key.C); // Line 3 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 4
        tv.NewKeyDownEvent (Key.D); // Line 4 content

        // Assert: Content size should have been correct at each ContentsChanged event
        Assert.True (contentSizeWasCorrectInHandler, "Content size should be updated during ContentsChanged event, not just after");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that scrollbar visibility is correct *during* the ContentsChanged event.
    ///     The bug in issue #4660 is that scrollbar visibility isn't updated until after
    ///     OnContentsChanged returns, which can cause issues for event handlers.
    ///     See https://github.com/gui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ScrollBar_Visibility_Is_Correct_During_ContentsChanged_Event ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();
        tv.SetRelativeLayout (new Size (100, 100));

        // Track whether scrollbar visibility was correct when ContentsChanged fired
        // and content exceeded viewport
        var scrollbarWasVisibleWhenContentExceededViewport = false;

        tv.ContentsChanged += (_, _) =>
                              {
                                  // When we have 4 lines (height=3 viewport), scrollbar should be visible
                                  if (tv.Lines > tv.Viewport.Height)
                                  {
                                      scrollbarWasVisibleWhenContentExceededViewport = tv.VerticalScrollBar.Visible;
                                  }
                              };

        // Act: Type text that creates 4 lines (exceeds viewport height of 3)
        tv.NewKeyDownEvent (Key.A); // Line 1
        tv.NewKeyDownEvent (Key.Enter); // Creates line 2
        tv.NewKeyDownEvent (Key.B); // Line 2 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 3
        tv.NewKeyDownEvent (Key.C); // Line 3 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 4 - now content > viewport
        tv.NewKeyDownEvent (Key.D); // Line 4 content

        // Assert: Scrollbar should have been visible DURING ContentsChanged when content exceeded viewport
        Assert.True (scrollbarWasVisibleWhenContentExceededViewport,
                     "Vertical scrollbar should be visible during ContentsChanged event when content exceeds viewport height");
    }

    [Fact]
    public void Command_Activate_SetsFocus ()
    {
        TextView textView  = new () { Text = "Test", Width = 10 };
        textView.BeginInit ();
        textView.EndInit ();
        Assert.False (textView.HasFocus);

        textView.InvokeCommand (Command.Activate);

        Assert.True (textView.HasFocus);

        textView.Dispose ();
    }

    [Fact]
    public void Command_HotKey_SetsFocus ()
    {
        TextView textView = new () { Text = "Test" };
        textView.BeginInit ();
        textView.EndInit ();
        Assert.False (textView.HasFocus);

        textView.InvokeCommand (Command.HotKey);

        Assert.True (textView.HasFocus);

        textView.Dispose ();
    }

    private TextView CreateTextView () => new () { Width = 30, Height = 10 };

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_Undo_Redo_Paste_DeleteAll ()
    {
        // Ctrl+Y → Redo (was Paste)
        // Ctrl+V → Paste (was PageDown via Emacs)
        // Ctrl+R → No longer Redo
        // Ctrl+G → No longer DeleteAll
        // Ctrl+Shift+D → DeleteAll
        TextView tv = new () { Width = 40, Height = 10, Text = "hello" };
        tv.InsertionPoint = new Point (tv.Text.Length, 0);

        // Ctrl+Z → Undo
        tv.NewKeyDownEvent (Key.Backspace); // delete so undo has something
        Assert.Equal ("hell", tv.Text);
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("hello", tv.Text);

        // Ctrl+Y → Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("hell", tv.Text);
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl)); // undo to restore
        Assert.Equal ("hello", tv.Text);

        // Ctrl+R no longer mapped to Redo
        Assert.False (tv.KeyBindings.TryGet (Key.R.WithCtrl, out _));

        // Ctrl+G no longer mapped to DeleteAll
        Assert.False (tv.KeyBindings.TryGet (Key.G.WithCtrl, out _));

        // Ctrl+Shift+D → DeleteAll (verify binding exists)
        Assert.True (tv.KeyBindings.TryGet (Key.D.WithCtrl.WithShift, out KeyBinding deleteAllBinding));
        Assert.Contains (Command.DeleteAll, deleteAllBinding.Commands);
    }

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_CtrlV_Is_Paste_Not_PageDown ()
    {
        // Ctrl+V should map to Paste, not PageDown
        TextView tv = new () { Width = 40, Height = 10 };
        Assert.True (tv.KeyBindings.TryGet (Key.V.WithCtrl, out KeyBinding binding));
        Assert.Contains (Command.Paste, binding.Commands);
        Assert.DoesNotContain (Command.PageDown, binding.Commands);
    }

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_NonWindows_Undo_Redo ()
    {
        if (PlatformDetection.IsWindows ())
        {
            return; // non-Windows-only bindings are not added on Windows
        }

        TextView tv = new () { Width = 40, Height = 10, Text = "hello" };
        tv.InsertionPoint = new Point (tv.Text.Length, 0);

        // Ctrl+/ → Undo
        tv.NewKeyDownEvent (Key.Backspace); // delete so undo has something
        Assert.Equal ("hell", tv.Text);
        Assert.True (tv.NewKeyDownEvent (new Key ('/').WithCtrl));
        Assert.Equal ("hello", tv.Text);

        // Ctrl+Shift+Z → Redo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl.WithShift));
        Assert.Equal ("hell", tv.Text);
    }
}
