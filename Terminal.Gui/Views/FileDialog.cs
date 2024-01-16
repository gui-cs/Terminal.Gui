using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>
/// Modal dialog for selecting files/directories. Has auto-complete and expandable
/// navigation pane (Recent, Root drives etc).
/// </summary>
public class FileDialog : Dialog {
	/// <summary>
	/// Gets the Path separators for the operating system
	/// </summary>
	internal static char [] Separators = {
		System.IO.Path.AltDirectorySeparatorChar,
		System.IO.Path.DirectorySeparatorChar
	};

	/// <summary>
	/// Characters to prevent entry into <see cref="tbPath"/>. Note that this is not using
	/// <see cref="System.IO.Path.GetInvalidFileNameChars"/> because we do want to allow directory
	/// separators, arrow keys etc.
	/// </summary>
	static readonly char [] badChars = {
		'"', '<', '>', '|', '*', '?'
	};

	Dictionary<IDirectoryInfo, string> _treeRoots = new ();
	MenuBarItem allowedTypeMenu;
	MenuBar allowedTypeMenuBar;
	MenuItem [] allowedTypeMenuItems;
	readonly Button btnBack;
	readonly Button btnCancel;
	readonly Button btnForward;
	readonly Button btnOk;
	readonly Button btnToggleSplitterCollapse;
	readonly Button btnUp;

	int currentSortColumn;

	bool currentSortIsAsc = true;

	bool disposed;
	string feedback;
	readonly IFileSystem fileSystem;

	readonly FileDialogHistory history;
	bool loaded;

	/// <summary>
	/// Locking object for ensuring only a single <see cref="SearchState"/> executes at once.
	/// </summary>
	internal object onlyOneSearchLock = new ();

	bool pushingState;
	readonly SpinnerView spinnerView;
	readonly TileView splitContainer;

