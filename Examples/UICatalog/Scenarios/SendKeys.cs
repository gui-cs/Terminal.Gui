using System;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("SendKeys", "SendKeys sample - Send key combinations.")]
[ScenarioCategory ("Mouse and Keyboard")]
public class SendKeys : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = GetQuitKeyAndName () };
        var label = new Label { X = Pos.Center (), Y = Pos.Center () - 6, Text = "Insert the text to send:" };
        win.Add (label);

        var txtInput = new TextField { X = Pos.Center (), Y = Pos.Center () - 5, Width = 20, Text = "MockKeyPresses" };
        win.Add (txtInput);

        var ckbShift = new CheckBox { X = Pos.Center (), Y = Pos.Center () - 4, Text = "Shift" };
        win.Add (ckbShift);

        var ckbAlt = new CheckBox { X = Pos.Center (), Y = Pos.Center () - 3, Text = "Alt" };
        win.Add (ckbAlt);

        var ckbControl = new CheckBox { X = Pos.Center (), Y = Pos.Center () - 2, Text = "Control" };
        win.Add (ckbControl);

        label = new Label { X = Pos.Center (), Y = Pos.Center () + 1, Text = "Result keys:" };
        win.Add (label);

        var txtResult = new TextField { X = Pos.Center (), Y = Pos.Center () + 2, Width = 20 };
        win.Add (txtResult);

        var rKeys = "";
        var rControlKeys = "";
        var IsShift = false;
        var IsAlt = false;
        var IsCtrl = false;

        txtResult.KeyDown += (s, e) =>
                             {
                                 rKeys += (char)e.KeyCode;

                                 if (!IsShift && e.IsShift)
                                 {
                                     rControlKeys += " Shift ";
                                     IsShift = true;
                                 }

                                 if (!IsAlt && e.IsAlt)
                                 {
                                     rControlKeys += " Alt ";
                                     IsAlt = true;
                                 }

                                 if (!IsCtrl && e.IsCtrl)
                                 {
                                     rControlKeys += " Ctrl ";
                                     IsCtrl = true;
                                 }
                             };

        var lblShippedKeys = new Label { X = Pos.Center (), Y = Pos.Center () + 3 };
        win.Add (lblShippedKeys);

        var lblShippedControlKeys = new Label { X = Pos.Center (), Y = Pos.Center () + 5 };
        win.Add (lblShippedControlKeys);

        var button = new Button { X = Pos.Center (), Y = Pos.Center () + 7, IsDefault = true, Text = "Process keys" };
        win.Add (button);

        void ProcessInput ()
        {
            rKeys = "";
            rControlKeys = "";
            txtResult.Text = "";
            IsShift = false;
            IsAlt = false;
            IsCtrl = false;
            txtResult.SetFocus ();

            foreach (char r in txtInput.Text)
            {
                ConsoleKey ck = char.IsLetter (r)
                                    ? (ConsoleKey)char.ToUpper (r)
                                    : (ConsoleKey)r;

                Application.Driver?.SendKeys (
                                             r,
                                             ck,
                                             ckbShift.CheckedState == CheckState.Checked,
                                             ckbAlt.CheckedState == CheckState.Checked,
                                             ckbControl.CheckedState == CheckState.Checked
                                            );
            }

            lblShippedKeys.Text = rKeys;
            lblShippedControlKeys.Text = rControlKeys;
            txtInput.SetFocus ();
        }

        button.Accepting += (s, e) => ProcessInput ();

        win.KeyDown += (s, e) =>
                       {
                           if (e.KeyCode == KeyCode.Enter)
                           {
                               ProcessInput ();
                               e.Handled = true;
                           }
                       };

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }
}
