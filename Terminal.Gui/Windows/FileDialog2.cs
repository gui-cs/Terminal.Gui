using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NStack;
using Terminal.Gui.Trees;
using static System.Environment;
using static Terminal.Gui.OpenDialog;

namespace Terminal.Gui {

	/// <summary>
	/// Modal dialog for selecting files/directories.  Has auto-complete and expandable
	/// navigation pane (Recent, Root drives etc).
	/// </summary>
	public class FileDialog2 : Dialog {

		// TODO : expose these somehow for localization without compromising case/switch statements
		private const string HeaderFilename = "Filename";
		private const string HeaderSize = "Size";
		private const string HeaderModified = "Modified";
		private const string HeaderType = "Type";

		private static char [] separators = new []
		{
			System.IO.Path.AltDirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar,
		};

		/// <summary>
		/// Characters to prevent entry into <see cref="tbPath"/>.  Note that this is not using
		/// <see cref="System.IO.Path.GetInvalidFileNameChars"/> because we do want to allow directory
		/// separators, arrow keys etc.
		/// </summary>
		private static char [] badChars = new []
		{
			'"','<','>','|','*','?',
		};


		/// <summary>
		/// The UI selected <see cref="AllowedType"/> from combo box. May be null.
		/// </summary>
		private AllowedType currentFilter;

		private bool pushingState = false;

		private FileDialogState state;

		private TextFieldWithAppendAutocomplete tbPath;

		private FileDialogSorter sorter;
		private FileDialogHistory history;

		private DataTable dtFiles;
		private TableView tableView;
		private TreeView<object> treeView;
		private SplitContainer splitContainer;
		private Button btnOk;
		private Button btnToggleSplitterCollapse;
		private Label lblForward;
		private Label lblBack;
		private Label lblUp;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDialog2"/> class.
		/// </summary>
		public FileDialog2 ()
		{
			const int okWidth = 6;

			var lblPath = new Label (">");
			this.btnOk = new Button ("Ok") {
				X = Pos.AnchorEnd (okWidth)
			};
			this.btnOk.Clicked += this.Accept;
			this.btnOk.KeyPress += (k) => {
				this.NavigateIf (k, Key.CursorLeft, this.tbPath);
				this.NavigateIf (k, Key.CursorDown, this.tableView);
			};

			this.lblUp = new Label (Driver.UpArrow.ToString ()) { X = 0, Y = 1 };
			this.lblUp.Clicked += () => this.history.Up ();

			this.lblBack = new Label (Driver.LeftArrow.ToString ()) { X = 2, Y = 1 };
			this.lblBack.Clicked += () => this.history.Back ();

			this.lblForward = new Label (Driver.RightArrow.ToString ()) { X = 3, Y = 1 };
			this.lblForward.Clicked += () => this.history.Forward ();
			this.tbPath = new TextFieldWithAppendAutocomplete {
				X = Pos.Right (lblPath),
				Width = Dim.Fill (okWidth + 1)
			};
			this.tbPath.KeyPress += (k) => {

				this.NavigateIf (k, Key.CursorDown, this.tableView);

				if (this.tbPath.CursorIsAtEnd ()) {
					this.NavigateIf (k, Key.CursorRight, this.btnOk);
				}

				this.AcceptIf (k, Key.Enter);

				this.SuppressIfBadChar (k);
			};

			this.splitContainer = new SplitContainer () {
				X = 0,
				Y = 2,
				Width = Dim.Fill (0),
				Height = Dim.Fill (1),
				SplitterDistance = 30,
			};
			this.splitContainer.Border.BorderStyle = BorderStyle.None;
			this.splitContainer.Border.DrawMarginFrame = false;
			this.splitContainer.Panels [0].Visible = false;

			this.tableView = new TableView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				FullRowSelect = true,
			};
			this.tableView.KeyPress += (k) => {
				if (this.tableView.SelectedRow <= 0) {
					this.NavigateIf (k, Key.CursorUp, this.tbPath);
				}

			};

			this.treeView = new TreeView<object> () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			this.treeView.TreeBuilder = new FileDialogTreeBuilder ();
			this.treeView.AspectGetter = (m) => m is DirectoryInfo d ? d.Name : m.ToString ();


			try {
				this.treeView.AddObjects (
					Environment.GetLogicalDrives ()
					.Select (d =>
						new FileDialogRootTreeNode (d, new DirectoryInfo (d))));

			} catch (Exception) {
				// Cannot get the system disks thats fine
			}


			this.treeView.AddObjects (
				Enum.GetValues (typeof (SpecialFolder))
				.Cast<SpecialFolder> ()
				.Where (this.IsValidSpecialFolder)
				.Select (this.GetTreeNode));

			this.treeView.SelectionChanged += this.TreeView_SelectionChanged;

			this.splitContainer.Panels [0].Add (this.treeView);
			this.splitContainer.Panels [1].Add (this.tableView);

			this.btnToggleSplitterCollapse = new Button (">>") {
				Y = Pos.AnchorEnd (1),
			};
			this.btnToggleSplitterCollapse.Clicked += () => {
				var newState = !this.splitContainer.Panels [0].Visible;
				this.splitContainer.Panels [0].Visible = newState;
				this.btnToggleSplitterCollapse.Text = newState ? "<<" : ">>";
			};

			this.tableView.Style.ShowHorizontalHeaderOverline = false;
			this.tableView.Style.ShowVerticalCellLines = false;
			this.tableView.Style.ShowVerticalHeaderLines = false;
			this.tableView.Style.AlwaysShowHeaders = true;


			this.SetupColorSchemes ();

			this.SetupTableColumns ();

			this.sorter = new FileDialogSorter (this, this.tableView);
			this.history = new FileDialogHistory (this);

			this.tableView.Table = this.dtFiles;

			this.tbPath.TextChanged += (s) => this.PathChanged ();

