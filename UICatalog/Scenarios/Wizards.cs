using System;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Wizards", "Demonstrates the Wizard class")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Wizards")]
[ScenarioCategory ("Runnable")]

public class Wizards : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = GetQuitKeyAndName () };

        var frame = new FrameView
        {
            X = Pos.Center (),
            Y = 0,
            Width = Dim.Percent (75),
            ColorScheme = Colors.ColorSchemes ["Base"],
            Title = "Wizard Options"
        };
        win.Add (frame);

        var label = new Label { X = 0, Y = 0, TextAlignment = Alignment.End, Text = "_Width:", Width = 10 };
        frame.Add (label);

        var widthEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "80"
        };
        frame.Add (widthEdit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Height:"
        };
        frame.Add (label);

        var heightEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "20"
        };
        frame.Add (heightEdit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Title:"
        };
        frame.Add (label);

        var titleEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 1,
            Text = "Gandolf"
        };
        frame.Add (titleEdit);

        void Win_Loaded (object sender, EventArgs args)
        {
            frame.Height = widthEdit.Frame.Height + heightEdit.Frame.Height + titleEdit.Frame.Height + 2;
            win.Loaded -= Win_Loaded;
        }

        win.Loaded += Win_Loaded;

        label = new ()
        {
            X = Pos.Center (), Y = Pos.AnchorEnd (1), TextAlignment = Alignment.End, Text = "Action:"
        };
        win.Add (label);

        var actionLabel = new Label
        {
            X = Pos.Right (label), Y = Pos.AnchorEnd (1), ColorScheme = Colors.ColorSchemes ["Error"]
        };
        win.Add (actionLabel);

        var showWizardButton = new Button
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 2, IsDefault = true, Text = "_Show Wizard"
        };

        showWizardButton.Accepting += (s, e) =>
                                   {
                                       try
                                       {
                                           var width = 0;
                                           int.TryParse (widthEdit.Text, out width);
                                           var height = 0;
                                           int.TryParse (heightEdit.Text, out height);

                                           if (width < 1 || height < 1)
                                           {
                                               MessageBox.ErrorQuery (
                                                                      "Nope",
                                                                      "Height and width must be greater than 0 (much bigger)",
                                                                      "Ok"
                                                                     );

                                               return;
                                           }

                                           actionLabel.Text = string.Empty;

                                           var wizard = new Wizard { Title = titleEdit.Text, Width = width, Height = height };

                                           wizard.MovingBack += (s, args) =>
                                                                {
                                                                    //args.Cancel = true;
                                                                    actionLabel.Text = "Moving Back";
                                                                };

                                           wizard.MovingNext += (s, args) =>
                                                                {
                                                                    //args.Cancel = true;
                                                                    actionLabel.Text = "Moving Next";
                                                                };

                                           wizard.Finished += (s, args) =>
                                                              {
                                                                  //args.Cancel = true;
                                                                  actionLabel.Text = "Finished";
                                                              };

                                           wizard.Cancelled += (s, args) =>
                                                               {
                                                                   //args.Cancel = true;
                                                                   actionLabel.Text = "Cancelled";
                                                               };

                                           // Add 1st step
                                           var firstStep = new WizardStep { Title = "End User License Agreement" };
                                           firstStep.NextButtonText = "Accept!";

                                           firstStep.HelpText =
                                               "This is the End User License Agreement.\n\n\n\n\n\nThis is a test of the emergency broadcast system. This is a test of the emergency broadcast system.\nThis is a test of the emergency broadcast system.\n\n\nThis is a test of the emergency broadcast system.\n\nThis is a test of the emergency broadcast system.\n\n\n\nThe end of the EULA.";

                                           RadioGroup radioGroup = new ()
                                           {
                                               RadioLabels = ["_One", "_Two", "_3"]
                                           };
                                           firstStep.Add (radioGroup);

                                           wizard.AddStep (firstStep);

                                           // Add 2nd step
                                           var secondStep = new WizardStep { Title = "Second Step" };
                                           wizard.AddStep (secondStep);

                                           secondStep.HelpText =
                                               "This is the help text for the Second Step.\n\nPress the button to change the Title.\n\nIf First Name is empty the step will prevent moving to the next step.";

                                           var buttonLbl = new Label { Text = "Second Step Button: ", X = 1, Y = 1 };

                                           var button = new Button
                                           {
                                               Text = "Press Me to Rename Step", X = Pos.Right (buttonLbl), Y = Pos.Top (buttonLbl)
                                           };

                                           RadioGroup radioGroup2 = new ()
                                           {
                                               RadioLabels = ["_A", "_B", "_C"],
                                               Orientation = Orientation.Horizontal
                                           };
                                           secondStep.Add (radioGroup2);

                                           button.Accepting += (s, e) =>
                                                            {
                                                                secondStep.Title = "2nd Step";

                                                                MessageBox.Query (
                                                                                  "Wizard Scenario",
                                                                                  "This Wizard Step's title was changed to '2nd Step'"
                                                                                 );
                                                            };
                                           secondStep.Add (buttonLbl, button);
                                           var lbl = new Label { Text = "First Name: ", X = 1, Y = Pos.Bottom (buttonLbl) };

                                           var firstNameField =
                                               new TextField { Text = "Number", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl) };
                                           secondStep.Add (lbl, firstNameField);
                                           lbl = new () { Text = "Last Name:  ", X = 1, Y = Pos.Bottom (lbl) };
                                           var lastNameField = new TextField { Text = "Six", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl) };
                                           secondStep.Add (lbl, lastNameField);

                                           var thirdStepEnabledCeckBox = new CheckBox
                                           {
                                               Text = "Enable Step _3",
                                               CheckedState = CheckState.UnChecked,
                                               X = Pos.Left (lastNameField),
                                               Y = Pos.Bottom (lastNameField)
                                           };
                                           secondStep.Add (thirdStepEnabledCeckBox);

                                           // Add a frame 
                                           var frame = new FrameView
                                           {
                                               X = 0,
                                               Y = Pos.Bottom (thirdStepEnabledCeckBox) + 2,
                                               Width = Dim.Fill (),
                                               Height = 4,
                                               Title = "A Broken Frame (by Depeche Mode)",
                                               TabStop = TabBehavior.NoStop
                                           };
                                           frame.Add (new TextField { Text = "This is a TextField inside of the frame." });
                                           secondStep.Add (frame);

                                           wizard.StepChanging += (s, args) =>
                                                                  {
                                                                      if (args.OldStep == secondStep && string.IsNullOrEmpty (firstNameField.Text))
                                                                      {
                                                                          args.Cancel = true;

                                                                          int btn = MessageBox.ErrorQuery (
                                                                               "Second Step",
                                                                               "You must enter a First Name to continue",
                                                                               "Ok"
                                                                              );
                                                                      }
                                                                  };

                                           // Add 3rd (optional) step
                                           var thirdStep = new WizardStep { Title = "Third Step (Optional)" };
                                           wizard.AddStep (thirdStep);

                                           thirdStep.HelpText =
                                               "This is step is optional (WizardStep.Enabled = false). Enable it with the checkbox in Step 2.";
                                           var step3Label = new Label { Text = "This step is optional.", X = 0, Y = 0 };
                                           thirdStep.Add (step3Label);
                                           var progLbl = new Label { Text = "Third Step ProgressBar: ", X = 1, Y = 10 };

                                           var progressBar = new ProgressBar
                                           {
                                               X = Pos.Right (progLbl), Y = Pos.Top (progLbl), Width = 40, Fraction = 0.42F
                                           };
                                           thirdStep.Add (progLbl, progressBar);
                                           thirdStep.Enabled = thirdStepEnabledCeckBox.CheckedState == CheckState.Checked;
                                           thirdStepEnabledCeckBox.CheckedStateChanged += (s, e) => { thirdStep.Enabled = thirdStepEnabledCeckBox.CheckedState == CheckState.Checked; };

                                           // Add 4th step
                                           var fourthStep = new WizardStep { Title = "Step Four" };
                                           wizard.AddStep (fourthStep);

                                           var someText = new TextView
                                           {
                                               Text =
                                                   "This step (Step Four) shows how to show/hide the Help pane. The step contains this TextView (but it's hard to tell it's a TextView because of Issue #1800).",
                                               X = 0,
                                               Y = 0,
                                               Width = Dim.Fill (),
                                               WordWrap = true,
                                               AllowsTab = false,
                                               ColorScheme = Colors.ColorSchemes ["Base"]
                                           };

                                           someText.Height = Dim.Fill (
                                                                       Dim.Func (
                                                                                 () => someText.SuperView is { IsInitialized: true }
                                                                                           ? someText.SuperView.Subviews
                                                                                                     .First (view => view.Y.Has<PosAnchorEnd> (out _))
                                                                                                     .Frame.Height
                                                                                           : 1));
                                           var help = "This is helpful.";
                                           fourthStep.Add (someText);

                                           var hideHelpBtn = new Button
                                           {
                                               Text = "Press me to show/hide help",
                                               X = Pos.Center (),
                                               Y = Pos.AnchorEnd ()
                                           };

                                           hideHelpBtn.Accepting += (s, e) =>
                                                                 {
                                                                     if (fourthStep.HelpText.Length > 0)
                                                                     {
                                                                         fourthStep.HelpText = string.Empty;
                                                                     }
                                                                     else
                                                                     {
                                                                         fourthStep.HelpText = help;
                                                                     }
                                                                 };
                                           fourthStep.Add (hideHelpBtn);
                                           fourthStep.NextButtonText = "_Go To Last Step";
                                           //var scrollBar = new ScrollBarView (someText, true);

                                           //scrollBar.ChangedPosition += (s, e) =>
                                           //                             {
                                           //                                 someText.TopRow = scrollBar.Position;

                                           //                                 if (someText.TopRow != scrollBar.Position)
                                           //                                 {
                                           //                                     scrollBar.Position = someText.TopRow;
                                           //                                 }

                                           //                                 someText.SetNeedsDraw ();
                                           //                             };

                                           //someText.DrawingContent += (s, e) =>
                                           //                        {
                                           //                            scrollBar.Size = someText.Lines;
                                           //                            scrollBar.Position = someText.TopRow;

                                           //                            if (scrollBar.OtherScrollBarView != null)
                                           //                            {
                                           //                                scrollBar.OtherScrollBarView.Size = someText.Maxlength;
                                           //                                scrollBar.OtherScrollBarView.Position = someText.LeftColumn;
                                           //                            }
                                           //                        };
                                           //fourthStep.Add (scrollBar);

                                           // Add last step
                                           var lastStep = new WizardStep { Title = "The last step" };
                                           wizard.AddStep (lastStep);

                                           lastStep.HelpText =
                                               "The wizard is complete!\n\nPress the Finish button to continue.\n\nPressing ESC will cancel the wizard.";

                                           var finalFinalStepEnabledCeckBox =
                                               new CheckBox { Text = "Enable _Final Final Step", CheckedState = CheckState.UnChecked, X = 0, Y = 1 };
                                           lastStep.Add (finalFinalStepEnabledCeckBox);

                                           // Add an optional FINAL last step
                                           var finalFinalStep = new WizardStep { Title = "The VERY last step" };
                                           wizard.AddStep (finalFinalStep);

                                           finalFinalStep.HelpText =
                                               "This step only shows if it was enabled on the other last step.";
                                           finalFinalStep.Enabled = thirdStepEnabledCeckBox.CheckedState == CheckState.Checked;

                                           finalFinalStepEnabledCeckBox.CheckedStateChanged += (s, e) =>
                                                                                   {
                                                                                       finalFinalStep.Enabled = finalFinalStepEnabledCeckBox.CheckedState == CheckState.Checked;
                                                                                   };

                                           Application.Run (wizard);
                                           wizard.Dispose ();
                                       }
                                       catch (FormatException)
                                       {
                                           actionLabel.Text = "Invalid Options";
                                       }
                                   };
        win.Add (showWizardButton);

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }

    private void Wizard_StepChanged (object sender, StepChangeEventArgs e)
    {
        throw new NotImplementedException ();
    }
}
