using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Run<T> Example", "Illustrates using Application.Run<T> to run a custom class")]
[ScenarioCategory ("Runnable")]
public class RunTExample : Scenario
{
    public override void Main ()
    {
        // No need to call Init if Application.Run<T> is used
        Application.Run<ExampleWindow> ().Dispose ();
        Application.Shutdown ();
    }

    public class ExampleWindow : Window
    {
        private readonly TextField _usernameText;

        public ExampleWindow ()
        {
            Title = $"Example App ({Application.QuitKey} to quit)";

            // Create input components and labels
            var usernameLabel = new Label { Text = "Username:" };

            _usernameText = new()
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
                X = Pos.Left (_usernameText),
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
                                   if (_usernameText.Text == "admin" && passwordText.Text == "password")
                                   {
                                       MessageBox.Query ("Login Successful", $"Username: {_usernameText.Text}", "Ok");
                                       Application.RequestStop ();
                                   }
                                   else
                                   {
                                       MessageBox.ErrorQuery (
                                                              "Error Logging In",
                                                              "Incorrect username or password (hint: admin/password)",
                                                              "Ok"
                                                             );
                                   }
                               };

            // Add the views to the Window
            Add (usernameLabel, _usernameText, passwordLabel, passwordText, btnLogin);
        }
    }
}
