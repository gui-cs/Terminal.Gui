using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog; 

class KeyBindingsDialog : Dialog {
    // TODO: Update to use Key instead of KeyCode
    private static readonly Dictionary<Command, KeyCode> CurrentBindings = new ();
    private readonly Command[] commands;
    private readonly ListView commandsListView;
    private readonly Label keyLabel;

    public KeyBindingsDialog () {
        Title = "Keybindings";

        //Height = 50;
        //Width = 10;
        if (ViewTracker.Instance == null) {
            ViewTracker.Initialize ();
        }

        // known commands that views can support
        commands = Enum.GetValues (typeof (Command)).Cast<Command> ().ToArray ();

        commandsListView = new ListView (commands) {
                                                       Width = Dim.Percent (50),
                                                       Height = Dim.Percent (100) - 1
                                                   };

        Add (commandsListView);

        keyLabel = new Label {
                                 Text = "Key: None",
                                 Width = Dim.Fill (),
                                 X = Pos.Percent (50),
                                 Y = 0
                             };
        Add (keyLabel);

        var btnChange = new Button {
                                       Text = "Ch_ange",
                                       X = Pos.Percent (50),
                                       Y = 1
                                   };
        Add (btnChange);
        btnChange.Clicked += RemapKey;

        var close = new Button ("Ok");
        close.Clicked += (s, e) => {
            Application.RequestStop ();
            ViewTracker.Instance.StartUsingNewKeyMap (CurrentBindings);
        };
        AddButton (close);

        var cancel = new Button ("Cancel");
        cancel.Clicked += (s, e) => Application.RequestStop ();
        AddButton (cancel);

        // Register event handler as the last thing in constructor to prevent early calls
        // before it is even shown (e.g. OnEnter)
        commandsListView.SelectedItemChanged += CommandsListView_SelectedItemChanged;

        // Setup to show first ListView entry
        SetTextBoxToShowBinding (commands.First ());
    }

    private void CommandsListView_SelectedItemChanged (object sender, ListViewItemEventArgs obj) {
        SetTextBoxToShowBinding ((Command)obj.Value);
    }

    private void RemapKey (object sender, EventArgs e) {
        Command cmd = commands[commandsListView.SelectedItem];
        KeyCode? key = null;

        // prompt user to hit a key
        var dlg = new Dialog { Title = "Enter Key" };
        dlg.KeyDown += (s, k) => {
            key = k.KeyCode;
            Application.RequestStop ();
        };
        Application.Run (dlg);

        if (key.HasValue) {
            CurrentBindings[cmd] = key.Value;
            SetTextBoxToShowBinding (cmd);
        }
    }

    private void SetTextBoxToShowBinding (Command cmd) {
        if (CurrentBindings.ContainsKey (cmd)) {
            keyLabel.Text = "Key: " + CurrentBindings[cmd];
        } else {
            keyLabel.Text = "Key: None";
        }

        SetNeedsDisplay ();
    }

    /// <summary>Tracks views as they are created in UICatalog so that their keybindings can be managed.</summary>
    private class ViewTracker {
        public static ViewTracker Instance;
        private Dictionary<Command, KeyCode> keybindings;

        /// <summary>All views seen so far and a bool to indicate if we have applied keybindings to them</summary>
        private readonly Dictionary<View, bool> knownViews = new ();

        private readonly object lockKnownViews = new ();

        public ViewTracker (View top) {
            RecordView (top);

            // Refresh known windows
            Application.AddTimeout (
                                    TimeSpan.FromMilliseconds (100),
                                    () => {
                                        lock (lockKnownViews) {
                                            RecordView (Application.Top);

                                            ApplyKeyBindingsToAllKnownViews ();
                                        }

                                        return true;
                                    });
        }

        internal static void Initialize () { Instance = new ViewTracker (Application.Top); }

        internal void StartUsingNewKeyMap (Dictionary<Command, KeyCode> currentBindings) {
            lock (lockKnownViews) {
                // change our knowledge of what keys to bind
                keybindings = currentBindings;

                // Mark that we have not applied the key bindings yet to any views
                foreach (View view in knownViews.Keys) {
                    knownViews[view] = false;
                }
            }
        }

        private void ApplyKeyBindingsToAllKnownViews () {
            if (keybindings == null) {
                return;
            }

            // Key is the view Value is whether we have already done it
            foreach (KeyValuePair<View, bool> viewDone in knownViews) {
                View view = viewDone.Key;
                bool done = viewDone.Value;

                if (done) {
                    // we have already applied keybindings to this view
                    continue;
                }

                HashSet<Command> supported = new HashSet<Command> (view.GetSupportedCommands ());

                foreach (KeyValuePair<Command, KeyCode> kvp in keybindings) {
                    // if the view supports the keybinding
                    if (supported.Contains (kvp.Key)) {
                        // if the key was bound to any other commands clear that
                        view.KeyBindings.Remove (kvp.Value);
                        view.KeyBindings.Add (kvp.Value, kvp.Key);
                    }

                    // mark that we have done this view so don't need to set keybindings again on it
                    knownViews[view] = true;
                }
            }
        }

        private void RecordView (View view) {
            if (!knownViews.ContainsKey (view)) {
                knownViews.Add (view, false);
            }

            // may already have subviews that were added to it
            // before we got to it
            foreach (View sub in view.Subviews) {
                RecordView (sub);
            }

            // TODO: BUG: Based on my new understanding of Added event I think this is wrong
            // (and always was wrong). Parents don't get to be told when new views are added
            // to them

            view.Added += (s, e) => RecordView (e.Child);
        }
    }
}
