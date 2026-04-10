#nullable enable

namespace UICatalog.Scenarios;

/// <summary>
///     Demonstrates the "cancel stop" pattern: a dialog that can cancel its own closing via
///     <see cref="Runnable.OnIsRunningChanging"/>.
/// </summary>
/// <remarks>
///     <para>
///         This scenario exercises the TG bug where <see cref="IRunnable.StopRequested"/> is not reset
///         when <see cref="IRunnable.IsRunningChanging"/> is cancelled (i.e. the stop is vetoed by a handler).
///     </para>
///     <para>
///         Before the fix the dialog would freeze after the user clicks "Stay" — the run loop had already
///         exited and was never re-entered, leaving the dialog visually present but unresponsive.
///         After the fix the dialog resumes normally and the counter keeps incrementing.
///     </para>
/// </remarks>
[ScenarioMetadata ("Cancel Stop Demo", "Demonstrates cancelling a stop via OnIsRunningChanging (freeze-on-cancel regression)")]
[ScenarioCategory ("Runnable")]
[ScenarioCategory ("Dialogs")]
public class CancelStopDemo : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        Label instructions = new ()
        {
            X = Pos.Center (),
            Y = 1,
            Text = "Press the button to open a dialog.\nThen press Esc or click Cancel inside the dialog.\nChoose \"Stay\" → the dialog should remain interactive (counter keeps going).\nChoose \"Leave\" → the dialog closes normally."
        };

        Button openBtn = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (instructions) + 1,
            Text = "Open Confirmable Dialog"
        };

        Label resultLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (openBtn) + 1,
            Text = "Result: (not run yet)"
        };

        openBtn.Accepting += (_, _) =>
        {
            using ConfirmableDialog dlg = new (app);

            app.Run (dlg);

            resultLabel.Text = dlg.Interactions == 0
                ? "Result: dialog was closed without interaction"
                : $"Result: dialog closed after {dlg.Interactions} cancel attempt(s) were vetoed";
        };

        mainWindow.Add (instructions, openBtn, resultLabel);

        app.Run (mainWindow);
    }

    /// <summary>
    ///     A dialog that cancels its first <paramref name="MaxCancels"/> stop attempts by returning
    ///     <see langword="true"/> from <see cref="OnIsRunningChanging"/>.
    /// </summary>
    private class ConfirmableDialog : Dialog
    {
        private readonly IApplication _app;
        private Label? _counterLabel;

        public ConfirmableDialog (IApplication app)
        {
            _app = app;
            Title = "Confirmable Dialog";
            Width = 60;
            Height = 14;
            X = Pos.Center ();
            Y = Pos.Center ();

            Label info = new ()
            {
                X = Pos.Center (),
                Y = 1,
                Text = "Press Esc or Cancel to try closing.\nThe dialog will ask for confirmation."
            };

            _counterLabel = new Label
            {
                X = Pos.Center (),
                Y = Pos.Bottom (info) + 1,
                Text = "Veto count: 0"
            };

            Button cancelBtn = new ()
            {
                X = Pos.Center (),
                Y = Pos.Bottom (_counterLabel) + 1,
                Text = "Cancel"
            };

            cancelBtn.Accepting += (_, _) => RequestStop ();

            Add (info, _counterLabel, cancelBtn);
        }

        /// <summary>Gets the number of times the stop was vetoed (user chose "Stay").</summary>
        public int Interactions { get; private set; }

        /// <inheritdoc/>
        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            if (!newIsRunning)
            {
                // Ask user whether they really want to close
                int? choice = MessageBox.Query (
                    _app,
                    "Confirm Close",
                    "Do you really want to close this dialog?",
                    "Stay",
                    "Leave"
                );

                if (choice == 0)
                {
                    // User chose "Stay" — veto the stop
                    Interactions++;

                    if (_counterLabel is { })
                    {
                        _counterLabel.Text = $"Veto count: {Interactions}";
                    }

                    return true; // Cancel the stop
                }
            }

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }
    }
}
