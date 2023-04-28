﻿using System;
using System.Linq;

namespace Terminal.Gui {
	internal class FileDialogTableSource : ITableSource {
		readonly FileDialogStyle style;
		readonly int currentSortColumn;
		readonly bool currentSortIsAsc;
		readonly FileDialogState state;

		public FileDialogTableSource (FileDialogState state, FileDialogStyle style, int currentSortColumn, bool currentSortIsAsc)
		{
			this.style = style;
			this.currentSortColumn = currentSortColumn;
			this.currentSortIsAsc = currentSortIsAsc;
			this.state = state;
		}

		public object this [int row, int col] => GetColumnValue(col, state.Children [row]);

		private object GetColumnValue (int col, FileSystemInfoStats stats)
		{
			switch(col) {
				case 0:
					var icon = stats.IsParent ? null : style.IconGetter?.Invoke (stats.FileSystemInfo);
				return icon + (stats?.Name ?? string.Empty);
				case 1:
					return stats?.HumanReadableLength ?? string.Empty;
				case 2:
					if (stats == null || stats.IsParent || stats.LastWriteTime == null) {
						return string.Empty;
					}
					return stats.LastWriteTime.Value.ToString (style.DateFormat);
				case 3:
					return stats?.Type ?? string.Empty;
				default:
					throw new ArgumentOutOfRangeException (nameof (col));
			}
		}

		public int Rows => state.Children.Count();

		public int Columns => 4;

		public string [] ColumnNames => new string []{
			MaybeAddSortArrows(style.FilenameColumnName,0),
			MaybeAddSortArrows(style.SizeColumnName,1),
			MaybeAddSortArrows(style.ModifiedColumnName,2),
			MaybeAddSortArrows(style.TypeColumnName,3)
		};

		private string MaybeAddSortArrows (string name, int idx)
		{
			if(idx == currentSortColumn)
			{
				return name + (currentSortIsAsc ? " (▲)" : " (▼)");
			}

			return name;
		}
	}
}