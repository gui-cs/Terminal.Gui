using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Line Drawing", "Demonstrates LineCanvas.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
public class LineDrawing : Scenario
{
    public override void Setup ()
    {
        var canvas = new DrawingArea { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        var tools = new ToolsView { Title = "Tools", X = Pos.Right (canvas) - 20, Y = 2 };

        tools.ColorChanged += c => canvas.SetColor (c);
        tools.SetStyle += b => canvas.LineStyle = b;
        tools.AddLayer += () => canvas.AddLayer ();

        Win.Add (canvas);
        Win.Add (tools);

        Win.KeyDown += (s, e) => { e.Handled = canvas.OnKeyDown (e); };
    }

    private class DrawingArea : View
    {
        public DrawingArea () { AddLayer (); }
        private readonly List<LineCanvas> _layers = new ();
        private readonly Stack<StraightLine> _undoHistory = new ();
        private Color _currentColor = new (Color.White);
        private LineCanvas _currentLayer;
        private StraightLine _currentLine;
        public LineStyle LineStyle { get; set; }

        public override void OnDrawContentComplete (Rect contentArea)
        {
            base.OnDrawContentComplete (contentArea);

            foreach (LineCanvas canvas in _layers)
            {
                foreach (KeyValuePair<Point, Cell> c in canvas.GetCellMap ())
                {
                    Driver.SetAttribute (c.Value.Attribute ?? ColorScheme.Normal);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    AddRune (c.Key.X, c.Key.Y, c.Value.Rune);
                }
            }
        }

        //// BUGBUG: Why is this not handled by a key binding???
        public override bool OnKeyDown (Key e)
        {
            // BUGBUG: These should be implemented with key bindings
            if (e.KeyCode == (KeyCode.Z | KeyCode.CtrlMask))
            {
                StraightLine pop = _currentLayer.RemoveLastLine ();

                if (pop != null)
                {
                    _undoHistory.Push (pop);
                    SetNeedsDisplay ();

                    return true;
                }
            }

            if (e.KeyCode == (KeyCode.Y | KeyCode.CtrlMask))
            {
                if (_undoHistory.Any ())
                {
                    StraightLine pop = _undoHistory.Pop ();
                    _currentLayer.AddLine (pop);
                    SetNeedsDisplay ();

                    return true;
                }
            }

            return false;
        }

        public override bool OnMouseEvent (MouseEvent mouseEvent)
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
            {
                if (_currentLine == null)
                {
                    // Mouse pressed down
                    _currentLine = new StraightLine (
                                                     new Point (mouseEvent.X, mouseEvent.Y),
                                                     0,
                                                     Orientation.Vertical,
                                                     LineStyle,
                                                     new Attribute (_currentColor, GetNormalColor ().Background)
                                                    );

                    _currentLayer.AddLine (_currentLine);
                }
                else
                {
                    // Mouse dragged
                    Point start = _currentLine.Start;
                    var end = new Point (mouseEvent.X, mouseEvent.Y);
                    var orientation = Orientation.Vertical;
                    int length = end.Y - start.Y;

                    // if line is wider than it is tall switch to horizontal
                    if (Math.Abs (start.X - end.X) > Math.Abs (start.Y - end.Y))
                    {
                        orientation = Orientation.Horizontal;
                        length = end.X - start.X;
                    }

                    if (length > 0)
                    {
                        length++;
                    }
                    else
                    {
                        length--;
                    }

                    _currentLine.Length = length;
                    _currentLine.Orientation = orientation;
                    _currentLayer.ClearCache ();
                    SetNeedsDisplay ();
                }
            }
            else
            {
                // Mouse released
                if (_currentLine != null)
                {
                    if (_currentLine.Length == 0)
                    {
                        _currentLine.Length = 1;
                    }

                    if (_currentLine.Style == LineStyle.None)
                    {
                        // Treat none as eraser
                        int idx = _layers.IndexOf (_currentLayer);
                        _layers.Remove (_currentLayer);

                        _currentLayer = new LineCanvas (
                                                        _currentLayer.Lines.Exclude (
                                                                                     _currentLine.Start,
                                                                                     _currentLine.Length,
                                                                                     _currentLine.Orientation
                                                                                    )
                                                       );

                        _layers.Insert (idx, _currentLayer);
                    }

                    _currentLine = null;
                    _undoHistory.Clear ();
                    SetNeedsDisplay ();
                }
            }

            return base.OnMouseEvent (mouseEvent);
        }

        internal void AddLayer ()
        {
            _currentLayer = new LineCanvas ();
            _layers.Add (_currentLayer);
        }

        internal void SetColor (Color c) { _currentColor = c; }
    }

    private class ToolsView : Window
    {
        public ToolsView ()
        {
            BorderStyle = LineStyle.Dotted;
            Border.Thickness = new Thickness (1, 2, 1, 1);
            Initialized += ToolsView_Initialized;
        }

        private Button _addLayerBtn;
        private ColorPicker _colorPicker;
        private RadioGroup _stylePicker;
        public event Action AddLayer;

        public override void BeginInit ()
        {
            base.BeginInit ();

            _colorPicker = new ColorPicker { X = 0, Y = 0, BoxHeight = 1, BoxWidth = 2 };

            _colorPicker.ColorChanged += (s, a) => ColorChanged?.Invoke (a.Color);

            _stylePicker = new RadioGroup
            {
                X = 0, Y = Pos.Bottom (_colorPicker), RadioLabels = Enum.GetNames (typeof (LineStyle)).ToArray ()
            };
            _stylePicker.SelectedItemChanged += (s, a) => { SetStyle?.Invoke ((LineStyle)a.SelectedItem); };
            _stylePicker.SelectedItem = 1;

            _addLayerBtn = new Button { Text = "New Layer", X = Pos.Center (), Y = Pos.Bottom (_stylePicker) };

            _addLayerBtn.Clicked += (s, a) => AddLayer?.Invoke ();
            Add (_colorPicker, _stylePicker, _addLayerBtn);
        }

        public event Action<Color> ColorChanged;
        public event Action<LineStyle> SetStyle;

        private void ToolsView_Initialized (object sender, EventArgs e)
        {
            LayoutSubviews ();

            Width = Math.Max (_colorPicker.Frame.Width, _stylePicker.Frame.Width) + GetAdornmentsThickness ().Horizontal;

            Height = _colorPicker.Frame.Height + _stylePicker.Frame.Height + _addLayerBtn.Frame.Height + GetAdornmentsThickness ().Vertical;
            SuperView.LayoutSubviews ();
        }
    }
}
