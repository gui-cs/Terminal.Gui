using System.Reflection;
using System.Text;

namespace UnitTests.ViewsTests;

public partial class TextViewTests
{
    [Fact]
    [AutoInitShutdown]
    public void HistoryText_Undo_Redo_Copy_Without_Selection_Multi_Line_Paste ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { App = ApplicationImpl.Instance, Text = text };

        tv.InsertionPoint = new (23, 0);

        Assert.True (tv.NewKeyDownEvent (Key.C.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ("This is the first line.", Clipboard.Contents);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (23, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (23, 0), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (23, 1), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Cut_Multi_Line_Another_Selected_Paste ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.W.WithCtrl));

        Assert.Equal (
                      $"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        tv.SelectionStartColumn = 12;
        tv.SelectionStartRow = 1;
        tv.InsertionPoint = new (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal (
                      $"This is the  line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (12, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the  line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 1), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Cut_Multi_Line_Selected_Paste ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.W.WithCtrl));

        Assert.Equal (
                      $"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (11, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (17, 0), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (17, 0), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Cut_Simple_Paste_Starting ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.W.WithCtrl));
        Assert.Equal ($"This is the  line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        tv.IsSelecting = false;

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the  line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Empty_Copy_Without_Selection_Multi_Line_Selected_Paste ()
    {
        var text = "\nThis is the first line.\nThis is the second line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        Assert.True (tv.NewKeyDownEvent (Key.C.WithCtrl));

        Assert.Equal (
                      $"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.",
                      tv.Text
                     );
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal (
                      $"{Environment.NewLine}{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"{Environment.NewLine}{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_KillToEndOfLine ()
    {
        var text = "First line.\nSecond line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        Assert.True (tv.NewKeyDownEvent (Key.K.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ("First line.", Clipboard.Contents);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.K.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ($"First line.{Environment.NewLine}", Clipboard.Contents);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.K.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", Clipboard.Contents);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_KillToLeftStart ()
    {
        var text = "First line.\nSecond line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (12, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl.WithShift));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ("Second line.", Clipboard.Contents);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl.WithShift));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ($"Second line.{Environment.NewLine}", Clipboard.Contents);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl.WithShift));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ($"Second line.{Environment.NewLine}First line.", Clipboard.Contents);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (12, 1), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_DeleteCharLeft ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var ntimes = 3;
        tv.InsertionPoint = new (7, 1);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharLeft ();
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 1), tv.InsertionPoint);

        tv.InsertionPoint = new (7, 0);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharLeft ();
        }

        Assert.Equal (
                      $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 0), tv.InsertionPoint);

        tv.InsertionPoint = new (7, 2);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharLeft ();
        }

        Assert.Equal (
                      $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

            switch (i)
            {
                case 0:
                    Assert.Equal (
                                  $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This  the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (5, 2), tv.InsertionPoint);

                    break;
                case 1:
                    Assert.Equal (
                                  $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This i the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (6, 2), tv.InsertionPoint);

                    break;
                case 2:
                    Assert.Equal (
                                  $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (7, 2), tv.InsertionPoint);

                    break;
            }
        }

        Assert.Equal (
                      $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 2), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

            switch (i)
            {
                case 0:
                    Assert.Equal (
                                  $"This  the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (5, 0), tv.InsertionPoint);

                    break;
                case 1:
                    Assert.Equal (
                                  $"This i the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (6, 0), tv.InsertionPoint);

                    break;
                case 2:
                    Assert.Equal (
                                  $"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (7, 0), tv.InsertionPoint);

                    break;
            }
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 0), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

            switch (i)
            {
                case 0:
                    Assert.Equal (
                                  $"This is the first line.{Environment.NewLine}This  the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (5, 1), tv.InsertionPoint);

                    break;
                case 1:
                    Assert.Equal (
                                  $"This is the first line.{Environment.NewLine}This i the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (6, 1), tv.InsertionPoint);

                    break;
                case 2:
                    Assert.Equal (
                                  $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (7, 1), tv.InsertionPoint);

                    break;
            }
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 0), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_DeleteCharRight ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var ntimes = 3;
        tv.InsertionPoint = new (7, 1);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharRight ();
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        tv.InsertionPoint = new (7, 0);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharRight ();
        }

        Assert.Equal (
                      $"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 0), tv.InsertionPoint);

        tv.InsertionPoint = new (7, 2);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharRight ();
        }

        Assert.Equal (
                      $"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This ise third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 2), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 2), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 0), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 0), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This ise third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 2), tv.InsertionPoint);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var messy = " messy";
        tv.InsertionPoint = new (7, 1);
        tv.InsertText (messy);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 1), tv.InsertionPoint);

        tv.InsertionPoint = new (7, 0);
        tv.InsertText (messy);

        Assert.Equal (
                      $"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 0), tv.InsertionPoint);

        tv.InsertionPoint = new (7, 2);
        tv.InsertText (messy);

        Assert.Equal (
                      $"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is messy the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 2), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 2), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 0), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 1), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 0), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is messy the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 2), tv.InsertionPoint);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_Copy_Simple_Paste_Starting_On_Letter ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.C.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ($"first line.{Environment.NewLine}This is the second", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.InsertionPoint);

        tv.IsSelecting = false;
        tv.InsertionPoint = new (17, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the seconfirst line.{Environment.NewLine}This is the secondd line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (18, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 1), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the seconfirst line.{Environment.NewLine}This is the secondd line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (18, 2), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_Copy_Simple_Paste_Starting_On_Space ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.C.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ($"first line.{Environment.NewLine}This is the second", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.InsertionPoint);

        tv.IsSelecting = false;

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the secondfirst line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (18, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (18, 1), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the secondfirst line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (18, 2), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_InsertText ()
    {
        var text =
            $"This is the first line.{Environment.NewLine}This is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var messy = " messy";
        tv.InsertionPoint = new (7, 0);
        tv.SelectionStartColumn = 11;
        tv.SelectionStartRow = 2;
        Assert.Equal (51 + Environment.NewLine.Length * 2, tv.SelectedLength);

        for (var i = 0; i < messy.Length; i++)
        {
            tv.InsertText (messy [i].ToString ());

            switch (i)
            {
                case 0:
                    Assert.Equal ("This is  third line.", tv.Text);
                    Assert.Equal (new (8, 0), tv.InsertionPoint);

                    break;
                case 1:
                    Assert.Equal ("This is m third line.", tv.Text);
                    Assert.Equal (new (9, 0), tv.InsertionPoint);

                    break;
                case 2:
                    Assert.Equal ("This is me third line.", tv.Text);
                    Assert.Equal (new (10, 0), tv.InsertionPoint);

                    break;
                case 3:
                    Assert.Equal ("This is mes third line.", tv.Text);
                    Assert.Equal (new (11, 0), tv.InsertionPoint);

                    break;
                case 4:
                    Assert.Equal ("This is mess third line.", tv.Text);
                    Assert.Equal (new (12, 0), tv.InsertionPoint);

                    break;
                case 5:
                    Assert.Equal ("This is messy third line.", tv.Text);
                    Assert.Equal (new (13, 0), tv.InsertionPoint);

                    break;
            }
        }

        Assert.Equal ("This is messy third line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (13, 0), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (2, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

            switch (i)
            {
                case 0:
                    Assert.Equal ("This is mess third line.", tv.Text);
                    Assert.Equal (new (12, 0), tv.InsertionPoint);

                    break;
                case 1:
                    Assert.Equal ("This is mes third line.", tv.Text);
                    Assert.Equal (new (11, 0), tv.InsertionPoint);

                    break;
                case 2:
                    Assert.Equal ("This is me third line.", tv.Text);
                    Assert.Equal (new (10, 0), tv.InsertionPoint);

                    break;
                case 3:
                    Assert.Equal ("This is m third line.", tv.Text);
                    Assert.Equal (new (9, 0), tv.InsertionPoint);

                    break;
                case 4:
                    Assert.Equal ("This is  third line.", tv.Text);
                    Assert.Equal (new (8, 0), tv.InsertionPoint);

                    break;
                case 5:
                    Assert.Equal (
                                  $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                                  tv.Text
                                 );
                    Assert.Equal (new (7, 0), tv.InsertionPoint);

                    break;
            }
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 0), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (2, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

            switch (i)
            {
                case 0:
                    Assert.Equal ("This is  third line.", tv.Text);
                    Assert.Equal (new (8, 0), tv.InsertionPoint);

                    break;
                case 1:
                    Assert.Equal ("This is m third line.", tv.Text);
                    Assert.Equal (new (9, 0), tv.InsertionPoint);

                    break;
                case 2:
                    Assert.Equal ("This is me third line.", tv.Text);
                    Assert.Equal (new (10, 0), tv.InsertionPoint);

                    break;
                case 3:
                    Assert.Equal ("This is mes third line.", tv.Text);
                    Assert.Equal (new (11, 0), tv.InsertionPoint);

                    break;
                case 4:
                    Assert.Equal ("This is mess third line.", tv.Text);
                    Assert.Equal (new (12, 0), tv.InsertionPoint);

                    break;
                case 5:
                    Assert.Equal ("This is messy third line.", tv.Text);
                    Assert.Equal (new (13, 0), tv.InsertionPoint);

                    break;
            }
        }

        Assert.Equal ("This is messy third line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (13, 0), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (2, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_InsertText_Twice_On_Same_Line ()
    {
        var text = "One\nTwo\nThree";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        tv.SelectionStartColumn = 0;
        tv.SelectionStartRow = 0;
        tv.InsertionPoint = new (0, 1);
        Assert.Equal (3 + Environment.NewLine.Length, tv.SelectedLength);

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ($"1Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        tv.SelectionStartColumn = 1;
        tv.SelectionStartRow = 0;
        tv.InsertionPoint = new (1, 1);
        Assert.Equal (4 + Environment.NewLine.Length, tv.SelectedLength);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ("12hree", tv.Text);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"1Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"1Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("12hree", tv.Text);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_InsertText_Twice_On_Same_Line_With_End_Line ()
    {
        var text = "One\nTwo\nThree\n";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        tv.SelectionStartColumn = 0;
        tv.SelectionStartRow = 0;
        tv.InsertionPoint = new (0, 1);
        Assert.Equal (3 + Environment.NewLine.Length, tv.SelectedLength);

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        tv.SelectionStartColumn = 1;
        tv.SelectionStartRow = 0;
        tv.InsertionPoint = new (1, 1);
        Assert.Equal (4 + Environment.NewLine.Length, tv.SelectedLength);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_With_Empty_Text ()
    {
        var tv = new TextView { Width = 10, Height = 2 };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.O.WithShift));
        Assert.Equal ("O", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.N));
        Assert.Equal ("On", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.E));
        Assert.Equal ("One", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"One{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.T.WithShift));
        Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.W));
        Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.O));
        Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (3, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.T.WithShift));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.H));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (2, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (3, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.E));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.E));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 3), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        tv.SelectionStartColumn = 0;
        tv.SelectionStartRow = 0;
        tv.InsertionPoint = new (0, 1);
        Assert.Equal (3 + Environment.NewLine.Length, tv.SelectedLength);

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        tv.SelectionStartColumn = 1;
        tv.SelectionStartRow = 0;
        tv.InsertionPoint = new (1, 1);
        Assert.Equal (4 + Environment.NewLine.Length, tv.SelectedLength);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        // Undoing
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (3, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (2, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (3, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("One", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("On", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("O", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);

        // Redoing
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("O", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("On", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("One", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (3, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (2, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (3, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 3), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Multi_Line_With_Empty_Text ()
    {
        var tv = new TextView { Width = 10, Height = 2 };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.O.WithShift));
        Assert.Equal ("O", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.N));
        Assert.Equal ("On", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.E));
        Assert.Equal ("One", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"One{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.T.WithShift));
        Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.W));
        Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.O));
        Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (3, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.T.WithShift));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.H));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (2, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (3, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.E));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.E));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 3), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        // Undoing
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (3, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (2, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (3, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("One", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("On", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("O", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redoing
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("O", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("On", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (2, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ("One", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new (3, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (1, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (2, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (3, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (1, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (2, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (3, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 3), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new (0, 3), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Setting_Clipboard_Multi_Line_Selected_Paste ()
    {
        var text = "This is the first line.\nThis is the second line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        Clipboard.Contents = "Inserted\nNewLine";

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal (
                      $"Inserted{Environment.NewLine}NewLineThis is the first line.{Environment.NewLine}This is the second line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));

        Assert.Equal (
                      $"Inserted{Environment.NewLine}NewLineThis is the first line.{Environment.NewLine}This is the second line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);
    }

    [Fact]
    [AutoInitShutdown]
    public void HistoryText_Undo_Redo_Simple_Copy_Multi_Line_Selected_Paste ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";

        var tv = new TextView
        {
            App = ApplicationImpl.Instance,
            Text = text
        };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.C.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal ("first", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (17, 0), tv.InsertionPoint);

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new (11, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (17, 0), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (12, 0), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new (17, 0), tv.InsertionPoint);
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Single_Line_DeleteCharLeft ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var ntimes = 3;
        tv.InsertionPoint = new (7, 1);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharLeft ();
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (4, 1), tv.InsertionPoint);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Single_Line_DeleteCharRight ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var ntimes = 3;
        tv.InsertionPoint = new (7, 1);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharRight ();
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Single_Line_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var messy = " messy";
        tv.InsertionPoint = new (7, 1);
        tv.InsertText (messy);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 1), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 1), tv.InsertionPoint);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Single_Line_Selected_DeleteCharLeft ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var ntimes = 3;
        tv.InsertionPoint = new (7, 1);
        tv.SelectionStartColumn = 11;
        tv.SelectionStartRow = 1;
        Assert.Equal (4, tv.SelectedLength);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharLeft ();
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This  second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This  second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (5, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Single_Line_Selected_DeleteCharRight ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var ntimes = 3;
        tv.InsertionPoint = new (7, 1);
        tv.SelectionStartColumn = 11;
        tv.SelectionStartRow = 1;
        Assert.Equal (4, tv.SelectedLength);

        for (var i = 0; i < ntimes; i++)
        {
            tv.DeleteCharRight ();
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This isecond line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < ntimes; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This isecond line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void HistoryText_Undo_Redo_Single_Line_Selected_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 10, Height = 2, Text = text };
        Runnable top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        var messy = " messy";
        tv.InsertionPoint = new (7, 1);
        tv.SelectionStartColumn = 11;
        tv.SelectionStartRow = 1;
        Assert.Equal (4, tv.SelectedLength);
        tv.InsertText (messy);

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is messy second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (7, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);

        for (var i = 0; i < messy.Length; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.R.WithCtrl));
        }

        Assert.Equal (
                      $"This is the first line.{Environment.NewLine}This is messy second line.{Environment.NewLine}This is the third line.",
                      tv.Text
                     );
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new (13, 1), tv.InsertionPoint);
        Assert.Equal (11, tv.SelectionStartColumn);
        Assert.Equal (1, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        top.Dispose ();
    }
}
