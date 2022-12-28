
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;

namespace Terminal.Gui {

	public class FileDialog2 : Dialog {
		public string Path { get => tbPath.Text.ToString (); set => tbPath.Text = value; }
		public const string HeaderFilename = "Filename";
		public const string HeaderSize = "Size";
		public const string HeaderModified = "Modified";
		public const string HeaderType = "Type";

		bool proceessingPathChanged = false;


		private TextFieldWithAppendAutocomplete tbPath;

		FileDialogSorter sorter;
		FileDialogHistory history;

		/// <summary>
		/// True to use Utc dates for date modified
		/// </summary>
		public static bool UseUtcDates = false;

		DataTable dtFiles;
		TableView tableView;

		private List<FileSystemInfoStats> fileStats = new List<FileSystemInfoStats> ();

		public static ColorScheme ColorSchemeDirectory;
		public static ColorScheme ColorSchemeDefault;
		public static ColorScheme ColorSchemeImage;

		/// <summary>
		/// ColorScheme to use for entries that are executable or match the users file extension
		/// provided (e.g. if role of dialog is to pick a .csv file)
		/// </summary>
		public static ColorScheme ColorSchemeExeOrInteresting;

		private Button btnOk;
		private Label lblForward;
		private Label lblBack;
		private Label lblUp;

		public FileDialog2 ()
		{
			// TODO: handle Save File / Folder too
			Title = "Open File";

			const int okWidth = 8;

			var lblPath = new Label (">");
			btnOk = new Button ("Ok") {
				X = Pos.AnchorEnd (okWidth),
				IsDefault = true
			};
			this.Add (btnOk);

			lblUp = new Label (Driver.UpArrow.ToString ()) { X = 0, Y = 1 };
			lblUp.Clicked += () => history.Up ();
			this.Add (lblUp);

			lblBack = new Label (Driver.LeftArrow.ToString ()) { X = 2, Y = 1 };
			lblBack.Clicked += () => history.Back ();
			this.Add (lblBack);

			lblForward = new Label (Driver.RightArrow.ToString ()) { X = 3, Y = 1 };
			lblForward.Clicked += () => history.Forward ();
			this.Add (lblForward);

			this.Add (lblPath);
			tbPath = new TextFieldWithAppendAutocomplete {
				X = Pos.Right (lblPath),
				Width = Dim.Fill (okWidth + 1)
			};
			this.Add (tbPath);

			tableView = new TableView {
				X = 0,
				Y = 2,
				Width = Dim.Fill (0),
				Height = Dim.Fill (1),
				FullRowSelect = true,
			};

			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowVerticalCellLines = false;
			tableView.Style.ShowVerticalHeaderLines = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.CellActivated += CellActivate;


			SetupColorSchemes ();

			SetupTableColumns ();

			sorter = new FileDialogSorter (this, tableView);
			history = new FileDialogHistory (this);

			tableView.Table = dtFiles;
			this.Add (tableView);

			tbPath.TextChanged += (s) => PathChanged ();

			// Give this view priority on key handling
			tableView.KeyUp += (k) => k.Handled = this.HandleKey (k.KeyEvent);
			tableView.SelectedCellChanged += TableView_SelectedCellChanged;
			tableView.ColorScheme = ColorSchemeDefault;

			// TODO: delay or consider not doing this to avoid double load
			tbPath.Text = Environment.CurrentDirectory;

			UpdateNavigationVisibility ();
		}

		private void UpdateNavigationVisibility ()
		{
			lblBack.Visible = history.CanBack ();
			lblForward.Visible = history.CanForward ();
			lblUp.Visible = history.CanUp ();
		}

		private void TableView_SelectedCellChanged (TableView.SelectedCellChangedEventArgs obj)
		{
			if (!tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows.Count == 0) {
				return;
			}

			var idx = (int)obj.Table.Rows [obj.NewRow] [0];

			try {
				proceessingPathChanged = true;
				tbPath.Text = fileStats [idx].FileSystemInfo.FullName;
				tbPath.ClearSuggestions ();

			} finally {

				proceessingPathChanged = false;
			}
		}

