using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NStack;
using Terminal.Gui.Resources;
using Terminal.Gui.Trees;
using static System.Environment;
using static Terminal.Gui.Configuration.ConfigurationManager;
using static Terminal.Gui.OpenDialog;

namespace Terminal.Gui {

	/// <summary>
	/// Modal dialog for selecting files/directories. Has auto-complete and expandable
	/// navigation pane (Recent, Root drives etc).
	/// </summary>
	public partial class FileDialog : Dialog {

		/// <summary>
		/// Gets settings for controlling how visual elements behave.  Style changes should
		/// be made before the <see cref="Dialog"/> is loaded and shown to the user for the
		/// first time.
		/// </summary>
		public FileDialogStyle Style { get; } = new FileDialogStyle ();

		/// <summary>
		/// Stores style settings for <see cref="FileDialog"/>.
		/// </summary>
		public class FileDialogStyle {

			/// <summary>
			/// Gets or sets the header text displayed in the Filename column of the files table.
			/// </summary>
			public string FilenameColumnName { get; set; } = Strings.fdFilename;

			/// <summary>
			/// Gets or sets the header text displayed in the Size column of the files table.
			/// </summary>
			public string SizeColumnName { get; set; } = Strings.fdSize;

			/// <summary>
			/// Gets or sets the header text displayed in the Modified column of the files table.
			/// </summary>
			public string ModifiedColumnName { get; set; } = Strings.fdModified;

			/// <summary>
			/// Gets or sets the header text displayed in the Type column of the files table.
			/// </summary>
			public string TypeColumnName { get; set; } = Strings.fdType;

			/// <summary>
			/// Gets or sets the text displayed in the 'Search' text box when user has not supplied any input yet.
			/// </summary>
			public string SearchCaption { get; internal set; } = Strings.fdSearchCaption;

			/// <summary>
			/// Gets or sets the text displayed in the 'Path' text box when user has not supplied any input yet.
			/// </summary>
			public string PathCaption { get; internal set; } = Strings.fdPathCaption;

			/// <summary>
			/// Gets or sets the text on the 'Ok' button.  Typically you may want to change this to
			/// "Open" or "Save" etc.
			/// </summary>
			public string OkButtonText { get; set; } = "Ok";

			/// <summary>
			/// Gets or sets error message when user attempts to select a file type that is not one of <see cref="AllowedTypes"/>
			/// </summary>
			public string WrongFileTypeFeedback { get; internal set; } = Strings.fdWrongFileTypeFeedback;

			/// <summary>
			/// Gets or sets error message when user selects a directory that does not exist and
			/// <see cref="OpenMode"/> is <see cref="OpenMode.Directory"/> and <see cref="MustExist"/> is <see langword="true"/>.
			/// </summary>
			public string DirectoryMustExistFeedback { get; internal set; } = Strings.fdDirectoryMustExistFeedback;

			/// <summary>
			/// Gets or sets error message when user <see cref="OpenMode"/> is <see cref="OpenMode.Directory"/>
			/// and user enters the name of an existing file (File system cannot have a folder with the same name as a file).
			/// </summary>
			public string FileAlreadyExistsFeedback { get; internal set; } = Strings.fdFileAlreadyExistsFeedback;

			/// <summary>
			/// Gets or sets error message when user selects a file that does not exist and
			/// <see cref="OpenMode"/> is <see cref="OpenMode.File"/> and <see cref="MustExist"/> is <see langword="true"/>.
			/// </summary>
			public string FileMustExistFeedback { get; internal set; } = Strings.fdFileMustExistFeedback;

			/// <summary>
			/// Gets or sets error message when user <see cref="OpenMode"/> is <see cref="OpenMode.File"/>
			/// and user enters the name of an existing directory (File system cannot have a folder with the same name as a file).
			/// </summary>
			public string DirectoryAlreadyExistsFeedback { get; internal set; } = Strings.fdDirectoryAlreadyExistsFeedback;

			/// <summary>
			/// Gets or sets error message when user selects a file/dir that does not exist and
			/// <see cref="OpenMode"/> is <see cref="OpenMode.Mixed"/> and <see cref="MustExist"/> is <see langword="true"/>.
			/// </summary>
			public string FileOrDirectoryMustExistFeedback { get; internal set; } = Strings.fdFileOrDirectoryMustExistFeedback;

			/// <summary>
			/// Gets the style settings for the table of files (in currently selected directory).
			/// </summary>
			public TableView.TableStyle TableStyle { get; internal set; }

			/// <summary>
			/// Gets the style settings for the collapse-able directory/places tree
			/// </summary>
			public TreeStyle TreeStyle { get; internal set; }

			/// <summary>
			/// Gets or Sets the method for getting the root tree objects that are displayed in
			/// the collapse-able tree in the <see cref="FileDialog"/>.  Defaults to all accessible
			/// <see cref="System.Environment.GetLogicalDrives"/> and unique
			/// <see cref="Environment.SpecialFolder"/>.
			/// </summary>
			/// <remarks>Must be configured before showing the dialog.</remarks>
			public FileDialogTreeRootGetter TreeRootGetter { get; set; } = DefaultTreeRootGetter;

