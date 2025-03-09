using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dynamic MenuBar", "Demonstrates how to change a MenuBar dynamically.")]
[ScenarioCategory ("Arrangement")]
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
            var lblTitle = new Label { Y = 1, Text = "Title:" };
            Add (lblTitle);

            TextTitle = new () { X = Pos.Right (lblTitle) + 2, Y = Pos.Top (lblTitle), Width = Dim.Fill () };
            Add (TextTitle);

            var lblHelp = new Label { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblTitle) + 1, Text = "Help:" };
            Add (lblHelp);

            TextHelp = new () { X = Pos.Left (TextTitle), Y = Pos.Top (lblHelp), Width = Dim.Fill () };
            Add (TextHelp);

            var lblAction = new Label { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblHelp) + 1, Text = "Action:" };
            Add (lblAction);

            TextAction = new ()
            {
                X = Pos.Left (TextTitle), Y = Pos.Top (lblAction), Width = Dim.Fill (), Height = 5
            };
            Add (TextAction);

            var lblHotKey = new Label { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblAction) + 5, Text = "HotKey:" };
            Add (lblHotKey);

            TextHotKey = new ()
            {
                X = Pos.Left (TextTitle), Y = Pos.Bottom (lblAction) + 5, Width = 2, ReadOnly = true
            };

            TextHotKey.TextChanging += (s, e) =>
                                       {
                                           if (!string.IsNullOrEmpty (e.NewValue) && char.IsLower (e.NewValue [0]))
                                           {
                                               e.NewValue = e.NewValue.ToUpper ();
                                           }
                                       };
            TextHotKey.TextChanged += (s, _) => TextHotKey.SelectAll ();
            TextHotKey.SelectAll ();
            Add (TextHotKey);

            CkbIsTopLevel = new ()
            {
                X = Pos.Left (lblTitle), Y = Pos.Bottom (lblHotKey) + 1, Text = "IsTopLevel"
            };
            Add (CkbIsTopLevel);

            CkbSubMenu = new ()
            {
                X = Pos.Left (lblTitle),
                Y = Pos.Bottom (CkbIsTopLevel),
                CheckedState = (_menuItem == null ? !_hasParent : HasSubMenus (_menuItem)) ? CheckState.Checked : CheckState.UnChecked,
                Text = "Has sub-menus"
            };
            Add (CkbSubMenu);

            CkbNullCheck = new ()
            {
                X = Pos.Left (lblTitle), Y = Pos.Bottom (CkbSubMenu), Text = "Allow null checked"
            };
            Add (CkbNullCheck);

            var rChkLabels = new [] { "NoCheck", "Checked", "Radio" };

            RbChkStyle = new ()
            {
                X = Pos.Left (lblTitle), Y = Pos.Bottom (CkbSubMenu) + 1, RadioLabels = rChkLabels
            };
            Add (RbChkStyle);

            var lblShortcut = new Label
            {
                X = Pos.Right (CkbSubMenu) + 10, Y = Pos.Top (CkbSubMenu), Text = "Shortcut:"
            };
            Add (lblShortcut);

            TextShortcutKey = new ()
            {
                X = Pos.X (lblShortcut), Y = Pos.Bottom (lblShortcut), Width = Dim.Fill (), ReadOnly = true
            };

            TextShortcutKey.KeyDown += (s, e) =>
                                    {
                                        TextShortcutKey.Text = e.ToString ();

                                    };

            Add (TextShortcutKey);

            var btnShortcut = new Button
            {
                X = Pos.X (lblShortcut), Y = Pos.Bottom (TextShortcutKey) + 1, Text = "Clear Shortcut"
            };
            btnShortcut.Accepting += (s, e) => { TextShortcutKey.Text = ""; };
            Add (btnShortcut);

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
                                             CkbSubMenu.SetNeedsDraw ();
                                             TextHelp.Enabled = true;
                                             TextAction.Enabled = true;
                                             TextShortcutKey.Enabled = true;
                                         }
                                         else
                                         {
                                             if ((_menuItem == null && !_hasParent) || _menuItem.Parent == null)
                                             {
                                                 CkbSubMenu.CheckedState = CheckState.Checked;
                                                 CkbSubMenu.SetNeedsDraw ();
                                                 TextShortcutKey.Enabled = false;
                                             }

                                             TextHelp.Text = "";
                                             TextHelp.Enabled = false;
                                             TextAction.Text = "";

                                             TextShortcutKey.Enabled =
                                                 e.NewValue == CheckState.Checked && CkbSubMenu.CheckedState == CheckState.UnChecked;
                                         }
                                     };

            CkbSubMenu.CheckedStateChanged += (s, e) =>
                                  {
                                      if (e.CurrentValue == CheckState.Checked)
                                      {
                                          CkbIsTopLevel.CheckedState = CheckState.UnChecked;
                                          CkbIsTopLevel.SetNeedsDraw ();
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
                                              CkbIsTopLevel.SetNeedsDraw ();
                                              TextShortcutKey.Enabled = true;
                                          }

                                          TextHelp.Enabled = true;
                                          TextAction.Enabled = true;

                                          if (_hasParent)
                                          {
                                              TextShortcutKey.Enabled = CkbIsTopLevel.CheckedState == CheckState.UnChecked
                                                                     && e.CurrentValue == CheckState.UnChecked;
                                          }
                                      }
                                  };

            CkbNullCheck.CheckedStateChanged += (s, e) =>
                                    {
                                        if (_menuItem != null)
                                        {
                                            _menuItem.AllowNullChecked = e.CurrentValue == CheckState.Checked;
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

            TextAction.Text = menuItem.Action != null
                                  ? GetTargetAction (menuItem.Action)
                                  : string.Empty;
            TextHotKey.Text = menuItem?.HotKey != Key.Empty ? menuItem.HotKey.ToString () : "";
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
                TextHotKey.Text = m.HotKey ?? string.Empty;
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

            var dialog = new Dialog
                { Title = "Enter the menu details.", Buttons = [btnOk, btnCancel], Height = Dim.Auto (DimAutoStyle.Content, 22, Application.Screen.Height) };

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
                    HotKey = TextHotKey.Text,
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
            TextHotKey.Text = "";
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

            var frmDelimiter = new FrameView
            {
                X = Pos.Center (),
                Y = 3,
                Width = 25,
                Height = 4,
                Title = "Shortcut Delimiter:"
            };

            var txtDelimiter = new TextField
            {
                X = Pos.Center (), Width = 2, Text = Key.Separator.ToString ()
            };


            var frmMenu = new FrameView { Y = 7, Width = Dim.Percent (50), Height = Dim.Fill (), Title = "Menus:" };

            var btnAddMenuBar = new Button { Y = 1, Text = "Add a MenuBar" };
            frmMenu.Add (btnAddMenuBar);

            var btnMenuBarUp = new Button { X = Pos.Center (), Text = Glyphs.UpArrow.ToString () };
            frmMenu.Add (btnMenuBarUp);

            var btnMenuBarDown = new Button { X = Pos.Center (), Y = Pos.Bottom (btnMenuBarUp), Text = Glyphs.DownArrow.ToString () };
            frmMenu.Add (btnMenuBarDown);

            var btnRemoveMenuBar = new Button { Y = 1, Text = "Remove a MenuBar" };

            btnRemoveMenuBar.X = Pos.AnchorEnd (0) - (Pos.Right (btnRemoveMenuBar) - Pos.Left (btnRemoveMenuBar));
            frmMenu.Add (btnRemoveMenuBar);

            var btnPrevious = new Button
            {
                X = Pos.Left (btnAddMenuBar), Y = Pos.Top (btnAddMenuBar) + 2, Text = Glyphs.LeftArrow.ToString ()
            };
            frmMenu.Add (btnPrevious);

            var btnAdd = new Button { Y = Pos.Top (btnPrevious) + 2, Text = " Add  " };
            btnAdd.X = Pos.AnchorEnd ();
            frmMenu.Add (btnAdd);

            var btnNext = new Button { X = Pos.X (btnAdd), Y = Pos.Top (btnPrevious), Text = Glyphs.RightArrow.ToString () };
            frmMenu.Add (btnNext);

            var lblMenuBar = new Label
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                TextAlignment = Alignment.Center,
                X = Pos.Right (btnPrevious) + 1,
                Y = Pos.Top (btnPrevious),

                Width = Dim.Fill () - Dim.Func (() => btnAdd.Frame.Width + 1),
                Height = 1
            };

            lblMenuBar.TextChanged += (s, e) =>
                                       {
                                           if (lblMenuBar.Text.Contains ("_"))
                                           {
                                               lblMenuBar.Text = lblMenuBar.Text.Replace ("_", "");
                                           }
                                       };
            frmMenu.Add (lblMenuBar);
            lblMenuBar.WantMousePositionReports = true;
            lblMenuBar.CanFocus = true;

            var lblParent = new Label
            {
                TextAlignment = Alignment.Center,
                X = Pos.Right (btnPrevious) + 1,
                Y = Pos.Top (btnPrevious) + 1,

                Width = Dim.Fill () - Dim.Width (btnAdd) - 1
            };
            frmMenu.Add (lblParent);

            var btnPreviowsParent = new Button
            {
                X = Pos.Left (btnAddMenuBar), Y = Pos.Top (btnPrevious) + 1, Text = ".."
            };
            frmMenu.Add (btnPreviowsParent);

            _lstMenus = new ()
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                X = Pos.Right (btnPrevious) + 1,
                Y = Pos.Top (btnPrevious) + 2,
                Width = lblMenuBar.Width,
                Height = Dim.Fill (),
                Source = new ListWrapper<DynamicMenuItemList> ([])
            };
            frmMenu.Add (_lstMenus);

            //lblMenuBar.TabIndex = btnPrevious.TabIndex + 1;
            //_lstMenus.TabIndex = lblMenuBar.TabIndex + 1;
            //btnNext.TabIndex = _lstMenus.TabIndex + 1;
            //btnAdd.TabIndex = btnNext.TabIndex + 1;

            var btnRemove = new Button { X = Pos.Left (btnAdd), Y = Pos.Top (btnAdd) + 1, Text = "Remove" };
            frmMenu.Add (btnRemove);

            var btnUp = new Button { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (btnRemove) + 2, Text = Glyphs.UpArrow.ToString () };
            frmMenu.Add (btnUp);

            var btnDown = new Button { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (btnUp) + 1, Text = Glyphs.DownArrow.ToString () };
            frmMenu.Add (btnDown);

            Add (frmMenu);

            var frmMenuDetails = new DynamicMenuBarDetails
            {
                X = Pos.Right (frmMenu),
                Y = Pos.Top (frmMenu),
                Width = Dim.Fill (),
                Height = Dim.Fill (2),
                Title = "Menu Details:"
            };
            Add (frmMenuDetails);

            btnMenuBarUp.Accepting += (s, e) =>
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
                                                _menuBar.SetNeedsDraw ();
                                            }
                                        }
                                    };

            btnMenuBarDown.Accepting += (s, e) =>
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
                                                  _menuBar.SetNeedsDraw ();
                                              }
                                          }
                                      };

            btnUp.Accepting += (s, e) =>
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

            btnDown.Accepting += (s, e) =>
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

            btnPreviowsParent.Accepting += (s, e) =>
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

            var btnOk = new Button { X = Pos.Right (frmMenu) + 20, Y = Pos.Bottom (frmMenuDetails), Text = "Ok" };
            Add (btnOk);

            var btnCancel = new Button { X = Pos.Right (btnOk) + 3, Y = Pos.Top (btnOk), Text = "Cancel" };
            btnCancel.Accepting += (s, e) => { SetFrameDetails (_currentEditMenuBarItem); };
            Add (btnCancel);

            txtDelimiter.TextChanging += (s, e) =>
                                          {
                                              if (!string.IsNullOrEmpty (e.NewValue))
                                              {
                                                  Key.Separator = e.NewValue.ToRunes () [0];
                                              }
                                              else
                                              {
                                                  e.Cancel = true;
                                                  txtDelimiter.SelectAll ();
                                              }
                                          };
            txtDelimiter.TextChanged += (s, _) =>
                                         {
                                             txtDelimiter.SelectAll ();
                                             SetFrameDetails ();
                                         };
            frmDelimiter.Add (txtDelimiter);
            txtDelimiter.SelectAll ();
            Add (frmDelimiter);

            _lstMenus.SelectedItemChanged += (s, e) => { SetFrameDetails (); };

            btnOk.Accepting += (s, e) =>
                             {
                                 if (string.IsNullOrEmpty (frmMenuDetails.TextTitle.Text) && _currentEditMenuBarItem != null)
                                 {
                                     MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
                                 }
                                 else if (_currentEditMenuBarItem != null)
                                 {
                                     var menuItem = new DynamicMenuItem
                                     {
                                         Title = frmMenuDetails.TextTitle.Text,
                                         Help = frmMenuDetails.TextHelp.Text,
                                         Action = frmMenuDetails.TextAction.Text,
                                         HotKey = frmMenuDetails.TextHotKey.Text,
                                         IsTopLevel = frmMenuDetails.CkbIsTopLevel?.CheckedState == CheckState.Checked,
                                         HasSubMenu = frmMenuDetails.CkbSubMenu?.CheckedState == CheckState.Checked,
                                         CheckStyle = frmMenuDetails.RbChkStyle.SelectedItem == 0
                                                          ? MenuItemCheckStyle.NoCheck
                                                          : frmMenuDetails.RbChkStyle.SelectedItem == 1
                                                              ? MenuItemCheckStyle.Checked
                                                              : MenuItemCheckStyle.Radio,
                                         ShortcutKey = frmMenuDetails.TextShortcutKey.Text
                                     };
                                     UpdateMenuItem (_currentEditMenuBarItem, menuItem, _lstMenus.SelectedItem);
                                 }
                             };

            btnAdd.Accepting += (s, e) =>
                              {
                                  if (MenuBar == null)
                                  {
                                      MessageBox.ErrorQuery ("Menu Bar Error", "Must add a MenuBar first!", "Ok");
                                      btnAddMenuBar.SetFocus ();

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
                                      menuBarItem.AddMenuBarItem (MenuBar, newMenu);


                                      DataContext.Menus.Add (new () { Title = newMenu.Title, MenuItem = newMenu });
                                      _lstMenus.MoveDown ();
                                  }
                              };

            btnRemove.Accepting += (s, e) =>
                                {
                                    MenuItem menuItem = (DataContext.Menus.Count > 0 && _lstMenus.SelectedItem > -1
                                                             ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem
                                                             : _currentEditMenuBarItem);

                                    if (menuItem != null)
                                    {
                                        menuItem.RemoveMenuItem ();

                                        if (_currentEditMenuBarItem == menuItem)
                                        {
                                            _currentEditMenuBarItem = null;

                                            if (menuItem.Parent is null)
                                            {
                                                _currentSelectedMenuBar = Math.Max (Math.Min (_currentSelectedMenuBar, _menuBar.Menus.Length - 1), 0);
                                            }

                                            SelectCurrentMenuBarItem ();
                                        }

                                        if (_lstMenus.SelectedItem > -1)
                                        {
                                            DataContext.Menus?.RemoveAt (_lstMenus.SelectedItem);
                                        }

                                        if (_lstMenus.Source.Count > 0 && _lstMenus.SelectedItem > _lstMenus.Source.Count - 1)
                                        {
                                            _lstMenus.SelectedItem = _lstMenus.Source.Count - 1;
                                        }

                                        if (_menuBar.Menus.Length == 0)
                                        {
                                            RemoveMenuBar ();
                                        }

                                        _lstMenus.SetNeedsDraw ();
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

            _lstMenus.HasFocusChanging += (s, e) =>
                               {
                                   MenuItem menuBarItem = _lstMenus.SelectedItem > -1 && DataContext.Menus.Count > 0
                                                              ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem
                                                              : null;
                                   SetFrameDetails (menuBarItem);
                               };

            btnNext.Accepting += (s, e) =>
                               {
                                   if (_menuBar != null && _currentSelectedMenuBar + 1 < _menuBar.Menus.Length)
                                   {
                                       _currentSelectedMenuBar++;
                                   }

                                   SelectCurrentMenuBarItem ();
                               };

            btnPrevious.Accepting += (s, e) =>
                                   {
                                       if (_currentSelectedMenuBar - 1 > -1)
                                       {
                                           _currentSelectedMenuBar--;
                                       }

                                       SelectCurrentMenuBarItem ();
                                   };

            lblMenuBar.HasFocusChanging += (s, e) =>
                                 {
                                     if (_menuBar?.Menus != null)
                                     {
                                         _currentMenuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
                                         SetFrameDetails (_menuBar.Menus [_currentSelectedMenuBar]);
                                     }
                                 };

            btnAddMenuBar.Accepting += (s, e) =>
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
                                         newMenu.AddMenuBarItem (MenuBar);

                                         _currentMenuBarItem = newMenu;
                                         _currentMenuBarItem.CheckType = item.CheckStyle;

                                         if (Key.TryParse (item.ShortcutKey, out Key key))
                                         {
                                             _currentMenuBarItem.ShortcutKey = key;
                                         }

                                         _currentSelectedMenuBar = _menuBar.Menus.Length - 1;
                                         _menuBar.Menus [_currentSelectedMenuBar] = newMenu;
                                         lblMenuBar.Text = newMenu.Title;
                                         SetListViewSource (_currentMenuBarItem, true);
                                         SetFrameDetails (_menuBar.Menus [_currentSelectedMenuBar]);
                                         _menuBar.SetNeedsDraw ();
                                     };

            btnRemoveMenuBar.Accepting += (s, e) =>
                                        {
                                            if (_menuBar == null)
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

                                            RemoveMenuBar ();

                                            SetListViewSource (_currentMenuBarItem, true);
                                            SetFrameDetails ();
                                        };

            void RemoveMenuBar ()
            {
                if (MenuBar != null && _currentMenuBarItem == null && _menuBar.Menus.Length == 0)
                {
                    Remove (_menuBar);
                    _menuBar.Dispose ();
                    _menuBar = null;
                    DataContext.Menus = new ();
                    _currentMenuBarItem = null;
                    _currentSelectedMenuBar = -1;
                    lblMenuBar.Text = string.Empty;
                }
                else
                {
                    lblMenuBar.Text = _menuBar.Menus [_currentSelectedMenuBar].Title;
                }
            }

            SetFrameDetails ();

            var ustringConverter = new UStringValueConverter ();
            ListWrapperConverter<DynamicMenuItemList> listWrapperConverter = new ListWrapperConverter<DynamicMenuItemList> ();

            var bdgMenuBar = new Binding (this, "MenuBar", lblMenuBar, "Text", ustringConverter);
            var bdgParent = new Binding (this, "Parent", lblParent, "Text", ustringConverter);
            var bdgMenus = new Binding (this, "Menus", _lstMenus, "Source", listWrapperConverter);

            void SetFrameDetails (MenuItem menuBarItem = null)
            {
                MenuItem menuItem;

                if (menuBarItem == null)
                {
                    menuItem = _lstMenus.SelectedItem > -1 && DataContext.Menus.Count > 0
                                   ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem
                                   : _currentEditMenuBarItem;
                }
                else
                {
                    menuItem = menuBarItem;
                }

                _currentEditMenuBarItem = menuItem;
                frmMenuDetails.EditMenuBarItem (menuItem);
                bool f = btnOk.Enabled == frmMenuDetails.Enabled;

                if (!f)
                {
                    btnOk.Enabled = frmMenuDetails.Enabled;
                    btnCancel.Enabled = frmMenuDetails.Enabled;
                }
            }

            void SelectCurrentMenuBarItem ()
            {
                MenuBarItem menuBarItem = null;

                if (_menuBar?.Menus is { Length: > 0 })
                {
                    menuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
                    lblMenuBar.Text = menuBarItem.Title;
                }

                SetFrameDetails (menuBarItem);
                _currentMenuBarItem = menuBarItem;
                DataContext.Menus = new ();
                SetListViewSource (_currentMenuBarItem, true);
                lblParent.Text = string.Empty;

                if (_currentMenuBarItem is null)
                {
                    lblMenuBar.Text = string.Empty;
                }
            }

            void SetListViewSource (MenuItem currentMenuBarItem, bool fill = false)
            {
                DataContext.Menus = [];
                var menuBarItem = currentMenuBarItem as MenuBarItem;

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
                    newMenu.Action = frmMenuDetails.CreateAction (newMenu, item);

                    if (Key.TryParse (item.ShortcutKey, out Key key))
                    {
                        newMenu.ShortcutKey = key;
                    }
                    newMenu.AllowNullChecked = item.AllowNullChecked;
                }
                else if (item.IsTopLevel)
                {
                    newMenu = new MenuBarItem (item.Title, item.Help, null);
                    newMenu.Action = frmMenuDetails.CreateAction (newMenu, item);

                    if (Key.TryParse (item.ShortcutKey, out Key key))
                    {
                        newMenu.ShortcutKey = key;
                    }
                }
                else
                {
                    newMenu = new MenuBarItem (item.Title, item.Help, null);

                    ((MenuBarItem)newMenu).Children [0].Action =
                        frmMenuDetails.CreateAction (newMenu, item);

                    if (Key.TryParse (item.ShortcutKey, out Key key))
                    {
                        ((MenuBarItem)newMenu).Children [0].ShortcutKey = key;
                    }
                }

                return newMenu;
            }

            void UpdateMenuItem (MenuItem currentEditMenuBarItem, DynamicMenuItem menuItem, int index)
            {
                currentEditMenuBarItem.Title = menuItem.Title;
                currentEditMenuBarItem.Help = menuItem.Help;
                currentEditMenuBarItem.CheckType = menuItem.CheckStyle;

                if (currentEditMenuBarItem.Parent is MenuBarItem parent
                    && parent.Children.Length == 1
                    && currentEditMenuBarItem.CheckType == MenuItemCheckStyle.Radio)
                {
                    currentEditMenuBarItem.Checked = true;
                }

                if (menuItem.IsTopLevel && currentEditMenuBarItem is MenuBarItem)
                {
                    ((MenuBarItem)currentEditMenuBarItem).Children = null;

                    currentEditMenuBarItem.Action =
                        frmMenuDetails.CreateAction (currentEditMenuBarItem, menuItem);

                    if (Key.TryParse (menuItem.ShortcutKey, out Key key))
                    {
                        currentEditMenuBarItem.ShortcutKey = key;
                    }

                    SetListViewSource (currentEditMenuBarItem, true);
                }
                else if (menuItem.HasSubMenu)
                {
                    currentEditMenuBarItem.Action = null;

                    if (currentEditMenuBarItem is MenuBarItem && ((MenuBarItem)currentEditMenuBarItem).Children == null)
                    {
                        ((MenuBarItem)currentEditMenuBarItem).Children = new MenuItem [] { };
                    }
                    else if (currentEditMenuBarItem.Parent != null)
                    {
                        frmMenuDetails.UpdateParent (ref currentEditMenuBarItem);
                    }
                    else
                    {
                        currentEditMenuBarItem =
                            new MenuBarItem (
                                             currentEditMenuBarItem.Title,
                                             new MenuItem [] { },
                                             currentEditMenuBarItem.Parent
                                            );
                    }

                    SetListViewSource (currentEditMenuBarItem, true);
                }
                else if (currentEditMenuBarItem is MenuBarItem && currentEditMenuBarItem.Parent != null)
                {
                    frmMenuDetails.UpdateParent (ref currentEditMenuBarItem);

                    currentEditMenuBarItem = new (
                                                   menuItem.Title,
                                                   menuItem.Help,
                                                   frmMenuDetails.CreateAction (currentEditMenuBarItem, menuItem),
                                                   null,
                                                   currentEditMenuBarItem.Parent
                                                  );
                }
                else
                {
                    if (currentEditMenuBarItem is MenuBarItem)
                    {
                        ((MenuBarItem)currentEditMenuBarItem).Children = null;
                        DataContext.Menus = new ();
                    }

                    currentEditMenuBarItem.Action =
                        frmMenuDetails.CreateAction (currentEditMenuBarItem, menuItem);

                    if (Key.TryParse (menuItem.ShortcutKey, out Key key))
                    {
                        currentEditMenuBarItem.ShortcutKey = key;
                    }
                }

                if (currentEditMenuBarItem.Parent == null)
                {
                    DataContext.MenuBar = currentEditMenuBarItem.Title;
                }
                else
                {
                    if (DataContext.Menus.Count == 0)
                    {
                        DataContext.Menus.Add (
                                               new ()
                                               {
                                                   Title = currentEditMenuBarItem.Title, MenuItem = currentEditMenuBarItem
                                               }
                                              );
                    }

                    DataContext.Menus [index] =
                        new ()
                        {
                            Title = currentEditMenuBarItem.Title, MenuItem = currentEditMenuBarItem
                        };
                }

                currentEditMenuBarItem.CheckType = menuItem.CheckStyle;
                SetFrameDetails (currentEditMenuBarItem);
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
        public override string ToString () { return $"{Title}, {MenuItem.HotKey}, {MenuItem.ShortcutKey} "; }
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
