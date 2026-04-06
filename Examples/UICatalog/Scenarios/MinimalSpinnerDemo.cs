namespace UICatalog.Scenarios;

[ScenarioMetadata ("Spinner Demo", "Minimal SpinnerView demo with auto-spin.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
public class MinimalSpinnerDemo : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window main = new () { Title = GetQuitKeyAndName () };

        SpinnerView spinner = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            AutoSpin = true
        };

        CheckBox chkAutoSpin = new ()
        {
            Text = "AutoSpin",
            X = Pos.Center (),
            Y = Pos.Bottom (spinner) + 1,
            Value = CheckState.Checked
        };
        chkAutoSpin.ValueChanged += (_, e) => spinner.AutoSpin = e.NewValue == CheckState.Checked;

        Label lblSequence = new ()
        {
            Text = "Sequence (comma-separated):",
            X = Pos.Center (),
            Y = Pos.Bottom (chkAutoSpin) + 1
        };

        TextField tfSequence = new ()
        {
            Text = string.Join (",", spinner.Sequence),
            X = Pos.Center (),
            Y = Pos.Bottom (lblSequence),
            Width = 30
        };
        tfSequence.Accepting += (_, _) =>
                                {
                                    string [] frames = tfSequence.Text
                                                                 .Split (',', StringSplitOptions.RemoveEmptyEntries);

                                    if (frames.Length > 0)
                                    {
                                        spinner.Sequence = frames;
                                    }
                                };

        Button btnAdvance = new ()
        {
            Text = "Advance",
            X = Pos.Center (),
            Y = Pos.Bottom (tfSequence) + 1
        };
        btnAdvance.Accepting += (_, _) => spinner.AdvanceAnimation ();

        main.Add (spinner, chkAutoSpin, lblSequence, tfSequence, btnAdvance);

        app.Run (main);
    }
}
