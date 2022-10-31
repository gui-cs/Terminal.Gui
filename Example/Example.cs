// This is a simple example application.  For the full range of functionality
// see the UICatalog project

// A simple Terminal.Gui example in C# - using C# 9.0 Top-level statements

using Terminal.Gui;

// Initialize the console
Application.Init();

// Creates the top-level window with border and title
var win = new Window("Example App (Ctrl+Q to quit)");

// Create input components and labels

var usernameLabel = new Label("Username:");
var usernameText = new TextField("")
{
    // Position text field adjacent to label
    X = Pos.Right(usernameLabel) + 1,

    // Fill remaining horizontal space with a margin of 1
    Width = Dim.Fill(1),
};

var passwordLabel = new Label(0,2,"Password:");
var passwordText = new TextField("")
{
    Secret = true,
    // align with the text box above
    X = Pos.Left(usernameText),
    Y = 2,
    Width = Dim.Fill(1),
};

// Create login button
var btnLogin = new Button("Login")
{
    Y = 4,
    // center the login button horizontally
    X = Pos.Center(),
    IsDefault = true,
};

// When login button is clicked display a message popup
btnLogin.Clicked += () => MessageBox.Query("Logging In", "Login Successful", "Ok");

// Add all the views to the window
win.Add(
    usernameLabel, usernameText, passwordLabel, passwordText,btnLogin
);

// Show the application
Application.Run(win);

// After the application exits, release and reset console for clean shutdown
Application.Shutdown();