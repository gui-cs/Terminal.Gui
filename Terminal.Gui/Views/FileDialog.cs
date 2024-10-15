using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>
///     Modal dialog for selecting files/directories. Has auto-complete and expandable navigation pane (Recent, Root
///     drives etc).
/// </summary>
public class FileDialog : Dialog
{
    private const int alignmentGroupInput = 32;
    private const int alignmentGroupComplete = 55;

    /// <summary>Gets the Path separators for the operating system</summary>
    internal static char [] Separators =
    [
        System.IO.Path.AltDirectorySeparatorChar,
        System.IO.Path.DirectorySeparatorChar
    ];

    /// <summary>
    ///     Characters to prevent entry into <see cref="_tbPath"/>. Note that this is not using
    ///     <see cref="System.IO.Path.GetInvalidFileNameChars"/> because we do want to allow directory separators, arrow keys
    ///     etc.
    /// </summary>
    private static readonly char [] _badChars = ['"', '<', '>', '|', '*', '?'];

    /// <summary>Locking object for ensuring only a single <see cref="SearchState"/> executes at once.</summary>
    internal object _onlyOneSearchLock = new ();

    private readonly Button _btnBack;
    private readonly Button _btnCancel;
    private readonly Button _btnForward;
    private readonly Button _btnOk;
    private readonly Button _btnToggleSplitterCollapse;
    private readonly Button _btnUp;
    private readonly IFileSystem _fileSystem;
    private readonly FileDialogHistory _history;
    private readonly SpinnerView _spinnerView;
    private readonly TileView _splitContainer;
    private readonly TableView _tableView;
    private readonly TextField _tbFind;
    private readonly TextField _tbPath;
    private readonly TreeView<IFileSystemInfo> _treeView;
    private MenuBarItem _allowedTypeMenu;
    private MenuBar _allowedTypeMenuBar;
    private MenuItem [] _allowedTypeMenuItems;
    private int _currentSortColumn;
    private bool _currentSortIsAsc = true;
    private bool _disposed;
    private string _feedback;
    private bool _loaded;

    private bool _pushingState;
    private Dictionary<IDirectoryInfo, string> _treeRoots = new ();

    /// <summary>Initializes a new instance of the <see cref="FileDialog"/> class.</summary>
    public FileDialog () : this (new FileSystem ()) { }

    /// <summary>Initializes a new instance of the <see cref="FileDialog"/> class with a custom <see cref="IFileSystem"/>.</summary>
    /// <remarks>This overload is mainly useful for testing.</remarks>
    internal FileDialog (IFileSystem fileSystem)
    {
        Height = Dim.Percent (80);
        Width = Dim.Percent (80);

        // Assume canceled
        Canceled = true;

        _fileSystem = fileSystem;
        Style = new FileDialogStyle (fileSystem);

        _btnOk = new Button
        {
            X = Pos.Align (Alignment.End, AlignmentModes.AddSpaceBetweenItems, alignmentGroupComplete),
            Y = Pos.AnchorEnd (),
            IsDefault = true, Text = Style.OkButtonText
        };
        _btnOk.Accepting += (s, e) => Accept (true);


        _btnCancel = new Button
        {
            X = Pos.Align (Alignment.End, AlignmentModes.AddSpaceBetweenItems, alignmentGroupComplete),
            Y = Pos.AnchorEnd(),
            Text = Strings.btnCancel
        };

        _btnCancel.Accepting += (s, e) =>
        {
            Canceled = true;
            Application.RequestStop ();
        };

        _btnUp = new Button { X = 0, Y = 1, NoPadding = true };
        _btnUp.Text = GetUpButtonText ();
        _btnUp.Accepting += (s, e) => _history.Up ();

        _btnBack = new Button { X = Pos.Right (_btnUp) + 1, Y = 1, NoPadding = true };
        _btnBack.Text = GetBackButtonText ();
        _btnBack.Accepting += (s, e) => _history.Back ();

        _btnForward = new Button { X = Pos.Right (_btnBack) + 1, Y = 1, NoPadding = true };
        _btnForward.Text = GetForwardButtonText ();
        _btnForward.Accepting += (s, e) => _history.Forward ();

        _tbPath = new TextField { Width = Dim.Fill (), CaptionColor = new Color (Color.Black) };

        _tbPath.KeyDown += (s, k) =>
                           {
                               ClearFeedback ();

                               AcceptIf (k, KeyCode.Enter);

                               SuppressIfBadChar (k);
                           };

        _tbPath.Autocomplete = new AppendAutocomplete (_tbPath);
        _tbPath.Autocomplete.SuggestionGenerator = new FilepathSuggestionGenerator ();

        _splitContainer = new TileView
        {
            X = 0,
            Y = Pos.Bottom (_btnBack),
            Width = Dim.Fill (),
            Height = Dim.Fill (Dim.Func (() => IsInitialized ? _btnOk.Frame.Height : 1)),
        };

        Initialized += (s, e) =>
                       {
                           _splitContainer.SetSplitterPos (0, 30);
                           _splitContainer.Tiles.ElementAt (0).ContentView.Visible = false;
                       };

        // this.splitContainer.Border.BorderStyle = BorderStyle.None;

        _tableView = new TableView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            FullRowSelect = true,
            CollectionNavigator = new FileDialogCollectionNavigator (this)
        };
        _tableView.KeyBindings.ReplaceCommands (Key.Space, Command.Select);
        _tableView.MouseClick += OnTableViewMouseClick;
        _tableView.Style.InvertSelectedCellFirstCharacter = true;
        Style.TableStyle = _tableView.Style;