		class TextFieldWithAppendAutocomplete : TextField {

			int? currentFragment = null;
			string [] validFragments = new string [0];

			char [] separators = new [] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

			public TextFieldWithAppendAutocomplete ()
			{
				KeyPress += (k) => {
					var key = k.KeyEvent.Key;
					if (key == Key.Tab) {
						k.Handled = AcceptSelectionIfAny ();
					} else
					if (key == Key.CursorUp) {
						k.Handled = CycleSuggestion (1);
					} else
					if (key == Key.CursorDown) {
						k.Handled = CycleSuggestion (-1);
					}
				};

				ColorScheme = new ColorScheme {
					Normal = new Attribute (Color.White, Color.Black),
					HotNormal = new Attribute (Color.White, Color.Black),
					Focus = new Attribute (Color.White, Color.Black),
					HotFocus = new Attribute (Color.White, Color.Black),
				};
			}

			private bool CycleSuggestion (int direction)
			{
				if (currentFragment == null || validFragments.Length <= 1) {
					return false;
				}

				currentFragment = (currentFragment + direction) % validFragments.Length;

				if (currentFragment < 0) {
					currentFragment = validFragments.Length - 1;
				}
				SetNeedsDisplay ();
				return true;
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				if (currentFragment == null) {
					return;
				}

				// draw it like its selected even though its not
				Driver.SetAttribute (new Attribute (Color.Black, Color.White));
				Move (Text.Length, 0);
				Driver.AddStr (validFragments [currentFragment.Value]);
			}

			private bool AcceptSelectionIfAny ()
			{
				if (currentFragment != null) {
					Text += validFragments [currentFragment.Value];
					CursorPosition = Text.Length + 1;

					ClearSuggestions ();
					return true;
				}

				return false;
			}

			internal void GenerateSuggestions (List<string> suggestions)
			{
				// if cursor is not at the end then user is editing the middle of the path
				if (CursorPosition < Text.Length - 1) {
					return;
				}

				var path = Text.ToString ();
				var last = path.LastIndexOfAny (separators);

				if (last == -1 || suggestions.Count == 0 || last >= path.Length - 1) {
					currentFragment = null;
					return;
				}

				var term = path.Substring (last + 1);

				// TODO: Be case insensitive on Windows
				var validSuggestions = suggestions
					.Where (s => s.StartsWith (term))
					.OrderBy (m => m.Length)
					.ToArray ();

				// nothing to suggest 
				if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length) {
					ClearSuggestions ();
					return;
				}

				validFragments = validSuggestions.Select (f => f.Substring (term.Length)).ToArray ();
				currentFragment = 0;
			}

			public void ClearSuggestions ()
			{
				currentFragment = null;
				validFragments = new string [0];
				SetNeedsDisplay ();
			}
		}

		public override void OnLoaded ()
		{
			base.OnLoaded ();
			tbPath.FocusFirst ();

			tbPath.TabIndex = 3;
			btnOk.TabIndex = 2;
			tableView.TabIndex = 1;

		}
		private bool HandleKey (KeyEvent keyEvent)
		{
			if (keyEvent.Key == Key.Backspace) {
				return history.Back ();
			}
			if (keyEvent.Key == (Key.ShiftMask | Key.Backspace)) {
				return history.Forward ();
			}

			return false;
		}


