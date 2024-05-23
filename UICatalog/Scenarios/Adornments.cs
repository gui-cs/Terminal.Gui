using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornments Demo", "Demonstrates Margin, Border, and Padding on Views.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Borders")]
public class Adornments : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public override void Main ()
    {
        Application.Init ();

        _diagnosticFlags = View.Diagnostics;

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };

        var editor = new AdornmentsEditor ();
        app.Add (editor);

        var window = new Window
        {
            Title = "The _Window",
            Arrangement = ViewArrangement.Movable,
            X = Pos.Right (editor),
            Width = Dim.Percent (60),
            Height = Dim.Percent (80)
        };
        app.Add (window);

        var tf1 = new TextField { Width = 10, Text = "TextField" };
        var color = new ColorPicker { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd () };
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
                             MessageBox.Query (20, 7, "Hi", $"Am I a {window.GetType ().Name}?", "Yes", "No");

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

        var btnButtonInWindow = new Button { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Button" };

        var labelAnchorEnd = new Label
        {
            Y = Pos.AnchorEnd (),
            Width = 40,
            Height = Dim.Percent (20),
            Text = "Label\nY=AnchorEnd(),Height=Dim.Percent(10)",
            ColorScheme = Colors.ColorSchemes ["Error"]
        };

        window.Margin.Data = "Margin";
        window.Margin.Thickness = new (3);

        window.Border.Data = "Border";
        window.Border.Thickness = new (3);

        window.Padding.Data = "Padding";
        window.Padding.Thickness = new (3);

        var longLabel = new Label
        {
            X = 40, Y = 5, Title = "This is long text (in a label) that should clip."
        };
        longLabel.TextFormatter.WordWrap = true;
        window.Add (tf1, color, button, label, btnButtonInWindow, labelAnchorEnd, longLabel);

        editor.Initialized += (s, e) => { editor.ViewToEdit = window; };

        window.Initialized += (s, e) =>
                              {
                                  var labelInPadding = new Label { X = 1, Y = 0, Title = "_Text:" };
                                  window.Padding.Add (labelInPadding);

                                  var textFieldInPadding = new TextField
                                  { X = Pos.Right (labelInPadding) + 1, Y = Pos.Top (labelInPadding), Width = 15, Text = "some text" };
                                  textFieldInPadding.Accept += (s, e) => MessageBox.Query (20, 7, "TextField", textFieldInPadding.Text, "Ok");
                                  window.Padding.Add (textFieldInPadding);

                                  var btnButtonInPadding = new Button { X = Pos.Center (), Y = 0, Text = "_Button in Padding" };
                                  btnButtonInPadding.Accept += (s, e) => MessageBox.Query (20, 7, "Hi", "Button in Padding Pressed!", "Ok");
                                  btnButtonInPadding.BorderStyle = LineStyle.Dashed;
                                  btnButtonInPadding.Border.Thickness = new (1, 1, 1, 1);
                                  window.Padding.Add (btnButtonInPadding);

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

        app.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();
    }

    /// <summary>
    ///     Provides a composable UI for editing the settings of an Adornment.
    /// </summary>
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

        private Buttons.NumericUpDown<int> _topEdit;
        private Buttons.NumericUpDown<int> _leftEdit;
        private Buttons.NumericUpDown<int> _bottomEdit;
        private Buttons.NumericUpDown<int> _rightEdit;
        private Thickness _thickness;
        private bool _isUpdating;

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
                ThicknessChanged?.Invoke (this, new () { Thickness = Thickness });

                if (IsInitialized)
                {
                    _isUpdating = true;
                    _topEdit.Value = _thickness.Top;
                    _leftEdit.Value = _thickness.Left;
                    _rightEdit.Value = _thickness.Right;
                    _bottomEdit.Value = _thickness.Bottom;

                    _isUpdating = false;
                }
            }
        }

        public event EventHandler<Attribute> AttributeChanged;
        public event EventHandler<ThicknessEventArgs> ThicknessChanged;

        private void AdornmentEditor_Initialized (object sender, EventArgs e)
        {
            SuperViewRendersLineCanvas = true;

            _topEdit = new ()
            {
                X = Pos.Center (), Y = 0
            };

            _topEdit.ValueChanging += Top_ValueChanging;
            Add (_topEdit);

            _leftEdit = new ()
            {
                X = Pos.Left (_topEdit) - Pos.Func (() => _topEdit.Digits) - 2, Y = Pos.Bottom (_topEdit)
            };

            _leftEdit.ValueChanging += Left_ValueChanging;
            Add (_leftEdit);

            _rightEdit = new () { X = Pos.Right (_leftEdit) + 5, Y = Pos.Bottom (_topEdit) };

            _rightEdit.ValueChanging += Right_ValueChanging;
            Add (_rightEdit);

            _bottomEdit = new () { X = Pos.Center (), Y = Pos.Bottom (_leftEdit) };

            _bottomEdit.ValueChanging += Bottom_ValueChanging;
            Add (_bottomEdit);

            var copyTop = new Button { X = Pos.Center (), Y = Pos.Bottom (_bottomEdit), Text = "Cop_y Top" };

            copyTop.Accept += (s, e) =>
                              {
                                  Thickness = new (Thickness.Top);
                                  _leftEdit.Value = _rightEdit.Value = _bottomEdit.Value = _topEdit.Value;
                              };
            Add (copyTop);

            // Foreground ColorPicker.
            _foregroundColorPicker.X = -1;
            _foregroundColorPicker.Y = Pos.Bottom (copyTop);
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

            _topEdit.Value = Thickness.Top;
            _leftEdit.Value = Thickness.Left;
            _rightEdit.Value = Thickness.Right;
            _bottomEdit.Value = Thickness.Bottom;

            Width = Dim.Auto () - 1;
            Height = Dim.Auto () - 1;
            LayoutSubviews ();
        }

        private void Top_ValueChanging (object sender, StateEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }

            Thickness.Top = e.NewValue;
        }

        private void Left_ValueChanging (object sender, StateEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }

            Thickness.Left = e.NewValue;
        }

        private void Right_ValueChanging (object sender, StateEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }

            Thickness.Right = e.NewValue;
        }

        private void Bottom_ValueChanging (object sender, StateEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }

            Thickness.Bottom = e.NewValue;
        }
    }

    /// <summary>
    ///     Provides an editor UI for the Margin, Border, and Padding of a View.
    /// </summary>
    public class AdornmentsEditor : View
    {
        private AdornmentEditor _borderEditor;
        private CheckBox _diagCheckBox;
        private AdornmentEditor _marginEditor;
        private string _origTitle = string.Empty;
        private AdornmentEditor _paddingEditor;
        private View _viewToEdit;

        public AdornmentsEditor ()
        {
            ColorScheme = Colors.ColorSchemes ["Dialog"];

            // TOOD: Use Dim.Auto
            Width = 36;
            Height = Dim.Fill ();
        }

        public View ViewToEdit
        {
            get => _viewToEdit;
            set
            {
                _origTitle = value.Title;
                _viewToEdit = value;

                _marginEditor = new ()
                {
                    X = 0,
                    Y = 0,
                    Title = "_Margin",
                    Thickness = _viewToEdit.Margin.Thickness,
                    Color = new (_viewToEdit.Margin.ColorScheme?.Normal ?? ColorScheme.Normal),
                    SuperViewRendersLineCanvas = true
                };
                _marginEditor.ThicknessChanged += Editor_ThicknessChanged;
                _marginEditor.AttributeChanged += Editor_AttributeChanged;
                Add (_marginEditor);

                _borderEditor = new ()
                {
                    X = Pos.Left (_marginEditor),
                    Y = Pos.Bottom (_marginEditor),
                    Title = "B_order",
                    Thickness = _viewToEdit.Border.Thickness,
                    Color = new (_viewToEdit.Border.ColorScheme?.Normal ?? ColorScheme.Normal),
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
                                            //_viewToEdit.Title = _origTitle;
                                        }
                                        else
                                        {
                                            _viewToEdit.Title = string.Empty;
                                        }
                                    };
                Add (ckbTitle);

                _paddingEditor = new ()
                {
                    X = Pos.Left (_borderEditor),
                    Y = Pos.Bottom (rbBorderStyle),
                    Title = "_Padding",
                    Thickness = _viewToEdit.Padding.Thickness,
                    Color = new (_viewToEdit.Padding.ColorScheme?.Normal ?? ColorScheme.Normal),
                    SuperViewRendersLineCanvas = true
                };
                _paddingEditor.ThicknessChanged += Editor_ThicknessChanged;
                _paddingEditor.AttributeChanged += Editor_AttributeChanged;
                Add (_paddingEditor);

                _diagCheckBox = new () { Text = "_Diagnostics", Y = Pos.Bottom (_paddingEditor) };
                _diagCheckBox.Checked = Diagnostics != ViewDiagnosticFlags.Off;

                _diagCheckBox.Toggled += (s, e) =>
                                         {
                                             if (e.NewValue == true)
                                             {
                                                 Diagnostics =
                                                     ViewDiagnosticFlags.Padding | ViewDiagnosticFlags.Ruler;
                                             }
                                             else
                                             {
                                                 Diagnostics = ViewDiagnosticFlags.Off;
                                             }
                                         };

                Add (_diagCheckBox);

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
