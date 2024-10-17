using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tile View Nesting", "Demonstrates recursive nesting of TileViews")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("LineView")]
public class TileViewNesting : Scenario
{
    private CheckBox _cbBorder;
    private CheckBox _cbHorizontal;
    private CheckBox _cbTitles;
    private CheckBox _cbUseLabels;
    private TextField _textField;
    private int _viewsCreated;
    private int _viewsToCreate;
    private View _workArea;

    /// <summary>Setup the scenario.</summary>
    public override void Main ()
    {
        Application.Init ();
        // Scenario Windows.
        var win = new Window
        {
            Title = GetName (),
            Y = 1
        };

        var lblViews = new Label { Text = "Number Of Views:" };
        _textField = new() { X = Pos.Right (lblViews), Width = 10, Text = "2" };

        _textField.TextChanged += (s, e) => SetupTileView ();

        _cbHorizontal = new() { X = Pos.Right (_textField) + 1, Text = "Horizontal" };
        _cbHorizontal.CheckedStateChanged += (s, e) => SetupTileView ();

        _cbBorder = new() { X = Pos.Right (_cbHorizontal) + 1, Text = "Border" };
        _cbBorder.CheckedStateChanged += (s, e) => SetupTileView ();

        _cbTitles = new() { X = Pos.Right (_cbBorder) + 1, Text = "Titles" };
        _cbTitles.CheckedStateChanged += (s, e) => SetupTileView ();

        _cbUseLabels = new() { X = Pos.Right (_cbTitles) + 1, Text = "Use Labels" };
        _cbUseLabels.CheckedStateChanged += (s, e) => SetupTileView ();

        _workArea = new() { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill () };

        var menu = new MenuBar
        {
            Menus =
            [
                new ("_File", new MenuItem [] { new ("_Quit", "", () => Quit ()) })
            ]
        };

        win.Add (lblViews);
        win.Add (_textField);
        win.Add (_cbHorizontal);
        win.Add (_cbBorder);
        win.Add (_cbTitles);
        win.Add (_cbUseLabels);
        win.Add (_workArea);

        SetupTileView ();

        var top = new Toplevel ();
        top.Add (menu);
        top.Add (win);

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }

    private void AddMoreViews (TileView to)
    {
        if (_viewsCreated == _viewsToCreate)
        {
            return;
        }

        if (!(to.Tiles.ElementAt (0).ContentView is TileView))
        {
            Split (to, true);
        }

        if (!(to.Tiles.ElementAt (1).ContentView is TileView))
        {
            Split (to, false);
        }

        if (to.Tiles.ElementAt (0).ContentView is TileView && to.Tiles.ElementAt (1).ContentView is TileView)
        {
            AddMoreViews ((TileView)to.Tiles.ElementAt (0).ContentView);
            AddMoreViews ((TileView)to.Tiles.ElementAt (1).ContentView);
        }
    }

    private View CreateContentControl (int number) { return _cbUseLabels.CheckedState == CheckState.Checked ? CreateLabelView (number) : CreateTextView (number); }

    private View CreateLabelView (int number)
    {
        return new Label
        {
            Width = Dim.Fill (),
            Height = 1,

            Text = number.ToString ().Repeat (1000),
            CanFocus = true
        };
    }

    private View CreateTextView (int number)
    {
        return new TextView
        {
            Width = Dim.Fill (), Height = Dim.Fill (), Text = number.ToString ().Repeat (1000), AllowsTab = false

            //WordWrap = true,  // TODO: This is very slow (like 10s to render with 45 views)
        };
    }

    private TileView CreateTileView (int titleNumber, Orientation orientation)
    {
        var toReturn = new TileView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),

            // flip the orientation
            Orientation = orientation
        };

        toReturn.Tiles.ElementAt (0).Title = _cbTitles.CheckedState == CheckState.Checked ? $"View {titleNumber}" : string.Empty;
        toReturn.Tiles.ElementAt (1).Title = _cbTitles.CheckedState == CheckState.Checked ? $"View {titleNumber + 1}" : string.Empty;

        return toReturn;
    }

    private int GetNumberOfViews ()
    {
        if (int.TryParse (_textField.Text, out int views) && views >= 0)
        {
            return views;
        }

        return 0;
    }

    private void Quit () { Application.RequestStop (); }

    private void SetupTileView ()
    {
        int numberOfViews = GetNumberOfViews ();

        CheckState titles = _cbTitles.CheckedState;
        CheckState border = _cbBorder.CheckedState;
        CheckState startHorizontal = _cbHorizontal.CheckedState;

        foreach (View sub in _workArea.Subviews)
        {
            sub.Dispose ();
        }

        _workArea.RemoveAll ();

        if (numberOfViews <= 0)
        {
            return;
        }

        TileView root = CreateTileView (1, startHorizontal == CheckState.Checked ? Orientation.Horizontal : Orientation.Vertical);

        root.Tiles.ElementAt (0).ContentView.Add (CreateContentControl (1));
        root.Tiles.ElementAt (0).Title = _cbTitles.CheckedState == CheckState.Checked ? "View 1" : string.Empty;
        root.Tiles.ElementAt (1).ContentView.Add (CreateContentControl (2));
        root.Tiles.ElementAt (1).Title = _cbTitles.CheckedState == CheckState.Checked ? "View 2" : string.Empty;

        root.LineStyle = border  == CheckState.Checked? LineStyle.Rounded : LineStyle.None;

        _workArea.Add (root);

        if (numberOfViews == 1)
        {
            root.Tiles.ElementAt (1).ContentView.Visible = false;
        }

        if (numberOfViews > 2)
        {
            _viewsCreated = 2;
            _viewsToCreate = numberOfViews;
            AddMoreViews (root);
        }
    }

    private void Split (TileView to, bool left)
    {
        if (_viewsCreated == _viewsToCreate)
        {
            return;
        }

        TileView newView;

        if (left)
        {
            to.TrySplitTile (0, 2, out newView);
        }
        else
        {
            to.TrySplitTile (1, 2, out newView);
        }

        _viewsCreated++;

        // During splitting the old Title will have been migrated to View1 so we only need
        // to set the Title on View2 (the one that gets our new TextView)
        newView.Tiles.ElementAt (1).Title = _cbTitles.CheckedState == CheckState.Checked ? $"View {_viewsCreated}" : string.Empty;

        // Flip orientation
        newView.Orientation = to.Orientation == Orientation.Vertical
                                  ? Orientation.Horizontal
                                  : Orientation.Vertical;

        newView.Tiles.ElementAt (1).ContentView.Add (CreateContentControl (_viewsCreated));
    }
}
