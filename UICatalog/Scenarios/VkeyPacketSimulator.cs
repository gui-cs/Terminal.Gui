using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.ConsoleDrivers;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("VkeyPacketSimulator", "Simulates the Virtual Key Packet")]
[ScenarioCategory ("Mouse and Keyboard")]
public class VkeyPacketSimulator : Scenario
{
    private static readonly ManualResetEventSlim _stopOutput = new (false);
    private readonly List<KeyCode> _keyboardStrokes = new ();
    private bool _outputStarted;
    private bool _wasUnknown;

    public override void Setup ()
    {
        var label = new Label { X = Pos.Center (), Text = "Input" };
        Win.Add (label);

        var btnInput = new Button { X = Pos.AnchorEnd (16), Text = "Select Input" };
        Win.Add (btnInput);

        const string ruler = "|123456789";

        var inputHorizontalRuler = new Label
        {
            Y = Pos.Bottom (btnInput), AutoSize = false, Width = Dim.Fill (), ColorScheme = Colors.ColorSchemes ["Error"]
        };
        Win.Add (inputHorizontalRuler);

        var inputVerticalRuler = new Label
        {
            Y = Pos.Bottom (btnInput),
            AutoSize = false,
            Width = 1,
            ColorScheme = Colors.ColorSchemes ["Error"],
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        Win.Add (inputVerticalRuler);

        var tvInput = new TextView
        {
            Title = "Input",
            X = 1,
            Y = Pos.Bottom (inputHorizontalRuler),
            Width = Dim.Fill (),
            Height = Dim.Percent (50) - 1
        };
        Win.Add (tvInput);

        label = new Label { X = Pos.Center (), Y = Pos.Bottom (tvInput), Text = "Output" };
        Win.Add (label);

        var btnOutput = new Button { X = Pos.AnchorEnd (17), Y = Pos.Top (label), Text = "Select Output" };
        Win.Add (btnOutput);

        var outputHorizontalRuler = new Label
        {
            Y = Pos.Bottom (btnOutput),
            AutoSize = false,
            Width = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Error"]
        };
        Win.Add (outputHorizontalRuler);

        var outputVerticalRuler = new Label
        {
            Y = Pos.Bottom (btnOutput),
            AutoSize = false,
            Width = 1,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Error"],
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        Win.Add (outputVerticalRuler);

        var tvOutput = new TextView
        {
            Title = "Output",
            X = 1,
            Y = Pos.Bottom (outputHorizontalRuler),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ReadOnly = true
        };

        // Detect unknown keys and reject them.
        tvOutput.KeyDown += (s, e) =>
                            {
                                //System.Diagnostics.Debug.WriteLine ($"Output - KeyDown: {e.KeyCode}");
                                if (e.NoAlt.NoCtrl.NoShift == KeyCode.Null)
                                {
                                    _wasUnknown = true;
                                    e.Handled = true;

                                    return;
                                }

                                //System.Diagnostics.Debug.WriteLine ($"Output - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");
                                if (_outputStarted)
                                {
                                    // If the key wasn't handled by the TextView will popup a Dialog with the keys pressed.
                                    bool? handled = tvOutput.OnInvokingKeyBindings (e);

                                    if (handled == null || handled == false)
                                    {
                                        if (!tvOutput.OnProcessKeyDown (e))
                                        {
                                            Application.Invoke (
                                                                () => MessageBox.Query (
                                                                                        "Keys",
                                                                                        $"'{
                                                                                            Key.ToString (
                                                                                                          e.KeyCode,
                                                                                                          MenuBar.ShortcutDelimiter
                                                                                                         )
                                                                                        }' pressed!",
                                                                                        "Ok"
                                                                                       )
                                                               );
                                        }
                                    }
                                }

                                e.Handled = true;
                                _stopOutput.Set ();
                            };

        Win.Add (tvOutput);

        tvInput.KeyDown += (s, e) =>
                           {
                               //System.Diagnostics.Debug.WriteLine ($"Input - KeyDown: {e.KeyCode.Key}");
                               if (e.KeyCode == Key.Empty)
                               {
                                   _wasUnknown = true;
                                   e.Handled = true;
                               }
                               else
                               {
                                   _wasUnknown = false;
                               }
                           };

        tvInput.InvokingKeyBindings += (s, e) =>
                                       {
                                           Key ev = e;

                                           //System.Diagnostics.Debug.WriteLine ($"Input - KeyPress: {ev}");
                                           //System.Diagnostics.Debug.WriteLine ($"Input - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");

                                           if (!e.IsValid)
                                           {
                                               _wasUnknown = true;
                                               e.Handled = true;

                                               return;
                                           }

                                           _keyboardStrokes.Add (e.KeyCode);
                                       };

        tvInput.KeyUp += (s, e) =>
                         {
                             //System.Diagnostics.Debug.WriteLine ($"Input - KeyUp: {e.Key}");
                             e.Handled = true;

                             if (!_wasUnknown && _keyboardStrokes.Count > 0)
                             {
                                 _outputStarted = true;
                                 tvOutput.ReadOnly = false;
                                 tvOutput.SetFocus ();
                                 tvOutput.SetNeedsDisplay ();

                                 Task.Run (
                                           () =>
                                           {
                                               while (_outputStarted)
                                               {
                                                   try
                                                   {
                                                       while (_keyboardStrokes.Count > 0)
                                                       {
                                                           if (_keyboardStrokes [0] == KeyCode.Null)
                                                           {
                                                               continue;
                                                           }

                                                           ConsoleKeyInfo consoleKeyInfo =
                                                               ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (_keyboardStrokes [0]);

                                                           char keyChar =
                                                               ConsoleKeyMapping.EncodeKeyCharForVKPacket (consoleKeyInfo);

                                                           Application.Driver.SendKeys (
                                                                                        keyChar,
                                                                                        ConsoleKey.Packet,
                                                                                        consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Shift),
                                                                                        consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt),
                                                                                        consoleKeyInfo.Modifiers
                                                                                                      .HasFlag (ConsoleModifiers.Control)
                                                                                       );

                                                           _stopOutput.Wait ();
                                                           _stopOutput.Reset ();
                                                           _keyboardStrokes.RemoveAt (0);

                                                           Application.Invoke (
                                                                               () =>
                                                                               {
                                                                                   tvOutput.ReadOnly = true;
                                                                                   tvInput.SetFocus ();
                                                                               }
                                                                              );
                                                       }

                                                       _outputStarted = false;
                                                   }
                                                   catch (Exception)
                                                   {
                                                       Application.Invoke (
                                                                           () =>
                                                                           {
                                                                               MessageBox.ErrorQuery (
                                                                                                      "Error",
                                                                                                      "Couldn't send the keystrokes!",
                                                                                                      "Ok"
                                                                                                     );
                                                                               Application.RequestStop ();
                                                                           }
                                                                          );
                                                   }
                                               }

                                               //System.Diagnostics.Debug.WriteLine ($"_outputStarted: {_outputStarted}");
                                           }
                                          );
                             }
                         };

