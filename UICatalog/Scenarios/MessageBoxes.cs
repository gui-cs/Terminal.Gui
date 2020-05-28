﻿using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "MessageBoxes", Description: "Demonstrates how to use MessageBoxes")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Bug Repro")]
	class MessageBoxes : Scenario {
		public override void Setup ()
		{
			var frame = new FrameView ("MessageBox Options") {
				X = Pos.Center(),
				Y = 1,
				Width = Dim.Percent(75),
				Height = 10
			};
			Win.Add (frame);

			var label = new Label ("Width:") {
				X = 0,
				Y = 0,
				Width = 15,
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var widthEdit = new TextField ("50") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (widthEdit);

			label = new Label ("Height:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width(label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var heightEdit = new TextField ("6") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (heightEdit);

			label = new Label ("Title:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var titleEdit = new TextField ("Title") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = Dim.Fill(),
				Height = 1
			};
			frame.Add (titleEdit);

			label = new Label ("Message:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var messageEdit = new TextField ("Message") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = Dim.Fill (),
				Height = 1
			};
			frame.Add (messageEdit);

			label = new Label ("Num Buttons:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var numButtonsEdit = new TextField ("3") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (numButtonsEdit);

			label = new Label ("Style:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var styleRadioGroup = new RadioGroup (new [] { "_Query", "_Error" } ) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5, // BUGBUG: This should cause clipping!
			};
			frame.Add (styleRadioGroup);

			frame.Height = Dim.Height (widthEdit) + Dim.Height (heightEdit) + Dim.Height (titleEdit) + Dim.Height (messageEdit) 
				+ Dim.Height(numButtonsEdit) + Dim.Height (styleRadioGroup) + 2;

			label = new Label ("Button Pressed:") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 2,
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			Win.Add (label);
			var buttonPressedLabel = new Label ("") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 4,
				Width = 25,
				Height = 1,
				ColorScheme = Colors.Error,
			};

			// BUGBUG: Why is this button not centered???
			var showMessageBoxButton = new Button ("Show MessageBox") {
				X = Pos.Center(),
				Y = Pos.Bottom (frame) + 2			,
				IsDefault = true,
				Clicked = () => {
					try {
						int width = int.Parse (widthEdit.Text.ToString ());
						int height = int.Parse (heightEdit.Text.ToString ());
						int numButtons = int.Parse (numButtonsEdit.Text.ToString ());
						var btns = new List<string> ();
						for (int i = 0; i < numButtons; i++) {
							btns.Add($"Btn {i}");
						}
						if (styleRadioGroup.Selected == 0) {
							buttonPressedLabel.Text = $"{MessageBox.Query (width, height, titleEdit.Text.ToString (), messageEdit.Text.ToString (), btns.ToArray ())}";
						} else {
							buttonPressedLabel.Text = $"{MessageBox.ErrorQuery (width, height, titleEdit.Text.ToString (), messageEdit.Text.ToString (), btns.ToArray ())}";
						}
					} catch {
						buttonPressedLabel.Text = "Invalid Options";
					}
				},
			};
			Win.Add (showMessageBoxButton);
			Win.Add (buttonPressedLabel);
		}
	}
}
