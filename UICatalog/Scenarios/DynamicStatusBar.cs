using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dynamic StatusBar", "Demonstrates how to add and remove a StatusBar and change items dynamically.")]
[ScenarioCategory ("Arrangement")]
public class DynamicStatusBar : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        Application.Run<DynamicStatusBarSample> ().Dispose ();
        Application.Shutdown ();
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
        private Shortcut _statusItem;

        public DynamicStatusBarDetails (Shortcut statusItem = null) : this ()
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
                                        TextShortcut.Text = e.ToString ();

                                    };
            Add (TextShortcut);

            var _btnShortcut = new Button
            {
                X = Pos.X (_lblShortcut), Y = Pos.Bottom (TextShortcut) + 1, Text = "Clear Shortcut"
            };
            _btnShortcut.Accepting += (s, e) => { TextShortcut.Text = ""; };
            Add (_btnShortcut);
        }

        public TextView TextAction { get; }
        public TextField TextShortcut { get; }
        public TextField TextTitle { get; }
        public Action CreateAction (DynamicStatusItem item) { return () => MessageBox.ErrorQuery (item.Title, item.Action, "Ok"); }

        public void EditStatusItem (Shortcut statusItem)
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

            TextShortcut.Text = statusItem.Key;
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

            btnOk.Accepting += (s, e) =>
                              {
                                  if (string.IsNullOrEmpty (TextTitle.Text))
                                  {
                                      MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
                                  }
                                  else
                                  {

                                      valid = true;
                                      Application.RequestStop ();
                                  }
                              };
            var btnCancel = new Button { Text = "Cancel" };

            btnCancel.Accepting += (s, e) =>
                                  {
                                      TextTitle.Text = string.Empty;
                                      Application.RequestStop ();
                                  };
            var dialog = new Dialog { Title = "Enter the menu details.", Buttons = [btnOk, btnCancel], Height = Dim.Auto (DimAutoStyle.Content, 17, Application.Screen.Height) };

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
        private Shortcut _currentEditStatusItem;
        private int _currentSelectedStatusBar = -1;
        private Shortcut _currentStatusItem;
        private StatusBar _statusBar;

        public DynamicStatusBarSample ()
        {
            DataContext = new DynamicStatusItemModel ();

            Title = $"{Application.QuitKey} to Quit - Scenario: Dynamic StatusBar";

            var _frmStatusBar = new FrameView
            {
                Y = 5, Width = Dim.Percent (50), Height = Dim.Fill (2), Title = "Items:"
            };

            var _btnAddStatusBar = new Button { Y = 1, Text = "Add a StatusBar" };
            _frmStatusBar.Add (_btnAddStatusBar);

            var _btnRemoveStatusBar = new Button { Y = 1, Text = "Remove a StatusBar" };

            _btnRemoveStatusBar.X = Pos.AnchorEnd ();
            _frmStatusBar.Add (_btnRemoveStatusBar);

            var _btnAdd = new Button { Y = Pos.Top (_btnRemoveStatusBar) + 2, Text = " Add  " };
            _btnAdd.X = Pos.AnchorEnd ();
            _frmStatusBar.Add (_btnAdd);

            _lstItems = new ListView
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                Y = Pos.Top (_btnAddStatusBar) + 2,
                Width = Dim.Fill () - Dim.Width (_btnAdd) - 1,
                Height = Dim.Fill (),
                Source = new ListWrapper<DynamicStatusItemList> ([])
            };
            _frmStatusBar.Add (_lstItems);

            var _btnRemove = new Button { X = Pos.Left (_btnAdd), Y = Pos.Top (_btnAdd) + 1, Text = "Remove" };
            _frmStatusBar.Add (_btnRemove);

            var _btnUp = new Button { X = Pos.Right (_lstItems) + 2, Y = Pos.Top (_btnRemove) + 2, Text = Glyphs.UpArrow.ToString () };
            _frmStatusBar.Add (_btnUp);

            var _btnDown = new Button { X = Pos.Right (_lstItems) + 2, Y = Pos.Top (_btnUp) + 1, Text = Glyphs.DownArrow.ToString () };
            _frmStatusBar.Add (_btnDown);

            Add (_frmStatusBar);

            var _frmStatusBarDetails = new DynamicStatusBarDetails
            {
                X = Pos.Right (_frmStatusBar),
                Y = Pos.Top (_frmStatusBar),
                Width = Dim.Fill (),
                Height = Dim.Fill (4),
                Title = "Shortcut Details:"
            };
            Add (_frmStatusBarDetails);

            _btnUp.Accepting += (s, e) =>
                              {
                                  int i = _lstItems.SelectedItem;
                                  Shortcut statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].Shortcut : null;

                                  if (statusItem != null)
                                  {
                                      Shortcut [] items = _statusBar.Subviews.Cast<Shortcut> ().ToArray ();

                                      if (i > 0)
                                      {
                                          items [i] = items [i - 1];
                                          items [i - 1] = statusItem;
                                          DataContext.Items [i] = DataContext.Items [i - 1];

                                          DataContext.Items [i - 1] =
                                              new DynamicStatusItemList (statusItem.Title, statusItem);
                                          _lstItems.SelectedItem = i - 1;
                                          _statusBar.SetNeedsDraw ();
                                      }
                                  }
                              };

            _btnDown.Accepting += (s, e) =>
                                {
                                    int i = _lstItems.SelectedItem;
                                    Shortcut statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].Shortcut : null;

                                    if (statusItem != null)
                                    {
                                        Shortcut [] items = _statusBar.Subviews.Cast<Shortcut> ().ToArray ();

                                        if (i < items.Length - 1)
                                        {
                                            items [i] = items [i + 1];
                                            items [i + 1] = statusItem;
                                            DataContext.Items [i] = DataContext.Items [i + 1];

                                            DataContext.Items [i + 1] =
                                                new DynamicStatusItemList (statusItem.Title, statusItem);
                                            _lstItems.SelectedItem = i + 1;
                                            _statusBar.SetNeedsDraw ();
                                        }
                                    }
                                };

            var _btnOk = new Button
            {
                X = Pos.Right (_frmStatusBar) + 20, Y = Pos.Bottom (_frmStatusBarDetails), Text = "Ok"
            };
            Add (_btnOk);

            var _btnCancel = new Button { X = Pos.Right (_btnOk) + 3, Y = Pos.Top (_btnOk), Text = "Cancel" };
            _btnCancel.Accepting += (s, e) => { SetFrameDetails (_currentEditStatusItem); };
            Add (_btnCancel);

            _lstItems.SelectedItemChanged += (s, e) => { SetFrameDetails (); };

            _btnOk.Accepting += (s, e) =>
                              {
                                  if (string.IsNullOrEmpty (_frmStatusBarDetails.TextTitle.Text) && _currentEditStatusItem != null)
                                  {
                                      MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
                                  }
                                  else if (_currentEditStatusItem != null)
                                  {

                                      var statusItem = new DynamicStatusItem
                                      {
                                          Title = _frmStatusBarDetails.TextTitle.Text,
                                          Action = _frmStatusBarDetails.TextAction.Text,
                                          Shortcut = _frmStatusBarDetails.TextShortcut.Text
                                      };
                                      UpdateStatusItem (_currentEditStatusItem, statusItem, _lstItems.SelectedItem);
                                  }
                              };

            _btnAdd.Accepting += (s, e) =>
                               {
                                   //if (StatusBar == null)
                                   //{
                                   //    MessageBox.ErrorQuery (
                                   //                           "StatusBar Bar Error",
                                   //                           "Must add a StatusBar first!",
                                   //                           "Ok"
                                   //                          );
                                   //    _btnAddStatusBar.SetFocus ();

                                   //    return;
                                   //}

                                   var frameDetails = new DynamicStatusBarDetails ();
                                   DynamicStatusItem item = frameDetails.EnterStatusItem ();

                                   if (item == null)
                                   {
                                       return;
                                   }

                                   Shortcut newStatusItem = CreateNewStatusBar (item);
                                   _currentSelectedStatusBar++;
                                   _statusBar.AddShortcutAt (_currentSelectedStatusBar, newStatusItem);
                                   DataContext.Items.Add (new DynamicStatusItemList (newStatusItem.Title, newStatusItem));
                                   _lstItems.MoveDown ();
                                   SetFrameDetails ();
                               };

            _btnRemove.Accepting += (s, e) =>
                                  {
                                      Shortcut statusItem = DataContext.Items.Count > 0
                                                                  ? DataContext.Items [_lstItems.SelectedItem].Shortcut
                                                                  : null;

                                      if (statusItem != null)
                                      {
                                          _statusBar.RemoveShortcut (_currentSelectedStatusBar);
                                          statusItem.Dispose ();
                                          DataContext.Items.RemoveAt (_lstItems.SelectedItem);

                                          if (_lstItems.Source.Count > 0 && _lstItems.SelectedItem > _lstItems.Source.Count - 1)
                                          {
                                              _lstItems.SelectedItem = _lstItems.Source.Count - 1;
                                          }

                                          _lstItems.SetNeedsDraw ();
                                          SetFrameDetails ();
                                      }
                                  };

            _lstItems.HasFocusChanging += (s, e) =>
                               {
                                   Shortcut statusItem = DataContext.Items.Count > 0
                                                               ? DataContext.Items [_lstItems.SelectedItem].Shortcut
                                                               : null;
                                   SetFrameDetails (statusItem);
                               };

            _btnAddStatusBar.Accepting += (s, e) =>
                                        {
                                            if (_statusBar != null)
                                            {
                                                return;
                                            }

                                            _statusBar = new StatusBar ();
                                            Add (_statusBar);
                                        };

            _btnRemoveStatusBar.Accepting += (s, e) =>
                                           {
                                               if (_statusBar == null)
                                               {
                                                   return;
                                               }

                                               Remove (_statusBar);
                                               _statusBar.Dispose ();
                                               _statusBar = null;
                                               DataContext.Items = [];
                                               _currentStatusItem = null;
                                               _currentSelectedStatusBar = -1;
                                               SetListViewSource (_currentStatusItem, true);
                                               SetFrameDetails ();
                                           };

            SetFrameDetails ();

            var ustringConverter = new UStringValueConverter ();
            var listWrapperConverter = new ListWrapperConverter<DynamicStatusItemList> ();

            var lstItems = new Binding (this, "Items", _lstItems, "Source", listWrapperConverter);

            void SetFrameDetails (Shortcut statusItem = null)
            {
                Shortcut newStatusItem;

                if (statusItem == null)
                {
                    newStatusItem = DataContext.Items.Count > 0
                                        ? DataContext.Items [_lstItems.SelectedItem].Shortcut
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

            void SetListViewSource (Shortcut _currentStatusItem, bool fill = false)
            {
                DataContext.Items = [];
                Shortcut statusItem = _currentStatusItem;

                if (!fill)
                {
                    return;
                }

                if (statusItem != null)
                {
                    foreach (Shortcut si in _statusBar.Subviews.Cast<Shortcut> ())
                    {
                        DataContext.Items.Add (new DynamicStatusItemList (si.Title, si));
                    }
                }
            }

            Shortcut CreateNewStatusBar (DynamicStatusItem item)
            {
                var newStatusItem = new Shortcut (item.Shortcut, item.Title, _frmStatusBarDetails.CreateAction (item));

                return newStatusItem;
            }

            void UpdateStatusItem (
                Shortcut _currentEditStatusItem,
                DynamicStatusItem statusItem,
                int index
            )
            {
                _statusBar.Subviews [index].Title = statusItem.Title;
                ((Shortcut)_statusBar.Subviews [index]).Action = _frmStatusBarDetails.CreateAction (statusItem);
                ((Shortcut)_statusBar.Subviews [index]).Key = statusItem.Shortcut;

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

        public DynamicStatusItemList (string title, Shortcut statusItem)
        {
            Title = title;
            Shortcut = statusItem;
        }

        public Shortcut Shortcut { get; set; }
        public string Title { get; set; }
        public override string ToString () { return $"{Title}, {Shortcut.Key}"; }
    }

    public class DynamicStatusItemModel : INotifyPropertyChanged
    {
        private ObservableCollection<DynamicStatusItemList> _items;
        private string _statusBar;
        public DynamicStatusItemModel () { Items = []; }

        public ObservableCollection<DynamicStatusItemList> Items
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

    public class ListWrapperConverter<T> : IValueConverter
    {
        public object Convert (object value, object parameter = null) { return new ListWrapper<T> ((ObservableCollection<T>)value); }
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
