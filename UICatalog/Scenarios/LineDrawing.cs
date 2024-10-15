using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

public interface ITool
{
    void OnMouseEvent (DrawingArea area, MouseEventArgs mouseEvent);
}

internal class DrawLineTool : ITool
{
    private StraightLine _currentLine;
    public LineStyle LineStyle { get; set; } = LineStyle.Single;

    /// <inheritdoc/>
    public void OnMouseEvent (DrawingArea area, MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            if (_currentLine == null)
            {
                // Mouse pressed down
                _currentLine = new (
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
                Point end = mouseEvent.Position;
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

                    area.CurrentLayer = new (
                                             area.CurrentLayer.Lines.Exclude (
                                                                              _currentLine.Start,
                                                                              _currentLine.Length,
                                                                              _currentLine.Orientation
                                                                             )
                                            );

                    area.Layers.Insert (idx, area.CurrentLayer);
                }

                _currentLine = null;
                area.ClearUndo ();
                area.SetNeedsDisplay ();
            }
        }

        mouseEvent.Handled = true;
    }
}

[ScenarioMetadata ("Line Drawing", "Demonstrates LineCanvas.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
public class LineDrawing : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = GetQuitKeyAndName () };
        var canvas = new DrawingArea { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        var tools = new ToolsView { Title = "Tools", X = Pos.Right (canvas) - 20, Y = 2 };

        tools.ColorChanged += (s, e) => canvas.SetAttribute (e);
        tools.SetStyle += b => canvas.CurrentTool = new DrawLineTool { LineStyle = b };
        tools.AddLayer += () => canvas.AddLayer ();

        win.Add (canvas);
        win.Add (tools);
        tools.CurrentColor = canvas.GetNormalColor ();
        canvas.CurrentAttribute = tools.CurrentColor;

        win.KeyDown += (s, e) => { e.Handled = canvas.NewKeyDownEvent (e); };

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }

    public static bool PromptForColor (string title, Color current, out Color newColor)
    {
        var accept = false;

        var d = new Dialog
        {
            Title = title,
            Width = Application.Force16Colors ? 35 : Dim.Auto (DimAutoStyle.Auto, Dim.Percent (80), Dim.Percent (90)),
            Height = 10
        };

        var btnOk = new Button
        {
            X = Pos.Center () - 5,
            Y = Application.Force16Colors ? 6 : 4,
            Text = "Ok",
            Width = Dim.Auto (),
            IsDefault = true
        };

        btnOk.Accepting += (s, e) =>
                        {
                            accept = true;
                            e.Cancel = true;
                            Application.RequestStop ();
                        };

        var btnCancel = new Button
        {
            X = Pos.Center () + 5,
            Y = 4,
            Text = "Cancel",
            Width = Dim.Auto ()
        };

        btnCancel.Accepting += (s, e) =>
                            {
                                e.Cancel = true;
                                Application.RequestStop ();
                            };

        d.Add (btnOk);
        d.Add (btnCancel);

        d.AddButton (btnOk);
        d.AddButton (btnCancel);

        View cp;
        if (Application.Force16Colors)
        {
            cp = new ColorPicker16
            {
                SelectedColor = current.GetClosestNamedColor16 (),
                Width = Dim.Fill ()
            };
        }
        else
        {
            cp = new ColorPicker
            {
                SelectedColor = current,
                Width = Dim.Fill (),
                Style = new () { ShowColorName = true, ShowTextFields = true }
            };
            ((ColorPicker)cp).ApplyStyleChanges ();
        }

        d.Add (cp);

        Application.Run (d);
        d.Dispose ();
        newColor = Application.Force16Colors ? ((ColorPicker16)cp).SelectedColor : ((ColorPicker)cp).SelectedColor;

        return accept;
    }
}

public class ToolsView : Window
{
    private Button _addLayerBtn;
    private readonly AttributeView _colors;
    private RadioGroup _stylePicker;

    public Attribute CurrentColor
    {
        get => _colors.Value;
        set => _colors.Value = value;
    }

    public ToolsView ()
    {
        BorderStyle = LineStyle.Dotted;
        Border.Thickness = new (1, 2, 1, 1);
        Initialized += ToolsView_Initialized;
        _colors = new ();
    }

    public event Action AddLayer;

