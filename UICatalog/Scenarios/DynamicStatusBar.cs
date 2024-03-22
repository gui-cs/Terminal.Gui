using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dynamic StatusBar", "Demonstrates how to add and remove a StatusBar and change items dynamically.")]
[ScenarioCategory ("Top Level Windows")]
public class DynamicStatusBar : Scenario
{
    public override void Init ()
    {
        Application.Init ();

        Top = new ();

        Top.Add (
                 new DynamicStatusBarSample { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" }
                );
    }

    public class Binding
    {
        private readonly PropertyInfo _sourceBindingProperty;
        private readonly object _sourceDataContext;
        private readonly IValueConverter _valueConverter;

        public Binding (
            View source,
            string sourcePropertyName,
            View target,
            string targetPropertyName,
            IValueConverter valueConverter = null
        )
        {
            Target = target;
            Source = source;
            SourcePropertyName = sourcePropertyName;
            TargetPropertyName = targetPropertyName;
            _sourceDataContext = Source.GetType ().GetProperty ("DataContext").GetValue (Source);
            _sourceBindingProperty = _sourceDataContext.GetType ().GetProperty (SourcePropertyName);
            _valueConverter = valueConverter;
            UpdateTarget ();

            var notifier = (INotifyPropertyChanged)_sourceDataContext;

            if (notifier != null)
            {
                notifier.PropertyChanged += (s, e) =>
                                            {
                                                if (e.PropertyName == SourcePropertyName)
                                                {
                                                    UpdateTarget ();
                                                }
                                            };
            }
        }

        public View Source { get; }
        public string SourcePropertyName { get; }
        public View Target { get; }
        public string TargetPropertyName { get; }

        private void UpdateTarget ()
        {
            try
            {
                object sourceValue = _sourceBindingProperty.GetValue (_sourceDataContext);

                if (sourceValue == null)
                {
                    return;
                }

                object finalValue = _valueConverter?.Convert (sourceValue) ?? sourceValue;

                PropertyInfo targetProperty = Target.GetType ().GetProperty (TargetPropertyName);
                targetProperty.SetValue (Target, finalValue);
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery ("Binding Error", $"Binding failed: {ex}.", "Ok");
            }
        }
    }

    public class DynamicStatusBarDetails : FrameView
    {
        private StatusItem _statusItem;

        public DynamicStatusBarDetails (StatusItem statusItem = null) : this ()
        {
            _statusItem = statusItem;
            Title = statusItem == null ? "Adding New StatusBar Item." : "Editing StatusBar Item.";
        }

        public DynamicStatusBarDetails ()
        {
            var _lblTitle = new Label { Y = 1, Text = "Title:" };
            Add (_lblTitle);

            TextTitle = new TextField { X = Pos.Right (_lblTitle) + 4, Y = Pos.Top (_lblTitle), Width = Dim.Fill () };
            Add (TextTitle);

            var _lblAction = new Label { X = Pos.Left (_lblTitle), Y = Pos.Bottom (_lblTitle) + 1, Text = "Action:" };
            Add (_lblAction);

            TextAction = new TextView
            {
                X = Pos.Left (TextTitle), Y = Pos.Top (_lblAction), Width = Dim.Fill (), Height = 5
            };
            Add (TextAction);

            var _lblShortcut = new Label
            {
                X = Pos.Left (_lblTitle), Y = Pos.Bottom (TextAction) + 1, Text = "Shortcut:"
            };
            Add (_lblShortcut);

            TextShortcut = new TextField
            {
                X = Pos.X (TextAction), Y = Pos.Y (_lblShortcut), Width = Dim.Fill (), ReadOnly = true
            };

            TextShortcut.KeyDown += (s, e) =>
                                    {
                                        if (!ProcessKey (e))
                                        {
                                            return;
                                        }

                                        if (CheckShortcut (e.KeyCode, true))
                                        {
                                            e.Handled = true;
                                        }
                                    };

            bool ProcessKey (Key ev)
            {
                switch (ev.KeyCode)
                {
                    case KeyCode.CursorUp:
                    case KeyCode.CursorDown:
                    case KeyCode.Tab:
                    case KeyCode.Tab | KeyCode.ShiftMask:
                        return false;
                }

                return true;
            }

            bool CheckShortcut (KeyCode k, bool pre)
            {
                StatusItem m = _statusItem != null ? _statusItem : new StatusItem (k, "", null);

                if (pre && !ShortcutHelper.PreShortcutValidation (k))
                {
                    TextShortcut.Text = "";

                    return false;
                }

                if (!pre)
                {
                    if (!ShortcutHelper.PostShortcutValidation (
                                                                ShortcutHelper.GetShortcutFromTag (
                                                                                                   TextShortcut.Text,
                                                                                                   StatusBar.ShortcutDelimiter
                                                                                                  )
                                                               ))
                    {
                        TextShortcut.Text = "";

                        return false;
                    }

                    return true;
                }

                TextShortcut.Text =
                    Key.ToString (
                                  k,
                                  StatusBar
                                      .ShortcutDelimiter
                                 ); //ShortcutHelper.GetShortcutTag (k, StatusBar.ShortcutDelimiter);

                return true;
            }

            TextShortcut.KeyUp += (s, e) =>
                                  {
                                      if (CheckShortcut (e.KeyCode, true))
                                      {
                                          e.Handled = true;
                                      }
                                  };
            Add (TextShortcut);

            var _btnShortcut = new Button
            {
                X = Pos.X (_lblShortcut), Y = Pos.Bottom (TextShortcut) + 1, Text = "Clear Shortcut"
            };
            _btnShortcut.Accept += (s, e) => { TextShortcut.Text = ""; };
            Add (_btnShortcut);
        }

        public TextView TextAction { get; }
        public TextField TextShortcut { get; }
        public TextField TextTitle { get; }
        public Action CreateAction (DynamicStatusItem item) { return () => MessageBox.ErrorQuery (item.Title, item.Action, "Ok"); }

        public void EditStatusItem (StatusItem statusItem)
        {
            if (statusItem == null)
            {
                Enabled = false;
                CleanEditStatusItem ();

                return;
            }

            Enabled = true;
            _statusItem = statusItem;
            TextTitle.Text = statusItem?.Title ?? "";

            TextAction.Text = statusItem != null && statusItem.Action != null
                                  ? GetTargetAction (statusItem.Action)
                                  : string.Empty;

            TextShortcut.Text =
                Key.ToString (
                              (KeyCode)statusItem.Shortcut,
                              StatusBar
                                  .ShortcutDelimiter
                             ); //ShortcutHelper.GetShortcutTag (statusItem.Shortcut, StatusBar.ShortcutDelimiter) ?? "";
        }

        public DynamicStatusItem EnterStatusItem ()
        {
            var valid = false;

            if (_statusItem == null)
            {
                var m = new DynamicStatusItem ();
                TextTitle.Text = m.Title;
                TextAction.Text = m.Action;
            }
            else
            {
                EditStatusItem (_statusItem);
            }

            var btnOk = new Button { IsDefault = true, Text = "OK" };

            btnOk.Accept += (s, e) =>
                              {
                                  if (string.IsNullOrEmpty (TextTitle.Text))
                                  {
                                      MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
                                  }
                                  else
                                  {
                                      if (!string.IsNullOrEmpty (TextShortcut.Text))
                                      {
                                          TextTitle.Text = DynamicStatusBarSample.SetTitleText (
                                                                                                TextTitle.Text,
                                                                                                TextShortcut.Text
                                                                                               );
                                      }

                                      valid = true;
                                      Application.RequestStop ();
                                  }
                              };
            var btnCancel = new Button { Text = "Cancel" };

            btnCancel.Accept += (s, e) =>
                                  {
                                      TextTitle.Text = string.Empty;
                                      Application.RequestStop ();
                                  };
            var dialog = new Dialog { Title = "Enter the menu details.", Buttons = [btnOk, btnCancel] };

            Width = Dim.Fill ();
            Height = Dim.Fill () - 1;
            dialog.Add (this);
            TextTitle.SetFocus ();
            TextTitle.CursorPosition = TextTitle.Text.Length;
            Application.Run (dialog);
            dialog.Dispose ();

            return valid
                       ? new DynamicStatusItem
                       {
                           Title = TextTitle.Text, Action = TextAction.Text, Shortcut = TextShortcut.Text
                       }
                       : null;
        }

        private void CleanEditStatusItem ()
        {
            TextTitle.Text = "";
            TextAction.Text = "";
            TextShortcut.Text = "";
        }

        private string GetTargetAction (Action action)
        {
            object me = action.Target;

            if (me == null)
            {
                throw new ArgumentException ();
            }

            var v = new object ();

            foreach (FieldInfo field in me.GetType ().GetFields ())
            {
                if (field.Name == "item")
                {
                    v = field.GetValue (me);
                }
            }

            return v == null || !(v is DynamicStatusItem item) ? string.Empty : item.Action;
        }
    }

    public class DynamicStatusBarSample : Window
    {
        private readonly ListView _lstItems;
        private StatusItem _currentEditStatusItem;
        private int _currentSelectedStatusBar = -1;
        private StatusItem _currentStatusItem;
        private StatusBar _statusBar;

        public DynamicStatusBarSample ()
        {
            DataContext = new DynamicStatusItemModel ();

            var _frmDelimiter = new FrameView
            {
                X = Pos.Center (),
                Y = 0,
                Width = 25,
                Height = 4,
                Title = "Shortcut Delimiter:"
            };

            var _txtDelimiter = new TextField { X = Pos.Center (), Width = 2, Text = $"{StatusBar.ShortcutDelimiter}" };

            _txtDelimiter.TextChanged += (s, _) =>
                                             StatusBar.ShortcutDelimiter = _txtDelimiter.Text.ToRunes () [0];
            _frmDelimiter.Add (_txtDelimiter);

            Add (_frmDelimiter);

            var _frmStatusBar = new FrameView
            {
                Y = 5, Width = Dim.Percent (50), Height = Dim.Fill (2), Title = "Items:"
            };

            var _btnAddStatusBar = new Button { Y = 1, Text = "Add a StatusBar" };
            _frmStatusBar.Add (_btnAddStatusBar);

            var _btnRemoveStatusBar = new Button { Y = 1, Text = "Remove a StatusBar" };

            _btnRemoveStatusBar.X = Pos.AnchorEnd () - (Pos.Right (_btnRemoveStatusBar) - Pos.Left (_btnRemoveStatusBar));
            _frmStatusBar.Add (_btnRemoveStatusBar);

            var _btnAdd = new Button { Y = Pos.Top (_btnRemoveStatusBar) + 2, Text = " Add  " };
            _btnAdd.X = Pos.AnchorEnd () - (Pos.Right (_btnAdd) - Pos.Left (_btnAdd));
            _frmStatusBar.Add (_btnAdd);

            _lstItems = new ListView
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                Y = Pos.Top (_btnAddStatusBar) + 2,
                Width = Dim.Fill () - Dim.Width (_btnAdd) - 1,
                Height = Dim.Fill (),
                Source = new ListWrapper (new List<DynamicStatusItemList> ())
            };
            _frmStatusBar.Add (_lstItems);

            var _btnRemove = new Button { X = Pos.Left (_btnAdd), Y = Pos.Top (_btnAdd) + 1, Text = "Remove" };
            _frmStatusBar.Add (_btnRemove);

            var _btnUp = new Button { X = Pos.Right (_lstItems) + 2, Y = Pos.Top (_btnRemove) + 2, Text = "^" };
            _frmStatusBar.Add (_btnUp);

            var _btnDown = new Button { X = Pos.Right (_lstItems) + 2, Y = Pos.Top (_btnUp) + 1, Text = "v" };
            _frmStatusBar.Add (_btnDown);

            Add (_frmStatusBar);

            var _frmStatusBarDetails = new DynamicStatusBarDetails
            {
                X = Pos.Right (_frmStatusBar),
                Y = Pos.Top (_frmStatusBar),
                Width = Dim.Fill (),
                Height = Dim.Fill (4),
                Title = "StatusBar Item Details:"
            };
            Add (_frmStatusBarDetails);

            _btnUp.Accept += (s, e) =>
                              {
                                  int i = _lstItems.SelectedItem;
                                  StatusItem statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].StatusItem : null;

                                  if (statusItem != null)
                                  {
                                      StatusItem [] items = _statusBar.Items;

                                      if (i > 0)
                                      {
                                          items [i] = items [i - 1];
                                          items [i - 1] = statusItem;
                                          DataContext.Items [i] = DataContext.Items [i - 1];

                                          DataContext.Items [i - 1] =
                                              new DynamicStatusItemList (statusItem.Title, statusItem);
                                          _lstItems.SelectedItem = i - 1;
                                          _statusBar.SetNeedsDisplay ();
                                      }
                                  }
                              };

            _btnDown.Accept += (s, e) =>
                                {
                                    int i = _lstItems.SelectedItem;
                                    StatusItem statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].StatusItem : null;

                                    if (statusItem != null)
                                    {
                                        StatusItem [] items = _statusBar.Items;

                                        if (i < items.Length - 1)
                                        {
                                            items [i] = items [i + 1];
                                            items [i + 1] = statusItem;
                                            DataContext.Items [i] = DataContext.Items [i + 1];

                                            DataContext.Items [i + 1] =
                                                new DynamicStatusItemList (statusItem.Title, statusItem);
                                            _lstItems.SelectedItem = i + 1;
                                            _statusBar.SetNeedsDisplay ();
                                        }
                                    }
                                };

