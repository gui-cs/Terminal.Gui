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

using System;
using System.Collections.Generic;
using NStack;
using System.IO;
using System.Linq;

namespace Terminal.Gui {
	internal class DirListView : View {
		int top, selected;
		DirectoryInfo dirInfo;
		List<(string, bool, bool)> infos;
		internal bool canChooseFiles = true;
		internal bool canChooseDirectories = false;
		internal bool allowsMultipleSelection = false;
		FileDialog host;

		public DirListView (FileDialog host)
		{
			infos = new List<(string, bool, bool)> ();
			CanFocus = true;
			this.host = host;
		}

		bool IsAllowed (FileSystemInfo fsi)
		{
			if (fsi.Attributes.HasFlag (FileAttributes.Directory))
				return true;
			if (allowedFileTypes == null)
				return true;
			foreach (var ft in allowedFileTypes)
				if (fsi.Name.EndsWith (ft))
					return true;
			return false;
		}

		internal void Reload ()
		{
			dirInfo = new DirectoryInfo (directory.ToString ());
			infos = (from x in dirInfo.GetFileSystemInfos ()
				 where IsAllowed (x)
				 orderby (!x.Attributes.HasFlag (FileAttributes.Directory)) + x.Name
				 select (x.Name, x.Attributes.HasFlag (FileAttributes.Directory), false)).ToList ();
			infos.Insert (0, ("..", true, false));
			top = 0;
			selected = 0;
			SetNeedsDisplay ();
		}

		ustring directory;
		public ustring Directory {
			get => directory;
			set {
				if (directory == value)
					return;
				directory = value;
				Reload ();
			}
		}

		public override void PositionCursor ()
		{
			Move (0, selected - top);
		}

		int lastSelected;
		bool shiftOnWheel;
		public override bool MouseEvent (MouseEvent me)
		{
			if ((me.Flags & (MouseFlags.Button1Clicked | MouseFlags.Button1DoubleClicked |
				MouseFlags.WheeledUp | MouseFlags.WheeledDown)) == 0)
				return false;

			if (!HasFocus)
				SuperView.SetFocus (this);

			if (infos == null)
				return false;

			if (me.Y + top >= infos.Count)
				return true;

			int lastSelectedCopy = shiftOnWheel ? lastSelected : selected;

			switch (me.Flags) {
			case MouseFlags.Button1Clicked:
				SetSelected (me);
				SelectionChanged ();
				SetNeedsDisplay ();
				break;
			case MouseFlags.Button1DoubleClicked:
				SetSelected (me);
				if (ExecuteSelection ()) {
					host.canceled = false;
					Application.RequestStop ();
				}
				return true;
			case MouseFlags.Button1Clicked | MouseFlags.ButtonShift:
				SetSelected (me);
				if (shiftOnWheel)
					lastSelected = lastSelectedCopy;
				shiftOnWheel = false;
				PerformMultipleSelection (lastSelected);
				return true;
			case MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl:
				SetSelected (me);
				PerformMultipleSelection ();
				return true;
			case MouseFlags.WheeledUp:
				SetSelected (me);
				selected = lastSelected;
				MoveUp ();
				return true;
			case MouseFlags.WheeledDown:
				SetSelected (me);
				selected = lastSelected;
				MoveDown ();
				return true;
			case MouseFlags.WheeledUp | MouseFlags.ButtonShift:
				SetSelected (me);
				selected = lastSelected;
				lastSelected = lastSelectedCopy;
				shiftOnWheel = true;
				MoveUp ();
				return true;
			case MouseFlags.WheeledDown | MouseFlags.ButtonShift:
				SetSelected (me);
				selected = lastSelected;
				lastSelected = lastSelectedCopy;
				shiftOnWheel = true;
				MoveDown ();
				return true;
			}

			return true;
		}

		void SetSelected (MouseEvent me)
		{
			lastSelected = selected;
			selected = top + me.Y;
		}

		void DrawString (int line, string str)
		{
			var f = Frame;
			var width = f.Width;
			var ustr = ustring.Make (str);

			Move (allowsMultipleSelection ? 3 : 2, line);
			int byteLen = ustr.Length;
			int used = allowsMultipleSelection ? 2 : 1;
			for (int i = 0; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = Rune.ColumnWidth (rune);
				if (used + count >= width)
					break;
				Driver.AddRune (rune);
				used += count;
				i += size;
			}
			for (; used < width - 1; used++) {
				Driver.AddRune (' ');
			}
		}

		public override void Redraw (Rect bounds)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			var item = top;
			bool focused = HasFocus;
			var width = bounds.Width;

