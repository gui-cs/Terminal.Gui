using System.IO;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("HexEditor", "A binary (hex) editor using the HexView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Top Level Windows")]
[ScenarioCategory ("Files and IO")]
public class HexEditor : Scenario
{
    private string _fileName = "demo.bin";
    private HexView _hexView;
    private MenuItem _miAllowEdits;
    private bool _saved = true;
    private StatusItem _siPositionChanged;
    private StatusBar _statusBar;

    public override void Setup ()
    {
        Win.Title = GetName () + "-" + _fileName ?? "Untitled";

        CreateDemoFile (_fileName);

        //CreateUnicodeDemoFile (_fileName);

        _hexView = new HexView (LoadFile ()) { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        _hexView.Edited += _hexView_Edited;
        _hexView.PositionChanged += _hexView_PositionChanged;
        Win.Add (_hexView);

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem (
                                 "_File",
                                 new MenuItem []
                                 {
                                     new ("_New", "", () => New ()),
                                     new ("_Open", "", () => Open ()),
                                     new ("_Save", "", () => Save ()),
                                     null,
                                     new ("_Quit", "", () => Quit ())
                                 }
                                ),
                new MenuBarItem (
                                 "_Edit",
                                 new MenuItem []
                                 {
                                     new ("_Copy", "", () => Copy ()),
                                     new ("C_ut", "", () => Cut ()),
                                     new ("_Paste", "", () => Paste ())
                                 }
                                ),
                new MenuBarItem (
                                 "_Options",
                                 new []
                                 {
                                     _miAllowEdits = new MenuItem (
                                                                   "_AllowEdits",
                                                                   "",
                                                                   () => ToggleAllowEdits ()
                                                                  )
                                     {
                                         Checked = _hexView.AllowEdits,
                                         CheckType = MenuItemCheckStyle
                                             .Checked
                                     }
                                 }
                                )
            ]
        };
        Top.Add (menu);

        _statusBar = new StatusBar (
                                    new []
                                    {
                                        new (KeyCode.F2, "~F2~ Open", () => Open ()),
                                        new (KeyCode.F3, "~F3~ Save", () => Save ()),
                                        new (
                                             Application.QuitKey,
                                             $"{Application.QuitKey} to Quit",
                                             () => Quit ()
                                            ),
                                        _siPositionChanged = new StatusItem (
                                                                             KeyCode.Null,
                                                                             $"Position: {
                                                                                 _hexView.Position
                                                                             } Line: {
                                                                                 _hexView.CursorPosition.Y
                                                                             } Col: {
                                                                                 _hexView.CursorPosition.X
                                                                             } Line length: {
                                                                                 _hexView.BytesPerLine
                                                                             }",
                                                                             () => { }
                                                                            )
                                    }
                                   );
        Top.Add (_statusBar);
    }

    private void _hexView_Edited (object sender, HexViewEditEventArgs e) { _saved = false; }

    private void _hexView_PositionChanged (object sender, HexViewEventArgs obj)
    {
        _siPositionChanged.Title =
            $"Position: {
                obj.Position
            } Line: {
                obj.CursorPosition.Y
            } Col: {
                obj.CursorPosition.X
            } Line length: {
                obj.BytesPerLine
            }";
        _statusBar.SetNeedsDisplay ();
    }

    private void Copy () { MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok"); }

    private void CreateDemoFile (string fileName)
    {
        var sb = new StringBuilder ();
        sb.Append ("Hello world.\n");
        sb.Append ("This is a test of the Emergency Broadcast System.\n");

        StreamWriter sw = File.CreateText (fileName);
        sw.Write (sb.ToString ());
        sw.Close ();
    }

    private void CreateUnicodeDemoFile (string fileName)
    {
        var sb = new StringBuilder ();
        sb.Append ("Hello world.\n");
        sb.Append ("This is a test of the Emergency Broadcast System.\n");

        byte [] buffer = Encoding.Unicode.GetBytes (sb.ToString ());
        var ms = new MemoryStream (buffer);
        var file = new FileStream (fileName, FileMode.Create, FileAccess.Write);
        ms.WriteTo (file);
        file.Close ();
        ms.Close ();
    }

    private void Cut () { MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok"); }

    private Stream LoadFile ()
    {
        var stream = new MemoryStream ();

        if (!_saved && _hexView != null && _hexView.Edits.Count > 0)
        {
            if (MessageBox.ErrorQuery (
                                       "Save",
                                       "The changes were not saved. Want to open without saving?",
                                       "Yes",
                                       "No"
                                      )
                == 1)
            {
                return _hexView.Source;
            }

            _hexView.DiscardEdits ();
            _saved = true;
        }

        if (_fileName != null)
        {
            byte [] bin = File.ReadAllBytes (_fileName);
            stream.Write (bin);
            Win.Title = GetName () + "-" + _fileName;
            _saved = true;
        }
        else
        {
            Win.Title = GetName () + "-" + (_fileName ?? "Untitled");
        }

        return stream;
    }

    private void New ()
    {
        _fileName = null;
        _hexView.Source = LoadFile ();
    }

    private void Open ()
    {
        var d = new OpenDialog { Title = "Open", AllowsMultipleSelection = false };
        Application.Run (d);

        if (!d.Canceled)
        {
            _fileName = d.FilePaths [0];
            _hexView.Source = LoadFile ();
            _hexView.DisplayStart = 0;
        }
        d.Dispose ();
    }

    private void Paste () { MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok"); }
    private void Quit () { Application.RequestStop (); }

    private void Save ()
    {
        if (_fileName != null)
        {
            using (var fs = new FileStream (_fileName, FileMode.OpenOrCreate))
            {
                _hexView.ApplyEdits (fs);

                //_hexView.Source.Position = 0;
                //_hexView.Source.CopyTo (fs);
                //fs.Flush ();
            }

            _saved = true;
        }
        else
        {
            _hexView.ApplyEdits ();
        }
    }

    private void ToggleAllowEdits () { _hexView.AllowEdits = (bool)(_miAllowEdits.Checked = !_miAllowEdits.Checked); }
}