            var _btnOk = new Button
            {
                X = Pos.Right (_frmStatusBar) + 20, Y = Pos.Bottom (_frmStatusBarDetails), Text = "Ok"
            };
            Add (_btnOk);

            var _btnCancel = new Button { X = Pos.Right (_btnOk) + 3, Y = Pos.Top (_btnOk), Text = "Cancel" };
            _btnCancel.Accept += (s, e) => { SetFrameDetails (_currentEditStatusItem); };
            Add (_btnCancel);

            _lstItems.SelectedItemChanged += (s, e) => { SetFrameDetails (); };

            _btnOk.Accept += (s, e) =>
                              {
                                  if (string.IsNullOrEmpty (_frmStatusBarDetails.TextTitle.Text) && _currentEditStatusItem != null)
                                  {
                                      MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
                                  }
                                  else if (_currentEditStatusItem != null)
                                  {
                                      _frmStatusBarDetails.TextTitle.Text = SetTitleText (
                                                                                          _frmStatusBarDetails.TextTitle.Text,
                                                                                          _frmStatusBarDetails.TextShortcut.Text
                                                                                         );

                                      var statusItem = new DynamicStatusItem
                                      {
                                          Title = _frmStatusBarDetails.TextTitle.Text,
                                          Action = _frmStatusBarDetails.TextAction.Text,
                                          Shortcut = _frmStatusBarDetails.TextShortcut.Text
                                      };
                                      UpdateStatusItem (_currentEditStatusItem, statusItem, _lstItems.SelectedItem);
                                  }
                              };

