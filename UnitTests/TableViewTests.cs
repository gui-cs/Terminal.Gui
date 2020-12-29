using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;

namespace UnitTests {
	public class TableViewTests 
	{

        [Fact]
        public void EnsureValidScrollOffsets_WithNoCells()
        {
            var tableView = new TableView();

            Assert.Equal(0,tableView.RowOffset);
            Assert.Equal(0,tableView.ColumnOffset);

            // Set empty table
            tableView.Table = new DataTable();

            // Since table has no rows or columns scroll offset should default to 0
            tableView.EnsureValidScrollOffsets();
            Assert.Equal(0,tableView.RowOffset);
            Assert.Equal(0,tableView.ColumnOffset);
        }



        [Fact]
        public void EnsureValidScrollOffsets_LoadSmallerTable()
        {
            var tableView = new TableView();
            tableView.Bounds = new Rect(0,0,25,10);

            Assert.Equal(0,tableView.RowOffset);
            Assert.Equal(0,tableView.ColumnOffset);

            // Set big table
            tableView.Table = BuildTable(25,50);

            // Scroll down and along
            tableView.RowOffset = 20;
            tableView.ColumnOffset = 10;

            tableView.EnsureValidScrollOffsets();

            // The scroll should be valid at the moment
            Assert.Equal(20,tableView.RowOffset);
            Assert.Equal(10,tableView.ColumnOffset);

            // Set small table
            tableView.Table = BuildTable(2,2);

            // Setting a small table should automatically trigger fixing the scroll offsets to ensure valid cells
            Assert.Equal(0,tableView.RowOffset);
            Assert.Equal(0,tableView.ColumnOffset);


            // Trying to set invalid indexes should not be possible
            tableView.RowOffset = 20;
            tableView.ColumnOffset = 10;

            Assert.Equal(1,tableView.RowOffset);
            Assert.Equal(1,tableView.ColumnOffset);
        }
        
        /// <summary>
		/// Builds a simple table of string columns with the requested number of columns and rows
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static DataTable BuildTable(int cols, int rows)
		{
			var dt = new DataTable();

			for(int c = 0; c < cols; c++) {
				dt.Columns.Add("Col"+c);
			}
				
			for(int r = 0; r < rows; r++) {
				var newRow = dt.NewRow();

				for(int c = 0; c < cols; c++) {
					newRow[c] = $"R{r}C{c}";
				}

				dt.Rows.Add(newRow);
			}
			
			return dt;
		}
	}
}