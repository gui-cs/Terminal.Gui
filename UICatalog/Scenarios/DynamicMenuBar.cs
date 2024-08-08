using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dynamic MenuBar", "Demonstrates how to change a MenuBar dynamically.")]
[ScenarioCategory ("Top Level Windows")]
[ScenarioCategory ("Menus")]
public class DynamicMenuBar : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        DynamicMenuBarSample appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
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

    public class DynamicMenuBarDetails : FrameView
    {
        private bool _hasParent;
        private MenuItem _menuItem;

        public DynamicMenuBarDetails (MenuItem menuItem = null, bool hasParent = false) : this ()
        {
            _menuItem = menuItem;
            _hasParent = hasParent;
            Title = menuItem == null ? "Adding New Menu." : "Editing Menu.";
        }

        public DynamicMenuBarDetails ()
        {
            var _lblTitle = new Label { Y = 1, Text = "Title:" };
            Add (_lblTitle);

            TextTitle = new () { X = Pos.Right (_lblTitle) + 2, Y = Pos.Top (_lblTitle), Width = Dim.Fill () };
            Add (TextTitle);

            var _lblHelp = new Label { X = Pos.Left (_lblTitle), Y = Pos.Bottom (_lblTitle) + 1, Text = "Help:" };
            Add (_lblHelp);

            TextHelp = new () { X = Pos.Left (TextTitle), Y = Pos.Top (_lblHelp), Width = Dim.Fill () };
            Add (TextHelp);

            var _lblAction = new Label { X = Pos.Left (_lblTitle), Y = Pos.Bottom (_lblHelp) + 1, Text = "Action:" };
            Add (_lblAction);

            TextAction = new ()
            {
                X = Pos.Left (TextTitle), Y = Pos.Top (_lblAction), Width = Dim.Fill (), Height = 5
            };
            Add (TextAction);

            CkbIsTopLevel = new ()
            {
                X = Pos.Left (_lblTitle), Y = Pos.Bottom (_lblAction) + 5, Text = "IsTopLevel"
            };
            Add (CkbIsTopLevel);

            CkbSubMenu = new ()
            {
                X = Pos.Left (_lblTitle),
                Y = Pos.Bottom (CkbIsTopLevel),
                CheckedState = (_menuItem == null ? !_hasParent : HasSubMenus (_menuItem)) ? CheckState.Checked : CheckState.UnChecked,
                Text = "Has sub-menus"
            };
            Add (CkbSubMenu);

            CkbNullCheck = new ()
            {
                X = Pos.Left (_lblTitle), Y = Pos.Bottom (CkbSubMenu), Text = "Allow null checked"
            };
            Add (CkbNullCheck);

            var _rChkLabels = new [] { "NoCheck", "Checked", "Radio" };

            RbChkStyle = new ()
            {
                X = Pos.Left (_lblTitle), Y = Pos.Bottom (CkbSubMenu) + 1, RadioLabels = _rChkLabels
            };
            Add (RbChkStyle);

            var _lblShortcut = new Label
            {
                X = Pos.Right (CkbSubMenu) + 10, Y = Pos.Top (CkbSubMenu), Text = "Shortcut:"
            };
            Add (_lblShortcut);

            TextShortcutKey = new ()
            {
                X = Pos.X (_lblShortcut), Y = Pos.Bottom (_lblShortcut), Width = Dim.Fill (), ReadOnly = true
            };

            TextShortcutKey.KeyDown += (s, e) =>
                                    {
                                        TextShortcutKey.Text = e.ToString ();

                                    };

            Add (TextShortcutKey);

            var _btnShortcut = new Button
            {
                X = Pos.X (_lblShortcut), Y = Pos.Bottom (TextShortcutKey) + 1, Text = "Clear Shortcut"
            };
            _btnShortcut.Accept += (s, e) => { TextShortcutKey.Text = ""; };
            Add (_btnShortcut);

            CkbIsTopLevel.CheckedStateChanging += (s, e) =>
                                     {
                                         if ((_menuItem != null && _menuItem.Parent != null && e.NewValue == CheckState.Checked)
                                             || (_menuItem == null && _hasParent && e.NewValue == CheckState.Checked))
                                         {
                                             MessageBox.ErrorQuery (
                                                                    "Invalid IsTopLevel",
                                                                    "Only menu bar can have top level menu item!",
                                                                    "Ok"
                                                                   );
                                             e.Cancel = true;

                                             return;
                                         }

                                         if (e.NewValue == CheckState.Checked)
                                         {
                                             CkbSubMenu.CheckedState = CheckState.UnChecked;
                                             CkbSubMenu.SetNeedsDisplay ();
                                             TextHelp.Enabled = true;
                                             TextAction.Enabled = true;
                                             TextShortcutKey.Enabled = true;
                                         }
                                         else
                                         {
                                             if ((_menuItem == null && !_hasParent) || _menuItem.Parent == null)
                                             {
                                                 CkbSubMenu.CheckedState = CheckState.Checked;
                                                 CkbSubMenu.SetNeedsDisplay ();
                                                 TextShortcutKey.Enabled = false;
                                             }

                                             TextHelp.Text = "";
                                             TextHelp.Enabled = false;
                                             TextAction.Text = "";

                                             TextShortcutKey.Enabled =
                                                 e.NewValue == CheckState.Checked && CkbSubMenu.CheckedState == CheckState.UnChecked;
                                         }
                                     };

            CkbSubMenu.CheckedStateChanging += (s, e) =>
                                  {
                                      if (e.NewValue == CheckState.Checked)
                                      {
                                          CkbIsTopLevel.CheckedState = CheckState.UnChecked;
                                          CkbIsTopLevel.SetNeedsDisplay ();
                                          TextHelp.Text = "";
                                          TextHelp.Enabled = false;
                                          TextAction.Text = "";
                                          TextAction.Enabled = false;
                                          TextShortcutKey.Text = "";
                                          TextShortcutKey.Enabled = false;
                                      }
                                      else
                                      {
                                          if (!_hasParent)
                                          {
                                              CkbIsTopLevel.CheckedState = CheckState.Checked;
                                              CkbIsTopLevel.SetNeedsDisplay ();
                                              TextShortcutKey.Enabled = true;
                                          }

                                          TextHelp.Enabled = true;
                                          TextAction.Enabled = true;

                                          if (_hasParent)
                                          {
                                              TextShortcutKey.Enabled = CkbIsTopLevel.CheckedState == CheckState.UnChecked
                                                                     && e.NewValue == CheckState.UnChecked;
                                          }
                                      }
                                  };

            CkbNullCheck.CheckedStateChanging += (s, e) =>
                                    {
                                        if (_menuItem != null)
                                        {
                                            _menuItem.AllowNullChecked = e.NewValue == CheckState.Checked;
                                        }
                                    };

            //Add (_frmMenuDetails);
        }

        public CheckBox CkbIsTopLevel { get; }
        public CheckBox CkbNullCheck { get; }
        public CheckBox CkbSubMenu { get; }
        public RadioGroup RbChkStyle { get; }
        public TextView TextAction { get; }
        public TextField TextHelp { get; }
        public TextField TextHotKey { get; }
        public TextField TextShortcutKey { get; }
        public TextField TextTitle { get; }

        public Action CreateAction (MenuItem menuItem, DynamicMenuItem item)
        {
            switch (menuItem.CheckType)
            {
                case MenuItemCheckStyle.NoCheck:
                    return () => MessageBox.ErrorQuery (item.Title, item.Action, "Ok");
                case MenuItemCheckStyle.Checked:
                    return menuItem.ToggleChecked;
                case MenuItemCheckStyle.Radio:
                    break;
            }

            return () =>
                   {
                       menuItem.Checked = true;
                       var parent = menuItem?.Parent as MenuBarItem;

                       if (parent != null)
                       {
                           MenuItem [] childrens = parent.Children;

                           for (var i = 0; i < childrens.Length; i++)
                           {
                               MenuItem child = childrens [i];

                               if (child != menuItem)
                               {
                                   child.Checked = false;
                               }
                           }
                       }
                   };
        }

        public void EditMenuBarItem (MenuItem menuItem)
        {
            if (menuItem == null)
            {
                _hasParent = false;
                Enabled = false;
                CleanEditMenuBarItem ();

                return;
            }

            _hasParent = menuItem.Parent != null;
            Enabled = true;
            _menuItem = menuItem;
            TextTitle.Text = menuItem?.Title ?? "";
            TextHelp.Text = menuItem?.Help ?? "";

            TextAction.Text = menuItem != null && menuItem.Action != null
                                  ? GetTargetAction (menuItem.Action)
                                  : string.Empty;
            CkbIsTopLevel.CheckedState = IsTopLevel (menuItem) ? CheckState.Checked : CheckState.UnChecked;
            CkbSubMenu.CheckedState = HasSubMenus (menuItem) ? CheckState.Checked : CheckState.UnChecked;
            CkbNullCheck.CheckedState = menuItem.AllowNullChecked ? CheckState.Checked : CheckState.UnChecked;
            TextHelp.Enabled = CkbSubMenu.CheckedState == CheckState.UnChecked;
            TextAction.Enabled = CkbSubMenu.CheckedState == CheckState.UnChecked;
            RbChkStyle.SelectedItem = (int)(menuItem?.CheckType ?? MenuItemCheckStyle.NoCheck);
            TextShortcutKey.Text = menuItem?.ShortcutTag ?? "";

            TextShortcutKey.Enabled = CkbIsTopLevel.CheckedState == CheckState.Checked && CkbSubMenu.CheckedState == CheckState.UnChecked
                                   || CkbIsTopLevel.CheckedState == CheckState.UnChecked && CkbSubMenu.CheckedState == CheckState.UnChecked;
        }

        public DynamicMenuItem EnterMenuItem ()
        {
            var valid = false;

            if (_menuItem == null)
            {
                var m = new DynamicMenuItem ();
                TextTitle.Text = m.Title;
                TextHelp.Text = m.Help;
                TextAction.Text = m.Action;
                CkbIsTopLevel.CheckedState = CheckState.UnChecked;
                CkbSubMenu.CheckedState = !_hasParent ? CheckState.Checked : CheckState.UnChecked;
                CkbNullCheck.CheckedState = CheckState.UnChecked;
                TextHelp.Enabled = _hasParent;
                TextAction.Enabled = _hasParent;
                TextShortcutKey.Enabled = _hasParent;
            }
            else
            {
                EditMenuBarItem (_menuItem);
            }

            var btnOk = new Button { IsDefault = true, Text = "Ok" };

            btnOk.Accept += (s, e) =>
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

            btnCancel.Accept += (s, e) =>
                                {
                                    TextTitle.Text = string.Empty;
                                    Application.RequestStop ();
                                };

            var dialog = new Dialog
                { Title = "Enter the menu details.", Buttons = [btnOk, btnCancel], Height = Dim.Auto (DimAutoStyle.Content, 22, Driver.Rows) };

            Width = Dim.Fill ();
            Height = Dim.Fill () - 1;
            dialog.Add (this);
            TextTitle.SetFocus ();
            TextTitle.CursorPosition = TextTitle.Text.Length;
            Application.Run (dialog);
            dialog.Dispose ();

            if (valid)
            {
                return new ()
                {
                    Title = TextTitle.Text,
                    Help = TextHelp.Text,
                    Action = TextAction.Text,
                    IsTopLevel = CkbIsTopLevel?.CheckedState == CheckState.Checked,
                    HasSubMenu = CkbSubMenu?.CheckedState == CheckState.Checked,
                    CheckStyle = RbChkStyle.SelectedItem == 0 ? MenuItemCheckStyle.NoCheck :
                                 RbChkStyle.SelectedItem == 1 ? MenuItemCheckStyle.Checked :
                                 MenuItemCheckStyle.Radio,
                    ShortcutKey = TextShortcutKey.Text,
                    AllowNullChecked = CkbNullCheck?.CheckedState == CheckState.Checked,
                };
            }

            return null;
        }

        public void UpdateParent (ref MenuItem menuItem)
        {
            var parent = menuItem.Parent as MenuBarItem;
            int idx = parent.GetChildrenIndex (menuItem);

            if (!(menuItem is MenuBarItem))
            {
                menuItem = new MenuBarItem (menuItem.Title, new MenuItem [] { }, menuItem.Parent);

                if (idx > -1)
                {
                    parent.Children [idx] = menuItem;
                }
            }
            else
            {
                menuItem = new (
                                menuItem.Title,
                                menuItem.Help,
                                CreateAction (menuItem, new ()),
                                null,
                                menuItem.Parent
                               );

                if (idx > -1)
                {
                    parent.Children [idx] = menuItem;
                }
            }
        }

        private void CleanEditMenuBarItem ()
        {
            TextTitle.Text = "";
            TextHelp.Text = "";
            TextAction.Text = "";
            CkbIsTopLevel.CheckedState = CheckState.UnChecked;
            CkbSubMenu.CheckedState = CheckState.UnChecked;
            RbChkStyle.SelectedItem = (int)MenuItemCheckStyle.NoCheck;
            TextShortcutKey.Text = "";
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

            return v == null || !(v is DynamicMenuItem item) ? string.Empty : item.Action;
        }

        private bool HasSubMenus (MenuItem menuItem)
        {
            var menuBarItem = menuItem as MenuBarItem;

            if (menuBarItem != null && menuBarItem.Children != null && (menuBarItem.Children.Length > 0 || menuBarItem.Action == null))
            {
                return true;
            }

            return false;
        }

        private bool IsTopLevel (MenuItem menuItem)
        {
            var topLevel = menuItem as MenuBarItem;

            if (topLevel != null && topLevel.Parent == null && (topLevel.Children == null || topLevel.Children.Length == 0) && topLevel.Action != null)
            {
                return true;
            }

            return false;
        }
    }

    public class DynamicMenuBarSample : Window
    {
        private readonly ListView _lstMenus;
        private MenuItem _currentEditMenuBarItem;
        private MenuItem _currentMenuBarItem;
        private int _currentSelectedMenuBar;
        private MenuBar _menuBar;

        public DynamicMenuBarSample ()
        {
            DataContext = new ();

            var _frmDelimiter = new FrameView
            {
                X = Pos.Center (),
                Y = 3,
                Width = 25,
                Height = 4,
                Title = "Shortcut Delimiter:"
            };

            var _txtDelimiter = new TextField
            {
                X = Pos.Center (), Width = 2, Text = Key.ShortcutDelimiter.ToString ()
            };

            _txtDelimiter.TextChanging += (s, e) =>
                                          {
                                              if (!string.IsNullOrEmpty (e.NewValue))
                                              {
                                                  Key.ShortcutDelimiter = e.NewValue.ToRunes () [0];
                                              }
                                              else
                                              {
                                                  e.Cancel = true;
                                                  _txtDelimiter.SelectAll ();
                                              }
                                          };
            _txtDelimiter.TextChanged += (s, _) => _txtDelimiter.SelectAll ();
            _frmDelimiter.Add (_txtDelimiter);
            _txtDelimiter.SelectAll ();
            Add (_frmDelimiter);

            var _frmMenu = new FrameView { Y = 7, Width = Dim.Percent (50), Height = Dim.Fill (), Title = "Menus:" };

            var _btnAddMenuBar = new Button { Y = 1, Text = "Add a MenuBar" };
            _frmMenu.Add (_btnAddMenuBar);

            var _btnMenuBarUp = new Button { X = Pos.Center (), Text = CM.Glyphs.UpArrow.ToString () };
            _frmMenu.Add (_btnMenuBarUp);

            var _btnMenuBarDown = new Button { X = Pos.Center (), Y = Pos.Bottom (_btnMenuBarUp), Text = CM.Glyphs.DownArrow.ToString () };
            _frmMenu.Add (_btnMenuBarDown);

            var _btnRemoveMenuBar = new Button { Y = 1, Text = "Remove a MenuBar" };

            _btnRemoveMenuBar.X = Pos.AnchorEnd (0) - (Pos.Right (_btnRemoveMenuBar) - Pos.Left (_btnRemoveMenuBar));
            _frmMenu.Add (_btnRemoveMenuBar);

            var _btnPrevious = new Button
            {
                X = Pos.Left (_btnAddMenuBar), Y = Pos.Top (_btnAddMenuBar) + 2, Text = CM.Glyphs.LeftArrow.ToString ()
            };
            _frmMenu.Add (_btnPrevious);

            var _btnAdd = new Button { Y = Pos.Top (_btnPrevious) + 2, Text = " Add  " };
            _btnAdd.X = Pos.AnchorEnd ();
            _frmMenu.Add (_btnAdd);

            var _btnNext = new Button { X = Pos.X (_btnAdd), Y = Pos.Top (_btnPrevious), Text = CM.Glyphs.RightArrow.ToString () };
            _frmMenu.Add (_btnNext);

            var _lblMenuBar = new Label
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                TextAlignment = Alignment.Center,
                X = Pos.Right (_btnPrevious) + 1,
                Y = Pos.Top (_btnPrevious),

                Width = Dim.Fill () - Dim.Func (() => _btnAdd.Frame.Width + 1),
                Height = 1
            };
            _frmMenu.Add (_lblMenuBar);
            _lblMenuBar.WantMousePositionReports = true;
            _lblMenuBar.CanFocus = true;

            var _lblParent = new Label
            {
                TextAlignment = Alignment.Center,
                X = Pos.Right (_btnPrevious) + 1,
                Y = Pos.Top (_btnPrevious) + 1,

                Width = Dim.Fill () - Dim.Width (_btnAdd) - 1
            };
            _frmMenu.Add (_lblParent);

            var _btnPreviowsParent = new Button
            {
                X = Pos.Left (_btnAddMenuBar), Y = Pos.Top (_btnPrevious) + 1, Text = ".."
            };
            _frmMenu.Add (_btnPreviowsParent);

            _lstMenus = new ()
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                X = Pos.Right (_btnPrevious) + 1,
                Y = Pos.Top (_btnPrevious) + 2,
                Width = _lblMenuBar.Width,
                Height = Dim.Fill (),
                Source = new ListWrapper<DynamicMenuItemList> ([])
            };
            _frmMenu.Add (_lstMenus);

            _lblMenuBar.TabIndex = _btnPrevious.TabIndex + 1;
            _lstMenus.TabIndex = _lblMenuBar.TabIndex + 1;
            _btnNext.TabIndex = _lstMenus.TabIndex + 1;
            _btnAdd.TabIndex = _btnNext.TabIndex + 1;

            var _btnRemove = new Button { X = Pos.Left (_btnAdd), Y = Pos.Top (_btnAdd) + 1, Text = "Remove" };
            _frmMenu.Add (_btnRemove);

            var _btnUp = new Button { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (_btnRemove) + 2, Text = CM.Glyphs.UpArrow.ToString () };
            _frmMenu.Add (_btnUp);

            var _btnDown = new Button { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (_btnUp) + 1, Text = CM.Glyphs.DownArrow.ToString () };
            _frmMenu.Add (_btnDown);

            Add (_frmMenu);

            var _frmMenuDetails = new DynamicMenuBarDetails
            {
                X = Pos.Right (_frmMenu),
                Y = Pos.Top (_frmMenu),
                Width = Dim.Fill (),
                Height = Dim.Fill (2),
                Title = "Menu Details:"
            };
            Add (_frmMenuDetails);

            _btnMenuBarUp.Accept += (s, e) =>
                                    {
                                        int i = _currentSelectedMenuBar;

                                        MenuBarItem menuItem = _menuBar != null && _menuBar.Menus.Length > 0
                                                                   ? _menuBar.Menus [i]
                                                                   : null;

                                        if (menuItem != null)
                                        {
                                            MenuBarItem [] menus = _menuBar.Menus;

                                            if (i > 0)
                                            {
                                                menus [i] = menus [i - 1];
                                                menus [i - 1] = menuItem;
                                                _currentSelectedMenuBar = i - 1;
                                                _menuBar.SetNeedsDisplay ();
                                            }
                                        }
                                    };

            _btnMenuBarDown.Accept += (s, e) =>
                                      {
                                          int i = _currentSelectedMenuBar;

                                          MenuBarItem menuItem = _menuBar != null && _menuBar.Menus.Length > 0
                                                                     ? _menuBar.Menus [i]
                                                                     : null;

                                          if (menuItem != null)
                                          {
                                              MenuBarItem [] menus = _menuBar.Menus;

                                              if (i < menus.Length - 1)
                                              {
                                                  menus [i] = menus [i + 1];
                                                  menus [i + 1] = menuItem;
                                                  _currentSelectedMenuBar = i + 1;
                                                  _menuBar.SetNeedsDisplay ();
                                              }
                                          }
                                      };

            _btnUp.Accept += (s, e) =>
                             {
                                 int i = _lstMenus.SelectedItem;
                                 MenuItem menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [i].MenuItem : null;

                                 if (menuItem != null)
                                 {
                                     MenuItem [] childrens = ((MenuBarItem)_currentMenuBarItem).Children;

                                     if (i > 0)
                                     {
                                         childrens [i] = childrens [i - 1];
                                         childrens [i - 1] = menuItem;
                                         DataContext.Menus [i] = DataContext.Menus [i - 1];

                                         DataContext.Menus [i - 1] =
                                             new () { Title = menuItem.Title, MenuItem = menuItem };
                                         _lstMenus.SelectedItem = i - 1;
                                     }
                                 }
                             };

            _btnDown.Accept += (s, e) =>
                               {
                                   int i = _lstMenus.SelectedItem;
                                   MenuItem menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [i].MenuItem : null;

                                   if (menuItem != null)
                                   {
                                       MenuItem [] childrens = ((MenuBarItem)_currentMenuBarItem).Children;

                                       if (i < childrens.Length - 1)
                                       {
                                           childrens [i] = childrens [i + 1];
                                           childrens [i + 1] = menuItem;
                                           DataContext.Menus [i] = DataContext.Menus [i + 1];

                                           DataContext.Menus [i + 1] =
                                               new () { Title = menuItem.Title, MenuItem = menuItem };
                                           _lstMenus.SelectedItem = i + 1;
                                       }
                                   }
                               };

            _btnPreviowsParent.Accept += (s, e) =>
                                         {
                                             if (_currentMenuBarItem != null && _currentMenuBarItem.Parent != null)
                                             {
                                                 MenuItem mi = _currentMenuBarItem;
                                                 _currentMenuBarItem = _currentMenuBarItem.Parent as MenuBarItem;
                                                 SetListViewSource (_currentMenuBarItem, true);
                                                 int i = ((MenuBarItem)_currentMenuBarItem).GetChildrenIndex (mi);

                                                 if (i > -1)
                                                 {
                                                     _lstMenus.SelectedItem = i;
                                                 }

                                                 if (_currentMenuBarItem.Parent != null)
                                                 {
                                                     DataContext.Parent = _currentMenuBarItem.Title;
                                                 }
                                                 else
                                                 {
                                                     DataContext.Parent = string.Empty;
                                                 }
                                             }
                                             else
                                             {
                                                 DataContext.Parent = string.Empty;
                                             }
                                         };

            var _btnOk = new Button { X = Pos.Right (_frmMenu) + 20, Y = Pos.Bottom (_frmMenuDetails), Text = "Ok" };
            Add (_btnOk);

            var _btnCancel = new Button { X = Pos.Right (_btnOk) + 3, Y = Pos.Top (_btnOk), Text = "Cancel" };
            _btnCancel.Accept += (s, e) => { SetFrameDetails (_currentEditMenuBarItem); };
            Add (_btnCancel);

            _lstMenus.SelectedItemChanged += (s, e) => { SetFrameDetails (); };

            _btnOk.Accept += (s, e) =>
                             {
                                 if (string.IsNullOrEmpty (_frmMenuDetails.TextTitle.Text) && _currentEditMenuBarItem != null)
                                 {
                                     MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
                                 }
                                 else if (_currentEditMenuBarItem != null)
                                 {
                                     var menuItem = new DynamicMenuItem
                                     {
                                         Title = _frmMenuDetails.TextTitle.Text,
                                         Help = _frmMenuDetails.TextHelp.Text,
                                         Action = _frmMenuDetails.TextAction.Text,
                                         HotKey = _frmMenuDetails.TextHotKey.Text,
                                         IsTopLevel = _frmMenuDetails.CkbIsTopLevel?.CheckedState == CheckState.Checked,
                                         HasSubMenu = _frmMenuDetails.CkbSubMenu?.CheckedState == CheckState.Checked,
                                         CheckStyle = _frmMenuDetails.RbChkStyle.SelectedItem == 0
                                                          ? MenuItemCheckStyle.NoCheck
                                                          : _frmMenuDetails.RbChkStyle.SelectedItem == 1
                                                              ? MenuItemCheckStyle.Checked
                                                              : MenuItemCheckStyle.Radio,
                                         ShortcutKey = _frmMenuDetails.TextShortcutKey.Text
                                     };
                                     UpdateMenuItem (_currentEditMenuBarItem, menuItem, _lstMenus.SelectedItem);
                                 }
                             };

            _btnAdd.Accept += (s, e) =>
                              {
                                  if (MenuBar == null)
                                  {
                                      MessageBox.ErrorQuery ("Menu Bar Error", "Must add a MenuBar first!", "Ok");
                                      _btnAddMenuBar.SetFocus ();

                                      return;
                                  }

                                  var frameDetails = new DynamicMenuBarDetails (null, _currentMenuBarItem != null);
                                  DynamicMenuItem item = frameDetails.EnterMenuItem ();

                                  if (item == null)
                                  {
                                      return;
                                  }

                                  if (_currentMenuBarItem is not MenuBarItem)
                                  {
                                      var parent = _currentMenuBarItem.Parent as MenuBarItem;
                                      int idx = parent.GetChildrenIndex (_currentMenuBarItem);

                                      _currentMenuBarItem = new MenuBarItem (
                                                                             _currentMenuBarItem.Title,
                                                                             new MenuItem [] { },
                                                                             _currentMenuBarItem.Parent
                                                                            );
                                      _currentMenuBarItem.CheckType = item.CheckStyle;
                                      parent.Children [idx] = _currentMenuBarItem;
                                  }
                                  else
                                  {
                                      MenuItem newMenu = CreateNewMenu (item, _currentMenuBarItem);
                                      var menuBarItem = _currentMenuBarItem as MenuBarItem;
                                      menuBarItem.AddMenuBarItem (newMenu);


                                      DataContext.Menus.Add (new () { Title = newMenu.Title, MenuItem = newMenu });
                                      _lstMenus.MoveDown ();
                                  }
                              };

            _btnRemove.Accept += (s, e) =>
                                 {
                                     MenuItem menuItem = (DataContext.Menus.Count > 0
                                                              ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem
                                                              : null)
                                                         ?? _currentEditMenuBarItem;

                                     if (menuItem != null)
                                     {
                                         menuItem.RemoveMenuItem ();

                                         if (_lstMenus.SelectedItem > -1)
                                         {
                                             DataContext.Menus?.RemoveAt (_lstMenus.SelectedItem);
                                         }

                                         if (_lstMenus.Source.Count > 0 && _lstMenus.SelectedItem > _lstMenus.Source.Count - 1)
                                         {
                                             _lstMenus.SelectedItem = _lstMenus.Source.Count - 1;
                                         }

                                         _lstMenus.SetNeedsDisplay ();
                                         SetFrameDetails ();
                                     }
                                 };

            _lstMenus.OpenSelectedItem += (s, e) =>
                                          {
                                              _currentMenuBarItem = DataContext.Menus [e.Item].MenuItem;

                                              if (!(_currentMenuBarItem is MenuBarItem))
                                              {
                                                  MessageBox.ErrorQuery ("Menu Open Error", "Must allows sub menus first!", "Ok");

                                                  return;
                                              }

                                              DataContext.Parent = _currentMenuBarItem.Title;
                                              DataContext.Menus = new ();
                                              SetListViewSource (_currentMenuBarItem, true);
                                              MenuItem menuBarItem = DataContext.Menus.Count > 0 ? DataContext.Menus [0].MenuItem : null;
                                              SetFrameDetails (menuBarItem);
                                          };

            _lstMenus.Enter += (s, e) =>
                               {
                                   MenuItem menuBarItem = _lstMenus.SelectedItem > -1 && DataContext.Menus.Count > 0
                                                              ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem
                                                              : null;
                                   SetFrameDetails (menuBarItem);
                               };

            _btnNext.Accept += (s, e) =>
                               {
                                   if (_menuBar != null && _currentSelectedMenuBar + 1 < _menuBar.Menus.Length)
                                   {
                                       _currentSelectedMenuBar++;
                                   }

                                   SelectCurrentMenuBarItem ();
                               };

            _btnPrevious.Accept += (s, e) =>
                                   {
                                       if (_currentSelectedMenuBar - 1 > -1)
                                       {
                                           _currentSelectedMenuBar--;
                                       }

                                       SelectCurrentMenuBarItem ();
                                   };

            _lblMenuBar.Enter += (s, e) =>
                                 {
                                     if (_menuBar?.Menus != null)
                                     {
                                         _currentMenuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
                                         SetFrameDetails (_menuBar.Menus [_currentSelectedMenuBar]);
                                     }
                                 };

            _btnAddMenuBar.Accept += (s, e) =>
                                     {
                                         var frameDetails = new DynamicMenuBarDetails (null);
                                         DynamicMenuItem item = frameDetails.EnterMenuItem ();

                                         if (item == null)
                                         {
                                             return;
                                         }

                                         if (MenuBar == null)
                                         {
                                             _menuBar = new ();
                                             Add (_menuBar);
                                         }

                                         var newMenu = CreateNewMenu (item) as MenuBarItem;
                                         newMenu.AddMenuBarItem ();

                                         _currentMenuBarItem = newMenu;
                                         _currentMenuBarItem.CheckType = item.CheckStyle;

                                         if (Key.TryParse (item.ShortcutKey, out Key key))
                                         {
                                             _currentMenuBarItem.ShortcutKey = key;
                                         }

                                         _currentSelectedMenuBar = _menuBar.Menus.Length - 1;
                                         _menuBar.Menus [_currentSelectedMenuBar] = newMenu;
                                         _lblMenuBar.Text = newMenu.Title;
                                         SetListViewSource (_currentMenuBarItem, true);
                                         SetFrameDetails (_menuBar.Menus [_currentSelectedMenuBar]);
                                         _menuBar.SetNeedsDisplay ();
                                     };

            _btnRemoveMenuBar.Accept += (s, e) =>
                                        {
                                            if (_menuBar == null || _menuBar.Menus.Length == 0)
                                            {
                                                return;
                                            }

                                            if (_menuBar != null && _menuBar.Menus.Length > 0)
                                            {
                                                _currentMenuBarItem.RemoveMenuItem ();



                                                if (_currentSelectedMenuBar - 1 >= 0 && _menuBar.Menus.Length > 0)
                                                {
                                                    _currentSelectedMenuBar--;
                                                }

                                                _currentMenuBarItem = _menuBar.Menus?.Length > 0
                                                                          ? _menuBar.Menus [_currentSelectedMenuBar]
                                                                          : null;
                                            }

                                            if (MenuBar != null && _currentMenuBarItem == null && _menuBar.Menus.Length == 0)
                                            {
                                                Remove (_menuBar);
                                                _menuBar.Dispose ();
                                                _menuBar = null;
                                                DataContext.Menus = new ();
                                                _currentMenuBarItem = null;
                                                _currentSelectedMenuBar = -1;
                                                _lblMenuBar.Text = string.Empty;
                                            }
                                            else
                                            {
                                                _lblMenuBar.Text = _menuBar.Menus [_currentSelectedMenuBar].Title;
                                            }

                                            SetListViewSource (_currentMenuBarItem, true);
                                            SetFrameDetails ();
                                        };

            SetFrameDetails ();

            var ustringConverter = new UStringValueConverter ();
            ListWrapperConverter<DynamicMenuItemList> listWrapperConverter = new ListWrapperConverter<DynamicMenuItemList> ();

            var lblMenuBar = new Binding (this, "MenuBar", _lblMenuBar, "Text", ustringConverter);
            var lblParent = new Binding (this, "Parent", _lblParent, "Text", ustringConverter);
            var lstMenus = new Binding (this, "Menus", _lstMenus, "Source", listWrapperConverter);

            void SetFrameDetails (MenuItem menuBarItem = null)
            {
                MenuItem menuItem;

                if (menuBarItem == null)
                {
                    menuItem = _lstMenus.SelectedItem > -1 && DataContext.Menus.Count > 0
                                   ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem
                                   : null;
                }
                else
                {
                    menuItem = menuBarItem;
                }

                _currentEditMenuBarItem = menuItem;
                _frmMenuDetails.EditMenuBarItem (menuItem);
                bool f = _btnOk.Enabled == _frmMenuDetails.Enabled;

                if (!f)
                {
                    _btnOk.Enabled = _frmMenuDetails.Enabled;
                    _btnCancel.Enabled = _frmMenuDetails.Enabled;
                }
            }

            void SelectCurrentMenuBarItem ()
            {
                MenuBarItem menuBarItem = null;

                if (_menuBar?.Menus != null)
                {
                    menuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
                    _lblMenuBar.Text = menuBarItem.Title;
                }

                SetFrameDetails (menuBarItem);
                _currentMenuBarItem = menuBarItem;
                DataContext.Menus = new ();
                SetListViewSource (_currentMenuBarItem, true);
                _lblParent.Text = string.Empty;
            }

            void SetListViewSource (MenuItem _currentMenuBarItem, bool fill = false)
            {
                DataContext.Menus = [];
                var menuBarItem = _currentMenuBarItem as MenuBarItem;

                if (menuBarItem != null && menuBarItem?.Children == null)
                {
                    return;
                }

                if (!fill)
                {
                    return;
                }

                if (menuBarItem != null)
                {
                    foreach (MenuItem child in menuBarItem?.Children)
                    {
                        var m = new DynamicMenuItemList { Title = child.Title, MenuItem = child };
                        DataContext.Menus.Add (m);
                    }
                }
            }

            MenuItem CreateNewMenu (DynamicMenuItem item, MenuItem parent = null)
            {
                MenuItem newMenu;

                if (item.HasSubMenu)
                {
                    newMenu = new MenuBarItem (item.Title, new MenuItem [] { }, parent);
                }
                else if (parent != null)
                {
                    newMenu = new (item.Title, item.Help, null, null, parent);
                    newMenu.CheckType = item.CheckStyle;
                    newMenu.Action = _frmMenuDetails.CreateAction (newMenu, item);
                    newMenu.Shortcut = ShortcutHelper.GetShortcutFromTag (item.Shortcut);

                    if (Key.TryParse (item.ShortcutKey, out Key key))
                    {
                        newMenu.ShortcutKey = key;
                    }
                    newMenu.AllowNullChecked = item.AllowNullChecked;
                }
                else if (item.IsTopLevel)
                {
                    newMenu = new MenuBarItem (item.Title, item.Help, null);
                    newMenu.Action = _frmMenuDetails.CreateAction (newMenu, item);

                    if (Key.TryParse (item.ShortcutKey, out Key key))
                    {
                        newMenu.ShortcutKey = key;
                    }
                }
                else
                {
                    newMenu = new MenuBarItem (item.Title, item.Help, null);

                    ((MenuBarItem)newMenu).Children [0].Action =
                        _frmMenuDetails.CreateAction (newMenu, item);

                    if (Key.TryParse (item.ShortcutKey, out Key key))
                    {
                        ((MenuBarItem)newMenu).Children [0].ShortcutKey = key;
                    }
                }

                return newMenu;
            }

            void UpdateMenuItem (MenuItem _currentEditMenuBarItem, DynamicMenuItem menuItem, int index)
            {
                _currentEditMenuBarItem.Title = menuItem.Title;
                _currentEditMenuBarItem.Help = menuItem.Help;
                _currentEditMenuBarItem.CheckType = menuItem.CheckStyle;
                var parent = _currentEditMenuBarItem.Parent as MenuBarItem;

                if (parent != null && parent.Children.Length == 1 && _currentEditMenuBarItem.CheckType == MenuItemCheckStyle.Radio)
                {
                    _currentEditMenuBarItem.Checked = true;
                }

                if (menuItem.IsTopLevel && _currentEditMenuBarItem is MenuBarItem)
                {
                    ((MenuBarItem)_currentEditMenuBarItem).Children = null;

                    _currentEditMenuBarItem.Action =
                        _frmMenuDetails.CreateAction (_currentEditMenuBarItem, menuItem);

                    if (Key.TryParse (menuItem.ShortcutKey, out Key key))
                    {
                        _currentEditMenuBarItem.ShortcutKey = key;
                    }

                    SetListViewSource (_currentEditMenuBarItem, true);
                }
                else if (menuItem.HasSubMenu)
                {
                    _currentEditMenuBarItem.Action = null;

                    if (_currentEditMenuBarItem is MenuBarItem && ((MenuBarItem)_currentEditMenuBarItem).Children == null)
                    {
                        ((MenuBarItem)_currentEditMenuBarItem).Children = new MenuItem [] { };
                    }
                    else if (_currentEditMenuBarItem.Parent != null)
                    {
                        _frmMenuDetails.UpdateParent (ref _currentEditMenuBarItem);
                    }
                    else
                    {
                        _currentEditMenuBarItem =
                            new MenuBarItem (
                                             _currentEditMenuBarItem.Title,
                                             new MenuItem [] { },
                                             _currentEditMenuBarItem.Parent
                                            );
                    }

                    SetListViewSource (_currentEditMenuBarItem, true);
                }
                else if (_currentEditMenuBarItem is MenuBarItem && _currentEditMenuBarItem.Parent != null)
                {
                    _frmMenuDetails.UpdateParent (ref _currentEditMenuBarItem);

                    _currentEditMenuBarItem = new (
                                                   menuItem.Title,
                                                   menuItem.Help,
                                                   _frmMenuDetails.CreateAction (_currentEditMenuBarItem, menuItem),
                                                   null,
                                                   _currentEditMenuBarItem.Parent
                                                  );
                }
                else
                {
                    if (_currentEditMenuBarItem is MenuBarItem)
                    {
                        ((MenuBarItem)_currentEditMenuBarItem).Children = null;
                        DataContext.Menus = new ();
                    }

                    _currentEditMenuBarItem.Action =
                        _frmMenuDetails.CreateAction (_currentEditMenuBarItem, menuItem);

                    if (Key.TryParse (menuItem.ShortcutKey, out Key key))
                    {
                        _currentEditMenuBarItem.ShortcutKey = key;
                    }
                }

                if (_currentEditMenuBarItem.Parent == null)
                {
                    DataContext.MenuBar = _currentEditMenuBarItem.Title;
                }
                else
                {
                    if (DataContext.Menus.Count == 0)
                    {
                        DataContext.Menus.Add (
                                               new ()
                                               {
                                                   Title = _currentEditMenuBarItem.Title, MenuItem = _currentEditMenuBarItem
                                               }
                                              );
                    }

                    DataContext.Menus [index] =
                        new ()
                        {
                            Title = _currentEditMenuBarItem.Title, MenuItem = _currentEditMenuBarItem
                        };
                }

                _currentEditMenuBarItem.CheckType = menuItem.CheckStyle;
                SetFrameDetails (_currentEditMenuBarItem);
            }

            //_frmMenuDetails.Initialized += (s, e) => _frmMenuDetails.Enabled = false;
        }

        public DynamicMenuItemModel DataContext { get; set; }
    }

    public class DynamicMenuItem
    {
        public string Action { get; set; } = string.Empty;
        public bool AllowNullChecked { get; set; }
        public MenuItemCheckStyle CheckStyle { get; set; }
        public bool HasSubMenu { get; set; }
        public string Help { get; set; } = string.Empty;
        public bool IsTopLevel { get; set; }
        public string HotKey { get; set; }
        public string ShortcutKey { get; set; }
        public string Title { get; set; } = "_New";
    }

    public class DynamicMenuItemList
    {
        public MenuItem MenuItem { get; set; }
        public string Title { get; set; }
        public override string ToString () { return $"{Title}, {(Key)MenuItem.ShortcutKey}"; }
    }

    public class DynamicMenuItemModel : INotifyPropertyChanged
    {
        private string _menuBar;
        private ObservableCollection<DynamicMenuItemList> _menus;
        private string _parent;
        public DynamicMenuItemModel () { Menus = []; }

        public string MenuBar
        {
            get => _menuBar;
            set
            {
                if (value == _menuBar)
                {
                    return;
                }

                _menuBar = value;

                PropertyChanged?.Invoke (
                                         this,
                                         new (GetPropertyName ())
                                        );
            }
        }

        public ObservableCollection<DynamicMenuItemList> Menus
        {
            get => _menus;
            set
            {
                if (value == _menus)
                {
                    return;
                }

                _menus = value;

                PropertyChanged?.Invoke (
                                         this,
                                         new (GetPropertyName ())
                                        );
            }
        }

        public string Parent
        {
            get => _parent;
            set
            {
                if (value == _parent)
                {
                    return;
                }

                _parent = value;

                PropertyChanged?.Invoke (
                                         this,
                                         new (GetPropertyName ())
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
