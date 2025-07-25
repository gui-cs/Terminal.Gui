#nullable enable
using System.Reflection;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Configuration Editor", "Edits of Terminal.Gui Config Files")]
[ScenarioCategory ("TabView")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Files and IO")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Configuration")]
public class ConfigurationEditor : Scenario
{
    private TabView? _tabView;
    private Shortcut? _lenShortcut;

    public override void Main ()
    {
        Application.Init ();

        Window? win = new ();

        _lenShortcut = new ()
        {
            Title = "",
        };

        Shortcut quitShortcut = new ()
        {
            Key = Application.QuitKey,
            Title = $"Quit",
            Action = Quit
        };

        Shortcut reloadShortcut = new  ()
        {
            Key = Key.F5.WithShift,
            Title = "Reload",
        };
        reloadShortcut.Accepting += (s, e) =>
                                    {
                                        Reload ();
                                        e.Handled = true;
                                    };

        Shortcut saveShortcut = new  ()
        {
            Key = Key.F4,
            Title = "Save",
            Action = Save
        };

        StatusBar statusBar = new ([quitShortcut, reloadShortcut, saveShortcut, _lenShortcut]);

        _tabView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (Dim.Func (_ => statusBar.Frame.Height))
        };

        win.Add (_tabView, statusBar);

        win.Loaded += (s, a) =>
                      {
                          Open ();
                      };

        ConfigurationManager.Applied += ConfigurationManagerOnApplied;

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();

        return;

        void ConfigurationManagerOnApplied (object? sender, ConfigurationManagerEventArgs e)
        {
            Application.Top?.SetNeedsDraw ();
        }
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
        foreach (KeyValuePair<ConfigLocations, string> config in ConfigurationManager.SourcesManager!.Sources)
        {
            var homeDir = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}";
            var fileInfo = new FileInfo (config.Value.Replace ("~", homeDir));

            var editor = new ConfigTextView
            {
                Title = config.Value.StartsWith ("resource://") ? fileInfo.Name : config.Value,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                FileInfo = fileInfo,
            };

            if (config.Value == "HardCoded")
            {
                editor.Title = "HardCoded";

            }

            Tab tab = new ()
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
        foreach (ConfigTextView editor in _tabView!.Tabs.Select (v =>
                                                                {
                                                                    if (v.View is ConfigTextView ctv)
                                                                    {
                                                                        return ctv;
                                                                    }

                                                                    return null;
                                                                }).Cast<ConfigTextView> ())
        {
            if (!editor.IsDirty)
            {
                continue;
            }

            int result = MessageBox.Query (
                                           "Save Changes",
                                           $"Save changes to {editor.FileInfo!.Name}",
                                           "_Yes",
                                           "_No",
                                           "_Cancel"
                                          );

            switch (result)
            {
                case 0:
                    editor.Save ();

                    break;

                case 1:
                    // user decided not save changes
                    break;
                case -1 or 2:
                    // user cancelled
                    return;
            }
        }

        Application.RequestStop ();
    }

    private static void Reload ()
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

        internal FileInfo? FileInfo { get; init; }

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

                if (string.IsNullOrEmpty (name))
                {
                    return;
                }

                using Stream? stream = assembly.GetManifestResourceStream (name);
                using var reader = new StreamReader (stream!);
                Text = reader.ReadToEnd ();
                ReadOnly = true;
                Enabled = true;

                return;
            }

            if (FileInfo!.FullName.Contains ("HardCoded"))
            {
                Text = ConfigurationManager.GetHardCodedConfig ()!;
                ReadOnly = true;
                Enabled = true;
            }
            else if (FileInfo!.FullName.Contains ("RuntimeConfig"))
            {
                Text = ConfigurationManager.RuntimeConfig!;
            }
            else if (!FileInfo.Exists)
            {
                // Create empty config file
                Text = ConfigurationManager.GetEmptyConfig ();
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
