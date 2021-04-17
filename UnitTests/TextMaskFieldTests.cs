using Xunit;

namespace Terminal.Gui {
	public class TextMaskFieldTests {
		private TextMaskField _textMaskField_Mask;
		private TextMaskField _textMaskField_Regex;

		public TextMaskFieldTests ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			_textMaskField_Mask = new TextMaskField ("+(999) 000-0000");
			_textMaskField_Regex = new TextMaskField ("^[A-Z][0-9]$", TextMaskField.TextMaskType.Regex);
		}

		[Fact]
		public void Validate_Regex_Field_RegexValidateInput_False ()
		{
			// Allow input. Validates field.
			_textMaskField_Regex.RegexValidateInput = false;

			_textMaskField_Regex.ProcessKey (new KeyEvent ((Key)(int)'E', new KeyModifiers ()));
			Assert.Equal ("E", _textMaskField_Regex.Text);
			Assert.False (_textMaskField_Regex.IsValid);

			_textMaskField_Regex.ProcessKey (new KeyEvent ((Key)(int)'Z', new KeyModifiers ()));
			Assert.Equal ("EZ", _textMaskField_Regex.Text);
			Assert.False (_textMaskField_Regex.IsValid);

			_textMaskField_Regex.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("E", _textMaskField_Regex.Text);
			Assert.False (_textMaskField_Regex.IsValid);

			_textMaskField_Regex.ProcessKey (new KeyEvent ((Key)(int)'4', new KeyModifiers ()));
			Assert.Equal ("E4", _textMaskField_Regex.Text);
			Assert.True (_textMaskField_Regex.IsValid);
		}

		[Fact]
		public void Validate_Mask_Field ()
		{
			_textMaskField_Mask.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			_textMaskField_Mask.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			_textMaskField_Mask.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));

			_textMaskField_Mask.ProcessKey (new KeyEvent ((Key)(int)'1', new KeyModifiers ()));
			Assert.Equal ("+(   ) 1  -    ", _textMaskField_Mask.Text);
			Assert.False (_textMaskField_Mask.IsValid);

			_textMaskField_Mask.ProcessKey (new KeyEvent ((Key)(int)'2', new KeyModifiers ()));
			Assert.Equal ("+(   ) 12 -    ", _textMaskField_Mask.Text);
			Assert.False (_textMaskField_Mask.IsValid);

			_textMaskField_Mask.ProcessKey (new KeyEvent ((Key)(int)'3', new KeyModifiers ()));
			Assert.Equal ("+(   ) 123-    ", _textMaskField_Mask.Text);
			Assert.False (_textMaskField_Mask.IsValid);

			_textMaskField_Mask.ProcessKey (new KeyEvent ((Key)(int)'4', new KeyModifiers ()));
			Assert.Equal ("+(   ) 123-4   ", _textMaskField_Mask.Text);
			Assert.False (_textMaskField_Mask.IsValid);

			_textMaskField_Mask.ProcessKey (new KeyEvent ((Key)(int)'5', new KeyModifiers ()));
			Assert.Equal ("+(   ) 123-45  ", _textMaskField_Mask.Text);
			Assert.False (_textMaskField_Mask.IsValid);

			_textMaskField_Mask.ProcessKey (new KeyEvent ((Key)(int)'6', new KeyModifiers ()));
			Assert.Equal ("+(   ) 123-456 ", _textMaskField_Mask.Text);
			Assert.False (_textMaskField_Mask.IsValid);

			_textMaskField_Mask.ProcessKey (new KeyEvent ((Key)(int)'7', new KeyModifiers ()));
			Assert.Equal ("+(   ) 123-4567", _textMaskField_Mask.Text);
			Assert.True (_textMaskField_Mask.IsValid);
		}
	}
}
