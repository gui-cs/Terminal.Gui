// mdv — A Terminal.Gui Markdown viewer
//
// Usage:
//   mdv <file.md> [file2.md ...]               Full-screen interactive mode (default)
//   mdv --print <file.md> [file2.md ...]        Print mode: renders to terminal and exits

using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Drawing;
using System.Reflection;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TextMateSharp.Grammars;
using Command = Terminal.Gui.Input.Command;

// ReSharper disable AccessToDisposedClosure

Option<bool> printOption = new ("--print") { Description = "Print mode: renders markdown to the terminal and exits." };
printOption.Aliases.Add ("-p");

Option<ThemeName> themeOption = new ("--theme")
{
    Description = $"The syntax-highlighting theme to use. Available: {string.Join (", ", Enum.GetNames<ThemeName> ())}",
    DefaultValueFactory = _ => ThemeName.DarkPlus
};
themeOption.Aliases.Add ("-t");

Argument<string []> filesArgument = new ("files")
{
    Description = "One or more markdown file paths (glob patterns supported).", Arity = ArgumentArity.OneOrMore
};

RootCommand rootCommand = new ("mdv — A Terminal.Gui Markdown viewer") { printOption, themeOption, filesArgument };

// Override --help to render the embedded README.md in print mode
HelpOption helpOption = rootCommand.Options.OfType<HelpOption> ().First ();
helpOption.Action = new MarkdownHelpAction (() => RenderMarkdown (ReadEmbeddedReadme (), ThemeName.DarkPlus));

rootCommand.SetAction (parseResult =>
                       {
                           bool print = parseResult.GetValue (printOption);
                           ThemeName syntaxTheme = parseResult.GetValue (themeOption);
                           string [] filePatterns = parseResult.GetValue (filesArgument) ?? [];

                           List<string> files = ExpandFiles ([.. filePatterns]);

                           if (files.Count == 0)
                           {
                               Console.Error.WriteLine ("No matching files found.");

                               return;
                           }

                           ConfigurationManager.Enable (ConfigLocations.All);

                           if (print)
                           {
                               RenderMarkdown (string.Join ("\n\n---\n\n", files.Select (File.ReadAllText)), syntaxTheme);
                           }
                           else
                           {
                               RunFullScreen (files, syntaxTheme);
                           }
                       });

return rootCommand.Parse (args).Invoke ();

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
// Print mode — render markdown content into the scrollback buffer, then exit
// ---------------------------------------------------------------------------

static void RenderMarkdown (string markdown, ThemeName syntaxTheme)
{
    // Prevent the ANSI driver from trying to read/write real terminal size or capabilities,
    // since we're just emitting ANSI and exiting immediately.
    Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
    IApplication app = Application.Create ();
    app.Init (DriverRegistry.Names.ANSI);

    // Set the screen size to the current size
    app.Driver?.SetScreenSize (Console.WindowWidth, Console.WindowHeight);

    Markdown markdownView = new ()
    {
        App = app,
        SyntaxHighlighter = new TextMateSyntaxHighlighter (syntaxTheme),
        Width = Dim.Fill (),
        Height = Dim.Fill (),
        Text = markdown
    };

    // Layout to get natural size
    markdownView.SetRelativeLayout (app.Screen.Size);
    markdownView.Layout ();

    // Set the screen size to the natural size of the formatted markdown
    app.Driver?.SetScreenSize (markdownView.GetContentSize ().Width, markdownView.GetContentSize ().Height);
    markdownView.SetRelativeLayout (app.Screen.Size);

    markdownView.Frame = app.Screen with { X = 0, Y = 0 };
    markdownView.Layout ();

    // Ensure the contents are clear
    app.Driver?.ClearContents ();

    markdownView.Draw ();
    Console.WriteLine (app.Driver?.ToAnsi ());
}

static string ReadEmbeddedReadme ()
{
    var assembly = Assembly.GetExecutingAssembly ();
    string resourceName = assembly.GetManifestResourceNames ().First (n => n.EndsWith ("README.md", StringComparison.Ordinal));

    using Stream stream = assembly.GetManifestResourceStream (resourceName)!;
    using StreamReader reader = new (stream);

    return reader.ReadToEnd ();
}

// ---------------------------------------------------------------------------
// Full-screen mode — interactive viewer with StatusBar
// ---------------------------------------------------------------------------

static void RunFullScreen (List<string> files, ThemeName syntaxTheme)
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

    List<Shortcut> statusItems = [new (Application.GetDefaultKey (Command.Quit), "Quit", window.RequestStop), contentWidthShortcut];

    // Theme selector
    DropDownList<ThemeName> themeDropDown = new () { Value = syntaxTheme, CanFocus = false };

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
        List<string> fileNames = [.. files.Select (Path.GetFileName)!];
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

    //Load & Sync content-width control after initial layout
    window.Initialized += (_, _) =>
                          {
                              // Load the first file
                              LoadFile (files [0]);
                              updatingContentWidth = true;
                              contentWidthUpDown.Value = markdownView.Viewport.Width;
                              updatingContentWidth = false;
                          };

    app.Run (window);
    window.Dispose ();
    app.Dispose ();

    return;

    void LoadFile (string filePath)
    {
        string content = File.ReadAllText (filePath);
        markdownView.Text = content;

        FileInfo fileInfo = new (filePath);
        fileSizeShortcut.Title = FormatFileSize (fileInfo.Length);
        statusShortcut.Title = Path.GetFileName (filePath);
    }
}

// ---------------------------------------------------------------------------
// Custom help action — renders the embedded README.md via the print pipeline
// ---------------------------------------------------------------------------

internal sealed class MarkdownHelpAction (Action renderHelp) : SynchronousCommandLineAction
{
    public override int Invoke (ParseResult parseResult)
    {
        renderHelp ();

        return 0;
    }
}
