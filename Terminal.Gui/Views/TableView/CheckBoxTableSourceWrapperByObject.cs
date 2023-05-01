using System;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Implementation of <see cref="CheckBoxTableSourceWrapperBase"/> which records toggled rows
	/// by a property on row objects.
	/// </summary>
	public class CheckBoxTableSourceWrapperByObject<T> : CheckBoxTableSourceWrapperBase {
		private readonly EnumerableTableSource<T> toWrap;
		readonly Func<T, bool> getter;
		readonly Action<T, bool> setter;

		public CheckBoxTableSourceWrapperByObject (
			TableView tableView,
			EnumerableTableSource<T> toWrap,
			Func<T,bool> getter,
			Action<T,bool> setter) : base (tableView, toWrap)
		{
			this.toWrap = toWrap;
			this.getter = getter;
			this.setter = setter;
		}

		protected override bool IsChecked (int row)
		{
			return getter (toWrap.Data.ElementAt (row));
		}

		protected override void ToggleAllRows ()
		{
			ToggleRows (Enumerable.Range (0, toWrap.Rows).ToArray());
		}

		protected override void ToggleRow (int row)
		{
			var d = toWrap.Data.ElementAt (row);
			setter (d, !getter(d));
		}

		protected override void ToggleRows (int [] range)
		{
			// if all are ticked untick them
			if (range.All (IsChecked)) {
				// select none
				foreach (var r in range) {
					setter (toWrap.Data.ElementAt (r), false);
				}
			} else {
				// otherwise tick all
				foreach (var r in range) {
					setter (toWrap.Data.ElementAt (r), true);
				}
			}
		}
	}
}