            _btnAdd.Accept += (s, e) =>
                               {
                                   if (StatusBar == null)
                                   {
                                       MessageBox.ErrorQuery (
                                                              "StatusBar Bar Error",
                                                              "Must add a StatusBar first!",
                                                              "Ok"
                                                             );
                                       _btnAddStatusBar.SetFocus ();

                                       return;
                                   }

                                   var frameDetails = new DynamicStatusBarDetails ();
                                   DynamicStatusItem item = frameDetails.EnterStatusItem ();

                                   if (item == null)
                                   {
                                       return;
                                   }

                                   StatusItem newStatusItem = CreateNewStatusBar (item);
                                   _currentSelectedStatusBar++;
                                   _statusBar.AddItemAt (_currentSelectedStatusBar, newStatusItem);
                                   DataContext.Items.Add (new DynamicStatusItemList (newStatusItem.Title, newStatusItem));
                                   _lstItems.MoveDown ();
                                   SetFrameDetails ();
                               };

            _btnRemove.Accept += (s, e) =>
                                  {
                                      StatusItem statusItem = DataContext.Items.Count > 0
                                                                  ? DataContext.Items [_lstItems.SelectedItem].StatusItem
                                                                  : null;

                                      if (statusItem != null)
                                      {
                                          _statusBar.RemoveItem (_currentSelectedStatusBar);
                                          DataContext.Items.RemoveAt (_lstItems.SelectedItem);

                                          if (_lstItems.Source.Count > 0 && _lstItems.SelectedItem > _lstItems.Source.Count - 1)
                                          {
                                              _lstItems.SelectedItem = _lstItems.Source.Count - 1;
                                          }

                                          _lstItems.SetNeedsDisplay ();
                                          SetFrameDetails ();
                                      }
                                  };