		private void SetupColorSchemes ()
		{
			if (ColorSchemeDirectory != null) {
				return;
			}
			ColorSchemeDirectory = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Blue, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Blue, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Blue),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Blue),

			};

			ColorSchemeDefault = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.White, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.White, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Black),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.White),
			};
			ColorSchemeImage = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Magenta, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Magenta, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Magenta),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Magenta),
			};
			ColorSchemeExeOrInteresting = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Green, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Green, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Green),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Green),
			};
		}

		private void SetupTableColumns ()
		{
			dtFiles = new DataTable ();

			var nameStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderFilename, typeof (int)));
			nameStyle.RepresentationGetter = (i) => fileStats [(int)i].FileSystemInfo.Name;

			var sizeStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderSize, typeof (int)));
			sizeStyle.RepresentationGetter = (i) => fileStats [(int)i].HumanReadableLength;

			var dateModifiedStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderModified, typeof (int)));
			dateModifiedStyle.RepresentationGetter = (i) => fileStats [(int)i].DateModified?.ToString () ?? "";

			var typeStyle = tableView.Style.GetOrCreateColumnStyle (dtFiles.Columns.Add (HeaderType, typeof (int)));
			typeStyle.RepresentationGetter = (i) => fileStats [(int)i].Type ?? "";

			tableView.Style.RowColorGetter = ColorGetter;
		}


		private void CellActivate (TableView.CellActivatedEventArgs obj)
		{
			var stats = RowToStats (obj.Row);


			if (stats.FileSystemInfo is DirectoryInfo d) {
				proceessingPathChanged = true;
				tbPath.ClearSuggestions ();
				Path = d.FullName;
				tbPath.CursorPosition = tbPath.Text.Length;
				proceessingPathChanged = false;
				SetupAsDirectory (d);
				history.ClearForward ();
				return;
			}
		}

		private ColorScheme ColorGetter (TableView.RowColorGetterArgs args)
		{
			var stats = RowToStats (args.RowIndex);

			if (stats.IsDir ()) {
				return ColorSchemeDirectory;
			}
			if (stats.IsImage ()) {
				return ColorSchemeImage;
			}
			if (stats.IsExecutable ()) {
				return ColorSchemeExeOrInteresting;
			}

			return ColorSchemeDefault;
		}

		private FileSystemInfoStats RowToStats (int rowIndex)
		{
			return fileStats [(int)tableView.Table.Rows [rowIndex] [0]];
		}


		private void PathChanged ()
		{
			// avoid re-entry
			if (proceessingPathChanged) {
				return;
			}

			proceessingPathChanged = true;
			try {
				var path = tbPath.Text?.ToString ();

				if (string.IsNullOrWhiteSpace (path)) {
					SetupAsClear ();
					return;
				}

				var dir = new DirectoryInfo (path);

				if (dir.Exists) {
					SetupAsDirectory (dir);
				} else
				if (dir.Parent?.Exists ?? false) {
					SetupAsDirectory (dir.Parent);
				}

			} finally {
				proceessingPathChanged = false;
			}

			UpdateNavigationVisibility ();
		}

		private void SetupAsDirectory (DirectoryInfo dir)
		{
			history.Push (dir);

			// TODO : Access permissions Exceptions, Dir not exists etc
			var entries = dir.GetFileSystemInfos ();
			dtFiles.Rows.Clear ();
			fileStats.Clear ();

			var suggestions = entries.Select (
				e => e is DirectoryInfo ? e.Name + System.IO.Path.DirectorySeparatorChar
							: e.Name
							).ToList ();
			tbPath.GenerateSuggestions (suggestions);


			foreach (var e in entries) {
				fileStats.Add (new FileSystemInfoStats (e));
			}

			for (int i = 0; i < fileStats.Count; i++) {
				dtFiles.Rows.Add (i, i, i, i);
			}

			sorter.ApplySort ();
			tableView.Update ();

			tbPath.CursorPosition = tbPath.Text.Length;
		}

		private void SetupAsClear ()
		{

		}

		class FileSystemInfoStats {

			public FileSystemInfo FileSystemInfo { get; }
			public string HumanReadableLength { get; }
			public long MachineReadableLength { get; }
			public DateTime? DateModified { get; }
			public string Type { get; }

			/*
			* Blue: Directory
			* Green: Executable or recognized data file
			* Cyan (Sky Blue): Symbolic link file
			* Yellow with black background: Device
			* Magenta (Pink): Graphic image file
			* Red: Archive file
			* Red with black background: Broken link
			*/

			public FileSystemInfoStats (FileSystemInfo fsi)
			{
				FileSystemInfo = fsi;

				if (fsi is FileInfo fi) {
					MachineReadableLength = fi.Length;
					HumanReadableLength = GetHumanReadableFileSize (MachineReadableLength);
					DateModified = FileDialog2.UseUtcDates ? File.GetLastWriteTimeUtc (fi.FullName) : File.GetLastWriteTime (fi.FullName);
					Type = fi.Extension;
				} else {
					HumanReadableLength = "";
					Type = "dir";
				}
			}

			static readonly string [] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
			static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
			static readonly List<string> ExecutableExtensions = new List<string> { ".EXE", ".BAT" };

			// TODO: is executable;

			public bool IsDir ()
			{
				return Type == "dir";
			}
			public bool IsImage ()
			{
				return this.FileSystemInfo is FileSystemInfo f &&
					ImageExtensions.Contains (f.Extension,
					StringComparer.InvariantCultureIgnoreCase);
			}
			public bool IsExecutable ()
			{
				// TODO: handle linux executable status
				return this.FileSystemInfo is FileSystemInfo f &&
					ExecutableExtensions.Contains (f.Extension,
					StringComparer.InvariantCultureIgnoreCase);
			}

			const long byteConversion = 1024;
			public static string GetHumanReadableFileSize (long value)
			{

				if (value < 0) { return "-" + GetHumanReadableFileSize (-value); }
				if (value == 0) { return "0.0 bytes"; }

				int mag = (int)Math.Log (value, byteConversion);
				double adjustedSize = (value / Math.Pow (1000, mag));


				return string.Format ("{0:n2} {1}", adjustedSize, SizeSuffixes [mag]);
			}

			internal object GetOrderByValue (string columnName)
			{
				switch (columnName) {
				case HeaderFilename: return FileSystemInfo.Name;
				case HeaderSize: return MachineReadableLength;
				case HeaderModified: return DateModified;
				case HeaderType: return Type;
				default: throw new ArgumentOutOfRangeException (nameof (columnName));
				}
			}

			internal object GetOrderByDefault ()
			{
				if (IsDir ()) {
					return -1;
				}

				return 100;
			}
		}

		class FileDialogHistory {
			private Stack<DirectoryInfo> back = new Stack<DirectoryInfo> ();
			private Stack<DirectoryInfo> forward = new Stack<DirectoryInfo> ();
			private bool skipNavigationPush = false;
			private FileDialog2 dlg;
			private DirectoryInfo currentDirectory;

			public FileDialogHistory (FileDialog2 dlg)
			{
				this.dlg = dlg;
			}

			public bool Back ()
			{

				DirectoryInfo goTo = null;

				if (CanBack ()) {

					goTo = back.Pop ();
				} else if (CanUp ()) {
					goTo = currentDirectory?.Parent;
				}

				// nowhere to go
				if (goTo == null) {
					return false;
				}

				forward.Push (currentDirectory);
				GoTo (goTo);
				return true;
			}

			internal bool CanBack ()
			{
				return back.Count > 0;
			}

			internal bool Forward ()
			{
				if (forward.Count > 0) {
					GoTo (forward.Pop ());
					return true;
				}

				return false;
			}
			public bool Up ()
			{
				var parent = currentDirectory?.Parent;
				if (parent != null) {

					back.Push (parent);
					GoTo (parent);
					return true;
				}

				return false;
			}

			internal bool CanUp ()
			{
				return currentDirectory?.Parent != null;
			}

			private void GoTo (DirectoryInfo goTo)
			{
				// Navigate backwards or up but suppress history tracking for op
				skipNavigationPush = true;
				dlg.Path = goTo.FullName;
				skipNavigationPush = false;
				dlg.SetNeedsDisplay ();
				dlg.UpdateNavigationVisibility ();
			}

			internal void Push (DirectoryInfo dir)
			{
				// if changing directory
				if (back.Count == 0 || back.Peek () != currentDirectory) {

					if (currentDirectory != null && !skipNavigationPush) {
						back.Push (currentDirectory);
						ClearForward ();
					}

					currentDirectory = dir;
				}
			}

			internal bool CanForward ()
			{
				return forward.Count > 0;
			}

			internal void ClearForward ()
			{
				forward.Clear ();
			}
		}
		private class FileDialogSorter {
			readonly FileDialog2 dlg;
			private TableView tableView;

			private DataColumn currentSort = null;
			private bool currentSortIsAsc = true;

			public FileDialogSorter (FileDialog2 dlg, TableView tableView)
			{
				this.dlg = dlg;
				this.tableView = tableView;

				// if user clicks the mouse in TableView
				this.tableView.MouseClick += e => {

					this.tableView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out DataColumn clickedCol);

					if (clickedCol != null) {
						if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {

							// left click in a header
							SortColumn (clickedCol);
						} else if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

							// right click in a header
							ShowHeaderContextMenu (clickedCol, e);
						}
					}
				};

			}
			private void SortColumn (DataColumn clickedCol)
			{
				GetProposedNewSortOrder (clickedCol, out var isAsc);
				SortColumn (clickedCol, isAsc);
			}

			private void SortColumn (DataColumn col, bool isAsc)
			{
				// set a sort order
				currentSort = col;
				currentSortIsAsc = isAsc;

				ApplySort ();
			}

			public void ApplySort ()
			{
				var col = currentSort;

				// TODO: Consider preserving selection
				tableView.Table.Rows.Clear ();

				var colName = col == null ? null : StripArrows (col.ColumnName);

				var ordered =
					colName == null ? dlg.fileStats.Select ((v, i) => new { v, i }).OrderBy (f => f.v.GetOrderByDefault ()).ToArray () :
					currentSortIsAsc ?
					    dlg.fileStats.Select ((v, i) => new { v, i }).OrderBy (f => f.v.GetOrderByValue (colName)).ToArray () :
					    dlg.fileStats.Select ((v, i) => new { v, i }).OrderByDescending (f => f.v.GetOrderByValue (colName)).ToArray ();

				foreach (var o in ordered) {
					BuildRow (o.i);
				}

				foreach (DataColumn c in tableView.Table.Columns) {

					// remove any lingering sort indicator
					c.ColumnName = TrimArrows (c.ColumnName);

					// add a new one if this the one that is being sorted
					if (c == col) {
						c.ColumnName += currentSortIsAsc ? '▲' : '▼';
					}
				}

				tableView.Update ();
			}

			private void BuildRow (int idx)
			{
				tableView.Table.Rows.Add (idx, idx, idx, idx);
			}

			private static string TrimArrows (string columnName)
			{
				return columnName.TrimEnd ('▼', '▲');
			}
			private static string StripArrows (string columnName)
			{
				return columnName.Replace ("▼", "").Replace ("▲", "");
			}
			private string GetProposedNewSortOrder (DataColumn clickedCol, out bool isAsc)
			{
				// work out new sort order
				if (currentSort == clickedCol && currentSortIsAsc) {
					isAsc = false;
					return $"{clickedCol.ColumnName} DESC";
				} else {
					isAsc = true;
					return $"{clickedCol.ColumnName} ASC";
				}
			}

			private void ShowHeaderContextMenu (DataColumn clickedCol, View.MouseEventArgs e)
			{
				var sort = GetProposedNewSortOrder (clickedCol, out var isAsc);

				var contextMenu = new ContextMenu (e.MouseEvent.X + 1, e.MouseEvent.Y + 1,
					new MenuBarItem (new MenuItem [] {
					new MenuItem ($"Hide {TrimArrows(clickedCol.ColumnName)}", "", () => HideColumn(clickedCol)),
					new MenuItem ($"Sort {StripArrows(sort)}","",()=>SortColumn(clickedCol,isAsc)),
					})
				);

				contextMenu.Show ();
			}

			private void HideColumn (DataColumn clickedCol)
			{
				var style = tableView.Style.GetOrCreateColumnStyle (clickedCol);
				style.Visible = false;
				tableView.Update ();
			}
		}
	}
}