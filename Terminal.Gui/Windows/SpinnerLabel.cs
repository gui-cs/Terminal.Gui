using System;

namespace Terminal.Gui {

	public partial class FileDialog {
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
	}
}