using System.IO;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("HexEditor", "A binary (hex) editor using the HexView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Overlapped")]
[ScenarioCategory ("Files and IO")]
public class HexEditor : Scenario
{
    private string _fileName = "demo.bin";
    private HexView _hexView;
    private MenuItem _miAllowEdits;
    private bool _saved = true;
    private Shortcut _scAddress;
    private Shortcut _scInfo;
    private Shortcut _scPosition;
    private StatusBar _statusBar;

    public override void Main ()
    {
        Application.Init ();

        var app = new Toplevel
        {
            ColorScheme = Colors.ColorSchemes ["Base"]
        };

        CreateDemoFile (_fileName);

        _hexView = new (new MemoryStream (Encoding.UTF8.GetBytes ("Demo text.")))
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            Title = _fileName ?? "Untitled",
            BorderStyle = LineStyle.Rounded,
        };
        _hexView.Edited += _hexView_Edited;
        _hexView.PositionChanged += _hexView_PositionChanged;
        app.Add (_hexView);

        var menu = new MenuBar
        {
            Menus =
            [
                new (
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
                new (
                     "_Edit",
                     new MenuItem []
                     {
                         new ("_Copy", "", () => Copy ()),
                         new ("C_ut", "", () => Cut ()),
                         new ("_Paste", "", () => Paste ())
                     }
                    ),
                new (
                     "_Options",
                     new []
                     {
                         _miAllowEdits = new (
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
        app.Add (menu);

        var addressWidthUpDown = new NumericUpDown
        {
            Value = _hexView.AddressWidth
        };

        NumericUpDown<long> addressUpDown = new NumericUpDown<long>
        {
            Value = _hexView.Address,
            Format = $"0x{{0:X{_hexView.AddressWidth}}}"
        };

        addressWidthUpDown.ValueChanging += (sender, args) =>
                                            {
                                                args.Cancel = args.NewValue is < 0 or > 8;

                                                if (!args.Cancel)
                                                {
                                                    _hexView.AddressWidth = args.NewValue;

                                                    // ReSharper disable once AccessToDisposedClosure
                                                    addressUpDown.Format = $"0x{{0:X{_hexView.AddressWidth}}}";
                                                }
                                            };

        addressUpDown.ValueChanging += (sender, args) =>
                                       {
                                           args.Cancel = args.NewValue is < 0;

                                           if (!args.Cancel)
                                           {
                                               _hexView.Address = args.NewValue;
                                           }
                                       };

        _statusBar = new (
                          [
                              new (Key.F2, "Open", Open),
                              new (Key.F3, "Save", Save),
                              new ()
                              {
                                  CommandView = addressWidthUpDown,
                                  HelpText = "Address Width"
                              },
                              _scAddress = new ()
                              {
                                  CommandView = addressUpDown,
                                  HelpText = "Address:"
                              },
                              _scInfo = new (Key.Empty, string.Empty, () => { }),
                              _scPosition = new (Key.Empty, string.Empty, () => { })
                          ])
        {
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast
        };
        app.Add (_statusBar);

        _hexView.Source = LoadFile ();

        Application.Run (app);
        addressUpDown.Dispose ();
        addressWidthUpDown.Dispose ();
        app.Dispose ();
        Application.Shutdown ();
    }

    private void _hexView_Edited (object sender, HexViewEditEventArgs e) { _saved = false; }

    private void _hexView_PositionChanged (object sender, HexViewEventArgs obj)
    {
        _scInfo.Title =
            $"Bytes: {_hexView.Source!.Length}";
        _scPosition.Title =
            $"L: {obj.Position.Y} C: {obj.Position.X} Per Line: {obj.BytesPerLine}";

        if (_scAddress.CommandView is NumericUpDown<long> addrNumericUpDown)
        {
            addrNumericUpDown.Value = obj.Address;
        }
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
        sb.Append ("Hello world with wide codepoints: 𝔹Aℝ𝔽.\n");
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

        if (!_saved && _hexView.Edits.Count > 0)
        {
            if (MessageBox.ErrorQuery (
                                       "Save",
                                       "The changes were not saved. Want to open without saving?",
                                       "_Yes",
                                       "_No"
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
            _hexView.Title = _fileName;
            _saved = true;
        }
        else
        {
            _hexView.Title = _fileName ?? "Untitled";
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

    private void Paste () { MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "_Ok"); }
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
