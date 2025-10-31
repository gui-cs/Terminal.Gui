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

    private OptionSelector<ShadowStyle>? _optionsShadow;

    private FlagSelector? _flagSelectorTransparent;

    private void MarginEditor_AdornmentChanged (object? sender, EventArgs e)
    {
        if (AdornmentToEdit is { })
        {
            _optionsShadow!.Value = ((Margin)AdornmentToEdit).ShadowStyle;
        }

        if (AdornmentToEdit is { })
        {
            _flagSelectorTransparent!.Value = (int)((Margin)AdornmentToEdit).ViewportSettings;
        }
    }

    private void MarginEditor_Initialized (object? sender, EventArgs e)
    {
        _optionsShadow = new ()
        {
            X = 0,
            Y = Pos.Bottom (SubViews.ElementAt(SubViews.Count-1)),

            SuperViewRendersLineCanvas = true,
            Title = "_Shadow",
            BorderStyle = LineStyle.Single,
            AssignHotKeys = true
        };

        if (AdornmentToEdit is { })
        {
            _optionsShadow.Value = ((Margin)AdornmentToEdit).ShadowStyle;
        }

        _optionsShadow.ValueChanged += (_, args) =>
                                        {
                                            ((Margin)AdornmentToEdit!).ShadowStyle = (ShadowStyle)args.Value!;
                                        };

        Add (_optionsShadow);

        _flagSelectorTransparent = new FlagSelector<ViewportSettingsFlags> ()
        {
            X = 0,
            Y = Pos.Bottom (_optionsShadow),

            SuperViewRendersLineCanvas = true,
            Title = "_ViewportSettings",
            BorderStyle = LineStyle.Single,
        };
        _flagSelectorTransparent.Values = [(int)ViewportSettingsFlags.Transparent, (int)ViewportSettingsFlags.TransparentMouse];
        _flagSelectorTransparent.Labels = ["Transparent", "TransparentMouse"];
        _flagSelectorTransparent.AssignHotKeys = true;

        Add (_flagSelectorTransparent);

        if (AdornmentToEdit is { })
        {
            _flagSelectorTransparent.Value = (int)((Margin)AdornmentToEdit).ViewportSettings;
        }

        _flagSelectorTransparent.ValueChanged += (_, args) =>
                                                 {
                                                     ((Margin)AdornmentToEdit!).ViewportSettings = (ViewportSettingsFlags)args.Value!;
                                                 };


    }
}