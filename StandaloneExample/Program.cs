namespace StandaloneExample {
	using System.Linq;
	using Terminal.Gui;
	using System;
	using NStack;
	using System.Text;
	using Rune = System.Rune;
	using System.Runtime.InteropServices;
	using System.Diagnostics;

	static class Demo {
		class Box10x : View {
			public Box10x (int x, int y) : base (new Rect (x, y, 10, 10))
			{
			}

			public override void Redraw (Rect region)
			{
				Driver.SetAttribute (ColorScheme.Focus);

				for (int y = 0; y < 10; y++) {
					Move (0, y);
					for (int x = 0; x < 10; x++) {
						Driver.AddRune ((Rune)('0' + ((x + y) % 10)));
					}
				}
			}
		}

		class Filler : View {
			public Filler (Rect rect) : base (rect)
			{
			}

			public override void Redraw (Rect region)
			{
				Driver.SetAttribute (ColorScheme.Focus);
				var f = Frame;

				for (int y = 0; y < f.Width; y++) {
					Move (0, y);
					for (int x = 0; x < f.Height; x++) {
						var r = (x % 3) switch {
							0 => '.',
							1 => 'o',
							_ => 'O',
						};
						Driver.AddRune (r);
					}
				}
			}
		}

		static void ShowTextAlignments ()
		{
			var container = new Window ("Show Text Alignments - Press Esc to return") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			container.KeyUp += (e) => {
				if (e.KeyEvent.Key == Key.Esc)
					container.Running = false;
			};

			const int i = 0;
			const string txt = "Hello world, how are you doing today?";
			container.Add (
					new Label ($"{i + 1}-{txt}") { TextAlignment = TextAlignment.Left, Y = 3, Width = Dim.Fill () },
					new Label ($"{i + 2}-{txt}") { TextAlignment = TextAlignment.Right, Y = 5, Width = Dim.Fill () },
					new Label ($"{i + 3}-{txt}") { TextAlignment = TextAlignment.Centered, Y = 7, Width = Dim.Fill () },
					new Label ($"{i + 4}-{txt}") { TextAlignment = TextAlignment.Justified, Y = 9, Width = Dim.Fill () }
				);

			Application.Run (container);
		}

		static void ShowEntries (View container)
		{
			scrollView = new ScrollView (new Rect (50, 10, 20, 8)) {
				ContentSize = new Size (100, 100),
				ContentOffset = new Point (-1, -1),
				ShowVerticalScrollIndicator = true,
				ShowHorizontalScrollIndicator = true
			};

			AddScrollViewChild ();

			// This is just to debug the visuals of the scrollview when small
			var scrollView2 = new ScrollView (new Rect (72, 10, 3, 3)) {
				ContentSize = new Size (100, 100),
				ShowVerticalScrollIndicator = true,
				ShowHorizontalScrollIndicator = true
			};
			scrollView2.Add (new Box10x (0, 0));
			var progress = new ProgressBar (new Rect (68, 1, 10, 1));
			bool timer (MainLoop _)
			{
				progress.Pulse ();
				return true;
			}

			Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), timer);

			// A little convoluted, this is because I am using this to test the
			// layout based on referencing elements of another view:

			var login = new Label ("Login: ") { X = 3, Y = 6 };
			var password = new Label ("Password: ") {
				X = Pos.Left (login),
				Y = Pos.Bottom (login) + 1
			};
			var loginText = new TextField ("") {
				X = Pos.Right (password),
				Y = Pos.Top (login),
				Width = 40
			};
			var passText = new TextField ("") {
				Secret = true,
				X = Pos.Left (loginText),
				Y = Pos.Top (password),
				Width = Dim.Width (loginText)
			};

			// Add some content
			container.Add (
				login,
				loginText,
				password,
				passText,
				new FrameView (new Rect (3, 10, 25, 6), "Options", new View [] {
				new CheckBox (1, 0, "Remember me"),
				new RadioGroup (1, 2, new ustring [] { "_Personal", "_Company" }) }
				),
				new ListView (new Rect (60, 6, 16, 4), new string [] {
				"First row",
				"<>",
				"This is a very long row that should overflow what is shown",
				"4th",
				"There is an empty slot on the second row",
				"Whoa",
				"This is so cool"
				}),
				scrollView,
				scrollView2,
				new Button ("Ok") { X = 3, Y = 19 },
				new Button ("Cancel") { X = 10, Y = 19 },
				progress,
				new Label ("Press F9 (on Unix ESC+9 is an alias) to activate the menubar") { X = 3, Y = 22 }
			);
		}

		private static void AddScrollViewChild ()
		{
			if (isBox10x) {
				scrollView.Add (new Box10x (0, 0));
			} else {
				scrollView.Add (new Filler (new Rect (0, 0, 40, 40)));
			}
			scrollView.ContentOffset = Point.Empty;
		}

		static void NewFile ()
		{
			var okButton = new Button ("Ok", is_default: true);
			okButton.Clicked += () => Application.RequestStop ();
			var cancelButton = new Button ("Cancel");
			cancelButton.Clicked += () => Application.RequestStop ();

			var d = new Dialog (
			    "New File", 50, 20,
			    okButton,
			    cancelButton);

			var ml2 = new Label (1, 1, "Mouse Debug Line");
			d.Add (ml2);
			Application.Run (d);
		}

		static bool Quit ()
		{
			var n = MessageBox.Query (50, 7, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
			return n == 0;
		}

		static void Close ()
		{
			MessageBox.ErrorQuery (50, 7, "Error", "There is nothing to close", "Ok");
		}

		private static void ScrollViewCheck ()
		{
			isBox10x = miScrollViewCheck.Children [0].Checked = !miScrollViewCheck.Children [0].Checked;
			miScrollViewCheck.Children [1].Checked = !miScrollViewCheck.Children [1].Checked;

			scrollView.RemoveAll ();
			AddScrollViewChild ();
		}

		private static void OpenUrl (string url)
		{
			try {
				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					url = url.Replace ("&", "^&");
					Process.Start (new ProcessStartInfo ("cmd", $"/c start {url}") { CreateNoWindow = true });
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					using (var process = new Process {
						StartInfo = new ProcessStartInfo {
							FileName = "xdg-open",
							Arguments = url,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
							UseShellExecute = false
						}
					}) {
						process.Start ();
					}
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					Process.Start ("open", url);
				}
			} catch {
				throw;
			}
		}

		public static Label ml;
		private static MenuBarItem miScrollViewCheck;
		private static bool isBox10x = true;
		private static Window win;
		private static ScrollView scrollView;

		static void Main (string [] args)
		{
			if (args.Length > 0 && args.Contains ("-usc")) {
				Application.UseSystemConsole = true;
			}

			Console.OutputEncoding = Encoding.Default;

			Application.Init ();

			var top = Application.Top;

			win = new Window ("Hello") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill () - 1
			};

			StringBuilder aboutMessage = new StringBuilder ();
			aboutMessage.AppendLine (@"");
			aboutMessage.AppendLine (@"UI Catalog is a comprehensive sample library for Terminal.Gui");
			aboutMessage.AppendLine (@"");
			aboutMessage.AppendLine (@"  _______                  _             _   _____       _ ");
			aboutMessage.AppendLine (@" |__   __|                (_)           | | / ____|     (_)");
			aboutMessage.AppendLine (@"    | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ ");
			aboutMessage.AppendLine (@"    | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |");
			aboutMessage.AppendLine (@"    | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |");
			aboutMessage.AppendLine (@"    |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|");
			aboutMessage.AppendLine (@"");
			aboutMessage.AppendLine ($"Using Terminal.Gui Version: {FileVersionInfo.GetVersionInfo (typeof (Terminal.Gui.Application).Assembly.Location).FileVersion}");
			aboutMessage.AppendLine (@"");

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "Creates new file", NewFile),
					new MenuItem ("_Open", "", null),
					new MenuItem ("_Close", "", () => Close ()),
					new MenuItem ("_Quit", "", () => { if (Quit ()) top.Running = false; })
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				}),
				new MenuBarItem ("A_ssorted", new MenuItem [] {
					new MenuItem ("_Show text alignments", "", () => ShowTextAlignments (), null, null, Key.AltMask | Key.CtrlMask | Key.G)
				}),
				miScrollViewCheck = new MenuBarItem ("ScrollView", new MenuItem [] {
					new MenuItem ("Box10x", "", () => ScrollViewCheck()) {CheckType = MenuItemCheckStyle.Radio, Checked = true },
					new MenuItem ("Filler", "", () => ScrollViewCheck()) {CheckType = MenuItemCheckStyle.Radio }
				}),
				new MenuBarItem ("_Help", new MenuItem [] {
					new MenuItem ("_gui.cs API Overview", "", () => OpenUrl ("https://migueldeicaza.github.io/gui.cs/articles/overview.html"), null, null, Key.F1),
					new MenuItem ("gui.cs _README", "", () => OpenUrl ("https://github.com/migueldeicaza/gui.cs"), null, null, Key.F2),
					new MenuItem ("_About...", "About this app", () =>  MessageBox.Query (aboutMessage.Length + 2, 15, "About", aboutMessage.ToString(), "_Ok"), null, null, Key.CtrlMask | Key.A),
				})
			});

			ShowEntries (win);
			int count = 0;
			ml = new Label (new Rect (3, 17, 47, 1), "Mouse: ");
			Application.RootMouseEvent += (me) => ml.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";

			win.Add (ml);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F1, "~F1~ Help", () => MessageBox.Query (50, 7, "Help", "Helping", "Ok")),
				new StatusItem(Key.F2, "~F2~ Load", () => MessageBox.Query (50, 7, "Load", "Loading", "Ok")),
				new StatusItem(Key.F3, "~F3~ Save", () => MessageBox.Query (50, 7, "Save", "Saving", "Ok")),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => { if (Quit ()) top.Running = false; }),
				new StatusItem(Key.Null, Application.Driver.GetType().Name, null)
			});

			top.Add (win, menu, statusBar);
			Application.Run ();

			Application.Shutdown ();
		}
	}
}