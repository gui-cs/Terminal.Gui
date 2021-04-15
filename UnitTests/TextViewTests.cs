using Xunit;

namespace Terminal.Gui {
	public class TextViewTests {
		private TextView _textView;

		public TextViewTests ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			//                   1         2         3 
			//         01234567890123456789012345678901=32 (Length)
			var txt = "TAB to jump between text fields.";
			var buff = new byte [txt.Length];
			for (int i = 0; i < txt.Length; i++) {
				buff [i] = (byte)txt [i];
			}
			var ms = new System.IO.MemoryStream (buff).ToArray ();
			_textView = new TextView () { Width = 30, Height = 10 };
			_textView.Text = ms;
		}

		[Fact]
		public void Changing_Selection_Or_CursorPosition_Update_SelectedLength_And_SelectedText ()
		{
			_textView.SelectionStartColumn = 2;
			_textView.SelectionStartRow = 0;
			Assert.Equal (0, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (2, _textView.SelectedLength);
			Assert.Equal ("TA", _textView.SelectedText);
			_textView.CursorPosition = new Point (20, 0);
			Assert.Equal (2, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (18, _textView.SelectedLength);
			Assert.Equal ("B to jump between ", _textView.SelectedText);
		}

		[Fact]
		public void Selection_With_Value_Less_Than_Zero_Changes_To_Zero ()
		{
			_textView.SelectionStartColumn = -2;
			_textView.SelectionStartRow = -2;
			Assert.Equal (0, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void Selection_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textView.CursorPosition = new Point (2, 0);
			_textView.SelectionStartColumn = 33;
			_textView.SelectionStartRow = 1;
			Assert.Equal (32, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (30, _textView.SelectedLength);
			Assert.Equal ("B to jump between text fields.", _textView.SelectedText);
		}

		[Fact]
		public void Selection_With_Empty_Text ()
		{
			_textView = new TextView ();
			_textView.CursorPosition = new Point (2, 0);
			_textView.SelectionStartColumn = 33;
			_textView.SelectionStartRow = 1;
			Assert.Equal (0, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void Selection_And_CursorPosition_With_Value_Greater_Than_Text_Length_Changes_Both_To_Text_Length ()
		{
			_textView.CursorPosition = new Point (33, 2);
			_textView.SelectionStartColumn = 33;
			_textView.SelectionStartRow = 33;
			Assert.Equal (32, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (32, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void CursorPosition_With_Value_Less_Than_Zero_Changes_To_Zero ()
		{
			_textView.CursorPosition = new Point (-1, -1);
			Assert.Equal (0, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void CursorPosition_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textView.CursorPosition = new Point (33, 1);
			Assert.Equal (32, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void WordForward_With_No_Selection ()
		{
			_textView.CursorPosition = new Point (0, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordBackward_With_No_Selection ()
		{
			_textView.CursorPosition = new Point (_textView.Text.Length, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordForward_With_Selection ()
		{
			_textView.CursorPosition = new Point (0, 0);
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (4, _textView.SelectedLength);
					Assert.Equal ("TAB ", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (7, _textView.SelectedLength);
					Assert.Equal ("TAB to ", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("TAB to jump ", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (20, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between ", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (25, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between text ", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (32, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordBackward_With_Selection ()
		{
			_textView.CursorPosition = new Point (_textView.Text.Length, 0);
			_textView.SelectionStartColumn = _textView.Text.Length;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (7, _textView.SelectedLength);
					Assert.Equal ("fields.", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("text fields.", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (20, _textView.SelectedLength);
					Assert.Equal ("between text fields.", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (25, _textView.SelectedLength);
					Assert.Equal ("jump between text fields.", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (28, _textView.SelectedLength);
					Assert.Equal ("to jump between text fields.", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (32, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordForward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textView.CursorPosition = new Point (10, 0);
			_textView.SelectionStartColumn = 10;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (2, _textView.SelectedLength);
					Assert.Equal ("p ", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (10, _textView.SelectedLength);
					Assert.Equal ("p between ", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (15, _textView.SelectedLength);
					Assert.Equal ("p between text ", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (32, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (22, _textView.SelectedLength);
					Assert.Equal ("p between text fields.", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordBackward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textView.CursorPosition = new Point (10, 0);
			_textView.SelectionStartColumn = 10;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (3, _textView.SelectedLength);
					Assert.Equal ("jum", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (6, _textView.SelectedLength);
					Assert.Equal ("to jum", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (10, _textView.SelectedLength);
					Assert.Equal ("TAB to jum", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordForward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                          1         2         3         4         5    
			//                0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textView.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textView.CursorPosition = new Point (0, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (6, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (9, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (28, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (38, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (40, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (46, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (48, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (55, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordBackward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                          1         2         3         4         5    
			//                0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textView.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textView.CursorPosition = new Point (_textView.Text.Length, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (54, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (48, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (46, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (40, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (38, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (28, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (9, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (6, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 10:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordBackward_Multiline_With_Selection ()
		{
			//		          4         3          2         1
			//		  87654321098765432109876 54321098765432109876543210-Length
			//			    1         2              1         2
			//                01234567890123456789012  0123456789012345678901234
			_textView.Text = "This is the first line.\nThis is the second line.";

			_textView.MoveEnd ();
			_textView.SelectionStartColumn = _textView.CurrentColumn;
			_textView.SelectionStartRow = _textView.CurrentRow;
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (19, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (5, _textView.SelectedLength);
					Assert.Equal ("line.", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("second line.", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (16, _textView.SelectedLength);
					Assert.Equal ("the second line.", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (19, _textView.SelectedLength);
					Assert.Equal ("is the second line.", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (24, _textView.SelectedLength);
					Assert.Equal ("This is the second line.", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (23, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (25, _textView.SelectedLength);
					Assert.Equal ("\nThis is the second line.", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (18, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (30, _textView.SelectedLength);
					Assert.Equal ("line.\nThis is the second line.", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (36, _textView.SelectedLength);
					Assert.Equal ("first line.\nThis is the second line.", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (40, _textView.SelectedLength);
					Assert.Equal ("the first line.\nThis is the second line.", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (43, _textView.SelectedLength);
					Assert.Equal ("is the first line.\nThis is the second line.", _textView.SelectedText);
					break;
				case 10:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (48, _textView.SelectedLength);
					Assert.Equal ("This is the first line.\nThis is the second line.", _textView.SelectedText);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void WordForward_Multiline_With_Selection ()
		{
			//			    1         2          3         4
			//		  01234567890123456789012 34567890123456789012345678-Length
			//			    1         2              1         2
			//                01234567890123456789012  0123456789012345678901234
			_textView.Text = "This is the first line.\nThis is the second line.";

			_textView.SelectionStartColumn = _textView.CurrentColumn;
			_textView.SelectionStartRow = _textView.CurrentRow;
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (5, _textView.SelectedLength);
					Assert.Equal ("This ", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (8, _textView.SelectedLength);
					Assert.Equal ("This is ", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("This is the ", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (18, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (18, _textView.SelectedLength);
					Assert.Equal ("This is the first ", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (23, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (23, _textView.SelectedLength);
					Assert.Equal ("This is the first line.", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (24, _textView.SelectedLength);
					Assert.Equal ("This is the first line.\n", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (29, _textView.SelectedLength);
					Assert.Equal ("This is the first line.\nThis ", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (32, _textView.SelectedLength);
					Assert.Equal ("This is the first line.\nThis is ", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (36, _textView.SelectedLength);
					Assert.Equal ("This is the first line.\nThis is the ", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (19, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (43, _textView.SelectedLength);
					Assert.Equal ("This is the first line.\nThis is the second ", _textView.SelectedText);
					break;
				case 10:
					Assert.Equal (24, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (48, _textView.SelectedLength);
					Assert.Equal ("This is the first line.\nThis is the second line.", _textView.SelectedText);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void Kill_To_End_Delete_Forwards_And_Copy_To_The_Clipboard ()
		{
			_textView.Text = "This is the first line.\nThis is the second line.";
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("\r\nThis is the second line.", _textView.Text);
					break;
				case 1:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the second line.", _textView.Text);
					break;
				case 2:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					_textView.Paste ();
					Assert.Equal ("This is the second line.", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void Kill_To_Start_Delete_Backwards_And_Copy_To_The_Clipboard ()
		{
			_textView.Text = "This is the first line.\nThis is the second line.";
			//_textView.CursorPosition = new Point (0, _textView.Lines - 1);
			_textView.MoveEnd ();
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line.\r\n", _textView.Text);
					break;
				case 1:
					Assert.Equal (23, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line.", _textView.Text);
					break;
				case 2:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					_textView.Paste ();
					Assert.Equal ("This is the first line.", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void Kill_Delete_WordForward ()
		{
			_textView.Text = "This is the first line.";
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("is the first line.", _textView.Text);
					break;
				case 1:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("the first line.", _textView.Text);
					break;
				case 2:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("first line.", _textView.Text);
					break;
				case 3:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("line.", _textView.Text);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void Kill_Delete_WordBackward ()
		{
			_textView.Text = "This is the first line.";
			//_textView.CursorPosition = new Point (0, _textView.Lines - 1);
			_textView.MoveEnd ();
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (18, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first ", _textView.Text);
					break;
				case 1:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the ", _textView.Text);
					break;
				case 2:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is ", _textView.Text);
					break;
				case 3:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This ", _textView.Text);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		public void Copy_Or_Cut_Null_If_No_Selection ()
		{
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			_textView.Copy ();
			Assert.Equal ("", _textView.SelectedText);
			_textView.Cut ();
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void Copy_Or_Cut_Not_Null_If_Has_Selection ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.Copy ();
			Assert.Equal ("text", _textView.SelectedText);
			_textView.Cut ();
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void Copy_Or_Cut_And_Paste_With_Selection ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.Copy ();
			Assert.Equal ("text", _textView.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.Paste ();
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.Cut ();
			_textView.Paste ();
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
		}

		[Fact]
		public void Copy_Or_Cut_And_Paste_With_No_Selection ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.Copy ();
			Assert.Equal ("text", _textView.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			Assert.True (_textView.Selecting);
			_textView.Selecting = false;
			_textView.Paste ();
			Assert.Equal ("TAB to jump between texttext fields.", _textView.Text);
			_textView.SelectionStartColumn = 24;
			_textView.SelectionStartRow = 0;
			_textView.Cut ();
			Assert.Equal ("", _textView.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			Assert.True (_textView.Selecting);
			_textView.Selecting = false;
			_textView.Paste ();
			Assert.Equal ("TAB to jump between texttext fields.", _textView.Text);
		}

		[Fact]
		public void Cut_Not_Allowed_If_ReadOnly_Is_True ()
		{
			_textView.ReadOnly = true;
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.Copy ();
			Assert.Equal ("text", _textView.SelectedText);
			_textView.Cut (); // Selecting is set to false after Cut.
			Assert.Equal ("", _textView.SelectedText);
			_textView.ReadOnly = false;
			Assert.False (_textView.Selecting);
			_textView.Selecting = true; // Needed to set Selecting to true.
			_textView.Copy ();
			Assert.Equal ("text", _textView.SelectedText);
			_textView.Cut ();
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void Paste_Always_Clear_The_SelectedText ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.Copy ();
			Assert.Equal ("text", _textView.SelectedText);
			_textView.Paste ();
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		public void TextChanged_Event ()
		{
			_textView.TextChanged += () => {
				if (_textView.Text == "changing") {
					Assert.Equal ("changing", _textView.Text);
					_textView.Text = "changed";
				}
			};

			_textView.Text = "changing";
			Assert.Equal ("changed", _textView.Text);
		}

		[Fact]
		public void Used_Is_True_By_Default ()
		{
			_textView.CursorPosition = new Point (10, 0);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumup between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusp between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumusep between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedp between text fields.", _textView.Text);
		}

		[Fact]
		public void Used_Is_False ()
		{
			_textView.Used = false;
			_textView.CursorPosition = new Point (10, 0);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumu between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusbetween text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumuseetween text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedtween text fields.", _textView.Text);
		}
	}
}
