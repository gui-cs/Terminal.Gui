﻿using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("FileDialog", "Demonstrates how to the FileDialog class")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Files and IO")]
public class FileDialogExamples : Scenario
{
    private CheckBox _cbAllowMultipleSelection;
    private CheckBox _cbAlwaysTableShowHeaders;
    private CheckBox _cbCaseSensitive;
    private CheckBox _cbDrivesOnlyInTree;
    private CheckBox _cbFlipButtonOrder;
    private CheckBox _cbMustExist;
    private CheckBox _cbShowTreeBranchLines;
    private CheckBox _cbUseColors;
    private RadioGroup _rgAllowedTypes;
    private RadioGroup _rgCaption;
    private RadioGroup _rgIcons;
    private RadioGroup _rgOpenMode;
    private TextField _tbCancelButton;
    private TextField _tbOkButton;

    public override void Main ()
    {
        Application.Init ();
        var y = 0;
        var x = 1;
        var win = new Window { Title = GetQuitKeyAndName () };

        _cbMustExist = new CheckBox { State = CheckState.Checked, Y = y++, X = x, Text = "Must Exist" };
        win.Add (_cbMustExist);

        _cbUseColors = new CheckBox { State = FileDialogStyle.DefaultUseColors ? CheckState.Checked : CheckState.UnChecked, Y = y++, X = x, Text = "Use Colors" };
        win.Add (_cbUseColors);

        _cbCaseSensitive = new CheckBox { State = CheckState.UnChecked, Y = y++, X = x, Text = "Case Sensitive Search" };
        win.Add (_cbCaseSensitive);

        _cbAllowMultipleSelection = new CheckBox { State = CheckState.UnChecked, Y = y++, X = x, Text = "Multiple" };
        win.Add (_cbAllowMultipleSelection);

        _cbShowTreeBranchLines = new CheckBox { State = CheckState.Checked, Y = y++, X = x, Text = "Tree Branch Lines" };
        win.Add (_cbShowTreeBranchLines);

        _cbAlwaysTableShowHeaders = new CheckBox { State = CheckState.Checked, Y = y++, X = x, Text = "Always Show Headers" };
        win.Add (_cbAlwaysTableShowHeaders);

        _cbDrivesOnlyInTree = new CheckBox { State = CheckState.UnChecked, Y = y++, X = x, Text = "Only Show Drives" };
        win.Add (_cbDrivesOnlyInTree);

        y = 0;
        x = 24;

        win.Add (
                 new LineView (Orientation.Vertical) { X = x++, Y = 1, Height = 4 }
                );
        win.Add (new Label { X = x++, Y = y++, Text = "Caption" });

        _rgCaption = new RadioGroup { X = x, Y = y };
        _rgCaption.RadioLabels = new [] { "Ok", "Open", "Save" };
        win.Add (_rgCaption);

        y = 0;
        x = 34;

        win.Add (
                 new LineView (Orientation.Vertical) { X = x++, Y = 1, Height = 4 }
                );
        win.Add (new Label { X = x++, Y = y++, Text = "OpenMode" });

        _rgOpenMode = new RadioGroup { X = x, Y = y };
        _rgOpenMode.RadioLabels = new [] { "File", "Directory", "Mixed" };
        win.Add (_rgOpenMode);

        y = 0;
        x = 48;

        win.Add (
                 new LineView (Orientation.Vertical) { X = x++, Y = 1, Height = 4 }
                );
        win.Add (new Label { X = x++, Y = y++, Text = "Icons" });

        _rgIcons = new RadioGroup { X = x, Y = y };
        _rgIcons.RadioLabels = new [] { "None", "Unicode", "Nerd*" };
        win.Add (_rgIcons);

        win.Add (new Label { Y = Pos.AnchorEnd (2), Text = "* Requires installing Nerd fonts" });
        win.Add (new Label { Y = Pos.AnchorEnd (1), Text = "  (see: https://github.com/devblackops/Terminal-Icons)" });

        y = 5;
        x = 24;

        win.Add (
                 new LineView (Orientation.Vertical) { X = x++, Y = y + 1, Height = 4 }
                );
        win.Add (new Label { X = x++, Y = y++, Text = "Allowed" });

        _rgAllowedTypes = new RadioGroup { X = x, Y = y };
        _rgAllowedTypes.RadioLabels = new [] { "Any", "Csv (Recommended)", "Csv (Strict)" };
        win.Add (_rgAllowedTypes);

        y = 5;
        x = 45;

        win.Add (
                 new LineView (Orientation.Vertical) { X = x++, Y = y + 1, Height = 4 }
                );
        win.Add (new Label { X = x++, Y = y++, Text = "Buttons" });

        win.Add (new Label { X = x, Y = y++, Text = "Ok Text:" });
        _tbOkButton = new TextField { X = x, Y = y++, Width = 12 };
        win.Add (_tbOkButton);
        win.Add (new Label { X = x, Y = y++, Text = "Cancel Text:" });
        _tbCancelButton = new TextField { X = x, Y = y++, Width = 12 };
        win.Add (_tbCancelButton);
        _cbFlipButtonOrder = new CheckBox { X = x, Y = y++, Text = "Flip Order" };
        win.Add (_cbFlipButtonOrder);

        var btn = new Button { X = 1, Y = 9, Text = "Run Dialog" };

        SetupHandler (btn);
        win.Add (btn);

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }

