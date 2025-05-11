using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Localization", "Test for localization resources.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Tests")]
public class Localization : Scenario
{
    private CheckBox _allowAnyCheckBox;
    private string [] _cultureInfoNameSource;
    private CultureInfo [] _cultureInfoSource;
    private OpenMode _currentOpenMode = OpenMode.File;
    private ComboBox _languageComboBox;
    public CultureInfo CurrentCulture { get; private set; } = Thread.CurrentThread.CurrentUICulture;

    public void Quit ()
    {
        SetCulture (CultureInfo.InvariantCulture);
        Application.RequestStop ();
    }

    public void SetCulture (CultureInfo culture)
    {
        if (_cultureInfoSource [_languageComboBox.SelectedItem] != culture)
        {
            _languageComboBox.SelectedItem = Array.IndexOf (_cultureInfoSource, culture);
        }

        if (CurrentCulture == culture)
        {
            return;
        }

        CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Application.LayoutAndDraw ();
    }

    public override void Main ()
    {
        Application.Init ();
        var top = new Toplevel ();
        var win = new Window { Title = GetQuitKeyAndName () };
        _cultureInfoSource = Application.SupportedCultures.Append (CultureInfo.InvariantCulture).ToArray ();

        _cultureInfoNameSource = Application.SupportedCultures.Select (c => $"{c.NativeName} ({c.Name})")
                                            .Append ("Invariant")
                                            .ToArray ();

        MenuItem [] languageMenus = Application.SupportedCultures
                                               .Select (
                                                        c => new MenuItem (
                                                                           $"{c.NativeName} ({c.Name})",
                                                                           "",
                                                                           () => SetCulture (c)
                                                                          )
                                                       )
                                               .Concat (
                                                        new MenuItem []
                                                        {
                                                            null,
                                                            new (
                                                                 "Invariant",
                                                                 "",
                                                                 () =>
                                                                     SetCulture (
                                                                                 CultureInfo
                                                                                     .InvariantCulture
                                                                                )
                                                                )
                                                        }
                                                       )
                                               .ToArray ();

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new MenuBarItem (
                                          "_Language",
                                          languageMenus
                                         ),
                         null,
                         new ("_Quit", "", Quit)
                     }
                    )
            ]
        };
        top.Add (menu);

        var selectLanguageLabel = new Label
        {
            X = 2,
            Y = 1,

            Width = Dim.Fill (2),
            Text = "Please select a language."
        };
        win.Add (selectLanguageLabel);

        _languageComboBox = new()
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
        win.Add (_languageComboBox);

        var textAndFileDialogLabel = new Label
        {
            X = 2,
            Y = Pos.Top (_languageComboBox) + 3,

            Width = Dim.Fill (2),
            Height = 1,
            Text =
                "Right click on the text field to open a context menu, click the button to open a file dialog.\r\nOpen mode will loop through 'File', 'Directory' and 'Mixed' as 'Open' or 'Save' button clicked."
        };
        win.Add (textAndFileDialogLabel);

        var textField = new TextView
        {
            X = 2, Y = Pos.Bottom (textAndFileDialogLabel) + 1, Width = Dim.Fill (32), Height = 1
        };
        win.Add (textField);

        _allowAnyCheckBox = new()
        {
            X = Pos.Right (textField) + 1,
            Y = Pos.Bottom (textAndFileDialogLabel) + 1,
            CheckedState = CheckState.UnChecked,
            Text = "Allow any"
        };
        win.Add (_allowAnyCheckBox);

        var openDialogButton = new Button
        {
            X = Pos.Right (_allowAnyCheckBox) + 1, Y = Pos.Bottom (textAndFileDialogLabel) + 1, Text = "Open"
        };
        openDialogButton.Accepting += (sender, e) => ShowFileDialog (false);
        win.Add (openDialogButton);

        var saveDialogButton = new Button
        {
            X = Pos.Right (openDialogButton) + 1, Y = Pos.Bottom (textAndFileDialogLabel) + 1, Text = "Save"
        };
        saveDialogButton.Accepting += (sender, e) => ShowFileDialog (true);
        win.Add (saveDialogButton);

        var wizardLabel = new Label
        {
            X = 2,
            Y = Pos.Bottom (textField) + 1,

            Width = Dim.Fill (2),
            Text = "Click the button to open a wizard."
        };
        win.Add (wizardLabel);

        var wizardButton = new Button { X = 2, Y = Pos.Bottom (wizardLabel) + 1, Text = "Open _wizard" };
        wizardButton.Accepting += (sender, e) => ShowWizard ();
        win.Add (wizardButton);

        win.Unloaded += (sender, e) => Quit ();

        win.Y = Pos.Bottom (menu);
        top.Add (win);

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }

    public void ShowFileDialog (bool isSaveFile)
    {
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

        Application.Run (dialog);
        dialog.Dispose ();
    }

    public void ShowWizard ()
    {
        var wizard = new Wizard { Height = 8, Width = 36, Title = "The wizard" };
        wizard.AddStep (new() { HelpText = "Wizard first step" });
        wizard.AddStep (new() { HelpText = "Wizard step 2", NextButtonText = ">>> (_N)" });
        wizard.AddStep (new() { HelpText = "Wizard last step" });
        Application.Run (wizard);
        wizard.Dispose ();
    }

    private void LanguageComboBox_SelectChanged (object sender, ListViewItemEventArgs e)
    {
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
