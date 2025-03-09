#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using UICatalog;
using UICatalog.Scenarios;

/// <summary>
///     Demonstrates creating and drawing regions through mouse dragging.
/// </summary>
[ScenarioMetadata ("Regions", "Region Tester")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Drawing")]
public class RegionScenario : Scenario
{
    private readonly Region _region = new ();
    private Point? _dragStart;
    private bool _isDragging;

    private readonly Rune? _previewFillRune = Glyphs.Stipple;

    private RegionDrawStyles _drawStyle;
    private RegionOp _regionOp;

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            TabStop = TabBehavior.TabGroup
        };
        app.Padding!.Thickness = new (1);

        var tools = new ToolsView { Title = "Tools", X = Pos.AnchorEnd (), Y = 2 };

        tools.CurrentAttribute = app.ColorScheme!.HotNormal;

        tools.SetStyle += b =>
                          {
                              _drawStyle = (RegionDrawStyles)b;
                              app.SetNeedsDraw ();
                          };

        tools.RegionOpChanged += (s, e) => { _regionOp = e; };

        //tools.AddLayer += () => canvas.AddLayer ();

        app.Add (tools);

        // Add drag handling to window
        app.MouseEvent += (s, e) =>
                          {
                              if (e.Flags.HasFlag (MouseFlags.Button1Pressed))
                              {
                                  if (!e.Flags.HasFlag (MouseFlags.ReportMousePosition))
                                  { // Start drag
                                      _dragStart = e.ScreenPosition;
                                      _isDragging = true;
                                  }
                                  else
                                  {
                                      // Drag
                                      if (_isDragging && _dragStart.HasValue)
                                      {
                                          app.SetNeedsDraw ();
                                      }
                                  }
                              }

                              if (e.Flags.HasFlag (MouseFlags.Button1Released))
                              {
                                  if (_isDragging && _dragStart.HasValue)
                                  {
                                      // Add the new region
                                      AddRectangleFromPoints (_dragStart.Value, e.ScreenPosition, _regionOp);
                                      _isDragging = false;
                                      _dragStart = null;
                                  }

                                  app.SetNeedsDraw ();
                              }
                          };

        // Draw the regions
        app.DrawingContent += (s, e) =>
                              {
                                  // Draw all regions with single line style
                                  //_region.FillRectangles (_attribute.Value, _fillRune);
                                  switch (_drawStyle)
                                  {
                                      case RegionDrawStyles.FillOnly:
                                          _region.FillRectangles (tools.CurrentAttribute!.Value, _previewFillRune);

                                          break;

                                      case RegionDrawStyles.InnerBoundaries:
                                          _region.DrawBoundaries (app.LineCanvas, LineStyle.Single, tools.CurrentAttribute);
                                          _region.FillRectangles (tools.CurrentAttribute!.Value, (Rune)' ');

                                          break;

                                      case RegionDrawStyles.OuterBoundary:
                                          _region.DrawOuterBoundary (app.LineCanvas, LineStyle.Single, tools.CurrentAttribute);
                                          _region.FillRectangles (tools.CurrentAttribute!.Value, (Rune)' ');

                                          break;
                                  }

                                  // If currently dragging, draw preview rectangle
                                  if (_isDragging && _dragStart.HasValue)
                                  {
                                      Point currentMousePos = Application.GetLastMousePosition ()!.Value;
                                      Rectangle previewRect = GetRectFromPoints (_dragStart.Value, currentMousePos);
                                      var previewRegion = new Region (previewRect);

                                      previewRegion.FillRectangles (tools.CurrentAttribute!.Value, (Rune)' ');

                                      previewRegion.DrawBoundaries (
                                                                    app.LineCanvas,
                                                                    LineStyle.Dashed,
                                                                    new (
                                                                         tools.CurrentAttribute!.Value.Foreground.GetHighlightColor (),
                                                                         tools.CurrentAttribute!.Value.Background));
                                  }
                              };

        Application.Run (app);

        // Clean up
        app.Dispose ();
        Application.Shutdown ();
    }

    private void AddRectangleFromPoints (Point start, Point end, RegionOp op)
    {
        Rectangle rect = GetRectFromPoints (start, end);
        var region = new Region (rect);
        _region.Combine (region, op); // Or RegionOp.MinimalUnion if you want minimal rectangles
    }

    private Rectangle GetRectFromPoints (Point start, Point end)
    {
        int left = Math.Min (start.X, end.X);
        int top = Math.Min (start.Y, end.Y);
        int right = Math.Max (start.X, end.X);
        int bottom = Math.Max (start.Y, end.Y);

        // Ensure minimum width and height of 1
        int width = Math.Max (1, right - left + 1);
        int height = Math.Max (1, bottom - top + 1);

        return new (left, top, width, height);
    }
}

public enum RegionDrawStyles
{
    FillOnly = 0,

    InnerBoundaries = 1,

    OuterBoundary = 2
}

public class ToolsView : Window
{
    //private Button _addLayerBtn;
    private readonly AttributeView _attributeView = new ();
    private RadioGroup? _stylePicker;
    private RegionOpSelector? _regionOpSelector;

    public Attribute? CurrentAttribute
    {
        get => _attributeView.Value;
        set => _attributeView.Value = value;
    }

