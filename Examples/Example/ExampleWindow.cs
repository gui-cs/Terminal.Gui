using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

// Defines a top-level window with border and title
public class ExampleWindow : Window
{
    public static string? UserName { get; set; }

    public ExampleWindow ()
    {
        Title = $"Example App ({Application.QuitKey} to quit)";

        // Create input components and labels
        var usernameLabel = new Label { Text = "Username:" };

        var userNameText = new TextField
        {
            // Position text field adjacent to the label
            X = Pos.Right (usernameLabel) + 1,

            // Fill remaining horizontal space
            Width = Dim.Fill ()
        };

        var passwordLabel = new Label
        {
            Text = "Password:", X = Pos.Left (usernameLabel), Y = Pos.Bottom (usernameLabel) + 1
        };

        var passwordText = new TextField
        {
            Secret = true,

            // align with the text box above
            X = Pos.Left (userNameText),
            Y = Pos.Top (passwordLabel),
            Width = Dim.Fill ()
        };

        // Create login button
        var btnLogin = new Button
        {
            Text = "Login",
            Y = Pos.Bottom (passwordLabel) + 1,

            // center the login button horizontally
            X = Pos.Center (),
            IsDefault = true
        };

        // When login button is clicked display a message popup
        btnLogin.Accepting += (s, e) =>
        {
            if (userNameText.Text == "admin" && passwordText.Text == "password")
            {
                MessageBox.Query ("Logging In", "Login Successful", "Ok");
                UserName = userNameText.Text;
                Application.RequestStop ();
            }
            else
            {
                MessageBox.ErrorQuery ("Logging In", "Incorrect username or password", "Ok");
            }
            // When Accepting is handled, set e.Handled to true to prevent further processing.
            e.Handled = true;
        };

        Height = Dim.Fill ();
        Width = Dim.Fill ();

        // Add the views to the Window
        Add (usernameLabel, userNameText, passwordLabel, passwordText, btnLogin);
    }
}