	readonly TableView tableView;
	readonly TextField tbFind;
	readonly TextField tbPath;
	readonly TreeView<IFileSystemInfo> treeView;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileDialog"/> class.
	/// </summary>
	public FileDialog () : this (new FileSystem ()) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="FileDialog"/> class with
	/// a custom <see cref="IFileSystem"/>.
	/// </summary>
	/// <remarks>This overload is mainly useful for testing.</remarks>
	public FileDialog (IFileSystem fileSystem)
	{
		this.fileSystem = fileSystem;
		Style = new FileDialogStyle (fileSystem);

		btnOk = new Button (Style.OkButtonText) {
			Y = Pos.AnchorEnd (1),
			X = Pos.Function (CalculateOkButtonPosX),
			IsDefault = true
		};
		btnOk.Clicked += (s, e) => Accept (true);
		btnOk.KeyDown += (s, k) => {
			NavigateIf (k, KeyCode.CursorLeft, btnCancel);
			NavigateIf (k, KeyCode.CursorUp, tableView);
		};

		btnCancel = new Button (Strings.btnCancel) {
			Y = Pos.AnchorEnd (1),
			X = Pos.Right (btnOk) + 1
		};
		btnCancel.KeyDown += (s, k) => {
			NavigateIf (k, KeyCode.CursorLeft, btnToggleSplitterCollapse);
			NavigateIf (k, KeyCode.CursorUp, tableView);
			NavigateIf (k, KeyCode.CursorRight, btnOk);
		};
		btnCancel.Clicked += (s, e) => {
			Application.RequestStop ();
		};

		btnUp = new Button { X = 0, Y = 1, NoPadding = true };
		btnUp.Text = GetUpButtonText ();
		btnUp.Clicked += (s, e) => history.Up ();

		btnBack = new Button { X = Pos.Right (btnUp) + 1, Y = 1, NoPadding = true };
		btnBack.Text = GetBackButtonText ();
		btnBack.Clicked += (s, e) => history.Back ();

		btnForward = new Button { X = Pos.Right (btnBack) + 1, Y = 1, NoPadding = true };
		btnForward.Text = GetForwardButtonText ();
		btnForward.Clicked += (s, e) => history.Forward ();

		tbPath = new TextField {
			Width = Dim.Fill (),
			CaptionColor = new Color (Color.Black)
		};
		tbPath.KeyDown += (s, k) => {

			ClearFeedback ();

			AcceptIf (k, KeyCode.Enter);

			SuppressIfBadChar (k);
		};

		tbPath.Autocomplete = new AppendAutocomplete (tbPath);
		tbPath.Autocomplete.SuggestionGenerator = new FilepathSuggestionGenerator ();

		splitContainer = new TileView {
			X = 0,
			Y = 2,
			Width = Dim.Fill (),
			Height = Dim.Fill (1)
		};

		Initialized += (s, e) => {
			splitContainer.SetSplitterPos (0, 30);
			splitContainer.Tiles.ElementAt (0).ContentView.Visible = false;
		};
		//			this.splitContainer.Border.BorderStyle = BorderStyle.None;

		tableView = new TableView {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			FullRowSelect = true,
			CollectionNavigator = new FileDialogCollectionNavigator (this)
		};
		tableView.KeyBindings.Add (KeyCode.Space, Command.ToggleChecked);
		tableView.MouseClick += OnTableViewMouseClick;
		tableView.Style.InvertSelectedCellFirstCharacter = true;
		Style.TableStyle = tableView.Style;

		var nameStyle = Style.TableStyle.GetOrCreateColumnStyle (0);
		nameStyle.MinWidth = 10;
		nameStyle.ColorGetter = ColorGetter;

		var sizeStyle = Style.TableStyle.GetOrCreateColumnStyle (1);
		sizeStyle.MinWidth = 10;
		sizeStyle.ColorGetter = ColorGetter;

		var dateModifiedStyle = Style.TableStyle.GetOrCreateColumnStyle (2);
		dateModifiedStyle.MinWidth = 30;
		dateModifiedStyle.ColorGetter = ColorGetter;

		var typeStyle = Style.TableStyle.GetOrCreateColumnStyle (3);
		typeStyle.MinWidth = 6;
		typeStyle.ColorGetter = ColorGetter;

		tableView.KeyDown += (s, k) => {
			if (tableView.SelectedRow <= 0) {
				NavigateIf (k, KeyCode.CursorUp, tbPath);
			}
			if (tableView.SelectedRow == tableView.Table.Rows - 1) {
				NavigateIf (k, KeyCode.CursorDown, btnToggleSplitterCollapse);
			}

			if (splitContainer.Tiles.First ().ContentView.Visible && tableView.SelectedColumn == 0) {
				NavigateIf (k, KeyCode.CursorLeft, treeView);
			}

			if (k.Handled) { }
		};

		treeView = new TreeView<IFileSystemInfo> {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};

		var fileDialogTreeBuilder = new FileSystemTreeBuilder ();
		treeView.TreeBuilder = fileDialogTreeBuilder;
		treeView.AspectGetter = AspectGetter;
		Style.TreeStyle = treeView.Style;

		treeView.SelectionChanged += TreeView_SelectionChanged;

		splitContainer.Tiles.ElementAt (0).ContentView.Add (treeView);
		splitContainer.Tiles.ElementAt (1).ContentView.Add (tableView);

		btnToggleSplitterCollapse = new Button (GetToggleSplitterText (false)) {
			Y = Pos.AnchorEnd (1)
		};
		btnToggleSplitterCollapse.Clicked += (s, e) => {
			var tile = splitContainer.Tiles.ElementAt (0);

			var newState = !tile.ContentView.Visible;
			tile.ContentView.Visible = newState;
			btnToggleSplitterCollapse.Text = GetToggleSplitterText (newState);
			LayoutSubviews ();
		};

		tbFind = new TextField {
			X = Pos.Right (btnToggleSplitterCollapse) + 1,
			CaptionColor = new Color (Color.Black),
			Width = 30,
			Y = Pos.AnchorEnd (1),
			HotKey = KeyCode.F | KeyCode.AltMask
		};
		spinnerView = new SpinnerView {
			X = Pos.Right (tbFind) + 1,
			Y = Pos.AnchorEnd (1),
			Visible = false
		};

		tbFind.TextChanged += (s, o) => RestartSearch ();
		tbFind.KeyDown += (s, o) => {
			if (o.KeyCode == KeyCode.Enter) {
				RestartSearch ();
				o.Handled = true;
			}

			if (o.KeyCode == KeyCode.Esc) {
				if (CancelSearch ()) {
					o.Handled = true;
				}
			}
			if (tbFind.CursorIsAtEnd ()) {
				NavigateIf (o, KeyCode.CursorRight, btnCancel);
			}
			if (tbFind.CursorIsAtStart ()) {
				NavigateIf (o, KeyCode.CursorLeft, btnToggleSplitterCollapse);
			}
		};

		tableView.Style.ShowHorizontalHeaderOverline = true;
		tableView.Style.ShowVerticalCellLines = true;
		tableView.Style.ShowVerticalHeaderLines = true;
		tableView.Style.AlwaysShowHeaders = true;
		tableView.Style.ShowHorizontalHeaderUnderline = true;
		tableView.Style.ShowHorizontalScrollIndicators = true;

		history = new FileDialogHistory (this);

		tbPath.TextChanged += (s, e) => PathChanged ();

		tableView.CellActivated += CellActivate;
		tableView.KeyUp += (s, k) => k.Handled = TableView_KeyUp (k);
		tableView.SelectedCellChanged += TableView_SelectedCellChanged;

		tableView.KeyBindings.Add (KeyCode.Home, Command.TopHome);
		tableView.KeyBindings.Add (KeyCode.End, Command.BottomEnd);
		tableView.KeyBindings.Add (KeyCode.Home | KeyCode.ShiftMask, Command.TopHomeExtend);
		tableView.KeyBindings.Add (KeyCode.End | KeyCode.ShiftMask, Command.BottomEndExtend);

		treeView.KeyDown += (s, k) => {

			var selected = treeView.SelectedObject;
			if (selected != null) {
				if (!treeView.CanExpand (selected) || treeView.IsExpanded (selected)) {
					NavigateIf (k, KeyCode.CursorRight, tableView);
				} else if (treeView.GetObjectRow (selected) == 0) {
					NavigateIf (k, KeyCode.CursorUp, tbPath);
				}
			}

			if (k.Handled) {
				return;
			}

			k.Handled = TreeView_KeyDown (k);

		};

		AllowsMultipleSelection = false;

		UpdateNavigationVisibility ();

		// Determines tab order
		Add (btnToggleSplitterCollapse);
		Add (tbFind);
		Add (spinnerView);
		Add (btnOk);
		Add (btnCancel);
		Add (btnUp);
		Add (btnBack);
		Add (btnForward);
		Add (tbPath);
		Add (splitContainer);
	}

	/// <summary>
	/// Gets settings for controlling how visual elements behave.  Style changes should
	/// be made before the <see cref="Dialog"/> is loaded and shown to the user for the
	/// first time.
	/// </summary>
	public FileDialogStyle Style { get; }

