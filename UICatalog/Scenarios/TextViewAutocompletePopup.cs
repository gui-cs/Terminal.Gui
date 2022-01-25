using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "TextView Autocomplete Popup", Description: "Show five TextView Autocomplete Popup effects")]
	[ScenarioCategory ("Controls")]
	public class TextViewAutocompletePopup : Scenario {

		TextView textViewTopLeft;
		TextView textViewTopRight;
		TextView textViewBottomLeft;
		TextView textViewBottomRight;
		TextView textViewCentered;
		MenuItem miMultiline;
		MenuItem miWrap;
		StatusItem siMultiline;
		StatusItem siWrap;
		int height = 10;

		public override void Setup ()
		{
			Win.Title = GetName ();
			var width = 20;
			var colorScheme = Colors.Dialog;
			var text = " jamp jemp jimp jomp jump";

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					miMultiline =  new MenuItem ("_Multiline", "", () => Multiline()){CheckType = MenuItemCheckStyle.Checked},
					miWrap =  new MenuItem ("_Word Wrap", "", () => WordWrap()){CheckType = MenuItemCheckStyle.Checked},
					new MenuItem ("_Quit", "", () => Quit())
				})
			});
			Top.Add (menu);

			textViewTopLeft = new TextView () {
				Width = width,
				Height = height,
				ColorScheme = colorScheme,
				Text = text
			};
			textViewTopLeft.DrawContent += TextViewTopLeft_DrawContent;
			Win.Add (textViewTopLeft);

			textViewTopRight = new TextView () {
				X = Pos.AnchorEnd () - width,
				Width = width,
				Height = height,
				ColorScheme = colorScheme,
				Text = text
			};
			textViewTopRight.DrawContent += TextViewTopRight_DrawContent;
			Win.Add (textViewTopRight);

			textViewBottomLeft = new TextView () {
				Y = Pos.Bottom (Win) - height - 3,
				Width = width,
				Height = height,
				ColorScheme = colorScheme,
				Text = text
			};
			textViewBottomLeft.DrawContent += TextViewBottomLeft_DrawContent;
			Win.Add (textViewBottomLeft);

			textViewBottomRight = new TextView () {
				X = Pos.Right (Win) - width - 2,
				Y = Pos.Bottom (Win) - height - 3,
				Width = width,
				Height = height,
				ColorScheme = colorScheme,
				Text = text
			};
			textViewBottomRight.DrawContent += TextViewBottomRight_DrawContent;
			Win.Add (textViewBottomRight);

			textViewCentered = new TextView () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = width,
				Height = height,
				ColorScheme = colorScheme,
				Text = text
			};
			textViewCentered.DrawContent += TextViewCentered_DrawContent;
			Win.Add (textViewCentered);

			miMultiline.Checked = textViewTopLeft.Multiline;
			miWrap.Checked = textViewTopLeft.WordWrap;

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
				siMultiline = new StatusItem(Key.Null, "", null),
				siWrap = new StatusItem(Key.Null, "", null)
			});
			Top.Add (statusBar);

			Win.LayoutStarted += Win_LayoutStarted;
		}

		private void Win_LayoutStarted (View.LayoutEventArgs obj)
		{
			miMultiline.Checked = textViewTopLeft.Multiline;
			miWrap.Checked = textViewTopLeft.WordWrap;
			SetMultilineStatusText ();
			SetWrapStatusText ();

			if (miMultiline.Checked) {
				height = 10;
			} else {
				height = 1;
			}
			textViewBottomLeft.Y = textViewBottomRight.Y = Pos.Bottom (Win) - height - 3;
		}

		private void SetMultilineStatusText ()
		{
			siMultiline.Title = $"Multiline: {miMultiline.Checked}";
		}

		private void SetWrapStatusText ()
		{
			siWrap.Title = $"WordWrap: {miWrap.Checked}";
		}

		private void SetAllSuggestions (TextView view)
		{
			view.Autocomplete.AllSuggestions = Regex.Matches (view.Text.ToString (), "\\w+")
				.Select (s => s.Value)
				.Distinct ().ToList ();
		}

		private void TextViewCentered_DrawContent (Rect obj)
		{
			SetAllSuggestions (textViewCentered);
		}

		private void TextViewBottomRight_DrawContent (Rect obj)
		{
			SetAllSuggestions (textViewBottomRight);
		}

		private void TextViewBottomLeft_DrawContent (Rect obj)
		{
			SetAllSuggestions (textViewBottomLeft);
		}

		private void TextViewTopRight_DrawContent (Rect obj)
		{
			SetAllSuggestions (textViewTopRight);
		}

		private void TextViewTopLeft_DrawContent (Rect obj)
		{
			SetAllSuggestions (textViewTopLeft);
		}

		private void Multiline ()
		{
			miMultiline.Checked = !miMultiline.Checked;
			SetMultilineStatusText ();
			textViewTopLeft.Multiline = miMultiline.Checked;
			textViewTopRight.Multiline = miMultiline.Checked;
			textViewBottomLeft.Multiline = miMultiline.Checked;
			textViewBottomRight.Multiline = miMultiline.Checked;
			textViewCentered.Multiline = miMultiline.Checked;
		}

		private void WordWrap ()
		{
			miWrap.Checked = !miWrap.Checked;
			textViewTopLeft.WordWrap = miWrap.Checked;
			textViewTopRight.WordWrap = miWrap.Checked;
			textViewBottomLeft.WordWrap = miWrap.Checked;
			textViewBottomRight.WordWrap = miWrap.Checked;
			textViewCentered.WordWrap = miWrap.Checked;
			miWrap.Checked = textViewTopLeft.WordWrap;
			SetWrapStatusText ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