			for (int row = 0; row < f.Height; row++, item++) {
				bool isSelected = item == selected;
				Move (0, row);
				var newcolor = focused ? (isSelected ? ColorScheme.HotNormal : ColorScheme.Focus) : ColorScheme.Focus;
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

				Driver.AddRune (isSelected ? '>' : ' ');

				if (allowsMultipleSelection)
					Driver.AddRune (fi.Item3 ? '*' : ' ');

				if (fi.Item2)
					Driver.AddRune ('/');
				else
					Driver.AddRune (' ');
				DrawString (row, fi.Item1);
			}
		}

		public Action<(string, bool)> SelectedChanged { get; set; }
		public Action<ustring> DirectoryChanged { get; set; }
		public Action<ustring> FileChanged { get; set; }

		void SelectionChanged ()
		{
			if (FilePaths.Count > 0)
				FileChanged?.Invoke (string.Join (", ", GetFilesName (FilePaths)));
			else
				FileChanged?.Invoke (infos [selected].Item2 ? "" : Path.GetFileName (infos [selected].Item1));
			if (SelectedChanged != null) {
				var sel = infos [selected];
				SelectedChanged ((sel.Item1, sel.Item2));
			}
		}

		List<string> GetFilesName (IReadOnlyList<string> files)
		{
			List<string> filesName = new List<string> ();

			foreach (var file in files) {
				filesName.Add (Path.GetFileName (file));
			}

			return filesName;
		}

		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
			case Key.CursorUp:
			case Key.ControlP:
				MoveUp ();
				return true;

			case Key.CursorDown:
			case Key.ControlN:
				MoveDown ();
				return true;

			case Key.ControlV:
			case Key.PageDown:
				var n = (selected + Frame.Height);
				if (n > infos.Count)
					n = infos.Count - 1;
				if (n != selected) {
					selected = n;
					if (infos.Count >= Frame.Height)
						top = selected;
					else
						top = 0;
					SelectionChanged ();

					SetNeedsDisplay ();
				}
				return true;

			case Key.Enter:
				if (ExecuteSelection ())
					return false;
				else
					return true;

			case Key.PageUp:
				n = (selected - Frame.Height);
				if (n < 0)
					n = 0;
				if (n != selected) {
					selected = n;
					top = selected;
					SelectionChanged ();
					SetNeedsDisplay ();
				}
				return true;

			case Key.Space:
			case Key.ControlT:
				PerformMultipleSelection ();
				return true;

			case Key.Home:
				MoveFirst ();
				return true;

