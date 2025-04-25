#nullable enable
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("SimpleDialog", "SimpleDialog ")]
public sealed class SimpleDialog : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };


        appWindow.DrawingText += (s, e) =>
                                 {
                                     appWindow!.FillRect (appWindow!.Viewport, Glyphs.Dot);
                                     e.Cancel = true;
                                 };

        Dialog dialog = new () { Id = "dialog", Width = 20, Height = 4, Title = "Dialog" };
        dialog.Arrangement |= ViewArrangement.Resizable;

        var button = new Button
        {
            Id = "button", 
            X = 0,
            Y = 0, 
            NoDecorations = true,
            NoPadding = true,
            Text = "A",
            //WantContinuousButtonPressed = false,
            HighlightStyle = HighlightStyle.None,
            ShadowStyle = ShadowStyle.Transparent,
        };

        button.Accepting += (s, e) =>
                            {
                                Application.Run (dialog);
                                e.Cancel = true;
                            };
        appWindow.Add (button);

        // Run - Start the application.
        Application.Run (appWindow);
        dialog.Dispose ();
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