        ColumnStyle nameStyle = Style.TableStyle.GetOrCreateColumnStyle (0);
        nameStyle.MinWidth = 10;
        nameStyle.ColorGetter = ColorGetter;

        ColumnStyle sizeStyle = Style.TableStyle.GetOrCreateColumnStyle (1);
        sizeStyle.MinWidth = 10;
        sizeStyle.ColorGetter = ColorGetter;

        ColumnStyle dateModifiedStyle = Style.TableStyle.GetOrCreateColumnStyle (2);
        dateModifiedStyle.MinWidth = 30;
        dateModifiedStyle.ColorGetter = ColorGetter;

        ColumnStyle typeStyle = Style.TableStyle.GetOrCreateColumnStyle (3);
        typeStyle.MinWidth = 6;
        typeStyle.ColorGetter = ColorGetter;
        
        _treeView = new TreeView<IFileSystemInfo> { Width = Dim.Fill (), Height = Dim.Fill () };

        var fileDialogTreeBuilder = new FileSystemTreeBuilder ();
        _treeView.TreeBuilder = fileDialogTreeBuilder;
        _treeView.AspectGetter = AspectGetter;
        Style.TreeStyle = _treeView.Style;

        _treeView.SelectionChanged += TreeView_SelectionChanged;

        _splitContainer.Tiles.ElementAt (0).ContentView.Add (_treeView);
        _splitContainer.Tiles.ElementAt (1).ContentView.Add (_tableView);

        _btnToggleSplitterCollapse = new Button
        {
            X = Pos.Align (Alignment.Start, AlignmentModes.AddSpaceBetweenItems, alignmentGroupInput),
            Y = Pos.AnchorEnd (), Text = GetToggleSplitterText (false)
        };

        _btnToggleSplitterCollapse.Accepting += (s, e) =>
                                              {
                                                  Tile tile = _splitContainer.Tiles.ElementAt (0);

                                                  bool newState = !tile.ContentView.Visible;
                                                  tile.ContentView.Visible = newState;
                                                  _btnToggleSplitterCollapse.Text = GetToggleSplitterText (newState);
                                                  LayoutSubviews ();
                                              };

        _tbFind = new TextField
        {
            X = Pos.Align (Alignment.Start,AlignmentModes.AddSpaceBetweenItems, alignmentGroupInput),
            CaptionColor = new Color (Color.Black),
            Width = 30,
            Y = Pos.Top (_btnToggleSplitterCollapse),
            HotKey = Key.F.WithAlt
        };
        _spinnerView = new SpinnerView { X = Pos.Align (Alignment.Start, AlignmentModes.AddSpaceBetweenItems, alignmentGroupInput), Y = Pos.AnchorEnd (1), Visible = false };

        _tbFind.TextChanged += (s, o) => RestartSearch ();

        _tbFind.KeyDown += (s, o) =>
                           {
                               if (o.KeyCode == KeyCode.Enter)
                               {
                                   RestartSearch ();
                                   o.Handled = true;
                               }

                               if (o.KeyCode == KeyCode.Esc)
                               {
                                   if (CancelSearch ())
                                   {
                                       o.Handled = true;
                                   }
                               }
                           };

        _tableView.Style.ShowHorizontalHeaderOverline = true;
        _tableView.Style.ShowVerticalCellLines = true;
        _tableView.Style.ShowVerticalHeaderLines = true;
        _tableView.Style.AlwaysShowHeaders = true;
        _tableView.Style.ShowHorizontalHeaderUnderline = true;
        _tableView.Style.ShowHorizontalScrollIndicators = true;

        _history = new FileDialogHistory (this);

        _tbPath.TextChanged += (s, e) => PathChanged ();

        _tableView.CellActivated += CellActivate;
        _tableView.KeyDown += (s, k) => k.Handled = TableView_KeyUp (k);
        _tableView.SelectedCellChanged += TableView_SelectedCellChanged;

        _tableView.KeyBindings.ReplaceCommands (Key.Home, Command.Start);
        _tableView.KeyBindings.ReplaceCommands (Key.End, Command.End);
        _tableView.KeyBindings.ReplaceCommands (Key.Home.WithShift, Command.StartExtend);
        _tableView.KeyBindings.ReplaceCommands (Key.End.WithShift, Command.EndExtend);
        
        AllowsMultipleSelection = false;

        UpdateNavigationVisibility ();

        Add (_tbPath);
        Add (_btnUp);
        Add (_btnBack);
        Add (_btnForward);
        Add (_splitContainer);
        Add (_btnToggleSplitterCollapse);
        Add (_tbFind);
        Add (_spinnerView);

