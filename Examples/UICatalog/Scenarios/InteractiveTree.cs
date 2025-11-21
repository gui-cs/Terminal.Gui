#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Interactive Tree", "Create nodes and child nodes in TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class InteractiveTree : Scenario
{
    private TreeView? _treeView;

    public override void Main ()
    {
        Application.Init ();

        Window appWindow = new ()
        {
            Title = GetName (),
            BorderStyle = LineStyle.None    
        };

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
                  new MenuBarItem (
                                   "_File",
                                   [
                                       new MenuItem
                                       {
                                           Title = "_Quit",
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        _treeView = new ()
        {
            X = 0,
            Y = Pos.Bottom (menu),
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };
        _treeView.KeyDown += TreeView_KeyPress;

        // StatusBar
        StatusBar statusBar = new (
                                   [
                                       new (Application.QuitKey, "Quit", Quit),
                                       new (Key.C.WithCtrl, "Add Child", AddChildNode),
                                       new (Key.T.WithCtrl, "Add Root", AddRootNode),
                                       new (Key.R.WithCtrl, "Rename Node", RenameNode)
                                   ]
                                  );

        appWindow.Add (menu, _treeView, statusBar);

        Application.Run (appWindow);
        appWindow.Dispose ();
        Application.Shutdown ();
    }

    private void AddChildNode ()
    {
        if (_treeView is null)
        {
            return;
        }

        ITreeNode? node = _treeView.SelectedObject;

        if (node is { })
        {
            if (GetText ("Text", "Enter text for node:", "", out string entered))
            {
                node.Children.Add (new TreeNode (entered));
                _treeView.RefreshObject (node);
            }
        }
    }

    private void AddRootNode ()
    {
        if (_treeView is null)
        {
            return;
        }

        if (GetText ("Text", "Enter text for node:", "", out string entered))
        {
            _treeView.AddObject (new TreeNode (entered));
        }
    }

    private bool GetText (string title, string label, string initialText, out string enteredText)
    {
        var okPressed = false;

        Button ok = new () { Text = "Ok", IsDefault = true };

        ok.Accepting += (s, e) =>
                        {
                            okPressed = true;
                            Application.RequestStop ();
                        };
        Button cancel = new () { Text = "Cancel" };
        cancel.Accepting += (s, e) => Application.RequestStop ();
        Dialog d = new () { Title = title, Buttons = [ok, cancel] };

        Label lbl = new () { X = 0, Y = 1, Text = label };

        TextField tf = new () { Text = initialText, X = 0, Y = 2, Width = Dim.Fill () };

        d.Add (lbl, tf);
        tf.SetFocus ();

        Application.Run (d);
        d.Dispose ();

        enteredText = okPressed ? tf.Text : string.Empty;

        return okPressed;
    }

    private void Quit () { Application.RequestStop (); }

    private void RenameNode ()
    {
        if (_treeView is null)
        {
            return;
        }

        ITreeNode? node = _treeView.SelectedObject;

        if (node is { })
        {
            if (GetText ("Text", "Enter text for node:", node.Text, out string entered))
            {
                node.Text = entered;
                _treeView.RefreshObject (node);
            }
        }
    }

    private void TreeView_KeyPress (object? sender, Key obj)
    {
        if (_treeView is null)
        {
            return;
        }

        if (obj.KeyCode == Key.Delete)
        {
            ITreeNode? toDelete = _treeView.SelectedObject;

            if (toDelete is null)
            {
                return;
            }

            obj.Handled = true;

            // if it is a root object remove it
            if (_treeView.Objects.Contains (toDelete))
            {
                _treeView.Remove (toDelete);
            }
            else
            {
                ITreeNode? parent = _treeView.GetParent (toDelete);

                if (parent is null)
                {
                    MessageBox.ErrorQuery (
                                           "Could not delete",
                                           $"Parent of '{toDelete}' was unexpectedly null",
                                           "Ok"
                                          );
                }
                else
                {
                    //update the model
                    parent.Children.Remove (toDelete);

                    //refresh the tree
                    _treeView.RefreshObject (parent);
                }
            }
        }
    }
}