            _lstItems.Enter += (s, e) =>
                               {
                                   StatusItem statusItem = DataContext.Items.Count > 0
                                                               ? DataContext.Items [_lstItems.SelectedItem].StatusItem
                                                               : null;
                                   SetFrameDetails (statusItem);
                               };

            _btnAddStatusBar.Accept += (s, e) =>
                                        {
                                            if (_statusBar != null)
                                            {
                                                return;
                                            }

                                            _statusBar = new StatusBar ();
                                            Add (_statusBar);
                                        };

            _btnRemoveStatusBar.Accept += (s, e) =>
                                           {
                                               if (_statusBar == null)
                                               {
                                                   return;
                                               }

                                               Remove (_statusBar);
                                               _statusBar = null;
                                               DataContext.Items = new List<DynamicStatusItemList> ();
                                               _currentStatusItem = null;
                                               _currentSelectedStatusBar = -1;
                                               SetListViewSource (_currentStatusItem, true);
                                               SetFrameDetails ();
                                           };

            SetFrameDetails ();

            var ustringConverter = new UStringValueConverter ();
            var listWrapperConverter = new ListWrapperConverter ();

            var lstItems = new Binding (this, "Items", _lstItems, "Source", listWrapperConverter);

            void SetFrameDetails (StatusItem statusItem = null)
            {
                StatusItem newStatusItem;

                if (statusItem == null)
                {
                    newStatusItem = DataContext.Items.Count > 0
                                        ? DataContext.Items [_lstItems.SelectedItem].StatusItem
                                        : null;
                }
                else
                {
                    newStatusItem = statusItem;
                }

                _currentEditStatusItem = newStatusItem;
                _frmStatusBarDetails.EditStatusItem (newStatusItem);
                bool f = _btnOk.Enabled == _frmStatusBarDetails.Enabled;

                if (!f)
                {
                    _btnOk.Enabled = _frmStatusBarDetails.Enabled;
                    _btnCancel.Enabled = _frmStatusBarDetails.Enabled;
                }
            }

