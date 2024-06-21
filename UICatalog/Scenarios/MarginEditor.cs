using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

public class MarginEditor : AdornmentEditor
{
    public MarginEditor ()
    {
        Title = "_Margin";
        Initialized += MarginEditor_Initialized;
    }

    private void MarginEditor_Initialized (object sender, EventArgs e)
    {
        var ckbShadow = new CheckBox
        {
            X = 0,
            //Y = Pos.AnchorEnd(),
            // BUGBUG: Hack until dimauto is working properly
            Y = Pos.Bottom (Subviews [^1]),

            SuperViewRendersLineCanvas = true,
            Title = "_Shadow",
            Enabled = AdornmentToEdit is { },
        };

        ckbShadow.Toggled += (sender, args) =>
                             {
                                 ((Margin)AdornmentToEdit).EnableShadow (args.NewValue!.Value);
                             };
        Add (ckbShadow);
    }
}