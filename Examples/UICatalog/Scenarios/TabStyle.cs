#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tab Style", "Demonstrates tab-style borders on Views composing via SuperViewRendersLineCanvas.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Adornments")]
public sealed class TabStyle : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single, AutoSelectViewToEdit = true, Arrangement = ViewArrangement.Movable, X = Pos.AnchorEnd ()
        };
        adornmentsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);

        ViewportSettingsEditor viewportSettingsEditor = new ()
        {
            BorderStyle = LineStyle.Single, AutoSelectViewToEdit = true, Arrangement = ViewArrangement.Movable, Y = Pos.AnchorEnd ()
        };
        viewportSettingsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);

        // ── Tab1: focused tab at offset 0 ──────────────────────────────
        View tab1 = CreateTabView ("Tab1", 0, false);
        tab1.Width = Dim.Percent (50);
        tab1.Height = Dim.Fill (viewportSettingsEditor);
        tab1.Text = "content1";

        // ── Tab2: unfocused tab at offset 5 ────────────────────────────
        View tab2 = CreateTabView ("Tab2", 5, false);
        tab2.Width = Dim.Percent (50);
        tab2.Height = Dim.Fill (viewportSettingsEditor);
        tab2.Text = "content2";

        appWindow.Add (tab1, tab2);

        // Let the editors track whichever tab is clicked
        adornmentsEditor.AutoSelectViewToEdit = true;
        adornmentsEditor.AutoSelectSuperView = appWindow;
        adornmentsEditor.AutoSelectAdornments = true;
        adornmentsEditor.ShowViewIdentifier = true;

        viewportSettingsEditor.AutoSelectViewToEdit = true;
        viewportSettingsEditor.AutoSelectSuperView = appWindow;
        viewportSettingsEditor.AutoSelectAdornments = true;
        viewportSettingsEditor.ShowViewIdentifier = true;

        appWindow.Add (adornmentsEditor, viewportSettingsEditor);
        tab1.SetFocus ();

        app.Run (appWindow);
    }

    /// <summary>Creates a View configured with a tab-style border on <see cref="Side.Top"/>.</summary>
    private static View CreateTabView (string title, int tabOffset, bool hasFocus)
    {
        View view = new ()
        {
            CanFocus = true,
            HasFocus = hasFocus,
            SuperViewRendersLineCanvas = true,
            BorderStyle = LineStyle.Rounded,
            Title = title,
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = tabOffset;

        return view;
    }
}
