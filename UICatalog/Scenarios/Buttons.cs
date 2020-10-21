﻿using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Buttons", Description: "Demonstrates all sorts of Buttons")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Layout")]
	class Buttons : Scenario {
		public override void Setup ()
		{
			// Add a label & text field so we can demo IsDefault
			var editLabel = new Label ("TextField (to demo IsDefault):") {
				X = 0,
				Y = 0,
			};
			Win.Add (editLabel);
			// Add a TextField using Absolute layout. 
			var edit = new TextField (31, 0, 15, "");
			Win.Add (edit);

			// This is the default button (IsDefault = true); if user presses ENTER in the TextField
			// the scenario will quit
			var defaultButton = new Button ("_Quit") {
				X = Pos.Center (),
				//TODO: Change to use Pos.AnchorEnd()
				Y = Pos.Bottom (Win) - 3,
				IsDefault = true,
			};
			defaultButton.Clicked += () => Application.RequestStop ();
			Win.Add (defaultButton);

			var swapButton = new Button (50, 0, "Swap Default (Absolute Layout)");
			swapButton.Clicked += () => {
				defaultButton.IsDefault = !defaultButton.IsDefault;
				swapButton.IsDefault = !swapButton.IsDefault;
			};
			Win.Add (swapButton);

			static void DoMessage (Button button, ustring txt)
			{
				button.Clicked += () => {
					var btnText = button.Text.ToString ();
					MessageBox.Query ("Message", $"Did you click {txt}?", "Yes", "No");
				};
			}

			var colorButtonsLabel = new Label ("Color Buttons:") {
				X = 0,
				Y = Pos.Bottom (editLabel) + 1,
			};
			Win.Add (colorButtonsLabel);

			//View prev = colorButtonsLabel;

			//With this method there is no need to call Top.Ready += () => Top.Redraw (Top.Bounds);
			var x = Pos.Right (colorButtonsLabel) + 2;
			foreach (var colorScheme in Colors.ColorSchemes) {
				var colorButton = new Button ($"{colorScheme.Key}") {
					ColorScheme = colorScheme.Value,
					//X = Pos.Right (prev) + 2,
					X = x,
					Y = Pos.Y (colorButtonsLabel),
				};
				DoMessage (colorButton, colorButton.Text);
				Win.Add (colorButton);
				//prev = colorButton;
				x += colorButton.Frame.Width + 2;
			}

			Button button;
			Win.Add (button = new Button ("A super long _Button that will probably expose a bug in clipping or wrapping of text. Will it?") {
				X = 2,
				Y = Pos.Bottom (colorButtonsLabel) + 1,
			});
			DoMessage (button, button.Text);

			// Note the 'N' in 'Newline' will be the hotkey
			Win.Add (button = new Button ("a Newline\nin the button") {
				X = 2,
				Y = Pos.Bottom (button) + 1,
			});
			button.Clicked += () => MessageBox.Query ("Message", "Question?", "Yes", "No");

			var textChanger = new Button ("Te_xt Changer") {
				X = 2,
				Y = Pos.Bottom (button) + 1,
			};
			Win.Add (textChanger);
			textChanger.Clicked += () => textChanger.Text += "!";

			Win.Add (button = new Button ("Lets see if this will move as \"Text Changer\" grows") {
				X = Pos.Right (textChanger) + 2,
				Y = Pos.Y (textChanger),
			});

			var removeButton = new Button ("Remove this button") {
				X = 2,
				Y = Pos.Bottom (button) + 1,
				ColorScheme = Colors.Error
			};
			Win.Add (removeButton);
			// This in intresting test case because `moveBtn` and below are laid out relative to this one!
			removeButton.Clicked += () => Win.Remove (removeButton);

			var computedFrame = new FrameView ("Computed Layout") {
				X = 0,
				Y = Pos.Bottom (removeButton) + 1,
				Width = Dim.Percent (50),
				Height = 5
			};
			Win.Add (computedFrame);

			// Demonstrates how changing the View.Frame property can move Views
			var moveBtn = new Button ("Move This \u263b Button _via Pos") {
				X = 0,
				Y = Pos.Center () - 1,
				Width = 30,
				ColorScheme = Colors.Error,
			};
			moveBtn.Clicked += () => {
				moveBtn.X = moveBtn.Frame.X + 5;
				// This is already fixed with the call to SetNeedDisplay() in the Pos Dim.
				//computedFrame.LayoutSubviews (); // BUGBUG: This call should not be needed. View.X is not causing relayout correctly
			};
			computedFrame.Add (moveBtn);

			// Demonstrates how changing the View.Frame property can SIZE Views (#583)
			var sizeBtn = new Button ("Size This \u263a Button _via Pos") {
				X = 0,
				Y = Pos.Center () + 1,
				Width = 30,
				ColorScheme = Colors.Error,
			};
			sizeBtn.Clicked += () => {
				sizeBtn.Width = sizeBtn.Frame.Width + 5;
				//computedFrame.LayoutSubviews (); // FIXED: This call should not be needed. View.X is not causing relayout correctly
			};
			computedFrame.Add (sizeBtn);

			var absoluteFrame = new FrameView ("Absolute Layout") {
				X = Pos.Right (computedFrame),
				Y = Pos.Bottom (removeButton) + 1,
				Width = Dim.Fill (),
				Height = 5
			};
			Win.Add (absoluteFrame);

			// Demonstrates how changing the View.Frame property can move Views
			var moveBtnA = new Button (0, 0, "Move This Button via Frame") {
				ColorScheme = Colors.Error,
			};
			moveBtnA.Clicked += () => {
				moveBtnA.Frame = new Rect (moveBtnA.Frame.X + 5, moveBtnA.Frame.Y, moveBtnA.Frame.Width, moveBtnA.Frame.Height);
			};
			absoluteFrame.Add (moveBtnA);

			// Demonstrates how changing the View.Frame property can SIZE Views (#583)
			var sizeBtnA = new Button (0, 2, " ~  s  gui.cs   master ↑10 = Со_хранить") {
				ColorScheme = Colors.Error,
			};
			sizeBtnA.Clicked += () => {
				sizeBtnA.Frame = new Rect (sizeBtnA.Frame.X, sizeBtnA.Frame.Y, sizeBtnA.Frame.Width + 5, sizeBtnA.Frame.Height);
			};
			absoluteFrame.Add (sizeBtnA);

			var label = new Label ("Text Alignment (changes the four buttons above): ") {
				X = 2,
				Y = Pos.Bottom (computedFrame) + 1,
			};
			Win.Add (label);

			var radioGroup = new RadioGroup (new ustring [] { "Left", "Right", "Centered", "Justified" }) {
				X = 4,
				Y = Pos.Bottom (label) + 1,
				SelectedItem = 2,
			};
			Win.Add (radioGroup);

			// Demo changing hotkey
			ustring MoveHotkey (ustring txt)
			{
				// Remove the '_'
				var runes = txt.ToRuneList ();

				var i = runes.IndexOf ('_');
				ustring start = "";
				if (i > -1) {
					start = ustring.Make (runes.GetRange (0, i));
				}
				txt = start + ustring.Make (runes.GetRange (i + 1, runes.Count - (i + 1)));

				runes = txt.ToRuneList ();

				// Move over one or go to start
				i++;
				if (i >= runes.Count) {
					i = 0;
				}

				// Slip in the '_'
				start = ustring.Make (runes.GetRange (0, i));
				return start + ustring.Make ('_') + ustring.Make (runes.GetRange (i, runes.Count - i));
			}

			var mhkb = "Click to Change th_is Button's Hotkey";
			var moveHotKeyBtn = new Button (mhkb) {
				X = 2,
				Y = Pos.Bottom (radioGroup) + 1,
				Width = Dim.Width (computedFrame) - 2,
				ColorScheme = Colors.TopLevel,
			};
			moveHotKeyBtn.Clicked += () => {
				moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text);
			};
			Win.Add (moveHotKeyBtn);

			var muhkb = ustring.Make (" ~  s  gui.cs   master ↑10 = Сохранить");
			var moveUnicodeHotKeyBtn = new Button (muhkb) {
				X = Pos.Left (absoluteFrame) + 1,
				Y = Pos.Bottom (radioGroup) + 1,
				Width = Dim.Width (absoluteFrame) - 2, // BUGBUG: Not always the width isn't calculated correctly.
				ColorScheme = Colors.TopLevel,
			};
			moveUnicodeHotKeyBtn.Clicked += () => {
				moveUnicodeHotKeyBtn.Text = MoveHotkey (moveUnicodeHotKeyBtn.Text);
			};
			Win.Add (moveUnicodeHotKeyBtn);

			radioGroup.SelectedItemChanged += (args) => {
				switch (args.SelectedItem) {
				case 0:
					moveBtn.TextAlignment = TextAlignment.Left;
					sizeBtn.TextAlignment = TextAlignment.Left;
					moveBtnA.TextAlignment = TextAlignment.Left;
					sizeBtnA.TextAlignment = TextAlignment.Left;
					moveHotKeyBtn.TextAlignment = TextAlignment.Left;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Left;
					break;
				case 1:
					moveBtn.TextAlignment = TextAlignment.Right;
					sizeBtn.TextAlignment = TextAlignment.Right;
					moveBtnA.TextAlignment = TextAlignment.Right;
					sizeBtnA.TextAlignment = TextAlignment.Right;
					moveHotKeyBtn.TextAlignment = TextAlignment.Right;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Right;
					break;
				case 2:
					moveBtn.TextAlignment = TextAlignment.Centered;
					sizeBtn.TextAlignment = TextAlignment.Centered;
					moveBtnA.TextAlignment = TextAlignment.Centered;
					sizeBtnA.TextAlignment = TextAlignment.Centered;
					moveHotKeyBtn.TextAlignment = TextAlignment.Centered;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Centered;
					break;
				case 3:
					moveBtn.TextAlignment = TextAlignment.Justified;
					sizeBtn.TextAlignment = TextAlignment.Justified;
					moveBtnA.TextAlignment = TextAlignment.Justified;
					sizeBtnA.TextAlignment = TextAlignment.Justified;
					moveHotKeyBtn.TextAlignment = TextAlignment.Justified;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Justified;
					break;
				}
			};

			Top.Ready += () => radioGroup.Refresh ();
		}
	}
}