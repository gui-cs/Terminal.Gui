using System;
using System.Collections.Generic;
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
        textField.KeyBindings.Add (Key.F4, KeyBindingScope.Application, Command.SelectAll);

        appWindow.Add (label, textField);

        List<string> bindingList = new ();
        ListView keyBindingsListView = new ()
        {
            X = 0,
            Y = Pos.Bottom (textField) + 1,
            Width = 60,
            Height = Dim.Fill (1),
            CanFocus = true,
            Source = new ListWrapper (bindingList),
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