			case Key.End:
				MoveLast ();
				return true;
			}
			return base.ProcessKey (keyEvent);
		}

		void MoveLast ()
		{
			selected = infos.Count - 1;
			top = infos.Count () - 1;
			SelectionChanged ();
			SetNeedsDisplay ();
		}

		void MoveFirst ()
		{
			selected = 0;
			top = 0;
			SelectionChanged ();
			SetNeedsDisplay ();
		}

		void MoveDown ()
		{
			if (selected + 1 < infos.Count) {
				selected++;
				if (selected >= top + Frame.Height)
					top++;
				SelectionChanged ();
				SetNeedsDisplay ();
			}
		}

		void MoveUp ()
		{
			if (selected > 0) {
				selected--;
				if (selected < top)
					top = selected;
				SelectionChanged ();
				SetNeedsDisplay ();
			}
		}

		internal bool ExecuteSelection ()
		{
			var isDir = infos [selected].Item2;

			if (isDir) {
				Directory = Path.GetFullPath (Path.Combine (Path.GetFullPath (Directory.ToString ()), infos [selected].Item1));
				DirectoryChanged?.Invoke (Directory);
			} else {
				FileChanged?.Invoke (infos [selected].Item1);
				if (canChooseFiles) {
					// Ensures that at least one file is selected.
					if (FilePaths.Count == 0)
						PerformMultipleSelection ();
					// Let the OK handler take it over
					return true;
				}
				// No files allowed, do not let the default handler take it.
			}
			return false;
		}

		void PerformMultipleSelection (int? firstSelected = null)
		{
			if (allowsMultipleSelection) {
				int first = Math.Min (firstSelected ?? selected, selected);
				int last = Math.Max (selected, firstSelected ?? selected);
				for (int i = first; i <= last; i++) {
					if ((canChooseFiles && infos [i].Item2 == false) ||
					    (canChooseDirectories && infos [i].Item2 &&
					     infos [i].Item1 != "..")) {
						infos [i] = (infos [i].Item1, infos [i].Item2, !infos [i].Item3);
					}
				}
				SelectionChanged ();
				SetNeedsDisplay ();
			}
		}

		string [] allowedFileTypes;
		public string [] AllowedFileTypes {
			get => allowedFileTypes;
			set {
				allowedFileTypes = value;
				Reload ();
			}
		}

		public string MakePath (string relativePath)
		{
			return Path.GetFullPath (Path.Combine (Directory.ToString (), relativePath));
		}

		public IReadOnlyList<string> FilePaths {
			get {
				if (allowsMultipleSelection) {
					var res = new List<string> ();
					foreach (var item in infos)
						if (item.Item3)
							res.Add (MakePath (item.Item1));
					return res;
				} else {
					if (infos [selected].Item2) {
						if (canChooseDirectories)
							return new List<string> () { MakePath (infos [selected].Item1) };
						return Array.Empty<string> ();
					} else {
						if (canChooseFiles)
							return new List<string> () { MakePath (infos [selected].Item1) };
						return Array.Empty<string> ();
					}
				}
			}
		}
	}

	/// <summary>
	/// Base class for the <see cref="OpenDialog"/> and the <see cref="SaveDialog"/>
	/// </summary>
	public class FileDialog : Dialog {
		Button prompt, cancel;
		Label nameFieldLabel, message, dirLabel;
		TextField dirEntry, nameEntry;
		internal DirListView dirListView;

		/// <summary>
		/// Initializes a new <see cref="FileDialog"/>.
		/// </summary>
		public FileDialog () : this (title: string.Empty, prompt: string.Empty, nameFieldLabel: string.Empty, message: string.Empty) { }

		/// <summary>
		/// Initializes a new instance of <see cref="FileDialog"/>
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="prompt">The prompt.</param>
		/// <param name="nameFieldLabel">The name field label.</param>
		/// <param name="message">The message.</param>
		public FileDialog (ustring title, ustring prompt, ustring nameFieldLabel, ustring message) : base (title, Driver.Cols - 20, Driver.Rows - 5, null)
		{
			this.message = new Label (Rect.Empty, "MESSAGE" + message);
			var msgLines = Label.MeasureLines (message, Driver.Cols - 20);

			dirLabel = new Label ("Directory: ") {
				X = 1,
				Y = 1 + msgLines
			};

			dirEntry = new TextField ("") {
				X = Pos.Right (dirLabel),
				Y = 1 + msgLines,
				Width = Dim.Fill () - 1
			};
			Add (dirLabel, dirEntry);

			this.nameFieldLabel = new Label ("Open: ") {
				X = 6,
				Y = 3 + msgLines,
			};
			nameEntry = new TextField ("") {
				X = Pos.Left (dirEntry),
				Y = 3 + msgLines,
				Width = Dim.Fill () - 1
			};
			Add (this.nameFieldLabel, nameEntry);

			dirListView = new DirListView (this) {
				X = 1,
				Y = 3 + msgLines + 2,
				Width = Dim.Fill () - 1,
				Height = Dim.Fill () - 2,
			};
			DirectoryPath = Path.GetFullPath (Environment.CurrentDirectory);
			Add (dirListView);
			dirListView.DirectoryChanged = (dir) => dirEntry.Text = dir;
			dirListView.FileChanged = (file) => nameEntry.Text = file;

			this.cancel = new Button ("Cancel");
			this.cancel.Clicked += () => {
				canceled = true;
				Application.RequestStop ();
			};
			AddButton (cancel);

			this.prompt = new Button (prompt) {
				IsDefault = true,
			};
			this.prompt.Clicked += () => {
				dirListView.ExecuteSelection ();
				canceled = false;
				Application.RequestStop ();
			};
			AddButton (this.prompt);

			Width = Dim.Percent (80);
			Height = Dim.Percent (80);

			// On success, we will set this to false.
			canceled = true;
		}

		internal bool canceled;

		///<inheritdoc/>
		public override void WillPresent ()
		{
			base.WillPresent ();
			//SetFocus (nameEntry);
		}

		/// <summary>
		/// Gets or sets the prompt label for the <see cref="Button"/> displayed to the user
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
		/// Gets or sets a value indicating whether this <see cref="FileDialog"/> can create directories.
		/// </summary>
		/// <value><c>true</c> if can create directories; otherwise, <c>false</c>.</value>
		public bool CanCreateDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FileDialog"/> is extension hidden.
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
				dirListView.Directory = value;
			}
		}

		/// <summary>
		/// The array of filename extensions allowed, or null if all file extensions are allowed.
		/// </summary>
		/// <value>The allowed file types.</value>
		public string [] AllowedFileTypes {
			get => dirListView.AllowedFileTypes;
			set => dirListView.AllowedFileTypes = value;
		}


		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FileDialog"/> allows the file to be saved with a different extension
		/// </summary>
		/// <value><c>true</c> if allows other file types; otherwise, <c>false</c>.</value>
		public bool AllowsOtherFileTypes { get; set; }

		/// <summary>
		/// The File path that is currently shown on the panel
		/// </summary>
		/// <value>The absolute file path for the file path entered.</value>
		public ustring FilePath {
			get => dirListView.MakePath (nameEntry.Text.ToString ());
			set {
				nameEntry.Text = Path.GetFileName (value.ToString ());
			}
		}

		/// <summary>
		/// Check if the dialog was or not canceled.
		/// </summary>
		public bool Canceled { get => canceled; }
	}

	/// <summary>
	///  The <see cref="SaveDialog"/> provides an interactive dialog box for users to pick a file to 
	///  save.
	/// </summary>
	/// <remarks>
	/// <para>
	///   To use, create an instance of <see cref="SaveDialog"/>, and pass it to
	///   <see cref="Application.Run()"/>. This will run the dialog modally,
	///   and when this returns, the <see cref="FileName"/>property will contain the selected file name or 
	///   null if the user canceled. 
	/// </para>
	/// </remarks>
	public class SaveDialog : FileDialog {
		/// <summary>
		/// Initializes a new <see cref="SaveDialog"/>.
		/// </summary>
		public SaveDialog () : this (title: string.Empty, message: string.Empty) { }

		/// <summary>
		/// Initializes a new <see cref="SaveDialog"/>.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="message">The message.</param>
		public SaveDialog (ustring title, ustring message) : base (title, prompt: "Save", nameFieldLabel: "Save as:", message: message) { }

		/// <summary>
		/// Gets the name of the file the user selected for saving, or null
		/// if the user canceled the <see cref="SaveDialog"/>.
		/// </summary>
		/// <value>The name of the file.</value>
		public ustring FileName {
			get {
				if (canceled)
					return null;
				return Path.GetFileName (FilePath.ToString ());
			}
		}
	}

	/// <summary>
	/// The <see cref="OpenDialog"/>provides an interactive dialog box for users to select files or directories.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The open dialog can be used to select files for opening, it can be configured to allow
	///   multiple items to be selected (based on the AllowsMultipleSelection) variable and
	///   you can control whether this should allow files or directories to be selected.
	/// </para>
	/// <para>
	///   To use, create an instance of <see cref="OpenDialog"/>, and pass it to
	///   <see cref="Application.Run()"/>. This will run the dialog modally,
	///   and when this returns, the list of filds will be available on the <see cref="FilePaths"/> property.
	/// </para>
	/// <para>
	/// To select more than one file, users can use the spacebar, or control-t.
	/// </para>
	/// </remarks>
	public class OpenDialog : FileDialog {
		/// <summary>
		/// Initializes a new <see cref="OpenDialog"/>.
		/// </summary>
		public OpenDialog () : this (title: string.Empty, message: string.Empty) { }

		/// <summary>
		/// Initializes a new <see cref="OpenDialog"/>.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		public OpenDialog (ustring title, ustring message) : base (title, prompt: "Open", nameFieldLabel: "Open", message: message)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Terminal.Gui.OpenDialog"/> can choose files.
		/// </summary>
		/// <value><c>true</c> if can choose files; otherwise, <c>false</c>.  Defaults to <c>true</c></value>
		public bool CanChooseFiles {
			get => dirListView.canChooseFiles;
			set {
				dirListView.canChooseFiles = value;
				dirListView.Reload ();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="OpenDialog"/> can choose directories.
		/// </summary>
		/// <value><c>true</c> if can choose directories; otherwise, <c>false</c> defaults to <c>false</c>.</value>
		public bool CanChooseDirectories {
			get => dirListView.canChooseDirectories;
			set {
				dirListView.canChooseDirectories = value;
				dirListView.Reload ();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="OpenDialog"/> allows multiple selection.
		/// </summary>
		/// <value><c>true</c> if allows multiple selection; otherwise, <c>false</c>, defaults to false.</value>
		public bool AllowsMultipleSelection {
			get => dirListView.allowsMultipleSelection;
			set {
				dirListView.allowsMultipleSelection = value;
				dirListView.Reload ();
			}
		}

		/// <summary>
		/// Returns the selected files, or an empty list if nothing has been selected
		/// </summary>
		/// <value>The file paths.</value>
		public IReadOnlyList<string> FilePaths {
			get => dirListView.FilePaths;
		}
	}
}
