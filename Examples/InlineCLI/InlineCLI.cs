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
using System.Drawing;
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
    private readonly Shortcut? _cursorShortcut;
    private readonly Shortcut? _driverShortcut;
    private readonly Shortcut? _screenShortcut;
    private readonly Shortcut? _frameShortcut;

    public InlinePromptView ()
    {
        Title = "Inline CLI Demo";

        Border.Thickness = new Thickness (0, 3, 0, 0);
        Border.LineStyle = LineStyle.Rounded;

        Arrangement = ViewArrangement.TopResizable;

        Width = Dim.Fill ();
        Height = Dim.Auto ();

        StatusBar statusBar = new () { AlignmentModes = AlignmentModes.IgnoreFirstOrLast, SchemeName = SchemeName };
        Shortcut itemCountShortcut = new () { Title = "No items", MouseHighlightStates = MouseState.None, Enabled = false };

        _cursorShortcut = new Shortcut { Text = "Cursor", MouseHighlightStates = MouseState.None, Enabled = false };
        _driverShortcut = new Shortcut { Text = "Driver", MouseHighlightStates = MouseState.None, Enabled = false };
        _screenShortcut = new Shortcut { Text = "Screen", MouseHighlightStates = MouseState.None, Enabled = false };
        _frameShortcut = new Shortcut { Text = "Frame", MouseHighlightStates = MouseState.None, Enabled = false };

        Label infoLabel = new ()
        {
            Text = "Demonstrates Inline Application Mode.\nType a message and press Enter. Press Esc to exit.",
            TextAlignment = Alignment.Center,
            Width = Dim.Fill (),
            Enabled = false
        };

        ObservableCollection<string> items = [];

        ListView<string> outputList = new ()
        {
            Y = Pos.Bottom (infoLabel), Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 1), BorderStyle = LineStyle.Dotted
        };
        outputList.Border.Thickness = new Thickness (0, 0, 0, 1);

        outputList.ValueChanged += (_, _) => { };

        outputList.SetSource (items);

        View inputIndicator = new ()
        {
            Text = $"{Glyphs.RightArrow}",
            Y = Pos.Bottom (outputList),
            Width = 2,
            Height = 2,
            Enabled = false,
            BorderStyle = LineStyle.Dotted
        };
        inputIndicator.Border.Thickness = new Thickness (0, 0, 0, 1);

        TextField inputField = new ()
        {
            X = Pos.Right (inputIndicator), Y = Pos.Top (inputIndicator), Width = Dim.Fill (), BorderStyle = inputIndicator.BorderStyle
        };
        inputField.Border.Thickness = new Thickness (0, 0, 0, 1);

        inputField.Accepted += (_, _) =>
                               {
                                   string text = inputField.Text;

                                   items.Add ($"{Glyphs.BlackCircle} {text}");
                                   inputField.Text = string.Empty;
                                   itemCountShortcut.Title = $"Items: {items.Count}";
                               };

        statusBar.Add (_cursorShortcut, _driverShortcut, _screenShortcut, _frameShortcut, itemCountShortcut);

        Add (infoLabel, outputList, inputIndicator, inputField, statusBar);
        inputField.SetFocus ();
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (newIsRunning)
        {
            _cursorShortcut?.Title = $"Cursor:{App?.Driver?.InlineState.InlineCursorRow}";
            _driverShortcut?.Title = $"Driver:{Format (App?.Driver?.Screen)}";
            _screenShortcut?.Title = $"App:{Format (App?.Screen)}";
            _frameShortcut?.Title = $"Frame:{Format (Frame)}";
            App?.ScreenChanged += AppOnScreenChanged;
            App?.Driver?.SizeChanged += DriverOnSizeChanged;
        }
        else
        {
            App?.ScreenChanged -= AppOnScreenChanged;
            App?.Driver?.SizeChanged -= DriverOnSizeChanged;
        }

        return;

        void AppOnScreenChanged (object? sender, EventArgs<Rectangle> e) => _screenShortcut?.Title = $"App:{Format (e.Value)}";

        void DriverOnSizeChanged (object? sender, SizeChangedEventArgs e) =>
            _driverShortcut?.Title = $"Driver:{Format (new Rectangle (new Point (0, 0), e.Size!.Value))}";
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (in frame);
        _frameShortcut?.Title = $"Frame:{Format (frame)}";
    }

    private static string Format (Rectangle? rect) =>
        rect is null ? $"({Glyphs.Null})" : $"({rect.Value.X},{rect.Value.Y},{rect.Value.Width},{rect.Value.Height})";
}
