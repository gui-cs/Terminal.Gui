using System.Text;
using GitHub.Copilot.SDK;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace AI;

/// <summary>
///     Handles single-turn mode: streams one answer inline, then exits.
/// </summary>
internal sealed class SingleTurnView : Window
{
    private readonly IApplication _app;
    private readonly CopilotClient _client;
    private readonly string _model;
    private readonly string _prompt;
    private readonly MarkdownView _responseView;

    public SingleTurnView (IApplication app, CopilotClient client, string model, string prompt)
    {
        _app = app;
        _client = client;
        _model = model;
        _prompt = prompt;

        Title = $"Copilot ({model})";
        Width = Dim.Fill ();
        Height = Dim.Auto ();
        Border.LineStyle = LineStyle.Rounded;

        _responseView = new MarkdownView { Width = Dim.Fill (), Height = Dim.Auto () };

        Add (_responseView);
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (newIsRunning)
        {
            _ = streamResponse ();
        }
    }

    private async Task streamResponse ()
    {
        try
        {
            await using CopilotSession session = await _client.CreateSessionAsync (new SessionConfig
            {
                Model = _model,
                Streaming = true,
                OnPermissionRequest = PermissionHandler.ApproveAll
            });

            StringBuilder responseText = new ();
            TaskCompletionSource done = new ();

            session.On (evt =>
                        {
                            switch (evt)
                            {
                                case AssistantMessageDeltaEvent delta:
                                    responseText.Append (delta.Data.DeltaContent);

                                    _app.Invoke (() => { _responseView.Text = responseText.ToString (); });

                                    break;

                                case SessionIdleEvent:
                                    done.TrySetResult ();

                                    break;

                                case SessionErrorEvent err:
                                    _app.Invoke (() => { _responseView.Text = $"Error: {err.Data.Message}"; });
                                    done.TrySetResult ();

                                    break;
                            }
                        });

            await session.SendAsync (new MessageOptions { Prompt = _prompt });
            await done.Task;
        }
        catch (Exception ex)
        {
            _app.Invoke (() => _responseView.Text = $"Error: {ex.Message}");
        }

        // Give the UI a moment to render the final update, then exit
        _app.Invoke (RequestStop);
    }

    public static int Run (IApplication app, CopilotClient client, string model, string prompt)
    {
        SingleTurnView view = new (app, client, model, prompt);
        app.Run (view);
        app.Dispose ();

        return 0;
    }
}