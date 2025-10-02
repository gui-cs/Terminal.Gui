using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("KeyBindings", "Illustrates the KeyBindings API.")]
[ScenarioCategory ("Mouse and Keyboard")]
public sealed class KeyBindings : Scenario
{
    private readonly ObservableCollection<string> _focusedBindings = [];
    private ListView _focusedBindingsListView;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            SuperViewRendersLineCanvas = true,
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
            Text = $"""
                    These keys will cause this view to show a message box:
                    - Hotkey: k, K, Alt-K, Alt-Shift-K
                    - Focused: F3
                    - Application: F4
                    Pressing Esc or {Application.QuitKey} will cause it to quit the app.
                    """,
            BorderStyle = LineStyle.Dashed
        };
        appWindow.Add (keyBindingsDemo);

        ObservableCollection<string> appBindings = new ();
        ListView appBindingsListView = new ()
        {
            Title = "_Application Bindings",
            BorderStyle = LineStyle.Single,
            X = -1,
            Y = Pos.Bottom (keyBindingsDemo) + 1,
            Width = Dim.Auto (),
            Height = Dim.Fill () + 1,
            CanFocus = true,
            Source = new ListWrapper<string> (appBindings),
            SuperViewRendersLineCanvas = true
        };
        appWindow.Add (appBindingsListView);

        foreach (Key key in Application.KeyBindings.GetBindings().ToDictionary().Keys)
        {
            var binding = Application.KeyBindings.Get (key);
            appBindings.Add ($"{key} -> {binding.Target?.GetType ().Name} - {binding.Commands [0]}");
        }

        ObservableCollection<string> hotkeyBindings = new ();
        ListView hotkeyBindingsListView = new ()
        {
            Title = "_Hotkey Bindings",
            BorderStyle = LineStyle.Single,
            X = Pos.Right (appBindingsListView) - 1,
            Y = Pos.Bottom (keyBindingsDemo) + 1,
            Width = Dim.Auto (),
            Height = Dim.Fill () + 1,
            CanFocus = true,
            Source = new ListWrapper<string> (hotkeyBindings),
            SuperViewRendersLineCanvas = true

        };
        appWindow.Add (hotkeyBindingsListView);

        foreach (var subview in appWindow.SubViews)
        {
            foreach (KeyValuePair<Key, KeyBinding> binding in subview.HotKeyBindings.GetBindings ())
            {
                hotkeyBindings.Add ($"{binding.Key} -> {subview.GetType ().Name} - {binding.Value.Commands [0]}");
            }
        }

        _focusedBindingsListView = new ()
        {
            Title = "_Focused Bindings",
            BorderStyle = LineStyle.Single,
            X = Pos.Right (hotkeyBindingsListView) - 1,
            Y = Pos.Bottom (keyBindingsDemo) + 1,
            Width = Dim.Auto (),
            Height = Dim.Fill () + 1,
            CanFocus = true,
            Source = new ListWrapper<string> (_focusedBindings),
            SuperViewRendersLineCanvas = true

        };
        appWindow.Add (_focusedBindingsListView);

        Application.Navigation!.FocusedChanged += Application_HasFocusChanged;

        // Run - Start the application.
        Application.Run (appWindow);
        Application.Navigation!.FocusedChanged -= Application_HasFocusChanged;
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }


    private void Application_HasFocusChanged (object sender, EventArgs e)
    {
        View focused = Application.Navigation!.GetFocused ();

        if (focused == null)
        {
            return;
        }

        _focusedBindingsListView.Title = $"_Focused ({focused?.GetType ().Name}) Bindings";

        _focusedBindings.Clear ();
        foreach (var binding in focused?.KeyBindings!.GetBindings ())
        {
            _focusedBindings.Add ($"{binding.Key} -> {binding.Value.Commands [0]}");
        }
    }
}

public class KeyBindingsDemo : View
{
    public KeyBindingsDemo ()
    {
        CanFocus = true;


        AddCommand (Command.Save, ctx =>
                                 {
                                     MessageBox.Query ($"{ctx.Command}", $"Ctx: {ctx}", buttons: "Ok");
                                     return true;
                                 });
        AddCommand (Command.New, ctx =>
                                {
                                    MessageBox.Query ($"{ctx.Command}", $"Ctx: {ctx}", buttons: "Ok");
                                    return true;
                                });
        AddCommand (Command.HotKey, ctx =>
        {
            MessageBox.Query ($"{ctx.Command}", $"Ctx: {ctx}\nCommand: {ctx.Command}", buttons: "Ok");
            SetFocus ();
            return true;
        });

        KeyBindings.Add (Key.F2, Command.Save);
        KeyBindings.Add (Key.F3, Command.New); // same as specifying KeyBindingScope.Focused
        Application.KeyBindings.Add (Key.F4, this, Command.New);

        AddCommand (Command.Quit, ctx =>
                                         {
                                             if (ctx is not CommandContext<KeyBinding> keyCommandContext)
                                             {
                                                 return false;
                                             }
                                             MessageBox.Query ($"{keyCommandContext.Binding}", $"Key: {keyCommandContext.Binding.Key}\nCommand: {ctx.Command}", buttons: "Ok");
                                             Application.RequestStop ();
                                             return true;
                                         });
        Application.KeyBindings.Add (Key.Q.WithAlt, this, Command.Quit);
    }
}