			private static IEnumerable<FileDialogRootTreeNode> DefaultTreeRootGetter ()
			{
				var roots = new List<FileDialogRootTreeNode> ();
				try {
					foreach (var d in Environment.GetLogicalDrives ()) {
						roots.Add (new FileDialogRootTreeNode (d, new DirectoryInfo (d)));
					}

				} catch (Exception) {
					// Cannot get the system disks thats fine
				}


				try {
					foreach (var special in Enum.GetValues (typeof (Environment.SpecialFolder)).Cast<SpecialFolder> ()) {
						try {
							var path = Environment.GetFolderPath (special);
							if (
								!string.IsNullOrWhiteSpace (path)
								&& Directory.Exists (path)
								&& !roots.Any (r => string.Equals (r.Path.FullName, path))) {

								roots.Add (new FileDialogRootTreeNode (
								special.ToString (),
								new DirectoryInfo (Environment.GetFolderPath (special))));
							}
						} catch (Exception) {
							// Special file exists but contents are unreadable (permissions?)
							// skip it anyway
						}
					}
				} catch (Exception) {
					// Cannot get the special files for this OS oh well
				}

				return roots;
			}
		}

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

		private static char [] separators = new []
		{
			System.IO.Path.AltDirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar,
		};

		/// <summary>
		/// Characters to prevent entry into <see cref="tbPath"/>. Note that this is not using
		/// <see cref="System.IO.Path.GetInvalidFileNameChars"/> because we do want to allow directory
		/// separators, arrow keys etc.
		/// </summary>
		private static char [] badChars = new []
		{
			'"','<','>','|','*','?',
		};


		/// <summary>
		/// The UI selected <see cref="IAllowedType"/> from combo box. May be null.
		/// </summary>
		private IAllowedType currentFilter;

		private bool pushingState = false;
		private bool loaded = false;

		private FileDialogState state;
		private object onlyOneSearchLock = new object ();
		private bool disposed = false;
		private TextFieldWithAppendAutocomplete tbPath;

		private FileDialogSorter sorter;
		private FileDialogHistory history;

		private DataTable dtFiles;
		private TableView tableView;
		private TreeView<object> treeView;
		private TileView splitContainer;
		private Button btnOk;
		private Button btnCancel;
		private Button btnToggleSplitterCollapse;
		private Label lblForward;
		private Label lblBack;
		private Label lblUp;
		private string feedback;

		private CollectionNavigator collectionNavigator = new CollectionNavigator ();

		private CaptionedTextField tbFind;
		private SpinnerLabel spinnerLabel;
		private MenuBar allowedTypeMenuBar;
		private MenuBarItem allowedTypeMenu;
		private MenuItem [] allowedTypeMenuItems;

		/// <summary>
		/// Event fired when user attempts to confirm a selection (or multi selection).
		/// Allows you to cancel the selection or undertake alternative behavior e.g.
		/// open a dialog "File already exists, Overwrite? yes/no".
		/// </summary>
		public event EventHandler<FilesSelectedEventArgs> FilesSelected;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDialog"/> class.
		/// </summary>
		public FileDialog ()
		{
			var lblPath = new Label (">");
			this.btnOk = new Button (Style.OkButtonText) {
				Y = Pos.AnchorEnd (1),
				X = Pos.Function (() =>
					this.Bounds.Width
					- btnOk.Bounds.Width
					// TODO: Fiddle factor, seems the Bounds are wrong for someone
					- 2)
			};
			this.btnOk.Clicked += (s,e)=> this.Accept();
			this.btnOk.KeyPress += (s,k) => {
				this.NavigateIf (k, Key.CursorLeft, this.btnCancel);
				this.NavigateIf (k, Key.CursorUp, this.tableView);
			};

			this.btnCancel = new Button ("Cancel") {
				Y = Pos.AnchorEnd (1),
				X = Pos.Function (() =>
					this.Bounds.Width
					- btnOk.Bounds.Width
					- btnCancel.Bounds.Width
					- 1
					// TODO: Fiddle factor, seems the Bounds are wrong for someone
					- 2
					)
			};
			this.btnCancel.KeyPress += (s, k) => {
				this.NavigateIf (k, Key.CursorLeft, this.btnToggleSplitterCollapse);
				this.NavigateIf (k, Key.CursorUp, this.tableView);
				this.NavigateIf (k, Key.CursorRight, this.btnOk);
			};
			this.btnCancel.Clicked += (s,e) => {
				Application.RequestStop ();
			};

			this.lblUp = new Label (Driver.UpArrow.ToString ()) { X = 0, Y = 1 };
			this.lblUp.Clicked += (s, e) => this.history.Up ();

			this.lblBack = new Label (Driver.LeftArrow.ToString ()) { X = 2, Y = 1 };
			this.lblBack.Clicked += (s, e) => this.history.Back ();

			this.lblForward = new Label (Driver.RightArrow.ToString ()) { X = 3, Y = 1 };
			this.lblForward.Clicked += (s, e) => this.history.Forward ();
			this.tbPath = new TextFieldWithAppendAutocomplete {
				X = Pos.Right (lblPath),
				Width = Dim.Fill (1),
				Caption = Style.PathCaption,
				CaptionColor = Color.DarkGray,
			};
			this.tbPath.KeyPress += (s, k) => {
				ClearFeedback ();

				this.NavigateIf (k, Key.CursorDown, this.tableView);

				if (this.tbPath.CursorIsAtEnd ()) {
					this.NavigateIf (k, Key.CursorRight, this.btnOk);
				}

				this.AcceptIf (k, Key.Enter);

				this.SuppressIfBadChar (k);
			};

			this.splitContainer = new TileView () {
				X = 0,
				Y = 2,
				Width = Dim.Fill (0),
				Height = Dim.Fill (1),
			};
			this.splitContainer.SetSplitterPos (0, 30);
			this.splitContainer.Border.BorderStyle = BorderStyle.None;
			this.splitContainer.Tiles.ElementAt (0).ContentView.Visible = false;

			this.tableView = new TableView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				FullRowSelect = true,
			};
			this.tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);
			Style.TableStyle = tableView.Style;
			this.tableView.KeyPress += (s, k) => {
				if (this.tableView.SelectedRow <= 0) {
					this.NavigateIf (k, Key.CursorUp, this.tbPath);
				} else {
					if (splitContainer.Tiles.First ().ContentView.Visible && tableView.SelectedColumn == 0) {
						this.NavigateIf (k, Key.CursorLeft, this.treeView);
					}

					if (this.tableView.HasFocus &&
					!k.KeyEvent.Key.HasFlag (Key.CtrlMask) &&
					!k.KeyEvent.Key.HasFlag (Key.AltMask) &&
					 char.IsLetterOrDigit ((char)k.KeyEvent.KeyValue)) {
						CycleToNextTableEntryBeginningWith (k);
					}
				}

			};

