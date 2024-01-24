using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.ConsoleDrivers;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("VkeyPacketSimulator", "Simulates the Virtual Key Packet")]
[ScenarioCategory ("Mouse and Keyboard")]
public class VkeyPacketSimulator : Scenario {
	List<KeyCode> _keyboardStrokes = new ();
	bool _outputStarted = false;
	bool _wasUnknown = false;
	static ManualResetEventSlim _stopOutput = new (false);

	public override void Setup ()
	{
		var label = new Label ("Input") {
			X = Pos.Center ()
		};
		Win.Add (label);

		var btnInput = new Button ("Select Input") {
			X = Pos.AnchorEnd (16)
		};
		Win.Add (btnInput);

		const string ruler = "|123456789";

		var inputHorizontalRuler = new Label ("") {
			Y = Pos.Bottom (btnInput),
			Width = Dim.Fill (),
			ColorScheme = Colors.ColorSchemes ["Error"],
			AutoSize = false
		};
		Win.Add (inputHorizontalRuler);

		var inputVerticalRuler = new Label ("", TextDirection.TopBottom_LeftRight) {
			Y = Pos.Bottom (btnInput),
			Width = 1,
			ColorScheme = Colors.ColorSchemes ["Error"],
			AutoSize = false
		};
		Win.Add (inputVerticalRuler);

		var tvInput = new TextView {
			Title = "Input",
			X = 1,
			Y = Pos.Bottom (inputHorizontalRuler),
			Width = Dim.Fill (),
			Height = Dim.Percent (50) - 1
		};
		Win.Add (tvInput);

		label = new Label ("Output") {
			X = Pos.Center (),
			Y = Pos.Bottom (tvInput)
		};
		Win.Add (label);

		var btnOutput = new Button ("Select Output") {
			X = Pos.AnchorEnd (17),
			Y = Pos.Top (label)
		};
		Win.Add (btnOutput);

		var outputHorizontalRuler = new Label ("") {
			Y = Pos.Bottom (btnOutput),
			Width = Dim.Fill (),
			ColorScheme = Colors.ColorSchemes ["Error"],
			AutoSize = false
		};
		Win.Add (outputHorizontalRuler);

		var outputVerticalRuler = new Label ("", TextDirection.TopBottom_LeftRight) {
			Y = Pos.Bottom (btnOutput),
			Width = 1,
			Height = Dim.Fill (),
			ColorScheme = Colors.ColorSchemes ["Error"],
			AutoSize = false
		};
		Win.Add (outputVerticalRuler);

		var tvOutput = new TextView {
			Title = "Output",
			X = 1,
			Y = Pos.Bottom (outputHorizontalRuler),
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			ReadOnly = true
		};

		// Detect unknown keys and reject them.
		tvOutput.KeyDown += (s, e) => {
			//System.Diagnostics.Debug.WriteLine ($"Output - KeyDown: {e.KeyCode}");
			if (e.NoAlt.NoCtrl.NoShift == KeyCode.Null) {
				_wasUnknown = true;
				e.Handled = true;
				return;
			}

			//System.Diagnostics.Debug.WriteLine ($"Output - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");
			if (_outputStarted) {
				// If the key wasn't handled by the TextView will popup a Dialog with the keys pressed.
				var handled = tvOutput.OnInvokingKeyBindings (e);
				if (handled == null || handled == false) {
					if (!tvOutput.OnProcessKeyDown (e)) {
						Application.Invoke (() => MessageBox.Query ("Keys", $"'{Key.ToString (e.KeyCode, MenuBar.ShortcutDelimiter)}' pressed!", "Ok"));
					}
				}
			}
			e.Handled = true;
			_stopOutput.Set ();
		};

		Win.Add (tvOutput);

		tvInput.KeyDown += (s, e) => {
			//System.Diagnostics.Debug.WriteLine ($"Input - KeyDown: {e.KeyCode.Key}");
			if (e.KeyCode == Key.Empty) {
				_wasUnknown = true;
				e.Handled = true;
			} else {
				_wasUnknown = false;
			}
		};

		tvInput.InvokingKeyBindings += (s, e) => {
			var ev = e;
			//System.Diagnostics.Debug.WriteLine ($"Input - KeyPress: {ev}");
			//System.Diagnostics.Debug.WriteLine ($"Input - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");

			if (!e.IsValid) {
				_wasUnknown = true;
				e.Handled = true;
				return;
			}

			_keyboardStrokes.Add (e.KeyCode);
		};

		tvInput.KeyUp += (s, e) => {
			//System.Diagnostics.Debug.WriteLine ($"Input - KeyUp: {e.Key}");
			e.Handled = true;
			if (!_wasUnknown && _keyboardStrokes.Count > 0) {
				_outputStarted = true;
				tvOutput.ReadOnly = false;
				tvOutput.SetFocus ();
				tvOutput.SetNeedsDisplay ();

				Task.Run (() => {
					while (_outputStarted) {
						try {
							while (_keyboardStrokes.Count > 0) {
								if (_keyboardStrokes [0] == KeyCode.Null) {
									continue;
								}
								var consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (_keyboardStrokes [0]);
								var keyChar = ConsoleKeyMapping.EncodeKeyCharForVKPacket (consoleKeyInfo);
								Application.Driver.SendKeys (keyChar, ConsoleKey.Packet, consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Shift),
									consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt), consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Control));

								_stopOutput.Wait ();
								_stopOutput.Reset ();
								_keyboardStrokes.RemoveAt (0);
								Application.Invoke (() => {
									tvOutput.ReadOnly = true;
									tvInput.SetFocus ();
								});
							}
							_outputStarted = false;

						} catch (Exception) {
							Application.Invoke (() => {
								MessageBox.ErrorQuery ("Error", "Couldn't send the keystrokes!", "Ok");
								Application.RequestStop ();
							});
						}
					}
					//System.Diagnostics.Debug.WriteLine ($"_outputStarted: {_outputStarted}");
				});
			}
		};

		btnInput.Clicked += (s, e) => {
			if (!tvInput.HasFocus && _keyboardStrokes.Count == 0) {
				tvInput.SetFocus ();
			}
		};

		btnOutput.Clicked += (s, e) => {
			if (!tvOutput.HasFocus && _keyboardStrokes.Count == 0) {
				tvOutput.SetFocus ();
			}
		};

		tvInput.SetFocus ();

		void Win_LayoutComplete (object sender, LayoutEventArgs obj)
		{
			inputHorizontalRuler.Text = outputHorizontalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)inputHorizontalRuler.Bounds.Width / (double)ruler.Length)) [0..inputHorizontalRuler.Bounds.Width];
			inputVerticalRuler.Height = tvInput.Frame.Height + 1;
			inputVerticalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)inputVerticalRuler.Bounds.Height / (double)ruler.Length)) [0..inputVerticalRuler.Bounds.Height];
			outputVerticalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)outputVerticalRuler.Bounds.Height / (double)ruler.Length)) [0..outputVerticalRuler.Bounds.Height];
		}

		Win.LayoutComplete += Win_LayoutComplete;
	}
}
