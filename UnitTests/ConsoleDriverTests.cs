using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	public class ConsoleDriverTests {
		[Fact]
		public void Init_Inits ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);

			// MockDriver is always 80x25
			Assert.Equal (Console.BufferWidth, driver.Cols);
			Assert.Equal (Console.BufferHeight, driver.Rows);
			driver.End ();
		}

		[Fact]
		public void End_Cleans_Up ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			FakeConsole.ForegroundColor = ConsoleColor.Red;
			Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);

			FakeConsole.BackgroundColor = ConsoleColor.Green;
			Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);
			driver.Move (2, 3);
			Assert.Equal (2, Console.CursorLeft);
			Assert.Equal (3, Console.CursorTop);

			driver.End ();
			Assert.Equal (0, Console.CursorLeft);
			Assert.Equal (0, Console.CursorTop);
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);
		}

		[Fact]
		public void SetColors_Changes_Colors ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);

			Console.ForegroundColor = ConsoleColor.Red;
			Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);

			Console.BackgroundColor = ConsoleColor.Green;
			Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);

			Console.ResetColor ();
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);
			driver.End ();
		}

		[Fact]
		public void FakeDriver_Only_Sends_Keystrokes_Through_MockKeyPresses ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var top = Application.Top;
			var view = new View ();
			var count = 0;
			var wasKeyPressed = false;

			view.KeyPress += (e) => {
				wasKeyPressed = true;
			};
			top.Add (view);

			Application.Iteration += () => {
				count++;
				if (count == 10) {
					Application.RequestStop ();
				}
			};

			Application.Run ();

			Assert.False (wasKeyPressed);
		}

		[Fact]
		public void FakeDriver_MockKeyPresses ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var text = "MockKeyPresses";
			var mKeys = new Stack<ConsoleKeyInfo> ();
			foreach (var r in text.Reverse ()) {
				var ck = char.IsLetter (r) ? (ConsoleKey)char.ToUpper (r) : (ConsoleKey)r;
				var cki = new ConsoleKeyInfo (r, ck, false, false, false);
				mKeys.Push (cki);
			}
			FakeConsole.MockKeyPresses = mKeys;

			var top = Application.Top;
			var view = new View ();
			var rText = "";
			var idx = 0;

			view.KeyPress += (e) => {
				Assert.Equal (text [idx], (char)e.KeyEvent.Key);
				rText += (char)e.KeyEvent.Key;
				Assert.Equal (rText, text.Substring (0, idx + 1));
				e.Handled = true;
				idx++;
			};
			top.Add (view);

			Application.Iteration += () => {
				if (mKeys.Count == 0) {
					Application.RequestStop ();
				}
			};

			Application.Run ();

			Assert.Equal ("MockKeyPresses", rText);
		}

		[Fact]
		public void SendKeys_Test ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var top = Application.Top;
			var view = new View ();
			var shift = false; var alt = false; var control = false;
			Key key = default;
			Key lastKey = default;
			List<Key> keyEnums = GetKeys ();
			int i = 0;
			int idxKey = 0;
			var PushIterations = 0;
			var PopIterations = 0;

			List<Key> GetKeys ()
			{
				List<Key> keys = new List<Key> ();

				foreach (Key k in Enum.GetValues (typeof (Key))) {
					if ((uint)k <= 0xff) {
						keys.Add (k);
					} else if ((uint)k > 0xff) {
						break;
					}
				}

				return keys;
			}

			view.KeyPress += (e) => {
				e.Handled = true;
				PopIterations++;
				var rMk = new KeyModifiers () {
					Shift = e.KeyEvent.IsShift,
					Alt = e.KeyEvent.IsAlt,
					Ctrl = e.KeyEvent.IsCtrl
				};
				lastKey = ShortcutHelper.GetModifiersKey (new KeyEvent (e.KeyEvent.Key, rMk));
				Assert.Equal (key, lastKey);
			};
			top.Add (view);

			Application.Iteration += () => {
				switch (i) {
				case 0:
					SendKeys ();
					break;
				case 1:
					shift = true;
					SendKeys ();
					break;
				case 2:
					alt = true;
					SendKeys ();
					break;
				case 3:
					control = true;
					SendKeys ();
					break;
				}
				if (PushIterations == keyEnums.Count * 4) {
					Application.RequestStop ();
				}
			};

			void SendKeys ()
			{
				var k = keyEnums [idxKey];
				var c = (char)k;
				var ck = char.IsLetter (c) ? (ConsoleKey)char.ToUpper (c) : (ConsoleKey)c;
				var mk = new KeyModifiers () {
					Shift = shift,
					Alt = alt,
					Ctrl = control
				};
				key = ShortcutHelper.GetModifiersKey (new KeyEvent (k, mk));
				Application.Driver.SendKeys (c, ck, shift, alt, control);
				PushIterations++;
				if (idxKey + 1 < keyEnums.Count) {
					idxKey++;
				} else {
					idxKey = 0;
					i++;
				}
			}

			Application.Run ();

			Assert.Equal (key, lastKey);
		}
	}
}
