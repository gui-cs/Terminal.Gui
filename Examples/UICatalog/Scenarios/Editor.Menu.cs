#nullable enable

using System.Globalization;
using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

public partial class Editor
{
    private MenuItem [] CreateScrollBarsMenu ()
    {
        if (_textView is null)
        {
            return [];
        }

        List<MenuItem> menuItems = [];

        bool hasScrollBars = _textView.ViewportSettings.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar);
        CheckBox scrollBarCheckBox = new () { Title = "_Scroll Bars", Value = hasScrollBars ? CheckState.Checked : CheckState.UnChecked };

        scrollBarCheckBox.ValueChanged += (_, _) =>
                                          {
                                              if (scrollBarCheckBox.Value == CheckState.Checked)
                                              {
                                                  _textView.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar | ViewportSettingsFlags.HasHorizontalScrollBar;
                                              }
                                              else
                                              {
                                                  _textView.ViewportSettings &= ~(ViewportSettingsFlags.HasVerticalScrollBar | ViewportSettingsFlags.HasHorizontalScrollBar);
                                              }
                                          };

        MenuItem verticalItem = new () { CommandView = scrollBarCheckBox };

        verticalItem.Accepting += (_, e) =>
                                  {
                                      scrollBarCheckBox.AdvanceCheckState ();
                                      e.Handled = true;
                                  };

        menuItems.Add (verticalItem);

        return [.. menuItems];
    }

    private MenuItem [] GetSupportedCultures ()
    {
        if (_cultureInfos is null)
        {
            return [];
        }

        List<MenuItem> supportedCultures = [];
        List<CheckBox> allCheckBoxes = [];
        int index = -1;

        foreach (CultureInfo c in _cultureInfos)
        {
            if (index == -1)
            {
                CreateCultureMenuItem ("_English", "en-US", Thread.CurrentThread.CurrentUICulture.Name == "en-US");
                index++;
            }

            CreateCultureMenuItem ($"_{c.Parent.EnglishName}", c.Name, Thread.CurrentThread.CurrentUICulture.Name == c.Name);
        }

        return [.. supportedCultures];

        void CreateCultureMenuItem (string title, string cultureName, bool isChecked)
        {
            CheckBox checkBox = new () { Title = title, Value = isChecked ? CheckState.Checked : CheckState.UnChecked };

            allCheckBoxes.Add (checkBox);

            checkBox.ValueChanged += (_, e) =>
                                     {
                                         if (e.NewValue != CheckState.Checked)
                                         {
                                             return;
                                         }
                                         Thread.CurrentThread.CurrentUICulture = new CultureInfo (cultureName);

                                         foreach (CheckBox cb in allCheckBoxes)
                                         {
                                             cb.Value = cb == checkBox ? CheckState.Checked : CheckState.UnChecked;
                                         }
                                     };

            MenuItem item = new () { CommandView = checkBox };

            item.Accepting += (_, e) =>
                              {
                                  checkBox.AdvanceCheckState ();
                                  e.Handled = true;
                              };

            supportedCultures.Add (item);
        }
    }

    private MenuItem CreateWrapChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Word Wrap" };
        }

        CheckBox checkBox = new () { Title = "_Word Wrap", Value = _textView.WordWrap ? CheckState.Checked : CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) => { _textView.WordWrap = checkBox.Value == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (_, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateReadOnlyChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Read Only" };
        }

        CheckBox checkBox = new () { Title = "Read Only", Value = _textView.ReadOnly ? CheckState.Checked : CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) => { _textView.ReadOnly = checkBox.Value == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (_, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateCanFocusChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "CanFocus" };
        }

        CheckBox checkBox = new () { Title = "CanFocus", Value = _textView.CanFocus ? CheckState.Checked : CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) =>
                                 {
                                     _textView.CanFocus = checkBox.Value == CheckState.Checked;

                                     if (_textView.CanFocus)
                                     {
                                         _textView.SetFocus ();
                                     }
                                 };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (_, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateEnabledChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Enabled" };
        }

        CheckBox checkBox = new () { Title = "Enabled", Value = _textView.Enabled ? CheckState.Checked : CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) =>
                                 {
                                     _textView.Enabled = checkBox.Value == CheckState.Checked;

                                     if (_textView.Enabled)
                                     {
                                         _textView.SetFocus ();
                                     }
                                 };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (_, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateVisibleChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Visible" };
        }

        CheckBox checkBox = new () { Title = "Visible", Value = _textView.Visible ? CheckState.Checked : CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) =>
                                 {
                                     _textView.Visible = checkBox.Value == CheckState.Checked;

                                     if (_textView.Visible)
                                     {
                                         _textView.SetFocus ();
                                     }
                                 };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (_, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }
}
