using System;
using System.Collections;
using NStack;

namespace Terminal.Gui
{

    public partial class ListView
    {
        //
        // This class is the built-in IListDataSource that renders arbitrary
        // IList instances
        //
        class ListWrapper : IListDataSource {
			IList src;
			BitArray marks;
			int count;

			public ListWrapper (IList source)
			{
				count = source.Count;
				marks = new BitArray (count);
				this.src = source;
			}

			public int Count => src.Count;

			void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width)
			{
				int byteLen = ustr.Length;
				int used = 0;
				for (int i = 0; i < byteLen;) {
					(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
					var count = Rune.ColumnWidth (rune);
					if (used+count >= width)
						break;
					driver.AddRune (rune);
					used += count;
					i += size;
				}
				for (; used < width; used++) {
					driver.AddRune (' ');
				}
			}

			public void Render (ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width)
			{
				container.Move (col, line);
				var t = src [item];
				if (t is ustring) {
					RenderUstr (driver, (ustring)t, col, line, width);
				} else if (t is string) {
					RenderUstr (driver, (string)t, col, line, width);
				} else
					RenderUstr (driver, t.ToString (), col, line, width);
			}

			public bool IsMarked (int item)
			{
				if (item >= 0 && item < count)
					return marks [item];
				return false;
			}

			public void SetMark (int item, bool value)
			{
				if (item >= 0 && item < count)
					marks [item] = value;
			}
		}
	}
}
