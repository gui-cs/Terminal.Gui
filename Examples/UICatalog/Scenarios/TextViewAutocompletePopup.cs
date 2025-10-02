using System.Linq;
using System.Text.RegularExpressions;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TextView Autocomplete Popup", "Shows five TextView Autocomplete Popup effects")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class TextViewAutocompletePopup : Scenario
{
    private int _height = 10;
    private MenuItem _miMultiline;
    private MenuItem _miWrap;
    private Shortcut _siMultiline;
    private Shortcut _siWrap;
    private TextView _textViewBottomLeft;
    private TextView _textViewBottomRight;
    private TextView _textViewCentered;
    private TextView _textViewTopLeft;
    private TextView _textViewTopRight;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Toplevel appWindow = new ();

        var width = 20;
        var text = " jamp jemp jimp jomp jump";

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new []
                     {
                         _miMultiline =
                             new (
                                  "_Multiline",
                                  "",
                                  () => Multiline ()
                                 ) { CheckType = MenuItemCheckStyle.Checked },
                         _miWrap = new (
                                        "_Word Wrap",
                                        "",
                                        () => WordWrap ()
                                       ) { CheckType = MenuItemCheckStyle.Checked },
                         new ("_Quit", "", () => Quit ())
                     }
                    )
            ]
        };
        appWindow.Add (menu);

        _textViewTopLeft = new()
        {
            Y = 1,
            Width = width, Height = _height, Text = text
        };
        _textViewTopLeft.DrawingContent += TextViewTopLeft_DrawContent;
        appWindow.Add (_textViewTopLeft);

        _textViewTopRight = new()
        {
            X = Pos.AnchorEnd (width), Y = 1,
            Width = width, Height = _height, Text = text
        };
        _textViewTopRight.DrawingContent += TextViewTopRight_DrawContent;
        appWindow.Add (_textViewTopRight);

        _textViewBottomLeft = new()
        {
            Y = Pos.AnchorEnd (_height), Width = width, Height = _height, Text = text
        };
        _textViewBottomLeft.DrawingContent += TextViewBottomLeft_DrawContent;
        appWindow.Add (_textViewBottomLeft);

        _textViewBottomRight = new()
        {
            X = Pos.AnchorEnd (width),
            Y = Pos.AnchorEnd (_height),
            Width = width,
            Height = _height,
            Text = text
        };
        _textViewBottomRight.DrawingContent += TextViewBottomRight_DrawContent;
        appWindow.Add (_textViewBottomRight);

        _textViewCentered = new()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = width,
            Height = _height,
            Text = text
        };
        _textViewCentered.DrawingContent += TextViewCentered_DrawContent;
        appWindow.Add (_textViewCentered);

        _miMultiline.Checked = _textViewTopLeft.Multiline;
        _miWrap.Checked = _textViewTopLeft.WordWrap;

        var statusBar = new StatusBar (
                                       new []
                                       {
                                           new (
                                                Application.QuitKey,
                                                "Quit",
                                                () => Quit ()
                                               ),
                                           _siMultiline = new (Key.Empty, "", null),
                                           _siWrap = new (Key.Empty, "", null)
                                       }
                                      );
        appWindow.Add (statusBar);

        appWindow.SubViewLayout += Win_LayoutStarted;

        // Run - Start the application.
        Application.Run (appWindow);

        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void Multiline ()
    {
        _miMultiline.Checked = !_miMultiline.Checked;
        SetMultilineStatusText ();
        _textViewTopLeft.Multiline = (bool)_miMultiline.Checked;
        _textViewTopRight.Multiline = (bool)_miMultiline.Checked;
        _textViewBottomLeft.Multiline = (bool)_miMultiline.Checked;
        _textViewBottomRight.Multiline = (bool)_miMultiline.Checked;
        _textViewCentered.Multiline = (bool)_miMultiline.Checked;
    }

    private void Quit () { Application.RequestStop (); }

    private void SetAllSuggestions (TextView view)
    {
        ((SingleWordSuggestionGenerator)view.Autocomplete.SuggestionGenerator).AllSuggestions = Regex
                                                                                                .Matches (view.Text, "\\w+")
                                                                                                .Select (s => s.Value)
                                                                                                .Distinct ()
                                                                                                .ToList ();
    }

    private void SetMultilineStatusText () { _siMultiline.Title = $"Multiline: {_miMultiline.Checked}"; }

    private void SetWrapStatusText () { _siWrap.Title = $"WordWrap: {_miWrap.Checked}"; }
    private void TextViewBottomLeft_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (_textViewBottomLeft); }
    private void TextViewBottomRight_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (_textViewBottomRight); }
    private void TextViewCentered_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (_textViewCentered); }
    private void TextViewTopLeft_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (_textViewTopLeft); }
    private void TextViewTopRight_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (_textViewTopRight); }

    private void Win_LayoutStarted (object sender, LayoutEventArgs obj)
    {
        _miMultiline.Checked = _textViewTopLeft.Multiline;
        _miWrap.Checked = _textViewTopLeft.WordWrap;
        SetMultilineStatusText ();
        SetWrapStatusText ();

        if (_miMultiline.Checked == true)
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
        _miWrap.Checked = !_miWrap.Checked;
        _textViewTopLeft.WordWrap = (bool)_miWrap.Checked;
        _textViewTopRight.WordWrap = (bool)_miWrap.Checked;
        _textViewBottomLeft.WordWrap = (bool)_miWrap.Checked;
        _textViewBottomRight.WordWrap = (bool)_miWrap.Checked;
        _textViewCentered.WordWrap = (bool)_miWrap.Checked;
        _miWrap.Checked = _textViewTopLeft.WordWrap;
        SetWrapStatusText ();
    }
}
