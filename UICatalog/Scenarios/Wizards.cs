using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Wizards", Description: "Demonstrates how to the Wizard class")]
	[ScenarioCategory ("Dialogs")]
	public class Wizards : Scenario {
		public override void Setup ()
		{
			Win.ColorScheme = Colors.Base;
			var frame = new FrameView ("Wizard Options") {
				X = Pos.Center (),
				Y = 0,
				Width = Dim.Percent (75),
				Height = 10,
				ColorScheme = Colors.Base,
			};
			Win.Add (frame);

			var label = new Label ("width:") {
				X = 0,
				Y = 0,
				Width = 15,
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var widthEdit = new TextField ("0") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (widthEdit);

			label = new Label ("height:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var heightEdit = new TextField ("0") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (heightEdit);

			frame.Add (new Label ("If height & width are both 0,") {
				X = Pos.Right (widthEdit) + 2,
				Y = Pos.Top (widthEdit),
			});
			frame.Add (new Label ("the Wizard will size to 80% of container.") {
				X = Pos.Right (heightEdit) + 2,
				Y = Pos.Top (heightEdit),
			});

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
				Width = Dim.Fill (),
				Height = 1
			};
			frame.Add (titleEdit);

			void Top_Loaded ()
			{
				frame.Height = Dim.Height (widthEdit) + Dim.Height (heightEdit) + Dim.Height (titleEdit) + 2;
				Top.Loaded -= Top_Loaded;
			}
			Top.Loaded += Top_Loaded;

			label = new Label ("Action:") {
				X = Pos.Center (),
				Y = Pos.AnchorEnd (1),
				AutoSize = true,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			Win.Add (label);
			var actionLabel = new Label (" ") {
				X = Pos.Right (label),
				Y = Pos.AnchorEnd (1),
				AutoSize = true,
				ColorScheme = Colors.Error,
			};

			var showWizardButton = new Button ("Show Wizard") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 2,
				IsDefault = true,
			};
			showWizardButton.Clicked += () => {
				try {
					int width = int.Parse (widthEdit.Text.ToString ());
					int height = int.Parse (heightEdit.Text.ToString ());

					var wizard = new Wizard ();
					wizard.Title = titleEdit.Text;
					wizard.MovingBack += (args) => {
						//args.Cancel = true;
						actionLabel.Text = "Moving Back";
					};

					wizard.MovingNext += (args) => {
						//args.Cancel = true;
						actionLabel.Text = "Moving Next";
					};

					wizard.Finished += (args) => {
						//args.Cancel = true;
						actionLabel.Text = "Finished";
					};

					// Add 1st step
					var firstStep = new Wizard.WizardStep ("End User License Agreement");
					wizard.AddStep (firstStep);
					firstStep.ShowControls = false;
					firstStep.NextButtonText = "Accept";
					firstStep.HelpText = "This is the End User License Agreement.\nThis is a test of the emergency broadcast system. This is a test of the emergency broadcast system. This is a test of the emergency broadcast system.\n\n\n\n\n\n\nTHe end of the EULA.";

					// Add 2nd step
					var secondStep = new Wizard.WizardStep ("Second Step");
					wizard.AddStep (secondStep);
					secondStep.HelpText = "This is the help text for the Second Step.\n\nPress the button to see a message box.\n\nEnter name too.";
					var buttonLbl = new Label () { Text = "Second Step Button: ", AutoSize = true, X = 1, Y = 1 };
					var button = new Button () {
						Text = "Press Me",
						X = Pos.Right (buttonLbl),
						Y = Pos.Top (buttonLbl)
					};
					button.Clicked += () => {
						MessageBox.Query ("Wizard Scenario", "The Second Step Button was pressed.");
					};
					secondStep.Controls.Add (buttonLbl, button);
					var lbl = new Label () { Text = "First Name: ", AutoSize = true, X = 1, Y = Pos.Bottom (buttonLbl) };
					var firstNameField = new TextField () { Width = 30, X = Pos.Right(lbl), Y = Pos.Top (lbl) };
					secondStep.Controls.Add (lbl, firstNameField);
					lbl = new Label () { Text = "Last Name:  ", AutoSize = true, X = 1, Y = Pos.Bottom (lbl) };
					var lastNameField = new TextField () { Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl) };
					secondStep.Controls.Add (lbl, lastNameField);

					// Add 3rd step
					var thirdStep = new Wizard.WizardStep ("Third Step");
					wizard.AddStep (thirdStep);
					thirdStep.HelpText = "This is the help text for the Third Step.";
					var progLbl = new Label () { Text = "Third Step ProgressBar: ", AutoSize = true, X = 1, Y = 10 };
					var progressBar = new ProgressBar () {
						X = Pos.Right (progLbl),
						Y = Pos.Top (progLbl),
						Width = 40,
						Fraction = 0.42F
					};
					thirdStep.Controls.Add (progLbl, progressBar);

					// Add 4th step
					var fourthStep = new Wizard.WizardStep ("Hidden Help pane");
					wizard.AddStep (fourthStep);
					fourthStep.ShowHelp = false;
					var someText = new TextView () {
						Text = "This step shows how to hide the Help pane. The control pane contains this TextView.",
						X = 0,
						Y = 0,
						Width = Dim.Fill (),
						Height = Dim.Fill (),
						WordWrap = true,
					};
					fourthStep.Controls.Add (someText);
					var scrollBar = new ScrollBarView (someText, true);

					scrollBar.ChangedPosition += () => {
						someText.TopRow = scrollBar.Position;
						if (someText.TopRow != scrollBar.Position) {
							scrollBar.Position = someText.TopRow;
						}
						someText.SetNeedsDisplay ();
					};

					scrollBar.VisibleChanged += () => {
						if (scrollBar.Visible && someText.RightOffset == 0) {
							someText.RightOffset = 1;
						} else if (!scrollBar.Visible && someText.RightOffset == 1) {
							someText.RightOffset = 0;
						}
					};

					someText.DrawContent += (e) => {
						scrollBar.Size = someText.Lines;
						scrollBar.Position = someText.TopRow;
						if (scrollBar.OtherScrollBarView != null) {
							scrollBar.OtherScrollBarView.Size = someText.Maxlength;
							scrollBar.OtherScrollBarView.Position = someText.LeftColumn;
						}
						scrollBar.LayoutSubviews ();
						scrollBar.Refresh ();
					};
					fourthStep.Controls.Add (scrollBar);

					// Add last step
					var lastStep = new Wizard.WizardStep ("The last step");
					wizard.AddStep (lastStep);
					lastStep.HelpText = "The wizard is complete! Press the Finish button to continue. Pressing ESC will cancel the wizard.";
					

					// TODO: Demo setting initial Pane

					wizard.Finished += (args) => {
						Application.RequestStop (wizard);
					};

					Application.Run (wizard);

				} catch (FormatException) {
					actionLabel.Text = "Invalid Options";
				}
			};
			Win.Add (showWizardButton);

			Win.Add (actionLabel);
		}
	}
}
