// Native AOT test application for Terminal.Gui.
// Exercises the config-property-hosting view types that are most sensitive to AOT trimming:
// Button, CheckBox, Dialog, FrameView, Label, MenuBar, Menu, MessageBox, OptionSelector,
// StatusBar, TextField, TextView, Window.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NativeAot;

public static class Program
{
    private static void Main (string [] args)
    {
#pragma warning disable IL2026, IL3050
        Run ();
#pragma warning restore IL2026, IL3050
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.Application.Init(IDriver, String)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Application.Init(IDriver, String)")]
    private static void Run ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        IApplication app = Application.Create ().Init ();

        #region Localization sanity check

        if (Equals (Thread.CurrentThread.CurrentUICulture, CultureInfo.InvariantCulture) && Application.SupportedCultures!.Count == 0)
        {
            Debug.Assert (Application.SupportedCultures.Count == 0);
        }
        else
        {
            Debug.Assert (Application.SupportedCultures!.Count > 0);
            Debug.Assert (Equals (CultureInfo.CurrentCulture, Thread.CurrentThread.CurrentUICulture));
        }

        #endregion

        string? result = app.Run<AotTestWindow> ().GetResult<string> ();
        app.Dispose ();

        Console.WriteLine (string.IsNullOrEmpty (result) ? "Cancelled" : $"Result: {result}");
    }
}

/// <summary>
///     A window that exercises the Terminal.Gui view types most sensitive to AOT trimming.
///     Each view type with a <see cref="ConfigurationPropertyAttribute" /> is instantiated here
///     so that its config properties are deep-cloned during initialization, exercising the
///     <see cref="DeepCloner" /> code paths that are prone to AOT failures.
/// </summary>
public sealed class AotTestWindow : Runnable<string?>
{
    public AotTestWindow ()
    {
        Title = $"AOT Test ({Application.GetDefaultKey (Command.Quit)} to quit)";

        // ── MenuBar ──────────────────────────────────────────────────
        MenuBar menuBar = new ()
        {
            Menus =
            [
                new MenuBarItem (
                                 "File",
                                 [
                                     new MenuItem ("Open", "", () => { }),
                                     new MenuItem ("Quit", "", () => App?.RequestStop ())
                                 ]),
                new MenuBarItem (
                                 "Help",
                                 [
                                     new MenuItem ("About", "", () => MessageBox.Query (App!, "About", "Native AOT Test App", "OK"))
                                 ])
            ]
        };

        // ── Login area (FrameView) ───────────────────────────────────
        FrameView loginFrame = new ()
        {
            Title = "Login",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = 6
        };

        Label usernameLabel = new () { Text = "Username:" };

        TextField userNameText = new ()
        {
            X = Pos.Right (usernameLabel) + 1,
            Width = Dim.Fill ()
        };

        Label passwordLabel = new ()
        {
            Text = "Password:",
            X = Pos.Left (usernameLabel),
            Y = Pos.Bottom (usernameLabel) + 1
        };

        TextField passwordText = new ()
        {
            Secret = true,
            X = Pos.Left (userNameText),
            Y = Pos.Top (passwordLabel),
            Width = Dim.Fill ()
        };

        loginFrame.Add (usernameLabel, userNameText, passwordLabel, passwordText);

        // ── Options area (CheckBox, OptionSelector) ──────────────────
        FrameView optionsFrame = new ()
        {
            Title = "Options",
            X = 0,
            Y = Pos.Bottom (loginFrame),
            Width = Dim.Fill (),
            Height = 5
        };

        CheckBox rememberMe = new ()
        {
            Text = "Remember me",
            X = 0,
            Y = 0
        };

        OptionSelector loginMode = new ()
        {
            X = Pos.Right (rememberMe) + 4,
            Y = 0,
            Labels = ["Standard", "SSO", "Token"]
        };

        optionsFrame.Add (rememberMe, loginMode);

        // ── Notes area (TextView) ────────────────────────────────────
        FrameView notesFrame = new ()
        {
            Title = "Notes",
            X = 0,
            Y = Pos.Bottom (optionsFrame),
            Width = Dim.Fill (),
            Height = Dim.Fill (3)
        };

        TextView notesText = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "Enter any notes here..."
        };

        notesFrame.Add (notesText);

        // ── Login button ─────────────────────────────────────────────
        Button btnLogin = new ()
        {
            Text = "Login",
            Y = Pos.AnchorEnd (2),
            X = Pos.Center (),
            IsDefault = true
        };

        btnLogin.Accepting += (_, e) =>
                               {
                                   if (userNameText.Text == "admin" && passwordText.Text == "password")
                                   {
                                       MessageBox.Query (App!, "Logging In", "Login Successful", "OK");
                                       Result = userNameText.Text;
                                       App?.RequestStop ();
                                   }
                                   else
                                   {
                                       MessageBox.ErrorQuery (App!, "Logging In", "Incorrect username or password", "OK");
                                   }

                                   e.Handled = true;
                               };

        // ── StatusBar ────────────────────────────────────────────────
        StatusBar statusBar = new ()
        {
            Y = Pos.AnchorEnd (1)
        };

        Shortcut helpShortcut = new ()
        {
            Text = "Help",
            Key = Key.F1,
            BindKeyToApplication = true
        };

        helpShortcut.Accepting += (_, e) =>
                                   {
                                       MessageBox.Query (App!, "Help", "This is the AOT test application.", "OK");
                                       e.Handled = true;
                                   };

        statusBar.Add (helpShortcut);

        Add (menuBar, loginFrame, optionsFrame, notesFrame, btnLogin, statusBar);
    }
}
