// 
// FileDialog.cs: File system dialogs for open and save
//

using System;
using System.Collections.Generic;
using NStack;
using System.IO;
using System.Linq;

namespace Terminal.Gui {
	internal class DirListView : View {
		int topFile, currentFile;
		DirectoryInfo dirInfo;
		List<FileSystemInfo> infos;

		public DirListView ()
		{
			infos = new List<FileSystemInfo> ();
			CanFocus = true;
		}

		void Reload ()
		{
			dirInfo = new DirectoryInfo (directory);
			infos = (from x in dirInfo.GetFileSystemInfos () orderby (!x.Attributes.HasFlag (FileAttributes.Directory)) + x.Name select x).ToList ();
			topFile = 0;
			currentFile = 0;
			SetNeedsDisplay ();
		}

		string directory;
		public string Directory {
			get => directory;
			set {
				if (directory != value)
					return;
				
				directory = value;
				Reload ();
			}
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (ColorScheme.Focus);
			var g = Frame;

			for (int y = 0; y < g.Height; y++) {
				Move (0, y);
				for (int x = 0; x < g.Width; x++) {
					Rune r;
					switch (x % 3) {
					case 0:
						r = '.';
						break;
					case 1:
						r = 'o';
						break;
					default:
						r = 'O';
						break;
					}
					Driver.AddRune (r);
				}
			}
			return;
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			var item = topFile;
			bool focused = HasFocus;
			var width = region.Width;

			bool isSelected = false;
			for (int row = 0; row < f.Height; row++, item++) {
				var newcolor = focused ? (isSelected ? ColorScheme.Focus : ColorScheme.Normal) : ColorScheme.Normal;
				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}
				if (item >= infos.Count) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
					continue;
				}
				var fi = infos [item];
				var ustr = ustring.Make (fi.Name);
				int byteLen = ustr.Length;
				int used = 0;
				for (int i = 0; i < byteLen;) {
					(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
					var count = Rune.ColumnWidth (rune);
					if (used + count >= width)
						break;
					Driver.AddRune (rune);
					used += count;
					i += size;
				}
				for (; used < width; used++) {
					Driver.AddRune (' ');
				}
			}
		}
	}

	public class FileDialog : Dialog {
		Button prompt, cancel;
		Label nameFieldLabel, message, dirLabel;
		TextField dirEntry, nameEntry;
		DirListView dirListView;

		public FileDialog (ustring title, ustring prompt, ustring nameFieldLabel, ustring message) : base (title, Driver.Cols - 20, Driver.Rows - 6, null)
		{
			this.message = new Label (Rect.Empty, "MESSAGE" + message);
			var msgLines = Label.MeasureLines (message, Driver.Cols - 20);

			dirLabel = new Label ("Directory: ") {
				X = 2,
				Y = 1 + msgLines
			};

			dirEntry = new TextField ("") {
				X = 12,
				Y = 1 + msgLines,
				Width = Dim.Fill () - 1
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

			dirListView = new DirListView () {
				X = 2,
				Y = 3 + msgLines + 2,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Directory = "."	
			};
			Add (dirListView);

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
