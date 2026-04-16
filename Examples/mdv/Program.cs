// mdv — A Terminal.Gui Markdown viewer
//
// Usage:
//   mdv <file.md> [file2.md ...]               Inline mode: renders and exits
//   mdv --full-screen <file.md> [file2.md ...]  Full-screen interactive mode

using System.Collections.ObjectModel;
using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TextMateSharp.Grammars;
// ReSharper disable AccessToDisposedClosure

ConfigurationManager.RuntimeConfig = """
                                     {
                                         "Theme": "Anders"
                                     }
                                     """;
ConfigurationManager.Enable (ConfigLocations.All);

var fullScreen = false;
ThemeName syntaxTheme = ThemeName.DarkPlus;
List<string> filePatterns = [];

for (var i = 0; i < args.Length; i++)
{
    string arg = args [i];

    if (arg is "--full-screen" or "-f")
    {
        fullScreen = true;
    }
    else if (arg is "--theme" or "-t" && i + 1 < args.Length)
    {
        i++;

        if (Enum.TryParse (args [i], true, out ThemeName parsed))
        {
            syntaxTheme = parsed;
        }
        else
        {
            Console.Error.WriteLine ($"Unknown theme '{args [i]}'. Available: {string.Join (", ", Enum.GetNames<ThemeName> ())}");

            return 1;
        }
    }
    else
    {
        filePatterns.Add (arg);
    }
}

List<string> files = ExpandFiles (filePatterns);

if (files.Count == 0)
{
    Console.Error.WriteLine ("Usage: mdv [--full-screen] [--theme <ThemeName>] <file.md> [file2.md ...]");

    return 1;
}

return fullScreen ? RunFullScreen (files, syntaxTheme) : RunInline (files, syntaxTheme);

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static List<string> ExpandFiles (List<string> patterns)
{
    List<string> result = [];

    foreach (string pattern in patterns)
    {
        if (pattern.Contains ('*') || pattern.Contains ('?'))
        {
            string directory = Path.GetDirectoryName (pattern) is { Length: > 0 } dir ? dir : ".";
            string filePattern = Path.GetFileName (pattern);

            if (Directory.Exists (directory))
            {
                result.AddRange (Directory.GetFiles (directory, filePattern));
            }
        }
        else if (File.Exists (pattern))
        {
            result.Add (Path.GetFullPath (pattern));
        }
        else
        {
            Console.Error.WriteLine ($"Warning: File not found: {pattern}");
        }
    }

    return result;
}

static string FormatFileSize (long bytes)
{
    string [] sizes = ["B", "KB", "MB", "GB", "TB"];
    var order = 0;
    double size = bytes;

    while (size >= 1024 && order < sizes.Length - 1)
    {
        order++;
        size /= 1024;
    }

    return $"{size:0.##} {sizes [order]}";
}

// ---------------------------------------------------------------------------
// Inline mode — render markdown into the scrollback buffer, then exit
// ---------------------------------------------------------------------------

static int RunInline (List<string> files, ThemeName syntaxTheme)
{
    string markdown = string.Join ("\n\n---\n\n", files.Select (File.ReadAllText));

    Application.AppModel = AppModel.Inline;
    IApplication app = Application.Create ().Init ();

    Runnable window = new () { Title = "TUI Markdown Viewer", Width = Dim.Fill (), Height = Dim.Auto () };

    Markdown markdownView = new () { Width = Dim.Fill (), Height = Dim.Auto (), Text = markdown, SyntaxHighlighter = new TextMateSyntaxHighlighter (syntaxTheme) };

    // No scrollbar in inline mode — content should be fully visible
    markdownView.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;

    window.Add (markdownView);

    // Quit after the first render so the content stays in scrollback
    window.Initialized += (_, _) => app.Invoke (window.RequestStop);

    app.Run (window);
    window.Dispose ();
    app.Dispose ();

    return 0;
}

// ---------------------------------------------------------------------------
// Full-screen mode — interactive viewer with StatusBar
// ---------------------------------------------------------------------------

