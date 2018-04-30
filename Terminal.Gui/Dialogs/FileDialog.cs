// 
// FileDialog.cs: File system dialogs for open and save
//

using System;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	public class FileDialog : Dialog {
		Button prompt, cancel;
		Label nameFieldLabel, message, dirLabel;
		TextField dirEntry, nameEntry;

		public FileDialog (ustring title, ustring prompt, ustring nameFieldLabel, ustring message) : base (title, Driver.Cols - 20, Driver.Rows - 6, null)
		{
			this.message = new Label (Rect.Empty, message);
			var msgLines = Label.MeasureLines (message, Driver.Cols - 20);

			dirLabel = new Label ("Directory: ") {
				X = 2,
				Y = 1 + msgLines
			};

			dirEntry = new TextField ("") {
				X = 12,
				Y = 1 + msgLines
			};
			Add (dirLabel, dirEntry);

			this.nameFieldLabel = new Label (nameFieldLabel) {
				X = 2,
				Y = 3 + msgLines,
			};
			nameEntry = new TextField ("") {
				X = 2 + nameFieldLabel.RuneCount + 1,
				Y = 3 + msgLines,
				Width = Dim.Fill () - 1
			};
			Add (this.nameFieldLabel, nameEntry);

			this.cancel = new Button ("Cancel");
			AddButton (cancel);

			this.prompt = new Button (prompt);
			AddButton (this.prompt);
		}

		/// <summary>
		/// Gets or sets the prompt label for the button displayed to the user
		/// </summary>
		/// <value>The prompt.</value>
		public ustring Prompt {
			get => prompt.Text;
			set {
				prompt.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the name field label.
		/// </summary>
		/// <value>The name field label.</value>
		public ustring NameFieldLabel {
			get => nameFieldLabel.Text;
			set {
				nameFieldLabel.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the message displayed to the user, defaults to nothing
		/// </summary>
		/// <value>The message.</value>
		public ustring Message {
			get => message.Text;
			set {
				message.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.FileDialog"/> can create directories.
		/// </summary>
		/// <value><c>true</c> if can create directories; otherwise, <c>false</c>.</value>
		public bool CanCreateDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.FileDialog"/> is extension hidden.
		/// </summary>
		/// <value><c>true</c> if is extension hidden; otherwise, <c>false</c>.</value>
		public bool IsExtensionHidden { get; set; }

		/// <summary>
		/// Gets or sets the directory path for this panel
		/// </summary>
		/// <value>The directory path.</value>
		public ustring DirectoryPath {
			get => dirEntry.Text;
			set {
				dirEntry.Text = value;
			}
		}

		/// <summary>
		/// The array of filename extensions allowed, or null if all file extensions are allowed.
		/// </summary>
		/// <value>The allowed file types.</value>
		public ustring [] AllowedFileTypes { get; set; }


		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.FileDialog"/> allows the file to be saved with a different extension
		/// </summary>
		/// <value><c>true</c> if allows other file types; otherwise, <c>false</c>.</value>
		public bool AllowsOtherFileTypes { get; set; }

		/// <summary>
		/// The File path that is currently shown on the panel
		/// </summary>
		/// <value>The absolute file path for the file path entered.</value>
		public ustring FilePath {
			get => nameEntry.Text;
			set {
				nameEntry.Text = value;
			}
		}
	}

	public class SaveDialog : FileDialog {
		public SaveDialog (ustring title, ustring message) : base (title, prompt: "Save", nameFieldLabel: "Save as:", message: message)
		{
		}
	}

	public class OpenDialog : FileDialog {
		public OpenDialog (ustring title, ustring message) : base (title, prompt: "Open", nameFieldLabel: "Open", message: message)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.OpenDialog"/> can choose files.
		/// </summary>
		/// <value><c>true</c> if can choose files; otherwise, <c>false</c>.</value>
		public bool CanChooseFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.OpenDialog"/> can choose directories.
		/// </summary>
		/// <value><c>true</c> if can choose directories; otherwise, <c>false</c>.</value>
		public bool CanChooseDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.OpenDialog"/> allows multiple selection.
		/// </summary>
		/// <value><c>true</c> if allows multiple selection; otherwise, <c>false</c>.</value>
		public bool AllowsMultipleSelection { get; set; }

		/// <summary>
		/// Gets the file paths selected
		/// </summary>
		/// <value>The file paths.</value>
		public IReadOnlyList<ustring> FilePaths { get; }
	}
}
