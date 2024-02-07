using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui;

namespace UICatalog.Scenarios; 

[ScenarioMetadata ("TextView Autocomplete Popup", "Shows five TextView Autocomplete Popup effects")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class TextViewAutocompletePopup : Scenario {
    private int height = 10;
    private MenuItem miMultiline;
    private MenuItem miWrap;
    private StatusItem siMultiline;
    private StatusItem siWrap;
    private TextView textViewBottomLeft;
    private TextView textViewBottomRight;
    private TextView textViewCentered;
    private TextView textViewTopLeft;
    private TextView textViewTopRight;

    public override void Setup () {
        Win.Title = GetName ();
        var width = 20;
        var text = " jamp jemp jimp jomp jump";

        var menu = new MenuBar (
                                new MenuBarItem[] {
                                                      new (
                                                           "_File",
                                                           new[] {
                                                                     miMultiline =
                                                                         new MenuItem (
                                                                          "_Multiline",
                                                                          "",
                                                                          () => Multiline ())
                                                                         { CheckType = MenuItemCheckStyle.Checked },
                                                                     miWrap = new MenuItem (
                                                                               "_Word Wrap",
                                                                               "",
                                                                               () => WordWrap ()) {
                                                                                  CheckType = MenuItemCheckStyle.Checked
                                                                              },
                                                                     new ("_Quit", "", () => Quit ())
                                                                 })
                                                  });
        Application.Top.Add (menu);

        textViewTopLeft = new TextView {
                                           Width = width,
                                           Height = height,
                                           Text = text
                                       };
        textViewTopLeft.DrawContent += TextViewTopLeft_DrawContent;
        Win.Add (textViewTopLeft);

        textViewTopRight = new TextView {
                                            X = Pos.AnchorEnd (width),
                                            Width = width,
                                            Height = height,
                                            Text = text
                                        };
        textViewTopRight.DrawContent += TextViewTopRight_DrawContent;
        Win.Add (textViewTopRight);

        textViewBottomLeft = new TextView {
                                              Y = Pos.AnchorEnd (height),
                                              Width = width,
                                              Height = height,
                                              Text = text
                                          };
        textViewBottomLeft.DrawContent += TextViewBottomLeft_DrawContent;
        Win.Add (textViewBottomLeft);

        textViewBottomRight = new TextView {
                                               X = Pos.AnchorEnd (width),
                                               Y = Pos.AnchorEnd (height),
                                               Width = width,
                                               Height = height,
                                               Text = text
                                           };
        textViewBottomRight.DrawContent += TextViewBottomRight_DrawContent;
        Win.Add (textViewBottomRight);

        textViewCentered = new TextView {
                                            X = Pos.Center (),
                                            Y = Pos.Center (),
                                            Width = width,
                                            Height = height,
                                            Text = text
                                        };
        textViewCentered.DrawContent += TextViewCentered_DrawContent;
        Win.Add (textViewCentered);

        miMultiline.Checked = textViewTopLeft.Multiline;
        miWrap.Checked = textViewTopLeft.WordWrap;

        var statusBar = new StatusBar (
                                       new[] {
                                                 new (
                                                      Application.QuitKey,
                                                      $"{Application.QuitKey} to Quit",
                                                      () => Quit ()),
                                                 siMultiline = new StatusItem (KeyCode.Null, "", null),
                                                 siWrap = new StatusItem (KeyCode.Null, "", null)
                                             });
        Application.Top.Add (statusBar);

        Win.LayoutStarted += Win_LayoutStarted;
    }

    private void Multiline () {
        miMultiline.Checked = !miMultiline.Checked;
        SetMultilineStatusText ();
        textViewTopLeft.Multiline = (bool)miMultiline.Checked;
        textViewTopRight.Multiline = (bool)miMultiline.Checked;
        textViewBottomLeft.Multiline = (bool)miMultiline.Checked;
        textViewBottomRight.Multiline = (bool)miMultiline.Checked;
        textViewCentered.Multiline = (bool)miMultiline.Checked;
    }

    private void Quit () { Application.RequestStop (); }

    private void SetAllSuggestions (TextView view) {
        ((SingleWordSuggestionGenerator)view.Autocomplete.SuggestionGenerator).AllSuggestions = Regex
            .Matches (view.Text, "\\w+")
            .Select (s => s.Value)
            .Distinct ()
            .ToList ();
    }

    private void SetMultilineStatusText () { siMultiline.Title = $"Multiline: {miMultiline.Checked}"; }
    private void SetWrapStatusText () { siWrap.Title = $"WordWrap: {miWrap.Checked}"; }

    private void TextViewBottomLeft_DrawContent (object sender, DrawEventArgs e) {
        SetAllSuggestions (textViewBottomLeft);
    }

    private void TextViewBottomRight_DrawContent (object sender, DrawEventArgs e) {
        SetAllSuggestions (textViewBottomRight);
    }

    private void TextViewCentered_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (textViewCentered); }
    private void TextViewTopLeft_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (textViewTopLeft); }
    private void TextViewTopRight_DrawContent (object sender, DrawEventArgs e) { SetAllSuggestions (textViewTopRight); }

    private void Win_LayoutStarted (object sender, LayoutEventArgs obj) {
        miMultiline.Checked = textViewTopLeft.Multiline;
        miWrap.Checked = textViewTopLeft.WordWrap;
        SetMultilineStatusText ();
        SetWrapStatusText ();

        if (miMultiline.Checked == true) {
            height = 10;
        } else {
            height = 1;
        }

        textViewBottomLeft.Y = textViewBottomRight.Y = Pos.AnchorEnd (height);
    }

    private void WordWrap () {
        miWrap.Checked = !miWrap.Checked;
        textViewTopLeft.WordWrap = (bool)miWrap.Checked;
        textViewTopRight.WordWrap = (bool)miWrap.Checked;
        textViewBottomLeft.WordWrap = (bool)miWrap.Checked;
        textViewBottomRight.WordWrap = (bool)miWrap.Checked;
        textViewCentered.WordWrap = (bool)miWrap.Checked;
        miWrap.Checked = textViewTopLeft.WordWrap;
        SetWrapStatusText ();
    }
}
