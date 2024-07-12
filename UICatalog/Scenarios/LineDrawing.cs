using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using static Terminal.Gui.SpinnerStyle;

namespace UICatalog.Scenarios;

public interface ITool
{
    void OnMouseEvent (DrawingArea area, MouseEvent mouseEvent);
}

class DrawLineTool : ITool
{
    private StraightLine _currentLine;
    public LineStyle LineStyle { get; set; } = LineStyle.Single;

    /// <inheritdoc />
    public void OnMouseEvent (DrawingArea area, MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            if (_currentLine == null)
            {
                // Mouse pressed down
                _currentLine = new StraightLine (
                                                 mouseEvent.Position,
                                                 0,
                                                 Orientation.Vertical,
                                                 LineStyle,
                                                 area.CurrentAttribute
                                                );

                area.CurrentLayer.AddLine (_currentLine);
            }
            else
            {
                // Mouse dragged
                Point start = _currentLine.Start;
                var end = mouseEvent.Position;
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
                area.CurrentLayer.ClearCache ();
                area.SetNeedsDisplay ();
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
                    int idx = area.Layers.IndexOf (area.CurrentLayer);
                    area.Layers.Remove (area.CurrentLayer);

                    area.CurrentLayer = new LineCanvas (
                                                        area.CurrentLayer.Lines.Exclude (
                                                                                         _currentLine.Start,
                                                                                         _currentLine.Length,
                                                                                         _currentLine.Orientation
                                                                                        )
                                                       );

                    area.Layers.Insert (idx, area.CurrentLayer);
                }

                _currentLine = null;
                area.ClearUndo();
                area.SetNeedsDisplay ();
            }
        }
    }
}

[ScenarioMetadata ("Line Drawing", "Demonstrates LineCanvas.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
public class LineDrawing : Scenario
{

    public override void Setup ()
    {
        var canvas = new DrawingArea { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        var tools = new ToolsView { Title = "Tools", X = Pos.Right (canvas) - 20, Y = 2 };
        tools.CurrentColor = canvas.GetNormalColor ();

        tools.ColorChanged += (s,e) => canvas.SetAttribute (e);
        tools.SetStyle += b => canvas.CurrentTool = new DrawLineTool (){LineStyle = b};
        tools.AddLayer += () => canvas.AddLayer ();

        Win.Add (canvas);
        Win.Add (tools);

        Win.KeyDown += (s, e) => { e.Handled = canvas.OnKeyDown (e); };
    }
}
public class ToolsView : Window
{
    private Button _addLayerBtn;
    private AttributeView _colors;
    private RadioGroup _stylePicker;


    public Attribute CurrentColor
    {
        get => _colors.Value;
        set => _colors.Value = value;
    }

    public ToolsView ()
    {
        BorderStyle = LineStyle.Dotted;
        Border.Thickness = new Thickness (1, 2, 1, 1);
        Initialized += ToolsView_Initialized;
        _colors = new AttributeView ()
        {
        };
    }

    public event Action AddLayer;

    public override void BeginInit ()
    {
        base.BeginInit ();

        _colors.ValueChanged += (s, e) => ColorChanged?.Invoke (this, e);

        _stylePicker = new RadioGroup
        {
            X = 0, Y = Pos.Bottom (_colors), RadioLabels = Enum.GetNames (typeof (LineStyle)).ToArray ()
        };
        _stylePicker.SelectedItemChanged += (s, a) => { SetStyle?.Invoke ((LineStyle)a.SelectedItem); };
        _stylePicker.SelectedItem = 1;

        _addLayerBtn = new Button { Text = "New Layer", X = Pos.Center (), Y = Pos.Bottom (_stylePicker) };

        _addLayerBtn.Accept += (s, a) => AddLayer?.Invoke ();
        Add (_colors, _stylePicker, _addLayerBtn);
    }

    public event EventHandler<Attribute> ColorChanged;
    public event Action<LineStyle> SetStyle;

    private void ToolsView_Initialized (object sender, EventArgs e)
    {
        LayoutSubviews ();

        Width = Math.Max (_colors.Frame.Width, _stylePicker.Frame.Width) + GetAdornmentsThickness ().Horizontal;

        Height = _colors.Frame.Height + _stylePicker.Frame.Height + _addLayerBtn.Frame.Height + GetAdornmentsThickness ().Vertical;
        SuperView.LayoutSubviews ();
    }
}

public class DrawingArea : View
{
    public readonly List<LineCanvas> Layers = new ();
    private readonly Stack<StraightLine> _undoHistory = new ();
    public Attribute CurrentAttribute { get; set; }
    public LineCanvas CurrentLayer { get; set; }

    public ITool CurrentTool { get; set; } = new DrawLineTool ();
    public DrawingArea ()
    {
        CurrentAttribute = GetNormalColor ();
        AddLayer ();
    }

    public override void OnDrawContentComplete (Rectangle viewport)
    {
        base.OnDrawContentComplete (viewport);

        foreach (LineCanvas canvas in Layers)
        {
            foreach (KeyValuePair<Point, Cell?> c in canvas.GetCellMap ())
            {
                if (c.Value is { })
                {
                    Driver.SetAttribute (c.Value.Value.Attribute ?? ColorScheme.Normal);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    AddRune (c.Key.X, c.Key.Y, c.Value.Value.Rune);
                }
            }
        }
    }

    //// BUGBUG: Why is this not handled by a key binding???
    public override bool OnKeyDown (Key e)
    {
        // BUGBUG: These should be implemented with key bindings
        if (e.KeyCode == (KeyCode.Z | KeyCode.CtrlMask))
        {
            StraightLine pop = CurrentLayer.RemoveLastLine ();

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
                CurrentLayer.AddLine (pop);
                SetNeedsDisplay ();

                return true;
            }
        }

        return false;
    }

    protected override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        CurrentTool.OnMouseEvent (this, mouseEvent);

        return base.OnMouseEvent (mouseEvent);
    }

    internal void AddLayer ()
    {
        CurrentLayer = new LineCanvas ();
        Layers.Add (CurrentLayer);
    }

    internal void SetAttribute (Attribute a) { CurrentAttribute = a; }

    public void ClearUndo ()
    {
        _undoHistory.Clear ();
    }
}
