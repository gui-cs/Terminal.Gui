using System.Text;
using GitHub.Copilot.SDK;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace AI;

/// <summary>
///     An inline Window with conversation history, input field, status bar, and slash commands.
/// </summary>
internal sealed class ChatView : Window
{
    private readonly IApplication _app;
    private readonly CopilotClient _client;
    private string _model;
    private readonly MarkdownView _conversationView;
    private readonly StringBuilder _conversationText = new ();
    private readonly TextField _inputField;
    private readonly View _inputIndicator;
    private readonly SpinnerView _spinner;
    private readonly StatusBar _statusBar;
    private CopilotSession? _session;
    private bool _isStreaming;

    public int ExitCode { get; } = 0;

    public ChatView (IApplication app, CopilotClient client, string model)
    {
        _app = app;
        _client = client;
        _model = model;

        Title = $"Copilot Chat ({model})";
        Width = Dim.Fill ();
        Height = Dim.Auto ();
        Border.LineStyle = LineStyle.Rounded;
        Border.Thickness = new Thickness (0, 3, 0, 0);
        Border.Settings = BorderSettings.Gradient | BorderSettings.Title;

        _spinner = new SpinnerView
        {
            AutoSpin = false,
            Style = new SpinnerStyle.FingerDance (),
            SyncWithTerminal = true,
            Height = 1,
            Width = 5,
            Visible = false
        };
        Shortcut spinnerShortcut = new () { CommandView = _spinner, MouseHighlightStates = MouseState.None, Enabled = false };
        Shortcut quitShortcut = new (Application.GetDefaultKey (Command.Quit), "Quit", RequestStop);

        _statusBar = new StatusBar { AlignmentModes = AlignmentModes.IgnoreFirstOrLast, SchemeName = SchemeName, BorderStyle = LineStyle.None };
        _statusBar.Add (spinnerShortcut, quitShortcut);

        _conversationView = new MarkdownView
        {
            Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 1, maximumContentDim: Dim.Func (_ => GetMaxConversationHeight ()))
        };

        _inputIndicator = new View
        {
            Text = $"{Glyphs.RightArrow}",
            Y = Pos.Bottom (_conversationView),
            Width = 2,
            Height = 3,
            Enabled = false
        };
        _inputIndicator.Border.LineStyle = LineStyle.Dotted;
        _inputIndicator.Border.Thickness = new Thickness (0, 1, 0, 1);

        _inputField = new TextField { X = Pos.Right (_inputIndicator), Y = Pos.Top (_inputIndicator), Width = Dim.Fill () };
        _inputField.Border.LineStyle = _inputIndicator.Border.LineStyle;
        _inputField.Border.Thickness = new Thickness (0, 1, 0, 1);

        _inputField.Autocomplete.SuggestionGenerator = new SlashCommandSuggestionGenerator ();
        _inputField.Accepted += OnInputAccepted;

        Add (_conversationView, _inputIndicator, _inputField, _statusBar);
        _inputField.SetFocus ();

        return;

