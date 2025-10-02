using System.Linq;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Interactive Tree", "Create nodes and child nodes in TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class InteractiveTree : Scenario
{
    private TreeView _treeView;

    public override void Main ()
    {
        Application.Init ();
        var appWindow = new Toplevel ()
        {
            Title = GetName (),
        };

        var menu = new MenuBar
        {
            Menus =
            [
                new ("_File", new MenuItem [] { new ("_Quit", "", Quit) })
            ]
        };
        appWindow.Add (menu);

        _treeView = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };
        _treeView.KeyDown += TreeView_KeyPress;

        appWindow.Add (_treeView);

        var statusBar = new StatusBar (
                                       new Shortcut []
                                       {
                                           new (Application.QuitKey, "Quit", Quit),
                                           new (Key.C.WithCtrl, "Add Child", AddChildNode),
                                           new (Key.T.WithCtrl, "Add Root", AddRootNode),
                                           new (Key.R.WithCtrl, "Rename Node", RenameNode)
                                       }
                                      );
        appWindow.Add (statusBar);

        Application.Run (appWindow);
        appWindow.Dispose ();
        Application.Shutdown ();
    }

    private void AddChildNode ()
    {
        ITreeNode node = _treeView.SelectedObject;

        if (node != null)
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
        if (GetText ("Text", "Enter text for node:", "", out string entered))
        {
            _treeView.AddObject (new TreeNode (entered));
        }
    }

    private bool GetText (string title, string label, string initialText, out string enteredText)
    {
        var okPressed = false;

        var ok = new Button { Text = "Ok", IsDefault = true };

        ok.Accepting += (s, e) =>
                     {
                         okPressed = true;
                         Application.RequestStop ();
                     };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepting += (s, e) => Application.RequestStop ();
        var d = new Dialog { Title = title, Buttons = [ok, cancel] };

        var lbl = new Label { X = 0, Y = 1, Text = label };

        var tf = new TextField { Text = initialText, X = 0, Y = 2, Width = Dim.Fill () };

        d.Add (lbl, tf);
        tf.SetFocus ();

        Application.Run (d);
        d.Dispose ();

        enteredText = okPressed ? tf.Text : null;

        return okPressed;
    }

    private void Quit () { Application.RequestStop (); }

    private void RenameNode ()
    {
        ITreeNode node = _treeView.SelectedObject;

        if (node != null)
        {
            if (GetText ("Text", "Enter text for node:", node.Text, out string entered))
            {
                node.Text = entered;
                _treeView.RefreshObject (node);
            }
        }
    }

    private void TreeView_KeyPress (object sender, Key obj)
    {
        if (obj.KeyCode == Key.Delete)
        {
            ITreeNode toDelete = _treeView.SelectedObject;

            if (toDelete == null)
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
                ITreeNode parent = _treeView.GetParent (toDelete);

                if (parent == null)
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
