using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "Localization", Description: "Test for localization resources.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Tests")]
public class Localization : Scenario {
	public CultureInfo CurrentCulture { get; private set; } = Thread.CurrentThread.CurrentUICulture;

	private CultureInfo [] _cultureInfoSource;
	private string [] _cultureInfoNameSource;
	private ComboBox _languageComboBox;
	private CheckBox _allowAnyCheckBox;
	private OpenMode _currentOpenMode = OpenMode.File;

	public override void Setup ()
	{
		base.Setup ();
		_cultureInfoSource = Application.SupportedCultures.Append (CultureInfo.InvariantCulture).ToArray ();
		_cultureInfoNameSource = Application.SupportedCultures.Select (c => $"{c.NativeName} ({c.Name})").Append ("Invariant").ToArray ();
		var languageMenus = Application.SupportedCultures
			.Select (c => new MenuItem ($"{c.NativeName} ({c.Name})", "", () => SetCulture (c)))
			.Concat (
				new MenuItem [] {
						null,
						new MenuItem ("Invariant", "", () => SetCulture (CultureInfo.InvariantCulture))
				}
			)
			.ToArray ();
		var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuBarItem("_Language", languageMenus),
					null,
					new MenuItem ("_Quit", "", Quit),
				}),
			});
		Application.Top.Add (menu);

		var selectLanguageLabel = new Label { X = 2, Y = 1, Width = Dim.Fill(2), AutoSize = true, Text = "Please select a language." };
		Win.Add (selectLanguageLabel);

		_languageComboBox = new ComboBox {
			X = 2,
			Y = Pos.Bottom (selectLanguageLabel) + 1,
			Width = _cultureInfoNameSource.Select (cn => cn.Length + 3).Max (),
			Height = _cultureInfoNameSource.Length + 1,
			HideDropdownListOnClick = true,
			AutoSize = true,
			Source = new ListWrapper (_cultureInfoNameSource),
			SelectedItem = _cultureInfoNameSource.Length - 1
		};
		_languageComboBox.SetSource (_cultureInfoNameSource);
		_languageComboBox.SelectedItemChanged += LanguageComboBox_SelectChanged;
		Win.Add (_languageComboBox);

		var textAndFileDialogLabel = new Label { X = 2, Y = Pos.Top(_languageComboBox) + 3, Width = Dim.Fill(2), AutoSize = true, 
			Text = "Right click on the text field to open a context menu, click the button to open a file dialog.\r\nOpen mode will loop through 'File', 'Directory' and 'Mixed' as 'Open' or 'Save' button clicked." };
		Win.Add (textAndFileDialogLabel);

		var textField = new TextView {
			X = 2,
			Y = Pos.Bottom (textAndFileDialogLabel) + 1,
			Width = Dim.Fill (32),
			Height = 1
		};
		Win.Add (textField);

		_allowAnyCheckBox = new CheckBox {
			X = Pos.Right (textField) + 1,
			Y = Pos.Bottom (textAndFileDialogLabel) + 1,
			Checked = false,
			Text = "Allow any"
		};
		Win.Add (_allowAnyCheckBox);

		var openDialogButton = new Button {
			X = Pos.Right (_allowAnyCheckBox) + 1,
			Y = Pos.Bottom (textAndFileDialogLabel) + 1,
			Text = "Open"
		};
		openDialogButton.Clicked += (sender, e) => ShowFileDialog (false);
		Win.Add (openDialogButton);

		var saveDialogButton = new Button {
			X = Pos.Right (openDialogButton) + 1,
			Y = Pos.Bottom (textAndFileDialogLabel) + 1,
			Text = "Save"
		};
		saveDialogButton.Clicked += (sender, e) => ShowFileDialog (true);
		Win.Add (saveDialogButton);

		var wizardLabel = new Label { X = 2, Y = Pos.Bottom(textField) + 1, Width = Dim.Fill(2), AutoSize = true, Text = "Click the button to open a wizard." };
		Win.Add (wizardLabel);

		var wizardButton = new Button {
			X = 2,
			Y = Pos.Bottom (wizardLabel) + 1,
			Text = "Open _wizard"
		};
		wizardButton.Clicked += (sender, e) => ShowWizard ();
		Win.Add (wizardButton);

		Win.Unloaded += (sender, e) => Quit ();
	}

	public void SetCulture (CultureInfo culture)
	{
		if (_cultureInfoSource [_languageComboBox.SelectedItem] != culture) {
			_languageComboBox.SelectedItem = Array.IndexOf (_cultureInfoSource, culture);
		}
		if (this.CurrentCulture == culture) return;
		this.CurrentCulture = culture;
		Thread.CurrentThread.CurrentUICulture = culture;
		Application.Refresh ();
	}
	private void LanguageComboBox_SelectChanged (object sender, ListViewItemEventArgs e)
	{
		if (e.Value is string cultureName) {
			var index = Array.IndexOf (_cultureInfoNameSource, cultureName);
			if (index >= 0) {
				SetCulture (_cultureInfoSource [index]);
			}
		}
	}
	public void ShowFileDialog (bool isSaveFile)
	{
		FileDialog dialog = isSaveFile ? new SaveDialog () : new OpenDialog ("", null, _currentOpenMode);
		dialog.AllowedTypes = new List<IAllowedType> () {
				(_allowAnyCheckBox.Checked ?? false) ? new AllowedTypeAny() : new AllowedType("Dynamic link library", ".dll"),
				new AllowedType("Json", ".json"),
				new AllowedType("Text", ".txt"),
				new AllowedType("Yaml", ".yml", ".yaml")
			};
		dialog.MustExist = !isSaveFile;
		dialog.AllowsMultipleSelection = !isSaveFile;
		_currentOpenMode++;
		if (_currentOpenMode > OpenMode.Mixed) {
			_currentOpenMode = OpenMode.File;
		}
		Application.Run (dialog);
	}
	public void ShowWizard ()
	{
		Wizard wizard = new Wizard {
			Height = 8,
			Width = 36,
			Title = "The wizard"
		};
		wizard.AddStep (new WizardStep () {
			HelpText = "Wizard first step",
		});
		wizard.AddStep (new WizardStep () {
			HelpText = "Wizard step 2",
			NextButtonText = ">>> (_N)"
		});
		wizard.AddStep (new WizardStep () {
			HelpText = "Wizard last step"
		});
		Application.Run (wizard);
	}
	public void Quit ()
	{
		SetCulture (CultureInfo.InvariantCulture);
		Application.RequestStop ();
	}
}
