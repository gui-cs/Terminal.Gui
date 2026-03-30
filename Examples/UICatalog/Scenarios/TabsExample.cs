#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tabs Example", "Demonstrates the Tabs and Tab views with tab-style borders.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
public sealed class TabsExample : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        // ── Main Tabs control ──
        Tabs tabs = new ()
        {
            X = 2,
            Y = 3,
            Title = "_Tabs",
            Width = Dim.Percent (70),
            Height = Dim.Percent (70),
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable ,
            BorderStyle = LineStyle.Double
        };

        tabs.EnableForDesign ();

        // ── Editors ──
        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single, AutoSelectViewToEdit = true, Arrangement = ViewArrangement.Movable, X = Pos.AnchorEnd ()
        };
        adornmentsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);
        adornmentsEditor.AutoSelectSuperView = appWindow;
        adornmentsEditor.AutoSelectAdornments = true;
        adornmentsEditor.ShowViewIdentifier = true;

        ViewportSettingsEditor viewportSettingsEditor = new ()
        {
            BorderStyle = LineStyle.Single, AutoSelectViewToEdit = true, Arrangement = ViewArrangement.Movable, Y = Pos.AnchorEnd ()
        };
        viewportSettingsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);
        viewportSettingsEditor.AutoSelectSuperView = appWindow;
        viewportSettingsEditor.AutoSelectAdornments = true;
        viewportSettingsEditor.ShowViewIdentifier = true;

        appWindow.Add (tabs, adornmentsEditor, viewportSettingsEditor);

        app.Run (appWindow);
    }
}