        int GetMaxConversationHeight ()
        {
            int screenHeight = _app.Driver?.Screen.Height ?? 40;
            int windowAdornments = GetAdornmentsThickness ().Vertical;
            int siblingHeight = _inputIndicator!.Frame.Height + _statusBar.Frame.Height;

            return Math.Max (1, screenHeight - windowAdornments - siblingHeight);
        }
    }

    private async void OnInputAccepted (object? sender, EventArgs e)
    {
        string text = _inputField.Text.Trim ();

        if (string.IsNullOrEmpty (text) || _isStreaming)
        {
            return;
        }

        _inputField.Text = string.Empty;

        if (text.StartsWith ('/'))
        {
            HandleSlashCommand (text);

            return;
        }

        _isStreaming = true;
        _spinner.AutoSpin = true;
        _spinner.Visible = true;
        _inputField.Enabled = false;

        AppendToConversation ($"\n{Glyphs.BlackCircle} You: {text}\n\n{Glyphs.Diamond} Copilot: ");

        try
        {
            _session ??= await _client.CreateSessionAsync (new SessionConfig
            {
                Model = _model, Streaming = true, OnPermissionRequest = PermissionHandler.ApproveAll
            });

            TaskCompletionSource done = new ();

            using IDisposable subscription = _session.On (evt =>
                                                          {
                                                              switch (evt)
                                                              {
                                                                  case AssistantMessageDeltaEvent delta:
                                                                      _app.Invoke (() => AppendToConversation (delta.Data.DeltaContent));

                                                                      break;

                                                                  case SessionIdleEvent:
                                                                      _app.Invoke (() => AppendToConversation ("\n"));
                                                                      done.TrySetResult ();

                                                                      break;

                                                                  case SessionErrorEvent err:
                                                                      _app.Invoke (() => AppendToConversation ($"\n[Error: {err.Data.Message}]\n"));
                                                                      done.TrySetResult ();

                                                                      break;
                                                              }
                                                          });

            await _session.SendAsync (new MessageOptions { Prompt = text });
            await done.Task;
        }
        catch (Exception ex)
        {
            AppendToConversation ($"\n[Error: {ex.Message}]\n");
        }
        finally
        {
            _isStreaming = false;
            _spinner.Visible = false;
            _spinner.AutoSpin = false;
            _inputField.Enabled = true;
            _inputField.SetFocus ();
        }
    }

    private async Task ValidateAndSwitchModel (string newModel)
    {
        _inputField.Enabled = false;

        try
        {
            CopilotSession testSession = await _client.CreateSessionAsync (new SessionConfig
            {
                Model = newModel,
                Streaming = true,
                OnPermissionRequest = PermissionHandler.ApproveAll
            });

            if (_session is { })
            {
                await _session.DisposeAsync ();
            }

            _session = testSession;
            _model = newModel;

            _app.Invoke (() =>
                         {
                             Title = $"Copilot Chat ({newModel})";
                             AppendToConversation (" \u2713\n");
                             _inputField.Enabled = true;
                             _inputField.SetFocus ();
                         });
        }
        catch (Exception ex)
        {
            _app.Invoke (() =>
                         {
                             AppendToConversation ($"\n{Glyphs.Diamond} Failed: {ex.Message}\n  Keeping model: {_model}\n");
                             _inputField.Enabled = true;
                             _inputField.SetFocus ();
                         });
        }
    }

    private void AppendToConversation (string text)
    {
        _conversationText.Append (text);
        _conversationView.Text = _conversationText.ToString ();
    }

    private void HandleSlashCommand (string command)
    {
        string cmd = command.TrimStart ('/').ToLowerInvariant ();
        string [] parts = cmd.Split (' ', 2);

        switch (parts [0])
        {
            case "quit" or "exit":
                RequestStop ();

                break;

            case "clear":
                _conversationText.Clear ();
                _conversationView.Text = string.Empty;
                AppendToConversation ($"{Glyphs.Diamond} Conversation cleared.\n");

                break;

            case "model":
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace (parts [1]))
                {
                    string newModel = parts [1].Trim ();
                    AppendToConversation ($"\n{Glyphs.Diamond} Switching to {newModel}...");
                    _ = ValidateAndSwitchModel (newModel);
                }
                else
                {
                    AppendToConversation ($"\n{Glyphs.Diamond} Current model: {_model}\n  Usage: /model <name>\n");
                }

                break;

            case "help":
                AppendToConversation ($"\n{Glyphs.Diamond} Commands:\n"
                                      + "  /help          Show this help\n"
                                      + "  /clear         Clear conversation\n"
                                      + "  /model <name>  Switch model\n"
                                      + "  /compact       Summarize conversation\n"
                                      + "  /quit          Exit chat\n");

                break;

            case "compact":
                AppendToConversation ($"\n{Glyphs.Diamond} Compacting conversation...\n");

                // TODO: Send conversation summary request to model

                break;

            default:
                AppendToConversation ($"\n{Glyphs.Diamond} Unknown command: /{parts [0]}. Type /help for commands.\n");

                break;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _session?.DisposeAsync ().AsTask ().GetAwaiter ().GetResult ();
        }

        base.Dispose (disposing);
    }
}