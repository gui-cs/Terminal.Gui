#nullable enable
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Styles", "Shows Attribute.TextStyles including bold, italic, etc...")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Colors")]
public sealed class TestStyles : Scenario
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

        appWindow.DrawingContent += OnAppWindowOnDrawingContent;

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void OnAppWindowOnDrawingContent (object? sender, DrawEventArgs args)
    {
        if (sender is View { } sendingView)
        {
            var y = 0;
            var x = 0;
            int maxWidth = sendingView.Viewport.Width; // Get the available width of the view

            TextStyle [] allStyles = Enum.GetValues (typeof (TextStyle))
                                         .Cast<TextStyle> ()
                                         .Where (style => style != TextStyle.None)
                                         .ToArray ();

            // Draw individual flags on the first line
            foreach (TextStyle style in allStyles)
            {
                string text = Enum.GetName (typeof (TextStyle), style)!;
                int textWidth = text.Length;

                // Check if the text fits in the current line
                if (x + textWidth >= maxWidth)
                {
                    x = 0; // Move to the next line
                    y++;
                }

                sendingView.Move (x, y);

                var attr = new Attribute (sendingView.GetNormalColor ())
                {
                    TextStyle = style
                };
                sendingView.SetAttribute (attr);
                sendingView.AddStr (text);

                x += textWidth + 2; // Add spacing between entries
            }

            // Add a blank line
            y += 2;
            x = 0;

            // Generate all combinations of TextStyle (excluding individual flags)
            int totalCombinations = 1 << allStyles.Length; // 2^n combinations

            for (var i = 1; i < totalCombinations; i++) // Start from 1 to skip "None"
            {
                var combination = (TextStyle)0;
                List<string> styleNames = new ();

                for (var bit = 0; bit < allStyles.Length; bit++)
                {
                    if ((i & (1 << bit)) != 0)
                    {
                        combination |= allStyles [bit];
                        styleNames.Add (Enum.GetName (typeof (TextStyle), allStyles [bit])!);
                    }
                }

                // Skip individual flags
                if (styleNames.Count == 1)
                {
                    continue;
                }

                string text = $"[{string.Join (" | ", styleNames)}]";
                int textWidth = text.Length;

                // Check if the text fits in the current line
                if (x + textWidth >= maxWidth)
                {
                    x = 0; // Move to the next line
                    y++;
                }

                sendingView.Move (x, y);

                var attr = new Attribute (sendingView.GetNormalColor ())
                {
                    TextStyle = combination
                };
                sendingView.SetAttribute (attr);
                sendingView.AddStr (text);

                x += textWidth + 2; // Add spacing between entries
            }

            args.Cancel = true;
        }
    }
}
