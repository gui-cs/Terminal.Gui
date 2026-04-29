#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Editor", "A Text Editor using the TextView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Arrangement")]
[ScenarioCategory ("Files and IO")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Menus")]
public partial class Editor : Scenario
{
    private IApplication? _app;
    private Window? _appWindow;
    private List<CultureInfo>? _cultureInfos;
    private string? _fileName = "demo.txt";
    private bool _forceMinimumPosToZero = true;
    private string? _lastDirectory;
    private bool _matchCase;
    private bool _matchWholeWord;
    private CheckBox? _miForceMinimumPosToZeroCheckBox;
    private byte []? _originalText;
    private bool _saved = true;
    private string _textToFind = string.Empty;
    private string _textToReplace = string.Empty;
    private TextView? _textView;
    private FindReplaceWindow? _findReplaceWindow;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        // Find the repository root by walking up from the current directory looking for README.md
        string repoRoot = FindRepoRoot () ?? Environment.CurrentDirectory;
        string readmePath = Path.Combine (repoRoot, "README.md");

        if (File.Exists (readmePath))
        {
            _fileName = readmePath;
        }
        else
        {
            CreateDemoFile (_fileName!);
        }

        _lastDirectory = Path.GetDirectoryName (Path.GetFullPath (_fileName!));

        _appWindow = new Window { Title = Path.GetFileName (_fileName) ?? "Untitled", BorderStyle = LineStyle.None };

        _cultureInfos = Application.SupportedCultures?.ToList ();

        _textView = new TextView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            ScrollBars = true
        };

        LoadFile ();

        _appWindow.Add (_textView);

        // MenuBar
        MenuBar menu = new ();

        menu.Add (new MenuBarItem (Strings.menuFile,
                                   [
                                       new MenuItem { Title = Strings.cmdNew, Action = () => New () },
                                       new MenuItem { Title = Strings.cmdOpen, Action = Open },
                                       new MenuItem { Title = Strings.cmdSave, Action = () => Save () },
                                       new MenuItem { Title = Strings.cmdSaveAs, Action = () => SaveAs () },
                                       new MenuItem { Title = Strings.cmdClose, Action = CloseFile },
                                       new MenuItem { Title = Strings.cmdQuit, Action = Quit }
                                   ]));

        menu.Add (new MenuBarItem ("_Edit",
                                   [
                                       new MenuItem { Title = Strings.cmdCopy, Key = Key.C.WithCtrl, Action = Copy },
                                       new MenuItem { Title = Strings.cmdCut, Key = Key.W.WithCtrl, Action = Cut },
                                       new MenuItem { Title = Strings.cmdPaste, Key = Key.Y.WithCtrl, Action = Paste },
                                       new MenuItem { Title = Strings.cmdFind, Key = Key.S.WithCtrl, Action = Find },
                                       new MenuItem { Title = "Find _Next", Key = Key.S.WithCtrl.WithShift, Action = FindNext },
                                       new MenuItem { Title = "Find P_revious", Key = Key.S.WithCtrl.WithShift.WithAlt, Action = FindPrevious },
                                       new MenuItem { Title = "_Replace", Key = Key.R.WithCtrl, Action = Replace },
                                       new MenuItem { Title = "Replace Ne_xt", Key = Key.R.WithCtrl.WithShift, Action = ReplaceNext },
                                       new MenuItem { Title = "Replace Pre_vious", Key = Key.R.WithCtrl.WithShift.WithAlt, Action = ReplacePrevious },
                                       new MenuItem { Title = "Replace _All", Key = Key.A.WithCtrl.WithShift.WithAlt, Action = ReplaceAll },
                                       new MenuItem { Title = Strings.cmdSelectAll, Key = Key.T.WithCtrl, Action = SelectAll }
                                   ]));

        menu.Add (new MenuBarItem ("_ScrollBars", CreateScrollBarsMenu ()));

        menu.Add (new MenuBarItem ("Forma_t",
                                   [
                                       CreateWrapChecked (),
                                       CreateAutocomplete (),
                                       CreateAllowsTabChecked (),
                                       CreateReadOnlyChecked (),
                                       CreateUseSameRuneTypeForWords (),
                                       CreateSelectWordOnlyOnDoubleClick (),
                                       new MenuItem { Title = "Colors", Key = Key.L.WithCtrl, Action = () => _textView?.PromptForColors () }
                                   ]));

        menu.Add (new MenuBarItem ("_View", [CreateCanFocusChecked (), CreateEnabledChecked (), CreateVisibleChecked ()]));

        _miForceMinimumPosToZeroCheckBox = new CheckBox
        {
            Title = "ForceMinimumPosTo_Zero", Value = _forceMinimumPosToZero ? CheckState.Checked : CheckState.UnChecked
        };

        _miForceMinimumPosToZeroCheckBox.ValueChanged += (_, e) =>
                                                         {
                                                             _forceMinimumPosToZero = e.NewValue == CheckState.Checked;

                                                             // Note: PopoverMenu.ForceMinimumPosToZero property doesn't exist in v2
                                                             // if (_textView?.ContextMenu is not null)
                                                             // {
                                                             //     _textView.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
                                                             // }
                                                         };

        menu.Add (new MenuBarItem ("Conte_xtMenu",
                                   [new MenuItem { CommandView = _miForceMinimumPosToZeroCheckBox }, new MenuBarItem ("_Languages", GetSupportedCultures ())]));

        _appWindow.Add (menu);

        Shortcut siCursorPosition = new (Key.Empty, "", null);

        StatusBar statusBar = new ([
                                       new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit),
                                       new Shortcut (Key.F2, "Open", Open),
                                       new Shortcut (Key.F3, "Save", () => Save ()),
                                       new Shortcut (Key.F4, "Save As", () => SaveAs ()),
                                       new Shortcut (Key.Empty, $"OS Clipboard IsSupported : {app.Clipboard!.IsSupported}", null),
                                       siCursorPosition
                                   ]) { AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast };

        _textView.UnwrappedCursorPositionChanged += (_, e) =>
                                                    {
                                                        List<Cell> line = _textView.GetLine (e.Y).Take (e.X).ToList ();
                                                        int colsWidth = _textView.GetColumnsWidth (line);
                                                        siCursorPosition.Title = $"Ln {e.Y + 1}, Cc {e.X + 1}, Col {colsWidth + 1}";
                                                    };

        _appWindow.Add (statusBar);

        _appWindow.IsRunningChanged += (_, e) =>
                                       {
                                           if (!e.Value)
                                           {
                                               Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                                           }
                                       };

        CreateFindReplace ();

        // Run - Start the application.
        app.Run (_appWindow);
        _appWindow.Dispose ();
    }

    private bool CanCloseFile ()
    {
        if (_textView is null || _originalText is null || _appWindow is null)
        {
            return true;
        }

        if (_textView.Text == Encoding.Unicode.GetString (_originalText))
        {
            return true;
        }

        Debug.Assert (_textView.IsDirty);

        int? r = MessageBox.ErrorQuery (_appWindow!.App!,
                                        "Save File",
                                        $"Do you want save changes in {_appWindow.Title}?",
                                        Strings.btnCancel,
                                        Strings.btnNo,
                                        Strings.btnYes);

        return r switch
               {
                   2 => Save (),
                   1 => true,
                   _ => false
               };
    }

    private void CloseFile ()
    {
        if (!CanCloseFile () || _textView is null)
        {
            return;
        }

        try
        {
            _textView.CloseFile ();
            New (false);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_appWindow!.App!, "Error", ex.Message, Strings.btnOk);
        }
    }

    private void Copy () => _textView?.Copy ();

    private void Cut () => _textView?.Cut ();

    private void SelectAll () => _textView?.SelectAll ();

    private void CreateDemoFile (string fileName)
    {
        StringBuilder sb = new ();

        sb.Append ("Hello world.\n");
        sb.Append ("This is a test of the Emergency Broadcast System.\n");

        for (var i = 0; i < 30; i++)
        {
            sb.Append ($"{i} - This is a test with a very long line and many lines to test the ScrollViewBar against the TextView. - {i}\n");
        }

        StreamWriter sw = File.CreateText (fileName);
        sw.Write (sb.ToString ());
        sw.Close ();
    }

    /// <summary>
    ///     Walks up the directory tree from the current directory looking for the repository root
    ///     (identified by Terminal.sln).
    /// </summary>
    private static string? FindRepoRoot ()
    {
        DirectoryInfo? dir = new (Environment.CurrentDirectory);

        while (dir is { })
        {
            if (File.Exists (Path.Combine (dir.FullName, "Terminal.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private void LoadFile ()
    {
        if (_fileName is null || _textView is null || _appWindow is null)
        {
            return;
        }

        _textView.Load (_fileName);
        _originalText = Encoding.Unicode.GetBytes (_textView.Text);
        _appWindow.Title = _fileName;
        _saved = true;
    }

    private void New (bool checkChanges = true)
    {
        if (_appWindow is null || _textView is null)
        {
            return;
        }

        if (checkChanges && !CanCloseFile ())
        {
            return;
        }

        _appWindow.Title = "Untitled.txt";
        _fileName = null;
        _originalText = new MemoryStream ().ToArray ();
        _textView.Text = Encoding.Unicode.GetString (_originalText);
    }

    private void Open ()
    {
        if (!CanCloseFile ())
        {
            return;
        }

        List<IAllowedType> aTypes = [new AllowedType ("Text", ".txt;.bin;.xml;.json", ".txt", ".bin", ".xml", ".json", ".md"), new AllowedTypeAny ()];

        OpenDialog d = new () { Title = "Open", AllowedTypes = aTypes, AllowsMultipleSelection = false };

        if (_lastDirectory is { })
        {
            d.Path = _lastDirectory;
        }

        _app?.Run (d);

        if (!d.Canceled && d.FilePaths.Count > 0)
        {
            _fileName = d.FilePaths [0];
            _lastDirectory = Path.GetDirectoryName (Path.GetFullPath (_fileName));
            LoadFile ();
        }

        d.Dispose ();
    }

    private void Paste () => _textView?.Paste ();

    private void Quit ()
    {
        if (!CanCloseFile ())
        {
            return;
        }

        _appWindow?.RequestStop ();
    }

    private bool Save ()
    {
        if (_fileName is { } && _appWindow is { })
        {
            return SaveFile (_appWindow.Title, _fileName);
        }

        return SaveAs ();
    }

    private bool SaveAs ()
    {
        if (_appWindow is null)
        {
            return false;
        }

        List<IAllowedType> aTypes = [new AllowedType ("Text Files", ".txt", ".bin", ".xml"), new AllowedTypeAny ()];

        SaveDialog sd = new () { Title = "Save file", AllowedTypes = aTypes };

        if (_lastDirectory is { })
        {
            sd.Path = _lastDirectory;
        }
        else
        {
            sd.Path = _appWindow.Title;
        }

        _app?.Run (sd);
        bool canceled = sd.Canceled;
        string path = sd.Path;
        string fileName = sd.FileName ?? string.Empty;
        sd.Dispose ();

        if (canceled)
        {
            _saved = false;

            return _saved;
        }

        _lastDirectory = Path.GetDirectoryName (Path.GetFullPath (path));

        if (!File.Exists (path) || MessageBox.Query (_app!, "Save File", "File already exists. Overwrite any way?", Strings.btnNo, Strings.btnYes) == 1)
        {
            return SaveFile (fileName, path);
        }

        _saved = false;

        return _saved;
    }

    private bool SaveFile (string title, string file)
    {
        if (_appWindow is null || _textView is null)
        {
            return false;
        }

        try
        {
            _appWindow.Title = title;
            _fileName = file;
            File.WriteAllText (_fileName, _textView.Text);
            _originalText = Encoding.Unicode.GetBytes (_textView.Text);
            _saved = true;
            _textView.ClearHistoryChanges ();
            MessageBox.Query (_appWindow.App!, "Save File", "File was successfully saved.", Strings.btnOk);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_appWindow.App!, "Error", ex.Message, Strings.btnOk);

            return false;
        }

        return true;
    }
}
