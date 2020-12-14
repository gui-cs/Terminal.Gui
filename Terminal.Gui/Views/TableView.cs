using NStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui.Views {

	/// <summary>
	/// Defines rendering options that affect how the table is displayed
	/// </summary>
	public class TableStyle {
		
		/// <summary>
		/// When scrolling down always lock the column headers in place as the first row of the table
		/// </summary>
		public bool AlwaysShowHeaders {get;set;} = false;

		/// <summary>
		/// True to render a solid line above the headers
		/// </summary>
		public bool ShowHorizontalHeaderOverline {get;set;} = true;

		/// <summary>
		/// True to render a solid line under the headers
		/// </summary>
		public bool ShowHorizontalHeaderUnderline {get;set;} = true;

		/// <summary>
		/// True to render a solid line vertical line between cells
		/// </summary>
		public bool ShowVerticalCellLines {get;set;} = true;

		/// <summary>
		/// True to render a solid line vertical line between headers
		/// </summary>
		public bool ShowVerticalHeaderLines {get;set;} = true;
	}
	
	/// <summary>
	/// View for tabular data based on a <see cref="DataTable"/>
	/// </summary>
	public class TableView : View {

		private int columnOffset;
		private int rowOffset;
		private int selectedRow;
		private int selectedColumn;
		private DataTable table;
		private TableStyle style = new TableStyle();

		/// <summary>
		/// The data table to render in the view.  Setting this property automatically updates and redraws the control.
		/// </summary>
		public DataTable Table { get => table; set {table = value; Update(); } }
		
		/// <summary>
		/// Contains options for changing how the table is rendered
		/// </summary>
		public TableStyle Style { get => style; set {style = value; Update(); } }
						
		/// <summary>
		/// Zero indexed offset for the upper left <see cref="DataColumn"/> to display in <see cref="Table"/>.
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int ColumnOffset {
			get => columnOffset;

			//try to prevent this being set to an out of bounds column
			set => columnOffset = Table == null ? 0 : Math.Min (Table.Columns.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// Zero indexed offset for the <see cref="DataRow"/> to display in <see cref="Table"/> on line 2 of the control (first line being headers)
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int RowOffset {
			get => rowOffset;
			set => rowOffset = Table == null ? 0 : Math.Min (Table.Rows.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// The index of <see cref="DataTable.Columns"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedColumn {
			get => selectedColumn;

			//try to prevent this being set to an out of bounds column
			set => selectedColumn = Table == null ? 0 :  Math.Min (Table.Columns.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// The index of <see cref="DataTable.Rows"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedRow {
			get => selectedRow;
			set => selectedRow =  Table == null ? 0 : Math.Min (Table.Rows.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// The maximum number of characters to render in any given column.  This prevents one long column from pushing out all the others
		/// </summary>
		public int MaximumCellWidth { get; set; } = 100;

		/// <summary>
		/// The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/>
		/// </summary>
		public string NullSymbol { get; set; } = "-";

		/// <summary>
		/// The symbol to add after each cell value and header value to visually seperate values (if not using vertical gridlines)
		/// </summary>
		public char SeparatorSymbol { get; set; } = ' ';

		/// <summary>
		/// Initialzies a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. 
		/// </summary>
		/// <param name="table">The table to display in the control</param>
		public TableView (DataTable table) : this ()
		{
			this.Table = table;
		}

		/// <summary>
		/// Initialzies a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. Set the <see cref="Table"/> property to begin editing
		/// </summary>
		public TableView () : base ()
		{
			CanFocus = true;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			var frame = Frame;

			// What columns to render at what X offset in viewport
			Dictionary<DataColumn, int> columnsToRender = CalculateViewport (bounds);

			Driver.SetAttribute (ColorScheme.Normal);
			
			//invalidate current row (prevents scrolling around leaving old characters in the frame
			Driver.AddStr (new string (' ', bounds.Width));

			int line = 0;

			if(ShouldRenderHeaders()){
				// Render something like:
				/*
					┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐
					│ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
					└────────────────────┴──────────┴───────────┴──────────────┴─────────┘
				*/
				if(Style.ShowHorizontalHeaderOverline){
					RenderHeaderOverline(line,bounds.Width,columnsToRender);
					line++;
				}

				RenderHeaderMidline(line,bounds.Width,columnsToRender);
				line++;

				if(Style.ShowHorizontalHeaderUnderline){
					RenderHeaderUnderline(line,bounds.Width,columnsToRender);
					line++;
				}
			}
					
			//render the cells
			for (; line < frame.Height; line++) {

				ClearLine(line,bounds.Width);

				//work out what Row to render
				var rowToRender = RowOffset + (line - GetHeaderHeight());

				//if we have run off the end of the table
				if ( Table == null || rowToRender >= Table.Rows.Count || rowToRender < 0)
					continue;

				RenderRow(line,bounds.Width,rowToRender,columnsToRender);
			}
		}

		/// <summary>
		/// Clears a line of the console by filling it with spaces
		/// </summary>
		/// <param name="row"></param>
		/// <param name="width"></param>
		private void ClearLine(int row, int width)
		{            
			Move (0, row);
			Driver.SetAttribute (ColorScheme.Normal);
			Driver.AddStr (new string (' ', width));
		}

		/// <summary>
		/// Returns the amount of vertical space required to display the header
		/// </summary>
		/// <returns></returns>
		private int GetHeaderHeight()
		{
			int heightRequired = 1;
			
			if(Style.ShowHorizontalHeaderOverline)
				heightRequired++;

			if(Style.ShowHorizontalHeaderUnderline)
				heightRequired++;
			
			return heightRequired;
		}

		private void RenderHeaderOverline(int row,int availableWidth, Dictionary<DataColumn, int> columnsToRender)
		{
			// Renders a line above table headers (when visible) like:
			// ┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐

			for(int c = 0;c< availableWidth;c++) {

				var rune = Driver.HLine;

				if (Style.ShowVerticalHeaderLines){
							
					if(c == 0){
						rune = Driver.ULCorner;
					}	
					// if the next column is the start of a header
					else if(columnsToRender.Values.Contains(c+1)){
						rune = Driver.TopTee;
					}
					else if(c == availableWidth -1){
						rune = Driver.URCorner;
					}
				}

				AddRuneAt(Driver,c,row,rune);
			}
		}

		private void RenderHeaderMidline(int row,int availableWidth, Dictionary<DataColumn, int> columnsToRender)
		{
			// Renders something like:
			// │ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
						
			ClearLine(row,availableWidth);

			//render start of line
			if(style.ShowVerticalHeaderLines)
				AddRune(0,row,Driver.VLine);

			foreach (var kvp in columnsToRender) {
				
				//where the header should start
				var col = kvp.Value;

				RenderSeparator(col-1,row);
									
				Move (col, row);
				Driver.AddStr(Truncate (kvp.Key.ColumnName, availableWidth - kvp.Value));

			}

			//render end of line
			if(style.ShowVerticalHeaderLines)
				AddRune(availableWidth-1,row,Driver.VLine);
		}

		private void RenderHeaderUnderline(int row,int availableWidth, Dictionary<DataColumn, int> columnsToRender)
		{
			// Renders a line below the table headers (when visible) like:
			// ├──────────┼───────────┼───────────────────┼──────────┼────────┼─────────────┤
								
			for(int c = 0;c< availableWidth;c++) {

				var rune = Driver.HLine;

				if (Style.ShowVerticalHeaderLines){
					if(c == 0){
						rune = Style.ShowVerticalCellLines ? Driver.LeftTee : Driver.LLCorner;
					}	
					// if the next column is the start of a header
					else if(columnsToRender.Values.Contains(c+1)){
					
						/*TODO: is ┼ symbol in Driver?*/ 
						rune = Style.ShowVerticalCellLines ? '┼' :Driver.BottomTee;
					}
					else if(c == availableWidth -1){
						rune = Style.ShowVerticalCellLines ? Driver.RightTee : Driver.LRCorner;
					}
				}

				AddRuneAt(Driver,c,row,rune);
			}
			
		}
		private void RenderRow(int row, int availableWidth, int rowToRender, Dictionary<DataColumn, int> columnsToRender)
		{
			//render start of line
			if(style.ShowVerticalHeaderLines)
				AddRune(0,row,Driver.VLine);

			// Render cells for each visible header for the current row
			foreach (var kvp in columnsToRender) {

				// move to start of cell (in line with header positions)
				Move (kvp.Value, row);

				// Set color scheme based on whether the current cell is the selected one
				bool isSelectedCell = rowToRender == SelectedRow && kvp.Key.Ordinal == SelectedColumn;
				Driver.SetAttribute (isSelectedCell ? ColorScheme.HotFocus : ColorScheme.Normal);

				// Render the (possibly truncated) cell value
				var valueToRender = GetRenderedVal (Table.Rows [rowToRender] [kvp.Key]);
				Driver.AddStr (Truncate (valueToRender, availableWidth - kvp.Value));
				
				// Reset color scheme to normal and render the vertical line (or space) at the end of the cell
				Driver.SetAttribute (ColorScheme.Normal);
				RenderSeparator(kvp.Value-1,row);
			}

			//render end of line
			if(style.ShowVerticalHeaderLines)
				AddRune(availableWidth-1,row,Driver.VLine);
		}
		
		private void RenderSeparator(int col, int row)
		{
			if(col<0)
				return;

			Rune symbol = style.ShowVerticalHeaderLines ? Driver.VLine : SeparatorSymbol;
			AddRune(col,row,symbol);
		}

		void AddRuneAt (ConsoleDriver d,int col, int row, Rune ch)
		{
			Move (col, row);
			d.AddRune (ch);
		}

		/// <summary>
		/// Truncates <paramref name="valueToRender"/> so that it occupies a maximum of <paramref name="availableHorizontalSpace"/>
		/// </summary>
		/// <param name="valueToRender"></param>
		/// <param name="availableHorizontalSpace"></param>
		/// <returns></returns>
		private ustring Truncate (string valueToRender, int availableHorizontalSpace)
		{
			if (string.IsNullOrEmpty (valueToRender) || valueToRender.Length < availableHorizontalSpace)
				return valueToRender;

			return valueToRender.Substring (0, availableHorizontalSpace);
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
			case Key.CursorLeft:
				SelectedColumn--;
				Update ();
				break;
			case Key.CursorRight:
				SelectedColumn++;
				Update ();
				break;
			case Key.CursorDown:
				SelectedRow++;
				Update ();
				break;
			case Key.CursorUp:
				SelectedRow--;
				Update ();
				break;
			case Key.PageUp:
				SelectedRow -= Frame.Height;
				Update ();
				break;
			case Key.PageDown:
				SelectedRow += Frame.Height;
				Update ();
				break;
			case Key.Home | Key.CtrlMask:
				SelectedRow = 0;
				SelectedColumn = 0;
				Update ();
				break;
			case Key.Home:
				SelectedColumn = 0;
				Update ();
				break;
			case Key.End | Key.CtrlMask:
				//jump to end of table
				SelectedRow =  Table == null ? 0 : Table.Rows.Count - 1;
				SelectedColumn =  Table == null ? 0 : Table.Columns.Count - 1;
				Update ();
				break;
			case Key.End:
				//jump to end of row
				SelectedColumn =  Table == null ? 0 : Table.Columns.Count - 1;
				Update ();
				break;
			default:
				// Not a keystroke we care about
				return false;
			}
			PositionCursor ();
			return true;
		}

		/// <summary>
		/// Updates the view to reflect changes to <see cref="Table"/> and to (<see cref="ColumnOffset"/> / <see cref="RowOffset"/>) etc
		/// </summary>
		/// <remarks>This always calls <see cref="View.SetNeedsDisplay()"/></remarks>
		public void Update()
		{
			if(Table == null) {
				SetNeedsDisplay ();
				return;
			}

			//if user opened a large table scrolled down a lot then opened a smaller table (or API deleted a bunch of columns without telling anyone)
			ColumnOffset = Math.Max(Math.Min(ColumnOffset,Table.Columns.Count -1),0);
			RowOffset = Math.Max(Math.Min(RowOffset,Table.Rows.Count -1),0);
			SelectedColumn = Math.Max(Math.Min(SelectedColumn,Table.Columns.Count -1),0);
			SelectedRow = Math.Max(Math.Min(SelectedRow,Table.Rows.Count -1),0);

			Dictionary<DataColumn, int> columnsToRender = CalculateViewport (Bounds);
			var headerHeight = GetHeaderHeight();

			//if we have scrolled too far to the left 
			if (SelectedColumn < columnsToRender.Keys.Min (col => col.Ordinal)) {
				ColumnOffset = SelectedColumn;
			}

			//if we have scrolled too far to the right
			if (SelectedColumn > columnsToRender.Keys.Max (col => col.Ordinal)) {
				ColumnOffset = SelectedColumn;
			}

			//if we have scrolled too far down
			if (SelectedRow >= RowOffset + (Bounds.Height - headerHeight)) {
				RowOffset = SelectedRow;
			}
			//if we have scrolled too far up
			if (SelectedRow < RowOffset) {
				RowOffset = SelectedRow;
			}

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Calculates which columns should be rendered given the <paramref name="bounds"/> in which to display and the <see cref="ColumnOffset"/>
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="padding"></param>
		/// <returns></returns>
		private Dictionary<DataColumn, int> CalculateViewport (Rect bounds, int padding = 1)
		{
			Dictionary<DataColumn, int> toReturn = new Dictionary<DataColumn, int> ();

			if(Table == null)
				return toReturn;
			
			int usedSpace = 0;

			//if horizontal space is required at the start of the line (before the first header)
			if(Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines)
				usedSpace+=2;
			
			int availableHorizontalSpace = bounds.Width;
			int rowsToRender = bounds.Height;

			// reserved for the headers row
			if(ShouldRenderHeaders())
				rowsToRender -= GetHeaderHeight(); 

			bool first = true;

			foreach (var col in Table.Columns.Cast<DataColumn>().Skip (ColumnOffset)) {

				int startingIdxForCurrentHeader = usedSpace;

				// is there enough space for this column (and it's data)?
				usedSpace += CalculateMaxRowSize (col, rowsToRender) + padding;

				// no (don't render it) unless its the only column we are render (that must be one massively wide column!)
				if (!first && usedSpace > availableHorizontalSpace)
					return toReturn;

				// there is space
				toReturn.Add (col, startingIdxForCurrentHeader);
				first=false;
			}

			return toReturn;
		}

		private bool ShouldRenderHeaders()
		{
		    return Style.AlwaysShowHeaders || rowOffset == 0;
		}

		/// <summary>
		/// Returns the maximum of the <paramref name="col"/> name and the maximum length of data that will be rendered starting at <see cref="RowOffset"/> and rendering <paramref name="rowsToRender"/>
		/// </summary>
		/// <param name="col"></param>
		/// <param name="rowsToRender"></param>
		/// <returns></returns>
		private int CalculateMaxRowSize (DataColumn col, int rowsToRender)
		{
			int spaceRequired = col.ColumnName.Length;

			// if table has no rows
			if(RowOffset < 0)
				return spaceRequired;


			for (int i = RowOffset; i < RowOffset + rowsToRender && i < Table.Rows.Count; i++) {

				//expand required space if cell is bigger than the last biggest cell or header
				spaceRequired = Math.Max (spaceRequired, GetRenderedVal (Table.Rows [i] [col]).Length);
			}

			return spaceRequired;
		}

		/// <summary>
		/// Returns the value that should be rendered to best represent a strongly typed <paramref name="value"/> read from <see cref="Table"/>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private string GetRenderedVal (object value)
		{
			if (value == null || value == DBNull.Value) {
				return NullSymbol;
			}

			var representation = value.ToString ();

			//if it is too long to fit
			if (representation.Length > MaximumCellWidth)
				return representation.Substring (0, MaximumCellWidth);

			return representation;
		}
	}
}
