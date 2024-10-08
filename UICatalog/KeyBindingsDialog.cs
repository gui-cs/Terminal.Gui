using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui;

namespace UICatalog;

internal class KeyBindingsDialog : Dialog
{
    // TODO: Update to use Key instead of KeyCode
    private static readonly Dictionary<Command, KeyCode> CurrentBindings = new ();

    private readonly ObservableCollection<Command> _commands;
    private readonly ListView _commandsListView;
    private readonly Label _keyLabel;

    public KeyBindingsDialog ()
    {
        Title = "Keybindings";

        //Height = Dim.Percent (80);
        //Width = Dim.Percent (80);
        if (ViewTracker.Instance == null)
        {
            ViewTracker.Initialize ();
        }

        // known commands that views can support
        _commands = new (Enum.GetValues (typeof (Command)).Cast<Command> ().ToArray ());

        _commandsListView = new ListView
        {
            Width = Dim.Percent (50),
            Height = Dim.Fill (Dim.Func (() => IsInitialized ? Subviews.First (view => view.Y.Has<PosAnchorEnd> (out _)).Frame.Height : 1)),
            Source = new ListWrapper<Command> (_commands),
            SelectedItem = 0
        };

        Add (_commandsListView);

        _keyLabel = new Label { Text = "Key: None", Width = Dim.Fill (), X = Pos.Percent (50), Y = 0 };
        Add (_keyLabel);

        var btnChange = new Button { X = Pos.Percent (50), Y = 1, Text = "Ch_ange" };
        Add (btnChange);
        btnChange.Accepting += RemapKey;

        var close = new Button { Text = "Ok" };

        close.Accepting += (s, e) =>
                         {
                             Application.RequestStop ();
                             ViewTracker.Instance.StartUsingNewKeyMap (CurrentBindings);
                         };
        AddButton (close);

        var cancel = new Button { Text = "Cancel" };
        cancel.Accepting += (s, e) => Application.RequestStop ();
        AddButton (cancel);

        // Register event handler as the last thing in constructor to prevent early calls
        // before it is even shown (e.g. OnHasFocusChanging)
        _commandsListView.SelectedItemChanged += CommandsListView_SelectedItemChanged;

        // Setup to show first ListView entry
        SetTextBoxToShowBinding (_commands.First ());
    }

    private void CommandsListView_SelectedItemChanged (object sender, ListViewItemEventArgs obj) { SetTextBoxToShowBinding ((Command)obj.Value); }

    private void RemapKey (object sender, EventArgs e)
    {
        Command cmd = _commands [_commandsListView.SelectedItem];
        KeyCode? key = null;

        // prompt user to hit a key
        var dlg = new Dialog { Title = "Enter Key" };

        dlg.KeyDown += (s, k) =>
                       {
                           key = k.KeyCode;
                           Application.RequestStop ();
                       };
        Application.Run (dlg);
        dlg.Dispose ();

        if (key.HasValue)
        {
            CurrentBindings [cmd] = key.Value;
            SetTextBoxToShowBinding (cmd);
        }
    }

    private void SetTextBoxToShowBinding (Command cmd)
    {
        if (CurrentBindings.ContainsKey (cmd))
        {
            _keyLabel.Text = "Key: " + CurrentBindings [cmd];
        }
        else
        {
            _keyLabel.Text = "Key: None";
        }

        SetNeedsDisplay ();
    }

    /// <summary>Tracks views as they are created in UICatalog so that their keybindings can be managed.</summary>
    private class ViewTracker
    {
        /// <summary>All views seen so far and a bool to indicate if we have applied keybindings to them</summary>
        private readonly Dictionary<View, bool> _knownViews = new ();

        private readonly object _lockKnownViews = new ();
        private Dictionary<Command, KeyCode> _keybindings;

        private ViewTracker (View top)
        {
            RecordView (top);

            // Refresh known windows
            Application.AddTimeout (
                                    TimeSpan.FromMilliseconds (100),
                                    () =>
                                    {
                                        lock (_lockKnownViews)
                                        {
                                            RecordView (Application.Top);

                                            ApplyKeyBindingsToAllKnownViews ();
                                        }

                                        return true;
                                    }
                                   );
        }

        public static ViewTracker Instance { get; private set; }
        internal static void Initialize () { Instance = new ViewTracker (Application.Top); }

        internal void StartUsingNewKeyMap (Dictionary<Command, KeyCode> currentBindings)
        {
            lock (_lockKnownViews)
            {
                // change our knowledge of what keys to bind
                _keybindings = currentBindings;

                // Mark that we have not applied the key bindings yet to any views
                foreach (View view in _knownViews.Keys)
                {
                    _knownViews [view] = false;
                }
            }
        }

        private void ApplyKeyBindingsToAllKnownViews ()
        {
            if (_keybindings == null)
            {
                return;
            }

            // Key is the view Value is whether we have already done it
            foreach (KeyValuePair<View, bool> viewDone in _knownViews)
            {
                View view = viewDone.Key;
                bool done = viewDone.Value;

                if (done)
                {
                    // we have already applied keybindings to this view
                    continue;
                }

                HashSet<Command> supported = new (view.GetSupportedCommands ());

                foreach (KeyValuePair<Command, KeyCode> kvp in _keybindings)
                {
                    // if the view supports the keybinding
                    if (supported.Contains (kvp.Key))
                    {
                        // if the key was bound to any other commands clear that
                        view.KeyBindings.Remove (kvp.Value);
                        view.KeyBindings.Add (kvp.Value, kvp.Key);
                    }

                    // mark that we have done this view so don't need to set keybindings again on it
                    _knownViews [view] = true;
                }
            }
        }

        private void RecordView (View view)
        {
            if (!_knownViews.ContainsKey (view))
            {
                _knownViews.Add (view, false);
            }

            // may already have subviews that were added to it
            // before we got to it
            foreach (View sub in view.Subviews)
            {
                RecordView (sub);
            }

            // TODO: BUG: Based on my new understanding of Added event I think this is wrong
            // (and always was wrong). Parents don't get to be told when new views are added
            // to them

            view.Added += (s, e) => RecordView (e.SubView);
        }
    }
}
