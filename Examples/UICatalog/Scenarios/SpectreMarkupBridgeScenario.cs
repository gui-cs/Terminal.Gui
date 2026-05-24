#nullable enable
using System.Collections.ObjectModel;
using Terminal.Gui.Interop.Spectre;
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Spectre Markup Bridge", "Demonstrates Spectre markup parsing and SetMarkup integration.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
public sealed class SpectreMarkupBridgeScenario : Scenario
{
    private readonly ObservableCollection<string> _segments = [];
    private Label? _previewLabel;
    private TextField? _markupField;
    private Label? _statusLabel;
    private ListView? _segmentListView;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        Label intro = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Text = "Enter Spectre markup, then apply it to the preview label and inspect parsed StyledSegment values."
        };

        _markupField = new TextField
        {
            X = 0,
            Y = Pos.Bottom (intro),
            Width = Dim.Fill () - 17,
            Height = 1,
            Text = "[bold yellow]Terminal.Gui[/] [link=https://spectreconsole.net][underline blue]Spectre link[/][/]"
        };

        Button applyButton = new ()
        {
            X = Pos.Right (_markupField),
            Y = Pos.Top (_markupField),
            Width = 17,
            Text = "_Apply Markup"
        };
        applyButton.Accepting += (_, e) =>
                                {
                                    e.Handled = true;
                                    ApplyMarkup ();
                                };

        FrameView previewFrame = new ()
        {
            X = 0,
            Y = Pos.Bottom (_markupField),
            Width = Dim.Fill (),
            Height = 5,
            Title = "Preview"
        };

        _previewLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = 1
        };

        _statusLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom (_previewLabel),
            Width = Dim.Fill (),
            Height = 1
        };

        previewFrame.Add (_previewLabel, _statusLabel);

        _segmentListView = new ListView
        {
            X = 0,
            Y = Pos.Bottom (previewFrame),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = "Parsed segments",
            Source = new ListWrapper<string> (_segments)
        };

        appWindow.Add (intro, _markupField, applyButton, previewFrame, _segmentListView);
        ApplyMarkup ();
        app.Run (appWindow);
    }

    private void ApplyMarkup ()
    {
        if (_previewLabel is null || _markupField is null || _statusLabel is null || _segmentListView is null)
        {
            return;
        }

        string markup = _markupField.Text;

        try
        {
            IReadOnlyList<StyledSegment> parsed = SpectreMarkupBridge.ParseMarkup (markup);
            _previewLabel.SetMarkup (markup);

            _segments.Clear ();

            for (int i = 0; i < parsed.Count; i++)
            {
                StyledSegment segment = parsed [i];
                TgAttribute attribute = segment.Attribute ?? TgAttribute.Default;
                string escapedText = segment.Text.Replace ("\n", "\\n", StringComparison.Ordinal);
                string url = segment.Url ?? "-";
                _segments.Add ($"{i:00}: '{escapedText}' | fg={attribute.Foreground} bg={attribute.Background} style={attribute.Style} url={url}");
            }

            _segmentListView.SetSource (_segments);
            _statusLabel.SetMarkup ($"[green]Parsed {parsed.Count} segment(s) successfully.[/]");
        }
        catch (Exception ex)
        {
            _segments.Clear ();
            _segmentListView.SetSource (_segments);
            _statusLabel.Text = $"Parse error: {ex.Message}";
            _previewLabel.Text = string.Empty;
        }
    }
}