    public override void BeginInit ()
    {
        base.BeginInit ();

        _colors.ValueChanged += (s, e) => ColorChanged?.Invoke (this, e);

        _stylePicker = new()
        {
            X = 0, Y = Pos.Bottom (_colors), RadioLabels = Enum.GetNames (typeof (LineStyle)).ToArray ()
        };
        _stylePicker.SelectedItemChanged += (s, a) => { SetStyle?.Invoke ((LineStyle)a.SelectedItem); };
        _stylePicker.SelectedItem = 1;

        _addLayerBtn = new() { Text = "New Layer", X = Pos.Center (), Y = Pos.Bottom (_stylePicker) };

        _addLayerBtn.Accepting += (s, a) => AddLayer?.Invoke ();
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
    public DrawingArea () { AddLayer (); }

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
    protected override bool OnKeyDown (Key e)
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

    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        CurrentTool.OnMouseEvent (this, mouseEvent);

        return mouseEvent.Handled;
    }

    internal void AddLayer ()
    {
        CurrentLayer = new ();
        Layers.Add (CurrentLayer);
    }

    internal void SetAttribute (Attribute a) { CurrentAttribute = a; }

    public void ClearUndo () { _undoHistory.Clear (); }
}

public class AttributeView : View
{
    public event EventHandler<Attribute> ValueChanged;
    private Attribute _value;

    public Attribute Value
    {
        get => _value;
        set
        {
            _value = value;
            ValueChanged?.Invoke (this, value);
        }
    }

    private static readonly HashSet<(int, int)> ForegroundPoints = new()
    {
        (0, 0), (1, 0), (2, 0),
        (0, 1), (1, 1), (2, 1)
    };

    private static readonly HashSet<(int, int)> BackgroundPoints = new()
    {
        (3, 1),
        (1, 2), (2, 2), (3, 2)
    };

    public AttributeView ()
    {
        Width = 4;
        Height = 3;
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        Color fg = Value.Foreground;
        Color bg = Value.Background;

        bool isTransparentFg = fg == GetNormalColor ().Background;
        bool isTransparentBg = bg == GetNormalColor ().Background;

        Driver.SetAttribute (new (fg, isTransparentFg ? Color.Gray : fg));

        // Square of foreground color
        foreach ((int, int) point in ForegroundPoints)
        {
            // Make pattern like this when it is same color as background of control
            /*▓▒
              ▒▓*/
            Rune rune;

            if (isTransparentFg)
            {
                rune = (Rune)(point.Item1 % 2 == point.Item2 % 2 ? '▓' : '▒');
            }
            else
            {
                rune = (Rune)'█';
            }

            AddRune (point.Item1, point.Item2, rune);
        }

        Driver.SetAttribute (new (bg, isTransparentBg ? Color.Gray : bg));

        // Square of background color
        foreach ((int, int) point in BackgroundPoints)
        {
            // Make pattern like this when it is same color as background of control
            /*▓▒
              ▒▓*/
            Rune rune;

            if (isTransparentBg)
            {
                rune = (Rune)(point.Item1 % 2 == point.Item2 % 2 ? '▓' : '▒');
            }
            else
            {
                rune = (Rune)'█';
            }

            AddRune (point.Item1, point.Item2, rune);
        }
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            if (IsForegroundPoint (mouseEvent.Position.X, mouseEvent.Position.Y))
            {
                ClickedInForeground ();
            }
            else if (IsBackgroundPoint (mouseEvent.Position.X, mouseEvent.Position.Y))
            {
                ClickedInBackground ();
            }

            mouseEvent.Handled = true;
        }

        return mouseEvent.Handled;
    }

    private bool IsForegroundPoint (int x, int y) { return ForegroundPoints.Contains ((x, y)); }

    private bool IsBackgroundPoint (int x, int y) { return BackgroundPoints.Contains ((x, y)); }

    private void ClickedInBackground ()
    {
        if (LineDrawing.PromptForColor ("Background", Value.Background, out Color newColor))
        {
            Value = new (Value.Foreground, newColor);
            SetNeedsDisplay ();
        }
    }

    private void ClickedInForeground ()
    {
        if (LineDrawing.PromptForColor ("Foreground", Value.Foreground, out Color newColor))
        {
            Value = new (newColor, Value.Background);
            SetNeedsDisplay ();
        }
    }
}
