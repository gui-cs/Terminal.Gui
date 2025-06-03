using System;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("True Colors", "Demonstration of true color support.")]
[ScenarioCategory ("Colors")]
public class TrueColors : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        var x = 2;
        var y = 1;

        bool canTrueColor = Application.Driver?.SupportsTrueColor ?? false;

        var lblDriverName = new Label
        {
            X = x, Y = y++, Text = $"Current driver is {Application.Driver?.GetType ().Name}"
        };
        app.Add (lblDriverName);
        y++;

        var cbSupportsTrueColor = new CheckBox
        {
            X = x,
            Y = y++,
            CheckedState = canTrueColor ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = false,
            Enabled = false,
            Text = "Driver supports true color "
        };
        app.Add (cbSupportsTrueColor);

        var cbUseTrueColor = new CheckBox
        {
            X = x,
            Y = y++,
            CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            Enabled = canTrueColor,
            Text = "Force 16 colors"
        };
        cbUseTrueColor.CheckedStateChanging += (_, evt) => { Application.Force16Colors = evt.Result == CheckState.Checked; };
        app.Add (cbUseTrueColor);

        y += 2;
        SetupGradient ("Red gradient", x, ref y, i => new (i, 0));
        SetupGradient ("Green gradient", x, ref y, i => new (0, i));
        SetupGradient ("Blue gradient", x, ref y, i => new (0, 0, i));
        SetupGradient ("Yellow gradient", x, ref y, i => new (i, i));
        SetupGradient ("Magenta gradient", x, ref y, i => new (i, 0, i));
        SetupGradient ("Cyan gradient", x, ref y, i => new (0, i, i));
        SetupGradient ("Gray gradient", x, ref y, i => new (i, i, i));

        app.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 2, Text = "Mouse over to get the gradient view color:" }
                );

        app.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 4, Text = "Red:" }
                );

        app.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 5, Text = "Green:" }
                );

        app.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 6, Text = "Blue:" }
                );

        app.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 8, Text = "Darker:" }
                );

        app.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 9, Text = "Lighter:" }
                );

        var lblRed = new Label { X = Pos.AnchorEnd (32), Y = 4, Text = "na" };
        app.Add (lblRed);
        var lblGreen = new Label { X = Pos.AnchorEnd (32), Y = 5, Text = "na" };
        app.Add (lblGreen);
        var lblBlue = new Label { X = Pos.AnchorEnd (32), Y = 6, Text = "na" };
        app.Add (lblBlue);

        var lblDarker = new Label { X = Pos.AnchorEnd (32), Y = 8, Text = "     " };
        app.Add (lblDarker);

        var lblLighter = new Label { X = Pos.AnchorEnd (32), Y = 9, Text = "    " };
        app.Add (lblLighter);

        Application.MouseEvent += (s, e) =>
                                  {
                                      if (e.View == null)
                                      {
                                          return;
                                      }

                                      if (e.Flags == MouseFlags.Button1Clicked)
                                      {
                                          Attribute normal = e.View.GetAttributeForRole (VisualRole.Normal);

                                          lblLighter.SetScheme (new (e.View.GetScheme ())
                                          {
                                              Normal = new (
                                                            normal.Foreground,
                                                            normal.Background.GetBrighterColor ()
                                                           )
                                          });
                                      }
                                      else
                                      {
                                          Attribute normal = e.View.GetAttributeForRole (VisualRole.Normal);
                                          lblRed.Text = normal.Foreground.R.ToString ();
                                          lblGreen.Text = normal.Foreground.G.ToString ();
                                          lblBlue.Text = normal.Foreground.B.ToString ();
                                      }
                                  };
        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();

        return;

        void SetupGradient (string name, int x, ref int y, Func<int, Color> colorFunc)
        {
            var gradient = new Label { X = x, Y = y++, Text = name };
            app.Add (gradient);

            for (int dx = x, i = 0; i <= 256; i += 4)
            {
                var l = new Label
                {
                    X = dx++,
                    Y = y
                };
                l.SetScheme (new ()
                {
                    Normal = new (
                                  colorFunc (Math.Clamp (i, 0, 255)),
                                  colorFunc (Math.Clamp (i, 0, 255))
                                 )
                });
                app.Add (l);
            }

            y += 2;
        }
    }
}
