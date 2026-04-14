#nullable enable

using System.Collections.ObjectModel;
using System.Text.Json;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Markdown", "Demonstrates MarkdownView by browsing Terminal.Gui docs from GitHub.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public class Markdown : Scenario
{
    private static readonly HttpClient _httpClient = new ();

    private const string _docsApiUrl = "https://api.github.com/repos/gui-cs/Terminal.Gui/contents/docfx/docs?ref=develop";

    private IApplication? _app;
    private ListView? _docList;
    private MarkdownView? _markdownView;
    private FrameView? _viewerFrame;
    private Shortcut? _statusShortcut;
    private SpinnerView? _spinner;

    private List<DocEntry> _docs = [];

    public override void Main ()
    {
        _app = Application.Create ();
        _app.Init ();

        Window window = new ()
        {
            Title = GetName (),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        FrameView listFrame = new ()
        {
            Title = "_Docs",
            X = 0,
            Y = 0,
            Width = 30,
            Height = Dim.Fill (1)
        };

        _docList = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        _docList.ValueChanged += OnDocListValueChanged;
        listFrame.Add (_docList);

        _viewerFrame = new ()
        {
            Title = "MarkdownView",
            X = Pos.Right (listFrame),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };

        _markdownView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        _markdownView.LinkClicked += (_, e) =>
                                     {
                                         if (_statusShortcut is { })
                                         {
                                             _statusShortcut.Title = e.Url;
                                         }

                                         e.Handled = true;
                                     };

        _viewerFrame.Add (_markdownView);

        _spinner = new ()
        {
            AutoSpin = true,
            Visible = false
        };

        _statusShortcut = new (Key.Empty, "Ready", null);

        Shortcut spinnerShortcut = new () { CommandView = _spinner, Title = "" };

        StatusBar statusBar = new (
                                   [
                                       new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", () => window.RequestStop ()),
                                       _statusShortcut,
                                       spinnerShortcut
                                   ]);

        window.Add (listFrame, _viewerFrame, statusBar);

        window.Initialized += (_, _) => _ = LoadDocListAsync ();

        _app.Run (window);

        window.Dispose ();
        _app.Dispose ();
    }

    private async Task LoadDocListAsync ()
    {
        ShowSpinner ("Loading doc list...");

        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear ();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd ("Terminal.Gui-UICatalog/1.0");

            string json = await _httpClient.GetStringAsync (_docsApiUrl).ConfigureAwait (false);

            using JsonDocument doc = JsonDocument.Parse (json);
            List<DocEntry> entries = [];

            foreach (JsonElement element in doc.RootElement.EnumerateArray ())
            {
                string? name = element.GetProperty ("name").GetString ();
                string? downloadUrl = element.GetProperty ("download_url").GetString ();

                if (name is not null && downloadUrl is not null && name.EndsWith (".md", StringComparison.OrdinalIgnoreCase))
                {
                    entries.Add (new DocEntry (name, downloadUrl));
                }
            }

            entries.Sort ((a, b) => string.Compare (a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            _app?.Invoke (() =>
                          {
                              _docs = entries;
                              ObservableCollection<string> names = new (_docs.Select (d => d.Name));
                              _docList?.SetSource (names);
                              HideSpinner ("Ready");
                          });
        }
        catch (Exception ex)
        {
            _app?.Invoke (() =>
                          {
                              HideSpinner ("Error loading doc list");

                              if (_markdownView is { })
                              {
                                  _markdownView.Markdown = $"# Error\n\nFailed to load doc list:\n\n`{ex.Message}`";
                              }
                          });
        }
    }

    private void OnDocListValueChanged (object? sender, ValueChangedEventArgs<int?> e)
    {
        if (e.NewValue is null || e.NewValue < 0 || e.NewValue >= _docs.Count)
        {
            return;
        }

        DocEntry entry = _docs [e.NewValue.Value];
        _ = LoadDocContentAsync (entry);
    }

    private async Task LoadDocContentAsync (DocEntry entry)
    {
        ShowSpinner ($"Loading {entry.Name}...");

        try
        {
            string content = await _httpClient.GetStringAsync (entry.DownloadUrl).ConfigureAwait (false);

            _app?.Invoke (() =>
                          {
                              if (_markdownView is { })
                              {
                                  _markdownView.Markdown = content;
                              }

                              if (_viewerFrame is { })
                              {
                                  _viewerFrame.Title = entry.Name;
                              }

                              HideSpinner (entry.Name);
                          });
        }
        catch (Exception ex)
        {
            _app?.Invoke (() =>
                          {
                              HideSpinner ("Error");

                              if (_markdownView is { })
                              {
                                  _markdownView.Markdown = $"# Error\n\nFailed to load `{entry.Name}`:\n\n`{ex.Message}`";
                              }
                          });
        }
    }

    private void ShowSpinner (string message)
    {
        _app?.Invoke (() =>
                      {
                          if (_spinner is { })
                          {
                              _spinner.Visible = true;
                          }

                          if (_statusShortcut is { })
                          {
                              _statusShortcut.Title = message;
                          }
                      });
    }

    private void HideSpinner (string message)
    {
        if (_spinner is { })
        {
            _spinner.Visible = false;
        }

        if (_statusShortcut is { })
        {
            _statusShortcut.Title = message;
        }
    }

    private sealed record DocEntry (string Name, string DownloadUrl);
}
