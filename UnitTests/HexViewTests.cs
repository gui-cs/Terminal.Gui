using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.Views {
	public class HexViewTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var hv = new HexView ();
			Assert.NotNull (hv.Source);
			Assert.IsAssignableFrom<System.IO.MemoryStream> (hv.Source);
			Assert.True (hv.CanFocus);
			Assert.True (hv.AllowEdits);

			hv = new HexView (new System.IO.MemoryStream ());
			Assert.NotNull (hv.Source);
			Assert.IsAssignableFrom<System.IO.Stream> (hv.Source);
			Assert.True (hv.CanFocus);
			Assert.True (hv.AllowEdits);
		}

		private Stream LoadStream (bool unicode = false)
		{
			MemoryStream stream = new MemoryStream ();
			byte [] bArray;
			string memString = "Hello world.\nThis is a test of the Emergency Broadcast System.\n";

			Assert.Equal (63, memString.Length);

			if (unicode) {
				bArray = Encoding.Unicode.GetBytes (memString);
				Assert.Equal (126, bArray.Length);
			} else {
				bArray = Encoding.Default.GetBytes (memString);
				Assert.Equal (63, bArray.Length);
			}
			stream.Write (bArray);

			return stream;
		}

		[Fact]
		public void AllowEdits_Edits_ApplyEdits ()
		{
			var hv = new HexView (LoadStream (true)) {
				Width = 20,
				Height = 20
			};

			Assert.Empty (hv.Edits);
			hv.AllowEdits = false;
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.False (hv.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ())));
			Assert.Empty (hv.Edits);
			Assert.Equal (126, hv.Source.Length);

			hv.AllowEdits = true;
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Single (hv.Edits);
			Assert.Equal (65, hv.Edits.ToList () [0].Value);
			Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
			Assert.Equal (126, hv.Source.Length);

			// Appends byte
			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal (2, hv.Edits.Count);
			Assert.Equal (66, hv.Edits.ToList () [1].Value);
			Assert.Equal ('B', (char)hv.Edits.ToList () [1].Value);
			Assert.Equal (126, hv.Source.Length);

			hv.ApplyEdits ();
			Assert.Empty (hv.Edits);
			Assert.Equal (127, hv.Source.Length);
		}

		[Fact]
		public void DisplayStart_Source ()
		{
			var hv = new HexView (LoadStream (true)) {
				Width = 20,
				Height = 20
			};

			Assert.Equal (0, hv.DisplayStart);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (4 * hv.Frame.Height, hv.DisplayStart);
			Assert.Equal (hv.Source.Length, hv.Source.Position);


			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			// already on last page and so the DisplayStart is the same as before
			Assert.Equal (4 * hv.Frame.Height, hv.DisplayStart);
			Assert.Equal (hv.Source.Length, hv.Source.Position);
		}

		[Fact]
		public void Edited_Event ()
		{
			var hv = new HexView (LoadStream (true)) { Width = 20, Height = 20 };
			KeyValuePair<long, byte> keyValuePair = default;
			hv.Edited += (e) => keyValuePair = e;

			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D6, new KeyModifiers ())));

			Assert.Equal (0, (int)keyValuePair.Key);
			Assert.Equal (70, (int)keyValuePair.Value);
		}

		[Fact]
		public void DiscardEdits_Method ()
		{
			var hv = new HexView (LoadStream (true)) { Width = 20, Height = 20 };
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Single (hv.Edits);
			Assert.Equal (65, hv.Edits.ToList () [0].Value);
			Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
			Assert.Equal (126, hv.Source.Length);

			hv.DiscardEdits ();
			Assert.Empty (hv.Edits);
		}

		[Fact]
		public void Position_Using_Encoding_Unicode ()
		{
			var hv = new HexView (LoadStream (true)) { Width = 20, Height = 20 };
			Assert.Equal (126, hv.Source.Length);
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// left side needed to press twice
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (1, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (2, hv.Position);

			// right side only needed to press one time
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (2, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// last position is equal to the source length
			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (127, hv.Position);
			Assert.Equal (hv.Position - 1, hv.Source.Length);
		}

		[Fact]
		public void Position_Using_Encoding_Default ()
		{
			var hv = new HexView (LoadStream ()) { Width = 20, Height = 20 };
			Assert.Equal (63, hv.Source.Length);
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// left side needed to press twice
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (1, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (2, hv.Position);

			// right side only needed to press one time
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (2, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// last position is equal to the source length
			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (64, hv.Position);
			Assert.Equal (hv.Position - 1, hv.Source.Length);
		}

		[Fact]
		[AutoInitShutdown]
		public void CursorPosition_Property ()
		{
			var hv = new HexView (LoadStream ()) { Width = Dim.Fill (), Height = Dim.Fill () };
			Application.Top.Add (hv);
			Application.Begin (Application.Top);

			Assert.Equal (new Point (1, 1), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ())));
			var bytesPerLine = hv.CursorPosition.X;
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (2, 1), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (2, 2), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			var col = hv.CursorPosition.X;
			var line = hv.CursorPosition.Y;
			var offset = (line - 1) * (bytesPerLine - col);
			Assert.Equal (hv.Position, col * line + offset);
		}
	}
}
