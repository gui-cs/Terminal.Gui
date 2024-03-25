using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornments Demo", "Demonstrates Margin, Border, and Padding on Views.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Borders")]
public class Adornments : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

        var view = new Window { Title = "The _Window" };
        var tf1 = new TextField { Width = 10, Text = "TextField" };
        var color = new ColorPicker { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd (11) };
        color.BorderStyle = LineStyle.RoundedDotted;

        color.ColorChanged += (s, e) =>
                              {
                                  color.SuperView.ColorScheme = new (color.SuperView.ColorScheme)
                                  {
                                      Normal = new (
                                                    color.SuperView.ColorScheme.Normal.Foreground,
                                                    e.Color
                                                   )
                                  };
                              };

        var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Press me!" };

        button.Accept += (s, e) =>
                             MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");

        var label = new TextView
        {
            X = Pos.Center (),
            Y = Pos.Bottom (button),
            Title = "Title",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.",
            Width = 40,
            Height = 6 // TODO: Use Dim.Auto
        };
        label.Border.Thickness = new (1, 3, 1, 1);

        var btnButtonInWindow = new Button { X = Pos.AnchorEnd (10), Y = Pos.AnchorEnd (1), Text = "Button" };

        var tv = new Label
        {
            AutoSize = false,
            Y = Pos.AnchorEnd (3),
            Width = 25,
            Height = Dim.Fill (),
            Text = "Label\nY=AnchorEnd(3),Height=Dim.Fill()"
        };

        view.Margin.Data = "Margin";
        view.Margin.Thickness = new (3);

        view.Border.Data = "Border";
        view.Border.Thickness = new (3);

        view.Padding.Data = "Padding";
        view.Padding.Thickness = new (3);

        view.Add (tf1, color, button, label, btnButtonInWindow, tv);

        var editor = new AdornmentsEditor
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
            ColorScheme = Colors.ColorSchemes [TopLevelColorScheme]

            //BorderStyle = LineStyle.None,
        };
        view.X = 36;
        view.Y = 0;
        view.Width = Dim.Percent (60);
        view.Height = Dim.Percent (80);

        editor.Initialized += (s, e) => { editor.ViewToEdit = view; };

        view.Initialized += (s, e) =>
                            {
                                var labelInPadding = new Label () {  X = 1, Y = 0, Title = "_Text:" };
                                view.Padding.Add (labelInPadding);

                                var textFieldInPadding = new TextField () { X = Pos.Right (labelInPadding) + 1, Y = Pos.Top (labelInPadding), Width = 15, Text = "some text" };
                                textFieldInPadding.Accept += (s, e) => MessageBox.Query (20, 7, "TextField", textFieldInPadding.Text, "Ok");
                                view.Padding.Add (textFieldInPadding);

                                var btnButtonInPadding = new Button { X = Pos.Center (), Y = 0, Text = "_Button in Padding" };
                                btnButtonInPadding.Accept += (s, e) => MessageBox.Query (20, 7, "Hi", "Button in Padding Pressed!", "Ok");
                                btnButtonInPadding.BorderStyle = LineStyle.Dashed;
                                btnButtonInPadding.Border.Thickness = new (1,1,1,1);
                                view.Padding.Add (btnButtonInPadding);

#if SUBVIEW_BASED_BORDER
                                btnButtonInPadding.Border.CloseButton.Visible = true;

                                view.Border.CloseButton.Visible = true;
                                view.Border.CloseButton.Accept += (s, e) =>
                                                                  {
                                                                      MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
                                                                      e.Cancel = true;
                                                                  };

                                view.Accept += (s, e) => MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
#endif
                            };

        Top.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        Application.Run (editor);
        editor.Dispose ();

        Application.Shutdown ();
    }

    public override void Run () { }

    public class AdornmentEditor : View
    {
        private readonly ColorPicker _backgroundColorPicker = new ()
        {
            Title = "_BG",
            BoxWidth = 1,
            BoxHeight = 1,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        private readonly ColorPicker _foregroundColorPicker = new ()
        {
            Title = "_FG",
            BoxWidth = 1,
            BoxHeight = 1,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        private TextField _bottomEdit;
        private bool _isUpdating;
        private TextField _leftEdit;
        private TextField _rightEdit;
        private Thickness _thickness;
        private TextField _topEdit;

        public AdornmentEditor ()
        {
            Margin.Thickness = new (0);
            BorderStyle = LineStyle.Double;
            Initialized += AdornmentEditor_Initialized;
        }

        public Attribute Color
        {
            get => new (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor);
            set
            {
                _foregroundColorPicker.SelectedColor = value.Foreground.GetClosestNamedColor ();
                _backgroundColorPicker.SelectedColor = value.Background.GetClosestNamedColor ();
            }
        }

        public Thickness Thickness
        {
            get => _thickness;
            set
            {
                if (_isUpdating)
                {
                    return;
                }

                _thickness = value;
                ThicknessChanged?.Invoke (this, new() { Thickness = Thickness });

                if (IsInitialized)
                {
                    _isUpdating = true;

                    if (_topEdit.Text != _thickness.Top.ToString ())
                    {
                        _topEdit.Text = _thickness.Top.ToString ();
                    }

                    if (_leftEdit.Text != _thickness.Left.ToString ())
                    {
                        _leftEdit.Text = _thickness.Left.ToString ();
                    }

                    if (_rightEdit.Text != _thickness.Right.ToString ())
                    {
                        _rightEdit.Text = _thickness.Right.ToString ();
                    }

                    if (_bottomEdit.Text != _thickness.Bottom.ToString ())
                    {
                        _bottomEdit.Text = _thickness.Bottom.ToString ();
                    }

                    _isUpdating = false;
                }
            }
        }

        public event EventHandler<Attribute> AttributeChanged;
        public event EventHandler<ThicknessEventArgs> ThicknessChanged;

        private void AdornmentEditor_Initialized (object sender, EventArgs e)
        {
            var editWidth = 3;

            _topEdit = new() { X = Pos.Center (), Y = 0, Width = editWidth };

            _topEdit.Accept += Edit_Accept;
            Add (_topEdit);

            _leftEdit = new()
            {
                X = Pos.Left (_topEdit) - editWidth, Y = Pos.Bottom (_topEdit), Width = editWidth
            };

            _leftEdit.Accept += Edit_Accept;
            Add (_leftEdit);

            _rightEdit = new() { X = Pos.Right (_topEdit), Y = Pos.Bottom (_topEdit), Width = editWidth };

            _rightEdit.Accept += Edit_Accept;
            Add (_rightEdit);

            _bottomEdit = new() { X = Pos.Center (), Y = Pos.Bottom (_leftEdit), Width = editWidth };

            _bottomEdit.Accept += Edit_Accept;
            Add (_bottomEdit);

            var copyTop = new Button { X = Pos.Center () + 1, Y = Pos.Bottom (_bottomEdit), Text = "Cop_y Top" };

            copyTop.Accept += (s, e) =>
                              {
                                  Thickness = new (Thickness.Top);

                                  if (string.IsNullOrEmpty (_topEdit.Text))
                                  {
                                      _topEdit.Text = "0";
                                  }

                                  _bottomEdit.Text = _leftEdit.Text = _rightEdit.Text = _topEdit.Text;
                              };
            Add (copyTop);

            // Foreground ColorPicker.
            _foregroundColorPicker.X = -1;
            _foregroundColorPicker.Y = Pos.Bottom (copyTop) + 1;
            _foregroundColorPicker.SelectedColor = Color.Foreground.GetClosestNamedColor ();

            _foregroundColorPicker.ColorChanged += (o, a) =>
                                                       AttributeChanged?.Invoke (
                                                                                 this,
                                                                                 new (
                                                                                      _foregroundColorPicker.SelectedColor,
                                                                                      _backgroundColorPicker.SelectedColor
                                                                                     )
                                                                                );
            Add (_foregroundColorPicker);

            // Background ColorPicker.
            _backgroundColorPicker.X = Pos.Right (_foregroundColorPicker) - 1;
            _backgroundColorPicker.Y = Pos.Top (_foregroundColorPicker);
            _backgroundColorPicker.SelectedColor = Color.Background.GetClosestNamedColor ();

            _backgroundColorPicker.ColorChanged += (o, a) =>
                                                       AttributeChanged?.Invoke (
                                                                                 this,
                                                                                 new (
                                                                                      _foregroundColorPicker.SelectedColor,
                                                                                      _backgroundColorPicker.SelectedColor
                                                                                     )
                                                                                );
            Add (_backgroundColorPicker);

            _topEdit.Text = $"{Thickness.Top}";
            _leftEdit.Text = $"{Thickness.Left}";
            _rightEdit.Text = $"{Thickness.Right}";
            _bottomEdit.Text = $"{Thickness.Bottom}";

            LayoutSubviews ();
            Height = GetAdornmentsThickness ().Vertical + 4 + 4;
            Width = GetAdornmentsThickness ().Horizontal + _foregroundColorPicker.Frame.Width * 2 - 3;
        }

        private void Edit_Accept (object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            Thickness = new (
                             int.Parse (_leftEdit.Text),
                             int.Parse (_topEdit.Text),
                             int.Parse (_rightEdit.Text),
                             int.Parse (_bottomEdit.Text));
        }
    }

    public class AdornmentsEditor : Window
    {
        private AdornmentEditor _borderEditor;
        private CheckBox _diagCheckBox;
        private AdornmentEditor _marginEditor;
        private string _origTitle = string.Empty;
        private AdornmentEditor _paddingEditor;
        private View _viewToEdit;

        public View ViewToEdit
        {
            get => _viewToEdit;
            set
            {
                _origTitle = value.Title;
                _viewToEdit = value;

                _marginEditor = new()
                {
                    X = 0,
                    Y = 0,
                    Title = "_Margin",
                    Thickness = _viewToEdit.Margin.Thickness,
                    Color = new (_viewToEdit.Margin.ColorScheme.Normal),
                    SuperViewRendersLineCanvas = true
                };
                _marginEditor.ThicknessChanged += Editor_ThicknessChanged;
                _marginEditor.AttributeChanged += Editor_AttributeChanged;
                Add (_marginEditor);

                _borderEditor = new()
                {
                    X = Pos.Left (_marginEditor),
                    Y = Pos.Bottom (_marginEditor),
                    Title = "B_order",
                    Thickness = _viewToEdit.Border.Thickness,
                    Color = new (_viewToEdit.Border.ColorScheme.Normal),
                    SuperViewRendersLineCanvas = true
                };
                _borderEditor.ThicknessChanged += Editor_ThicknessChanged;
                _borderEditor.AttributeChanged += Editor_AttributeChanged;
                Add (_borderEditor);

                List<LineStyle> borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();

                var rbBorderStyle = new RadioGroup
                {
                    X = Pos.Right (_borderEditor) - 1,
                    Y = Pos.Top (_borderEditor),
                    SelectedItem = (int)_viewToEdit.Border.LineStyle,
                    BorderStyle = LineStyle.Double,
                    Title = "Border St_yle",
                    SuperViewRendersLineCanvas = true,
                    RadioLabels = borderStyleEnum.Select (
                                                          e => e.ToString ()
                                                         )
                                                 .ToArray ()
                };
                Add (rbBorderStyle);

                rbBorderStyle.SelectedItemChanged += (s, e) =>
                                                     {
                                                         LineStyle prevBorderStyle = _viewToEdit.BorderStyle;
                                                         _viewToEdit.Border.LineStyle = (LineStyle)e.SelectedItem;

                                                         if (_viewToEdit.Border.LineStyle == LineStyle.None)
                                                         {
                                                             _viewToEdit.Border.Thickness = new (0);
                                                         }
                                                         else if (prevBorderStyle == LineStyle.None && _viewToEdit.Border.LineStyle != LineStyle.None)
                                                         {
                                                             _viewToEdit.Border.Thickness = new (1);
                                                         }

                                                         _borderEditor.Thickness = new (
                                                                                        _viewToEdit.Border.Thickness.Left,
                                                                                        _viewToEdit.Border.Thickness.Top,
                                                                                        _viewToEdit.Border.Thickness.Right,
                                                                                        _viewToEdit.Border.Thickness.Bottom
                                                                                       );
                                                         _viewToEdit.SetNeedsDisplay ();
                                                         LayoutSubviews ();
                                                     };

                var ckbTitle = new CheckBox
                {
                    BorderStyle = LineStyle.Double,
                    X = Pos.Left (_borderEditor),
                    Y = Pos.Bottom (_borderEditor) - 1,

                    //Width = Dim.Width (_borderEditor),
                    Checked = true,
                    SuperViewRendersLineCanvas = true,
                    Text = "Show Title"
                };

                ckbTitle.Toggled += (sender, args) =>
                                    {
                                        if (ckbTitle.Checked == true)
                                        {
                                            _viewToEdit.Title = _origTitle;
                                        }
                                        else
                                        {
                                            _viewToEdit.Title = string.Empty;
                                        }
                                    };
                Add (ckbTitle);

                _paddingEditor = new()
                {
                    X = Pos.Left (_borderEditor),
                    Y = Pos.Bottom (rbBorderStyle),
                    Title = "_Padding",
                    Thickness = _viewToEdit.Padding.Thickness,
                    Color = new (_viewToEdit.Padding.ColorScheme.Normal),
                    SuperViewRendersLineCanvas = true
                };
                _paddingEditor.ThicknessChanged += Editor_ThicknessChanged;
                _paddingEditor.AttributeChanged += Editor_AttributeChanged;
                Add (_paddingEditor);

                _diagCheckBox = new CheckBox { Text = "_Diagnostics", Y = Pos.Bottom (_paddingEditor) };
                _diagCheckBox.Checked = View.Diagnostics != ViewDiagnosticFlags.Off;

                _diagCheckBox.Toggled += (s, e) =>
                                         {
                                             if (e.NewValue == true)
                                             {
                                                 View.Diagnostics =
                                                     ViewDiagnosticFlags.Padding | ViewDiagnosticFlags.Ruler;
                                             }
                                             else
                                             {
                                                 View.Diagnostics = ViewDiagnosticFlags.Off;
                                             }
                                         };

                Add (_diagCheckBox);
                Add (_viewToEdit);

                _viewToEdit.LayoutComplete += (s, e) =>
                                              {
                                                  if (ckbTitle.Checked == true)
                                                  {
                                                      _viewToEdit.Title = _origTitle;
                                                  }
                                                  else
                                                  {
                                                      _viewToEdit.Title = string.Empty;
                                                  }
                                              };
            }
        }

        private void Editor_AttributeChanged (object sender, Attribute attr)
        {
            switch (sender.ToString ())
            {
                case var s when s == _marginEditor.ToString ():
                    _viewToEdit.Margin.ColorScheme = new (_viewToEdit.Margin.ColorScheme) { Normal = attr };

                    break;
                case var s when s == _borderEditor.ToString ():
                    _viewToEdit.Border.ColorScheme = new (_viewToEdit.Border.ColorScheme) { Normal = attr };

                    break;
                case var s when s == _paddingEditor.ToString ():
                    _viewToEdit.Padding.ColorScheme =
                        new (_viewToEdit.Padding.ColorScheme) { Normal = attr };

                    break;
            }
        }

        private void Editor_ThicknessChanged (object sender, ThicknessEventArgs e)
        {
            try
            {
                switch (sender.ToString ())
                {
                    case var s when s == _marginEditor.ToString ():
                        _viewToEdit.Margin.Thickness = e.Thickness;

                        break;
                    case var s when s == _borderEditor.ToString ():
                        _viewToEdit.Border.Thickness = e.Thickness;

                        break;
                    case var s when s == _paddingEditor.ToString ():
                        _viewToEdit.Padding.Thickness = e.Thickness;

                        break;
                }
            }
            catch
            {
                switch (sender.ToString ())
                {
                    case var s when s == _marginEditor.ToString ():
                        _viewToEdit.Margin.Thickness = e.PreviousThickness;

                        break;
                    case var s when s == _borderEditor.ToString ():
                        _viewToEdit.Border.Thickness = e.PreviousThickness;

                        break;
                    case var s when s == _paddingEditor.ToString ():
                        _viewToEdit.Padding.Thickness = e.PreviousThickness;

                        break;
                }
            }
        }
    }
}