			this.treeView = new TreeView<object> () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			this.treeView.TreeBuilder = new FileDialogTreeBuilder ();
			this.treeView.AspectGetter = (m) => m is DirectoryInfo d ? d.Name : m.ToString ();
			this.Style.TreeStyle = treeView.Style;

			this.treeView.SelectionChanged += this.TreeView_SelectionChanged;

			this.splitContainer.Tiles.ElementAt (0).ContentView.Add (this.treeView);
			this.splitContainer.Tiles.ElementAt (1).ContentView.Add (this.tableView);

			this.btnToggleSplitterCollapse = new Button (">>") {
				Y = Pos.AnchorEnd (1),
			};
			this.btnToggleSplitterCollapse.Clicked += (s, e) => {
				var tile = this.splitContainer.Tiles.ElementAt (0);

				var newState = !tile.ContentView.Visible;
				tile.ContentView.Visible = newState;
				this.btnToggleSplitterCollapse.Text = newState ? "<<" : ">>";
			};


			tbFind = new CaptionedTextField {
				X = Pos.Right (this.btnToggleSplitterCollapse) + 1,
				Caption = Style.SearchCaption,
				Width = 16,
				Y = Pos.AnchorEnd (1),
			};
			spinnerLabel = new SpinnerLabel () {
				X = Pos.Right (tbFind) + 1,
				Y = Pos.AnchorEnd (1),
				Width = 1,
				Height = 1,
				Visible = false,
			};

