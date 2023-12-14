using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.ConsoleDrivers;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "VkeyPacketSimulator", Description: "Simulates the Virtual Key Packet")]
	[ScenarioCategory ("Mouse and Keyboard")]
	public class VkeyPacketSimulator : Scenario {
		List<int> _keyboardStrokes = new List<int> ();
		bool _outputStarted = false;
		bool _wasUnknown = false;
		static ManualResetEventSlim _stopOutput = new ManualResetEventSlim (false);

		public override void Setup ()
		{
			var label = new Label ("Input") {
				X = Pos.Center ()
			};
			Win.Add (label);

			var btnInput = new Button ("Select Input") {
				X = Pos.AnchorEnd (16),
			};
			Win.Add (btnInput);

			const string ruler = "|123456789";

			var inputHorizontalRuler = new Label ("") {
				Y = Pos.Bottom (btnInput),
				Width = Dim.Fill (),
				ColorScheme = Colors.Error,
				AutoSize = false
			};
			Win.Add (inputHorizontalRuler);

			var inputVerticalRuler = new Label ("", TextDirection.TopBottom_LeftRight) {
				Y = Pos.Bottom (btnInput),
				Width = 1,
				ColorScheme = Colors.Error,
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
				ColorScheme = Colors.Error,
				AutoSize = false
			};
			Win.Add (outputHorizontalRuler);

			var outputVerticalRuler = new Label ("", TextDirection.TopBottom_LeftRight) {
				Y = Pos.Bottom (btnOutput),
				Width = 1,
				Height = Dim.Fill (),
				ColorScheme = Colors.Error,
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
				//System.Diagnostics.Debug.WriteLine ($"Output - KeyDown: {e.Key}");
				//e.Handled = true;
				if (e.ConsoleDriverKey == KeyCode.Unknown) {
					_wasUnknown = true;
				}
			};

			tvOutput.KeyUp += (s, e) => {
				//System.Diagnostics.Debug.WriteLine ($"Output - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");
				if (_outputStarted && _keyboardStrokes.Count > 0) {
					//// TODO: Tig: I don't understand what this is trying to do
					//if (!tvOutput.ProcessKeyDown (e)) {
					//	Application.Invoke (() => {
					//		MessageBox.Query ("Keys", $"'{KeyEventArgs.ToString (e.ConsoleDriverKey, MenuBar.ShortcutDelimiter)}' pressed!", "Ok");
					//	});
					//}
					e.Handled = true;
					_stopOutput.Set ();
				}
				//System.Diagnostics.Debug.WriteLine ($"Output - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");
			};

			Win.Add (tvOutput);

			KeyEventArgs unknownChar = null;

			tvInput.KeyUp += (s, e) => {
				//System.Diagnostics.Debug.WriteLine ($"Input - KeyUp: {e.Key}");
				//var ke = e;

				if (e.ConsoleDriverKey == KeyCode.Unknown) {
					_wasUnknown = true;
					e.Handled = true;
					return;
				}
				if (_wasUnknown && _keyboardStrokes.Count == 1) {
					_wasUnknown = false;
				} else if (_wasUnknown && char.IsLetter ((char)e.ConsoleDriverKey)) {
					_wasUnknown = false;
				}
				if (_keyboardStrokes.Count == 0) {
					AddKeyboardStrokes (e);
				} else {
					_keyboardStrokes.Insert (0, 0);
				}
				if (_wasUnknown && (int)e.ConsoleDriverKey - (int)(e.ConsoleDriverKey & (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.ShiftMask)) != 0) {
					unknownChar = e;
				}
				e.Handled = true;
				if (!_wasUnknown && _keyboardStrokes.Count > 0) {
					_outputStarted = true;
					tvOutput.ReadOnly = false;
					tvOutput.SetFocus ();
					tvOutput.SetNeedsDisplay ();

					Task.Run (() => {
						while (_outputStarted) {
							try {
								ConsoleModifiers mod = new ConsoleModifiers ();
								if (e.ConsoleDriverKey.HasFlag (KeyCode.ShiftMask)) {
									mod |= ConsoleModifiers.Shift;
								}
								if (e.ConsoleDriverKey.HasFlag (KeyCode.AltMask)) {
									mod |= ConsoleModifiers.Alt;
								}
								if (e.ConsoleDriverKey.HasFlag (KeyCode.CtrlMask)) {
									mod |= ConsoleModifiers.Control;
								}
								for (int i = 0; i < _keyboardStrokes.Count; i++) {
									var consoleKey = ConsoleKeyMapping.GetConsoleKeyFromKey ((uint)_keyboardStrokes [i], mod, out _, out _);
									Application.Driver.SendKeys ((char)consoleKey, ConsoleKey.Packet, mod.HasFlag (ConsoleModifiers.Shift),
										mod.HasFlag (ConsoleModifiers.Alt), mod.HasFlag (ConsoleModifiers.Control));
								}
								//}
							} catch (Exception) {
								Application.Invoke (() => {
									MessageBox.ErrorQuery ("Error", "Couldn't send the keystrokes!", "Ok");
									Application.RequestStop ();
								});
							}
							_stopOutput.Wait ();
							_stopOutput.Reset ();
							_keyboardStrokes.RemoveAt (0);
							if (_keyboardStrokes.Count == 0) {
								_outputStarted = false;
								Application.Invoke (() => {
									tvOutput.ReadOnly = true;
									tvInput.SetFocus ();
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
				inputHorizontalRuler.Text = outputHorizontalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)(inputHorizontalRuler.Bounds.Width) / (double)ruler.Length)) [0..(inputHorizontalRuler.Bounds.Width)];
				inputVerticalRuler.Height = tvInput.Frame.Height + 1;
				inputVerticalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)(inputVerticalRuler.Bounds.Height) / (double)ruler.Length)) [0..(inputVerticalRuler.Bounds.Height)];
				outputVerticalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)(outputVerticalRuler.Bounds.Height) / (double)ruler.Length)) [0..(outputVerticalRuler.Bounds.Height)];
			}

			Win.LayoutComplete += Win_LayoutComplete;
		}

		private void AddKeyboardStrokes (KeyEventArgs e)
		{
			var keyChar = (int)e.ConsoleDriverKey;
			var mK = (int)(e.ConsoleDriverKey & (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.ShiftMask));
			keyChar &= ~mK;
			_keyboardStrokes.Add (keyChar);
		}
	}
}