static int RunFullScreen (List<string> files, ThemeName syntaxTheme)
{
    IApplication app = Application.Create ().Init ();

    Runnable window = new () { Title = "TUI Markdown Viewer", Width = Dim.Fill (), Height = Dim.Fill () };

    Markdown markdownView = new ()
    {
        Width = Dim.Fill (),
        Height = Dim.Fill (1), // leave room for StatusBar
        SyntaxHighlighter = new TextMateSyntaxHighlighter (syntaxTheme),
        UseThemeBackground = true
    };

    // Vertical scrollbar is already enabled by MarkdownView constructor
    markdownView.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;

    // -----------------------------------------------------------------------
    // StatusBar items (mirrors the Deepdives scenario)
    // -----------------------------------------------------------------------

    var updatingContentWidth = false;

    NumericUpDown contentWidthUpDown = new () { Value = 80 };

    contentWidthUpDown.ValueChanging += (_, changeArgs) =>
                                        {
                                            if (updatingContentWidth)
                                            {
                                                return;
                                            }

                                            int newWidth = changeArgs.NewValue;

                                            if (newWidth < 1)
                                            {
                                                changeArgs.Handled = true;

                                                return;
                                            }

                                            Size currentContentSize = markdownView.GetContentSize ();
                                            markdownView.SetContentSize (currentContentSize with { Width = newWidth });
                                        };

    Shortcut contentWidthShortcut = new () { CommandView = contentWidthUpDown, HelpText = "Content Width" };

    Shortcut lineCountShortcut = new () { Title = "0 lines", MouseHighlightStates = MouseState.None, Enabled = false };

    Shortcut fileSizeShortcut = new () { Title = "0 B", MouseHighlightStates = MouseState.None, Enabled = false };

    Shortcut statusShortcut = new (Key.Empty, "Ready", null);

    SpinnerView spinner = new () { Style = new SpinnerStyle.Aesthetic (), Width = 8, AutoSpin = false, Visible = false };

    Shortcut spinnerShortcut = new () { CommandView = spinner, Title = "" };

    // -----------------------------------------------------------------------
    // MarkdownView event wiring
    // -----------------------------------------------------------------------

    markdownView.LinkClicked += (_, e) =>
                                {
                                    statusShortcut.Title = e.Url;
                                    e.Handled = true;
                                };

    markdownView.SubViewsLaidOut += (_, _) => { lineCountShortcut.Title = $"{markdownView.LineCount} lines"; };

    markdownView.ViewportChanged += (_, e) =>
                                    {
                                        if (e.NewViewport.Size == e.OldViewport.Size)
                                        {
                                            return;
                                        }

                                        updatingContentWidth = true;
                                        contentWidthUpDown.Value = markdownView.Viewport.Width;
                                        updatingContentWidth = false;
                                    };

    // -----------------------------------------------------------------------
    // Build the StatusBar
    // -----------------------------------------------------------------------

    List<Shortcut> statusItems =
    [
        new (Application.GetDefaultKey (Command.Quit), "Quit", window.RequestStop),
        contentWidthShortcut
    ];

    // Theme selector
    DropDownList<ThemeName> themeDropDown = new () { Value = syntaxTheme };

    themeDropDown.ValueChanged += (_, e) =>
                                  {
                                      if (e.Value is not { } themeName)
                                      {
                                          return;
                                      }

                                      TextMateSyntaxHighlighter highlighter = new (themeName);
                                      markdownView.SyntaxHighlighter = highlighter;

                                      string text = markdownView.Text;
                                      markdownView.Text = string.Empty;
                                      markdownView.Text = text;
                                  };

    statusItems.Add (new Shortcut { Title = "Theme", CommandView = themeDropDown });

    // Theme background toggle
    CheckBox themeBgCheckBox = new () { Text = "Theme _BG", Value = CheckState.Checked };

    themeBgCheckBox.ValueChanged += (_, e) =>
                                           {
                                               markdownView.UseThemeBackground = e.NewValue == CheckState.Checked;

                                               string text = markdownView.Text;
                                               markdownView.Text = string.Empty;
                                               markdownView.Text = text;
                                           };

    statusItems.Add (new Shortcut { CommandView = themeBgCheckBox });

    statusItems.AddRange ([lineCountShortcut, fileSizeShortcut, statusShortcut, spinnerShortcut]);

    // File selector when multiple files are provided
    if (files.Count > 1)
    {
        List<string> fileNames = [.. files.Select (f => Path.GetFileName (f))];
        ObservableCollection<string> fileNamesOc = new (fileNames);

        DropDownList fileSelector = new () { Source = new ListWrapper<string> (fileNamesOc), ReadOnly = true, Text = fileNames [0], Width = 30 };

        fileSelector.ValueChanged += (_, _) =>
                                     {
                                         string selectedName = fileSelector.Text;
                                         int index = fileNames.IndexOf (selectedName);

                                         if (index < 0 || index >= files.Count)
                                         {
                                             return;
                                         }

                                         LoadFile (files [index]);
                                     };

        Shortcut fileSelectorShortcut = new () { CommandView = fileSelector, HelpText = "File" };
        statusItems.Insert (1, fileSelectorShortcut);
    }

    StatusBar statusBar = new (statusItems) { AlignmentModes = AlignmentModes.IgnoreFirstOrLast };

    window.Add (markdownView, statusBar);

    // Load the first file
    LoadFile (files [0]);

    // Sync content-width control after initial layout and scroll to top
    window.Initialized += (_, _) =>
                          {
                              updatingContentWidth = true;
                              contentWidthUpDown.Value = markdownView.Viewport.Width;
                              updatingContentWidth = false;
                              markdownView.Viewport = markdownView.Viewport with { X = 0, Y = 0 };
                          };

    app.Run (window);
    window.Dispose ();
    app.Dispose ();

    return 0;

    // -- local helper -------------------------------------------------------
    void LoadFile (string filePath)
    {
        string content = File.ReadAllText (filePath);
        markdownView.Text = content;
        markdownView.Viewport = markdownView.Viewport with { X = 0, Y = 0 };

        FileInfo fileInfo = new (filePath);
        fileSizeShortcut.Title = FormatFileSize (fileInfo.Length);
        statusShortcut.Title = Path.GetFileName (filePath);
    }
}
