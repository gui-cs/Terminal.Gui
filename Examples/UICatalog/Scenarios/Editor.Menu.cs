#nullable enable

using System.Globalization;
using System.Text.RegularExpressions;

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

        CheckBox scrollBarCheckBox = new () { Title = "_Scroll Bars", Value = _textView.ScrollBars ? CheckState.Checked : CheckState.UnChecked };

        scrollBarCheckBox.ValueChanged += (_, _) => { _textView.ScrollBars = scrollBarCheckBox.Value == CheckState.Checked; };

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

    private MenuItem CreateAutocomplete ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Autocomplete" };
        }

        SingleWordSuggestionGenerator singleWordGenerator = new ();
        _textView.Autocomplete.SuggestionGenerator = singleWordGenerator;

        CheckBox checkBox = new () { Title = "Autocomplete", Value = CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) =>
                                 {
                                     if (checkBox.Value == CheckState.Checked)
                                     {
                                         singleWordGenerator.AllSuggestions =
                                             Regex.Matches (_textView.Text, "\\w+").Select (m => m.Value).Distinct ().ToList ();
                                     }
                                     else
                                     {
                                         singleWordGenerator.AllSuggestions.Clear ();
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

    private MenuItem CreateAllowsTabChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Tab Enters Tab" };
        }

        CheckBox checkBox = new () { Title = "Tab Enters Tab", Value = _textView.TabKeyAddsTab ? CheckState.Checked : CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) => { _textView.TabKeyAddsTab = checkBox.Value == CheckState.Checked; };

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

    private MenuItem CreateUseSameRuneTypeForWords ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "UseSameRuneTypeForWords" };
        }

        CheckBox checkBox = new () { Title = "UseSameRuneTypeForWords", Value = _textView.UseSameRuneTypeForWords ? CheckState.Checked : CheckState.UnChecked };

        checkBox.ValueChanged += (_, _) => { _textView.UseSameRuneTypeForWords = checkBox.Value == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (_, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateSelectWordOnlyOnDoubleClick ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "SelectWordOnlyOnDoubleClick" };
        }

        CheckBox checkBox = new ()
        {
            Title = "SelectWordOnlyOnDoubleClick", Value = _textView.SelectWordOnlyOnDoubleClick ? CheckState.Checked : CheckState.UnChecked
        };

        checkBox.ValueChanged += (_, _) => { _textView.SelectWordOnlyOnDoubleClick = checkBox.Value == CheckState.Checked; };

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
