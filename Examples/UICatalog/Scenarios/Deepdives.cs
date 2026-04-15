#nullable enable

using System.Collections.ObjectModel;
using System.Text.Json;
using Terminal.Gui.SyntaxHighlighting;

// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Deepdives", "Use MarkDownView to provide a TG Deep Dive browser.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public class Deepdives : Scenario
{
    private static readonly HttpClient _httpClient = new ();

    private const string DOCS_API_URL = "https://api.github.com/repos/gui-cs/Terminal.Gui/contents/docfx/docs?ref=develop";

    private IApplication? _app;
    private ListView? _docList;
    private MarkdownView? _markdownView;
    private FrameView? _viewerFrame;
    private Shortcut? _statusShortcut;
    private SpinnerView? _spinner;
    private NumericUpDown? _contentWidthUpDown;
    private bool _updatingContentWidth;

    private List<DocEntry> _docs = [];

    public override void Main ()
    {
        _app = Application.Create ();
        _app.Init ();

        Window window = new () { Title = GetName (), Width = Dim.Fill (), Height = Dim.Fill () };

        FrameView listFrame = new ()
        {
            Title = "_Docs",
            X = 0,
            Y = 0,
            Width = 30,
            Height = Dim.Fill (1)
        };

        _docList = new ListView { Width = Dim.Fill (), Height = Dim.Fill () };

        _docList.ValueChanged += OnDocListValueChanged;
        listFrame.Add (_docList);

        _viewerFrame = new FrameView
        {
            Title = "MarkdownView",
            X = Pos.Right (listFrame),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };

        _markdownView = new MarkdownView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),

            // Default is TextMateSharp.Grammars.ThemeName.DarkPlus
            SyntaxHighlighter = new TextMateSyntaxHighlighter ()
        };
        _markdownView.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;

        _markdownView.LinkClicked += (_, e) =>
                                     {
                                         _statusShortcut?.Title = e.Url;

                                         e.Handled = true;
                                     };

        // Reset the content width control only when viewport SIZE changes (not scroll position)
        _markdownView.ViewportChanged += (_, e) =>
                                         {
                                             if (e.NewViewport.Size == e.OldViewport.Size)
                                             {
                                                 return;
                                             }

                                             SyncContentWidthToViewport ();
                                         };

        _viewerFrame.Add (_markdownView);

        _spinner = new SpinnerView { Style = new SpinnerStyle.Aesthetic (), Width = 8, AutoSpin = false, Visible = false };

        _statusShortcut = new Shortcut (Key.Empty, "Ready", null);

        Shortcut spinnerShortcut = new () { CommandView = _spinner, Title = "" };

        _contentWidthUpDown = new NumericUpDown { Value = 80 };

        _contentWidthUpDown.ValueChanging += (_, args) =>
                                             {
                                                 if (_markdownView is null || _updatingContentWidth)
                                                 {
                                                     return;
                                                 }

                                                 int newWidth = args.NewValue;

                                                 if (newWidth < 1)
                                                 {
                                                     args.Handled = true;

                                                     return;
                                                 }

                                                 Size currentContentSize = _markdownView.GetContentSize ();
                                                 _markdownView.SetContentSize (currentContentSize with { Width = newWidth });
                                             };

        Shortcut contentWidthShortcut = new () { CommandView = _contentWidthUpDown, HelpText = "Content Width" };

        StatusBar statusBar = new ([
                                       new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", window.RequestStop),
                                       contentWidthShortcut,
                                       _statusShortcut,
                                       spinnerShortcut
                                   ]) { AlignmentModes = AlignmentModes.IgnoreFirstOrLast };

        window.Add (listFrame, _viewerFrame, statusBar);

        // Set initial content width value after layout
        window.Initialized += (_, _) =>
                              {
                                  _ = LoadDocListAsync ();
                                  SyncContentWidthToViewport ();
                              };

        _app.Run (window);

        window.Dispose ();
        _app.Dispose ();
    }

    private void SyncContentWidthToViewport ()
    {
        if (_markdownView is null || _contentWidthUpDown is null)
        {
            return;
        }

        _updatingContentWidth = true;
        _contentWidthUpDown.Value = _markdownView.Viewport.Width;
        _updatingContentWidth = false;
    }

    private async Task LoadDocListAsync ()
    {
        ShowSpinner ("Loading doc list...");

        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear ();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd ("Terminal.Gui-UICatalog/1.0");

            string json = await _httpClient.GetStringAsync (DOCS_API_URL).ConfigureAwait (false);

            using JsonDocument doc = JsonDocument.Parse (json);
            List<DocEntry> entries = [];

            foreach (JsonElement element in doc.RootElement.EnumerateArray ())
            {
                string? name = element.GetProperty ("name").GetString ();
                string? downloadUrl = element.GetProperty ("download_url").GetString ();

                if (name is { } && downloadUrl is { } && name.EndsWith (".md", StringComparison.OrdinalIgnoreCase))
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
                              _docList?.SelectedItem = 0;
                              HideSpinner ("Ready");
                          });
        }
        catch (Exception ex)
        {
            _app?.Invoke (() =>
                          {
                              HideSpinner ("Error loading doc list");

                              _markdownView?.Markdown = $"# Error\n\nFailed to load doc list:\n\n`{ex.Message}`";
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
                              _markdownView?.Markdown = content;

                              _markdownView?.Viewport = _markdownView.Viewport with { X = 0, Y = 0 };

                              _viewerFrame?.Title = entry.Name;

                              HideSpinner (entry.Name);
                          });
        }
        catch (Exception ex)
        {
            _app?.Invoke (() =>
                          {
                              HideSpinner ("Error");

                              _markdownView?.Markdown = $"# Error\n\nFailed to load `{entry.Name}`:\n\n`{ex.Message}`";
                          });
        }
    }

    private void ShowSpinner (string message) =>
        _app?.Invoke (() =>
                      {
                          _spinner?.Visible = true;
                          _spinner?.AutoSpin = true;

                          _statusShortcut?.Title = message;
                      });

    private void HideSpinner (string message)
    {
        _spinner?.AutoSpin = false;
        _spinner?.Visible = false;

        _statusShortcut?.Title = message;
    }

    private sealed record DocEntry (string Name, string DownloadUrl);
}
