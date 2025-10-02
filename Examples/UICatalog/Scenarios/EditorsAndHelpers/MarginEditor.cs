#nullable enable
using System;

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

    private FlagSelector? _flagSelectorTransparent;

    private void MarginEditor_AdornmentChanged (object? sender, EventArgs e)
    {
        if (AdornmentToEdit is { })
        {
            _rgShadow!.SelectedItem = (int)((Margin)AdornmentToEdit).ShadowStyle;
        }

        if (AdornmentToEdit is { })
        {
            _flagSelectorTransparent!.Value = (uint)((Margin)AdornmentToEdit).ViewportSettings;
        }
    }

    private void MarginEditor_Initialized (object? sender, EventArgs e)
    {
        _rgShadow = new RadioGroup
        {
            X = 0,
            Y = Pos.Bottom (SubViews.ElementAt(SubViews.Count-1)),

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

        var flags = new Dictionary<uint, string> ()
        {
            { (uint)Terminal.Gui.ViewBase.ViewportSettingsFlags.Transparent, "Transparent" },
            { (uint)Terminal.Gui.ViewBase.ViewportSettingsFlags.TransparentMouse, "TransparentMouse" }
        };

        _flagSelectorTransparent = new FlagSelector ()
        {
            X = 0,
            Y = Pos.Bottom (_rgShadow),

            SuperViewRendersLineCanvas = true,
            Title = "_ViewportSettings",
            BorderStyle = LineStyle.Single,
        };
        _flagSelectorTransparent.SetFlags(flags.AsReadOnly ());


        Add (_flagSelectorTransparent);

        if (AdornmentToEdit is { })
        {
            _flagSelectorTransparent.Value = (uint)((Margin)AdornmentToEdit).ViewportSettings;
        }

        _flagSelectorTransparent.ValueChanged += (_, args) =>
                                                 {
                                                     ((Margin)AdornmentToEdit!).ViewportSettings = (ViewportSettingsFlags)args.Value!;
                                                 };


    }
}