            void SetListViewSource (StatusItem _currentStatusItem, bool fill = false)
            {
                DataContext.Items = new List<DynamicStatusItemList> ();
                StatusItem statusItem = _currentStatusItem;

                if (!fill)
                {
                    return;
                }

                if (statusItem != null)
                {
                    foreach (StatusItem si in _statusBar.Items)
                    {
                        DataContext.Items.Add (new DynamicStatusItemList (si.Title, si));
                    }
                }
            }

            StatusItem CreateNewStatusBar (DynamicStatusItem item)
            {
                var newStatusItem = new StatusItem (
                                                    ShortcutHelper.GetShortcutFromTag (
                                                                                       item.Shortcut,
                                                                                       StatusBar.ShortcutDelimiter
                                                                                      ),
                                                    item.Title,
                                                    _frmStatusBarDetails.CreateAction (item)
                                                   );

                return newStatusItem;
            }

            void UpdateStatusItem (
                StatusItem _currentEditStatusItem,
                DynamicStatusItem statusItem,
                int index
            )
            {
                _currentEditStatusItem = CreateNewStatusBar (statusItem);
                _statusBar.Items [index] = _currentEditStatusItem;

                if (DataContext.Items.Count == 0)
                {
                    DataContext.Items.Add (
                                           new DynamicStatusItemList (
                                                                      _currentEditStatusItem.Title,
                                                                      _currentEditStatusItem
                                                                     )
                                          );
                }

                DataContext.Items [index] = new DynamicStatusItemList (
                                                                       _currentEditStatusItem.Title,
                                                                       _currentEditStatusItem
                                                                      );
                SetFrameDetails (_currentEditStatusItem);
            }