			tbFind.TextChanged += (s, o) => RestartSearch ();
			tbFind.KeyPress += (s, o) => {
				if (o.KeyEvent.Key == Key.Enter) {
					RestartSearch ();
					o.Handled = true;
				}
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

			this.tbPath.TextChanged += (s,e) => this.PathChanged ();

			this.tableView.CellActivated += this.CellActivate;
			this.tableView.KeyUp += (s, k) => k.Handled = this.TableView_KeyUp (k.KeyEvent);
			this.tableView.SelectedCellChanged += this.TableView_SelectedCellChanged;
			this.tableView.ColorScheme = ColorSchemeDefault;

			this.tableView.AddKeyBinding (Key.Home, Command.TopHome);
			this.tableView.AddKeyBinding (Key.End, Command.BottomEnd);
			this.tableView.AddKeyBinding (Key.Home | Key.ShiftMask, Command.TopHomeExtend);
			this.tableView.AddKeyBinding (Key.End | Key.ShiftMask, Command.BottomEndExtend);


			this.treeView.ColorScheme = ColorSchemeDefault;
			this.treeView.KeyDown += (s, k) => {


				var selected = treeView.SelectedObject;
				if (selected != null) {
					if (!treeView.CanExpand (selected) || treeView.IsExpanded (selected)) {
						this.NavigateIf (k, Key.CursorRight, this.tableView);
					} else
					if (treeView.GetObjectRow (selected) == 0) {
						this.NavigateIf (k, Key.CursorUp, this.tbPath);
					}
				}

				if (k.Handled) {
					return;
				}

				k.Handled = this.TreeView_KeyDown (k.KeyEvent);

			};

			this.AllowsMultipleSelection = false;

			this.UpdateNavigationVisibility ();

			// Determines tab order
			this.Add (this.btnToggleSplitterCollapse);
			this.Add (this.tbFind);
			this.Add (this.spinnerLabel);
			this.Add (this.btnOk);
			this.Add (this.btnCancel);
			this.Add (this.lblUp);
			this.Add (this.lblBack);
			this.Add (this.lblForward);
			this.Add (lblPath);
			this.Add (this.tbPath);
			this.Add (this.splitContainer);
		}
		/// <inheritdoc/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			if (this.NavigateIf (keyEvent, Key.CtrlMask | Key.F, this.tbFind)) {
				return true;
			}

			ClearFeedback ();

			if (allowedTypeMenuBar != null &&
				keyEvent.Key == Key.Tab &&
				allowedTypeMenuBar.IsMenuOpen) {
				allowedTypeMenuBar.CloseMenu (false, false, false);
			}

			return base.ProcessHotKey (keyEvent);
		}
		private void RestartSearch ()
		{
			if (disposed || state?.Directory == null) {
				return;
			}

			if (state is SearchState oldSearch) {
				oldSearch.Cancel ();
			}

			// user is clearing search terms
			if (tbFind.Text == null || tbFind.Text.Length == 0) {

				// Wait for search cancellation (if any) to finish
				// then push the current dir state
				lock (onlyOneSearchLock) {
					PushState (new FileDialogState (state.Directory, this), false);
				}
				return;
			}

			PushState (new SearchState (state?.Directory, this, tbFind.Text.ToString ()), true);
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			disposed = true;
			base.Dispose (disposing);

			if (state is SearchState search) {
				search.Cancel ();
			}
		}

		private void ClearFeedback ()
		{
			feedback = null;
		}

		private void CycleToNextTableEntryBeginningWith (KeyEventEventArgs keyEvent)
		{
			if (tableView.Table.Rows.Count == 0) {
				return;
			}

			var row = tableView.SelectedRow;

			// There is a multi select going on and not just for the current row
			if (tableView.GetAllSelectedCells ().Any (c => c.Y != row)) {
				return;
			}

			int match = collectionNavigator.GetNextMatchingItem (row, (char)keyEvent.KeyEvent.KeyValue);

			if (match != -1) {
				tableView.SelectedRow = match;
				tableView.EnsureValidSelection ();
				tableView.EnsureSelectedCellIsVisible ();
				keyEvent.Handled = true;
			}
		}

		private void UpdateCollectionNavigator ()
		{

			var collection = tableView
				.Table
				.Rows
				.Cast<DataRow> ()
				.Select ((o, idx) => RowToStats (idx))
				.Select (s => s.FileSystemInfo.Name)
				.ToArray ();

			collectionNavigator = new CollectionNavigator (collection);
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
		public ColorScheme ColorSchemeDirectory { private get; set; }

		/// <summary>
		/// Sets a <see cref="ColorScheme"/> to use for regular file rows of
		/// the <see cref="TableView"/>. Defaults to White text on Black background.
		/// </summary>
		public ColorScheme ColorSchemeDefault { private get; set; }

		/// <summary>
		/// Sets a <see cref="ColorScheme"/> to use for file rows with an image extension
		/// of the <see cref="TableView"/>. Defaults to White text on Black background.
		/// </summary>
		public ColorScheme ColorSchemeImage { private get; set; }

		/// <summary>
		/// Sets a <see cref="ColorScheme"/> to use for file rows with an executable extension
		/// or that match <see cref="AllowedTypes"/> in the <see cref="TableView"/>.
		/// </summary>
		public ColorScheme ColorSchemeExeOrRecommended { private get; set; }

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
			get => this.tbPath.Text.ToString ();
			set {
				this.tbPath.Text = value;
				this.tbPath.MoveCursorToEnd ();
			}
		}

		/// <summary>
		/// User defined delegate for picking which character(s)/unicode
		/// symbol(s) to use as an 'icon' for files/folders. Defaults to
		/// null (i.e. no icons).
		/// </summary>
		public Func<FileSystemInfo, string> IconGetter { get; set; } = null;

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
			get => this.tableView.MultiSelect;
			set => this.tableView.MultiSelect = value;
		}

		/// <summary>
		/// Gets or Sets a value indicating whether different colors
		/// should be used for different file types/directories.  Defaults
		/// to false.
		/// </summary>
		public bool UseColors { get; set; }

		/// <summary>
		/// Gets or Sets a collection of file types that the user can/must select. Only applies
		/// when <see cref="OpenMode"/> is <see cref="OpenMode.File"/> or <see cref="OpenMode.Mixed"/>.
		/// </summary>
		/// <remarks><see cref="AllowedTypeAny"/> adds the option to select any type (*.*). If this
		/// collection is empty then any type is supported and no Types drop-down is shown.</remarks> 
		public List<IAllowedType> AllowedTypes { get; set; } = new List<IAllowedType> ();

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


		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			this.Move (1, 0, false);

			// TODO: Refactor this to some Title drawing options class
			if (ustring.IsNullOrEmpty (Title)) {
				return;
			}

			var title = this.Title.ToString ();
			var titleWidth = title.Sum (c => Rune.ColumnWidth (c));

			if (titleWidth > bounds.Width) {
				title = title.Substring (0, bounds.Width);
			} else {
				if (titleWidth + 2 < bounds.Width) {
					title = '╡' + this.Title.ToString () + '╞';
				}
				titleWidth += 2;
			}

			var padLeft = ((bounds.Width - titleWidth) / 2) - 1;

			padLeft = Math.Min (bounds.Width, padLeft);
			padLeft = Math.Max (0, padLeft);

			var padRight = bounds.Width - (padLeft + titleWidth + 2);
			padRight = Math.Min (bounds.Width, padRight);
			padRight = Math.Max (0, padRight);

			Driver.SetAttribute (
			    new Attribute (this.ColorScheme.Normal.Foreground, this.ColorScheme.Normal.Background));

			Driver.AddStr (ustring.Make (Enumerable.Repeat (Driver.HDLine, padLeft)));

			Driver.SetAttribute (
			    new Attribute (this.ColorScheme.Normal.Foreground, this.ColorScheme.Normal.Background));
			Driver.AddStr (title);

			Driver.SetAttribute (
			    new Attribute (this.ColorScheme.Normal.Foreground, this.ColorScheme.Normal.Background));

			Driver.AddStr (ustring.Make (Enumerable.Repeat (Driver.HDLine, padRight)));

			if (!string.IsNullOrWhiteSpace (feedback)) {
				var feedbackWidth = feedback.Sum (c => Rune.ColumnWidth (c));
				var feedbackPadLeft = ((bounds.Width - feedbackWidth) / 2) - 1;

				feedbackPadLeft = Math.Min (bounds.Width, feedbackPadLeft);
				feedbackPadLeft = Math.Max (0, feedbackPadLeft);

				var feedbackPadRight = bounds.Width - (feedbackPadLeft + feedbackWidth + 2);
				feedbackPadRight = Math.Min (bounds.Width, feedbackPadRight);
				feedbackPadRight = Math.Max (0, feedbackPadRight);

				Move (0, Bounds.Height / 2);

				Driver.SetAttribute (new Attribute (Color.Red, this.ColorScheme.Normal.Background));
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
			this.btnOk.Text = Style.OkButtonText;

			treeView.AddObjects (Style.TreeRootGetter ());

			// if filtering on file type is configured then create the ComboBox and establish
			// initial filtering by extension(s)
			if (this.AllowedTypes.Any ()) {

				this.currentFilter = this.AllowedTypes [0];

				// Fiddle factor
				var width = this.AllowedTypes.Max (a => a.ToString ().Length) + 6;

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

				this.Add (allowedTypeMenuBar);
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

			if (ustring.IsNullOrEmpty (Title)) {
				switch (OpenMode) {
				case OpenMode.File:
					this.Title = $"OPEN {(MustExist ? "EXISTING " : "")}FILE";
					break;
				case OpenMode.Directory:
					this.Title = $"OPEN {(MustExist ? "EXISTING " : "")}DIRECTORY";
					break;
				case OpenMode.Mixed:
					this.Title = $"OPEN{(MustExist ? " EXISTING" : "")}";
					break;
				}
			}
			this.LayoutSubviews ();
		}

		private void AllowedTypeMenuClicked (int idx)
		{

			var allow = AllowedTypes [idx];
			for (int i = 0; i < AllowedTypes.Count; i++) {
				allowedTypeMenuItems [i].Checked = i == idx;
			}
			allowedTypeMenu.Title = allow.ToString ();

			this.currentFilter = allow;

			this.tbPath.ClearAllSelection ();
			this.tbPath.ClearSuggestions ();

			if (this.state != null) {
				this.state.RefreshChildren ();
				this.WriteStateToTableView ();
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

			this.MultiSelected = toMultiAccept.Select (s => s.FileSystemInfo.FullName).ToList ().AsReadOnly ();
			this.tbPath.Text = this.MultiSelected.Count == 1 ? this.MultiSelected [0] : string.Empty;

			FinishAccept ();
		}


		private void Accept (FileInfo f)
		{
			if (!this.IsCompatibleWithOpenMode (f.FullName, out var reason)) {
				feedback = reason;
				SetNeedsDisplay ();
				return;
			}

			this.tbPath.Text = f.FullName;

			if (AllowsMultipleSelection) {
				this.MultiSelected = new List<string> { f.FullName }.AsReadOnly ();
			}

			FinishAccept ();
		}

		private void Accept ()
		{
			if (!this.IsCompatibleWithOpenMode (this.tbPath.Text.ToString (), out string reason)) {
				if (reason != null) {
					feedback = reason;
					SetNeedsDisplay ();
				}
				return;
			}

			FinishAccept ();
		}

		private void FinishAccept ()
		{
			var e = new FilesSelectedEventArgs (this);

			this.FilesSelected?.Invoke (this, e);

			if (e.Cancel) {
				return;
			}

			this.Canceled = false;
			Application.RequestStop ();
		}

		private void NavigateIf (KeyEventEventArgs keyEvent, Key isKey, View to)
		{
			if (!keyEvent.Handled) {

				if (NavigateIf (keyEvent.KeyEvent, isKey, to)) {
					keyEvent.Handled = true;
				}
			}
		}

		private bool NavigateIf (KeyEvent keyEvent, Key isKey, View to)
		{
			if (keyEvent.Key == isKey) {

				to.FocusFirst ();
				if (to == tbPath) {
					tbPath.MoveCursorToEnd ();
				}
				return true;
			}

			return false;
		}

		private void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<object> e)
		{
			if (e.NewValue == null) {
				return;
			}

			this.tbPath.Text = FileDialogTreeBuilder.NodeToDirectory (e.NewValue).FullName;
		}

		private void UpdateNavigationVisibility ()
		{
			this.lblBack.Visible = this.history.CanBack ();
			this.lblForward.Visible = this.history.CanForward ();
			this.lblUp.Visible = this.history.CanUp ();
		}

		private void TableView_SelectedCellChanged (object sender, SelectedCellChangedEventArgs obj)
		{
			if (!this.tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows.Count == 0) {
				return;
			}

			if (this.tableView.MultiSelect && this.tableView.MultiSelectedRegions.Any ()) {
				return;
			}

			var stats = this.RowToStats (obj.NewRow);

			if (stats == null) {
				return;
			}
			FileSystemInfo dest;

			if (stats.IsParent) {
				dest = state.Directory;
			} else {
				dest = stats.FileSystemInfo;
			}

			try {
				this.pushingState = true;

				this.tbPath.SetTextTo (dest);
				this.state.Selected = stats;
				this.tbPath.ClearSuggestions ();

			} finally {

				this.pushingState = false;
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

			var nameStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (Style.FilenameColumnName, typeof (int)));
			nameStyle.RepresentationGetter = (i) => {

				var stats = this.state?.Children [(int)i];

				if (stats == null) {
					return string.Empty;
				}

				var icon = stats.IsParent ? null : IconGetter?.Invoke (stats.FileSystemInfo);

				if (icon != null) {
					return icon + stats.Name;
				}
				return stats.Name;
			};

			nameStyle.MinWidth = 50;

			var sizeStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (Style.SizeColumnName, typeof (int)));
			sizeStyle.RepresentationGetter = (i) => this.state?.Children [(int)i].HumanReadableLength ?? string.Empty;
			nameStyle.MinWidth = 10;

			var dateModifiedStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (Style.ModifiedColumnName, typeof (int)));
			dateModifiedStyle.RepresentationGetter = (i) => this.state?.Children [(int)i].DateModified?.ToString () ?? string.Empty;
			dateModifiedStyle.MinWidth = 30;

			var typeStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (Style.TypeColumnName, typeof (int)));
			typeStyle.RepresentationGetter = (i) => this.state?.Children [(int)i].Type ?? string.Empty;
			typeStyle.MinWidth = 6;
			this.tableView.Style.RowColorGetter = this.ColorGetter;
		}

		private void CellActivate (object sender, CellActivatedEventArgs obj)
		{
			var multi = this.MultiRowToStats ();
			string reason = null;
			if (multi.Any ()) {
				if (multi.All (m => this.IsCompatibleWithOpenMode (m.FileSystemInfo.FullName, out reason))) {
					this.Accept (multi);
					return;
				} else {
					if (reason != null) {
						feedback = reason;
						SetNeedsDisplay ();
					}

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

		private bool IsCompatibleWithAllowedExtensions (FileInfo file)
		{
			// no restrictions
			if (!this.AllowedTypes.Any ()) {
				return true;
			}
			return this.MatchesAllowedTypes (file);
		}

		private bool IsCompatibleWithAllowedExtensions (string path)
		{
			// no restrictions
			if (!this.AllowedTypes.Any ()) {
				return true;
			}

			return this.AllowedTypes.Any (t => t.IsAllowed (path));
		}

		/// <summary>
		/// Returns true if any <see cref="AllowedTypes"/> matches <paramref name="file"/>.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private bool MatchesAllowedTypes (FileInfo file)
		{
			return this.AllowedTypes.Any (t => t.IsAllowed (file.FullName));
		}
		private bool IsCompatibleWithOpenMode (string s, out string reason)
		{
			reason = null;
			if (string.IsNullOrWhiteSpace (s)) {
				return false;
			}

			if (!this.IsCompatibleWithAllowedExtensions (s)) {
				reason = Style.WrongFileTypeFeedback;
				return false;
			}

			switch (this.OpenMode) {
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

			PushState (new FileDialogState (d, this), addCurrentStateToHistory, setPathText, clearForward);
		}
		private void PushState (FileDialogState newState, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true)
		{
			if (state is SearchState search) {
				search.Cancel ();
			}

			try {
				this.pushingState = true;

				// push the old state to history
				if (addCurrentStateToHistory) {
					this.history.Push (this.state, clearForward);
				}

				this.tbPath.ClearSuggestions ();

				if (setPathText) {
					this.tbPath.Text = newState.Directory.FullName;
					this.tbPath.MoveCursorToEnd ();
				}

				this.state = newState;
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
			ClearFeedback ();
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
			UpdateCollectionNavigator ();
		}

		private void BuildRow (int idx)
		{
			this.tableView.Table.Rows.Add (idx, idx, idx, idx);
		}

		private ColorScheme ColorGetter (TableView.RowColorGetterArgs args)
		{
			var stats = this.RowToStats (args.RowIndex);

			if (!UseColors) {
				return ColorSchemeDefault;
			}

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
				return;
			}

			var dir = this.StringToDirectoryInfo (path);

			if (dir.Exists) {
				this.PushState (dir, true, false);
			} else
			if (dir.Parent?.Exists ?? false) {
				this.PushState (dir.Parent, true, false);
			}

			tbPath.GenerateSuggestions (state);
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


		/// <summary>
		/// Defines whether a given file/directory matches a set of
		/// search terms.
		/// </summary>
		public interface ISearchMatcher {
			/// <summary>
			/// Called once for each new search. Defines the string
			/// the user has provided as search terms.
			/// </summary>
			void Initialize (string terms);

			/// <summary>
			/// Return true if <paramref name="f"/> is a match to the
			/// last provided search terms
			/// </summary>
			bool IsMatch (FileSystemInfo f);
		}

		class DefaultSearchMatcher : ISearchMatcher {
			string terms;

			public void Initialize (string terms)
			{
				this.terms = terms;
			}

			public bool IsMatch (FileSystemInfo f)
			{
				//Contains overload with StringComparison is not available in (net472) or (netstandard2.0)
				//return f.Name.Contains (terms, StringComparison.OrdinalIgnoreCase);

				// This is the same
				return f.Name.IndexOf (terms, StringComparison.OrdinalIgnoreCase) >= 0;
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
					this.DateModified = FileDialog.UseUtcDates ? File.GetLastWriteTimeUtc (fi.FullName) : File.GetLastWriteTime (fi.FullName);
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

			internal object GetOrderByValue (FileDialog dlg, string columnName)
			{
				if (dlg.Style.FilenameColumnName == columnName)
					return this.FileSystemInfo.Name;

				if (dlg.Style.SizeColumnName == columnName)
					return this.MachineReadableLength;

				if (dlg.Style.ModifiedColumnName == columnName)
					return this.DateModified;

				if (dlg.Style.TypeColumnName == columnName)
					return this.Type;

				throw new ArgumentOutOfRangeException ("Unknown column " + nameof (columnName));
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

		/// <summary>
		/// State representing a recursive search from <see cref="FileDialogState.Directory"/>
		/// downwards.
		/// </summary>
		internal class SearchState : FileDialogState {

			bool cancel = false;
			bool finished = false;

			// TODO: Add thread safe child adding
			List<FileSystemInfoStats> found = new List<FileSystemInfoStats> ();
			object oLockFound = new object ();
			CancellationTokenSource token = new CancellationTokenSource ();

			public SearchState (DirectoryInfo dir, FileDialog parent, string searchTerms) : base (dir, parent)
			{
				parent.SearchMatcher.Initialize (searchTerms);
				Children = new FileSystemInfoStats [0];
				BeginSearch ();
			}

			private void BeginSearch ()
			{
				Task.Run (() => {
					RecursiveFind (Directory);
					finished = true;
				});

				Task.Run (() => {
					UpdateChildren ();
				});
			}

			private void UpdateChildren ()
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

					Application.MainLoop.Invoke (() => {
						Parent.spinnerLabel.Visible = false;
					});
				}
			}

			private void UpdateChildrenToFound ()
			{
				lock (oLockFound) {
					Children = found.ToArray ();
				}

				Application.MainLoop.Invoke (() => {
					Parent.tbPath.GenerateSuggestions (this);
					Parent.WriteStateToTableView ();

					Parent.spinnerLabel.Visible = true;
					Parent.spinnerLabel.SetNeedsDisplay ();
				});
			}

			private void RecursiveFind (DirectoryInfo directory)
			{
				foreach (var f in GetChildren (directory)) {

					if (cancel) {
						return;
					}

					if (f.IsParent) {
						continue;
					}

					lock (oLockFound) {
						if (found.Count >= FileDialog.MaxSearchResults) {
							finished = true;
							return;
						}
					}

					if (Parent.SearchMatcher.IsMatch (f.FileSystemInfo)) {
						lock (oLockFound) {
							found.Add (f);
						}
					}

					if (f.FileSystemInfo is DirectoryInfo sub) {
						RecursiveFind (sub);
					}
				}
			}

			internal override void RefreshChildren ()
			{
			}
			internal void Cancel ()
			{
				cancel = true;
				token.Cancel ();
			}
		}
		internal class FileDialogState {

			public FileSystemInfoStats Selected { get; set; }
			protected readonly FileDialog Parent;
			public FileDialogState (DirectoryInfo dir, FileDialog parent)
			{
				this.Directory = dir;
				Parent = parent;

				this.RefreshChildren ();
			}

			public DirectoryInfo Directory { get; }

			public FileSystemInfoStats [] Children { get; protected set; }

			internal virtual void RefreshChildren ()
			{
				var dir = this.Directory;
				Children = GetChildren (dir).ToArray ();
			}

			protected virtual IEnumerable<FileSystemInfoStats> GetChildren (DirectoryInfo dir)
			{
				try {

					List<FileSystemInfoStats> children;

					// if directories only
					if (Parent.OpenMode == OpenMode.Directory) {
						children = dir.GetDirectories ().Select (e => new FileSystemInfoStats (e)).ToList ();
					} else {
						children = dir.GetFileSystemInfos ().Select (e => new FileSystemInfoStats (e)).ToList ();
					}

					// if only allowing specific file types
					if (Parent.AllowedTypes.Any () && Parent.OpenMode == OpenMode.File) {

						children = children.Where (
							c => c.IsDir () ||
							(c.FileSystemInfo is FileInfo f && Parent.IsCompatibleWithAllowedExtensions (f)))
							.ToList ();
					}

					// if theres a UI filter in place too
					if (Parent.currentFilter != null) {
						children = children.Where (MatchesApiFilter).ToList ();
					}


					// allow navigating up as '..'
					if (dir.Parent != null) {
						children.Add (new FileSystemInfoStats (dir.Parent) { IsParent = true });
					}

					return children;
				} catch (Exception) {
					// Access permissions Exceptions, Dir not exists etc
					return Enumerable.Empty<FileSystemInfoStats> ();
				}
			}

			protected bool MatchesApiFilter (FileSystemInfoStats arg)
			{
				return arg.IsDir () ||
				(arg.FileSystemInfo is FileInfo f && Parent.currentFilter.IsAllowed (f.FullName));
			}
		}

		internal class CaptionedTextField : TextField {
			/// <summary>
			/// A text prompt to display in the field when it does not
			/// have focus and no text is yet entered.
			/// </summary>
			public ustring Caption { get; set; }

			/// <summary>
			/// The foreground color to use for the caption
			/// </summary>
			public Color CaptionColor { get; set; } = Color.Black;

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				if (HasFocus || Caption == null || Caption.Length == 0
					|| Text?.Length > 0) {
					return;
				}

				var color = new Attribute (CaptionColor, GetNormalColor ().Background);
				Driver.SetAttribute (color);

				Move (0, 0);
				var render = Caption;

				if (render.ConsoleWidth > Bounds.Width) {
					render = render.RuneSubstring (0, Bounds.Width);
				}

				Driver.AddStr (render);

			}
		}
		internal class SpinnerLabel : Label {
			private Rune [] runes = new Rune [] { '|', '/', '\u2500', '\\' };
			private int currentIdx = 0;
			private DateTime lastRender = DateTime.MinValue;

			public override void Redraw (Rect bounds)
			{
				if (DateTime.Now - lastRender > TimeSpan.FromMilliseconds (250)) {
					currentIdx = (currentIdx + 1) % runes.Length;
					Text = "" + runes [currentIdx];
				}

				base.Redraw (bounds);
			}
		}
		internal class TextFieldWithAppendAutocomplete : CaptionedTextField {

			private int? currentFragment = null;
			private string [] validFragments = new string [0];

			public TextFieldWithAppendAutocomplete ()
			{
				this.KeyPress += (s, k) => {
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
				Driver.SetAttribute (new Attribute (Color.DarkGray, Color.Black));
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
				var last = path.LastIndexOfAny (FileDialog.separators);

				if (last == -1 || suggestions.Length == 0 || last >= path.Length - 1) {
					this.currentFragment = null;
					return;
				}

				var term = path.Substring (last + 1);

				if (term.Equals (state?.Directory?.Name)) {
					this.ClearSuggestions ();
					return;
				}

				bool isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

				var validSuggestions = suggestions
					.Where (s => s.StartsWith (term, isWindows ?
						StringComparison.InvariantCultureIgnoreCase :
						StringComparison.InvariantCulture))
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
			private FileDialog dlg;

			public FileDialogHistory (FileDialog dlg)
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
			private readonly FileDialog dlg;
			private TableView tableView;

			private DataColumn currentSort = null;
			private bool currentSortIsAsc = true;

			public FileDialogSorter (FileDialog dlg, TableView tableView)
			{
				this.dlg = dlg;
				this.tableView = tableView;

				// if user clicks the mouse in TableView
				this.tableView.MouseClick += (s,e) => {

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
					sortAlgorithm = (v) => v.GetOrderByValue (dlg, colName);
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
				dlg.UpdateCollectionNavigator ();
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

			private void ShowHeaderContextMenu (DataColumn clickedCol, MouseEventArgs e)
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