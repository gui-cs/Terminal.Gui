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

    }
}