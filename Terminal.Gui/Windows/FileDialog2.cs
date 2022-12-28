
using System;
using System.Collections.Generic;
using NStack;
using System.IO;
using System.Linq;
using Terminal.Gui.Resources;
using System.Data;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Terminal.Gui {

	public class FileDialog2 : Dialog {
		public string Path { get => tbPath.Text.ToString (); set => tbPath.Text = value; }
		public const string HeaderFilename = "Filename";
		public const string HeaderSize = "Size";
		public const string HeaderModified = "Modified";
		public const string HeaderType = "Type";

		bool proceessingPathChanged = false;
		private Stack<DirectoryInfo> navigationStack = new Stack<DirectoryInfo> ();
		private bool skipNavigationPush = false;


		private TextFieldWithAppendAutocomplete tbPath;
		private DirectoryInfo currentDirectory;

		/// <summary>
		/// True to use Utc dates for date modified
		/// </summary>
		public static bool UseUtcDates = false;

		DataTable dtFiles;
		TableView tableView;

		List<FileSystemInfoStats> fileStats = new List<FileSystemInfoStats> ();

		public static ColorScheme ColorSchemeDirectory;
		public static ColorScheme ColorSchemeDefault;
		public FileDialog2 ()
		{
			const int okWidth = 8;

			var lblPath = new Label ("Path:");
			var btnOk = new Button ("Ok") {
				IsDefault = true,
				X = Pos.AnchorEnd (okWidth),
			};
			this.Add (btnOk);

			this.Add (lblPath);
			tbPath = new TextFieldWithAppendAutocomplete {
				X = Pos.Right (lblPath),
				Width = Dim.Fill (okWidth)
			};
			this.Add (tbPath);

			tableView = new TableView {
				X = 0,
				Y = 1,
				Width = Dim.Fill (0),
				Height = Dim.Fill (1),
				FullRowSelect = true,
			};

			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowVerticalCellLines = false;
			tableView.Style.ShowVerticalHeaderLines = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.CellActivated += CellActivate;

			// if user clicks the mouse in TableView
			tableView.MouseClick += e => {

				tableView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out DataColumn clickedCol);

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

			SetupColorSchemes ();

			SetupTableColumns ();

			tableView.Table = dtFiles;
			this.Add (tableView);

			tbPath.TextChanged += (s) => PathChanged ();

			// Give this view priority on key handling
			tableView.KeyUp += (k) => k.Handled = this.HandleKey (k.KeyEvent);
			tableView.SelectedCellChanged += TableView_SelectedCellChanged;
			tableView.ColorScheme = ColorSchemeDefault;

			// TODO: delay or consider not doing this to avoid double load
			tbPath.Text = Environment.CurrentDirectory;
		}

		private void TableView_SelectedCellChanged (TableView.SelectedCellChangedEventArgs obj)
		{
			// if Table is empty or no selection
			if(obj.NewRow == -1 || obj.Table.Rows.Count == 0) {
				return;
			}

			var idx = (int)obj.Table.Rows [obj.NewRow][0];

			try {
				proceessingPathChanged = true;
				tbPath.Text = fileStats [idx].FileSystemInfo.FullName;
				tbPath.ClearAllSelection ();

			} finally {

				proceessingPathChanged = false;
			}
		}

		class TextFieldWithAppendAutocomplete : TextField {

			int? currentFragment = null;
			string[] validFragments = new string[0];

			char [] separators = new [] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };
			
			public TextFieldWithAppendAutocomplete ()
			{
				KeyPress += (k) => {
					if (k.KeyEvent.Key == Key.Tab) {
						k.Handled = AcceptSelectionIfAny ();
					}
				};
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				if(currentFragment == null) {
					return;
				}

				// draw it like its selected even though its not
				Driver.SetAttribute (new Attribute (Color.Black,Color.White));
				Move (Text.Length,0);
				Driver.AddStr(validFragments[currentFragment.Value]);
			}

			private bool AcceptSelectionIfAny ()
			{
				if(currentFragment != null) {
					Text += validFragments [currentFragment.Value];
					CursorPosition = Text.Length+1;
					return true;
				}

				return false;
			}

			internal void GenerateSuggestions (List<string> suggestions)
			{
				// if cursor is not at the end then user is editing the middle of the path
				if(CursorPosition < Text.Length-1) {
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
					.ToArray();
				
				// nothing to suggest 
				if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length) {
					ClearSuggestions ();
					return;
				}

				validFragments = validSuggestions.Select (f => f.Substring (term.Length)).ToArray();
				currentFragment  = 0;
			}

			public void ClearSuggestions ()
			{
				currentFragment = null;
				validFragments = new string [0];
			}
		}
		private bool HandleKey (KeyEvent keyEvent)
		{
			if (keyEvent.Key == Key.Backspace) {
				return ProcessBackspaceKeystroke ();

			}
			return false;
		}

		/// <summary>
		/// Navigates backwards through the navigation history or if history
		/// is empty navigates up the current directory hierarchy.
		/// </summary>
		/// <returns></returns>
		private bool ProcessBackspaceKeystroke ()
		{
			DirectoryInfo goTo = null;

			if (navigationStack.Count > 0) {

				goTo = navigationStack.Pop ();
			} else {

				if (currentDirectory != null) {
					goTo = currentDirectory.Parent;
				}
			}

			// nowhere to go
			if (goTo == null) {
				return false;
			}

			// Navigate backwards or up but suppress history tracking for op
			skipNavigationPush = true;
			Path = goTo.FullName;
			skipNavigationPush = false;
			this.SetNeedsDisplay ();
			return true;
		}

		private void SortColumn (DataColumn clickedCol)
		{
			var sort = GetProposedNewSortOrder (clickedCol, out var isAsc);

			SortColumn (clickedCol, sort, isAsc);
		}

		private void SortColumn (DataColumn clickedCol, string sort, bool isAsc)
		{
			// set a sort order
			var style = tableView.Style.GetOrCreateColumnStyle (clickedCol);
			tableView.Table.DefaultView.Sort = sort;

			// TODO: Consider preserving selection
			dtFiles.Rows.Clear ();

			var colName = StripArrows (clickedCol.ColumnName);

			var ordered = isAsc ?
			    fileStats.Select ((v, i) => new { v, i }).OrderBy (f => f.v.GetOrderByValue (colName)).ToArray () :
			    fileStats.Select ((v, i) => new { v, i }).OrderByDescending (f => f.v.GetOrderByValue (colName)).ToArray ();

			foreach (var o in ordered) {
				dtFiles.Rows.Add (o.i, o.i, o.i, o.i);
			}

			foreach (DataColumn col in tableView.Table.Columns) {

				// remove any lingering sort indicator
				col.ColumnName = TrimArrows (col.ColumnName);

				// add a new one if this the one that is being sorted
				if (col == clickedCol) {
					col.ColumnName += isAsc ? '▲' : '▼';
				}
			}

			tableView.Update ();
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
			var sort = tableView.Table.DefaultView.Sort;

			if (sort?.EndsWith ("ASC") ?? false) {
				sort = $"{clickedCol.ColumnName} DESC";
				isAsc = false;
			} else {
				sort = $"{clickedCol.ColumnName} ASC";
				isAsc = true;
			}

			return sort;
		}

		private void ShowHeaderContextMenu (DataColumn clickedCol, View.MouseEventArgs e)
		{
			var sort = GetProposedNewSortOrder (clickedCol, out var isAsc);

			var contextMenu = new ContextMenu (e.MouseEvent.X + 1, e.MouseEvent.Y + 1,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ($"Hide {TrimArrows(clickedCol.ColumnName)}", "", () => HideColumn(clickedCol)),
					new MenuItem ($"Sort {StripArrows(sort)}","",()=>SortColumn(clickedCol,sort,isAsc)),
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
				Path = d.FullName;
				proceessingPathChanged = false;
				SetupAsDirectory (d);
				tbPath.ClearSuggestions ();
				return;
			}
		}

		private ColorScheme ColorGetter (TableView.RowColorGetterArgs args)
		{
			var stats = RowToStats (args.RowIndex);

			if (stats.Type == "dir") {
				return ColorSchemeDirectory;
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
				if(dir.Parent?.Exists ?? false){
					SetupAsDirectory (dir.Parent);
				}

			} finally {
				proceessingPathChanged = false;
			}

			return;
		}

		private void SetupAsDirectory (DirectoryInfo dir)
		{
			// if changing directory
			if (navigationStack.Count == 0 || navigationStack.Peek () != currentDirectory) {

				if (currentDirectory != null && !skipNavigationPush) {
					navigationStack.Push (currentDirectory);
				}

				currentDirectory = dir;

			}

			// TODO : Access permissions Exceptions, Dir not exists etc
			var entries = dir.GetFileSystemInfos ();
			dtFiles.Rows.Clear ();
			fileStats.Clear ();

			var suggestions = entries.Select (e => e.Name).ToList ();
			tbPath.GenerateSuggestions (suggestions);


			foreach (var e in entries) {
				fileStats.Add (new FileSystemInfoStats (e));
			}

			for (int i = 0; i < fileStats.Count; i++) {
				dtFiles.Rows.Add (i, i, i, i);
			}

			tableView.Update ();
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

			// TODO: is executable;

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
		}
	}
}