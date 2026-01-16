#nullable enable

using System.Text.RegularExpressions;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TextView Autocomplete Popup", "Shows five TextView Autocomplete Popup effects")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class TextViewAutocompletePopup : Scenario
{
    private int _height = 10;
    private CheckBox? _miMultilineCheckBox;
    private CheckBox? _miWrapCheckBox;
    private Shortcut? _siMultiline;
    private Shortcut? _siWrap;
    private TextView? _textViewBottomLeft;
    private TextView? _textViewBottomRight;
    private TextView? _textViewCentered;
    private TextView? _textViewTopLeft;
    private TextView? _textViewTopRight;

    private Window? _appWindow;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        _appWindow = new ()
        {
            BorderStyle = LineStyle.None
        };

        int width = 20;
        string text = " jamp jemp jimp jomp jump";

        // MenuBar
        MenuBar menu = new ();

        _textViewTopLeft = new ()
        {
            Y = Pos.Bottom (menu),
            Width = width,
            Height = _height,
            Text = text
        };
        _textViewTopLeft.DrawingContent += TextViewTopLeft_DrawContent;
        _appWindow.Add (_textViewTopLeft);

        _textViewTopRight = new ()
        {
            X = Pos.AnchorEnd (width),
            Y = Pos.Bottom (menu),
            Width = width,
            Height = _height,
            Text = text
        };
        _textViewTopRight.DrawingContent += TextViewTopRight_DrawContent;
        _appWindow.Add (_textViewTopRight);

        _textViewBottomLeft = new ()
        {
            Y = Pos.AnchorEnd (_height),
            Width = width,
            Height = _height,
            Text = text
        };
        _textViewBottomLeft.DrawingContent += TextViewBottomLeft_DrawContent;
        _appWindow.Add (_textViewBottomLeft);

        _textViewBottomRight = new ()
        {
            X = Pos.AnchorEnd (width),
            Y = Pos.AnchorEnd (_height),
            Width = width,
            Height = _height,
            Text = text
        };
        _textViewBottomRight.DrawingContent += TextViewBottomRight_DrawContent;
        _appWindow.Add (_textViewBottomRight);

        _textViewCentered = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = width,
            Height = _height,
            Text = text
        };
        _textViewCentered.DrawingContent += TextViewCentered_DrawContent;
        _appWindow.Add (_textViewCentered);

        // Setup menu checkboxes
        _miMultilineCheckBox = new ()
        {
            Title = "_Multiline",
            CheckedState = _textViewTopLeft.Multiline ? CheckState.Checked : CheckState.UnChecked
        };
        _miMultilineCheckBox.CheckedStateChanged += (_, _) => Multiline ();

        _miWrapCheckBox = new ()
        {
            Title = "_Word Wrap",
            CheckedState = _textViewTopLeft.WordWrap ? CheckState.Checked : CheckState.UnChecked
        };
        _miWrapCheckBox.CheckedStateChanged += (_, _) => WordWrap ();

        menu.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuItem
                                       {
                                           CommandView = _miMultilineCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miWrapCheckBox
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        // StatusBar
        _siMultiline = new (Key.Empty, "", null);
        _siWrap = new (Key.Empty, "", null);

        StatusBar statusBar = new (
                                   [
                                       new (
                                            Application.QuitKey,
                                            "Quit",
                                            () => Quit ()
                                           ),
                                       _siMultiline,
                                       _siWrap
                                   ]
                                  );

        _appWindow.Add (menu, statusBar);

        _appWindow.SubViewLayout += Win_LayoutStarted;

        app.Run (_appWindow);
        _appWindow?.Dispose ();
    }

    private void Multiline ()
    {
        if (_miMultilineCheckBox is null
            || _textViewTopLeft is null
            || _textViewTopRight is null
            || _textViewBottomLeft is null
            || _textViewBottomRight is null
            || _textViewCentered is null)
        {
            return;
        }

        SetMultilineStatusText ();
        _textViewTopLeft.Multiline = _miMultilineCheckBox.CheckedState == CheckState.Checked;
        _textViewTopRight.Multiline = _miMultilineCheckBox.CheckedState == CheckState.Checked;
        _textViewBottomLeft.Multiline = _miMultilineCheckBox.CheckedState == CheckState.Checked;
        _textViewBottomRight.Multiline = _miMultilineCheckBox.CheckedState == CheckState.Checked;
        _textViewCentered.Multiline = _miMultilineCheckBox.CheckedState == CheckState.Checked;
    }

    private void Quit () { _appWindow?.RequestStop (); }

    private void SetAllSuggestions (TextView view)
    {
        if (view.Autocomplete.SuggestionGenerator is SingleWordSuggestionGenerator generator)
        {
            generator.AllSuggestions = Regex
                                       .Matches (view.Text, "\\w+")
                                       .Select (s => s.Value)
                                       .Distinct ()
                                       .ToList ();
        }
    }

    private void SetMultilineStatusText ()
    {
        if (_siMultiline is not null && _miMultilineCheckBox is not null)
        {
            _siMultiline.Title = $"Multiline: {_miMultilineCheckBox.CheckedState == CheckState.Checked}";
        }
    }

    private void SetWrapStatusText ()
    {
        if (_siWrap is not null && _miWrapCheckBox is not null)
        {
            _siWrap.Title = $"WordWrap: {_miWrapCheckBox.CheckedState == CheckState.Checked}";
        }
    }

    private void TextViewBottomLeft_DrawContent (object? sender, DrawEventArgs e)
    {
        if (_textViewBottomLeft is not null)
        {
            SetAllSuggestions (_textViewBottomLeft);
        }
    }

    private void TextViewBottomRight_DrawContent (object? sender, DrawEventArgs e)
    {
        if (_textViewBottomRight is not null)
        {
            SetAllSuggestions (_textViewBottomRight);
        }
    }

    private void TextViewCentered_DrawContent (object? sender, DrawEventArgs e)
    {
        if (_textViewCentered is not null)
        {
            SetAllSuggestions (_textViewCentered);
        }
    }

    private void TextViewTopLeft_DrawContent (object? sender, DrawEventArgs e)
    {
        if (_textViewTopLeft is not null)
        {
            SetAllSuggestions (_textViewTopLeft);
        }
    }

    private void TextViewTopRight_DrawContent (object? sender, DrawEventArgs e)
    {
        if (_textViewTopRight is not null)
        {
            SetAllSuggestions (_textViewTopRight);
        }
    }

    private void Win_LayoutStarted (object? sender, LayoutEventArgs obj)
    {
        if (_textViewTopLeft is null || _miMultilineCheckBox is null || _miWrapCheckBox is null || _textViewBottomLeft is null || _textViewBottomRight is null)
        {
            return;
        }

        _miMultilineCheckBox.CheckedState = _textViewTopLeft.Multiline ? CheckState.Checked : CheckState.UnChecked;
        _miWrapCheckBox.CheckedState = _textViewTopLeft.WordWrap ? CheckState.Checked : CheckState.UnChecked;
        SetMultilineStatusText ();
        SetWrapStatusText ();

        if (_miMultilineCheckBox.CheckedState == CheckState.Checked)
        {
            _height = 10;
        }
        else
        {
            _height = 1;
        }

        _textViewBottomLeft.Y = _textViewBottomRight.Y = Pos.AnchorEnd (_height);
    }

    private void WordWrap ()
    {
        if (_miWrapCheckBox is null
            || _textViewTopLeft is null
            || _textViewTopRight is null
            || _textViewBottomLeft is null
            || _textViewBottomRight is null
            || _textViewCentered is null)
        {
            return;
        }

        _textViewTopLeft.WordWrap = _miWrapCheckBox.CheckedState == CheckState.Checked;
        _textViewTopRight.WordWrap = _miWrapCheckBox.CheckedState == CheckState.Checked;
        _textViewBottomLeft.WordWrap = _miWrapCheckBox.CheckedState == CheckState.Checked;
        _textViewBottomRight.WordWrap = _miWrapCheckBox.CheckedState == CheckState.Checked;
        _textViewCentered.WordWrap = _miWrapCheckBox.CheckedState == CheckState.Checked;
        _miWrapCheckBox.CheckedState = _textViewTopLeft.WordWrap ? CheckState.Checked : CheckState.UnChecked;
        SetWrapStatusText ();
    }
}
