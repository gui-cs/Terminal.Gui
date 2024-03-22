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

    private static Action _editorColorSchemeChanged;
    private StatusItem _lenStatusItem;
    private TileView _tileView;

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

    // Don't create a Window, just return the top-level view
    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
    }

    public void Save ()
    {
        if (_tileView.MostFocused is ConfigTextView editor)
        {
            editor.Save ();
        }
    }

    public override void Setup ()
    {
        _tileView = new TileView (0)
        {
            Width = Dim.Fill (), Height = Dim.Fill (1), Orientation = Orientation.Vertical, LineStyle = LineStyle.Single
        };

        Top.Add (_tileView);

        _lenStatusItem = new StatusItem (KeyCode.CharMask, "Len: ", null);

        var statusBar = new StatusBar (
                                       new []
                                       {
                                           new (
                                                Application.QuitKey,
                                                $"{Application.QuitKey} Quit",
                                                () => Quit ()
                                               ),
                                           new (KeyCode.F5, "~F5~ Reload", () => Reload ()),
                                           new (KeyCode.CtrlMask | KeyCode.S, "~^S~ Save", () => Save ()),
                                           _lenStatusItem
                                       }
                                      );

        Top.Add (statusBar);

        Top.Loaded += (s, a) => Open ();

        _editorColorSchemeChanged += () =>
                                     {
                                         foreach (Tile t in _tileView.Tiles)
                                         {
                                             t.ContentView.ColorScheme = EditorColorScheme;
                                             t.ContentView.SetNeedsDisplay ();
                                         }

                                         ;
                                     };

        _editorColorSchemeChanged.Invoke ();
    }

    private void Open ()
    {
        var subMenu = new MenuBarItem { Title = "_View" };

        foreach (string configFile in ConfigurationManager.Settings.Sources)
        {
            var homeDir = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}";
            var fileInfo = new FileInfo (configFile.Replace ("~", homeDir));

            Tile tile = _tileView.InsertTile (_tileView.Tiles.Count);
            tile.Title = configFile.StartsWith ("resource://") ? fileInfo.Name : configFile;

            var textView = new ConfigTextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                FileInfo = fileInfo,
                Tile = tile
            };

            tile.ContentView.Add (textView);

            textView.Read ();

            textView.Enter += (s, e) => { _lenStatusItem.Title = $"Len:{textView.Text.Length}"; };
        }

        Top.LayoutSubviews ();
    }

    private void Quit ()
    {
        foreach (Tile tile in _tileView.Tiles)
        {
            var editor = tile.ContentView.Subviews [0] as ConfigTextView;

            if (editor.IsDirty)
            {
                int result = MessageBox.Query (
                                               "Save Changes",
                                               $"Save changes to {editor.FileInfo.FullName}",
                                               "Yes",
                                               "No",
                                               "Cancel"
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
        if (_tileView.MostFocused is ConfigTextView editor)
        {
            editor.Read ();
        }
    }

    private class ConfigTextView : TextView
    {
        internal ConfigTextView ()
        {
            ContentsChanged += (s, obj) =>
                               {
                                   if (IsDirty)
                                   {
                                       if (!Tile.Title.EndsWith ('*'))
                                       {
                                           Tile.Title += '*';
                                       }
                                       else
                                       {
                                           Tile.Title = Tile.Title.TrimEnd ('*');
                                       }
                                   }
                               };
        }

        internal FileInfo FileInfo { get; set; }
        internal Tile Tile { get; set; }

        internal void Read ()
        {
            Assembly assembly = null;

            if (FileInfo.FullName.Contains ("[Terminal.Gui]"))
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
                string name = assembly
                              .GetManifestResourceNames ()
                              .FirstOrDefault (x => x.EndsWith ("config.json"));
                using Stream stream = assembly.GetManifestResourceStream (name);
                using var reader = new StreamReader (stream);
                Text = reader.ReadToEnd ();
                ReadOnly = true;
                Enabled = true;

                return;
            }

            if (!FileInfo.Exists)
            {
                // Create empty config file
                Text = ConfigurationManager.GetEmptyJson ();
            }
            else
            {
                Text = File.ReadAllText (FileInfo.FullName);
            }

            Tile.Title = Tile.Title.TrimEnd ('*');
        }

        internal void Save ()
        {
            if (!Directory.Exists (FileInfo.DirectoryName))
            {
                // Create dir
                Directory.CreateDirectory (FileInfo.DirectoryName!);
            }

            using StreamWriter writer = File.CreateText (FileInfo.FullName);
            writer.Write (Text);
            writer.Close ();
            Tile.Title = Tile.Title.TrimEnd ('*');
            IsDirty = false;
        }
    }
}
