// This is a test application for a native Aot file.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Terminal.Gui;

namespace NativeAot;

public static class Program
{
    [RequiresUnreferencedCode ("Calls Terminal.Gui.Application.Init(IConsoleDriver, String)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Application.Init(IConsoleDriver, String)")]
    private static void Main (string [] args)
    {
        Application.Init ();

        #region The code in this region is not intended for use in a native Aot self-contained. It's just here to make sure there is no functionality break with localization in Terminal.Gui using self-contained

        if (Equals(Thread.CurrentThread.CurrentUICulture, CultureInfo.InvariantCulture) && Application.SupportedCultures!.Count == 0)
        {
            // Only happens if the project has <InvariantGlobalization>true</InvariantGlobalization>
            Debug.Assert (Application.SupportedCultures.Count == 0);
        }
        else
        {
            Debug.Assert (Application.SupportedCultures!.Count > 0);
            Debug.Assert (Equals (CultureInfo.CurrentCulture, Thread.CurrentThread.CurrentUICulture));
        }

        #endregion

        ExampleWindow app = new ();
        Application.Run (app);

        // Dispose the app object before shutdown
        app.Dispose ();

        // Before the application exits, reset Terminal.Gui for clean shutdown
        Application.Shutdown ();

        // To see this output on the screen it must be done after shutdown,
        // which restores the previous screen.
        Console.WriteLine ($@"Username: {ExampleWindow.UserName}");
    }
}

// Defines a top-level window with border and title
public class ExampleWindow : Window
{
    public static string? UserName;

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
            // Anytime Accepting is handled, make sure to set e.Cancel to false.
            e.Cancel = false;
        };

        // Add the views to the Window
        Add (usernameLabel, userNameText, passwordLabel, passwordText, btnLogin);
    }
}
