// A simple Terminal.Gui example in C# - using C# 9.0 Top-level statements
// This is the same code found in the Termiminal Gui README.md file.

using Terminal.Gui;
using NStack;

Application.Init ();

// Creates the top-level window to show
var win = new Window ("Example App") {
	X = 0,
	Y = 1, // Leave one row for the toplevel menu

	// By using Dim.Fill(), this Window will automatically resize without manual intervention
	Width = Dim.Fill (),
	Height = Dim.Fill ()
};

Application.Top.Add (win);

// Creates a menubar, the item "New" has a help menu.
var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "Creates a new file", null),
				new MenuItem ("_Close", "",null),
				new MenuItem ("_Quit", "", () => { if (Quit ()) Application.Top.Running = false; })
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", null),
				new MenuItem ("C_ut", "", null),
				new MenuItem ("_Paste", "", null)
			})
		});
Application.Top.Add (menu);

static bool Quit ()
{
	var n = MessageBox.Query (50, 7, "Quit Example", "Are you sure you want to quit this example?", "Yes", "No");
	return n == 0;
}

var login = new Label ("Login: ") { X = 3, Y = 2 };
var password = new Label ("Password: ") {
	X = Pos.Left (login),
	Y = Pos.Top (login) + 1
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

// Add the views to the main window, 
win.Add (
	// Using Computed Layout:
	login, password, loginText, passText,

	// Using Absolute Layout:
	new CheckBox (3, 6, "Remember me"),
	new RadioGroup (3, 8, new ustring [] { "_Personal", "_Company" }, 0),
	new Button (3, 14, "Ok"),
	new Button (10, 14, "Cancel"),
	new Label (3, 18, "Press F9 or ESC plus 9 to activate the menubar")
);

// Run blocks until the user quits the application
Application.Run ();

// Always bracket Application.Init with .Shutdown.
Application.Shutdown ();