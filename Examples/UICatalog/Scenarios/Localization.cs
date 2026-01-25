#nullable enable

using System.Globalization;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Localization", "Test for localization resources.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Tests")]
public class Localization : Scenario
{
    private CheckBox? _allowAnyCheckBox;
    private IApplication? _app;
    private string []? _cultureInfoNameSource;
    private CultureInfo []? _cultureInfoSource;
    private OpenMode _currentOpenMode = OpenMode.File;
    private ComboBox? _languageComboBox;
    private Window? _win;
    public CultureInfo CurrentCulture { get; private set; } = Thread.CurrentThread.CurrentUICulture;

    public void Quit ()
    {
        SetCulture (CultureInfo.InvariantCulture);
        _win?.RequestStop ();
    }

    public void SetCulture (CultureInfo culture)
    {
        if (_languageComboBox is null || _cultureInfoSource is null)
        {
            return;
        }

        if (!Equals (_cultureInfoSource [_languageComboBox.SelectedItem], culture))
        {
            _languageComboBox.SelectedItem = Array.IndexOf (_cultureInfoSource, culture);
        }

        if (Equals (CurrentCulture, culture))
        {
            return;
        }

        CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        _app?.LayoutAndDraw ();
    }

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        _app = Application.Create ();
        _app.Init ();

        _win = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        _cultureInfoSource = Application.SupportedCultures!.Append (CultureInfo.InvariantCulture).ToArray ();

        _cultureInfoNameSource = Application.SupportedCultures!.Select (c => $"{c.NativeName} ({c.Name})")
                                            .Append ("Invariant")
                                            .ToArray ();

        MenuItem [] languageMenus = Application.SupportedCultures!
                                               .Select (c => new MenuItem
                                                        {
                                                            Title = $"{c.NativeName} ({c.Name})",
                                                            Action = () => SetCulture (c)
                                                        }
                                                       )
                                               .Concat (
                                                        [
                                                            new ()
                                                            {
                                                                Title = "Invariant",
                                                                Action = () => SetCulture (CultureInfo.InvariantCulture)
                                                            }
                                                        ]
                                                       )
                                               .ToArray ();

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuBarItem (
                                                        "_Language",
                                                        languageMenus
                                                       ),
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        Label selectLanguageLabel = new ()
        {
            X = 2,
            Y = Pos.Bottom (menu) + 1,
            Width = Dim.Fill (2),
            Text = "Please select a language."
        };
        _win.Add (selectLanguageLabel);

        _languageComboBox = new ()
        {
            X = 2,
            Y = Pos.Bottom (selectLanguageLabel) + 1,
            Width = _cultureInfoNameSource.Select (cn => cn.Length + 3).Max (),
            Height = _cultureInfoNameSource.Length + 1,
            HideDropdownListOnClick = true,
            Source = new ListWrapper<string> (new (_cultureInfoNameSource)),
            SelectedItem = _cultureInfoNameSource.Length - 1
        };
        _languageComboBox.SetSource<string> (new (_cultureInfoNameSource));
        _languageComboBox.SelectedItemChanged += LanguageComboBox_SelectChanged;
        _win.Add (_languageComboBox);

        Label textAndFileDialogLabel = new ()
        {
            X = 2,
            Y = Pos.Top (_languageComboBox) + 3,
            Width = Dim.Fill (2),
            Height = 1,
            Text =
                "Right click on the text field to open a context menu, click the button to open a file dialog.\r\nOpen mode will loop through 'File', 'Directory' and 'Mixed' as 'Open' or 'Save' button clicked."
        };
        _win.Add (textAndFileDialogLabel);

        TextView textField = new ()
        {
            X = 2,
            Y = Pos.Bottom (textAndFileDialogLabel) + 1,
            Width = Dim.Fill (32),
            Height = 1
        };
        _win.Add (textField);

        _allowAnyCheckBox = new ()
        {
            X = Pos.Right (textField) + 1,
            Y = Pos.Bottom (textAndFileDialogLabel) + 1,
            CheckedState = CheckState.UnChecked,
            Text = "Allow any"
        };
        _win.Add (_allowAnyCheckBox);

        Button openDialogButton = new ()
        {
            X = Pos.Right (_allowAnyCheckBox) + 1,
            Y = Pos.Bottom (textAndFileDialogLabel) + 1,
            Text = "Open"
        };
        openDialogButton.Accepting += (_, _) => ShowFileDialog (false);
        _win.Add (openDialogButton);

        Button saveDialogButton = new ()
        {
            X = Pos.Right (openDialogButton) + 1,
            Y = Pos.Bottom (textAndFileDialogLabel) + 1,
            Text = "Save"
        };
        saveDialogButton.Accepting += (_, _) => ShowFileDialog (true);
        _win.Add (saveDialogButton);

        Label wizardLabel = new ()
        {
            X = 2,
            Y = Pos.Bottom (textField) + 1,
            Width = Dim.Fill (2),
            Text = "Click the button to open a wizard."
        };
        _win.Add (wizardLabel);

        Button wizardButton = new () { X = 2, Y = Pos.Bottom (wizardLabel) + 1, Text = "Open _wizard" };
        wizardButton.Accepting += (_, _) => ShowWizard ();
        _win.Add (wizardButton);

        _win.IsRunningChanged += (_, _) => Quit ();

        _win.Add (menu);

        _app.Run (_win);
        _win.Dispose ();
        _app.Dispose ();
    }

    public void ShowFileDialog (bool isSaveFile)
    {
        if (_allowAnyCheckBox is null)
        {
            return;
        }

        FileDialog dialog = isSaveFile ? new SaveDialog () : new OpenDialog { OpenMode = _currentOpenMode };

        dialog.AllowedTypes =
        [
            _allowAnyCheckBox.CheckedState == CheckState.Checked
                ? new AllowedTypeAny ()
                : new AllowedType ("Dynamic link library", ".dll"),
            new AllowedType ("Json", ".json"),
            new AllowedType ("Text", ".txt"),
            new AllowedType ("Yaml", ".yml", ".yaml")
        ];
        dialog.MustExist = !isSaveFile;
        dialog.AllowsMultipleSelection = !isSaveFile;
        _currentOpenMode++;

        if (_currentOpenMode > OpenMode.Mixed)
        {
            _currentOpenMode = OpenMode.File;
        }

        _app?.Run (dialog);
        dialog.Dispose ();
    }

    public void ShowWizard ()
    {
        Wizard wizard = new () { Height = 8, Width = 36, Title = "The wizard" };
        wizard.AddStep (new () { HelpText = "Wizard first step" });
        wizard.AddStep (new () { HelpText = "Wizard step 2", NextButtonText = ">>> (_N)" });
        wizard.AddStep (new () { HelpText = "Wizard last step" });
        _app?.Run (wizard);
        wizard.Dispose ();
    }

    private void LanguageComboBox_SelectChanged (object? sender, ListViewItemEventArgs e)
    {
        if (_cultureInfoNameSource is null || _cultureInfoSource is null)
        {
            return;
        }

        if (e.Value is string cultureName)
        {
            int index = Array.IndexOf (_cultureInfoNameSource, cultureName);

            if (index >= 0)
            {
                SetCulture (_cultureInfoSource [index]);
            }
        }
    }
}
