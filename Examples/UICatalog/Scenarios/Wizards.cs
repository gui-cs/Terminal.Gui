#nullable enable

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
            X = 0,
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

        View actionLabel = new ()
        {
            X = Pos.Right (label), Y = Pos.AnchorEnd (1), SchemeName = "Error",
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        win.Add (actionLabel);

        Button showWizardButton = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (settingsFrame) + 2, IsDefault = true, Text = "_Show Wizard"
        };

        showWizardButton.Accepting += ShowWizard;
        win.Add (showWizardButton);
        app.Run (win);

        return;

        void ShowWizard (object? s, CommandEventArgs e)
        {
            actionLabel.Text = string.Empty;

            using Wizard wizard = new ();
            wizard.Title = titleEdit.Text;

            wizard.MovingBack += (_, args) =>
                                 {
                                     // Set Cancel to true to prevent moving back
                                     args.Cancel = false;
                                     actionLabel.Text = "Moving Back";
                                 };

            wizard.MovingNext += (_, args) =>
                                 {
                                     // Set Cancel to true to prevent moving next
                                     args.Cancel = false;
                                     actionLabel.Text = "Moving Next";
                                 };

            wizard.Accepting += (_, args) =>
                                {
                                    actionLabel.Text = "Finished";
                                    MessageBox.Query ((s as View)?.App!, "Wizard", "The Wizard has been completed and accepted!", "Ok");

                                    // Don't set args.Handled to true to allow the wizard to close
                                    args.Handled = false;
                                };

            wizard.Cancelled += (_, args) =>
                                {
                                    actionLabel.Text = "Cancelled";

                                    int? btn = MessageBox.Query ((s as View)?.App!, "Wizard", "Are you sure you want to cancel?", "Yes", "No");
                                    args.Cancel = btn == 1;
                                };

            ((IDesignable)wizard).EnableForDesign ();

            app.Run (wizard);
        }
    }
}
