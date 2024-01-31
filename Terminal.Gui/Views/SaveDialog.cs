// 
// FileDialog.cs: File system dialogs for open and save
//
// TODO:
//   * Add directory selector
//   * Implement subclasses
//   * Figure out why message text does not show
//   * Remove the extra space when message does not show
//   * Use a line separator to show the file listing, so we can use same colors as the rest
//   * DirListView: Add mouse support

#region

using Terminal.Gui.Resources;

#endregion

namespace Terminal.Gui {
    /// <summary>
    /// The <see cref="SaveDialog"/> provides an interactive dialog box for users to pick a file to
    /// save.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     To use, create an instance of <see cref="SaveDialog"/>, and pass it to
    ///     <see cref="Application.Run(Func{Exception, bool})"/>. This will run the dialog modally,
    ///     and when this returns, the <see cref="FileName"/>property will contain the selected file name or
    ///     null if the user canceled.
    ///     </para>
    /// </remarks>
    public class SaveDialog : FileDialog {
        /// <summary>
        /// Initializes a new <see cref="SaveDialog"/>.
        /// </summary>
        public SaveDialog () : this (title: string.Empty) { }

        /// <summary>
        /// Initializes a new <see cref="SaveDialog"/>.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="allowedTypes">The allowed types.</param>
        public SaveDialog (string title, List<IAllowedType> allowedTypes = null) {
            //: base (title, prompt: Strings.fdSave, nameFieldLabel: $"{Strings.fdSaveAs}:", message: message, allowedTypes) { }
            Title = title;
            Style.OkButtonText = Strings.btnSave;

            if (allowedTypes != null) {
                AllowedTypes = allowedTypes;
            }
        }

        /// <summary>
        /// Gets the name of the file the user selected for saving, or null
        /// if the user canceled the <see cref="SaveDialog"/>.
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName {
            get {
                if (Canceled)
                    return null;

                return Path;
            }
        }

        /// <summary>
        /// Gets the default title for the <see cref="SaveDialog"/>.
        /// </summary>
        /// <returns></returns>
        protected override string GetDefaultTitle () {
            List<string> titleParts = new () {
                                                 Strings.fdSave
                                             };
            if (MustExist) {
                titleParts.Add (Strings.fdExisting);
            }

            switch (OpenMode) {
                case OpenMode.File:
                    titleParts.Add (Strings.fdFile);

                    break;
                case OpenMode.Directory:
                    titleParts.Add (Strings.fdDirectory);

                    break;
            }

            return string.Join (' ', titleParts);
        }
    }
}
