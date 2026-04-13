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
using UICatalog;

// Set Inline mode BEFORE Init
Application.AppModel = AppModel.Inline;

//Application.ForceInlinePosition = new Point (0, 3);

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

        Logo logo = new () { X = Pos.Center () };

        Label infoLabel = new ()
        {
            Text = "Demonstrates Inline Application Mode.\nType a message and press Enter. Press Esc to exit.",
            Y = Pos.Bottom (logo),
            TextAlignment = Alignment.Center,
            Width = Dim.Fill (),
            Enabled = false
        };

        ObservableCollection<string> items = [];

        // Declared early so the local function can capture them.
        ListView<string> outputList = null!;
        View inputIndicator = null!;

        outputList = new ListView<string>
        {
            Y = Pos.Bottom (infoLabel),
            Width = Dim.Fill (),
            Height = Dim.Auto (minimumContentDim: 1, maximumContentDim: Dim.Func (_ => GetMaxListHeight ())),
            BorderStyle = LineStyle.Dotted
        };
        outputList.Border.Thickness = new Thickness (0, 0, 0, 1);

        outputList.SubViewsLaidOut += (_, _) =>
                                      {
                                          if (outputList.Index is { })
                                          {
                                              outputList.Viewport = outputList.Viewport with { Y = outputList.Index.Value };
                                          }
                                      };

        outputList.GettingAttributeForRole += (sender, args) =>
                                              {
                                                  var view = sender as View;

                                                  if (args.Role != VisualRole.Active)
                                                  {
                                                      return;
                                                  }
                                                  args.Result = view?.GetAttributeForRole (VisualRole.Normal);
                                                  args.Handled = true;
                                              };

        outputList.SetSource (items);

        inputIndicator = new View
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
                                   var text = $"{Glyphs.BlackCircle} {inputField.Text}";

                                   items.Add (text);
                                   outputList.MoveEnd ();

                                   inputField.Text = string.Empty;
                                   itemCountShortcut.Title = $"{items.Count}";
                               };

        statusBar.Add (_cursorShortcut, _driverShortcut, _screenShortcut, _frameShortcut, itemCountShortcut);

        Add (logo, infoLabel, outputList, inputIndicator, inputField, statusBar);
        inputField.SetFocus ();

        _cursorShortcut?.Title = $"{App?.Driver?.InlinePosition.Y}";
        _driverShortcut?.Title = $"{Format (App?.Driver?.Screen)}";
        _screenShortcut?.Title = $"{Format (App?.Screen)}";
        _frameShortcut?.Title = $"{Format (Frame)}";

        return;

        // Returns the maximum content height for the outputList so it doesn't grow beyond what the
        // terminal can display. Subtracts all sibling and adornment heights from App.Screen.Height.
        int GetMaxListHeight ()
        {
            int screenHeight = App?.Driver?.Screen.Height ?? 100;

            // Adornments of the containing InlinePromptView (this Window)
            int windowAdornments = (Border?.Thickness.Vertical ?? 0) + (Margin?.Thickness.Vertical ?? 0) + (Padding?.Thickness.Vertical ?? 0);

            // Heights of sibling views (everything except the list itself)
            int siblingHeight = (logo?.Frame.Height ?? 0)
                                + (infoLabel?.Frame.Height ?? 0)
                                + (inputIndicator?.Frame.Height ?? 0)
                                + (statusBar?.Frame.Height ?? 0);

            // The list's own adornment overhead (border bottom = 1)
            int listAdornments = (outputList.Border?.Thickness.Vertical ?? 0)
                                 + (outputList.Margin?.Thickness.Vertical ?? 0)
                                 + (outputList.Padding?.Thickness.Vertical ?? 0);

            int maxContent = screenHeight - windowAdornments - siblingHeight - listAdornments;

            return Math.Max (1, maxContent);
        }
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (newIsRunning)
        {
            App?.ScreenChanged += AppOnScreenChanged;
            App?.Driver?.SizeChanged += DriverOnSizeChanged;
        }
        else
        {
            App?.ScreenChanged -= AppOnScreenChanged;
            App?.Driver?.SizeChanged -= DriverOnSizeChanged;
        }
    }

    private void AppOnScreenChanged (object? sender, EventArgs<Rectangle> e) => _screenShortcut?.Title = $"{Format (e.Value)}";

    private void DriverOnSizeChanged (object? sender, SizeChangedEventArgs e) =>
        _driverShortcut?.Title = $"{Format (new Rectangle (new Point (0, 0), e.Size!.Value))}";

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (in frame);

        _cursorShortcut?.Title = $"{App?.Driver?.InlinePosition.Y}";
        _driverShortcut?.Title = $"{Format (App?.Driver?.Screen)}";
        _screenShortcut?.Title = $"{Format (App?.Screen)}";
        _frameShortcut?.Title = $"{Format (Frame)}";
    }

    private static string Format (Rectangle? rect) =>
        rect is null ? $"({Glyphs.Null})" : $"({rect.Value.X},{rect.Value.Y},{rect.Value.Width},{rect.Value.Height})";
}
