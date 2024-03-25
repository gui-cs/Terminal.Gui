using System;
using System.Collections;
using System.Collections.Generic;
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
    public override void Init ()
    {
        Application.Init ();

        Top = new ();

        Top.Add (
                 new DynamicMenuBarSample { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" }
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

            TextTitle = new TextField { X = Pos.Right (_lblTitle) + 2, Y = Pos.Top (_lblTitle), Width = Dim.Fill () };
            Add (TextTitle);

            var _lblHelp = new Label { X = Pos.Left (_lblTitle), Y = Pos.Bottom (_lblTitle) + 1, Text = "Help:" };
            Add (_lblHelp);

            TextHelp = new TextField { X = Pos.Left (TextTitle), Y = Pos.Top (_lblHelp), Width = Dim.Fill () };
            Add (TextHelp);

            var _lblAction = new Label { X = Pos.Left (_lblTitle), Y = Pos.Bottom (_lblHelp) + 1, Text = "Action:" };
            Add (_lblAction);

            TextAction = new TextView
            {
                X = Pos.Left (TextTitle), Y = Pos.Top (_lblAction), Width = Dim.Fill (), Height = 5
            };
            Add (TextAction);

            CkbIsTopLevel = new CheckBox
            {
                X = Pos.Left (_lblTitle), Y = Pos.Bottom (_lblAction) + 5, Text = "IsTopLevel"
            };
            Add (CkbIsTopLevel);

            CkbSubMenu = new CheckBox
            {
                X = Pos.Left (_lblTitle),
                Y = Pos.Bottom (CkbIsTopLevel),
                Checked = _menuItem == null ? !_hasParent : HasSubMenus (_menuItem),
                Text = "Has sub-menus"
            };
            Add (CkbSubMenu);

            CkbNullCheck = new CheckBox
            {
                X = Pos.Left (_lblTitle), Y = Pos.Bottom (CkbSubMenu), Text = "Allow null checked"
            };
            Add (CkbNullCheck);

            var _rChkLabels = new [] { "NoCheck", "Checked", "Radio" };

            RbChkStyle = new RadioGroup
            {
                X = Pos.Left (_lblTitle), Y = Pos.Bottom (CkbSubMenu) + 1, RadioLabels = _rChkLabels
            };
            Add (RbChkStyle);

            var _lblShortcut = new Label
            {
                X = Pos.Right (CkbSubMenu) + 10, Y = Pos.Top (CkbSubMenu), Text = "Shortcut:"
            };
            Add (_lblShortcut);

            TextShortcut = new TextField
            {
                X = Pos.X (_lblShortcut), Y = Pos.Bottom (_lblShortcut), Width = Dim.Fill (), ReadOnly = true
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
                MenuItem m = _menuItem != null ? _menuItem : new MenuItem ();

                if (pre && !ShortcutHelper.PreShortcutValidation (k))
                {
                    TextShortcut.Text = "";

                    return false;
                }

                if (!pre)
                {
                    if (!ShortcutHelper.PostShortcutValidation (
                                                                ShortcutHelper.GetShortcutFromTag (TextShortcut.Text)
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
                                  MenuBar.ShortcutDelimiter
                                 ); // ShortcutHelper.GetShortcutTag (k);

                return true;
            }

            TextShortcut.KeyUp += (s, e) =>
                                  {
                                      if (CheckShortcut (e.KeyCode, false))
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

            CkbIsTopLevel.Toggled += (s, e) =>
                                     {
                                         if ((_menuItem != null && _menuItem.Parent != null && (bool)CkbIsTopLevel.Checked)
                                             || (_menuItem == null && _hasParent && (bool)CkbIsTopLevel.Checked))
                                         {
                                             MessageBox.ErrorQuery (
                                                                    "Invalid IsTopLevel",
                                                                    "Only menu bar can have top level menu item!",
                                                                    "Ok"
                                                                   );
                                             CkbIsTopLevel.Checked = false;

                                             return;
                                         }

                                         if (CkbIsTopLevel.Checked == true)
                                         {
                                             CkbSubMenu.Checked = false;
                                             CkbSubMenu.SetNeedsDisplay ();
                                             TextHelp.Enabled = true;
                                             TextAction.Enabled = true;

                                             TextShortcut.Enabled =
                                                 CkbIsTopLevel.Checked == false && CkbSubMenu.Checked == false;
                                         }
                                         else
                                         {
                                             if ((_menuItem == null && !_hasParent) || _menuItem.Parent == null)
                                             {
                                                 CkbSubMenu.Checked = true;
                                                 CkbSubMenu.SetNeedsDisplay ();
                                                 TextShortcut.Enabled = false;
                                             }

                                             TextHelp.Text = "";
                                             TextHelp.Enabled = false;
                                             TextAction.Text = "";
                                             TextAction.Enabled = false;
                                         }
                                     };

            CkbSubMenu.Toggled += (s, e) =>
                                  {
                                      if ((bool)CkbSubMenu.Checked)
                                      {
                                          CkbIsTopLevel.Checked = false;
                                          CkbIsTopLevel.SetNeedsDisplay ();
                                          TextHelp.Text = "";
                                          TextHelp.Enabled = false;
                                          TextAction.Text = "";
                                          TextAction.Enabled = false;
                                          TextShortcut.Text = "";
                                          TextShortcut.Enabled = false;
                                      }
                                      else
                                      {
                                          if (!_hasParent)
                                          {
                                              CkbIsTopLevel.Checked = true;
                                              CkbIsTopLevel.SetNeedsDisplay ();
                                              TextShortcut.Enabled = false;
                                          }

                                          TextHelp.Enabled = true;
                                          TextAction.Enabled = true;

                                          TextShortcut.Enabled =
                                              CkbIsTopLevel.Checked == false && CkbSubMenu.Checked == false;
                                      }
                                  };

            CkbNullCheck.Toggled += (s, e) =>
                                    {
                                        if (_menuItem != null)
                                        {
                                            _menuItem.AllowNullChecked = (bool)CkbNullCheck.Checked;
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
        public TextField TextShortcut { get; }
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
            CkbIsTopLevel.Checked = IsTopLevel (menuItem);
            CkbSubMenu.Checked = HasSubMenus (menuItem);
            CkbNullCheck.Checked = menuItem.AllowNullChecked;
            TextHelp.Enabled = (bool)!CkbSubMenu.Checked;
            TextAction.Enabled = (bool)!CkbSubMenu.Checked;
            RbChkStyle.SelectedItem = (int)(menuItem?.CheckType ?? MenuItemCheckStyle.NoCheck);
            TextShortcut.Text = menuItem?.ShortcutTag ?? "";
            TextShortcut.Enabled = CkbIsTopLevel.Checked == false && CkbSubMenu.Checked == false;
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
                CkbIsTopLevel.Checked = false;
                CkbSubMenu.Checked = !_hasParent;
                CkbNullCheck.Checked = false;
                TextHelp.Enabled = _hasParent;
                TextAction.Enabled = _hasParent;
                TextShortcut.Enabled = _hasParent;
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
            var dialog = new Dialog { Title = "Enter the menu details.", Buttons = [btnOk, btnCancel] };

            Width = Dim.Fill ();
            Height = Dim.Fill () - 1;
            dialog.Add (this);
            TextTitle.SetFocus ();
            TextTitle.CursorPosition = TextTitle.Text.Length;
            Application.Run (dialog);
            dialog.Dispose ();

            if (valid)
            {
                return new DynamicMenuItem
                {
                    Title = TextTitle.Text,
                    Help = TextHelp.Text,
                    Action = TextAction.Text,
                    IsTopLevel = CkbIsTopLevel?.Checked ?? false,
                    HasSubMenu = CkbSubMenu?.Checked ?? false,
                    CheckStyle = RbChkStyle.SelectedItem == 0 ? MenuItemCheckStyle.NoCheck :
                                 RbChkStyle.SelectedItem == 1 ? MenuItemCheckStyle.Checked :
                                 MenuItemCheckStyle.Radio,
                    Shortcut = TextShortcut.Text,
                    AllowNullChecked = CkbNullCheck.Checked != null && (bool)CkbNullCheck.Checked
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
                menuItem = new MenuItem (
                                         menuItem.Title,
                                         menuItem.Help,
                                         CreateAction (menuItem, new DynamicMenuItem ()),
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
            CkbIsTopLevel.Checked = false;
            CkbSubMenu.Checked = false;
            RbChkStyle.SelectedItem = (int)MenuItemCheckStyle.NoCheck;
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
            DataContext = new DynamicMenuItemModel ();

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
                X = Pos.Center (), Width = 2, Text = MenuBar.ShortcutDelimiter.ToString ()
            };

            _txtDelimiter.TextChanged += (s, _) =>
                                             MenuBar.ShortcutDelimiter = _txtDelimiter.Text.ToRunes () [0];
            _frmDelimiter.Add (_txtDelimiter);

            Add (_frmDelimiter);

            var _frmMenu = new FrameView { Y = 7, Width = Dim.Percent (50), Height = Dim.Fill (), Title = "Menus:" };

            var _btnAddMenuBar = new Button { Y = 1, Text = "Add a MenuBar" };
            _frmMenu.Add (_btnAddMenuBar);

            var _btnMenuBarUp = new Button { X = Pos.Center (), Text = "^" };
            _frmMenu.Add (_btnMenuBarUp);

            var _btnMenuBarDown = new Button { X = Pos.Center (), Y = Pos.Bottom (_btnMenuBarUp), Text = "v" };
            _frmMenu.Add (_btnMenuBarDown);

            var _btnRemoveMenuBar = new Button { Y = 1, Text = "Remove a MenuBar" };

            _btnRemoveMenuBar.X = Pos.AnchorEnd () - (Pos.Right (_btnRemoveMenuBar) - Pos.Left (_btnRemoveMenuBar));
            _frmMenu.Add (_btnRemoveMenuBar);

            var _btnPrevious = new Button
            {
                X = Pos.Left (_btnAddMenuBar), Y = Pos.Top (_btnAddMenuBar) + 2, Text = "<"
            };
            _frmMenu.Add (_btnPrevious);

            var _btnAdd = new Button { Y = Pos.Top (_btnPrevious) + 2, Text = " Add  " };
            _btnAdd.X = Pos.AnchorEnd () - (Pos.Right (_btnAdd) - Pos.Left (_btnAdd));
            _frmMenu.Add (_btnAdd);

            var _btnNext = new Button { X = Pos.X (_btnAdd), Y = Pos.Top (_btnPrevious), Text = ">" };
            _frmMenu.Add (_btnNext);

            var _lblMenuBar = new Label
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                TextAlignment = TextAlignment.Centered,
                X = Pos.Right (_btnPrevious) + 1,
                Y = Pos.Top (_btnPrevious),
                AutoSize = false,
                Width = Dim.Fill () - Dim.Function (() => _btnAdd.Frame.Width + 1),
                Height = 1
            };
            _frmMenu.Add (_lblMenuBar);
            _lblMenuBar.WantMousePositionReports = true;
            _lblMenuBar.CanFocus = true;

            var _lblParent = new Label
            {
                TextAlignment = TextAlignment.Centered,
                X = Pos.Right (_btnPrevious) + 1,
                Y = Pos.Top (_btnPrevious) + 1,
                AutoSize = false,
                Width = Dim.Fill () - Dim.Width (_btnAdd) - 1
            };
            _frmMenu.Add (_lblParent);

            var _btnPreviowsParent = new Button
            {
                X = Pos.Left (_btnAddMenuBar), Y = Pos.Top (_btnPrevious) + 1, Text = ".."
            };
            _frmMenu.Add (_btnPreviowsParent);

            _lstMenus = new ListView
            {
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                X = Pos.Right (_btnPrevious) + 1,
                Y = Pos.Top (_btnPrevious) + 2,
                Width = _lblMenuBar.Width,
                Height = Dim.Fill (),
                Source = new ListWrapper (new List<DynamicMenuItemList> ())
            };
            _frmMenu.Add (_lstMenus);

            _lblMenuBar.TabIndex = _btnPrevious.TabIndex + 1;
            _lstMenus.TabIndex = _lblMenuBar.TabIndex + 1;
            _btnNext.TabIndex = _lstMenus.TabIndex + 1;
            _btnAdd.TabIndex = _btnNext.TabIndex + 1;

            var _btnRemove = new Button { X = Pos.Left (_btnAdd), Y = Pos.Top (_btnAdd) + 1, Text = "Remove" };
            _frmMenu.Add (_btnRemove);

            var _btnUp = new Button { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (_btnRemove) + 2, Text = "^" };
            _frmMenu.Add (_btnUp);

            var _btnDown = new Button { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (_btnUp) + 1, Text = "v" };
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
                                              new DynamicMenuItemList { Title = menuItem.Title, MenuItem = menuItem };
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
                                                new DynamicMenuItemList { Title = menuItem.Title, MenuItem = menuItem };
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
                                          IsTopLevel = _frmMenuDetails.CkbIsTopLevel?.Checked ?? false,
                                          HasSubMenu = _frmMenuDetails.CkbSubMenu?.Checked ?? false,
                                          CheckStyle = _frmMenuDetails.RbChkStyle.SelectedItem == 0
                                                           ? MenuItemCheckStyle.NoCheck
                                                           : _frmMenuDetails.RbChkStyle.SelectedItem == 1
                                                               ? MenuItemCheckStyle.Checked
                                                               : MenuItemCheckStyle.Radio,
                                          Shortcut = _frmMenuDetails.TextShortcut.Text
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

                                   if (!(_currentMenuBarItem is MenuBarItem))
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

                                       if (menuBarItem == null)
                                       {
                                           menuBarItem = new MenuBarItem (
                                                                          _currentMenuBarItem.Title,
                                                                          new [] { newMenu },
                                                                          _currentMenuBarItem.Parent
                                                                         );
                                       }
                                       else if (menuBarItem.Children == null)
                                       {
                                           menuBarItem.Children = new [] { newMenu };
                                       }
                                       else
                                       {
                                           MenuItem [] childrens = menuBarItem.Children;
                                           Array.Resize (ref childrens, childrens.Length + 1);
                                           childrens [childrens.Length - 1] = newMenu;
                                           menuBarItem.Children = childrens;
                                       }

                                       DataContext.Menus.Add (new DynamicMenuItemList { Title = newMenu.Title, MenuItem = newMenu });
                                       _lstMenus.MoveDown ();
                                   }
                               };

            _btnRemove.Accept += (s, e) =>
                                  {
                                      MenuItem menuItem = DataContext.Menus.Count > 0
                                                              ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem
                                                              : null;

                                      if (menuItem != null)
                                      {
                                          MenuItem [] childrens = ((MenuBarItem)_currentMenuBarItem).Children;
                                          childrens [_lstMenus.SelectedItem] = null;
                                          var i = 0;

                                          foreach (MenuItem c in childrens)
                                          {
                                              if (c != null)
                                              {
                                                  childrens [i] = c;
                                                  i++;
                                              }
                                          }

                                          Array.Resize (ref childrens, childrens.Length - 1);

                                          if (childrens.Length == 0)
                                          {
                                              if (_currentMenuBarItem.Parent == null)
                                              {
                                                  ((MenuBarItem)_currentMenuBarItem).Children = null;

                                                  //_currentMenuBarItem.Action = _frmMenuDetails.CreateAction (_currentEditMenuBarItem, new DynamicMenuItem (_currentMenuBarItem.Title));
                                              }
                                              else
                                              {
                                                  _currentMenuBarItem = new MenuItem (
                                                                                      _currentMenuBarItem.Title,
                                                                                      _currentMenuBarItem.Help,
                                                                                      _frmMenuDetails.CreateAction (
                                                                                           _currentEditMenuBarItem,
                                                                                           new DynamicMenuItem
                                                                                           {
                                                                                               Title = _currentEditMenuBarItem
                                                                                                   .Title
                                                                                           }
                                                                                          ),
                                                                                      null,
                                                                                      _currentMenuBarItem.Parent
                                                                                     );
                                              }
                                          }
                                          else
                                          {
                                              ((MenuBarItem)_currentMenuBarItem).Children = childrens;
                                          }

                                          DataContext.Menus.RemoveAt (_lstMenus.SelectedItem);

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
                                              DataContext.Menus = new List<DynamicMenuItemList> ();
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
                                              _menuBar = new MenuBar ();
                                              Add (_menuBar);
                                          }

                                          var newMenu = CreateNewMenu (item) as MenuBarItem;

                                          MenuBarItem [] menus = _menuBar.Menus;
                                          Array.Resize (ref menus, menus.Length + 1);
                                          menus [^1] = newMenu;
                                          _menuBar.Menus = menus;
                                          _currentMenuBarItem = newMenu;
                                          _currentMenuBarItem.CheckType = item.CheckStyle;
                                          _currentSelectedMenuBar = menus.Length - 1;
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
                                                 _menuBar.Menus [_currentSelectedMenuBar] = null;
                                                 var i = 0;

                                                 foreach (MenuBarItem m in _menuBar.Menus)
                                                 {
                                                     if (m != null)
                                                     {
                                                         _menuBar.Menus [i] = m;
                                                         i++;
                                                     }
                                                 }

                                                 MenuBarItem [] menus = _menuBar.Menus;
                                                 Array.Resize (ref menus, menus.Length - 1);
                                                 _menuBar.Menus = menus;

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
                                                 _menuBar = null;
                                                 DataContext.Menus = new List<DynamicMenuItemList> ();
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
            var listWrapperConverter = new ListWrapperConverter ();

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
                DataContext.Menus = new List<DynamicMenuItemList> ();
                SetListViewSource (_currentMenuBarItem, true);
                _lblParent.Text = string.Empty;
            }

            void SetListViewSource (MenuItem _currentMenuBarItem, bool fill = false)
            {
                DataContext.Menus = new List<DynamicMenuItemList> ();
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
                    newMenu = new MenuItem (item.Title, item.Help, null, null, parent);
                    newMenu.CheckType = item.CheckStyle;
                    newMenu.Action = _frmMenuDetails.CreateAction (newMenu, item);
                    newMenu.Shortcut = ShortcutHelper.GetShortcutFromTag (item.Shortcut);
                    newMenu.AllowNullChecked = item.AllowNullChecked;
                }
                else if (item.IsTopLevel)
                {
                    newMenu = new MenuBarItem (item.Title, item.Help, null);
                    newMenu.Action = _frmMenuDetails.CreateAction (newMenu, item);
                }
                else
                {
                    newMenu = new MenuBarItem (item.Title, item.Help, null);

                    ((MenuBarItem)newMenu).Children [0].Action =
                        _frmMenuDetails.CreateAction (newMenu, item);

                    ((MenuBarItem)newMenu).Children [0].Shortcut =
                        ShortcutHelper.GetShortcutFromTag (item.Shortcut);
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

                    _currentEditMenuBarItem = new MenuItem (
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
                        DataContext.Menus = new List<DynamicMenuItemList> ();
                    }

                    _currentEditMenuBarItem.Action =
                        _frmMenuDetails.CreateAction (_currentEditMenuBarItem, menuItem);

                    _currentEditMenuBarItem.Shortcut =
                        ShortcutHelper.GetShortcutFromTag (menuItem.Shortcut);
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
                                               new DynamicMenuItemList
                                               {
                                                   Title = _currentEditMenuBarItem.Title, MenuItem = _currentEditMenuBarItem
                                               }
                                              );
                    }

                    DataContext.Menus [index] =
                        new DynamicMenuItemList
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
        public string Shortcut { get; set; }
        public string Title { get; set; } = "_New";
    }

    public class DynamicMenuItemList
    {
        public MenuItem MenuItem { get; set; }
        public string Title { get; set; }
        public override string ToString () { return $"{Title}, {MenuItem}"; }
    }

    public class DynamicMenuItemModel : INotifyPropertyChanged
    {
        private string _menuBar;
        private List<DynamicMenuItemList> _menus;
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
                                         new PropertyChangedEventArgs (GetPropertyName ())
                                        );
            }
        }

        public List<DynamicMenuItemList> Menus
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
                                         new PropertyChangedEventArgs (GetPropertyName ())
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
