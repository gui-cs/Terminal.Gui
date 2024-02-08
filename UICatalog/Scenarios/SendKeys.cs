using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("SendKeys", "SendKeys sample - Send key combinations.")]
[ScenarioCategory ("Mouse and Keyboard")]
public class SendKeys : Scenario {
    public override void Setup () {
        var label = new Label { X = Pos.Center (), Y = Pos.Center () - 6, Text = "Insert the text to send:" };
        Win.Add (label);

        var txtInput = new TextField { X = Pos.Center (), Y = Pos.Center () - 5, Width = 20, Text = "MockKeyPresses" };
        Win.Add (txtInput);

        var ckbShift = new CheckBox { X = Pos.Center (), Y = Pos.Center () - 4, Text = "Shift" };
        Win.Add (ckbShift);

        var ckbAlt = new CheckBox { X = Pos.Center (), Y = Pos.Center () - 3, Text = "Alt" };
        Win.Add (ckbAlt);

        var ckbControl = new CheckBox { X = Pos.Center (), Y = Pos.Center () - 2, Text = "Control" };
        Win.Add (ckbControl);

        label = new Label { X = Pos.Center (), Y = Pos.Center () + 1, Text = "Result keys:" };
        Win.Add (label);

        var txtResult = new TextField { X = Pos.Center (), Y = Pos.Center () + 2, Width = 20 };
        Win.Add (txtResult);

        var rKeys = "";
        var rControlKeys = "";
        var IsShift = false;
        var IsAlt = false;
        var IsCtrl = false;

        txtResult.KeyDown += (s, e) => {
            rKeys += (char)e.KeyCode;
            if (!IsShift && e.IsShift) {
                rControlKeys += " Shift ";
                IsShift = true;
            }

            if (!IsAlt && e.IsAlt) {
                rControlKeys += " Alt ";
                IsAlt = true;
            }

            if (!IsCtrl && e.IsCtrl) {
                rControlKeys += " Ctrl ";
                IsCtrl = true;
            }
        };

        var lblShippedKeys = new Label { X = Pos.Center (), Y = Pos.Center () + 3, AutoSize = true };
        Win.Add (lblShippedKeys);

        var lblShippedControlKeys = new Label { X = Pos.Center (), Y = Pos.Center () + 5, AutoSize = true };
        Win.Add (lblShippedControlKeys);

        var button = new Button { X = Pos.Center (), Y = Pos.Center () + 7, IsDefault = true, Text = "Process keys" };
        Win.Add (button);

        void ProcessInput () {
            rKeys = "";
            rControlKeys = "";
            txtResult.Text = "";
            IsShift = false;
            IsAlt = false;
            IsCtrl = false;
            txtResult.SetFocus ();
            foreach (char r in txtInput.Text) {
                ConsoleKey ck = char.IsLetter (r)
                                    ? (ConsoleKey)char.ToUpper (r)
                                    : (ConsoleKey)r;
                Application.Driver.SendKeys (
                    r,
                    ck,
                    (bool)ckbShift.Checked,
                    (bool)ckbAlt.Checked,
                    (bool)ckbControl.Checked
                );
            }

            lblShippedKeys.Text = rKeys;
            lblShippedControlKeys.Text = rControlKeys;
            txtInput.SetFocus ();
        }

        button.Clicked += (s, e) => ProcessInput ();

        Win.KeyDown += (s, e) => {
            if (e.KeyCode == KeyCode.Enter) {
                ProcessInput ();
                e.Handled = true;
            }
        };
    }
}