        btnInput.Accept += (s, e) =>
                           {
                               if (!tvInput.HasFocus && _keyboardStrokes.Count == 0)
                               {
                                   tvInput.SetFocus ();
                               }
                           };

        btnOutput.Accept += (s, e) =>
                            {
                                if (!tvOutput.HasFocus && _keyboardStrokes.Count == 0)
                                {
                                    tvOutput.SetFocus ();
                                }
                            };

        tvInput.SetFocus ();

        void Win_LayoutComplete (object sender, LayoutEventArgs obj)
        {
            inputHorizontalRuler.Text = outputHorizontalRuler.Text =
                                            ruler.Repeat (
                                                          (int)Math.Ceiling (
                                                                             inputHorizontalRuler.ContentArea.Width
                                                                             / (double)ruler.Length
                                                                            )
                                                         ) [
                                                            ..inputHorizontalRuler.ContentArea.Width];
            inputVerticalRuler.Height = tvInput.Frame.Height + 1;

            inputVerticalRuler.Text =
                ruler.Repeat ((int)Math.Ceiling (inputVerticalRuler.ContentArea.Height / (double)ruler.Length)) [
                     ..inputVerticalRuler.ContentArea.Height];

            outputVerticalRuler.Text =
                ruler.Repeat ((int)Math.Ceiling (outputVerticalRuler.ContentArea.Height / (double)ruler.Length)) [
                     ..outputVerticalRuler.ContentArea.Height];
        }

        Win.LayoutComplete += Win_LayoutComplete;
    }
}