    private void ConfirmOverwrite (object sender, FilesSelectedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace (e.Dialog.Path))
        {
            if (File.Exists (e.Dialog.Path))
            {
                int result = MessageBox.Query ("Overwrite?", "File already exists", "Yes", "No");
                e.Cancel = result == 1;
            }
        }
    }

    private void CreateDialog ()
    {
        var fd = new FileDialog
        {
            OpenMode = Enum.Parse<OpenMode> (
                                             _rgOpenMode.RadioLabels [_rgOpenMode.SelectedItem]
                                            ),
            MustExist = _cbMustExist.State == CheckState.Checked,
            AllowsMultipleSelection = _cbAllowMultipleSelection.State == CheckState.Checked
        };

        fd.Style.OkButtonText = _rgCaption.RadioLabels [_rgCaption.SelectedItem];

        // If Save style dialog then give them an overwrite prompt
        if (_rgCaption.SelectedItem == 2)
        {
            fd.FilesSelected += ConfirmOverwrite;
        }

        fd.Style.IconProvider.UseUnicodeCharacters = _rgIcons.SelectedItem == 1;
        fd.Style.IconProvider.UseNerdIcons = _rgIcons.SelectedItem == 2;

        if (_cbCaseSensitive.State == CheckState.Checked)
        {
            fd.SearchMatcher = new CaseSensitiveSearchMatcher ();
        }

        fd.Style.UseColors = _cbUseColors.State == CheckState.Checked;

        fd.Style.TreeStyle.ShowBranchLines = _cbShowTreeBranchLines.State == CheckState.Checked;
        fd.Style.TableStyle.AlwaysShowHeaders = _cbAlwaysTableShowHeaders.State == CheckState.Checked;

        IDirectoryInfoFactory dirInfoFactory = new FileSystem ().DirectoryInfo;

        if (_cbDrivesOnlyInTree.State == CheckState.Checked)
        {
            fd.Style.TreeRootGetter = () => { return Environment.GetLogicalDrives ().ToDictionary (dirInfoFactory.New, k => k); };
        }

        if (_rgAllowedTypes.SelectedItem > 0)
        {
            fd.AllowedTypes.Add (new AllowedType ("Data File", ".csv", ".tsv"));

            if (_rgAllowedTypes.SelectedItem == 1)
            {
                fd.AllowedTypes.Insert (1, new AllowedTypeAny ());
            }
        }

        if (!string.IsNullOrWhiteSpace (_tbOkButton.Text))
        {
            fd.Style.OkButtonText = _tbOkButton.Text;
        }

        if (!string.IsNullOrWhiteSpace (_tbCancelButton.Text))
        {
            fd.Style.CancelButtonText = _tbCancelButton.Text;
        }

        if (_cbFlipButtonOrder.State == CheckState.Checked)
        {
            fd.Style.FlipOkCancelButtonLayoutOrder = true;
        }

        Application.Run (fd);

        var canceled = fd.Canceled;
        var multiSelected = fd.MultiSelected;
        var path = fd.Path;

        // This needs to be disposed before opening other toplevel
        fd.Dispose ();

        if (canceled)
        {
            MessageBox.Query (
                              "Canceled",
                              "You canceled navigation and did not pick anything",
                              "Ok"
                             );
        }
        else if (_cbAllowMultipleSelection.State == CheckState.Checked)
        {
            MessageBox.Query (
                              "Chosen!",
                              "You chose:" + Environment.NewLine + string.Join (Environment.NewLine, multiSelected.Select (m => m)),
                              "Ok"
                             );
        }
        else
        {
            MessageBox.Query (
                              "Chosen!",
                              "You chose:" + Environment.NewLine + path,
                              "Ok"
                             );
        }
    }

    private void SetupHandler (Button btn)
    {
        btn.Accept += (s, e) =>
                       {
                           try
                           {
                               CreateDialog ();
                           }
                           catch (Exception ex)
                           {
                               MessageBox.ErrorQuery ("Error", ex.ToString (), "Ok");
                           }
                       };
    }

    private class CaseSensitiveSearchMatcher : ISearchMatcher
    {
        private string _terms;
        public void Initialize (string terms) { _terms = terms; }
        public bool IsMatch (IFileSystemInfo f) { return f.Name.Contains (_terms, StringComparison.CurrentCulture); }
    }
}
