// A simple Terminal.Gui example demonstrating Inline mode rendering.
//
// This example shows how to use AppModel.Inline to render UI inline within
// the primary (scrollback) terminal buffer, similar to how Claude Code CLI
// and GitHub Copilot CLI render their UI.
//
// The application renders below the current shell prompt without switching
// to the alternate screen buffer. On exit, the rendered content stays in
// scrollback history.

using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Set Inline mode BEFORE Init
Application.AppModel = AppModel.Inline;

IApplication app = Application.Create ().Init ();
app.Run<InlinePromptView> ();
app.Dispose ();

/// <summary>
///     A simple inline prompt view that demonstrates the inline rendering mode.
///     Uses <c>Height = Dim.Auto(minimumContentSize: 10)</c> so the view sizes itself by content
///     with a minimum height. The framework automatically resizes <c>Screen</c> to the rows
///     available below the cursor and offsets rendering so the view appears at the cursor position.
/// </summary>
public sealed class InlinePromptView : Window
{
    public InlinePromptView ()
    {
        Title = "Inline CLI Demo (Esc to quit)";

        Border.Thickness = new Thickness (0, 4, 0, 0);
        Border.LineStyle = LineStyle.Rounded;

        Arrangement = ViewArrangement.TopResizable;

        Width = Dim.Fill ();
        Height = Dim.Auto (minimumContentDim: 10);

        Label statusLabel = new () { Text = "Type a message and press Enter. Press Esc to exit.", Width = Dim.Fill () };

        TextField inputField = new () { Y = Pos.Bottom (statusLabel) + 1, Width = Dim.Fill () };

        ObservableCollection<string> items = [];

        ListView<string> outputList = new ()
        {
            Y = Pos.Bottom (inputField) + 1,
            Width = Dim.Fill (),
            Height = Dim.Auto ()
        };

        outputList.SetSource (items);

        inputField.Accepted += (_, _) =>
                               {
                                   string text = inputField.Text;

                                   if (!string.IsNullOrEmpty (text))
                                   {
                                       items.Add ($"> {text}");
                                       inputField.Text = string.Empty;
                                   }
                               };

        Add (statusLabel, inputField, outputList);
    }
}
