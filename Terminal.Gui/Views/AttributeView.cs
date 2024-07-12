using System.Collections.Generic;
namespace Terminal.Gui;

public class AttributeView : View
{


    public event EventHandler<Attribute> ValueChanged;
    private Attribute _value;

    public Attribute Value
    {
        get => _value;
        set {
            _value = value;
            ValueChanged?.Invoke (this,value);
        }

    }

    private static readonly HashSet<(int, int)> ForegroundPoints = new HashSet<(int, int)>
    {
        (0, 0), (1, 0),(2,0),
        (0, 1), (1, 1),(2,1)
    };

    private static readonly HashSet<(int, int)> BackgroundPoints = new HashSet<(int, int)>
    {
        (3, 1), 
        (1, 2), (2, 2),(3,2)
    };

    public AttributeView ()
    {
        Width = 4;
        Height = 3;

    }
    /// <inheritdoc />
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        Driver.SetAttribute (new Attribute (Value.Foreground, Value.Foreground));
        // Square of foreground color
        foreach (var point in ForegroundPoints)
        {
            AddRune (point.Item1, point.Item2, (Rune)'█');
        }

        Driver.SetAttribute (new Attribute (Value.Background, Value.Background));
        // Square of background color
        foreach (var point in BackgroundPoints)
        {
            AddRune (point.Item1, point.Item2, (Rune)'█');
        }
    }

    // TODO focusable, keyboard support etc

    /// <inheritdoc />
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
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
        return base.OnMouseEvent (mouseEvent);
    }

    private bool IsForegroundPoint (int x, int y)
    {
        return ForegroundPoints.Contains ((x, y));
    }

    private bool IsBackgroundPoint (int x, int y)
    {
        return BackgroundPoints.Contains ((x, y));
    }

    private void ClickedInBackground ()
    {
        if (PromptFor ("Background", Value.Background, out var newColor))
        {
            Value = new Attribute(Value.Foreground,newColor);
        }
    }

    private void ClickedInForeground ()
    {
        if (PromptFor ("Foreground",Value.Foreground, out var newColor))
        {
            Value = new Attribute (newColor, Value.Background);
        }
    }

    private bool PromptFor (string title, Color current, out Color newColor)
    {
        bool accept = false;
        var d = new Dialog ()
        {
            Title = title,
            Height = 6
        };

        var btnOk = new Button ()
        {
            Text = "Ok"
        };

        btnOk.Accept += (s, e) =>
        {
            accept = true;
            e.Handled = true;
            Application.RequestStop ();
                        };
        var btnCancel = new Button () { Text = "Cancel"};
        btnCancel.Accept += (s, e) =>
        {
            e.Handled = true;
            Application.RequestStop ();
                            };

        d.AddButton (btnOk);
        d.AddButton (btnCancel);

        var cp = new ColorPicker2
        {
            Value = current,
             Width = Dim.Fill (),
             Height = Dim.Fill ()
        };

        d.Add (cp);

        Application.Run (d);
        newColor = cp.Value;
        return accept;
    }
}

