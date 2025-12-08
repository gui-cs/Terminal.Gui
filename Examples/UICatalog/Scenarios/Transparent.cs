// ReSharper disable AccessToDisposedClosure
#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Transparent", "Demonstrates View Transparency")]
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
                                   MessageBox.Query ((sender as View)?.App, "AppButton", "Transparency is cool!", "_Ok");
                                   args.Handled = true;
                               };
        appWindow.Add (appButton);

        // Add BigText demonstration
        var bigText = new BigText ()
        {
            X = Pos.Center (),
            Y = 1,
            Text = "tui",
            GlyphHeight = 6,
            Style = LineStyle.Double
        };
        appWindow.Add (bigText);

        var tv = new TransparentView ()
        {
            X = 2,
            Y = 2,
            Width = Dim.Fill (10),
            Height = Dim.Fill (10)
        };

        appWindow.ViewportChanged += (sender, args) =>
                                      {
                                          // Little hack to convert the Dim.Fill to actual size
                                          // So resizing works
                                          tv.Width = appWindow!.Frame.Width - 10;
                                          tv.Height = appWindow!.Frame.Height - 10;
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
            Title = "Transparent View - Move and Resize To See Transparency In Action";
            base.Text = "View.Text.\nThis should be opaque. Note how clipping works?";
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;
            BorderStyle = LineStyle.RoundedDotted;
            SchemeName = "Base";

            var transparentSubView = new View ()
            {
                Text = "Sizable/Movable SubView with border and shadow.",
                Id = "transparentSubView",
                X = Pos.Center (),
                Y = Pos.Center (),
                Width = 20,
                Height = 8,
                BorderStyle = LineStyle.Dashed,
                Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
                ShadowStyle = ShadowStyle.Transparent,
            };
            transparentSubView.Border!.Thickness = new (1, 1, 1, 1);
            transparentSubView.SchemeName = "Dialog";

            Button button = new Button ()
            {
                Title = "_Opaque Shadow",
                X = Pos.Center (),
                Y = 2,
                SchemeName = "Dialog",
            };
            button.Accepting += (sender, args) =>
                                {
                                    MessageBox.Query (App, "Clicked!", "Button in Transparent View", "_Ok");
                                    args.Handled = true;
                                };

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

            // Subscribe to DrawingContent event to draw "TUI" 
            DrawingContent += TransparentView_DrawingContent;

            base.Add (button);
            base.Add (shortcut);
            base.Add (transparentSubView);

            Padding!.Thickness = new (1);
            Padding.Text = "This is the Padding";
        }

        private void TransparentView_DrawingContent (object? sender, DrawEventArgs e)
        {
            // Draw "TUI" text using rectangular regions, positioned after "Hi"
            // Letter "T"
            Rectangle tTop = new (20, 5, 7, 2);      // Top horizontal bar
            Rectangle tStem = new (23, 7, 2, 8);     // Vertical stem

            // Letter "U"
            Rectangle uLeft = new (30, 5, 2, 8);     // Left vertical bar
            Rectangle uBottom = new (32, 13, 3, 2);  // Bottom horizontal bar
            Rectangle uRight = new (35, 5, 2, 8);    // Right vertical bar

            // Letter "I"
            Rectangle iTop = new (39, 5, 4, 2);      // Bar on top
            Rectangle iStem = new (40, 7, 2, 6);     // Vertical stem
            Rectangle iBottom = new (39, 13, 4, 2);      // Bar on Bottom

            // Draw "TUI" using the HotActive attribute
            SetAttributeForRole (VisualRole.HotActive);
            FillRect (tTop, Glyphs.BlackCircle);
            FillRect (tStem, Glyphs.BlackCircle);
            FillRect (uLeft, Glyphs.BlackCircle);
            FillRect (uBottom, Glyphs.BlackCircle);
            FillRect (uRight, Glyphs.BlackCircle);
            FillRect (iTop, Glyphs.BlackCircle);
            FillRect (iStem, Glyphs.BlackCircle);
            FillRect (iBottom, Glyphs.BlackCircle);

            Region tuiRegion = new Region (ViewportToScreen (tTop));
            tuiRegion.Union (ViewportToScreen (tStem));
            tuiRegion.Union (ViewportToScreen (uLeft));
            tuiRegion.Union (ViewportToScreen (uBottom));
            tuiRegion.Union (ViewportToScreen (uRight));
            tuiRegion.Union (ViewportToScreen (iTop));
            tuiRegion.Union (ViewportToScreen (iStem));
            tuiRegion.Union (ViewportToScreen (iBottom));

            // Register the drawn region for "TUI" to enable transparency effects
            e.DrawContext?.AddDrawnRegion (tuiRegion);
        }

        /// <inheritdoc />
        protected override bool OnDrawingContent (DrawContext? context)
        {
            base.OnDrawingContent (context);

            // Draw "Hi" text using rectangular regions
            // Letter "H"
            Rectangle hLeft = new (5, 5, 2, 10);      // Left vertical bar
            Rectangle hMiddle = new (7, 9, 3, 2);     // Middle horizontal bar
            Rectangle hRight = new (10, 5, 2, 10);    // Right vertical bar

            // Letter "i" (with some space between H and i)
            Rectangle iDot = new (15, 5, 2, 2);       // Dot on top
            Rectangle iStem = new (15, 9, 2, 6);      // Vertical stem

            // Draw "Hi" using the Highlight attribute
            SetAttributeForRole (VisualRole.Highlight);
            FillRect (hLeft, Glyphs.BlackCircle);
            FillRect (hMiddle, Glyphs.BlackCircle);
            FillRect (hRight, Glyphs.BlackCircle);
            FillRect (iDot, Glyphs.BlackCircle);
            FillRect (iStem, Glyphs.BlackCircle);

            // Register the drawn region for "Hi" to enable transparency effects
            Region hiRegion = new Region (ViewportToScreen (hLeft));
            hiRegion.Union (ViewportToScreen (hMiddle));
            hiRegion.Union (ViewportToScreen (hRight));
            hiRegion.Union (ViewportToScreen (iDot));
            hiRegion.Union (ViewportToScreen (iStem));
            context?.AddDrawnRegion (hiRegion);

            // Return false to allow DrawingContent event to fire
            return false;
        }

        /// <inheritdoc />
        protected override bool OnClearingViewport () { return false; }

        /// <inheritdoc />
        protected override bool OnMouseEvent (MouseEventArgs mouseEvent) { return false; }
    }

}
