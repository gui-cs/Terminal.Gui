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

		public static void Main (string [] args)
		{
			Application.Init ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Close", "", () => Close ()),
					new MenuItem ("_Quit", "", () => { Application.RequestStop (); })
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
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
			loginText.MouseEnter += LoginText_MouseEnter;
			loginText.MouseLeave += LoginText_MouseLeave;
			loginText.Enter += LoginText_Enter;
			loginText.Leave += LoginText_Leave;

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

			surface.Add (login, password, test, loginText, passText, testText);
			Application.Top.Add (menu, surface);
			Application.Run ();
		}

		private static void LoginText_Leave (object sender, EventArgs e)
		{
			((TextField)sender).Text = $"Leaving from: {sender}";
		}

		private static void LoginText_Enter (object sender, EventArgs e)
		{
			((TextField)sender).Text = $"Entering in: {sender}";
		}

		private static void LoginText_MouseLeave (object sender, MouseEvent e)
		{
			((TextField)sender).Text = $"Mouse leave at X: {e.X}; Y: {e.Y} HasFocus: {e.View.HasFocus}";
		}

		private static void LoginText_MouseEnter (object sender, MouseEvent e)
		{
			((TextField)sender).Text = $"Mouse enter at X: {e.X}; Y: {e.Y} HasFocus: {e.View.HasFocus}";
		}
	}
}
