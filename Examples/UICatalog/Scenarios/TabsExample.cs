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
            Width = Dim.Percent (70),
            Height = Dim.Percent (80)
        };

        Tab tab1 = new () { Title = "_Attribute" };
        AttributePicker attributePicker = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab1.Add (attributePicker);

        Tab tab2 = new () { Title = "_Line Style" };
        OptionSelector<LineStyle> lineStyleSelector = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab2.Add (lineStyleSelector);

        Tab tab3 = new () { Title = "Tab _Settings" };
        OptionSelector<Side> tabSideSelector = new () { Y = 1, BorderStyle = LineStyle.Single, Title = "Tab Side" };
        tab3.Add (tabSideSelector);

        tabs.Add (tab1, tab2, tab3);
        tabs.Value = tab1;

        // ── Side selector changes TabSide ──
        tabSideSelector.ValueChanged += (_, e) =>
                                         {
                                             if (e.Value is { })
                                             {
                                                 tabs.TabSide = e.Value.Value;
                                             }
                                         };

        // ── Line style selector changes TabLineStyle ──
        lineStyleSelector.ValueChanged += (_, e) =>
                                           {
                                               if (e.Value is { })
                                               {
                                                   tabs.TabLineStyle = e.Value.Value;
                                               }
                                           };

        // ── Editors ──
        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single,
            AutoSelectViewToEdit = true,
            Arrangement = ViewArrangement.Movable,
            X = Pos.AnchorEnd ()
        };
        adornmentsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);
        adornmentsEditor.AutoSelectSuperView = appWindow;
        adornmentsEditor.AutoSelectAdornments = true;
        adornmentsEditor.ShowViewIdentifier = true;

        ViewportSettingsEditor viewportSettingsEditor = new ()
        {
            BorderStyle = LineStyle.Single,
            AutoSelectViewToEdit = true,
            Arrangement = ViewArrangement.Movable,
            Y = Pos.AnchorEnd ()
        };
        viewportSettingsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);
        viewportSettingsEditor.AutoSelectSuperView = appWindow;
        viewportSettingsEditor.AutoSelectAdornments = true;
        viewportSettingsEditor.ShowViewIdentifier = true;

        appWindow.Add (tabs, adornmentsEditor, viewportSettingsEditor);

        app.Run (appWindow);
    }
}