            //_frmStatusBarDetails.Initialized += (s, e) => _frmStatusBarDetails.Enabled = false;
        }

        public DynamicStatusItemModel DataContext { get; set; }

        public static string SetTitleText (string title, string shortcut)
        {
            string txt = title;
            string [] split = title.Split ('~');

            if (split.Length > 1)
            {
                txt = split [2].Trim ();
                ;
            }

            if (string.IsNullOrEmpty (shortcut))
            {
                return txt;
            }

            return $"~{shortcut}~ {txt}";
        }
    }

    public class DynamicStatusItem
    {
        public string Action { get; set; } = "";
        public string Shortcut { get; set; }
        public string Title { get; set; } = "New";
    }

    public class DynamicStatusItemList
    {
        public DynamicStatusItemList () { }

        public DynamicStatusItemList (string title, StatusItem statusItem)
        {
            Title = title;
            StatusItem = statusItem;
        }

        public StatusItem StatusItem { get; set; }
        public string Title { get; set; }
        public override string ToString () { return $"{Title}, {StatusItem}"; }
    }

    public class DynamicStatusItemModel : INotifyPropertyChanged
    {
        private List<DynamicStatusItemList> _items;
        private string _statusBar;
        public DynamicStatusItemModel () { Items = []; }

        public List<DynamicStatusItemList> Items
        {
            get => _items;
            set
            {
                if (value == _items)
                {
                    return;
                }

                _items = value;

                PropertyChanged?.Invoke (
                                         this,
                                         new PropertyChangedEventArgs (GetPropertyName ())
                                        );
            }
        }

        public string StatusBar
        {
            get => _statusBar;
            set
            {
                if (value == _statusBar)
                {
                    return;
                }

                _statusBar = value;

                PropertyChanged?.Invoke (
                                         this,
                                         new PropertyChangedEventArgs (GetPropertyName ())
                                        );
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string GetPropertyName ([CallerMemberName] string propertyName = null) { return propertyName; }
    }

    public interface IValueConverter
    {
        object Convert (object value, object parameter = null);
    }

    public class ListWrapperConverter : IValueConverter
    {
        public object Convert (object value, object parameter = null) { return new ListWrapper ((IList)value); }
    }

    public class UStringValueConverter : IValueConverter
    {
        public object Convert (object value, object parameter = null)
        {
            byte [] data = Encoding.ASCII.GetBytes (value.ToString () ?? string.Empty);

            return StringExtensions.ToString (data);
        }
    }
}