	/// <summary>
	/// The maximum number of results that will be collected
	/// when searching before stopping.
	/// </summary>
	/// <remarks>
	/// This prevents performance issues e.g. when searching
	/// root of file system for a common letter (e.g. 'e').
	/// </remarks>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
	public static int MaxSearchResults { get; set; } = 10000;

	/// <summary>
	/// True if the file/folder must exist already to be selected.
	/// This prevents user from entering the name of something that
	/// doesn't exist. Defaults to false.
	/// </summary>
	public bool MustExist { get; set; }

	/// <summary>
	/// The UI selected <see cref="IAllowedType"/> from combo box. May be null.
	/// </summary>
	public IAllowedType CurrentFilter { get; private set; }

	/// <summary>
	/// Gets the currently open directory and known children presented in the dialog.
	/// </summary>
	internal FileDialogState State { get; private set; }

	/// <summary>
	/// Gets or sets behavior of the <see cref="FileDialog"/> when the user attempts
	/// to delete a selected file(s).  Set to null to prevent deleting.
	/// </summary>
	/// <remarks>
	/// Ensure you use a try/catch block with appropriate
	/// error handling (e.g. showing a <see cref="MessageBox"/>
	/// </remarks>
	public IFileOperations FileOperationsHandler { get; set; } = new DefaultFileOperations ();

	/// <summary>
	/// Gets or Sets which <see cref="System.IO.FileSystemInfo"/> type can be selected.
	/// Defaults to <see cref="OpenMode.Mixed"/> (i.e. <see cref="DirectoryInfo"/> or
	/// <see cref="FileInfo"/>).
	/// </summary>
	public OpenMode OpenMode { get; set; } = OpenMode.Mixed;

	/// <summary>
	/// Gets or Sets the selected path in the dialog. This is the result that should
	/// be used if <see cref="AllowsMultipleSelection"/> is off and <see cref="Canceled"/>
	/// is true.
	/// </summary>
	public string Path {
		get => tbPath.Text;
		set {
			tbPath.Text = value;
			tbPath.MoveEnd ();
		}
	}

	/// <summary>
	/// Defines how the dialog matches files/folders when using the search
	/// box. Provide a custom implementation if you want to tailor how matching
	/// is performed.
	/// </summary>
	public ISearchMatcher SearchMatcher { get; set; } = new DefaultSearchMatcher ();

	/// <summary>
	/// Gets or Sets a value indicating whether to allow selecting
	/// multiple existing files/directories. Defaults to false.
	/// </summary>
	public bool AllowsMultipleSelection {
		get => tableView.MultiSelect;
		set => tableView.MultiSelect = value;
	}

	/// <summary>
	/// Gets or Sets a collection of file types that the user can/must select. Only applies
	/// when <see cref="OpenMode"/> is <see cref="OpenMode.File"/> or <see cref="OpenMode.Mixed"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="AllowedTypeAny"/> adds the option to select any type (*.*). If this
	/// collection is empty then any type is supported and no Types drop-down is shown.
	/// </remarks>
	public List<IAllowedType> AllowedTypes { get; set; } = new ();

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
	public IReadOnlyList<string> MultiSelected { get; private set; }
		= Enumerable.Empty<string> ().ToList ().AsReadOnly ();

	/// <summary>
	/// Event fired when user attempts to confirm a selection (or multi selection).
	/// Allows you to cancel the selection or undertake alternative behavior e.g.
	/// open a dialog "File already exists, Overwrite? yes/no".
	/// </summary>
	public event EventHandler<FilesSelectedEventArgs> FilesSelected;

	int CalculateOkButtonPosX ()
	{
		if (!IsInitialized || !btnOk.IsInitialized || !btnCancel.IsInitialized) {
			return 0;
		}
		return Bounds.Width -
		       btnOk.Bounds.Width -
		       btnCancel.Bounds.Width -
		       1
		       // TODO: Fiddle factor, seems the Bounds are wrong for someone
		       -
		       2;
	}

	string AspectGetter (object o)
	{
		var fsi = (IFileSystemInfo)o;

		if (o is IDirectoryInfo dir && _treeRoots.ContainsKey (dir)) {

			// Directory has a special name e.g. 'Pictures'
			return _treeRoots [dir];
		}

		return (Style.IconProvider.GetIconWithOptionalSpace (fsi) + fsi.Name).Trim ();
	}

	void OnTableViewMouseClick (object sender, MouseEventEventArgs e)
	{
		var clickedCell = tableView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out var clickedCol);

