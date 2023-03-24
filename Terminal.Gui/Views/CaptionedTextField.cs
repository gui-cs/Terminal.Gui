using NStack;

namespace Terminal.Gui {

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
}