using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("KeyBindings", "Illustrates the KeyBindings API.")]
[ScenarioCategory ("Mouse and Keyboard")]
public sealed class KeyBindings : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
        };


        Label label = new ()
        {
            Title = "_Label:",
        };
        TextField textField = new ()
        {
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = 20,
        };

        appWindow.Add (label, textField);

        Button button = new ()
        {
            X = Pos.Right (textField) + 1,
            Y = Pos.Top (label),
            Text = "_Button",
        };
        appWindow.Add (button);

        KeyBindingsDemo keyBindingsDemo = new ()
        {
            X = Pos.Right (button) + 1,
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text),
            HotKeySpecifier = (Rune)'_',
            Title = "_KeyBindingsDemo",
            Text = @"These keys will cause this view to show a message box:
- Hotkey: k, K, Alt-K, Alt-Shift-K
- Focused: F3
- Application: F4
Pressing Ctrl-Q will cause it to quit the app.",
            BorderStyle = LineStyle.Dashed
        };
        appWindow.Add (keyBindingsDemo);

        ObservableCollection<string> bindingList = new ();
        ListView keyBindingsListView = new ()
        {
            X = 0,
            Y = Pos.Bottom (keyBindingsDemo) + 1,
            Width = 60,
            Height = Dim.Fill (1),
            CanFocus = true,
            Source = new ListWrapper<string> (bindingList),
        };
        appWindow.Add (keyBindingsListView);

        foreach (var binding in appWindow.KeyBindings.Bindings)
        {
            bindingList.Add ($"{appWindow.GetType ().Name} - {binding.Key} - {binding.Value.Scope}: {binding.Value.Commands [0]}");
        }

        foreach (var subview in appWindow.Subviews)
        {
            foreach (var binding in subview.KeyBindings.Bindings)
            {
                bindingList.Add ($"{subview.GetType ().Name} - {binding.Key} - {binding.Value.Scope}: {binding.Value.Commands [0]}");
            }
        }

        keyBindingsListView.SelectedItem = 0;
        //keyBindingsListView.MoveEnd ();

        //appWindow.Initialized += (s, e) =>
        //{
        //    keyBindingsListView.EnsureSelectedItemVisible ();
        //};
        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}

public class KeyBindingsDemo : View
{
    public KeyBindingsDemo ()
    {
        CanFocus = true;

        AddCommand (Command.New, ctx =>
                                {
                                    MessageBox.Query ("Hi", $"Key: {ctx.Key}\nCommand: {ctx.Command}", buttons: "Ok");

                                    return true;
                                });
        AddCommand (Command.HotKey, ctx =>
        {
            MessageBox.Query ("Hi", $"Key: {ctx.Key}\nCommand: {ctx.Command}", buttons: "Ok");
            SetFocus ();
            return true;
        });

        KeyBindings.Add (Key.F3, KeyBindingScope.Focused, Command.New);
        KeyBindings.Add (Key.F4, KeyBindingScope.Application, Command.New);


        AddCommand (Command.QuitToplevel, ctx =>
                                         {
                                             Application.RequestStop ();
                                             return true;
                                         });
        KeyBindings.Add (Key.Q.WithCtrl, KeyBindingScope.Application, Command.QuitToplevel);
    }
}
