#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Transparent", "Testing Transparency")]
public sealed class Transparent : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };
        appWindow.BorderStyle = LineStyle.None;
        appWindow.SchemeName = "Error";

        appWindow.Text = "App Text - Centered Vertically and Horizontally.\n2nd Line of Text.\n3rd Line of Text.";
        appWindow.TextAlignment = Alignment.Center;
        appWindow.VerticalTextAlignment = Alignment.Center;
        appWindow.ClearingViewport += (s, e) =>
                                    {
                                        if (s is View sender)
                                        {
                                            sender.FillRect (sender.Viewport, Glyphs.Stipple);
                                        }

                                        e.Cancel = true;
                                    };
        ViewportSettingsEditor viewportSettingsEditor = new ViewportSettingsEditor ()
        {
            Y = Pos.AnchorEnd (),
            //X = Pos.Right (adornmentsEditor),
            AutoSelectViewToEdit = true
        };
        appWindow.Add (viewportSettingsEditor);

        Button appButton = new Button ()
        {
            X = 10,
            Y = 4,
            Title = "_AppButton",
        };
        appButton.Accepting += (sender, args) =>
                               {
                                   MessageBox.Query ("AppButton", "Transparency is cool!", "_Ok");
                                   args.Handled = true;
                               };
        appWindow.Add (appButton);

        var tv = new TransparentView ()
        {
            X = 3,
            Y = 3,
            Width = 50,
            Height = 15
        };
        appWindow.Add (tv);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    public class TransparentView : FrameView
    {
        public TransparentView ()
        {
            Title = "Transparent View";
            //base.Text = "View.Text.\nThis should be opaque.\nNote how clipping works?";
            TextFormatter.Alignment = Alignment.Center;
            TextFormatter.VerticalAlignment = Alignment.Center;
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.Transparent | Terminal.Gui.ViewBase.ViewportSettingsFlags.TransparentMouse;
            BorderStyle = LineStyle.RoundedDotted;
            //SchemeName = "Base";

            var transparentSubView = new View ()
            {
                Text = "Sizable/Movable View with border. Should be opaque. No Shadow.",
                Id = "transparentSubView",
                X = 4,
                Y = 8,
                Width = 20,
                Height = 8,
                BorderStyle = LineStyle.Dashed,
                Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
                // ShadowStyle = ShadowStyle.Transparent,
            };
            transparentSubView.Border!.Thickness = new (1, 1, 1, 1);
            transparentSubView.SchemeName = "Dialog";
            //transparentSubView.Visible = false;

            Button button = new Button ()
            {
                Title = "_Opaque Shadows No Worky",
                X = Pos.Center (),
                Y = 2,
                SchemeName = "Dialog",
            };
            button.Accepting += (sender, args) =>
                                {
                                    MessageBox.Query ("Clicked!", "Button in Transparent View", "_Ok");
                                    args.Handled = true;
                                };
            //button.Visible = false;


            var shortcut = new Shortcut ()
            {
                Id = "shortcut",
                X = Pos.Center (),
                Y = Pos.AnchorEnd (),
                Title = "A _Shortcut",
                HelpText = "Help!",
                Key = Key.F11,
                SchemeName = "Base"

            };

            button.ClearingViewport += (sender, args) =>
                                       {
                                           args.Cancel = true;
                                       };


            base.Add (button);
            base.Add (shortcut);
            base.Add (transparentSubView);

            //Padding.Thickness = new (1);
            //Padding.SchemeName = "Error";

            Margin!.Thickness = new (1);
           // Margin.ViewportSettings |= Terminal.Gui.ViewportSettingsFlags.Transparent;
        }

        /// <inheritdoc />
        protected override bool OnClearingViewport () { return false; }

        /// <inheritdoc />
        protected override bool OnMouseEvent (MouseEventArgs mouseEvent) { return false; }
    }

}