			this.tableView.CellActivated += this.CellActivate;
			this.tableView.KeyUp += (k) => k.Handled = this.TableView_KeyUp (k.KeyEvent);
			this.tableView.SelectedCellChanged += this.TableView_SelectedCellChanged;
			this.tableView.ColorScheme = ColorSchemeDefault;
			
			this.tableView.AddKeyBinding (Key.Home, Command.TopHome);
			this.tableView.AddKeyBinding (Key.End, Command.BottomEnd);
			this.tableView.AddKeyBinding (Key.Home | Key.ShiftMask, Command.TopHomeExtend);
			this.tableView.AddKeyBinding (Key.End | Key.ShiftMask, Command.BottomEndExtend);


			this.treeView.ColorScheme = ColorSchemeDefault;
			this.treeView.KeyDown += (k) => k.Handled = this.TreeView_KeyDown (k.KeyEvent);

			this.AllowsMultipleSelection = false;

			this.UpdateNavigationVisibility ();

			// Determines tab order
			this.Add (this.btnOk);
			this.Add (this.lblUp);
			this.Add (this.lblBack);
			this.Add (this.lblForward);
			this.Add (lblPath);
			this.Add (this.tbPath);
			this.Add (this.splitContainer);
			this.Add (this.btnToggleSplitterCollapse);
		}

		/// <summary>
		/// Gets or sets a value indicating whether to use Utc dates for date modified.
		/// Defaults to <see langword="false"/>.
		/// </summary>
		public static bool UseUtcDates { get; set; } = false;

		/// <summary>
		/// Sets a <see cref="ColorScheme"/> to use for directories rows of
		/// the <see cref="TableView"/>.
		/// </summary>
		public static ColorScheme ColorSchemeDirectory { private get; set; }

		/// <summary>
		/// Sets a <see cref="ColorScheme"/> to use for regular file rows of
		/// the <see cref="TableView"/>.  Defaults to White text on Black background.
		/// </summary>
		public static ColorScheme ColorSchemeDefault { private get; set; }

		/// <summary>
		/// Sets a <see cref="ColorScheme"/> to use for file rows with an image extension
		/// of the <see cref="TableView"/>.  Defaults to White text on Black background.
		/// </summary>
		public static ColorScheme ColorSchemeImage { private get; set; }

		/// <summary>
		/// Sets a <see cref="ColorScheme"/> to use for file rows with an executable extension
		/// or that match <see cref="AllowedTypes"/> in the <see cref="TableView"/>.
		/// </summary>
		public static ColorScheme ColorSchemeExeOrRecommended { private get; set; }

		/// <summary>
		/// Gets or Sets which <see cref="System.IO.FileSystemInfo"/> type can be selected.
		/// Defaults to <see cref="OpenMode.Mixed"/> (i.e. <see cref="DirectoryInfo"/> or
		/// <see cref="FileInfo"/>).
		/// </summary>
		public OpenMode OpenMode { get; set; } = OpenMode.Mixed;

		/// <summary>
		/// Gets or Sets the selected path in the dialog.  This is the result that should
		/// be used if <see cref="AllowsMultipleSelection"/> is off and <see cref="Canceled"/>
		/// is true.
		/// </summary>
		public string Path { get => this.tbPath.Text.ToString (); set => this.tbPath.Text = value; }

		/// <summary>
		/// Gets or Sets a value indicating whether to allow selecting 
		/// multiple existing files/directories.
		/// </summary>
		public bool AllowsMultipleSelection {
			get => this.tableView.MultiSelect;
			set => this.tableView.MultiSelect = value;
		}

		/// <summary>
		/// Gets or Sets a collection of file types that the user can/must select.  Only applies
		/// when <see cref="OpenMode"/> is <see cref="OpenMode.File"/>.  See also
		/// <see cref="AllowedTypesIsStrict"/> if you only want to highlight files.
		/// </summary>
		public List<AllowedType> AllowedTypes { get; set; } = new List<AllowedType> ();

		/// <summary>
		/// Gets or sets a value indicating whether <see cref="AllowedTypes"/> is a strict
		/// requirement or simply a recommendation. Defaults to <see langword="true"/> (i.e.
		/// strict).
		/// </summary>
		public bool AllowedTypesIsStrict { get; set; }

		/// <summary>
		/// Gets a value indicating whether the <see cref="FileDialog"/> was closed
		/// without confirming a selection.
		/// </summary>
		public bool Canceled { get; private set; } = true;

		/// <summary>
		/// Gets all files/directories selected or an empty collection
		/// <see cref="AllowsMultipleSelection"/> is <see langword="false"/> or <see cref="Canceled"/>.
		/// </summary>
		/// <remarks>If selecting only a single file/directory then you should use <see cref="Path"/> instead.</remarks>
		public IReadOnlyList<FileSystemInfo> MultiSelected { get; private set; }


		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			this.Move (1, 0, false);

			if (Title == null || string.IsNullOrEmpty (Title.ToString())) {
				return;
			}

			var padding = ((bounds.Width - this.Title.Sum (c => Rune.ColumnWidth (c))) / 2) - 1;

			padding = Math.Min (bounds.Width, padding);
			padding = Math.Max (0, padding);

			Driver.SetAttribute (
			    new Attribute (this.ColorScheme.Normal.Foreground, this.ColorScheme.Normal.Background));

			Driver.AddStr (ustring.Make (Enumerable.Repeat (Driver.HDLine, padding)));

			Driver.SetAttribute (
			    new Attribute (this.ColorScheme.Normal.Foreground, this.ColorScheme.Normal.Background));
			Driver.AddStr (this.Title);

