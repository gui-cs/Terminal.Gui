using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.Views {

	/// <summary>
	/// View for tabular data based on a <see cref="DataTable"/>
	/// </summary>
	public class TableView : View {

		private int columnOffset;
		private int rowOffset;

		public DataTable Table { get; private set; }

		/// <summary>
		/// Zero indexed offset for the upper left <see cref="DataColumn"/> to display in <see cref="Table"/>.
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int ColumnOffset {
			get => columnOffset; 

			//try to prevent this being set to an out of bounds column
			set => columnOffset = Math.Min (Table.Columns.Count - 1, Math.Max (0, value));
		}


		/// <summary>
		/// Zero indexed offset for the <see cref="DataRow"/> to display in <see cref="Table"/> on line 2 of the control (first line being headers)
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int RowOffset { 
			get => rowOffset; 
			set => rowOffset = Math.Min (Table.Rows.Count - 1, Math.Max (0, value));
			}

		/// <summary>
		/// The maximum number of characters to render in any given column.  This prevents one long column from pushing out all the others
		/// </summary>
		public int MaximumCellWidth {get;set;} = 100;

		/// <summary>
		/// The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/>
		/// </summary>
		public string NullSymbol {get;set;} = "-";

		/// <summary>
		/// Initialzies a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. 
		/// </summary>
		/// <param name="table">The table to display in the control</param>
		public TableView (DataTable table) : base ()
		{
			this.Table = table ?? throw new ArgumentNullException (nameof (table));
		}
		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Attribute currentAttribute;
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);

			var frame = Frame;

			int activeColor = ColorScheme.HotNormal;
			int trackingColor = ColorScheme.HotFocus;

			// What columns to render at what X offset in viewport
			Dictionary<DataColumn, int> columnsToRender = CalculateViewport(bounds);

			Driver.SetAttribute (ColorScheme.HotNormal);

			// Render the headers
			foreach(var kvp in columnsToRender) {
				
				Move (kvp.Value,0);
				Driver.AddStr(kvp.Key.ColumnName);
			}

			//render the cells
			for (int line = 1; line < frame.Height; line++) {
				
				//work out what Row to render
				var rowToRender = RowOffset + (line-1);
				if(rowToRender >= Table.Rows.Count)
					break;

				foreach(var kvp in columnsToRender) {
					Move (kvp.Value,line);
					Driver.AddStr(GetRenderedVal(Table.Rows[rowToRender][kvp.Key]));
				}
			}

			/*

			for (int line = 1; line < frame.Height; line++) {
				var lineRect = new Rect (0, line, frame.Width, 1);
				if (!bounds.Contains (lineRect))
					continue;

				Move (0, line);
				Driver.SetAttribute (ColorScheme.HotNormal);
				Driver.AddStr ("test");

				currentAttribute = ColorScheme.HotNormal;
				SetAttribute (ColorScheme.Normal);
			}*/

			void SetAttribute (Attribute attribute)
			{
				if (currentAttribute != attribute) {
					currentAttribute = attribute;
					Driver.SetAttribute (attribute);
				}
			}

		}

		/// <summary>
		/// Calculates which columns should be rendered given the <paramref name="bounds"/> in which to display and the <see cref="ColumnOffset"/>
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="padding"></param>
		/// <returns></returns>
		private Dictionary<DataColumn,int> CalculateViewport(Rect bounds, int padding = 1)
		{
			Dictionary<DataColumn,int> toReturn = new Dictionary<DataColumn, int>();

			int usedSpace = 0;
			int availableHorizontalSpace = bounds.Width;
			int rowsToRender = bounds.Height-1; //1 reserved for the headers row
			
			foreach(var col in Table.Columns.Cast<DataColumn>().Skip(ColumnOffset)) {
				
				toReturn.Add(col,usedSpace);
				usedSpace += CalculateMaxRowSize(col,rowsToRender) + padding;

				if(usedSpace > availableHorizontalSpace)
					return toReturn;
				
			}
			
			return toReturn;
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

			for(int i = RowOffset; i<rowsToRender && i<Table.Rows.Count;i++) {

				//expand required space if cell is bigger than the last biggest cell or header
				spaceRequired = Math.Max(spaceRequired,GetRenderedVal(Table.Rows[i][col]).Length);
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
			if(value == null || value == DBNull.Value) 
			{
				return NullSymbol;
			}
			
			var representation = value.ToString();

			//if it is too long to fit
			if(representation.Length > MaximumCellWidth)
				return representation.Substring(0,MaximumCellWidth);

			return representation;
		}
	}
}