    public ToolsView ()
    {
        BorderStyle = LineStyle.Dotted;
        Border!.Thickness = new (1, 2, 1, 1);
        Height = Dim.Auto ();
        Width = Dim.Auto ();
    }

    //public event Action AddLayer;

    public override void BeginInit ()
    {
        base.BeginInit ();

        _attributeView.ValueChanged += (s, e) => AttributeChanged?.Invoke (this, e);

        _stylePicker = new ()
        {
            Width = Dim.Fill (),
            X = 0, Y = Pos.Bottom (_attributeView) + 1, RadioLabels = Enum.GetNames<RegionDrawStyles> ().Select (n => n = "_" + n).ToArray ()
        };
        _stylePicker.BorderStyle = LineStyle.Single;
        _stylePicker.Border!.Thickness = new (0, 1, 0, 0);
        _stylePicker.Title = "Draw Style";

        _stylePicker.SelectedItemChanged += (s, a) => { SetStyle?.Invoke ((LineStyle)a.SelectedItem); };
        _stylePicker.SelectedItem = (int)RegionDrawStyles.FillOnly;

        _regionOpSelector = new ()
        {
            X = 0,
            Y = Pos.Bottom (_stylePicker) + 1
        };
        _regionOpSelector.SelectedItemChanged += (s, a) => { RegionOpChanged?.Invoke (this, a); };
        _regionOpSelector.SelectedItem = RegionOp.MinimalUnion;

        //_addLayerBtn = new () { Text = "New Layer", X = Pos.Center (), Y = Pos.Bottom (_stylePicker) };

        //_addLayerBtn.Accepting += (s, a) => AddLayer?.Invoke ();
        Add (_attributeView, _stylePicker, _regionOpSelector); //, _addLayerBtn);
    }

    public event EventHandler<Attribute?>? AttributeChanged;
    public event EventHandler<RegionOp>? RegionOpChanged;
    public event Action<LineStyle>? SetStyle;
}

public class RegionOpSelector : View
{
    private readonly RadioGroup _radioGroup;

    public RegionOpSelector ()
    {
        Width = Dim.Auto ();
        Height = Dim.Auto ();

        BorderStyle = LineStyle.Single;
        Border!.Thickness = new (0, 1, 0, 0);
        Title = "RegionOp";

        _radioGroup = new ()
        {
            X = 0,
            Y = 0,
            RadioLabels = Enum.GetNames<RegionOp> ().Select (n => n = "_" + n).ToArray ()
        };
        _radioGroup.SelectedItemChanged += (s, a) => { SelectedItemChanged?.Invoke (this, (RegionOp)a.SelectedItem); };
        Add (_radioGroup);
    }

    public event EventHandler<RegionOp>? SelectedItemChanged;

    public RegionOp SelectedItem
    {
        get => (RegionOp)_radioGroup.SelectedItem;
        set => _radioGroup.SelectedItem = (int)value;
    }
}

public class AttributeView : View
{
    public event EventHandler<Attribute?>? ValueChanged;
    private Attribute? _value;

    public Attribute? Value
    {
        get => _value;
        set
        {
            _value = value;
            ValueChanged?.Invoke (this, value);
        }
    }

    private static readonly HashSet<(int, int)> _foregroundPoints =
    [
        (0, 0), (1, 0), (2, 0),
        (0, 1), (1, 1), (2, 1)
    ];

    private static readonly HashSet<(int, int)> _backgroundPoints =
    [
        (3, 1),
        (1, 2), (2, 2), (3, 2)
    ];

    public AttributeView ()
    {
        Width = Dim.Fill ();
        Height = 4;

        BorderStyle = LineStyle.Single;
        Border!.Thickness = new (0, 1, 0, 0);
        Title = "Attribute";
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        Color fg = Value?.Foreground ?? Color.Black;
        Color bg = Value?.Background ?? Color.Black;

        bool isTransparentFg = fg == GetNormalColor ().Background;
        bool isTransparentBg = bg == GetNormalColor ().Background;

        SetAttribute (new (fg, isTransparentFg ? Color.Gray : fg));

        // Square of foreground color
        foreach ((int, int) point in _foregroundPoints)
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

        SetAttribute (new (bg, isTransparentBg ? Color.Gray : bg));

        // Square of background color
        foreach ((int, int) point in _backgroundPoints)
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

        return true;
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
        }

        mouseEvent.Handled = true;

        return mouseEvent.Handled;
    }

    private bool IsForegroundPoint (int x, int y) { return _foregroundPoints.Contains ((x, y)); }

    private bool IsBackgroundPoint (int x, int y) { return _backgroundPoints.Contains ((x, y)); }

    private void ClickedInBackground ()
    {
        if (LineDrawing.PromptForColor ("Background", Value!.Value.Background, out Color newColor))
        {
            Value = new (Value!.Value.Foreground, newColor);
            SetNeedsDraw ();
        }
    }

    private void ClickedInForeground ()
    {
        if (LineDrawing.PromptForColor ("Foreground", Value!.Value.Foreground, out Color newColor))
        {
            Value = new (newColor, Value!.Value.Background);
            SetNeedsDraw ();
        }
    }
}
