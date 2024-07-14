﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("SpinnerView Styles", "Shows the SpinnerView Styles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
public class SpinnerViewStyles : Scenario
{
    public override void Main ()
    {
    
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        const int DEFAULT_DELAY = 130;
        const string DEFAULT_CUSTOM = @"-\|/";
        Dictionary<int, KeyValuePair<string, Type>> styleDict = new ();
        var i = 0;

        foreach (Type style in typeof (SpinnerStyle).GetNestedTypes ())
        {
            styleDict.Add (i, new KeyValuePair<string, Type> (style.Name, style));
            i++;
        }

        var preview = new View
        {
            X = Pos.Center (),
            Y = 0,
            Width = 22,
            Height = 3,

            //Title = "Preview",
            BorderStyle = LineStyle.Single
        };
        app.Add (preview);

        var spinner = new SpinnerView { X = Pos.Center (), Y = 0 };
        preview.Add (spinner);
        spinner.AutoSpin = true;

        var ckbAscii = new CheckBox
        {
            X = Pos.Center () - 7,
            Y = Pos.Bottom (preview),
            Enabled = false,
            State = CheckState.Checked,
            Text = "Ascii Only"
        };
        app.Add (ckbAscii);

        var ckbNoSpecial = new CheckBox
        {
            X = Pos.Center () + 7,
            Y = Pos.Bottom (preview),
            Enabled = false,
            State = CheckState.Checked,
            Text = "No Special"
        };
        app.Add (ckbNoSpecial);

        var ckbReverse = new CheckBox
        {
            X = Pos.Center () - 22, Y = Pos.Bottom (preview) + 1, State = CheckState.UnChecked, Text = "Reverse"
        };
        app.Add (ckbReverse);

        var ckbBounce = new CheckBox
        {
            X = Pos.Right (ckbReverse) + 2, Y = Pos.Bottom (preview) + 1, State = CheckState.UnChecked, Text = "Bounce"
        };
        app.Add (ckbBounce);

        var delayLabel = new Label { X = Pos.Right (ckbBounce) + 2, Y = Pos.Bottom (preview) + 1, Text = "Delay:" };
        app.Add (delayLabel);

        var delayField = new TextField
        {
            X = Pos.Right (delayLabel), Y = Pos.Bottom (preview) + 1, Width = 5, Text = DEFAULT_DELAY.ToString ()
        };
        app.Add (delayField);

        delayField.TextChanged += (s, e) =>
                                  {
                                      if (ushort.TryParse (delayField.Text, out ushort i))
                                      {
                                          spinner.SpinDelay = i;
                                      }
                                  };

        var customLabel = new Label { X = Pos.Right (delayField) + 2, Y = Pos.Bottom (preview) + 1, Text = "Custom:" };
        app.Add (customLabel);

        var customField = new TextField
        {
            X = Pos.Right (customLabel), Y = Pos.Bottom (preview) + 1, Width = 12, Text = DEFAULT_CUSTOM
        };
        app.Add (customField);

        string [] styleArray = styleDict.Select (e => e.Value.Key).ToArray ();

        if (styleArray.Length < 1)
        {
            return;
        }

        var styles = new ListView
        {
            X = Pos.Center (), Y = Pos.Bottom (preview) + 2, Height = Dim.Fill (), Width = Dim.Fill (1)
        };
        styles.SetSource (new ObservableCollection<string> (styleArray));
        styles.SelectedItem = 0; // SpinnerStyle.Custom;
        app.Add (styles);
        SetCustom ();

        customField.TextChanged += (s, e) =>
                                   {
                                       if (customField.Text.Length > 0)
                                       {
                                           if (styles.SelectedItem != 0)
                                           {
                                               styles.SelectedItem = 0; // SpinnerStyle.Custom
                                           }

                                           SetCustom ();
                                       }
                                   };

        styles.SelectedItemChanged += (s, e) =>
                                      {
                                          if (e.Item == 0)
                                          {
                                              // SpinnerStyle.Custom
                                              if (customField.Text.Length < 1)
                                              {
                                                  customField.Text = DEFAULT_CUSTOM;
                                              }

                                              if (delayField.Text.Length < 1)
                                              {
                                                  delayField.Text = DEFAULT_DELAY.ToString ();
                                              }

                                              SetCustom ();
                                          }
                                          else
                                          {
                                              spinner.Visible = true;
                                              spinner.Style = (SpinnerStyle)Activator.CreateInstance (styleDict [e.Item].Value);
                                              delayField.Text = spinner.SpinDelay.ToString ();
                                              ckbBounce.State = spinner.SpinBounce ? CheckState.Checked : CheckState.UnChecked;
                                              ckbNoSpecial.State = !spinner.HasSpecialCharacters ? CheckState.Checked : CheckState.UnChecked;
                                              ckbAscii.State = spinner.IsAsciiOnly ? CheckState.Checked : CheckState.UnChecked;
                                              ckbReverse.State = CheckState.UnChecked;
                                          }
                                      };

        ckbReverse.Toggle += (s, e) => { spinner.SpinReverse = e.NewValue == CheckState.Checked; };

        ckbBounce.Toggle += (s, e) => { spinner.SpinBounce = e.NewValue == CheckState.Checked; };

        app.Unloaded += App_Unloaded;

        void SetCustom ()
        {
            if (customField.Text.Length > 0)
            {
                spinner.Visible = true;

                if (ushort.TryParse (delayField.Text, out ushort d))
                {
                    spinner.SpinDelay = d;
                }
                else
                {
                    delayField.Text = DEFAULT_DELAY.ToString ();
                    spinner.SpinDelay = DEFAULT_DELAY;
                }

                List<string> str = new ();

                foreach (char c in customField.Text)
                {
                    str.Add (c.ToString ());
                }

                spinner.Sequence = str.ToArray ();
            }
            else
            {
                spinner.Visible = false;
            }
        }

        void App_Unloaded (object sender, EventArgs args)
        {
            if (spinner != null)
            {
                spinner.Dispose ();
                spinner = null;
            }

            app.Unloaded -= App_Unloaded;
        }


        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();
    }
}
