#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("WizardAsView", "Shows using the Wizard class in an non-modal way")]
[ScenarioCategory ("Wizards")]
public class WizardAsView : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        using IApplication app = Application.Instance;

        using Window window = new ();

        Wizard wizard = new ();
        (wizard as IDesignable).EnableForDesign ();

        View actionLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.AnchorEnd (),
            Text = "No Action Yet",
            Height = 1,
            Width = Dim.Auto (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error)
        };

        wizard.MovingBack += (_, args) =>
                             {
                                 // Set Cancel to true to prevent moving next
                                 args.Cancel = false;
                                 actionLabel.Text = "Moving Back";
                             };

        wizard.MovingNext += (_, args) =>
                             {
                                 // Set Cancel to true to prevent moving next
                                 args.Cancel = false;
                                 actionLabel.Text = "Moving Next";
                             };

        wizard.Accepting += (s, args) =>
                            {
                                actionLabel.Text = "Finished";

                                MessageBox.Query ((s as View)?.App!, "Wizard", "The Wizard has been completed and accepted!", "Ok");
                                args.Handled = true;
                                (s as View)?.App!.RequestStop ();
                            };

        wizard.Cancelled += (s, args) =>
                            {
                                actionLabel.Text = "Cancelled";

                                int? btn = MessageBox.Query ((s as View)?.App!, "Wizard", "Are you sure you want to cancel?", "Yes", "No");
                                args.Cancel = btn == 1;
                            };

        window.Add (wizard, actionLabel);

        app.Run (window);
    }
}
