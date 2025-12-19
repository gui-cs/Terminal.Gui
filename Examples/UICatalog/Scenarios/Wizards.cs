// ReSharper disable AccessToDisposedClosure
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
        using IApplication app = Application.Instance;
        using Window win = new ();
        win.Title = GetQuitKeyAndName ();

        FrameView settingsFrame = new ()
        {
            X = Pos.Center (),
            Y = 0,
            Width = Dim.Percent (75),
            Height = Dim.Auto (),
            Title = "Wizard Options"
        };
        win.Add (settingsFrame);

        Label label = new ()
        {
            X = 0, Y = 0, TextAlignment = Alignment.End, Text = "_Width:", Width = 10
        };
        settingsFrame.Add (label);

        TextField widthEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "80"
        };
        settingsFrame.Add (widthEdit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Height:"
        };
        settingsFrame.Add (label);

        TextField heightEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "20"
        };
        settingsFrame.Add (heightEdit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Title:"
        };
        settingsFrame.Add (label);

        TextField titleEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 1,
            Text = "Gandolf"
        };
        settingsFrame.Add (titleEdit);

        label = new ()
        {
            X = Pos.Center (), Y = Pos.AnchorEnd (1), TextAlignment = Alignment.End, Text = "Action:"
        };
        win.Add (label);

        Label actionLabel = new ()
        {
            X = Pos.Right (label), Y = Pos.AnchorEnd (1), SchemeName = "Error"
        };
        win.Add (actionLabel);

        Button showWizardButton = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (settingsFrame) + 2, IsDefault = true, Text = "_Show Wizard"
        };

        void ShowWizard (object s, CommandEventArgs e)
        {
            if (!int.TryParse (widthEdit.Text, out int width))
            {
                MessageBox.ErrorQuery (
                   (s as View)?.App!,
                   "Nope",
                   "Width must be a valid number",
                   "Ok");
                return;
            }
            if (!int.TryParse (heightEdit.Text, out int height))
            {
                MessageBox.ErrorQuery (
                   (s as View)?.App!,
                   "Nope",
                   "Height must be a valid number",
                   "Ok");
                return;
            }

            if (width < 1 || height < 1)
            {
                MessageBox.ErrorQuery (
                                       (s as View)?.App!,
                                       "Nope",
                                       "Height and width must be greater than 0 (much bigger)",
                                       "Ok");

                return;
            }

            actionLabel.Text = string.Empty;

            Wizard wizard = new () { Title = titleEdit.Text };

            wizard.MovingBack += (_, args) =>
                                 {
                                     //args.Cancel = true;
                                     actionLabel.Text = "Moving Back";
                                 };

            wizard.MovingNext += (_, args) =>
                                 {
                                     //args.Cancel = true;
                                     actionLabel.Text = "Moving Next";
                                 };

            wizard.Finished += (_, args) =>
                               {
                                   //args.Cancel = true;
                                   actionLabel.Text = "Finished";
                               };

            wizard.Cancelled += (_, args) =>
                                {
                                    //args.Cancel = true;
                                    actionLabel.Text = "Cancelled";
                                };

            // Add 1st step
            WizardStep firstStep = new () { Title = "End User License Agreement" };
            firstStep.NextButtonText = "Accept!";

            firstStep.HelpText = "This is the End User License Agreement.\n\n\n\n\n\nThis is a test of the emergency broadcast system. This is a test of the emergency broadcast system.\nThis is a test of the emergency broadcast system.\n\n\nThis is a test of the emergency broadcast system.\n\nThis is a test of the emergency broadcast system.\n\n\n\nThe end of the EULA.";

            OptionSelector optionSelector = new ()
            {
                Labels = ["_One", "_Two", "_3"]
            };
            firstStep.Add (optionSelector);

            wizard.AddStep (firstStep);

            //// Add 2nd step
            //WizardStep secondStep = new () { Title = "Second Step" };
            //wizard.AddStep (secondStep);

            //secondStep.HelpText = "This is the help text for the Second Step.\n\nPress the button to change the Title.\n\nIf First Name is empty the step will prevent moving to the next step.";

            //Label buttonLbl = new () { Text = "Second Step Button: ", X = 1, Y = 1 };

            //Button button = new ()
            //{
            //    Text = "Press Me to Rename Step", X = Pos.Right (buttonLbl), Y = Pos.Top (buttonLbl)
            //};

            //OptionSelector optionSelector2 = new ()
            //{
            //    Labels = ["_A", "_B", "_C"], Orientation = Orientation.Horizontal
            //};
            //secondStep.Add (optionSelector2);

            //button.Accepting += (sender, _) =>
            //                    {
            //                        secondStep.Title = "2nd Step";

            //                        MessageBox.Query (
            //                                          (sender as View)?.App!,
            //                                          "Wizard Scenario",
            //                                          "This Wizard Step's title was changed to '2nd Step'");
            //                    };
            //secondStep.Add (buttonLbl, button);
            //Label lbl = new ()
            //{
            //    Text = "First Name: ", X = 1, Y = Pos.Bottom (buttonLbl)
            //};

            //TextField firstNameField = new ()
            //{
            //    Text = "Number", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl)
            //};
            //secondStep.Add (lbl, firstNameField);
            //lbl = new ()
            //{
            //    Text = "Last Name:  ", X = 1, Y = Pos.Bottom (lbl)
            //};
            //TextField lastNameField = new ()
            //{
            //    Text = "Six", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl)
            //};
            //secondStep.Add (lbl, lastNameField);

            //CheckBox thirdStepEnabledCheckBox = new ()
            //{
            //    Text = "Enable Step _3", CheckedState = CheckState.UnChecked, X = Pos.Left (lastNameField), Y = Pos.Bottom (lastNameField)
            //};
            //secondStep.Add (thirdStepEnabledCheckBox);

            //// Add a frame
            //FrameView frame = new ()
            //{
            //    X = 0,
            //    Y = Pos.Bottom (thirdStepEnabledCheckBox) + 2,
            //    Width = Dim.Fill (),
            //    Height = 4,
            //    Title = "A Broken Frame (by Depeche Mode)",
            //    TabStop = TabBehavior.NoStop
            //};
            //frame.Add (new TextField
            //{
            //    Text = "This is a TextField inside of the frame."
            //});
            //secondStep.Add (frame);

            //wizard.StepChanging += (_, args) =>
            //                       {
            //                           if (args.OldStep != secondStep || !string.IsNullOrEmpty (firstNameField.Text))
            //                           {
            //                               return;
            //                           }

            //                           args.Cancel = true;

            //                           int? btn = MessageBox.ErrorQuery (
            //                                                             (s as View)?.App!,
            //                                                             "Second Step",
            //                                                             "You must enter a First Name to continue",
            //                                                             "Ok");
            //                       };

            //// Add 3rd (optional) step
            //WizardStep thirdStep = new () { Title = "Third Step (Optional)" };
            //wizard.AddStep (thirdStep);

            //thirdStep.HelpText = "This is step is optional (WizardStep.Enabled = false). Enable it with the checkbox in Step 2.";
            //Label step3Label = new ()
            //{
            //    Text = "This step is optional.", X = 0, Y = 0
            //};
            //thirdStep.Add (step3Label);
            //Label progLbl = new ()
            //{
            //    Text = "Third Step ProgressBar: ", X = 1, Y = 10
            //};

            //ProgressBar progressBar = new ()
            //{
            //    X = Pos.Right (progLbl),
            //    Y = Pos.Top (progLbl),
            //    Width = 40,
            //    Fraction = 0.42F
            //};
            //thirdStep.Add (progLbl, progressBar);
            //thirdStep.Enabled = thirdStepEnabledCheckBox.CheckedState == CheckState.Checked;

            //thirdStepEnabledCheckBox.CheckedStateChanged += (_, _) =>
            //                                                {
            //                                                    thirdStep.Enabled = thirdStepEnabledCheckBox.CheckedState == CheckState.Checked;
            //                                                };

            //// Add 4th step
            //WizardStep fourthStep = new () { Title = "Step Four" };
            //wizard.AddStep (fourthStep);

            //TextView someText = new ()
            //{
            //    Text = "This step (Step Four) shows how to show/hide the Help pane. The step contains this TextView (but it's hard to tell it's a TextView because of Issue #1800).",
            //    X = 0,
            //    Y = 0,
            //    Width = Dim.Fill (),
            //    WordWrap = true,
            //    AllowsTab = false,
            //    SchemeName = "Base"
            //};

            //someText.Height = Dim.Fill (
            //                            Dim.Func (v => someText.SuperView is { IsInitialized: true }
            //                                               ? someText.SuperView.SubViews.First (view => view.Y.Has<PosAnchorEnd> (out _))
            //                                                         .Frame.Height
            //                                               : 1));
            //fourthStep.Add (someText);

            //Button hideHelpBtn = new ()
            //{
            //    Text = "Press me to show/hide help", X = Pos.Center (), Y = Pos.AnchorEnd ()
            //};

            //hideHelpBtn.Accepting += (_, _) =>
            //                         {
            //                             fourthStep.HelpText = fourthStep.HelpText.Length > 0 ? string.Empty : "This is helpful.";
            //                         };
            //fourthStep.Add (hideHelpBtn);
            //fourthStep.NextButtonText = "_Go To Last Step";

            ////var scrollBar = new ScrollBarView (someText, true);

            ////scrollBar.ChangedPosition += (s, e) =>
            ////                             {
            ////                                 someText.TopRow = scrollBar.Position;

            ////                                 if (someText.TopRow != scrollBar.Position)
            ////                                 {
            ////                                     scrollBar.Position = someText.TopRow;
            ////                                 }

            ////                                 someText.SetNeedsDraw ();
            ////                             };

            ////someText.DrawingContent += (s, e) =>
            ////                        {
            ////                            scrollBar.Size = someText.Lines;
            ////                            scrollBar.Position = someText.TopRow;

            ////                            if (scrollBar.OtherScrollBarView != null)
            ////                            {
            ////                                scrollBar.OtherScrollBarView.Size = someText.Maxlength;
            ////                                scrollBar.OtherScrollBarView.Position = someText.LeftColumn;
            ////                            }
            ////                        };
            ////fourthStep.Add (scrollBar);

            //// Add last step
            //WizardStep lastStep = new () { Title = "The last step" };
            //wizard.AddStep (lastStep);

            //lastStep.HelpText = "The wizard is complete!\n\nPress the Finish button to continue.\n\nPressing ESC will cancel the wizard.";

            //CheckBox finalFinalStepEnabledCheckBox = new () { Text = "Enable _Final Final Step", CheckedState = CheckState.UnChecked, X = 0, Y = 1 };
            //lastStep.Add (finalFinalStepEnabledCheckBox);

            //// Add an optional FINAL last step
            //WizardStep finalFinalStep = new () { Title = "The VERY last step" };
            //wizard.AddStep (finalFinalStep);

            //finalFinalStep.HelpText = "This step only shows if it was enabled on the other last step.";
            //finalFinalStep.Enabled = thirdStepEnabledCheckBox.CheckedState == CheckState.Checked;

            //finalFinalStepEnabledCheckBox.CheckedStateChanged += (_, _) =>
            //                                                    {
            //                                                        finalFinalStep.Enabled = finalFinalStepEnabledCheckBox.CheckedState
            //                                                                                 == CheckState.Checked;
            //                                                    };

            app.Run (wizard);
            wizard.Dispose ();

        }

        showWizardButton.Accepting += ShowWizard;
        win.Add (showWizardButton);

        app.Run (win);
    }
}
