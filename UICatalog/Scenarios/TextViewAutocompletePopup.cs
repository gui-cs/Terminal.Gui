using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TextView Autocomplete Popup", "Shows five TextView Autocomplete Popup effects")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class TextViewAutocompletePopup : Scenario {
	int _height = 10;
	MenuItem _miMultiline;
	MenuItem _miWrap;
	StatusItem _siMultiline;
	StatusItem _siWrap;
	TextView _textViewBottomLeft;
	TextView _textViewBottomRight;
	TextView _textViewCentered;

	TextView _textViewTopLeft;
	TextView _textViewTopRight;

	public override void Setup ()
	{
		Win.Title = GetName ();
		var width = 20;
		var text = " jamp jemp jimp jomp jump";


		var menu = new MenuBar {
			Menus = [
				new MenuBarItem ("_File", new [] {
					_miMultiline = new MenuItem ("_Multiline", "", () => Multiline ())
						{ CheckType = MenuItemCheckStyle.Checked },
					_miWrap = new MenuItem ("_Word Wrap", "", () => WordWrap ())
						{ CheckType = MenuItemCheckStyle.Checked },
					new("_Quit", "", () => Quit ())
				})
			]
		};
		Application.Top.Add (menu);

		_textViewTopLeft = new TextView {
			Width = width,
			Height = _height,
			Text = text
		};
		_textViewTopLeft.DrawContent += TextViewTopLeft_DrawContent;
		Win.Add (_textViewTopLeft);

		_textViewTopRight = new TextView {
			X = Pos.AnchorEnd (width),
			Width = width,
			Height = _height,
			Text = text
		};
		_textViewTopRight.DrawContent += TextViewTopRight_DrawContent;
		Win.Add (_textViewTopRight);

		_textViewBottomLeft = new TextView {
			Y = Pos.AnchorEnd (_height),
			Width = width,
			Height = _height,
			Text = text
		};
		_textViewBottomLeft.DrawContent += TextViewBottomLeft_DrawContent;
		Win.Add (_textViewBottomLeft);

		_textViewBottomRight = new TextView {
			X = Pos.AnchorEnd (width),
			Y = Pos.AnchorEnd (_height),
			Width = width,
			Height = _height,
			Text = text
		};
		_textViewBottomRight.DrawContent += TextViewBottomRight_DrawContent;
		Win.Add (_textViewBottomRight);

		_textViewCentered = new TextView {
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = width,
			Height = _height,
			Text = text
		};
		_textViewCentered.DrawContent += TextViewCentered_DrawContent;
		Win.Add (_textViewCentered);

		_miMultiline.Checked = _textViewTopLeft.Multiline;
		_miWrap.Checked = _textViewTopLeft.WordWrap;

		var statusBar = new StatusBar (new [] {
			new(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit ()),
			_siMultiline = new StatusItem (KeyCode.Null, "", null),
			_siWrap = new StatusItem (KeyCode.Null, "", null)
		});
		Application.Top.Add (statusBar);

		Win.LayoutStarted += Win_LayoutStarted;
	}

	void Win_LayoutStarted (object sender, LayoutEventArgs obj)
	{
		_miMultiline.Checked = _textViewTopLeft.Multiline;
		_miWrap.Checked = _textViewTopLeft.WordWrap;
		SetMultilineStatusText ();
		SetWrapStatusText ();

		if (_miMultiline.Checked == true) {
			_height = 10;
		} else {
			_height = 1;
		}

		_textViewBottomLeft.Y = _textViewBottomRight.Y = Pos.AnchorEnd (_height);
	}

	void SetMultilineStatusText () => _siMultiline.Title = $"Multiline: {_miMultiline.Checked}";

	void SetWrapStatusText () => _siWrap.Title = $"WordWrap: {_miWrap.Checked}";

	void SetAllSuggestions (TextView view) =>
		((SingleWordSuggestionGenerator)view.Autocomplete.SuggestionGenerator).AllSuggestions = Regex
			.Matches (view.Text, "\\w+")
			.Select (s => s.Value)
			.Distinct ().ToList ();

	void TextViewCentered_DrawContent (object sender, DrawEventArgs e) => SetAllSuggestions (_textViewCentered);

	void TextViewBottomRight_DrawContent (object sender, DrawEventArgs e) =>
		SetAllSuggestions (_textViewBottomRight);

	void TextViewBottomLeft_DrawContent (object sender, DrawEventArgs e) => SetAllSuggestions (_textViewBottomLeft);

	void TextViewTopRight_DrawContent (object sender, DrawEventArgs e) => SetAllSuggestions (_textViewTopRight);

	void TextViewTopLeft_DrawContent (object sender, DrawEventArgs e) => SetAllSuggestions (_textViewTopLeft);

	void Multiline ()
	{
		_miMultiline.Checked = !_miMultiline.Checked;
		SetMultilineStatusText ();
		_textViewTopLeft.Multiline = (bool)_miMultiline.Checked;
		_textViewTopRight.Multiline = (bool)_miMultiline.Checked;
		_textViewBottomLeft.Multiline = (bool)_miMultiline.Checked;
		_textViewBottomRight.Multiline = (bool)_miMultiline.Checked;
		_textViewCentered.Multiline = (bool)_miMultiline.Checked;
	}

	void WordWrap ()
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

	void Quit () => Application.RequestStop ();
}