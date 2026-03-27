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
        View tab1 = CreateTabView ("_Attribute");
        tab1.Width = Dim.Percent (60);
        tab1.Height = Dim.Percent (70);
        tab1.Text = "content1";
        AttributePicker attributePicker = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab1.Add (attributePicker);

        // ── Tab2: unfocused tab at offset 5 ────────────────────────────
        View tab2 = CreateTabView ("_Line Style");
        tab2.Border.TabOffset = tab1.Border.TabEnd - 1;
        tab2.Width = Dim.Width (tab1);
        tab2.Height = Dim.Height (tab1);
        tab2.Text = "content2";
        OptionSelector<LineStyle> lineStyleSelector = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab2.Add (lineStyleSelector);

        lineStyleSelector.ValueChanged += (_, e) =>
                                          {
                                              tab1.Border.LineStyle = e.Value;
                                              tab2.Border.LineStyle = e.Value;
                                          };

        View tab3 = CreateTabView ("_Tab Settings");
        tab3.Border.TabOffset = tab2.Border.TabEnd - 1;
        tab3.Width = Dim.Width (tab2);
        tab3.Height = Dim.Height (tab2);
        tab3.Text = "content3";
        OptionSelector<Side> tabSideSelector = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab3.Add (tabSideSelector);

        tabSideSelector.ValueChanged += (_, e) =>
                                        {
                                            if (e.Value is null)
                                            {
                                                return;
                                            }
                                            tab1.Border.TabSide = e.Value.Value;
                                            tab2.Border.TabSide = e.Value.Value;
                                            tab3.Border.TabSide = e.Value.Value;
                                        };

        appWindow.Add (tab1, tab2, tab3);

        attributePicker.ValueChanged += (_, e) =>
                                        {
                                            if (e.NewValue is { })
                                            {
                                                tab1.Border.View?.SubViews.ElementAt (0)
                                                    .SetScheme (new Scheme (tab1.Border.View?.SubViews.ElementAt (0).GetScheme ())
                                                    {
                                                        Active =
                                                            new Attribute (e.NewValue.Value.Foreground,
                                                                           e.NewValue.Value.Background,
                                                                           e.NewValue.Value.Style),
                                                        HotActive = new Attribute (e.NewValue.Value.Foreground,
                                                                                   e.NewValue.Value.Background,
                                                                                   e.NewValue.Value.Style)
                                                    });
                                            }
                                        };

        lineStyleSelector.ValueChanged += (_, e) =>
                                          {
                                              tab1.Border.LineStyle = e.Value;
                                              tab2.Border.LineStyle = e.Value;
                                              tab3.Border.LineStyle = e.Value;
                                          };

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
    private static View CreateTabView (string title)
    {
        View view = new ()
        {
            CanFocus = true,
            SuperViewRendersLineCanvas = true,
            BorderStyle = LineStyle.Rounded,
            Title = title,
            Arrangement = ViewArrangement.Overlapped
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = Side.Top;

        return view;
    }
}
