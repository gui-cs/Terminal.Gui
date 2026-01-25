using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Terminal.Gui.ViewBase;

namespace MyFirstView;

public class FirstView : View
{
    public int Count
    {
        get => field;
        set
        {
            field = value;
            SetContentSize(new Size(Viewport.Width, value));
        }
    }

    /// <inheritdoc />
    protected override bool OnDrawingContent (DrawContext? context)
    {
        var from = Math.Max (Viewport.Y, 0);
        var to = Math.Min (Viewport.Y + Viewport.Height, Count);
        var line = 0;
        for (int i = from; i < to; i++, line++)
        {
            AddStr(1, line, $"Item {i + 1}");
        }
        return true;
    }
}
