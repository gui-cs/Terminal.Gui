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

    public class VirtualDemoView : FrameView
    {
        public VirtualDemoView ()
        {
            Width = Dim.Fill ();
            Height = Dim.Fill ();
            ColorScheme = Colors.ColorSchemes ["Base"];
            Text = "Virtual Demo View Text. This is long text.\nThe second line.\n3\n4\n5th line\nLine 6. This is a longer line that should wrap automatically.";
            CanFocus = true;
            BorderStyle = LineStyle.Rounded;
            Arrangement = ViewArrangement.Fixed;

            // TODO: Add a way to set the scroll settings in the Scenario
            ContentSize = new Size (60, 40);
            ScrollSettings = ScrollSettings.NoRestrict;

            // Things this view knows how to do
            AddCommand (Command.ScrollDown, () => ScrollVertical (1));
            AddCommand (Command.ScrollUp, () => ScrollVertical (-1));

            AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
            AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

            //AddCommand (Command.PageUp, () => PageUp ());
            //AddCommand (Command.PageDown, () => PageDown ());
            //AddCommand (Command.TopHome, () => Home ());
            //AddCommand (Command.BottomEnd, () => End ());

            // Default keybindings for all ListViews
            KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
            KeyBindings.Add (Key.CursorDown, Command.ScrollDown);
            KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
            KeyBindings.Add (Key.CursorRight, Command.ScrollRight);

            //KeyBindings.Add (Key.PageUp, Command.PageUp);
            //KeyBindings.Add (Key.PageDown, Command.PageDown);
            //KeyBindings.Add (Key.Home, Command.TopHome);
            //KeyBindings.Add (Key.End, Command.BottomEnd);

            Border.Add (new Label () { X = 23 });
            LayoutComplete += VirtualDemoView_LayoutComplete;

            MouseEvent += VirtualDemoView_MouseEvent;
        }

        private void VirtualDemoView_MouseEvent (object sender, MouseEventEventArgs e)
        {
            if (e.MouseEvent.Flags == MouseFlags.WheeledDown)
            {
                ScrollVertical (1);
                return;
            }
            if (e.MouseEvent.Flags == MouseFlags.WheeledUp)
            {
                ScrollVertical (-1);

                return;
            }

            if (e.MouseEvent.Flags == MouseFlags.WheeledRight)
            {
                ScrollHorizontal (1);
                return;
            }
            if (e.MouseEvent.Flags == MouseFlags.WheeledLeft)
            {
                ScrollHorizontal (-1);

                return;
            }

        }

        private void VirtualDemoView_LayoutComplete (object sender, LayoutEventArgs e)
        {
            var status = Border.Subviews.OfType<Label> ().FirstOrDefault ();

            if (status is { })
            {
                status.Title = $"Frame: {Frame}\n\nViewport: {Viewport}, ContentSize = {ContentSize}";
            }

            SetNeedsDisplay ();
        }
    }

    public override void Main ()
    {
        Application.Init ();

        var view = new VirtualDemoView { Title = "Virtual Scrolling" };

        var tf1 = new TextField { X = 20, Y = 7, Width = 10, Text = "TextField" };
        var color = new ColorPicker { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd (11), Y = 10 };
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

        var textView = new TextView
        {
            X = Pos.Center (),
            Y = 10,
            Title = "TextView",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.\nI have 3 lines of text with room for 2.",
            AllowsTab = false,
            Width = 30,
            Height = 6 // TODO: Use Dim.Auto
        };
        textView.Border.Thickness = new (1, 3, 1, 1);

        var charMap = new Scenarios.CharMap ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (textView) + 1,
            Width = 30,
            Height = 10
        };

        charMap.Accept += (s, e) =>
                              MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");

        var btnButtonInWindow = new Button { X = Pos.AnchorEnd (10), Y = 0, Text = "Button" };

        var tv = new Label
        {
            AutoSize = false,
            Y = Pos.AnchorEnd (3),
            Width = 25,
            Height = Dim.Fill (),
            Text = "Label\nY=AnchorEnd(3),Height=Dim.Fill()"
        };

        view.Margin.Data = "Margin";
        view.Margin.Thickness = new (0);

        view.Border.Data = "Border";
        view.Border.Thickness = new (3);

        view.Padding.Data = "Padding";
        view.Padding.Thickness = new (3);

        view.Add (btnButtonInWindow, tf1, color, charMap, textView, tv);
        var label2 = new Label
        {
            Id = "label2",
            X = 0,
            Y = 30,
            Text = "This label is long. It should clip to the Viewport (but not ContentArea). This is a virtual scrolling demo. Use the arrow keys and/or mouse wheel to scroll the content.",
        };
        label2.TextFormatter.WordWrap = true;
        view.Add (label2);

        var editor = new Adornments.AdornmentsEditor
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
            ColorScheme = Colors.ColorSchemes ["Dialog"]

            //BorderStyle = LineStyle.None,
        };

        editor.Initialized += (s, e) => { editor.ViewToEdit = view; };

        editor.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        //button.SetFocus ();

        Application.Run (editor);
        editor.Dispose ();
        Application.Shutdown ();
    }
}
