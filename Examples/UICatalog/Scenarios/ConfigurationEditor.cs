#nullable enable
using System.Reflection;
using Terminal.Gui.Document;
using Terminal.Gui.Editor;
using Editor = Terminal.Gui.Editor.Editor;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Configuration Editor", "Edits of Terminal.Gui Config Files")]
[ScenarioCategory ("Tabs")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Files and IO")]
[ScenarioCategory ("Editor")]
[ScenarioCategory ("Configuration")]
public class ConfigurationEditor : Scenario
{
    private Tabs? _tabs;
    private Shortcut? _lenShortcut;
    private IApplication? _app;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window win = new ();
        win.BorderStyle = LineStyle.None;

        _lenShortcut = new Shortcut { Title = "" };

        Shortcut quitShortcut = new () { Key = Application.GetDefaultKey (Command.Quit), Title = "Quit", Action = Quit };

        Shortcut reloadShortcut = new () { Key = Key.F5.WithShift, Title = "Reload" };

        reloadShortcut.Accepting += (_, e) =>
                                    {
                                        Reload ();
                                        e.Handled = true;
                                    };

        Shortcut saveShortcut = new () { Key = Key.F4, Title = "Save", Action = Save };

        StatusBar statusBar = new ([quitShortcut, reloadShortcut, saveShortcut, _lenShortcut]);

        _tabs = new Tabs { Width = Dim.Fill (), Height = Dim.Fill (statusBar) };

        win.Add (_tabs, statusBar);

        ConfigurationManager.Applied += ConfigurationManagerOnApplied;
        Open ();

        _tabs.Disposing += (_, _) =>
                         {
                             _tabs?.ValueChanged -= OnTabsOnValueChanged;
                         };
        app.Run (win);

        return;

        void ConfigurationManagerOnApplied (object? sender, ConfigurationManagerEventArgs e) => _app?.TopRunnableView?.SetNeedsDraw ();
    }

    public void Save ()
    {
        if (_app?.Navigation?.GetFocused () is ConfigEditorView editor)
        {
            editor.Save ();
        }
    }

    private void Open ()
    {
        foreach (KeyValuePair<ConfigLocations, string> config in ConfigurationManager.SourcesManager!.Sources)
        {
            var homeDir = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}";
            FileInfo fileInfo = new (config.Value.Replace ("~", homeDir));

            ConfigEditorView editor = new ()
            {
                Title = config.Value.StartsWith ("resource://", StringComparison.Ordinal) ? fileInfo.Name : config.Value,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                FileInfo = fileInfo
            };

            if (config.Value == "HardCoded")
            {
                editor.Title = "HardCoded";
            }

            View tab = new () { Title = config.Key.ToString () };
            tab.Add (editor);

            _tabs?.Add (tab);

            editor.Read ();

            editor.Disposing += (_, _) =>
                                {
                                    editor.Document!.Changed -= OnEditorDocumentChanged;
                                };
            editor.Document!.Changed += OnEditorDocumentChanged;

            _lenShortcut?.Title = $"{editor.Title}";
        }

        _tabs?.ValueChanged += OnTabsOnValueChanged;
    }

    private void OnTabsOnValueChanged (object? _, ValueChangedEventArgs<View?> args)
    {
        ConfigEditorView? editor = args.NewValue?.SubViews.OfType<ConfigEditorView> ().FirstOrDefault ();

        if (editor is { })
        {
            _lenShortcut!.Title = $"{editor.Title}";
        }
    }

    private void OnEditorDocumentChanged (object? o, DocumentChangeEventArgs _)
    {
        if (_app?.Navigation?.GetFocused () is not ConfigEditorView editor)
        {
            return;
        }

        _lenShortcut?.Title = _lenShortcut.Title.Replace ("*", "");

        if (!editor.Document!.UndoStack.IsOriginalFile)
        {
            _lenShortcut?.Title += "*";
        }
    }

    private void Quit ()
    {
        foreach (ConfigEditorView editor in _tabs?.TabCollection.SelectMany (t => t.SubViews.OfType<ConfigEditorView> ()) ?? [])
        {
            if (editor.Document!.UndoStack.IsOriginalFile)
            {
                continue;
            }

            int? result = MessageBox.Query (editor.App!, "Save Changes", $"Save changes to {editor.FileInfo!.Name}", Strings.btnCancel, Strings.btnNo, Strings.btnYes);

            switch (result)
            {
                case 2:
                    editor.Save ();

                    break;

                case 1:
                    // user decided not save changes
                    break;

                case 0:
                    // Cancel
                    return;
            }
        }

        _tabs?.App?.RequestStop ();
    }

    private void Reload ()
    {
        if (_app?.Navigation?.GetFocused () is ConfigEditorView editor)
        {
            editor.Read ();
        }
    }

    private class ConfigEditorView : Editor
    {
        internal ConfigEditorView ()
        {
            TabStop = TabBehavior.TabGroup;
            ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar | ViewportSettingsFlags.HasHorizontalScrollBar;
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
                string? name = assembly.GetManifestResourceNames ().FirstOrDefault (x => x.EndsWith ("config.json", StringComparison.Ordinal));

                if (string.IsNullOrEmpty (name))
                {
                    return;
                }

                using Stream? stream = assembly.GetManifestResourceStream (name);
                using var reader = new StreamReader (stream!);
                Text = reader.ReadToEnd ();
                ReadOnly = true;
                Document!.UndoStack.ClearAll ();
                Document!.UndoStack.MarkAsOriginalFile ();

                return;
            }

            if (FileInfo!.FullName.Contains ("HardCoded"))
            {
                Text = ConfigurationManager.GetHardCodedConfig ();
                ReadOnly = true;
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

            Document!.UndoStack.ClearAll ();
            Document!.UndoStack.MarkAsOriginalFile ();
        }

        internal void Save ()
        {
            if (FileInfo!.FullName.Contains ("RuntimeConfig"))
            {
                ConfigurationManager.RuntimeConfig = Text;
                Document!.UndoStack.ClearAll ();
                Document!.UndoStack.MarkAsOriginalFile ();

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
            Document!.UndoStack.ClearAll ();
            Document!.UndoStack.MarkAsOriginalFile ();
        }
    }
}
