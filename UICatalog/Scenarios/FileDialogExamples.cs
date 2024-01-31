#region

using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Terminal.Gui;

#endregion

namespace UICatalog.Scenarios {
    [ScenarioMetadata (Name: "FileDialog", Description: "Demonstrates how to the FileDialog class")]
    [ScenarioCategory ("Dialogs")]
    [ScenarioCategory ("Files and IO")]
    public class FileDialogExamples : Scenario {
        private CheckBox cbMustExist;
        private CheckBox cbUseColors;
        private CheckBox cbCaseSensitive;
        private CheckBox cbAllowMultipleSelection;
        private CheckBox cbShowTreeBranchLines;
        private CheckBox cbAlwaysTableShowHeaders;
        private CheckBox cbDrivesOnlyInTree;
        private RadioGroup rgCaption;
        private RadioGroup rgOpenMode;
        private RadioGroup rgIcons;
        private RadioGroup rgAllowedTypes;
        private TextField tbOkButton;
        private TextField tbCancelButton;
        private CheckBox cbFlipButtonOrder;

        public override void Setup () {
            var y = 0;
            var x = 1;

            cbMustExist = new CheckBox ("Must Exist") { Checked = true, Y = y++, X = x };
            Win.Add (cbMustExist);

            cbUseColors = new CheckBox ("Use Colors") { Checked = FileDialogStyle.DefaultUseColors, Y = y++, X = x };
            Win.Add (cbUseColors);

            cbCaseSensitive = new CheckBox ("Case Sensitive Search") { Checked = false, Y = y++, X = x };
            Win.Add (cbCaseSensitive);

            cbAllowMultipleSelection = new CheckBox ("Multiple") { Checked = false, Y = y++, X = x };
            Win.Add (cbAllowMultipleSelection);

            cbShowTreeBranchLines = new CheckBox ("Tree Branch Lines") { Checked = true, Y = y++, X = x };
            Win.Add (cbShowTreeBranchLines);

            cbAlwaysTableShowHeaders = new CheckBox ("Always Show Headers") { Checked = true, Y = y++, X = x };
            Win.Add (cbAlwaysTableShowHeaders);

            cbDrivesOnlyInTree = new CheckBox ("Only Show Drives") { Checked = false, Y = y++, X = x };
            Win.Add (cbDrivesOnlyInTree);

            y = 0;
            x = 24;

            Win.Add (
                     new LineView (Orientation.Vertical) {
                                                             X = x++,
                                                             Y = 1,
                                                             Height = 4
                                                         });
            Win.Add (new Label ("Caption") { X = x++, Y = y++ });

            rgCaption = new RadioGroup { X = x, Y = y };
            rgCaption.RadioLabels = new string[] { "Ok", "Open", "Save" };
            Win.Add (rgCaption);

            y = 0;
            x = 34;

            Win.Add (
                     new LineView (Orientation.Vertical) {
                                                             X = x++,
                                                             Y = 1,
                                                             Height = 4
                                                         });
            Win.Add (new Label ("OpenMode") { X = x++, Y = y++ });

            rgOpenMode = new RadioGroup { X = x, Y = y };
            rgOpenMode.RadioLabels = new string[] { "File", "Directory", "Mixed" };
            Win.Add (rgOpenMode);

            y = 0;
            x = 48;

            Win.Add (
                     new LineView (Orientation.Vertical) {
                                                             X = x++,
                                                             Y = 1,
                                                             Height = 4
                                                         });
            Win.Add (new Label ("Icons") { X = x++, Y = y++ });

            rgIcons = new RadioGroup { X = x, Y = y };
            rgIcons.RadioLabels = new string[] { "None", "Unicode", "Nerd*" };
            Win.Add (rgIcons);

            Win.Add (new Label ("* Requires installing Nerd fonts") { Y = Pos.AnchorEnd (2) });
            Win.Add (new Label ("  (see: https://github.com/devblackops/Terminal-Icons)") { Y = Pos.AnchorEnd (1) });

            y = 5;
            x = 24;

            Win.Add (
                     new LineView (Orientation.Vertical) {
                                                             X = x++,
                                                             Y = y + 1,
                                                             Height = 4
                                                         });
            Win.Add (new Label ("Allowed") { X = x++, Y = y++ });

            rgAllowedTypes = new RadioGroup { X = x, Y = y };
            rgAllowedTypes.RadioLabels = new string[] { "Any", "Csv (Recommended)", "Csv (Strict)" };
            Win.Add (rgAllowedTypes);

            y = 5;
            x = 45;

            Win.Add (
                     new LineView (Orientation.Vertical) {
                                                             X = x++,
                                                             Y = y + 1,
                                                             Height = 4
                                                         });
            Win.Add (new Label ("Buttons") { X = x++, Y = y++ });

            Win.Add (new Label ("Ok Text:") { X = x, Y = y++ });
            tbOkButton = new TextField () { X = x, Y = y++, Width = 12 };
            Win.Add (tbOkButton);
            Win.Add (new Label ("Cancel Text:") { X = x, Y = y++ });
            tbCancelButton = new TextField () { X = x, Y = y++, Width = 12 };
            Win.Add (tbCancelButton);
            cbFlipButtonOrder = new CheckBox ("Flip Order") { X = x, Y = y++ };
            Win.Add (cbFlipButtonOrder);

            var btn = new Button ($"Run Dialog") {
                                                     X = 1,
                                                     Y = 9
                                                 };

            SetupHandler (btn);
            Win.Add (btn);
        }