        Add(_btnOk);
        Add(_btnCancel);
    }

    /// <summary>
    ///     Gets or Sets a collection of file types that the user can/must select. Only applies when
    ///     <see cref="OpenMode"/> is <see cref="OpenMode.File"/> or <see cref="OpenMode.Mixed"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="AllowedTypeAny"/> adds the option to select any type (*.*). If this collection is empty then any
    ///     type is supported and no Types drop-down is shown.
    /// </remarks>
    public List<IAllowedType> AllowedTypes { get; set; } = [];

    /// <summary>
    ///     Gets or Sets a value indicating whether to allow selecting multiple existing files/directories. Defaults to
    ///     false.
    /// </summary>
    public bool AllowsMultipleSelection
    {
        get => _tableView.MultiSelect;
        set => _tableView.MultiSelect = value;
    }

    /// <summary>The UI selected <see cref="IAllowedType"/> from combo box. May be null.</summary>
    public IAllowedType CurrentFilter { get; private set; }

    /// <summary>
    ///     Gets or sets behavior of the <see cref="FileDialog"/> when the user attempts to delete a selected file(s). Set
    ///     to null to prevent deleting.
    /// </summary>
    /// <remarks>
    ///     Ensure you use a try/catch block with appropriate error handling (e.g. showing a <see cref="MessageBox"/>
    /// </remarks>
    public IFileOperations FileOperationsHandler { get; set; } = new DefaultFileOperations ();

    /// <summary>The maximum number of results that will be collected when searching before stopping.</summary>
    /// <remarks>This prevents performance issues e.g. when searching root of file system for a common letter (e.g. 'e').</remarks>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static int MaxSearchResults { get; set; } = 10000;

    /// <summary>
    ///     Gets all files/directories selected or an empty collection <see cref="AllowsMultipleSelection"/> is
    ///     <see langword="false"/> or <see cref="CancelSearch"/>.
    /// </summary>
    /// <remarks>If selecting only a single file/directory then you should use <see cref="Path"/> instead.</remarks>
    public IReadOnlyList<string> MultiSelected { get; private set; }
        = Enumerable.Empty<string> ().ToList ().AsReadOnly ();

    /// <summary>
    ///     True if the file/folder must exist already to be selected. This prevents user from entering the name of
    ///     something that doesn't exist. Defaults to false.
    /// </summary>
    public bool MustExist { get; set; }

    /// <summary>
    ///     Gets or Sets which <see cref="System.IO.FileSystemInfo"/> type can be selected. Defaults to
    ///     <see cref="OpenMode.Mixed"/> (i.e. <see cref="DirectoryInfo"/> or <see cref="FileInfo"/>).
    /// </summary>
    public virtual OpenMode OpenMode { get; set; } = OpenMode.Mixed;

    /// <summary>
    ///     Gets or Sets the selected path in the dialog. This is the result that should be used if
    ///     <see cref="AllowsMultipleSelection"/> is off and <see cref="CancelSearch"/> is true.
    /// </summary>
    public string Path
    {
        get => _tbPath.Text;
        set
        {
            _tbPath.Text = value;
            _tbPath.MoveEnd ();
        }
    }

    /// <summary>
    ///     Defines how the dialog matches files/folders when using the search box. Provide a custom implementation if you
    ///     want to tailor how matching is performed.
    /// </summary>
    public ISearchMatcher SearchMatcher { get; set; } = new DefaultSearchMatcher ();

    /// <summary>
    ///     Gets settings for controlling how visual elements behave.  Style changes should be made before the
    ///     <see cref="Dialog"/> is loaded and shown to the user for the first time.
    /// </summary>
    public FileDialogStyle Style { get; }

    /// <summary>Gets the currently open directory and known children presented in the dialog.</summary>
    internal FileDialogState State { get; private set; }

    /// <summary>
    ///     Event fired when user attempts to confirm a selection (or multi selection). Allows you to cancel the selection
    ///     or undertake alternative behavior e.g. open a dialog "File already exists, Overwrite? yes/no".
    /// </summary>
    public event EventHandler<FilesSelectedEventArgs> FilesSelected;

    /// <summary>
    ///     Returns true if there are no <see cref="AllowedTypes"/> or one of them agrees that <paramref name="file"/>
    ///     <see cref="IAllowedType.IsAllowed(string)"/>.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public bool IsCompatibleWithAllowedExtensions (IFileInfo file)
    {
        // no restrictions
        if (!AllowedTypes.Any ())
        {
            return true;
        }

        return MatchesAllowedTypes (file);
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        if (!string.IsNullOrWhiteSpace (_feedback))
        {
            int feedbackWidth = _feedback.EnumerateRunes ().Sum (c => c.GetColumns ());
            int feedbackPadLeft = (Viewport.Width - feedbackWidth) / 2 - 1;

            feedbackPadLeft = Math.Min (Viewport.Width, feedbackPadLeft);
            feedbackPadLeft = Math.Max (0, feedbackPadLeft);

            int feedbackPadRight = Viewport.Width - (feedbackPadLeft + feedbackWidth + 2);
            feedbackPadRight = Math.Min (Viewport.Width, feedbackPadRight);
            feedbackPadRight = Math.Max (0, feedbackPadRight);

            Move (0, Viewport.Height / 2);

            Driver.SetAttribute (new Attribute (Color.Red, ColorScheme.Normal.Background));
            Driver.AddStr (new string (' ', feedbackPadLeft));
            Driver.AddStr (_feedback);
            Driver.AddStr (new string (' ', feedbackPadRight));
        }
    }

    /// <inheritdoc/>
    public override void OnLoaded ()
    {
        base.OnLoaded ();

        if (_loaded)
        {
            return;
        }

        _loaded = true;

        // May have been updated after instance was constructed
        _btnOk.Text = Style.OkButtonText;
        _btnCancel.Text = Style.CancelButtonText;
        _btnUp.Text = GetUpButtonText ();
        _btnBack.Text = GetBackButtonText ();
        _btnForward.Text = GetForwardButtonText ();
        _btnToggleSplitterCollapse.Text = GetToggleSplitterText (false);

        _tbPath.Caption = Style.PathCaption;
        _tbFind.Caption = Style.SearchCaption;

        _tbPath.Autocomplete.ColorScheme = new ColorScheme (_tbPath.ColorScheme)
        {
            Normal = new Attribute (Color.Black, _tbPath.ColorScheme.Normal.Background)
        };

        _treeRoots = Style.TreeRootGetter ();
        Style.IconProvider.IsOpenGetter = _treeView.IsExpanded;

        _treeView.AddObjects (_treeRoots.Keys);

        // if filtering on file type is configured then create the ComboBox and establish
        // initial filtering by extension(s)
        if (AllowedTypes.Any ())
        {
            CurrentFilter = AllowedTypes [0];

            // Fiddle factor
            int width = AllowedTypes.Max (a => a.ToString ().Length) + 6;

            _allowedTypeMenu = new MenuBarItem (
                                                "<placeholder>",
                                                _allowedTypeMenuItems = AllowedTypes.Select (
                                                                                             (a, i) => new MenuItem (
                                                                                              a.ToString (),
                                                                                              null,
                                                                                              () => { AllowedTypeMenuClicked (i); })
                                                                                            )
                                                                                    .ToArray ()
                                               );

            _allowedTypeMenuBar = new MenuBar
            {
                Width = width,
                Y = 1,
                X = Pos.AnchorEnd (width),

                // TODO: Does not work, if this worked then we could tab to it instead
                // of having to hit F9
                CanFocus = true,
                TabStop = TabBehavior.TabStop,
                Menus = [_allowedTypeMenu]
            };
            AllowedTypeMenuClicked (0);

            // TODO: Using v1's menu bar here is a hack. Need to upgrade this.
            _allowedTypeMenuBar.DrawContentComplete += (s, e) =>
                                                       {
                                                           _allowedTypeMenuBar.Move (e.NewViewport.Width - 1, 0);
                                                           Driver.AddRune (Glyphs.DownArrow);
                                                       };

            Add (_allowedTypeMenuBar);
        }

        // if no path has been provided
        if (_tbPath.Text.Length <= 0)
        {
            Path = Environment.CurrentDirectory;
        }

        // to streamline user experience and allow direct typing of paths
        // with zero navigation we start with focus in the text box and any
        // default/current path fully selected and ready to be overwritten
        _tbPath.SetFocus ();
        _tbPath.SelectAll ();

        if (string.IsNullOrEmpty (Title))
        {
            Title = GetDefaultTitle ();
        }

        if (Style.FlipOkCancelButtonLayoutOrder)
        {
            _btnCancel.X = Pos.Func (CalculateOkButtonPosX);
            _btnOk.X = Pos.Right (_btnCancel) + 1;
            MoveSubviewTowardsStart (_btnCancel);
        }
        LayoutSubviews ();
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        _disposed = true;
        base.Dispose (disposing);

        CancelSearch ();
    }

    /// <summary>
    ///     Gets a default dialog title, when <see cref="View.Title"/> is not set or empty, result of the function will be
    ///     shown.
    /// </summary>
    protected virtual string GetDefaultTitle ()
    {
        List<string> titleParts = [Strings.fdOpen];

        if (MustExist)
        {
            titleParts.Add (Strings.fdExisting);
        }

        switch (OpenMode)
        {
            case OpenMode.File:
                titleParts.Add (Strings.fdFile);

                break;
            case OpenMode.Directory:
                titleParts.Add (Strings.fdDirectory);

                break;
        }

        return string.Join (' ', titleParts);
    }

    internal void ApplySort ()
    {
        FileSystemInfoStats [] stats = State?.Children ?? new FileSystemInfoStats [0];

        // This portion is never reordered (always .. at top then folders)
        IOrderedEnumerable<FileSystemInfoStats> forcedOrder = stats
                                                              .OrderByDescending (f => f.IsParent)
                                                              .ThenBy (f => f.IsDir ? -1 : 100);

        // This portion is flexible based on the column clicked (e.g. alphabetical)
        IOrderedEnumerable<FileSystemInfoStats> ordered =
            _currentSortIsAsc
                ? forcedOrder.ThenBy (
                                      f =>
                                          FileDialogTableSource.GetRawColumnValue (_currentSortColumn, f)
                                     )
                : forcedOrder.ThenByDescending (
                                                f =>
                                                    FileDialogTableSource.GetRawColumnValue (_currentSortColumn, f)
                                               );

        State.Children = ordered.ToArray ();

        _tableView.Update ();
    }

    /// <summary>Changes the dialog such that <paramref name="d"/> is being explored.</summary>
    /// <param name="d"></param>
    /// <param name="addCurrentStateToHistory"></param>
    /// <param name="setPathText"></param>
    /// <param name="clearForward"></param>
    /// <param name="pathText">Optional alternate string to set path to.</param>
    internal void PushState (
        IDirectoryInfo d,
        bool addCurrentStateToHistory,
        bool setPathText = true,
        bool clearForward = true,
        string pathText = null
    )
    {
        // no change of state
        if (d == State?.Directory)
        {
            return;
        }

        if (d.FullName == State?.Directory.FullName)
        {
            return;
        }

        PushState (
                   new FileDialogState (d, this),
                   addCurrentStateToHistory,
                   setPathText,
                   clearForward,
                   pathText
                  );
    }

    /// <summary>Select <paramref name="toRestore"/> in the table view (if present)</summary>
    /// <param name="toRestore"></param>
    internal void RestoreSelection (IFileSystemInfo toRestore)
    {
        _tableView.SelectedRow = State.Children.IndexOf (r => r.FileSystemInfo == toRestore);
        _tableView.EnsureSelectedCellIsVisible ();
    }

    internal void SortColumn (int col, bool isAsc)
    {
        // set a sort order
        _currentSortColumn = col;
        _currentSortIsAsc = isAsc;

        ApplySort ();
    }

    private void Accept (IEnumerable<FileSystemInfoStats> toMultiAccept)
    {
        if (!AllowsMultipleSelection)
        {
            return;
        }

        // Don't include ".." (IsParent) in multi-selections
        MultiSelected = toMultiAccept
                        .Where (s => !s.IsParent)
                        .Select (s => s.FileSystemInfo.FullName)
                        .ToList ()
                        .AsReadOnly ();

        Path = MultiSelected.Count == 1 ? MultiSelected [0] : string.Empty;

        FinishAccept ();
    }

    private void Accept (IFileInfo f)
    {
        if (!IsCompatibleWithOpenMode (f.FullName, out string reason))
        {
            _feedback = reason;
            SetNeedsDisplay ();

            return;
        }

        Path = f.FullName;

        if (AllowsMultipleSelection)
        {
            MultiSelected = new List<string> { f.FullName }.AsReadOnly ();
        }

        FinishAccept ();
    }

    private void Accept (bool allowMulti)
    {
        if (allowMulti && TryAcceptMulti ())
        {
            return;
        }

        if (!IsCompatibleWithOpenMode (_tbPath.Text, out string reason))
        {
            if (reason is { })
            {
                _feedback = reason;
                SetNeedsDisplay ();
            }

            return;
        }

        FinishAccept ();
    }

    private void AcceptIf (Key key, KeyCode isKey)
    {
        if (!key.Handled && key.KeyCode == isKey)
        {
            key.Handled = true;

            // User hit Enter in text box so probably wants the
            // contents of the text box as their selection not
            // whatever lingering selection is in TableView
            Accept (false);
        }
    }

    private void AllowedTypeMenuClicked (int idx)
    {
        IAllowedType allow = AllowedTypes [idx];

        for (var i = 0; i < AllowedTypes.Count; i++)
        {
            _allowedTypeMenuItems [i].Checked = i == idx;
        }

        _allowedTypeMenu.Title = allow.ToString ();

        CurrentFilter = allow;

        _tbPath.ClearAllSelection ();
        _tbPath.Autocomplete.ClearSuggestions ();

        if (State is { })
        {
            State.RefreshChildren ();
            WriteStateToTableView ();
        }
    }

    private string AspectGetter (object o)
    {
        var fsi = (IFileSystemInfo)o;

        if (o is IDirectoryInfo dir && _treeRoots.ContainsKey (dir))
        {
            // Directory has a special name e.g. 'Pictures'
            return _treeRoots [dir];
        }

        return (Style.IconProvider.GetIconWithOptionalSpace (fsi) + fsi.Name).Trim ();
    }

    private int CalculateOkButtonPosX ()
    {
        if (!IsInitialized || !_btnOk.IsInitialized || !_btnCancel.IsInitialized)
        {
            return 0;
        }

        return Viewport.Width
               - _btnOk.Viewport.Width
               - _btnCancel.Viewport.Width
               - 1

               // TODO: Fiddle factor, seems the Viewport are wrong for someone
               - 2;
    }

    private bool CancelSearch ()
    {
        if (State is SearchState search)
        {
            return search.Cancel ();
        }

        return false;
    }

    private void CellActivate (object sender, CellActivatedEventArgs obj)
    {
        if (TryAcceptMulti ())
        {
            return;
        }

        FileSystemInfoStats stats = RowToStats (obj.Row);

        if (stats.FileSystemInfo is IDirectoryInfo d)
        {
            PushState (d, true);

            //if (d == State?.Directory || d.FullName == State?.Directory.FullName)
            //{
            //    FinishAccept ();
            //}

            return;
        }

        if (stats.FileSystemInfo is IFileInfo f)
        {
            Accept (f);
        }
    }

    private void ClearFeedback () { _feedback = null; }

    private ColorScheme ColorGetter (CellColorGetterArgs args)
    {
        FileSystemInfoStats stats = RowToStats (args.RowIndex);

        if (!Style.UseColors)
        {
            return _tableView.ColorScheme;
        }

        Color color = Style.ColorProvider.GetColor (stats.FileSystemInfo) ?? new Color (Color.White);
        var black = new Color (Color.Black);

        // TODO: Add some kind of cache for this
        return new ColorScheme
        {
            Normal = new Attribute (color, black),
            HotNormal = new Attribute (color, black),
            Focus = new Attribute (black, color),
            HotFocus = new Attribute (black, color)
        };
    }

    private void Delete ()
    {
        IFileSystemInfo [] toDelete = GetFocusedFiles ();

        if (toDelete is { } && FileOperationsHandler.Delete (toDelete))
        {
            RefreshState ();
        }
    }

    private void FinishAccept ()
    {
        var e = new FilesSelectedEventArgs (this);

        FilesSelected?.Invoke (this, e);

        if (e.Cancel)
        {
            return;
        }

        // if user uses Path selection mode (e.g. Enter in text box)
        // then also copy to MultiSelected
        if (AllowsMultipleSelection && !MultiSelected.Any ())
        {
            MultiSelected = string.IsNullOrWhiteSpace (Path)
                                ? Enumerable.Empty<string> ().ToList ().AsReadOnly ()
                                : new List<string> { Path }.AsReadOnly ();
        }

        Canceled = false;
        Application.RequestStop ();
    }

    private string GetBackButtonText () { return Glyphs.LeftArrow + "-"; }

    private IFileSystemInfo [] GetFocusedFiles ()
    {
        if (!_tableView.HasFocus || !_tableView.CanFocus || FileOperationsHandler is null)
        {
            return null;
        }

        _tableView.EnsureValidSelection ();

        if (_tableView.SelectedRow < 0)
        {
            return null;
        }

        return _tableView.GetAllSelectedCells ()
                         .Select (c => c.Y)
                         .Distinct ()
                         .Select (RowToStats)
                         .Where (s => !s.IsParent)
                         .Select (d => d.FileSystemInfo)
                         .ToArray ();
    }

    private string GetForwardButtonText () { return "-" + Glyphs.RightArrow; }

    private string GetProposedNewSortOrder (int clickedCol, out bool isAsc)
    {
        // work out new sort order
        if (_currentSortColumn == clickedCol && _currentSortIsAsc)
        {
            isAsc = false;

            return string.Format (Strings.fdCtxSortDesc, _tableView.Table.ColumnNames [clickedCol]);
        }

        isAsc = true;

        return string.Format (Strings.fdCtxSortAsc, _tableView.Table.ColumnNames [clickedCol]);
    }

    private string GetToggleSplitterText (bool isExpanded)
    {
        return isExpanded
                   ? new string ((char)Glyphs.LeftArrow.Value, 2)
                   : new string ((char)Glyphs.RightArrow.Value, 2);
    }

    private string GetUpButtonText () { return Style.UseUnicodeCharacters ? "◭" : "▲"; }

    private void HideColumn (int clickedCol)
    {
        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (clickedCol);
        style.Visible = false;
        _tableView.Update ();
    }

    private bool IsCompatibleWithAllowedExtensions (string path)
    {
        // no restrictions
        if (!AllowedTypes.Any ())
        {
            return true;
        }

        return AllowedTypes.Any (t => t.IsAllowed (path));
    }

    private bool IsCompatibleWithOpenMode (string s, out string reason)
    {
        reason = null;

        if (string.IsNullOrWhiteSpace (s))
        {
            return false;
        }

        if (!IsCompatibleWithAllowedExtensions (s))
        {
            reason = Style.WrongFileTypeFeedback;

            return false;
        }

        switch (OpenMode)
        {
            case OpenMode.Directory:
                if (MustExist && !Directory.Exists (s))
                {
                    reason = Style.DirectoryMustExistFeedback;

                    return false;
                }

                if (File.Exists (s))
                {
                    reason = Style.FileAlreadyExistsFeedback;

                    return false;
                }

                return true;
            case OpenMode.File:

                if (MustExist && !File.Exists (s))
                {
                    reason = Style.FileMustExistFeedback;

                    return false;
                }

                if (Directory.Exists (s))
                {
                    reason = Style.DirectoryAlreadyExistsFeedback;

                    return false;
                }

                return true;
            case OpenMode.Mixed:
                if (MustExist && !File.Exists (s) && !Directory.Exists (s))
                {
                    reason = Style.FileOrDirectoryMustExistFeedback;

                    return false;
                }

                return true;
            default: throw new ArgumentOutOfRangeException (nameof (OpenMode));
        }
    }

    /// <summary>Returns true if any <see cref="AllowedTypes"/> matches <paramref name="file"/>.</summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private bool MatchesAllowedTypes (IFileInfo file) { return AllowedTypes.Any (t => t.IsAllowed (file.FullName)); }

    /// <summary>
    ///     If <see cref="TableView.MultiSelect"/> is this returns a union of all <see cref="FileSystemInfoStats"/> in the
    ///     selection.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<FileSystemInfoStats> MultiRowToStats ()
    {
        HashSet<FileSystemInfoStats> toReturn = new ();

        if (AllowsMultipleSelection && _tableView.MultiSelectedRegions.Any ())
        {
            foreach (Point p in _tableView.GetAllSelectedCells ())
            {
                FileSystemInfoStats add = State?.Children [p.Y];

                if (add is { })
                {
                    toReturn.Add (add);
                }
            }
        }

        return toReturn;
    }

    private void New ()
    {
        if (State is { })
        {
            IFileSystemInfo created = FileOperationsHandler.New (_fileSystem, State.Directory);

            if (created is { })
            {
                RefreshState ();
                RestoreSelection (created);
            }
        }
    }

    private void OnTableViewMouseClick (object sender, MouseEventArgs e)
    {
        Point? clickedCell = _tableView.ScreenToCell (e.Position.X, e.Position.Y, out int? clickedCol);

        if (clickedCol is { })
        {
            if (e.Flags.HasFlag (MouseFlags.Button1Clicked))
            {
                // left click in a header
                SortColumn (clickedCol.Value);
            }
            else if (e.Flags.HasFlag (MouseFlags.Button3Clicked))
            {
                // right click in a header
                ShowHeaderContextMenu (clickedCol.Value, e);
            }
        }
        else
        {
            if (clickedCell is { } && e.Flags.HasFlag (MouseFlags.Button3Clicked))
            {
                // right click in rest of table
                ShowCellContextMenu (clickedCell, e);
            }
        }
    }

    private void PathChanged ()
    {
        // avoid re-entry
        if (_pushingState)
        {
            return;
        }

        string path = _tbPath.Text;

        if (string.IsNullOrWhiteSpace (path))
        {
            return;
        }

        IDirectoryInfo dir = StringToDirectoryInfo (path);

        if (dir.Exists)
        {
            PushState (dir, true, false);
        }
        else if (dir.Parent?.Exists ?? false)
        {
            PushState (dir.Parent, true, false);
        }

        _tbPath.Autocomplete.GenerateSuggestions (
                                                  new AutocompleteFilepathContext (_tbPath.Text, _tbPath.CursorPosition, State)
                                                 );
    }

    private void PushState (
        FileDialogState newState,
        bool addCurrentStateToHistory,
        bool setPathText = true,
        bool clearForward = true,
        string pathText = null
    )
    {
        if (State is SearchState search)
        {
            search.Cancel ();
        }

        try
        {
            _pushingState = true;

            // push the old state to history
            if (addCurrentStateToHistory)
            {
                _history.Push (State, clearForward);
            }

            _tbPath.Autocomplete.ClearSuggestions ();

            if (pathText is { })
            {
                Path = pathText;
            }
            else if (setPathText)
            {
                Path = newState.Directory.FullName;
            }

            State = newState;

            _tbPath.Autocomplete.GenerateSuggestions (
                                                      new AutocompleteFilepathContext (_tbPath.Text, _tbPath.CursorPosition, State)
                                                     );

            WriteStateToTableView ();

            if (clearForward)
            {
                _history.ClearForward ();
            }

            _tableView.RowOffset = 0;
            _tableView.SelectedRow = 0;

            SetNeedsDisplay ();
            UpdateNavigationVisibility ();
        }
        finally
        {
            _pushingState = false;
        }

        ClearFeedback ();
    }

    private void RefreshState ()
    {
        State.RefreshChildren ();
        PushState (State, false, false, false);
    }

    private void Rename ()
    {
        IFileSystemInfo [] toRename = GetFocusedFiles ();

        if (toRename?.Length == 1)
        {
            IFileSystemInfo newNamed = FileOperationsHandler.Rename (_fileSystem, toRename.Single ());

            if (newNamed is { })
            {
                RefreshState ();
                RestoreSelection (newNamed);
            }
        }
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

    //			if (allowedTypeMenuBar is { } &&
    //				keyEvent.ConsoleDriverKey == Key.Tab &&
    //				allowedTypeMenuBar.IsMenuOpen) {
    //				allowedTypeMenuBar.CloseMenu (false, false, false);
    //			}

    //			return base.OnHotKey (keyEvent);
    //		}
    private void RestartSearch ()
    {
        if (_disposed || State?.Directory is null)
        {
            return;
        }

        if (State is SearchState oldSearch)
        {
            oldSearch.Cancel ();
        }

        // user is clearing search terms
        if (_tbFind.Text is null || _tbFind.Text.Length == 0)
        {
            // Wait for search cancellation (if any) to finish
            // then push the current dir state
            lock (_onlyOneSearchLock)
            {
                PushState (new FileDialogState (State.Directory, this), false);
            }

            return;
        }

        PushState (new SearchState (State?.Directory, this, _tbFind.Text), true);
    }

    private FileSystemInfoStats RowToStats (int rowIndex) { return State?.Children [rowIndex]; }

    private void ShowCellContextMenu (Point? clickedCell, MouseEventArgs e)
    {
        if (clickedCell is null)
        {
            return;
        }

        var contextMenu = new ContextMenu
        {
            Position = new Point (e.Position.X + 1, e.Position.Y + 1)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem (Strings.fdCtxNew, string.Empty, New),
                                             new MenuItem (Strings.fdCtxRename, string.Empty, Rename),
                                             new MenuItem (Strings.fdCtxDelete, string.Empty, Delete)
                                         ]
                                        );
        _tableView.SetSelection (clickedCell.Value.X, clickedCell.Value.Y, false);

        contextMenu.Show (menuItems);
    }

    private void ShowHeaderContextMenu (int clickedCol, MouseEventArgs e)
    {
        string sort = GetProposedNewSortOrder (clickedCol, out bool isAsc);

        var contextMenu = new ContextMenu
        {
            Position = new Point (e.Position.X + 1, e.Position.Y + 1)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem (
                                                           string.Format (
                                                                          Strings.fdCtxHide,
                                                                          StripArrows (_tableView.Table.ColumnNames [clickedCol])
                                                                         ),
                                                           string.Empty,
                                                           () => HideColumn (clickedCol)
                                                          ),
                                             new MenuItem (
                                                           StripArrows (sort),
                                                           string.Empty,
                                                           () => SortColumn (clickedCol, isAsc))
                                         ]
                                        );
        contextMenu.Show (menuItems);
    }

    private void SortColumn (int clickedCol)
    {
        GetProposedNewSortOrder (clickedCol, out bool isAsc);
        SortColumn (clickedCol, isAsc);

        _tableView.Table =
            new FileDialogTableSource (this, State, Style, _currentSortColumn, _currentSortIsAsc);
    }

    private IDirectoryInfo StringToDirectoryInfo (string path)
    {
        // if you pass new DirectoryInfo("C:") you get a weird object
        // where the FullName is in fact the current working directory.
        // really not what most users would expect
        if (Regex.IsMatch (path, "^\\w:$"))
        {
            return _fileSystem.DirectoryInfo.New (path + System.IO.Path.DirectorySeparatorChar);
        }

        return _fileSystem.DirectoryInfo.New (path);
    }

    private static string StripArrows (string columnName) { return columnName.Replace (" (▼)", string.Empty).Replace (" (▲)", string.Empty); }

    private void SuppressIfBadChar (Key k)
    {
        // don't let user type bad letters
        var ch = (char)k;

        if (_badChars.Contains (ch))
        {
            k.Handled = true;
        }
    }

    private bool TableView_KeyUp (Key keyEvent)
    {
        if (keyEvent.KeyCode == KeyCode.Backspace)
        {
            return _history.Back ();
        }

        if (keyEvent.KeyCode == (KeyCode.ShiftMask | KeyCode.Backspace))
        {
            return _history.Forward ();
        }

        if (keyEvent.KeyCode == KeyCode.Delete)
        {
            Delete ();

            return true;
        }

        if (keyEvent.KeyCode == (KeyCode.CtrlMask | KeyCode.R))
        {
            Rename ();

            return true;
        }

        if (keyEvent.KeyCode == (KeyCode.CtrlMask | KeyCode.N))
        {
            New ();

            return true;
        }

        return false;
    }

    private void TableView_SelectedCellChanged (object sender, SelectedCellChangedEventArgs obj)
    {
        if (!_tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows == 0)
        {
            return;
        }

        if (_tableView.MultiSelect && _tableView.MultiSelectedRegions.Any ())
        {
            return;
        }

        FileSystemInfoStats stats = RowToStats (obj.NewRow);

        if (stats is null)
        {
            return;
        }

        IFileSystemInfo dest;

        if (stats.IsParent)
        {
            dest = State.Directory;
        }
        else
        {
            dest = stats.FileSystemInfo;
        }

        try
        {
            _pushingState = true;

            Path = dest.FullName;
            State.Selected = stats;
            _tbPath.Autocomplete.ClearSuggestions ();
        }
        finally
        {
            _pushingState = false;
        }
    }

    private void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<IFileSystemInfo> e)
    {
        if (e.NewValue is null)
        {
            return;
        }

        Path = e.NewValue.FullName;
    }

    private bool TryAcceptMulti ()
    {
        IEnumerable<FileSystemInfoStats> multi = MultiRowToStats ();
        string reason = null;

        if (!multi.Any ())
        {
            return false;
        }

        if (multi.All (
                       m => IsCompatibleWithOpenMode (
                                                      m.FileSystemInfo.FullName,
                                                      out reason
                                                     )
                      ))
        {
            Accept (multi);

            return true;
        }

        if (reason is { })
        {
            _feedback = reason;
            SetNeedsDisplay ();
        }

        return false;
    }

    private void UpdateNavigationVisibility ()
    {
        _btnBack.Visible = _history.CanBack ();
        _btnForward.Visible = _history.CanForward ();
        _btnUp.Visible = _history.CanUp ();
    }

    private void WriteStateToTableView ()
    {
        if (State is null)
        {
            return;
        }

        _tableView.Table =
            new FileDialogTableSource (this, State, Style, _currentSortColumn, _currentSortIsAsc);

        ApplySort ();
        _tableView.Update ();
    }

    internal class FileDialogCollectionNavigator : CollectionNavigatorBase
    {
        private readonly FileDialog _fileDialog;
        public FileDialogCollectionNavigator (FileDialog fileDialog) { _fileDialog = fileDialog; }

        protected override object ElementAt (int idx)
        {
            object val = FileDialogTableSource.GetRawColumnValue (
                                                                  _fileDialog._tableView.SelectedColumn,
                                                                  _fileDialog.State?.Children [idx]
                                                                 );

            if (val is null)
            {
                return string.Empty;
            }

            return val.ToString ().Trim ('.');
        }

        protected override int GetCollectionLength () { return _fileDialog.State?.Children.Length ?? 0; }
    }

    /// <summary>State representing a recursive search from <see cref="FileDialogState.Directory"/> downwards.</summary>
    internal class SearchState : FileDialogState
    {
        // TODO: Add thread safe child adding
        private readonly List<FileSystemInfoStats> _found = [];
        private readonly object _oLockFound = new ();
        private readonly CancellationTokenSource _token = new ();
        private bool _cancel;
        private bool _finished;

        public SearchState (IDirectoryInfo dir, FileDialog parent, string searchTerms) : base (dir, parent)
        {
            parent.SearchMatcher.Initialize (searchTerms);
            Children = new FileSystemInfoStats [0];
            BeginSearch ();
        }

        /// <summary>
        ///     Cancels the current search (if any).  Returns true if a search was running and cancellation was successfully
        ///     set.
        /// </summary>
        /// <returns></returns>
        internal bool Cancel ()
        {
            bool alreadyCancelled = _token.IsCancellationRequested || _cancel;

            _cancel = true;
            _token.Cancel ();

            return !alreadyCancelled;
        }

        internal override void RefreshChildren () { }

        private void BeginSearch ()
        {
            Task.Run (
                      () =>
                      {
                          RecursiveFind (Directory);
                          _finished = true;
                      }
                     );

            Task.Run (() => { UpdateChildren (); });
        }

        private void RecursiveFind (IDirectoryInfo directory)
        {
            foreach (FileSystemInfoStats f in GetChildren (directory))
            {
                if (_cancel)
                {
                    return;
                }

                if (f.IsParent)
                {
                    continue;
                }

                lock (_oLockFound)
                {
                    if (_found.Count >= MaxSearchResults)
                    {
                        _finished = true;

                        return;
                    }
                }

                if (Parent.SearchMatcher.IsMatch (f.FileSystemInfo))
                {
                    lock (_oLockFound)
                    {
                        _found.Add (f);
                    }
                }

                if (f.FileSystemInfo is IDirectoryInfo sub)
                {
                    RecursiveFind (sub);
                }
            }
        }

        private void UpdateChildren ()
        {
            lock (Parent._onlyOneSearchLock)
            {
                while (!_cancel && !_finished)
                {
                    try
                    {
                        Task.Delay (250).Wait (_token.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _cancel = true;
                    }

                    if (_cancel || _finished)
                    {
                        break;
                    }

                    UpdateChildrenToFound ();
                }

                if (_finished && !_cancel)
                {
                    UpdateChildrenToFound ();
                }

                Application.Invoke (() => { Parent._spinnerView.Visible = false; });
            }
        }

        private void UpdateChildrenToFound ()
        {
            lock (_oLockFound)
            {
                Children = _found.ToArray ();
            }

            Application.Invoke (
                                () =>
                                {
                                    Parent._tbPath.Autocomplete.GenerateSuggestions (
                                                                                     new AutocompleteFilepathContext (
                                                                                          Parent._tbPath.Text,
                                                                                          Parent._tbPath.CursorPosition,
                                                                                          this
                                                                                         )
                                                                                    );
                                    Parent.WriteStateToTableView ();

                                    Parent._spinnerView.Visible = true;
                                    Parent._spinnerView.SetNeedsDisplay ();
                                }
                               );
        }
    }
}
