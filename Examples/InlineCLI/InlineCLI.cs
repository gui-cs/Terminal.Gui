// A simple Terminal.Gui example demonstrating Inline mode rendering.
//
// This example shows how to use AppModel.Inline to render UI inline within
// the primary (scrollback) terminal buffer, similar to how Claude Code CLI
// and GitHub Copilot CLI render their UI.
//
// The application renders below the current shell prompt without switching
// to the alternate screen buffer. On exit, the rendered content stays in
// scrollback history.

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Set Inline mode BEFORE Init
Application.AppModel = AppModel.Inline;

IApplication app = Application.Create ().Init ();
app.Run<InlinePromptView> ();
app.Dispose ();

// After Dispose, the shell prompt appears naturally below the rendered output.
Console.WriteLine ("Inline session complete. Content remains in scrollback.");

/// <summary>
///     A simple inline prompt view that demonstrates the inline rendering mode.
///     Uses <c>Y = Pos.AnchorEnd()</c> and <c>Height = Dim.Auto(minimumContentSize: 10)</c>
///     so the view anchors to the bottom of the Screen and sizes itself by content
///     with a minimum height. The first layout pass computes the view's Frame,
///     then <c>ApplicationImpl</c> sets <c>Screen.Height</c> to match.
/// </summary>
public sealed class InlinePromptView : Window
{
    public InlinePromptView ()
    {
        Title = "Inline CLI Demo (Esc to quit)";
        Width = Dim.Fill ();

        // Anchor to the bottom of the inline region and size by content with a minimum height.
        Y = Pos.AnchorEnd ();
        Height = Dim.Auto (minimumContentDim: 10);

        Label statusLabel = new ()
        {
            Text = "Type a message and press Enter. Press Esc to exit.",
            Width = Dim.Fill ()
        };

        TextField inputField = new ()
        {
            Y = Pos.Bottom (statusLabel) + 1,
            Width = Dim.Fill ()
        };

        Label outputLabel = new ()
        {
            Text = "Output will appear here...",
            Y = Pos.Bottom (inputField) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        inputField.Accepting += (_, e) =>
                                {
                                    string text = inputField.Text;

                                    if (!string.IsNullOrEmpty (text))
                                    {
                                        outputLabel.Text = $"> {text}";
                                        inputField.Text = string.Empty;
                                    }

                                    e.Handled = true;
                                };

        Add (statusLabel, inputField, outputLabel);
    }
}