        private void SetupHandler (Button btn) {
            btn.Clicked += (s, e) => {
                try {
                    CreateDialog ();
                }
                catch (Exception ex) {
                    MessageBox.ErrorQuery ("Error", ex.ToString (), "Ok");
                }
            };
        }

        private void CreateDialog () {
            var fd = new FileDialog () {
                                           OpenMode = Enum.Parse<OpenMode> (
                                                                            rgOpenMode
                                                                                .RadioLabels[rgOpenMode.SelectedItem]
                                                                                .ToString ()),
                                           MustExist = cbMustExist.Checked ?? false,
                                           AllowsMultipleSelection = cbAllowMultipleSelection.Checked ?? false,
                                       };

            fd.Style.OkButtonText = rgCaption.RadioLabels[rgCaption.SelectedItem].ToString ();

            // If Save style dialog then give them an overwrite prompt
            if (rgCaption.SelectedItem == 2) {
                fd.FilesSelected += ConfirmOverwrite;
            }

            fd.Style.IconProvider.UseUnicodeCharacters = rgIcons.SelectedItem == 1;
            fd.Style.IconProvider.UseNerdIcons = rgIcons.SelectedItem == 2;

            if (cbCaseSensitive.Checked ?? false) {
                fd.SearchMatcher = new CaseSensitiveSearchMatcher ();
            }

            fd.Style.UseColors = cbUseColors.Checked ?? false;

            fd.Style.TreeStyle.ShowBranchLines = cbShowTreeBranchLines.Checked ?? false;
            fd.Style.TableStyle.AlwaysShowHeaders = cbAlwaysTableShowHeaders.Checked ?? false;

            var dirInfoFactory = new FileSystem ().DirectoryInfo;

            if (cbDrivesOnlyInTree.Checked ?? false) {
                fd.Style.TreeRootGetter = () => {
                    return Environment.GetLogicalDrives ().ToDictionary (dirInfoFactory.New, k => k);
                };
            }

            if (rgAllowedTypes.SelectedItem > 0) {
                fd.AllowedTypes.Add (new AllowedType ("Data File", ".csv", ".tsv"));

                if (rgAllowedTypes.SelectedItem == 1) {
                    fd.AllowedTypes.Insert (1, new AllowedTypeAny ());
                }
            }

            if (!string.IsNullOrWhiteSpace (tbOkButton.Text)) {
                fd.Style.OkButtonText = tbOkButton.Text;
            }

            if (!string.IsNullOrWhiteSpace (tbCancelButton.Text)) {
                fd.Style.CancelButtonText = tbCancelButton.Text;
            }

            if (cbFlipButtonOrder.Checked ?? false) {
                fd.Style.FlipOkCancelButtonLayoutOrder = true;
            }

            Application.Run (fd);

            if (fd.Canceled) {
                MessageBox.Query (
                                  "Canceled",
                                  "You canceled navigation and did not pick anything",
                                  "Ok");
            } else if (cbAllowMultipleSelection.Checked ?? false) {
                MessageBox.Query (
                                  "Chosen!",
                                  "You chose:" + Environment.NewLine +
                                  string.Join (Environment.NewLine, fd.MultiSelected.Select (m => m)),
                                  "Ok");
            } else {
                MessageBox.Query (
                                  "Chosen!",
                                  "You chose:" + Environment.NewLine + fd.Path,
                                  "Ok");
            }
        }

        private void ConfirmOverwrite (object sender, FilesSelectedEventArgs e) {
            if (!string.IsNullOrWhiteSpace (e.Dialog.Path)) {
                if (File.Exists (e.Dialog.Path)) {
                    int result = MessageBox.Query ("Overwrite?", "File already exists", "Yes", "No");
                    e.Cancel = result == 1;
                }
            }
        }

        private class CaseSensitiveSearchMatcher : ISearchMatcher {
            private string terms;
            public void Initialize (string terms) { this.terms = terms; }
            public bool IsMatch (IFileSystemInfo f) { return f.Name.Contains (terms, StringComparison.CurrentCulture); }
        }
    }
}