		if (clickedCol != null) {
			if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {

				// left click in a header
				SortColumn (clickedCol.Value);
			} else if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

				// right click in a header
				ShowHeaderContextMenu (clickedCol.Value, e);
			}
		} else {
			if (clickedCell != null && e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

				// right click in rest of table
				ShowCellContextMenu (clickedCell, e);
			}
		}
	}

	string GetForwardButtonText () => "-" + Glyphs.RightArrow;

	string GetBackButtonText () => Glyphs.LeftArrow + "-";

	string GetUpButtonText () => Style.UseUnicodeCharacters ? "◭" : "▲";

	string GetToggleSplitterText (bool isExpanded) => isExpanded ?
		new string ((char)Glyphs.LeftArrow.Value, 2) :
		new string ((char)Glyphs.RightArrow.Value, 2);

	void Delete ()
	{
		var toDelete = GetFocusedFiles ();

		if (toDelete != null && FileOperationsHandler.Delete (toDelete)) {
			RefreshState ();
		}
	}

	void Rename ()
	{
		var toRename = GetFocusedFiles ();

		if (toRename?.Length == 1) {
			var newNamed = FileOperationsHandler.Rename (fileSystem, toRename.Single ());

			if (newNamed != null) {
				RefreshState ();
				RestoreSelection (newNamed);
			}
		}
	}

	void New ()
	{
		if (State != null) {
			var created = FileOperationsHandler.New (fileSystem, State.Directory);
			if (created != null) {
				RefreshState ();
				RestoreSelection (created);
			}
		}
	}

	IFileSystemInfo [] GetFocusedFiles ()
	{

		if (!tableView.HasFocus || !tableView.CanFocus || FileOperationsHandler == null) {
			return null;
		}

		tableView.EnsureValidSelection ();

		if (tableView.SelectedRow < 0) {
			return null;
		}

		return tableView.GetAllSelectedCells ()
				.Select (c => c.Y)
				.Distinct ()
				.Select (RowToStats)
				.Where (s => !s.IsParent)
				.Select (d => d.FileSystemInfo)
				.ToArray ();
	}


	//		/// <inheritdoc/>
	//		public override bool OnHotKey (KeyEventArgs keyEvent)
	//		{
	//#if BROKE_IN_2927
	//			// BUGBUG: Ctrl-F is forward in a TextField. 
	//			if (this.NavigateIf (keyEvent, Key.Alt | Key.F, this.tbFind)) {
	//				return true;
	//			}
	//#endif

	//			ClearFeedback ();

	//			if (allowedTypeMenuBar != null &&
	//				keyEvent.ConsoleDriverKey == Key.Tab &&
	//				allowedTypeMenuBar.IsMenuOpen) {
	//				allowedTypeMenuBar.CloseMenu (false, false, false);
	//			}

	//			return base.OnHotKey (keyEvent);
	//		}
	void RestartSearch ()
	{
		if (disposed || State?.Directory == null) {
			return;
		}

		if (State is SearchState oldSearch) {
			oldSearch.Cancel ();
		}

		// user is clearing search terms
		if (tbFind.Text == null || tbFind.Text.Length == 0) {

			// Wait for search cancellation (if any) to finish
			// then push the current dir state
			lock (onlyOneSearchLock) {
				PushState (new FileDialogState (State.Directory, this), false);
			}
			return;
		}

		PushState (new SearchState (State?.Directory, this, tbFind.Text), true);
	}

	/// <inheritdoc/>
	protected override void Dispose (bool disposing)
	{
		disposed = true;
		base.Dispose (disposing);

		CancelSearch ();
	}

	bool CancelSearch ()
	{
		if (State is SearchState search) {
			return search.Cancel ();
		}

		return false;
	}

	void ClearFeedback () => feedback = null;

	/// <inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		base.OnDrawContent (contentArea);

		if (!string.IsNullOrWhiteSpace (feedback)) {
			var feedbackWidth = feedback.EnumerateRunes ().Sum (c => c.GetColumns ());
			var feedbackPadLeft = (Bounds.Width - feedbackWidth) / 2 - 1;

			feedbackPadLeft = Math.Min (Bounds.Width, feedbackPadLeft);
			feedbackPadLeft = Math.Max (0, feedbackPadLeft);

			var feedbackPadRight = Bounds.Width - (feedbackPadLeft + feedbackWidth + 2);
			feedbackPadRight = Math.Min (Bounds.Width, feedbackPadRight);
			feedbackPadRight = Math.Max (0, feedbackPadRight);

			Move (0, Bounds.Height / 2);

			Driver.SetAttribute (new Attribute (Color.Red, ColorScheme.Normal.Background));
			Driver.AddStr (new string (' ', feedbackPadLeft));
			Driver.AddStr (feedback);
			Driver.AddStr (new string (' ', feedbackPadRight));
		}
	}

	/// <inheritdoc/>
	public override void OnLoaded ()
	{
		base.OnLoaded ();
		if (loaded) {
			return;
		}
		loaded = true;

		// May have been updated after instance was constructed
		btnOk.Text = Style.OkButtonText;
		btnCancel.Text = Style.CancelButtonText;
		btnUp.Text = GetUpButtonText ();
		btnBack.Text = GetBackButtonText ();
		btnForward.Text = GetForwardButtonText ();
		btnToggleSplitterCollapse.Text = GetToggleSplitterText (false);

		if (Style.FlipOkCancelButtonLayoutOrder) {
			btnCancel.X = Pos.Function (CalculateOkButtonPosX);
			btnOk.X = Pos.Right (btnCancel) + 1;


			// Flip tab order too for consistency
			var p1 = btnOk.TabIndex;
			var p2 = btnCancel.TabIndex;

			btnOk.TabIndex = p2;
			btnCancel.TabIndex = p1;
		}

		tbPath.Caption = Style.PathCaption;
		tbFind.Caption = Style.SearchCaption;

		tbPath.Autocomplete.ColorScheme = new ColorScheme (tbPath.ColorScheme) {
			Normal = new Attribute (Color.Black, tbPath.ColorScheme.Normal.Background)
		};

		_treeRoots = Style.TreeRootGetter ();
		Style.IconProvider.IsOpenGetter = treeView.IsExpanded;

		treeView.AddObjects (_treeRoots.Keys);

		// if filtering on file type is configured then create the ComboBox and establish
		// initial filtering by extension(s)
		if (AllowedTypes.Any ()) {

			CurrentFilter = AllowedTypes [0];

			// Fiddle factor
			var width = AllowedTypes.Max (a => a.ToString ().Length) + 6;

			allowedTypeMenu = new MenuBarItem ("<placeholder>",
				allowedTypeMenuItems = AllowedTypes.Select (
									   (a, i) => new MenuItem (a.ToString (), null, () => {
										   AllowedTypeMenuClicked (i);
									   }))
								   .ToArray ());

			allowedTypeMenuBar = new MenuBar (new [] { allowedTypeMenu }) {
				Width = width,
				Y = 1,
				X = Pos.AnchorEnd (width),

				// TODO: Does not work, if this worked then we could tab to it instead
				// of having to hit F9
				CanFocus = true,
				TabStop = true
			};
			AllowedTypeMenuClicked (0);

			allowedTypeMenuBar.Enter += (s, e) => {
				allowedTypeMenuBar.OpenMenu (0);
			};

			allowedTypeMenuBar.DrawContentComplete += (s, e) => {

				allowedTypeMenuBar.Move (e.Rect.Width - 1, 0);
				Driver.AddRune (Glyphs.DownArrow);

			};

			Add (allowedTypeMenuBar);
		}

		// if no path has been provided
		if (tbPath.Text.Length <= 0) {
			Path = Environment.CurrentDirectory;
		}

		// to streamline user experience and allow direct typing of paths
		// with zero navigation we start with focus in the text box and any
		// default/current path fully selected and ready to be overwritten
		tbPath.FocusFirst ();
		tbPath.SelectAll ();

		if (string.IsNullOrEmpty (Title)) {
			Title = GetDefaultTitle ();
		}
		LayoutSubviews ();
	}

	/// <summary>
	/// Gets a default dialog title, when <see cref="View.Title"/> is not set or empty,
	/// result of the function will be shown.
	/// </summary>
	protected virtual string GetDefaultTitle ()
	{
		List<string> titleParts = new () {
			Strings.fdOpen
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

	void AllowedTypeMenuClicked (int idx)
	{

		var allow = AllowedTypes [idx];
		for (var i = 0; i < AllowedTypes.Count; i++) {
			allowedTypeMenuItems [i].Checked = i == idx;
		}
		allowedTypeMenu.Title = allow.ToString ();

		CurrentFilter = allow;

		tbPath.ClearAllSelection ();
		tbPath.Autocomplete.ClearSuggestions ();

		if (State != null) {
			State.RefreshChildren ();
			WriteStateToTableView ();
		}
	}

	void SuppressIfBadChar (Key k)
	{
		// don't let user type bad letters
		var ch = (char)k;

		if (badChars.Contains (ch)) {
			k.Handled = true;
		}
	}

	bool TreeView_KeyDown (Key keyEvent)
	{
		if (treeView.HasFocus && Separators.Contains ((char)keyEvent)) {
			tbPath.FocusFirst ();

			// let that keystroke go through on the tbPath instead
			return true;
		}

		return false;
	}

	void AcceptIf (Key keyEvent, KeyCode isKey)
	{
		if (!keyEvent.Handled && keyEvent.KeyCode == isKey) {
			keyEvent.Handled = true;

			// User hit Enter in text box so probably wants the
			// contents of the text box as their selection not
			// whatever lingering selection is in TableView
			Accept (false);
		}
	}

	void Accept (IEnumerable<FileSystemInfoStats> toMultiAccept)
	{
		if (!AllowsMultipleSelection) {
			return;
		}

		// Don't include ".." (IsParent) in multiselections
		MultiSelected = toMultiAccept
				.Where (s => !s.IsParent)
				.Select (s => s.FileSystemInfo.FullName)
				.ToList ().AsReadOnly ();

		Path = MultiSelected.Count == 1 ? MultiSelected [0] : string.Empty;

		FinishAccept ();
	}

	void Accept (IFileInfo f)
	{
		if (!IsCompatibleWithOpenMode (f.FullName, out var reason)) {
			feedback = reason;
			SetNeedsDisplay ();
			return;
		}

		Path = f.FullName;

		if (AllowsMultipleSelection) {
			MultiSelected = new List<string> { f.FullName }.AsReadOnly ();
		}

		FinishAccept ();
	}

	void Accept (bool allowMulti)
	{
		if (allowMulti && TryAcceptMulti ()) {
			return;
		}

		if (!IsCompatibleWithOpenMode (tbPath.Text, out var reason)) {
			if (reason != null) {
				feedback = reason;
				SetNeedsDisplay ();
			}
			return;
		}

		FinishAccept ();
	}

	void FinishAccept ()
	{
		var e = new FilesSelectedEventArgs (this);

		FilesSelected?.Invoke (this, e);

		if (e.Cancel) {
			return;
		}

		// if user uses Path selection mode (e.g. Enter in text box)
		// then also copy to MultiSelected
		if (AllowsMultipleSelection && !MultiSelected.Any ()) {

			MultiSelected = string.IsNullOrWhiteSpace (Path) ?
				Enumerable.Empty<string> ().ToList ().AsReadOnly () :
				new List<string> { Path }.AsReadOnly ();
		}

		Canceled = false;
		Application.RequestStop ();
	}

	bool NavigateIf (Key keyEvent, KeyCode isKey, View to)
	{
		if (keyEvent.KeyCode == isKey) {

			to.FocusFirst ();
			if (to == tbPath) {
				tbPath.MoveEnd ();
			}
			return true;
		}

		return false;
	}

	void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<IFileSystemInfo> e)
	{
		if (e.NewValue == null) {
			return;
		}

		Path = e.NewValue.FullName;
	}

	void UpdateNavigationVisibility ()
	{
		btnBack.Visible = history.CanBack ();
		btnForward.Visible = history.CanForward ();
		btnUp.Visible = history.CanUp ();
	}

	void TableView_SelectedCellChanged (object sender, SelectedCellChangedEventArgs obj)
	{
		if (!tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows == 0) {
			return;
		}

		if (tableView.MultiSelect && tableView.MultiSelectedRegions.Any ()) {
			return;
		}

		var stats = RowToStats (obj.NewRow);

		if (stats == null) {
			return;
		}
		IFileSystemInfo dest;

		if (stats.IsParent) {
			dest = State.Directory;
		} else {
			dest = stats.FileSystemInfo;
		}

		try {
			pushingState = true;

			Path = dest.FullName;
			State.Selected = stats;
			tbPath.Autocomplete.ClearSuggestions ();

		} finally {

			pushingState = false;
		}
	}

	bool TableView_KeyUp (Key keyEvent)
	{
		if (keyEvent.KeyCode == KeyCode.Backspace) {
			return history.Back ();
		}
		if (keyEvent.KeyCode == (KeyCode.ShiftMask | KeyCode.Backspace)) {
			return history.Forward ();
		}

		if (keyEvent.KeyCode == KeyCode.Delete) {

			Delete ();
			return true;
		}

		if (keyEvent.KeyCode == (KeyCode.CtrlMask | KeyCode.R)) {

			Rename ();
			return true;
		}

		if (keyEvent.KeyCode == (KeyCode.CtrlMask | KeyCode.N)) {
			New ();
			return true;
		}

		return false;
	}

	void CellActivate (object sender, CellActivatedEventArgs obj)
	{
		if (TryAcceptMulti ()) {
			return;
		}

		var stats = RowToStats (obj.Row);

		if (stats.FileSystemInfo is IDirectoryInfo d) {
			PushState (d, true);
			return;
		}

		if (stats.FileSystemInfo is IFileInfo f) {
			Accept (f);
		}
	}

	bool TryAcceptMulti ()
	{
		var multi = MultiRowToStats ();
		string reason = null;

		if (!multi.Any ()) {
			return false;
		}

		if (multi.All (m => IsCompatibleWithOpenMode (
			m.FileSystemInfo.FullName, out reason))) {
			Accept (multi);
			return true;
		}
		if (reason != null) {
			feedback = reason;
			SetNeedsDisplay ();
		}

		return false;
	}

	/// <summary>
	/// Returns true if there are no <see cref="AllowedTypes"/> or one of them agrees
	/// that <paramref name="file"/> <see cref="IAllowedType.IsAllowed(string)"/>.
	/// </summary>
	/// <param name="file"></param>
	/// <returns></returns>
	public bool IsCompatibleWithAllowedExtensions (IFileInfo file)
	{
		// no restrictions
		if (!AllowedTypes.Any ()) {
			return true;
		}
		return MatchesAllowedTypes (file);
	}

	bool IsCompatibleWithAllowedExtensions (string path)
	{
		// no restrictions
		if (!AllowedTypes.Any ()) {
			return true;
		}

		return AllowedTypes.Any (t => t.IsAllowed (path));
	}

	/// <summary>
	/// Returns true if any <see cref="AllowedTypes"/> matches <paramref name="file"/>.
	/// </summary>
	/// <param name="file"></param>
	/// <returns></returns>
	bool MatchesAllowedTypes (IFileInfo file) => AllowedTypes.Any (t => t.IsAllowed (file.FullName));

	bool IsCompatibleWithOpenMode (string s, out string reason)
	{
		reason = null;
		if (string.IsNullOrWhiteSpace (s)) {
			return false;
		}

		if (!IsCompatibleWithAllowedExtensions (s)) {
			reason = Style.WrongFileTypeFeedback;
			return false;
		}

		switch (OpenMode) {
		case OpenMode.Directory:
			if (MustExist && !Directory.Exists (s)) {
				reason = Style.DirectoryMustExistFeedback;
				return false;
			}

			if (File.Exists (s)) {
				reason = Style.FileAlreadyExistsFeedback;
				return false;
			}
			return true;
		case OpenMode.File:

			if (MustExist && !File.Exists (s)) {
				reason = Style.FileMustExistFeedback;
				return false;
			}
			if (Directory.Exists (s)) {
				reason = Style.DirectoryAlreadyExistsFeedback;
				return false;
			}
			return true;
		case OpenMode.Mixed:
			if (MustExist && !File.Exists (s) && !Directory.Exists (s)) {
				reason = Style.FileOrDirectoryMustExistFeedback;
				return false;
			}
			return true;
		default: throw new ArgumentOutOfRangeException (nameof (OpenMode));
		}
	}

	/// <summary>
	/// Changes the dialog such that <paramref name="d"/> is being explored.
	/// </summary>
	/// <param name="d"></param>
	/// <param name="addCurrentStateToHistory"></param>
	/// <param name="setPathText"></param>
	/// <param name="clearForward"></param>
	/// <param name="pathText">Optional alternate string to set path to.</param>
	internal void PushState (IDirectoryInfo d, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true, string pathText = null)
	{
		// no change of state
		if (d == State?.Directory) {
			return;
		}
		if (d.FullName == State?.Directory.FullName) {
			return;
		}

		PushState (new FileDialogState (d, this), addCurrentStateToHistory, setPathText, clearForward, pathText);
	}

	void RefreshState ()
	{
		State.RefreshChildren ();
		PushState (State, false, false, false);
	}

	void PushState (FileDialogState newState, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true, string pathText = null)
	{
		if (State is SearchState search) {
			search.Cancel ();
		}

		try {
			pushingState = true;

			// push the old state to history
			if (addCurrentStateToHistory) {
				history.Push (State, clearForward);
			}

			tbPath.Autocomplete.ClearSuggestions ();

			if (pathText != null) {
				Path = pathText;
			} else if (setPathText) {
				Path = newState.Directory.FullName;
			}

			State = newState;
			tbPath.Autocomplete.GenerateSuggestions (
				new AutocompleteFilepathContext (tbPath.Text, tbPath.CursorPosition, State));

			WriteStateToTableView ();

			if (clearForward) {
				history.ClearForward ();
			}

			tableView.RowOffset = 0;
			tableView.SelectedRow = 0;

			SetNeedsDisplay ();
			UpdateNavigationVisibility ();

		} finally {

			pushingState = false;
		}
		ClearFeedback ();
	}

	void WriteStateToTableView ()
	{
		if (State == null) {
			return;
		}
		tableView.Table = new FileDialogTableSource (this, State, Style, currentSortColumn, currentSortIsAsc);

		ApplySort ();
		tableView.Update ();
	}

	ColorScheme ColorGetter (CellColorGetterArgs args)
	{
		var stats = RowToStats (args.RowIndex);

		if (!Style.UseColors) {
			return tableView.ColorScheme;
		}


		var color = Style.ColorProvider.GetColor (stats.FileSystemInfo) ?? new Color (Color.White);
		var black = new Color (Color.Black);

		// TODO: Add some kind of cache for this
		return new ColorScheme {
			Normal = new Attribute (color, black),
			HotNormal = new Attribute (color, black),
			Focus = new Attribute (black, color),
			HotFocus = new Attribute (black, color)
		};
	}

	/// <summary>
	/// If <see cref="TableView.MultiSelect"/> is this returns a union of all
	/// <see cref="FileSystemInfoStats"/> in the selection.
	/// </summary>
	/// <returns></returns>
	IEnumerable<FileSystemInfoStats> MultiRowToStats ()
	{
		var toReturn = new HashSet<FileSystemInfoStats> ();

		if (AllowsMultipleSelection && tableView.MultiSelectedRegions.Any ()) {

			foreach (var p in tableView.GetAllSelectedCells ()) {

				var add = State?.Children [p.Y];
				if (add != null) {
					toReturn.Add (add);
				}
			}
		}

		return toReturn;
	}

	FileSystemInfoStats RowToStats (int rowIndex) => State?.Children [rowIndex];

	void PathChanged ()
	{
		// avoid re-entry
		if (pushingState) {
			return;
		}

		var path = tbPath.Text;

		if (string.IsNullOrWhiteSpace (path)) {
			return;
		}

		var dir = StringToDirectoryInfo (path);

		if (dir.Exists) {
			PushState (dir, true, false);
		} else if (dir.Parent?.Exists ?? false) {
			PushState (dir.Parent, true, false);
		}

		tbPath.Autocomplete.GenerateSuggestions (new AutocompleteFilepathContext (tbPath.Text, tbPath.CursorPosition, State));
	}

	IDirectoryInfo StringToDirectoryInfo (string path)
	{
		// if you pass new DirectoryInfo("C:") you get a weird object
		// where the FullName is in fact the current working directory.
		// really not what most users would expect
		if (Regex.IsMatch (path, "^\\w:$")) {
			return fileSystem.DirectoryInfo.New (path + System.IO.Path.DirectorySeparatorChar);
		}

		return fileSystem.DirectoryInfo.New (path);
	}

	/// <summary>
	/// Select <paramref name="toRestore"/> in the table view (if present)
	/// </summary>
	/// <param name="toRestore"></param>
	internal void RestoreSelection (IFileSystemInfo toRestore)
	{
		tableView.SelectedRow = State.Children.IndexOf (r => r.FileSystemInfo == toRestore);
		tableView.EnsureSelectedCellIsVisible ();
	}

	internal void ApplySort ()
	{
		var stats = State?.Children ?? new FileSystemInfoStats [0];

		// This portion is never reordered (aways .. at top then folders)
		var forcedOrder = stats
				  .OrderByDescending (f => f.IsParent)
				  .ThenBy (f => f.IsDir ? -1 : 100);

		// This portion is flexible based on the column clicked (e.g. alphabetical)
		var ordered =
			currentSortIsAsc ?
				forcedOrder.ThenBy (f => FileDialogTableSource.GetRawColumnValue (currentSortColumn, f)) :
				forcedOrder.ThenByDescending (f => FileDialogTableSource.GetRawColumnValue (currentSortColumn, f));

		State.Children = ordered.ToArray ();

		tableView.Update ();
	}

	void SortColumn (int clickedCol)
	{
		GetProposedNewSortOrder (clickedCol, out var isAsc);
		SortColumn (clickedCol, isAsc);
		tableView.Table = new FileDialogTableSource (this, State, Style, currentSortColumn, currentSortIsAsc);
	}

	internal void SortColumn (int col, bool isAsc)
	{
		// set a sort order
		currentSortColumn = col;
		currentSortIsAsc = isAsc;

		ApplySort ();
	}

	string GetProposedNewSortOrder (int clickedCol, out bool isAsc)
	{
		// work out new sort order
		if (currentSortColumn == clickedCol && currentSortIsAsc) {
			isAsc = false;
			return string.Format (Strings.fdCtxSortDesc, tableView.Table.ColumnNames [clickedCol]);
		}
		isAsc = true;
		return string.Format (Strings.fdCtxSortAsc, tableView.Table.ColumnNames [clickedCol]);
	}

	void ShowHeaderContextMenu (int clickedCol, MouseEventEventArgs e)
	{
		var sort = GetProposedNewSortOrder (clickedCol, out var isAsc);

		var contextMenu = new ContextMenu (
			e.MouseEvent.X + 1,
			e.MouseEvent.Y + 1,
			new MenuBarItem (new MenuItem [] {
				new (string.Format (Strings.fdCtxHide, StripArrows (tableView.Table.ColumnNames [clickedCol])), string.Empty, () => HideColumn (clickedCol)),
				new (StripArrows (sort), string.Empty, () => SortColumn (clickedCol, isAsc))
			})
		);

		contextMenu.Show ();
	}

	static string StripArrows (string columnName) => columnName.Replace (" (▼)", string.Empty).Replace (" (▲)", string.Empty);

	void ShowCellContextMenu (Point? clickedCell, MouseEventEventArgs e)
	{
		if (clickedCell == null) {
			return;
		}

		var contextMenu = new ContextMenu (
			e.MouseEvent.X + 1,
			e.MouseEvent.Y + 1,
			new MenuBarItem (new MenuItem [] {
				new (Strings.fdCtxNew, string.Empty, New),
				new (Strings.fdCtxRename, string.Empty, Rename),
				new (Strings.fdCtxDelete, string.Empty, Delete)
			})
		);

		tableView.SetSelection (clickedCell.Value.X, clickedCell.Value.Y, false);

		contextMenu.Show ();
	}

	void HideColumn (int clickedCol)
	{
		var style = tableView.Style.GetOrCreateColumnStyle (clickedCol);
		style.Visible = false;
		tableView.Update ();
	}

	/// <summary>
	/// State representing a recursive search from <see cref="FileDialogState.Directory"/>
	/// downwards.
	/// </summary>
	internal class SearchState : FileDialogState {
		bool cancel;
		bool finished;

		// TODO: Add thread safe child adding
		readonly List<FileSystemInfoStats> found = new ();
		readonly object oLockFound = new ();
		readonly CancellationTokenSource token = new ();

		public SearchState (IDirectoryInfo dir, FileDialog parent, string searchTerms) : base (dir, parent)
		{
			parent.SearchMatcher.Initialize (searchTerms);
			Children = new FileSystemInfoStats [0];
			BeginSearch ();
		}

		void BeginSearch ()
		{
			Task.Run (() => {
				RecursiveFind (Directory);
				finished = true;
			});

			Task.Run (() => {
				UpdateChildren ();
			});
		}

		void UpdateChildren ()
		{
			lock (Parent.onlyOneSearchLock) {
				while (!cancel && !finished) {

					try {
						Task.Delay (250).Wait (token.Token);
					} catch (OperationCanceledException) {
						cancel = true;
					}

					if (cancel || finished) {
						break;
					}

					UpdateChildrenToFound ();
				}

				if (finished && !cancel) {
					UpdateChildrenToFound ();
				}

				Application.Invoke (() => {
					Parent.spinnerView.Visible = false;
				});
			}
		}

		void UpdateChildrenToFound ()
		{
			lock (oLockFound) {
				Children = found.ToArray ();
			}

			Application.Invoke (() => {
				Parent.tbPath.Autocomplete.GenerateSuggestions (
					new AutocompleteFilepathContext (Parent.tbPath.Text, Parent.tbPath.CursorPosition, this)
				);
				Parent.WriteStateToTableView ();

				Parent.spinnerView.Visible = true;
				Parent.spinnerView.SetNeedsDisplay ();
			});
		}

		void RecursiveFind (IDirectoryInfo directory)
		{
			foreach (var f in GetChildren (directory)) {

				if (cancel) {
					return;
				}

				if (f.IsParent) {
					continue;
				}

				lock (oLockFound) {
					if (found.Count >= MaxSearchResults) {
						finished = true;
						return;
					}
				}

				if (Parent.SearchMatcher.IsMatch (f.FileSystemInfo)) {
					lock (oLockFound) {
						found.Add (f);
					}
				}

				if (f.FileSystemInfo is IDirectoryInfo sub) {
					RecursiveFind (sub);
				}
			}
		}

		internal override void RefreshChildren () { }

		/// <summary>
		/// Cancels the current search (if any).  Returns true if a search
		/// was running and cancellation was successfully set.
		/// </summary>
		/// <returns></returns>
		internal bool Cancel ()
		{
			var alreadyCancelled = token.IsCancellationRequested || cancel;

			cancel = true;
			token.Cancel ();

			return !alreadyCancelled;
		}
	}

	internal class FileDialogCollectionNavigator : CollectionNavigatorBase {
		readonly FileDialog fileDialog;

		public FileDialogCollectionNavigator (FileDialog fileDialog) => this.fileDialog = fileDialog;

		protected override object ElementAt (int idx)
		{
			var val = FileDialogTableSource.GetRawColumnValue (fileDialog.tableView.SelectedColumn, fileDialog.State?.Children [idx]);
			if (val == null) {
				return string.Empty;
			}

			return val.ToString ().Trim ('.');
		}

		protected override int GetCollectionLength () => fileDialog.State?.Children.Length ?? 0;
	}
}