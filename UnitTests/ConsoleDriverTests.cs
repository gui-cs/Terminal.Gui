using System;
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
		public void FakeDriver_Always_Sends_ConsoleKey_Oem3_Without_Using_MockKeyPresses ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var top = Application.Top;
			var view = new View ();
			var oem3 = 0;

			view.KeyPress += (e) => {
				if (oem3 < 10) {
					Assert.Equal ('~', (uint)e.KeyEvent.Key);
					oem3++;
				} else {
					Application.RequestStop ();
				}
				e.Handled = true;
			};
			top.Add (view);

			Application.Run ();

			Assert.Equal (10, oem3);
		}

		[Fact]
		public void FakeDriver_MockKeyPresses ()
		{
			var text = "MockKeyPresses";
			var mKeys = new System.Collections.Generic.Stack<ConsoleKeyInfo> ();
			foreach (var r in text.Reverse ()) {
				var ck = char.IsLetter (r) ? (ConsoleKey)char.ToUpper (r) : (ConsoleKey)r;
				var cki = new ConsoleKeyInfo (r, ck, false, false, false);
				mKeys.Push (cki);
			}
			Console.MockKeyPresses = mKeys;

			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var top = Application.Top;
			var view = new View ();
			var rText = "";
			var idx = 0;

			view.KeyPress += (e) => {
				Assert.Equal (text [idx], (char)e.KeyEvent.Key);
				rText += (char)e.KeyEvent.Key;
				Assert.Equal (rText, text.Substring (0, idx + 1));
				e.Handled = true;
				if (rText == text) {
					Application.RequestStop ();
				}
				idx++;
			};
			top.Add (view);

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
			var wasSendKeysProcessed = false;

			view.KeyPress += (e) => {
				e.Handled = true;
				if ((char)e.KeyEvent.Key == '~') {
					return;
				}
				var rMk = new KeyModifiers () {
					Shift = e.KeyEvent.IsShift,
					Alt = e.KeyEvent.IsAlt,
					Ctrl = e.KeyEvent.IsCtrl
				};
				Assert.Equal (key, ShortcutHelper.GetModifiersKey (new KeyEvent (e.KeyEvent.Key, rMk)));
				wasSendKeysProcessed = true;
			};
			top.Add (view);

			Application.Iteration += () => {
				for (int i = 0; i < 4; i++) {
					switch (i) {
					case 1:
						shift = true;
						break;
					case 2:
						alt = true;
						break;
					case 3:
						control = true;
						break;
					}
					foreach (Key k in Enum.GetValues (typeof (Key))) {
						if (k == Key.Null) {
							continue;
						} else if ((uint)k > 255) {
							break;
						}
						var c = (char)k;
						var ck = char.IsLetter (c) ? (ConsoleKey)char.ToUpper (c) : (ConsoleKey)c;
						var mk = new KeyModifiers () {
							Shift = shift,
							Alt = alt,
							Ctrl = control
						};
						key = ShortcutHelper.GetModifiersKey (new KeyEvent (k, mk));
						Application.Driver.SendKeys (c, ck, shift, alt, control);
					}
				}
				Application.RequestStop ();
			};

			Application.Run ();

			Assert.True (wasSendKeysProcessed);
		}
	}
}
