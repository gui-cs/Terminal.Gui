#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("WizardAsView", "Shows using the Wizard class in an non-modal way")]
[ScenarioCategory ("Wizards")]
public class WizardAsView : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
                  new MenuBarItem (
                                   "_File",
                                   [
                                       new MenuItem
                                       {
                                           Title = "_Restart Configuration...",
                                           Action = () => MessageBox.Query (
                                                                            Application.Instance,
                                                                            "Wizard",
                                                                            "Are you sure you want to reset the Wizard and start over?",
                                                                            "Ok",
                                                                            "Cancel"
                                                                           )
                                       },
                                       new MenuItem
                                       {
                                           Title = "Re_boot Server...",
                                           Action = () => MessageBox.Query (
                                                                            Application.Instance,
                                                                            "Wizard",
                                                                            "Are you sure you want to reboot the server start over?",
                                                                            "Ok",
                                                                            "Cancel"
                                                                           )
                                       },
                                       new MenuItem
                                       {
                                           Title = "_Shutdown Server...",
                                           Action = () => MessageBox.Query (
                                                                            Application.Instance,
                                                                            "Wizard",
                                                                            "Are you sure you want to cancel setup and shutdown?",
                                                                            "Ok",
                                                                            "Cancel"
                                                                           )
                                       }
                                   ]
                                  )
                 );

        // No need for a Title because the border is disabled
        Wizard wizard = new ()
        {
            X = 0,
            Y = Pos.Bottom (menu),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ShadowStyle = ShadowStyle.None
        };

        // Set Modal to false to cause the Wizard class to render without a frame and
        // behave like an non-modal View (vs. a modal/pop-up Window).
       // wizard.Modal = false;

        wizard.MovingBack += (s, args) =>
                             {
                                 //args.Cancel = true;
                                 //actionLabel.Text = "Moving Back";
                             };

        wizard.MovingNext += (s, args) =>
                             {
                                 //args.Cancel = true;
                                 //actionLabel.Text = "Moving Next";
                             };

        wizard.Finished += (s, args) =>
                           {
                               //args.Cancel = true;
                               MessageBox.Query ((s as View)?.App!, "Setup Wizard", "Finished", "Ok");
                               Application.RequestStop ();
                           };

        wizard.Cancelled += (s, args) =>
                            {
                                int? btn = MessageBox.Query ((s as View)?.App!, "Setup Wizard", "Are you sure you want to cancel?", "Yes", "No");
                                args.Cancel = btn == 1;

                                if (btn == 0)
                                {
                                    Application.RequestStop ();
                                }
                            };

        // Add 1st step
        WizardStep firstStep = new () { Title = "End User License Agreement" };
        wizard.AddStep (firstStep);
        firstStep.NextButtonText = "Accept!";

        firstStep.HelpText =
            "This is the End User License Agreement.\n\n\n\n\n\nThis is a test of the emergency broadcast system. This is a test of the emergency broadcast system.\nThis is a test of the emergency broadcast system.\n\n\nThis is a test of the emergency broadcast system.\n\nThis is a test of the emergency broadcast system.\n\n\n\nThe end of the EULA.";

        // Add 2nd step
        WizardStep secondStep = new () { Title = "Second Step" };
        wizard.AddStep (secondStep);

        secondStep.HelpText =
            "This is the help text for the Second Step.\n\nPress the button to change the Title.\n\nIf First Name is empty the step will prevent moving to the next step.";

        Label buttonLbl = new () { Text = "Second Step Button: ", X = 0, Y = 0 };

        Button button = new ()
        {
            Text = "Press Me to Rename Step",
            X = Pos.Right (buttonLbl),
            Y = Pos.Top (buttonLbl)
        };

        button.Accepting += (s, e) =>
                            {
                                secondStep.Title = "2nd Step";

                                MessageBox.Query ((s as View)?.App!,
                                                  "Wizard Scenario",
                                                  "This Wizard Step's title was changed to '2nd Step'",
                                                  "Ok"
                                                 );
                            };
        secondStep.Add (buttonLbl, button);

        Label lbl = new () { Text = "First Name: ", X = Pos.Left (buttonLbl), Y = Pos.Bottom (buttonLbl) };
        TextField firstNameField = new () { Text = "Number", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl) };
        secondStep.Add (lbl, firstNameField);
        lbl = new () { Text = "Last Name:  ", X = Pos.Left (buttonLbl), Y = Pos.Bottom (lbl) };
        TextField lastNameField = new () { Text = "Six", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl) };
        secondStep.Add (lbl, lastNameField);

        // Add last step
        WizardStep lastStep = new () { Title = "The last step" };
        wizard.AddStep (lastStep);

        lastStep.HelpText =
            "The wizard is complete!\n\nPress the Finish button to continue.\n\nPressing Esc will cancel.";

        Window window = new ();
        window.Add (menu, wizard);

        Application.Run (window);
        window.Dispose ();
        Application.Shutdown ();
    }
}
