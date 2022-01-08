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

		private Stream LoadStream ()
		{
			MemoryStream stream = new MemoryStream ();
			UnicodeEncoding encoding = new UnicodeEncoding ();
			byte [] bArray = encoding.GetBytes (
				@"Hello world.\n
This is a test of the Emergency Broadcast System.\n
");

			stream.Write (bArray);

			return stream;
		}

		[Fact]
		public void AllowEdits_Edits_ApplyEdits ()
		{
			var hv = new HexView (LoadStream ()) {
				Width = 20,
				Height = 20
			};

			Assert.Empty (hv.Edits);
			hv.AllowEdits = false;
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.False (hv.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ())));
			Assert.Empty (hv.Edits);
			Assert.Equal (138, hv.Source.Length);

			hv.AllowEdits = true;
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Single (hv.Edits);
			Assert.Equal (65, hv.Edits.ToList () [0].Value);
			Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
			Assert.Equal (138, hv.Source.Length);

			// Appends byte
			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal (2, hv.Edits.Count);
			Assert.Equal (66, hv.Edits.ToList () [1].Value);
			Assert.Equal ('B', (char)hv.Edits.ToList () [1].Value);
			Assert.Equal (138, hv.Source.Length);

			hv.ApplyEdits ();
			Assert.Empty (hv.Edits);
			Assert.Equal (139, hv.Source.Length);
		}

		[Fact]
		public void DisplayStart_Source ()
		{
			var hv = new HexView (LoadStream ()) {
				Width = 20,
				Height = 20
			};

			Assert.Equal (0, hv.DisplayStart);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (4 * hv.Frame.Height, hv.DisplayStart);
			Assert.Equal (hv.Source.Length, hv.Source.Position);


			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (hv.Source.Length - 1, hv.DisplayStart);
			Assert.Equal (hv.Source.Length, hv.Source.Position);
		}

		[Fact]
		public void Edited_Event ()
		{
			var hv = new HexView (LoadStream ()) { Width = 20, Height = 20 };
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
			var hv = new HexView (LoadStream ()) { Width = 20, Height = 20 };
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Single (hv.Edits);
			Assert.Equal (65, hv.Edits.ToList () [0].Value);
			Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
			Assert.Equal (138, hv.Source.Length);

			hv.DiscardEdits ();
			Assert.Empty (hv.Edits);
		}
	}
}
