#nullable enable
using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

public class MarginEditor : AdornmentEditor
{
    public MarginEditor ()
    {
        Title = "_Margin";
        Initialized += MarginEditor_Initialized;
        AdornmentChanged += MarginEditor_AdornmentChanged;
    }

    private RadioGroup? _rgShadow;

    private void MarginEditor_AdornmentChanged (object? sender, EventArgs e)
    {
        if (AdornmentToEdit is { })
        {
            _rgShadow!.SelectedItem = (int)((Margin)AdornmentToEdit).ShadowStyle;
        }
    }

    private void MarginEditor_Initialized (object? sender, EventArgs e)
    {
        _rgShadow = new RadioGroup
        {
            X = 0,
            Y = Pos.AnchorEnd (),

            SuperViewRendersLineCanvas = true,
            Title = "_Shadow",
            BorderStyle = LineStyle.Single,
            RadioLabels = Enum.GetNames (typeof (ShadowStyle)),
        };

        if (AdornmentToEdit is { })
        {
            _rgShadow.SelectedItem = (int)((Margin)AdornmentToEdit).ShadowStyle;
        }

        _rgShadow.SelectedItemChanged += (_, args) =>
                                        {
                                            ((Margin)AdornmentToEdit!).ShadowStyle = (ShadowStyle)args.SelectedItem!;
                                        };

        Add (_rgShadow);
    }
}