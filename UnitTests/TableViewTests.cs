using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;

namespace Terminal.Gui.Views {

	public class TableViewTests {
		readonly ITestOutputHelper output;

		public TableViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}
		[Fact]
		public void EnsureValidScrollOffsets_WithNoCells ()
		{
			var tableView = new TableView ();

			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);

			// Set empty table
			tableView.Table = new DataTable ();

			// Since table has no rows or columns scroll offset should default to 0
			tableView.EnsureValidScrollOffsets ();
			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);
		}



		[Fact]
		public void EnsureValidScrollOffsets_LoadSmallerTable ()
		{
			var tableView = new TableView ();
			tableView.Bounds = new Rect (0, 0, 25, 10);

			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);

			// Set big table
			tableView.Table = BuildTable (25, 50);

			// Scroll down and along
			tableView.RowOffset = 20;
			tableView.ColumnOffset = 10;

			tableView.EnsureValidScrollOffsets ();

			// The scroll should be valid at the moment
			Assert.Equal (20, tableView.RowOffset);
			Assert.Equal (10, tableView.ColumnOffset);

			// Set small table
			tableView.Table = BuildTable (2, 2);

			// Setting a small table should automatically trigger fixing the scroll offsets to ensure valid cells
			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);


			// Trying to set invalid indexes should not be possible
			tableView.RowOffset = 20;
			tableView.ColumnOffset = 10;

			Assert.Equal (1, tableView.RowOffset);
			Assert.Equal (1, tableView.ColumnOffset);
		}

		[Fact]
		[AutoInitShutdown]
		public void Redraw_EmptyTable ()
		{
			var tableView = new TableView ();
			tableView.ColorScheme = new ColorScheme();
			tableView.Bounds = new Rect (0, 0, 25, 10);

			// Set a table with 1 column
			tableView.Table = BuildTable (1, 50);
			tableView.Redraw(tableView.Bounds);

			tableView.Table.Columns.Remove(tableView.Table.Columns[0]);
			tableView.Redraw(tableView.Bounds);
		}


		[Fact]
		public void SelectedCellChanged_NotFiredForSameValue ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50)
			};

			bool called = false;
			tableView.SelectedCellChanged += (e) => { called = true; };

			Assert.Equal (0, tableView.SelectedColumn);
			Assert.False (called);

			// Changing value to same as it already was should not raise an event
			tableView.SelectedColumn = 0;

			Assert.False (called);

			tableView.SelectedColumn = 10;
			Assert.True (called);
		}



		[Fact]
		public void SelectedCellChanged_SelectedColumnIndexesCorrect ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50)
			};

			bool called = false;
			tableView.SelectedCellChanged += (e) => {
				called = true;
				Assert.Equal (0, e.OldCol);
				Assert.Equal (10, e.NewCol);
			};

			tableView.SelectedColumn = 10;
			Assert.True (called);
		}

		[Fact]
		public void SelectedCellChanged_SelectedRowIndexesCorrect ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50)
			};

			bool called = false;
			tableView.SelectedCellChanged += (e) => {
				called = true;
				Assert.Equal (0, e.OldRow);
				Assert.Equal (10, e.NewRow);
			};

			tableView.SelectedRow = 10;
			Assert.True (called);
		}

		[Fact]
		public void Test_SumColumnWidth_UnicodeLength ()
		{
			Assert.Equal (11, "hello there".Sum (c => Rune.ColumnWidth (c)));

			// Creates a string with the peculiar (french?) r symbol
			String surrogate = "Les Mise" + Char.ConvertFromUtf32 (Int32.Parse ("0301", NumberStyles.HexNumber)) + "rables";

			// The unicode width of this string is shorter than the string length! 
			Assert.Equal (14, surrogate.Sum (c => Rune.ColumnWidth (c)));
			Assert.Equal (15, surrogate.Length);
		}

		[Fact]
		public void IsSelected_MultiSelectionOn_Vertical ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true
			};

			// 3 cell vertical selection
			tableView.SetSelection (1, 1, false);
			tableView.SetSelection (1, 3, true);

			Assert.False (tableView.IsSelected (0, 0));
			Assert.False (tableView.IsSelected (1, 0));
			Assert.False (tableView.IsSelected (2, 0));

			Assert.False (tableView.IsSelected (0, 1));
			Assert.True (tableView.IsSelected (1, 1));
			Assert.False (tableView.IsSelected (2, 1));

			Assert.False (tableView.IsSelected (0, 2));
			Assert.True (tableView.IsSelected (1, 2));
			Assert.False (tableView.IsSelected (2, 2));

			Assert.False (tableView.IsSelected (0, 3));
			Assert.True (tableView.IsSelected (1, 3));
			Assert.False (tableView.IsSelected (2, 3));

			Assert.False (tableView.IsSelected (0, 4));
			Assert.False (tableView.IsSelected (1, 4));
			Assert.False (tableView.IsSelected (2, 4));
		}


		[Fact]
		public void IsSelected_MultiSelectionOn_Horizontal ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true
			};

			// 2 cell horizontal selection
			tableView.SetSelection (1, 0, false);
			tableView.SetSelection (2, 0, true);

			Assert.False (tableView.IsSelected (0, 0));
			Assert.True (tableView.IsSelected (1, 0));
			Assert.True (tableView.IsSelected (2, 0));
			Assert.False (tableView.IsSelected (3, 0));

			Assert.False (tableView.IsSelected (0, 1));
			Assert.False (tableView.IsSelected (1, 1));
			Assert.False (tableView.IsSelected (2, 1));
			Assert.False (tableView.IsSelected (3, 1));
		}



		[Fact]
		public void IsSelected_MultiSelectionOn_BoxSelection ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true
			};

			// 4 cell horizontal in box 2x2
			tableView.SetSelection (0, 0, false);
			tableView.SetSelection (1, 1, true);

			Assert.True (tableView.IsSelected (0, 0));
			Assert.True (tableView.IsSelected (1, 0));
			Assert.False (tableView.IsSelected (2, 0));

			Assert.True (tableView.IsSelected (0, 1));
			Assert.True (tableView.IsSelected (1, 1));
			Assert.False (tableView.IsSelected (2, 1));

			Assert.False (tableView.IsSelected (0, 2));
			Assert.False (tableView.IsSelected (1, 2));
			Assert.False (tableView.IsSelected (2, 2));
		}

		[AutoInitShutdown]
		[Fact]
		public void PageDown_ExcludesHeaders ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};

			// Header should take up 2 lines
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.AlwaysShowHeaders = false;

			// ensure that TableView has the input focus
			Application.Top.Add (tableView);
			Application.Top.FocusFirst ();
			Assert.True (tableView.HasFocus);

			Assert.Equal (0, tableView.RowOffset);

			tableView.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ()));

			// window height is 5 rows 2 are header so page down should give 3 new rows
			Assert.Equal (3, tableView.SelectedRow);
			Assert.Equal (1, tableView.RowOffset);

			// header is no longer visible so page down should give 5 new rows
			tableView.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ()));

			Assert.Equal (8, tableView.SelectedRow);
			Assert.Equal (4, tableView.RowOffset);
		}

		[Fact]
		public void DeleteRow_SelectAll_AdjustsSelectionToPreventOverrun ()
		{
			// create a 4 by 4 table
			var tableView = new TableView () {
				Table = BuildTable (4, 4),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};

			tableView.SelectAll ();
			Assert.Equal (16, tableView.GetAllSelectedCells ().Count ());

			// delete one of the columns
			tableView.Table.Columns.RemoveAt (2);

			// table should now be 3x4
			Assert.Equal (12, tableView.GetAllSelectedCells ().Count ());

			// remove a row
			tableView.Table.Rows.RemoveAt (1);

			// table should now be 3x3
			Assert.Equal (9, tableView.GetAllSelectedCells ().Count ());
		}


		[Fact]
		public void DeleteRow_SelectLastRow_AdjustsSelectionToPreventOverrun ()
		{
			// create a 4 by 4 table
			var tableView = new TableView () {
				Table = BuildTable (4, 4),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};

			// select the last row
			tableView.MultiSelectedRegions.Clear ();
			tableView.MultiSelectedRegions.Push (new TableView.TableSelection (new Point (0, 3), new Rect (0, 3, 4, 1)));

			Assert.Equal (4, tableView.GetAllSelectedCells ().Count ());

			// remove a row
			tableView.Table.Rows.RemoveAt (0);

			tableView.EnsureValidSelection ();

			// since the selection no longer exists it should be removed
			Assert.Empty (tableView.MultiSelectedRegions);
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void GetAllSelectedCells_SingleCellSelected_ReturnsOne (bool multiSelect)
		{
			var tableView = new TableView () {
				Table = BuildTable (3, 3),
				MultiSelect = multiSelect,
				Bounds = new Rect (0, 0, 10, 5)
			};

			tableView.SetSelection (1, 1, false);

			Assert.Single (tableView.GetAllSelectedCells ());
			Assert.Equal (new Point (1, 1), tableView.GetAllSelectedCells ().Single ());
		}


		[Fact]
		public void GetAllSelectedCells_SquareSelection_ReturnsFour ()
		{
			var tableView = new TableView () {
				Table = BuildTable (3, 3),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};

			// move cursor to 1,1
			tableView.SetSelection (1, 1, false);
			// spread selection across to 2,2 (e.g. shift+right then shift+down)
			tableView.SetSelection (2, 2, true);

			var selected = tableView.GetAllSelectedCells ().ToArray ();

			Assert.Equal (4, selected.Length);
			Assert.Equal (new Point (1, 1), selected [0]);
			Assert.Equal (new Point (2, 1), selected [1]);
			Assert.Equal (new Point (1, 2), selected [2]);
			Assert.Equal (new Point (2, 2), selected [3]);
		}


		[Fact]
		public void GetAllSelectedCells_SquareSelection_FullRowSelect ()
		{
			var tableView = new TableView () {
				Table = BuildTable (3, 3),
				MultiSelect = true,
				FullRowSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};

			// move cursor to 1,1
			tableView.SetSelection (1, 1, false);
			// spread selection across to 2,2 (e.g. shift+right then shift+down)
			tableView.SetSelection (2, 2, true);

			var selected = tableView.GetAllSelectedCells ().ToArray ();

			Assert.Equal (6, selected.Length);
			Assert.Equal (new Point (0, 1), selected [0]);
			Assert.Equal (new Point (1, 1), selected [1]);
			Assert.Equal (new Point (2, 1), selected [2]);
			Assert.Equal (new Point (0, 2), selected [3]);
			Assert.Equal (new Point (1, 2), selected [4]);
			Assert.Equal (new Point (2, 2), selected [5]);
		}


		[Fact]
		public void GetAllSelectedCells_TwoIsolatedSelections_ReturnsSix ()
		{
			var tableView = new TableView () {
				Table = BuildTable (20, 20),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};

			/*  
				Sets up disconnected selections like:

				00000000000
				01100000000
				01100000000
				00000001100
				00000000000
			*/

			tableView.MultiSelectedRegions.Clear ();
			tableView.MultiSelectedRegions.Push (new TableView.TableSelection (new Point (1, 1), new Rect (1, 1, 2, 2)));
			tableView.MultiSelectedRegions.Push (new TableView.TableSelection (new Point (7, 3), new Rect (7, 3, 2, 1)));

			tableView.SelectedColumn = 8;
			tableView.SelectedRow = 3;

			var selected = tableView.GetAllSelectedCells ().ToArray ();

			Assert.Equal (6, selected.Length);

			Assert.Equal (new Point (1, 1), selected [0]);
			Assert.Equal (new Point (2, 1), selected [1]);
			Assert.Equal (new Point (1, 2), selected [2]);
			Assert.Equal (new Point (2, 2), selected [3]);
			Assert.Equal (new Point (7, 3), selected [4]);
			Assert.Equal (new Point (8, 3), selected [5]);
		}

		[Fact]
		public void TableView_ExpandLastColumn_True ()
		{
			var tv = SetUpMiniTable ();

			// the thing we are testing
			tv.Style.ExpandLastColumn = true;

			tv.Redraw (tv.Bounds);

			string expected = @"
┌─┬──────┐
│A│B     │
├─┼──────┤
│1│2     │
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void TableView_ExpandLastColumn_False ()
		{
			var tv = SetUpMiniTable ();

			// the thing we are testing
			tv.Style.ExpandLastColumn = false;

			tv.Redraw (tv.Bounds);

			string expected = @"
┌─┬─┬────┐
│A│B│    │
├─┼─┼────┤
│1│2│    │
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void TableView_ExpandLastColumn_False_ExactBounds ()
		{
			var tv = SetUpMiniTable ();

			// the thing we are testing
			tv.Style.ExpandLastColumn = false;
			// width exactly matches the max col widths
			tv.Bounds = new Rect (0, 0, 5, 4);

			tv.Redraw (tv.Bounds);

			string expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│2│
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void TableView_Activate()
		{
			string activatedValue = null;
			var tv = new TableView (BuildTable(1,1));
			tv.CellActivated += (c) => activatedValue = c.Table.Rows[c.Row][c.Col].ToString();

			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			// pressing enter should activate the first cell (selected cell)
			tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));
			Assert.Equal ("R0C0",activatedValue);

			// reset the test
			activatedValue = null;

			// clear keybindings and ensure that Enter does not trigger the event anymore
			tv.ClearKeybindings ();
			tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));
			Assert.Null(activatedValue);

			// New method for changing the activation key
			tv.AddKeyBinding (Key.z, Command.Accept);
			tv.ProcessKey (new KeyEvent (Key.z, new KeyModifiers ()));
			Assert.Equal ("R0C0", activatedValue);

			// reset the test
			activatedValue = null;
			tv.ClearKeybindings ();

			// Old method for changing the activation key
			tv.CellActivationKey = Key.z;
			tv.ProcessKey (new KeyEvent (Key.z, new KeyModifiers ()));
			Assert.Equal ("R0C0", activatedValue);
		}

		[Fact]
		public void TableView_ColorsTest_ColorGetter ()
		{
			var tv = SetUpMiniTable ();

			tv.Style.ExpandLastColumn = false;
			tv.Style.InvertSelectedCellFirstCharacter = true;

			// width exactly matches the max col widths
			tv.Bounds = new Rect (0, 0, 5, 4);
			
			// Create a style for column B
			var bStyle = tv.Style.GetOrCreateColumnStyle (tv.Table.Columns ["B"]);

			// when B is 2 use the custom highlight colour
			ColorScheme cellHighlight = new ColorScheme () { Normal = Attribute.Make (Color.BrightCyan, Color.DarkGray) };
			bStyle.ColorGetter = (a) => Convert.ToInt32(a.CellValue) == 2 ? cellHighlight : null;

			tv.Redraw (tv.Bounds);

			string expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│2│
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			string expectedColors = @"
00000
00000
00000
01020
";
			var invertedNormalColor = Application.Driver.MakeAttribute (tv.ColorScheme.Normal.Background, tv.ColorScheme.Normal.Foreground);

			GraphViewTests.AssertDriverColorsAre (expectedColors, new Attribute [] {
				// 0
				tv.ColorScheme.Normal,				
				// 1
				invertedNormalColor,				
				// 2
				cellHighlight.Normal});

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		private TableView SetUpMiniTable ()
		{

			var tv = new TableView ();
			tv.Bounds = new Rect (0, 0, 10, 4);

			var dt = new DataTable ();
			var colA = dt.Columns.Add ("A");
			var colB = dt.Columns.Add ("B");
			dt.Rows.Add (1, 2);

			tv.Table = dt;
			tv.Style.GetOrCreateColumnStyle (colA).MinWidth = 1;
			tv.Style.GetOrCreateColumnStyle (colA).MinWidth = 1;
			tv.Style.GetOrCreateColumnStyle (colB).MaxWidth = 1;
			tv.Style.GetOrCreateColumnStyle (colB).MaxWidth = 1;

			GraphViewTests.InitFakeDriver ();
			tv.ColorScheme = new ColorScheme () {
				Normal = Application.Driver.MakeAttribute (Color.White, Color.Black),
				HotFocus = Application.Driver.MakeAttribute (Color.White, Color.Black)
			};
			return tv;
		}

		[Fact]
		[AutoInitShutdown]
		public void ScrollDown_OneLineAtATime ()
		{
			var tableView = new TableView ();

			// Set big table
			tableView.Table = BuildTable (25, 50);

			// 1 header + 4 rows visible
			tableView.Bounds = new Rect (0, 0, 25, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;

			// select last row
			tableView.SelectedRow = 3; // row is 0 indexed so this is the 4th visible row

			// Scroll down
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorDown });

			// Scrolled off the page by 1 row so it should only have moved down 1 line of RowOffset
			Assert.Equal(4,tableView.SelectedRow);
			Assert.Equal (1, tableView.RowOffset);
		}

		[Fact]
		public void ScrollRight_SmoothScrolling ()
		{
			GraphViewTests.InitFakeDriver ();

			var tableView = new TableView ();
			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.Table = dt;

			// select last visible column
			tableView.SelectedColumn = 2; // column C

			tableView.Redraw (tableView.Bounds);

			string expected = 
				@"
│A│B│C│
│1│2│3│";

			GraphViewTests.AssertDriverContentsAre (expected, output);


			// Scroll right
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });


			tableView.Redraw (tableView.Bounds);

			// Note that with SmoothHorizontalScrolling only a single new column
			// is exposed when scrolling right.  This is not always the case though
			// sometimes if the leftmost column is long (i.e. A is a long column)
			// then when A is pushed off the screen multiple new columns could be exposed
			// (not just D but also E and F).  This is because TableView never shows
			// 'half cells' or scrolls by console unit (scrolling is done by table row/column increments).

			expected =
				@"
