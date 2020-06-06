using System;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Designer {
	class Surface : Window {
		public Surface () : base ("Designer")
		{
		}
	}

	class MainClass {
		static void Close ()
		{
			MessageBox.ErrorQuery (50, 7, "Error", "There is nothing to close", "Ok");
		}

		static void Copy ()
		{
			TextField textField = menu.LastFocused as TextField;
			if (textField != null && textField.SelectedLength != 0) {
				textField.Copy ();
			}
		}

		static void Cut ()
		{
			TextField textField = menu.LastFocused as TextField;
			if (textField != null && textField.SelectedLength != 0) {
				textField.Cut ();
			}
		}

		static void Paste ()
		{
			TextField textField = menu.LastFocused as TextField;
			if (textField != null) {
				textField.Paste ();
			}
		}

		public static MenuBar menu;

		public static void Main (string [] args)
		{
			Application.Init ();

			menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Close", "", () => Close ()),
					new MenuItem ("_Quit", "", () => { Application.RequestStop (); })
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", Copy),
					new MenuItem ("C_ut", "", Cut),
					new MenuItem ("_Paste", "", Paste)
				}),
			});

			var login = new Label ("Login: ") { X = 3, Y = 6 };
			var password = new Label ("Password: ") {
				X = Pos.Left (login),
				Y = Pos.Bottom (login) + 1
			};
			var test = new Label ("Test: ") {
				X = Pos.Left (login),
				Y = Pos.Bottom (password) + 1
			};

			var surface = new Surface () {
				X = 0,
				Y = 1,
				Width = Dim.Percent (50),
				Height = Dim.Percent (50)
			};

			var loginText = new TextField("") {
				X = Pos.Right(password),
				Y = Pos.Top(login),
				Width = Dim.Percent(90),
				ColorScheme = new ColorScheme() {
					Focus = Attribute.Make(Color.BrightYellow, Color.DarkGray),
					Normal = Attribute.Make(Color.Green, Color.BrightYellow),
					HotFocus = Attribute.Make(Color.BrightBlue, Color.Brown),
					HotNormal = Attribute.Make(Color.Red, Color.BrightRed),
				},
			};
			loginText.MouseEnter += (e) => Text_MouseEnter (e, loginText);
			loginText.MouseLeave += (e) => Text_MouseLeave (e, loginText);
			loginText.Enter += (e) => Text_Enter (e, loginText);
			loginText.Leave += (e) => Text_Leave (e, loginText);

			var passText = new TextField ("") {
				Secret = true,
				X = Pos.Left (loginText),
				Y = Pos.Top (password),
				Width = Dim.Width (loginText)
			};

			var testText = new TextField ("") {
				X = Pos.Left (loginText),
				Y = Pos.Top (test),
				Width = Dim.Width (loginText)
			};
			testText.MouseEnter += (e) => Text_MouseEnter (e, testText);
			testText.MouseLeave += (e) => Text_MouseLeave (e, testText);
			testText.Enter += (e) => Text_Enter (e, testText);
			testText.Leave += (e) => Text_Leave (e, testText);

			surface.Add (login, password, test, loginText, passText, testText);
			Application.Top.Add (menu, surface);
			Application.Run ();
		}

		private static void Text_Leave (View.FocusEventArgs e, TextField view)
		{
			view.Text = $"Leaving from: {view}";
		}

		private static void Text_Enter (View.FocusEventArgs e, TextField view)
		{
			view.Text = $"Entering in: {view}";
		}

		private static void Text_MouseLeave (View.MouseEventArgs e, TextField view)
		{
			view.Text = $"Mouse leave at X: {e.MouseEvent.X}; Y: {e.MouseEvent.Y} HasFocus: {e.MouseEvent.View.HasFocus}";
		}

		private static void Text_MouseEnter (View.MouseEventArgs e, TextField view)
		{
			view.Text = $"Mouse enter at X: {e.MouseEvent.X}; Y: {e.MouseEvent.Y} HasFocus: {e.MouseEvent.View.HasFocus}";
		}
	}
}
