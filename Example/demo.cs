using Terminal.Gui;
using System;
using Mono.Terminal;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using NStack;

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

					Driver.AddRune ((Rune)('0' + (x + y) % 10));
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
					Rune r;
					switch (x % 3) {
					case 0:
						Driver.AddRune (y.ToString ().ToCharArray (0, 1) [0]);
						if (y > 9)
							Driver.AddRune (y.ToString ().ToCharArray (1, 1) [0]);
						r = '.';
						break;
					case 1:
						r = 'o';
						break;
					default:
						r = 'O';
						break;
					}
					Driver.AddRune (r);
				}
			}
		}
	}


	static void ShowTextAlignments (View container)
	{
		int i = 0;
		string txt = "Hello world, how are you doing today";
		container.Add (
			    new FrameView (new Rect (75, 1, txt.Length + 6, 20), "Text Alignments") {
			new Label(new Rect(0, 1, 40, 3), $"{i+1}-{txt}") { TextAlignment = TextAlignment.Left },
			new Label(new Rect(0, 5, 40, 3), $"{i+2}-{txt}") { TextAlignment = TextAlignment.Right },
			new Label(new Rect(0, 9, 40, 3), $"{i+3}-{txt}") { TextAlignment = TextAlignment.Centered },
			new Label(new Rect(0, 13, 40, 3), $"{i+4}-{txt}") { TextAlignment = TextAlignment.Justified }
		    });
	}

	static void ShowEntries (View container)
	{
		var scrollView = new ScrollView (new Rect (50, 10, 20, 8)) {
			ContentSize = new Size (100, 100),
			ContentOffset = new Point (-1, -1),
			ShowVerticalScrollIndicator = true,
			ShowHorizontalScrollIndicator = true
		};
#if false
		scrollView.Add (new Box10x (0, 0));
#else	
		scrollView.Add (new Filler (new Rect (0, 0, 40, 40)));
#endif

		// This is just to debug the visuals of the scrollview when small
		var scrollView2 = new ScrollView (new Rect (72, 10, 3, 3)) {
			ContentSize = new Size (100, 100),
			ShowVerticalScrollIndicator = true,
			ShowHorizontalScrollIndicator = true
		};
		scrollView2.Add (new Box10x (0, 0));
		var progress = new ProgressBar (new Rect (68, 1, 10, 1));
		bool timer (MainLoop caller)
		{
			progress.Pulse ();
			return true;
		}

		//Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), timer);


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
			new FrameView (new Rect (3, 10, 25, 6), "Options"){
				new CheckBox (1, 0, "Remember me"),
				new RadioGroup (1, 2, new [] { "_Personal", "_Company" }),
			},
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
			//scrollView2,
			new Button (3, 19, "Ok"),
			new Button (10, 19, "Cancel"),
			new TimeField (3, 20, DateTime.Now),
			new TimeField (23, 20, DateTime.Now, true),
			progress,
			new Label (3, 24, "Press F9 (on Unix, ESC+9 is an alias) to activate the menubar"),
			menuKeysStyle,
			menuAutoMouseNav

		);

	}

	public static Label ml2;

	static void NewFile ()
	{
		var d = new Dialog (
			"New File", 50, 20,
			new Button ("Ok", is_default: true) { Clicked = () => { Application.RequestStop (); } },
			new Button ("Cancel") { Clicked = () => { Application.RequestStop (); } });
		ml2 = new Label (1, 1, "Mouse Debug Line");
		d.Add (ml2);
		Application.Run (d);
	}

	// 
	// Creates a nested editor
	static void Editor (Toplevel top)
	{
		var tframe = top.Frame;
		var ntop = new Toplevel (tframe);
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Close", "", () => {Application.RequestStop ();}),
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", null),
				new MenuItem ("C_ut", "", null),
				new MenuItem ("_Paste", "", null)
			}),
		});
		ntop.Add (menu);

		string fname = null;
		foreach (var s in new [] { "/etc/passwd", "c:\\windows\\win.ini" })
			if (System.IO.File.Exists (s)) {
				fname = s;
				break;
			}

		var win = new Window (fname ?? "Untitled") {
			X = 0,
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		ntop.Add (win);

		var text = new TextView (new Rect (0, 0, tframe.Width - 2, tframe.Height - 3));

		if (fname != null)
			text.Text = System.IO.File.ReadAllText (fname);
		win.Add (text);

		Application.Run (ntop);
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

	// Watch what happens when I try to introduce a newline after the first open brace
	// it introduces a new brace instead, and does not indent.  Then watch me fight
	// the editor as more oddities happen.

	public static void Open ()
	{
		var d = new OpenDialog ("Open", "Open a file");
		Application.Run (d);

		MessageBox.Query (50, 7, "Selected File", string.Join (", ", d.FilePaths), "Ok");
	}

	public static void ShowHex (Toplevel top)
	{
		var tframe = top.Frame;
		var ntop = new Toplevel (tframe);
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Close", "", () => {Application.RequestStop ();}),
			}),
		});
		ntop.Add (menu);

		var win = new Window ("/etc/passwd") {
			X = 0,
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		ntop.Add (win);

		var source = System.IO.File.OpenRead ("/etc/passwd");
		var hex = new HexView (source) {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (hex);
		Application.Run (ntop);

	}

	public class MenuItemDetails : MenuItem {
		ustring title;
		string help;
		Action action;

		public MenuItemDetails (ustring title, string help, Action action) : base (title, help, action)
		{
			this.title = title;
			this.help = help;
			this.action = action;
		}

		public static MenuItemDetails Instance (MenuItem mi)
		{
			return (MenuItemDetails)mi.GetMenuItem ();
		}
	}

	public delegate MenuItem MenuItemDelegate (MenuItemDetails menuItem);

	public static void ShowMenuItem (MenuItem mi)
	{
		BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
		MethodInfo minfo = typeof (MenuItemDetails).GetMethod ("Instance", flags);
		MenuItemDelegate mid = (MenuItemDelegate)Delegate.CreateDelegate (typeof (MenuItemDelegate), minfo);
		MessageBox.Query (70, 7, mi.Title.ToString (),
			$"{mi.Title.ToString ()} selected. Is from submenu: {mi.GetMenuBarItem ()}", "Ok");
	}

	private static void MenuKeysStyle_Toggled (object sender, EventArgs e)
	{
		menu.UseKeysUpDownAsKeysLeftRight = menuKeysStyle.Checked;
	}

	private static void MenuAutoMouseNav_Toggled (object sender, EventArgs e)
	{
		menu.WantMousePositionReports = menuAutoMouseNav.Checked;
	}

	//private static TextField GetTextFieldSelText (View vt)
	//{
	//	TextField textField;
	//	foreach (View v in vt.Subviews) {
	//		if (v is TextField && ((TextField)v).SelText != "")
	//			return v as TextField;
	//		else
	//			textField = GetTextFieldSelText (v);
	//		if (textField != null)
	//			return textField;
	//	}
	//	return null;
	//}

	private static void Copy ()
	{
		TextField textField = menu.LastFocused as TextField;
		if (textField != null && textField.SelLength > 0) {
			Clipboard.Contents = textField.SelText;
			textField.SelLength = 0;
			textField.SetNeedsDisplay ();
		}
	}

	private static void Cut ()
	{
		TextField textField = menu.LastFocused as TextField;
		if (textField != null && textField.SelLength > 0) {
			Clipboard.Contents = textField.SelText;
			string actualText = textField.Text.ToString ();
			string newText = actualText.Substring (0, textField.SelStart) + 
				actualText.Substring (textField.SelStart + textField.SelLength, actualText.Length - textField.SelStart - textField.SelLength);
			textField.Text = newText;
			textField.SelLength = 0;
			textField.CursorPosition = textField.SelStart == -1 ? textField.CursorPosition : textField.SelStart;
			textField.SetNeedsDisplay ();
		}
	}

	private static void Paste ()
	{
		TextField textField = menu.LastFocused as TextField;
		if (textField != null) {
			string actualText = textField.Text.ToString ();
			int start = textField.SelStart == -1 ? textField.CursorPosition : textField.SelStart;
			string newText = actualText.Substring (0, start) + 
				Clipboard.Contents?.ToString() +
				actualText.Substring (start + textField.SelLength, actualText.Length - start - textField.SelLength);
			textField.Text = newText;
			textField.SelLength = 0;
			textField.SetNeedsDisplay ();
		}
	}


	#region Selection Demo

	static void ListSelectionDemo ()
	{
		var d = new Dialog ("Selection Demo", 60, 20,
			new Button ("Ok", is_default: true) { Clicked = () => { Application.RequestStop (); } },
			new Button ("Cancel") { Clicked = () => { Application.RequestStop (); } });

		var animals = new List<string> () { "Alpaca", "Llama", "Lion", "Shark", "Goat" };
		var msg = new Label ("Use space bar or control-t to toggle selection") {
			X = 1,
			Y = 1,
			Width = Dim.Fill () - 1,
			Height = 1
		};

		var list = new ListView (animals) {
			X = 1,
			Y = 3,
			Width = Dim.Fill () - 4,
			Height = Dim.Fill () - 4,
			AllowsMarking = true
		};
		d.Add (msg, list);
		Application.Run (d);

		var result = "";
		for (int i = 0; i < animals.Count; i++) {
			if (list.Source.IsMarked (i)) {
				result += animals [i] + " ";
			}
		}
		MessageBox.Query (60, 10, "Selected Animals", result == "" ? "No animals selected" : result, "Ok");
	}
	#endregion


	public static Label ml;
	public static MenuBar menu;
	public static CheckBox menuKeysStyle;
	public static CheckBox menuAutoMouseNav;
	static void Main ()
	{
		if (Debugger.IsAttached)
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

		//Application.UseSystemConsole = true;
		Application.Init ();

		var top = Application.Top;

		//Open ();
#if true
		var win = new Window ("Hello") {
			X = 0,
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
#else
		var tframe = top.Frame;

		var win = new Window (new Rect (0, 1, tframe.Width, tframe.Height - 1), "Hello");
#endif
		MenuItemDetails [] menuItems = {
			new MenuItemDetails ("F_ind", "", null),
			new MenuItemDetails ("_Replace", "", null),
			new MenuItemDetails ("_Item1", "", null),
			new MenuItemDetails ("_Not From Sub Menu", "", null)
		};

		menuItems [0].Action = () => ShowMenuItem (menuItems [0]);
		menuItems [1].Action = () => ShowMenuItem (menuItems [1]);
		menuItems [2].Action = () => ShowMenuItem (menuItems [2]);
		menuItems [3].Action = () => ShowMenuItem (menuItems [3]);

		menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("Text Editor Demo", "", () => { Editor (top); }),
				new MenuItem ("_New", "Creates new file", NewFile),
				new MenuItem ("_Open", "", Open),
				new MenuItem ("_Hex", "", () => ShowHex (top)),
				new MenuItem ("_Close", "", () => Close ()),
				null,
				new MenuItem ("_Quit", "", () => { if (Quit ()) top.Running = false; })
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", Copy),
				new MenuItem ("C_ut", "", Cut),
				new MenuItem ("_Paste", "", Paste),
				new MenuItem ("_Find and Replace",
					new MenuBarItem (new MenuItem[] {menuItems [0], menuItems [1] })),
				menuItems[3]
			}),
			new MenuBarItem ("_List Demos", new MenuItem [] {
				new MenuItem ("Select Items", "", ListSelectionDemo),
			}),
			new MenuBarItem ("Test Menu and SubMenus", new MenuItem [] {
				new MenuItem ("SubMenu1Item1",
					new MenuBarItem (new MenuItem[] {
						new MenuItem ("SubMenu2Item1",
							new MenuBarItem (new MenuItem [] {
								new MenuItem ("SubMenu3Item1",
									new MenuBarItem (new MenuItem [] { menuItems [2] })
								)
							})
						)
					})
				)
			}),
		});

		menuKeysStyle = new CheckBox (3, 25, "UseKeysUpDownAsKeysLeftRight", true);
		menuKeysStyle.Toggled += MenuKeysStyle_Toggled;
		menuAutoMouseNav = new CheckBox (40, 25, "UseMenuAutoNavigation", true);
		menuAutoMouseNav.Toggled += MenuAutoMouseNav_Toggled;

		ShowEntries (win);

		int count = 0;
		ml = new Label (new Rect (3, 17, 47, 1), "Mouse: ");
		Application.RootMouseEvent += delegate (MouseEvent me) {

			ml.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";
		};

		var test = new Label (3, 18, "Se iniciará el análisis");
		win.Add (test);
		win.Add (ml);

		ShowTextAlignments (win);
		top.Add (win);
		top.Add (menu);
		Application.Run ();
	}
}
