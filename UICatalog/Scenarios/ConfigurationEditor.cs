#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Configuration Editor", "Edits Terminal.Gui Config Files.")]
[ScenarioCategory ("TabView")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Files and IO")]
[ScenarioCategory ("TextView")]
public class ConfigurationEditor : Scenario
{
    private static ColorScheme _editorColorScheme = new ()
    {
        Normal = new Attribute (Color.Red, Color.White),
        Focus = new Attribute (Color.Red, Color.Black),
        HotFocus = new Attribute (Color.BrightRed, Color.Black),
        HotNormal = new Attribute (Color.Magenta, Color.White)
    };

    private static Action? _editorColorSchemeChanged;
    private TabView? _tabView;
    private Shortcut? _lenShortcut;

    [SerializableConfigurationProperty (Scope = typeof (AppScope))]
    public static ColorScheme EditorColorScheme
    {
        get => _editorColorScheme;
        set
        {
            _editorColorScheme = value;
            _editorColorSchemeChanged?.Invoke ();
        }
    }

    public override void Main ()
    {
        Application.Init ();

        Toplevel top = new ();

        _lenShortcut = new Shortcut ()
        {
            Title = "",
        };

        var quitShortcut = new Shortcut ()
        {
            Key = Application.QuitKey,
            Title = $"Quit",
            Action = Quit
        };

        var reloadShortcut = new Shortcut ()
        {
            Key = Key.F5.WithShift,
            Title = "Reload",
        };
        reloadShortcut.Accepting += (s, e) => { Reload (); };

        var saveShortcut = new Shortcut ()
        {
            Key = Key.F4,
            Title = "Save",
            Action = Save
        };

        var statusBar = new StatusBar ([quitShortcut, reloadShortcut, saveShortcut, _lenShortcut]);

        _tabView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (Dim.Func (() => statusBar.Frame.Height))
        };

        top.Add (_tabView, statusBar);

        top.Loaded += (s, a) =>
                      {
                          Open ();
                          _editorColorSchemeChanged?.Invoke ();
                      };

        void OnEditorColorSchemeChanged ()
        {
            if (Application.Top is { })
            {
                return;
            }

            foreach (ConfigTextView t in _tabView.SubViews.Where (v => v is ConfigTextView).Cast<ConfigTextView> ())
            {
                t.ColorScheme = EditorColorScheme;
            }
        }

        _editorColorSchemeChanged += OnEditorColorSchemeChanged;

        Application.Run (top);
        _editorColorSchemeChanged -= OnEditorColorSchemeChanged;
        top.Dispose ();

        Application.Shutdown ();
    }
    public void Save ()
    {
        if (Application.Navigation?.GetFocused () is ConfigTextView editor)
        {
            editor.Save ();
        }
    }

    private void Open ()
    {
        foreach (var config in ConfigurationManager.Settings!.Sources)
        {
            var homeDir = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}";
            var fileInfo = new FileInfo (config.Value.Replace ("~", homeDir));

            var editor = new ConfigTextView
            {
                Title = config.Value.StartsWith ("resource://") ? fileInfo.Name : config.Value,
                Width = Dim.Fill (),
                Height = Dim.Fill(),
                FileInfo = fileInfo,
            };

            Tab tab = new Tab ()
            {
                View = editor,
                DisplayText = config.Key.ToString ()
            };

            _tabView!.AddTab (tab, false);

            editor.Read ();

            editor.ContentsChanged += (sender, args) =>
                                      {
                                          _lenShortcut!.Title = _lenShortcut!.Title.Replace ("*", "");
                                          if (editor.IsDirty)
                                          {
                                              _lenShortcut!.Title += "*";
                                          }
                                      };

            _lenShortcut!.Title = $"{editor.Title}";
        }

        _tabView!.SelectedTabChanged += (sender, args) =>
                                       {
                                           _lenShortcut!.Title = $"{args.NewTab.View!.Title}";
                                       };

    }

    private void Quit ()
    {
        foreach (ConfigTextView editor in _tabView!.Tabs.Select(v =>
                                                                {
                                                                    if (v.View is ConfigTextView ctv)
                                                                    {
                                                                        return ctv;
                                                                    }

                                                                    return null;
                                                                }).Cast<ConfigTextView> ())
        {
            if (editor.IsDirty)
            {
                int result = MessageBox.Query (
                                               "Save Changes",
                                               $"Save changes to {editor.FileInfo!.Name}",
                                               "_Yes",
                                               "_No",
                                               "_Cancel"
                                              );

                if (result == -1 || result == 2)
                {
                    // user cancelled
                }

                if (result == 0)
                {
                    editor.Save ();
                }
            }
        }

        Application.RequestStop ();
    }

    private void Reload ()
    {
        if (Application.Navigation?.GetFocused () is ConfigTextView editor)
        {
            editor.Read ();
        }
    }

    private class ConfigTextView : TextView
    {
        internal ConfigTextView ()
        {
            TabStop = TabBehavior.TabGroup;
        }

        internal FileInfo? FileInfo { get; set; }

        internal void Read ()
        {
            Assembly? assembly = null;

            if (FileInfo!.FullName.Contains ("[Terminal.Gui]"))
            {
                // Library resources
                assembly = typeof (ConfigurationManager).Assembly;
            }
            else if (FileInfo.FullName.Contains ("[UICatalog]"))
            {
                assembly = Assembly.GetEntryAssembly ();
            }

            if (assembly != null)
            {
                string? name = assembly
                               .GetManifestResourceNames ()
                               .FirstOrDefault (x => x.EndsWith ("config.json"));
                if (!string.IsNullOrEmpty (name))
                {

                    using Stream? stream = assembly.GetManifestResourceStream (name);
                    using var reader = new StreamReader (stream!);
                    Text = reader.ReadToEnd ();
                    ReadOnly = true;
                    Enabled = true;
                }

                return;
            }

            if (FileInfo!.FullName.Contains ("RuntimeConfig"))
            {
                Text = ConfigurationManager.RuntimeConfig!;

            } else if (!FileInfo.Exists)
            {
                // Create empty config file
                Text = ConfigurationManager.GetEmptyJson ();
            }
            else
            {
                Text = File.ReadAllText (FileInfo.FullName);
            }
        }

        internal void Save ()
        {
            if (FileInfo!.FullName.Contains ("RuntimeConfig"))
            {
                ConfigurationManager.RuntimeConfig = Text;
                IsDirty = false;
                return;
            }

            if (!Directory.Exists (FileInfo.DirectoryName))
            {
                // Create dir
                Directory.CreateDirectory (FileInfo.DirectoryName!);
            }

            using StreamWriter writer = File.CreateText (FileInfo.FullName);
            writer.Write (Text);
            writer.Close ();
            IsDirty = false;
        }
    }
}