│B│C│D│
│2│3│4│";

			GraphViewTests.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void ScrollRight_WithoutSmoothScrolling ()
		{
			GraphViewTests.InitFakeDriver ();

			var tableView = new TableView ();
			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = false;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.Table = dt;

			// select last visible column
			tableView.SelectedColumn = 2; // column C

			tableView.Redraw (tableView.Bounds);

			string expected =
				@"
│A│B│C│
│1│2│3│";

			GraphViewTests.AssertDriverContentsAre (expected, output);


			// Scroll right
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });


			tableView.Redraw (tableView.Bounds);

			// notice that without smooth scrolling we just update the first column
			// rendered in the table to the newly exposed column (D).  This is fast
			// since we don't have to worry about repeatedly measuring the content
			// area as we scroll until the new column (D) is exposed.  But it makes
			// the view 'jump' to expose all new columns


			expected =
				@"
│D│E│F│
│4│5│6│";

			GraphViewTests.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void LongColumnTest ()
		{
			GraphViewTests.InitFakeDriver ();

			var tableView = new TableView ();
			tableView.ColorScheme = Colors.TopLevel;

			// 25 characters can be printed into table
			tableView.Bounds = new Rect (0, 0, 25, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("Very Long Column");

			dt.Rows.Add (1, 2, new string('a',500));
			dt.Rows.Add (1, 2, "aaa");

			tableView.Table = dt;

			tableView.Redraw (tableView.Bounds);

			// default behaviour of TableView is not to render
			// columns unless there is sufficient space
			string expected = 
				@"
│A│B                    │
├─┼─────────────────────►
│1│2                    │
│1│2                    │
";

			GraphViewTests.AssertDriverContentsAre (expected, output);

			// get a style for the long column
			var style = tableView.Style.GetOrCreateColumnStyle(dt.Columns[2]);
			
			// one way the API user can fix this for long columns
			// is to specify a max width for the column
			style.MaxWidth = 10;

			tableView.Redraw (tableView.Bounds);
			expected = 
				@"
│A│B│Very Long          │
├─┼─┼───────────────────┤
│1│2│aaaaaaaaaa         │
│1│2│aaa                │
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			// revert the style change
			style.MaxWidth = TableView.DefaultMaxCellWidth;

			// another way API user can fix problem is to implement
			// RepresentationGetter and apply max length there

			style.RepresentationGetter = (s)=>{
				return s.ToString().Length < 15 ? s.ToString() : s.ToString().Substring(0,13)+"...";
			};

			tableView.Redraw (tableView.Bounds);
			expected = 
				@"
│A│B│Very Long Column   │
├─┼─┼───────────────────┤
│1│2│aaaaaaaaaaaaa...   │
│1│2│aaa                │
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			// revert style change
			style.RepresentationGetter = null;

			// Both of the above methods rely on having a fixed
			// size limit for the column.  These are awkward if a
			// table is resizeable e.g. Dim.Fill().  Ideally we want
			// to render in any space available and truncate the content
			// of the column dynamically so it fills the free space at
			// the end of the table.

			// We can now specify that the column can be any length
			// (Up to MaxWidth) but the renderer can accept using
			// less space down to this limit
			style.MinAcceptableWidth = 5;

			tableView.Redraw (tableView.Bounds);
			expected = 
				@"
│A│B│Very Long Column   │
├─┼─┼───────────────────┤
│1│2│aaaaaaaaaaaaaaaaaaa│
│1│2│aaa                │
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			// Now test making the width too small for the MinAcceptableWidth
			// the Column won't fit so should not be rendered
			Application.Shutdown ();
			GraphViewTests.InitFakeDriver ();

			tableView.Bounds = new Rect(0,0,9,5);
			tableView.Redraw (tableView.Bounds);
			expected =
@"
│A│B    │
├─┼─────►
│1│2    │
│1│2    │

";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			// setting width to 10 leaves just enough space for the column to
			// meet MinAcceptableWidth of 5.  Column width includes terminator line
			// symbol (e.g. ┤ or │)
			tableView.Bounds = new Rect (0, 0, 10, 5);
			tableView.Redraw (tableView.Bounds);
			expected =
@"
│A│B│Very│
├─┼─┼────┤
│1│2│aaaa│
│1│2│aaa │
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			Application.Shutdown ();
		}


		[Fact]
		public void ScrollIndicators ()
		{
			GraphViewTests.InitFakeDriver ();

			var tableView = new TableView ();
			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.Table = dt;

			// select last visible column
			tableView.SelectedColumn = 2; // column C

			tableView.Redraw (tableView.Bounds);

			// user can only scroll right so sees right indicator
			// Because first column in table is A
			string expected = 
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│";

			GraphViewTests.AssertDriverContentsAre (expected, output);


			// Scroll right
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });


			// since A is now pushed off screen we get indicator showing
			// that user can scroll left to see first column
			tableView.Redraw (tableView.Bounds);

			expected =
				@"
│B│C│D│
◄─┼─┼─►
│2│3│4│";

			GraphViewTests.AssertDriverContentsAre (expected, output);


			// Scroll right twice more (to end of columns)
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });

			tableView.Redraw (tableView.Bounds);

			expected =
				@"
│D│E│F│
◄─┼─┼─┤
│4│5│6│";

			GraphViewTests.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		/// <summary>
		/// Builds a simple table of string columns with the requested number of columns and rows
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static DataTable BuildTable (int cols, int rows)
		{
			var dt = new DataTable ();

			for (int c = 0; c < cols; c++) {
				dt.Columns.Add ("Col" + c);
			}

			for (int r = 0; r < rows; r++) {
				var newRow = dt.NewRow ();

				for (int c = 0; c < cols; c++) {
					newRow [c] = $"R{r}C{c}";
				}

				dt.Rows.Add (newRow);
			}

			return dt;
		}
	}
}