			Driver.SetAttribute (
			    new Attribute (this.ColorScheme.Normal.Foreground, this.ColorScheme.Normal.Background));
			Driver.AddStr (ustring.Make (Enumerable.Repeat (Driver.HDLine, padding)));
		}

		/// <inheritdoc/>
		public override void OnLoaded ()
		{
			base.OnLoaded ();

			// if filtering on file type is configured then create the ComboBox and establish
			// initial filtering by extension(s)
			if (this.AllowedTypes.Any ()) {

				this.currentFilter = this.AllowedTypes [0];
				var allowed = this.AllowedTypes.ToList ();

				if (!this.AllowedTypesIsStrict) {
					allowed.Insert (0, AllowedType.Any);
				}

				// +2 to allow space for dropdown arrow
				var width = this.AllowedTypes.Max (a => a.ToString ().Length) + 2;

				var combo = new ComboBox (allowed) {
					Width = width,
					ReadOnly = true,
					Y = 1,
					X = Pos.AnchorEnd (width),
					Height = allowed.Count + 1,
					SelectedItem = this.AllowedTypesIsStrict ? 0 : 1
				};

				combo.SelectedItemChanged += (e) => this.Combo_SelectedItemChanged (combo, e);

				this.Add (combo);
				this.LayoutSubviews ();
			}

			// if no path has been provided
			if (this.tbPath.Text.Length <= 0) {
				this.tbPath.Text = Environment.CurrentDirectory;
			}

			// to streamline user experience and allow direct typing of paths
			// with zero navigation we start with focus in the text box and any
			// default/current path fully selected and ready to be overwritten
			this.tbPath.FocusFirst ();
			this.tbPath.SelectAll ();

			if (Title == null || Title == string.Empty) {
				switch (OpenMode) {
				case OpenMode.File:
					this.Title = " OPEN FILE ";
					break;
				case OpenMode.Directory:
					this.Title = " OPEN DIRECTORY ";
					break;
				case OpenMode.Mixed:
					this.Title = " OPEN ";
					break;
				}
			}
		}

		private void SuppressIfBadChar (KeyEventEventArgs k)
		{
			// don't let user type bad letters
			var ch = (char)k.KeyEvent.KeyValue;

			if (badChars.Contains (ch)) {
				k.Handled = true;
			}
		}

		private bool TreeView_KeyDown (KeyEvent keyEvent)
		{
			if (this.treeView.HasFocus && separators.Contains ((char)keyEvent.KeyValue)) {
				this.tbPath.FocusFirst ();

				// let that keystroke go through on the tbPath instead
				return true;
			}

			return false;
		}

		private void AcceptIf (KeyEventEventArgs keyEvent, Key isKey)
		{
			if (!keyEvent.Handled && keyEvent.KeyEvent.Key == isKey) {
				keyEvent.Handled = true;
				this.Accept ();
			}
		}

		private void Accept (IEnumerable<FileSystemInfoStats> toMultiAccept)
		{
			if (!this.AllowsMultipleSelection) {
				return;
			}

			this.MultiSelected = toMultiAccept.Select (s => s.FileSystemInfo).ToList ().AsReadOnly ();
			this.tbPath.Text = this.MultiSelected.Count == 1 ? this.MultiSelected [0].FullName : string.Empty;
			this.Canceled = false;
			Application.RequestStop ();
		}
		private void Accept (FileInfo f)
		{
			if (!this.IsCompatibleWithOpenMode (f)) {
				return;
			}

			this.tbPath.Text = f.FullName;
			this.Canceled = false;
			Application.RequestStop ();
		}

		private void Accept ()
		{
			// if an autocomplete is showing
			if (this.tbPath.AcceptSelectionIfAny ()) {

				// enter just accepts it
				return;
			}

			if (!this.IsCompatibleWithOpenMode (this.tbPath.Text.ToString ())) {
				return;
			}


			this.Canceled = false;
			Application.RequestStop ();
		}

		private void NavigateIf (KeyEventEventArgs keyEvent, Key isKey, View to)
		{
			if (!keyEvent.Handled && keyEvent.KeyEvent.Key == isKey) {

				to.FocusFirst ();
				keyEvent.Handled = true;
			}
		}

		private void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<object> e)
		{
			if (e.NewValue == null) {
				return;
			}

			this.tbPath.Text = FileDialogTreeBuilder.NodeToDirectory (e.NewValue).FullName;
		}

		private bool IsValidSpecialFolder (SpecialFolder arg)
		{
			try {
				var path = Environment.GetFolderPath (arg);
				return !string.IsNullOrWhiteSpace (path) && Directory.Exists (path);
			} catch (Exception) {

				return false;
			}
		}
		private FileDialogRootTreeNode GetTreeNode (SpecialFolder arg)
		{
			return new FileDialogRootTreeNode (
				arg.ToString (),
				new DirectoryInfo (Environment.GetFolderPath (arg)));
		}

		private void UpdateNavigationVisibility ()
		{
			this.lblBack.Visible = this.history.CanBack ();
			this.lblForward.Visible = this.history.CanForward ();
			this.lblUp.Visible = this.history.CanUp ();
		}

		private void TableView_SelectedCellChanged (TableView.SelectedCellChangedEventArgs obj)
		{
			if (!this.tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows.Count == 0) {
				return;
			}

			if (this.tableView.MultiSelect && this.tableView.MultiSelectedRegions.Any ()) {
				return;
			}

			var stats = this.RowToStats (obj.NewRow);

			if (stats == null || stats.IsParent) {
				return;
			}

			try {
				this.pushingState = true;

				this.tbPath.SetTextTo (stats.FileSystemInfo);
				this.state.Selected = stats;
				this.tbPath.ClearSuggestions ();

			} finally {

				this.pushingState = false;
			}
		}


		private void Combo_SelectedItemChanged (ComboBox combo, ListViewItemEventArgs obj)
		{
			var allow = combo.Source.ToList () [obj.Item] as AllowedType;
			this.currentFilter = allow == null || allow.IsAny ? null : allow;

			this.tbPath.ClearAllSelection ();
			this.tbPath.ClearSuggestions ();

			if (this.state != null) {
				this.state.RefreshChildren (this);
				this.WriteStateToTableView ();
			}
		}

		private bool TableView_KeyUp (KeyEvent keyEvent)
		{
			if (keyEvent.Key == Key.Backspace) {
				return this.history.Back ();
			}
			if (keyEvent.Key == (Key.ShiftMask | Key.Backspace)) {
				return this.history.Forward ();
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
				Focus = Driver.MakeAttribute (Color.Black, Color.White),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.White),
			};
			ColorSchemeImage = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Magenta, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Magenta, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Magenta),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Magenta),
			};
			ColorSchemeExeOrRecommended = new ColorScheme {
				Normal = Driver.MakeAttribute (Color.Green, Color.Black),
				HotNormal = Driver.MakeAttribute (Color.Green, Color.Black),
				Focus = Driver.MakeAttribute (Color.Black, Color.Green),
				HotFocus = Driver.MakeAttribute (Color.Black, Color.Green),
			};
		}

		private void SetupTableColumns ()
		{
			this.dtFiles = new DataTable ();

			var nameStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (HeaderFilename, typeof (int)));
			nameStyle.RepresentationGetter = (i) => this.state?.Children [(int)i].Name ?? string.Empty;
			nameStyle.MinWidth = 50;

			var sizeStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (HeaderSize, typeof (int)));
			sizeStyle.RepresentationGetter = (i) => this.state?.Children [(int)i].HumanReadableLength ?? string.Empty;
			nameStyle.MinWidth = 10;

			var dateModifiedStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (HeaderModified, typeof (int)));
			dateModifiedStyle.RepresentationGetter = (i) => this.state?.Children [(int)i].DateModified?.ToString () ?? string.Empty;
			dateModifiedStyle.MinWidth = 30;

			var typeStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (HeaderType, typeof (int)));
			typeStyle.RepresentationGetter = (i) => this.state?.Children [(int)i].Type ?? string.Empty;
			typeStyle.MinWidth = 6;
			this.tableView.Style.RowColorGetter = this.ColorGetter;
		}

		private void CellActivate (TableView.CellActivatedEventArgs obj)
		{
			var multi = this.MultiRowToStats ();
			if (multi.Any ()) {
				if (multi.All (this.IsCompatibleWithOpenMode)) {
					this.Accept (multi);
				} else {
					return;
				}
			}


			var stats = this.RowToStats (obj.Row);


			if (stats.FileSystemInfo is DirectoryInfo d) {
				this.PushState (d, true);
				return;
			}

			if (stats.FileSystemInfo is FileInfo f) {
				this.Accept (f);
			}
		}

		private bool IsCompatibleWithOpenMode (FileSystemInfoStats arg)
		{
			// don't let the user select .. thats just going to be confusing
			if (arg.IsParent) {
				return false;
			}

			switch (this.OpenMode) {
			case OpenMode.Directory: return arg.IsDir ();
			case OpenMode.File: return !arg.IsDir () && this.IsCompatibleWithOpenMode (arg.FileSystemInfo);
			case OpenMode.Mixed: return true;
			default: throw new ArgumentOutOfRangeException (nameof (this.OpenMode));
			}
		}

		private bool IsCompatibleWithOpenMode (FileSystemInfo f)
		{
			switch (this.OpenMode) {
			case OpenMode.Directory: return f is DirectoryInfo;
			case OpenMode.File:
				if (f is FileInfo file) {
					return this.IsCompatibleWithAllowedExtensions (file);
				}

				return false;
			case OpenMode.Mixed: return true;
			default: throw new ArgumentOutOfRangeException (nameof (this.OpenMode));
			}
		}

		private bool IsCompatibleWithAllowedExtensions (FileInfo file)
		{
			// no restrictions
			if (!this.AllowedTypes.Any () || !this.AllowedTypesIsStrict) {
				return true;
			}
			return this.MatchesAllowedTypes (file);
		}

		private bool IsCompatibleWithAllowedExtensions (string path)
		{
			// no restrictions
			if (!this.AllowedTypes.Any () || !this.AllowedTypesIsStrict) {
				return true;
			}

			var extension = System.IO.Path.GetExtension (path);

			// There is a requirement to have a particular extension and we have none
			if (string.IsNullOrEmpty (extension)) {
				return false;
			}

			return this.AllowedTypes.Any (t => t.Matches (extension, false));
		}

		/// <summary>
		/// Returns true if any <see cref="AllowedTypes"/> matches <paramref name="file"/>
		/// regardless of <see cref="AllowedTypesIsStrict"/> status.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private bool MatchesAllowedTypes (FileInfo file)
		{
			return this.AllowedTypes.Any (t => t.Matches (file.Extension, true));
		}

		private bool IsCompatibleWithOpenMode (string s)
		{
			if (string.IsNullOrWhiteSpace (s)) {
				return false;
			}

			if (!this.IsCompatibleWithAllowedExtensions (s)) {
				return false;
			}

			switch (this.OpenMode) {
			case OpenMode.Directory: return !File.Exists (s);
			case OpenMode.File: return !Directory.Exists (s);
			case OpenMode.Mixed: return true;
			default: throw new ArgumentOutOfRangeException (nameof (this.OpenMode));
			}
		}

		private void PushState (DirectoryInfo d, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true)
		{
			// no change of state
			if (d == this.state?.Directory) {
				return;
			}
			if (d.FullName == this.state?.Directory.FullName) {
				return;
			}

			try {
				this.pushingState = true;

				// push the old state to history
				if (addCurrentStateToHistory) {
					this.history.Push (this.state, clearForward);
				}

				this.tbPath.ClearSuggestions ();

				if (setPathText) {
					this.tbPath.Text = d.FullName;
					this.tbPath.MoveCursorToEnd ();
				}

				this.state = new FileDialogState (d, this);
				this.tbPath.GenerateSuggestions (this.state);

				this.WriteStateToTableView ();

				if (clearForward) {
					this.history.ClearForward ();
				}

				this.tableView.RowOffset = 0;
				this.tableView.SelectedRow = 0;

				this.SetNeedsDisplay ();
				this.UpdateNavigationVisibility ();

			} finally {

				this.pushingState = false;
			}
		}

		private void WriteStateToTableView ()
		{
			if (this.state == null) {
				return;
			}

			this.dtFiles.Rows.Clear ();

			for (int i = 0; i < this.state.Children.Length; i++) {
				this.BuildRow (i);
			}

			this.sorter.ApplySort ();
			this.tableView.Update ();

		}
		private void BuildRow (int idx)
		{
			this.tableView.Table.Rows.Add (idx, idx, idx, idx);
		}

		private ColorScheme ColorGetter (TableView.RowColorGetterArgs args)
		{
			var stats = this.RowToStats (args.RowIndex);

			if (stats.IsDir ()) {
				return ColorSchemeDirectory;
			}
			if (stats.IsImage ()) {
				return ColorSchemeImage;
			}
			if (stats.IsExecutable ()) {
				return ColorSchemeExeOrRecommended;
			}
			if (stats.FileSystemInfo is FileInfo f && this.MatchesAllowedTypes (f)) {
				return ColorSchemeExeOrRecommended;
			}

			return ColorSchemeDefault;
		}

		/// <summary>
		/// If <see cref="TableView.MultiSelect"/> is on and multiple rows are selected
		/// this returns a union of all <see cref="FileSystemInfoStats"/> in the selection.
		/// </summary>
		/// <remarks>Returns an empty collection if there are not at least 2 rows in the selection</remarks>
		/// <returns></returns>
		private IEnumerable<FileSystemInfoStats> MultiRowToStats ()
		{
			var toReturn = new HashSet<FileSystemInfoStats> ();

			if (this.AllowsMultipleSelection && this.tableView.MultiSelectedRegions.Any ()) {

				foreach (var p in this.tableView.GetAllSelectedCells ()) {

					var add = this.state?.Children [(int)this.tableView.Table.Rows [p.Y] [0]];
					if (add != null) {
						toReturn.Add (add);
					}
				}
			}

			return toReturn.Count > 1 ? toReturn : Enumerable.Empty<FileSystemInfoStats> ();
		}
		private FileSystemInfoStats RowToStats (int rowIndex)
		{
			return this.state?.Children [(int)this.tableView.Table.Rows [rowIndex] [0]];
		}
		private int? StatsToRow (FileSystemInfoStats stats)
		{
			// find array index of the current state for the stats
			var idx = state?.Children.IndexOf ((f) => f.FileSystemInfo.FullName == stats.FileSystemInfo.FullName);

			if (idx != -1 && idx != null) {

				// find the row number in our DataTable where the cell
				// contains idx
				var match = tableView.Table.Rows
					.Cast<DataRow> ()
					.Select ((r, rIdx) => new { row = r, rowIdx = rIdx })
					.Where (t => (int)t.row [0] == idx)
					.ToArray ();

				if (match.Length == 1) {
					return match [0].rowIdx;
				}
			}

			return null;
		}


		private void PathChanged ()
		{
			// avoid re-entry
			if (this.pushingState) {
				return;
			}

			var path = this.tbPath.Text?.ToString ();

			if (string.IsNullOrWhiteSpace (path)) {
				this.SetupAsClear ();
				return;
			}

			var dir = this.StringToDirectoryInfo (path);

			if (dir.Exists) {
				this.PushState (dir, true);
			} else
			if (dir.Parent?.Exists ?? false) {
				this.PushState (dir.Parent, true, false);
			}
		}

		private DirectoryInfo StringToDirectoryInfo (string path)
		{
			// if you pass new DirectoryInfo("C:") you get a weird object
			// where the FullName is in fact the current working directory.
			// really not what most users would expect

			if (Regex.IsMatch (path, "^\\w:$")) {
				return new DirectoryInfo (path + System.IO.Path.DirectorySeparatorChar);
			}

			return new DirectoryInfo (path);
		}

		private void SetupAsDirectory (DirectoryInfo dir)
		{
			// TODO: Scrap this method
			this.PushState (dir, true);
		}

		private void SetupAsClear ()
		{

		}

		/// <summary>
		/// Describes a requirement on what <see cref="FileInfo"/> can be selected
		/// in a <see cref="FileDialog2"/>.
		/// </summary>
		public class AllowedType {

			/// <summary>
			/// Initializes a new instance of the <see cref="AllowedType"/> class.
			/// </summary>
			/// <param name="description">The human readable text to display.</param>
			/// <param name="extensions">Extension(s) to match e.g. .csv.</param>
			public AllowedType (string description, params string [] extensions)
			{
				if (extensions.Length == 0) {
					throw new ArgumentException ("You must supply at least one extension");
				}

				this.Description = description;
				this.Extensions = extensions;
			}

			/// <summary>
			/// Gets a value of <see cref="AllowedType"/> that matches any file.
			/// </summary>
			public static AllowedType Any { get; } = new AllowedType ("Any Files", ".*");

			/// <summary>
			/// Gets or Sets the human readable description for the file type
			/// e.g. "Comma Separated Values".
			/// </summary>
			public string Description { get; set; }

			/// <summary>
			/// Gets or Sets the permitted file extension(s) (e.g. ".csv").
			/// </summary>
			public string [] Extensions { get; set; }

			/// <summary>
			/// Gets a value indicating whether this instance is the
			/// static <see cref="Any"/> value which indicates matching
			/// any files.
			/// </summary>
			public bool IsAny => this == Any;

			/// <summary>
			/// Returns <see cref="Description"/> plus all <see cref="Extensions"/> separated by semicolons.
			/// </summary>
			public override string ToString ()
			{
				return $"{this.Description} ({string.Join (";", this.Extensions.Select (e => '*' + e).ToArray ())})";
			}

			internal bool Matches (string extension, bool strict)
			{
				if (this.IsAny) {
					return !strict;
				}

				return this.Extensions.Any (e => e.Equals (extension));
			}
		}

		/// <summary>
		/// Wrapper for <see cref="FileSystemInfo"/> that contains additional information
		/// (e.g. <see cref="IsParent"/>) and helper methods.
		/// </summary>
		internal class FileSystemInfoStats {


			/* ---- Colors used by the ls command line tool ----
			 *
			* Blue: Directory
			* Green: Executable or recognized data file
			* Cyan (Sky Blue): Symbolic link file
			* Yellow with black background: Device
			* Magenta (Pink): Graphic image file
			* Red: Archive file
			* Red with black background: Broken link
			*/

			private const long ByteConversion = 1024;

			private static readonly string [] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
			private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
			private static readonly List<string> ExecutableExtensions = new List<string> { ".EXE", ".BAT" };

			/// <summary>
			/// Initializes a new instance of the <see cref="FileSystemInfoStats"/> class.
			/// </summary>
			/// <param name="fsi">The directory of path to wrap.</param>
			public FileSystemInfoStats (FileSystemInfo fsi)
			{
				this.FileSystemInfo = fsi;

				if (fsi is FileInfo fi) {
					this.MachineReadableLength = fi.Length;
					this.HumanReadableLength = GetHumanReadableFileSize (this.MachineReadableLength);
					this.DateModified = FileDialog2.UseUtcDates ? File.GetLastWriteTimeUtc (fi.FullName) : File.GetLastWriteTime (fi.FullName);
					this.Type = fi.Extension;
				} else {
					this.HumanReadableLength = string.Empty;
					this.Type = "dir";
				}
			}

			/// <summary>
			/// Gets the wrapped <see cref="FileSystemInfo"/> (directory or file).
			/// </summary>
			public FileSystemInfo FileSystemInfo { get; }
			public string HumanReadableLength { get; }
			public long MachineReadableLength { get; }
			public DateTime? DateModified { get; }
			public string Type { get; }

			/// <summary>
			/// Gets or Sets a value indicating whether this instance represents
			/// the parent of the current state (i.e. "..").
			/// </summary>
			public bool IsParent { get; internal set; }
			public string Name => this.IsParent ? ".." : this.FileSystemInfo.Name;

			public bool IsDir ()
			{
				return this.Type == "dir";
			}

			public bool IsImage ()
			{
				return this.FileSystemInfo is FileSystemInfo f &&
					ImageExtensions.Contains (
						f.Extension,
						StringComparer.InvariantCultureIgnoreCase);
			}

			public bool IsExecutable ()
			{
				// TODO: handle linux executable status
				return this.FileSystemInfo is FileSystemInfo f &&
					ExecutableExtensions.Contains (
						f.Extension,
						StringComparer.InvariantCultureIgnoreCase);
			}

			internal object GetOrderByValue (string columnName)
			{
				switch (columnName) {
				case HeaderFilename: return this.FileSystemInfo.Name;
				case HeaderSize: return this.MachineReadableLength;
				case HeaderModified: return this.DateModified;
				case HeaderType: return this.Type;
				default: throw new ArgumentOutOfRangeException (nameof (columnName));
				}
			}

			internal object GetOrderByDefault ()
			{
				if (this.IsDir ()) {
					return -1;
				}

				return 100;
			}

			private static string GetHumanReadableFileSize (long value)
			{

				if (value < 0) {
					return "-" + GetHumanReadableFileSize (-value);
				}

				if (value == 0) {
					return "0.0 bytes";
				}

				int mag = (int)Math.Log (value, ByteConversion);
				double adjustedSize = value / Math.Pow (1000, mag);


				return string.Format ("{0:n2} {1}", adjustedSize, SizeSuffixes [mag]);
			}
		}

		internal class FileDialogState {

			public FileSystemInfoStats Selected { get; set; }
			public FileDialogState (DirectoryInfo dir, FileDialog2 parent)
			{
				this.Directory = dir;

				this.RefreshChildren (parent);
			}

			public DirectoryInfo Directory { get; }

			public FileSystemInfoStats [] Children { get; private set; }

			internal void RefreshChildren (FileDialog2 parent)
			{
				var dir = this.Directory;

				try {
					List<FileSystemInfoStats> children;

					// if directories only
					if (parent.OpenMode == OpenMode.Directory) {
						children = dir.GetDirectories ().Select (e => new FileSystemInfoStats (e)).ToList ();
					} else {
						children = dir.GetFileSystemInfos ().Select (e => new FileSystemInfoStats (e)).ToList ();
					}

					// if only allowing specific file types
					if (parent.AllowedTypes.Any () && parent.AllowedTypesIsStrict && parent.OpenMode == OpenMode.File) {

						children = children.Where (
							c => c.IsDir () ||
							(c.FileSystemInfo is FileInfo f && parent.IsCompatibleWithAllowedExtensions (f)))
							.ToList ();
					}

					// if theres a UI filter in place too
					if (parent.currentFilter != null) {
						children = children.Where (
								c => c.IsDir () ||
								(c.FileSystemInfo is FileInfo f && parent.currentFilter.Matches (f.Extension, true))
								).ToList ();
					}


					// allow navigating up as '..'
					if (dir.Parent != null) {
						children.Add (new FileSystemInfoStats (dir.Parent) { IsParent = true });
					}

					this.Children = children.ToArray ();
				} catch (Exception) {
					// Access permissions Exceptions, Dir not exists etc
					this.Children = new FileSystemInfoStats [0];
				}
			}

			internal void SetSelection (FileSystemInfoStats stats)
			{

			}
		}

		internal class TextFieldWithAppendAutocomplete : TextField {

			private int? currentFragment = null;
			private string [] validFragments = new string [0];

			public TextFieldWithAppendAutocomplete ()
			{
				this.KeyPress += (k) => {
					var key = k.KeyEvent.Key;
					if (key == Key.Tab) {
						k.Handled = this.AcceptSelectionIfAny ();
					} else
					if (key == Key.CursorUp) {
						k.Handled = this.CycleSuggestion (1);
					} else
					if (key == Key.CursorDown) {
						k.Handled = this.CycleSuggestion (-1);
					}
				};

				this.ColorScheme = new ColorScheme {
					Normal = new Attribute (Color.White, Color.Black),
					HotNormal = new Attribute (Color.White, Color.Black),
					Focus = new Attribute (Color.White, Color.Black),
					HotFocus = new Attribute (Color.White, Color.Black),
				};
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				if (!this.MakingSuggestion ()) {
					return;
				}

				// draw it like its selected even though its not
				Driver.SetAttribute (new Attribute (Color.Black, Color.White));
				this.Move (this.Text.Length, 0);
				Driver.AddStr (this.validFragments [this.currentFragment.Value]);
			}

			/// <summary>
			/// Accepts the current autocomplete suggestion displaying in the text box.
			/// Returns true if a valid suggestion was being rendered and acceptable or
			/// false if no suggestion was showing.
			/// </summary>
			/// <returns></returns>
			internal bool AcceptSelectionIfAny ()
			{
				if (this.MakingSuggestion ()) {
					this.Text += this.validFragments [this.currentFragment.Value];
					this.MoveCursorToEnd ();

					this.ClearSuggestions ();
					return true;
				}

				return false;
			}

			internal void MoveCursorToEnd ()
			{
				this.ClearAllSelection ();
				this.CursorPosition = this.Text.Length;
			}

			internal void GenerateSuggestions (FileDialogState state, params string [] suggestions)
			{
				if (!this.CursorIsAtEnd ()) {
					return;
				}

				var path = this.Text.ToString ();
				var last = path.LastIndexOfAny (FileDialog2.separators);

				if (last == -1 || suggestions.Length == 0 || last >= path.Length - 1) {
					this.currentFragment = null;
					return;
				}

				var term = path.Substring (last + 1);

				if (term.Equals (state?.Directory?.Name)) {
					this.ClearSuggestions ();
					return;
				}

				// TODO: Be case insensitive on Windows
				var validSuggestions = suggestions
					.Where (s => s.StartsWith (term))
					.OrderBy (m => m.Length)
					.ToArray ();


				// nothing to suggest
				if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length) {
					this.ClearSuggestions ();
					return;
				}

				this.validFragments = validSuggestions.Select (f => f.Substring (term.Length)).ToArray ();
				this.currentFragment = 0;
			}

			internal void ClearSuggestions ()
			{
				this.currentFragment = null;
				this.validFragments = new string [0];
				this.SetNeedsDisplay ();
			}

			internal void GenerateSuggestions (FileDialogState state)
			{
				if (state == null) {
					return;
				}

				var suggestions = state.Children.Select (
					e => e.FileSystemInfo is DirectoryInfo d
						? d.Name + System.IO.Path.DirectorySeparatorChar
						: e.FileSystemInfo.Name)
					.ToArray ();

				this.GenerateSuggestions (state, suggestions);
			}

			internal void SetTextTo (FileSystemInfo fileSystemInfo)
			{
				var newText = fileSystemInfo.FullName;
				if (fileSystemInfo is DirectoryInfo) {
					newText += System.IO.Path.DirectorySeparatorChar;
				}
				this.Text = newText;
				this.MoveCursorToEnd ();
			}

			internal bool CursorIsAtEnd ()
			{
				return this.CursorPosition == this.Text.Length;
			}

			/// <summary>
			/// Returns true if there is a suggestion that can be made and the control
			/// is in a state where user would expect to see auto-complete (i.e. focused and
			/// cursor in right place).
			/// </summary>
			/// <returns></returns>
			private bool MakingSuggestion ()
			{
				return this.currentFragment != null && this.HasFocus && this.CursorIsAtEnd ();
			}

			private bool CycleSuggestion (int direction)
			{
				if (this.currentFragment == null || this.validFragments.Length <= 1) {
					return false;
				}

				this.currentFragment = (this.currentFragment + direction) % this.validFragments.Length;

				if (this.currentFragment < 0) {
					this.currentFragment = this.validFragments.Length - 1;
				}
				this.SetNeedsDisplay ();
				return true;
			}
		}

		internal class FileDialogHistory {
			private Stack<FileDialogState> back = new Stack<FileDialogState> ();
			private Stack<FileDialogState> forward = new Stack<FileDialogState> ();
			private FileDialog2 dlg;

			public FileDialogHistory (FileDialog2 dlg)
			{
				this.dlg = dlg;
			}

			public bool Back ()
			{

				DirectoryInfo goTo = null;
				FileSystemInfoStats restoreSelection = null;

				if (this.CanBack ()) {

					var backTo = this.back.Pop ();
					goTo = backTo.Directory;
					restoreSelection = backTo.Selected;
				} else if (this.CanUp ()) {
					goTo = this.dlg.state?.Directory.Parent;
				}

				// nowhere to go
				if (goTo == null) {
					return false;
				}

				this.forward.Push (this.dlg.state);
				this.dlg.PushState (goTo, false, true, false);

				if (restoreSelection != null) {
					this.dlg.RestoreSelection (restoreSelection);
				}

				return true;
			}

			internal bool CanBack ()
			{
				return this.back.Count > 0;
			}

			internal bool Forward ()
			{
				if (this.forward.Count > 0) {

					this.dlg.PushState (this.forward.Pop ().Directory, true, true, false);
					return true;
				}

				return false;
			}

			internal bool Up ()
			{
				var parent = this.dlg.state?.Directory.Parent;
				if (parent != null) {

					this.back.Push (new FileDialogState (parent, this.dlg));
					this.dlg.PushState (parent, false);
					return true;
				}

				return false;
			}

			internal bool CanUp ()
			{
				return this.dlg.state?.Directory.Parent != null;
			}


			internal void Push (FileDialogState state, bool clearForward)
			{
				if (state == null) {
					return;
				}

				// if changing to a new directory push onto the Back history
				if (this.back.Count == 0 || this.back.Peek ().Directory.FullName != state.Directory.FullName) {

					this.back.Push (state);
					if (clearForward) {
						this.ClearForward ();
					}
				}
			}

			internal bool CanForward ()
			{
				return this.forward.Count > 0;
			}

			internal void ClearForward ()
			{
				this.forward.Clear ();
			}
		}

		private void RestoreSelection (FileSystemInfoStats toRestore)
		{
			var toReselect = StatsToRow (toRestore);

			if (toReselect.HasValue) {
				tableView.SelectedRow = toReselect.Value;
			}
		}

		private class FileDialogSorter {
			private readonly FileDialog2 dlg;
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
							this.SortColumn (clickedCol);
						} else if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

							// right click in a header
							this.ShowHeaderContextMenu (clickedCol, e);
						}
					}
				};

			}

			internal void ApplySort ()
			{
				var col = this.currentSort;

				// TODO: Consider preserving selection
				this.tableView.Table.Rows.Clear ();

				var colName = col == null ? null : StripArrows (col.ColumnName);

				var stats = this.dlg.state?.Children ?? new FileSystemInfoStats [0];

				// Do we sort on a column or just use the default sort order?
				Func<FileSystemInfoStats, object> sortAlgorithm;

				if (colName == null) {
					sortAlgorithm = (v) => v.GetOrderByDefault ();
					this.currentSortIsAsc = true;
				} else {
					sortAlgorithm = (v) => v.GetOrderByValue (colName);
				}

				var ordered =
					this.currentSortIsAsc ?
					    stats.Select ((v, i) => new { v, i })
						.OrderByDescending (f => f.v.IsParent)
						.ThenBy (f => sortAlgorithm (f.v))
						.ToArray () :
					    stats.Select ((v, i) => new { v, i })
						.OrderByDescending (f => f.v.IsParent)
						.ThenByDescending (f => sortAlgorithm (f.v))
						.ToArray ();

				foreach (var o in ordered) {
					this.dlg.BuildRow (o.i);
				}

				foreach (DataColumn c in this.tableView.Table.Columns) {

					// remove any lingering sort indicator
					c.ColumnName = TrimArrows (c.ColumnName);

					// add a new one if this the one that is being sorted
					if (c == col) {
						c.ColumnName += this.currentSortIsAsc ? '▲' : '▼';
					}
				}

				this.tableView.Update ();
			}

			private static string TrimArrows (string columnName)
			{
				return columnName.TrimEnd ('▼', '▲');
			}

			private static string StripArrows (string columnName)
			{
				return columnName.Replace ("▼", string.Empty).Replace ("▲", string.Empty);
			}

			private void SortColumn (DataColumn clickedCol)
			{
				this.GetProposedNewSortOrder (clickedCol, out var isAsc);
				this.SortColumn (clickedCol, isAsc);
			}

			private void SortColumn (DataColumn col, bool isAsc)
			{
				// set a sort order
				this.currentSort = col;
				this.currentSortIsAsc = isAsc;

				this.ApplySort ();
			}

			private string GetProposedNewSortOrder (DataColumn clickedCol, out bool isAsc)
			{
				// work out new sort order
				if (this.currentSort == clickedCol && this.currentSortIsAsc) {
					isAsc = false;
					return $"{clickedCol.ColumnName} DESC";
				} else {
					isAsc = true;
					return $"{clickedCol.ColumnName} ASC";
				}
			}

			private void ShowHeaderContextMenu (DataColumn clickedCol, View.MouseEventArgs e)
			{
				var sort = this.GetProposedNewSortOrder (clickedCol, out var isAsc);

				var contextMenu = new ContextMenu (
					e.MouseEvent.X + 1,
					e.MouseEvent.Y + 1,
					new MenuBarItem (new MenuItem []
					{
						new MenuItem($"Hide {TrimArrows(clickedCol.ColumnName)}", string.Empty, () => this.HideColumn(clickedCol)),
						new MenuItem($"Sort {StripArrows(sort)}",string.Empty, ()=> this.SortColumn(clickedCol,isAsc)),
					})
				);

				contextMenu.Show ();
			}

			private void HideColumn (DataColumn clickedCol)
			{
				var style = this.tableView.Style.GetOrCreateColumnStyle (clickedCol);
				style.Visible = false;
				this.tableView.Update ();
			}
		}


		private class FileDialogRootTreeNode {
			public FileDialogRootTreeNode (string displayName, DirectoryInfo path)
			{
				this.DisplayName = displayName;
				this.Path = path;
			}

			public string DisplayName { get; set; }
			public DirectoryInfo Path { get; set; }

			public override string ToString ()
			{
				return this.DisplayName;
			}
		}

		private class FileDialogTreeBuilder : ITreeBuilder<object> {
			public bool SupportsCanExpand => true;

			public bool CanExpand (object toExpand)
			{
				return this.TryGetDirectories (NodeToDirectory (toExpand)).Any ();
			}

			public IEnumerable<object> GetChildren (object forObject)
			{
				return this.TryGetDirectories (NodeToDirectory (forObject));
			}

			internal static DirectoryInfo NodeToDirectory (object toExpand)
			{
				return toExpand is FileDialogRootTreeNode f ? f.Path : (DirectoryInfo)toExpand;
			}

			private IEnumerable<DirectoryInfo> TryGetDirectories (DirectoryInfo directoryInfo)
			{
				try {
					return directoryInfo.EnumerateDirectories ();
				} catch (Exception) {

					return Enumerable.Empty<DirectoryInfo> ();
				}
			}

		}
	}
}