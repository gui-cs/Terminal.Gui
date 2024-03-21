using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("_Virtual Content Scrolling Demo", "Demonstrates scrolling built-into View")]
[ScenarioCategory ("Layout")]
public class VirtualScrolling : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public class VirtualDemoView : Window
    {
        public VirtualDemoView ()
        {
            Text = "Virtual Demo View Text. This is long text.\nThe second line.\n3\n4\n5th line.";
            Arrangement = ViewArrangement.Fixed;
            ContentSize = new Size (100, 50);

            // Things this view knows how to do
            AddCommand (Command.ScrollDown, () => ScrollVertical (1));
            AddCommand (Command.ScrollUp, () => ScrollVertical (-1));

            //AddCommand (Command.PageUp, () => PageUp ());
            //AddCommand (Command.PageDown, () => PageDown ());
            //AddCommand (Command.TopHome, () => Home ());
            //AddCommand (Command.BottomEnd, () => End ());

            // Default keybindings for all ListViews
            KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
            KeyBindings.Add (Key.CursorDown, Command.ScrollDown);

            //KeyBindings.Add (Key.PageUp, Command.PageUp);
            //KeyBindings.Add (Key.PageDown, Command.PageDown);
            //KeyBindings.Add (Key.Home, Command.TopHome);
            //KeyBindings.Add (Key.End, Command.BottomEnd);

            LayoutComplete += VirtualDemoView_LayoutComplete;
        }

        private void VirtualDemoView_LayoutComplete (object sender, LayoutEventArgs e)
        {
            Title = Viewport.ToString ();
            SetNeedsDisplay ();
        }

        private bool? ScrollVertical (int rows)
        {
            if (ContentSize == Size.Empty || ContentSize == Viewport.Size)
            {
                return true;
            }

            if (Viewport.Y + rows < 0)
            {
                return true;
            }

            Viewport = Viewport with { Y = Viewport.Y + rows };

            return true;
        }

        /// <inheritdoc />
        public override void OnDrawContent (Rectangle viewport)
        {
            base.OnDrawContent (viewport);

        }
    }

    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

        var view = new VirtualDemoView { Title = "The _Window With Content" };

        var tf1 = new TextField { X = 20, Y = 7, Width = 10, Text = "TextField" };
        var color = new ColorPicker { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd (11) };
        color.BorderStyle = LineStyle.RoundedDotted;

        color.ColorChanged += (s, e) =>
                              {
                                  color.SuperView.ColorScheme = new (color.SuperView.ColorScheme)
                                  {
                                      Normal = new (
                                                    color.SuperView.ColorScheme.Normal.Foreground,
                                                    e.Color
                                                   )
                                  };
                              };

        var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Press me!" };

        button.Accept += (s, e) =>
                             MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");

        var label = new TextView
        {
            X = Pos.Center (),
            Y = Pos.Bottom (button),
            Title = "Title",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.",
            Width = 40,
            Height = 6 // TODO: Use Dim.Auto
        };
        label.Border.Thickness = new (1, 3, 1, 1);

        var btnButtonInWindow = new Button { X = Pos.AnchorEnd (10), Y = Pos.AnchorEnd (1), Text = "Button" };

        var tv = new Label
        {
            AutoSize = false,
            Y = Pos.AnchorEnd (3),
            Width = 25,
            Height = Dim.Fill (),
            Text = "Label\nY=AnchorEnd(3),Height=Dim.Fill()"
        };

        view.Margin.Data = "Margin";
        view.Margin.Thickness = new (3);

        view.Border.Data = "Border";
        view.Border.Thickness = new (3);

        view.Padding.Data = "Padding";
        view.Padding.Thickness = new (3);

        view.Add (tf1, color, button, label, btnButtonInWindow, tv);

        var editor = new Adornments.AdornmentsEditor
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
            ColorScheme = Colors.ColorSchemes [TopLevelColorScheme]

            //BorderStyle = LineStyle.None,
        };

        editor.Initialized += (s, e) => { editor.ViewToEdit = view; };

        Application.Top.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        Application.Run (editor);
        Application.Shutdown ();
    }

    public override void Run () { }
}
