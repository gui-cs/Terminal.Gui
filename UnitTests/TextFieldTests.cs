using System;
using System.Reflection;
using Xunit;

namespace Terminal.Gui.Views {
	public class TextFieldTests {

		// This class enables test functions annotated with the [InitShutdown] attribute
		// to have a function called before the test function is called and after.
		// 
		// This is necessary because a) Application is a singleton and Init/Shutdown must be called
		// as a pair, and b) all unit test functions should be atomic.
		[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
		public class InitShutdown : Xunit.Sdk.BeforeAfterTestAttribute {

			public override void Before (MethodInfo methodUnderTest)
			{
				Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

				//                                                    1         2         3 
				//                                          01234567890123456789012345678901=32 (Length)
				TextFieldTests._textField = new TextField ("TAB to jump between text fields.");
			}

			public override void After (MethodInfo methodUnderTest)
			{
				TextFieldTests._textField = null;
				Application.Shutdown ();
			}
		}

		private static TextField _textField;

		[Fact]
		[InitShutdown]
		public void Changing_SelectedStart_Or_CursorPosition_Update_SelectedLength_And_SelectedText ()
		{
			_textField.SelectedStart = 2;
			Assert.Equal (32, _textField.CursorPosition);
			Assert.Equal (30, _textField.SelectedLength);
			Assert.Equal ("B to jump between text fields.", _textField.SelectedText);
			_textField.CursorPosition = 20;
			Assert.Equal (2, _textField.SelectedStart);
			Assert.Equal (18, _textField.SelectedLength);
			Assert.Equal ("B to jump between ", _textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void SelectedStart_With_Value_Less_Than_Minus_One_Changes_To_Minus_One ()
		{
			_textField.SelectedStart = -2;
			Assert.Equal (-1, _textField.SelectedStart);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void SelectedStart_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textField.CursorPosition = 2;
			_textField.SelectedStart = 33;
			Assert.Equal (32, _textField.SelectedStart);
			Assert.Equal (30, _textField.SelectedLength);
			Assert.Equal ("B to jump between text fields.", _textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void SelectedStart_And_CursorPosition_With_Value_Greater_Than_Text_Length_Changes_Both_To_Text_Length ()
		{
			_textField.CursorPosition = 33;
			_textField.SelectedStart = 33;
			Assert.Equal (32, _textField.CursorPosition);
			Assert.Equal (32, _textField.SelectedStart);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void SelectedStart_Greater_Than_CursorPosition_All_Selection_Is_Overwritten_On_Typing ()
		{
			_textField.SelectedStart = 19;
			_textField.CursorPosition = 12;
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jump u text fields.", _textField.Text);
		}

		[Fact]
		[InitShutdown]
		public void CursorPosition_With_Value_Less_Than_Zero_Changes_To_Zero ()
		{
			_textField.CursorPosition = -1;
			Assert.Equal (0, _textField.CursorPosition);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void CursorPosition_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textField.CursorPosition = 33;
			Assert.Equal (32, _textField.CursorPosition);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void WordForward_With_No_Selection ()
		{
			_textField.CursorPosition = 0;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void WordBackward_With_No_Selection ()
		{
			_textField.CursorPosition = _textField.Text.Length;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void WordForward_With_Selection ()
		{
			_textField.CursorPosition = 0;
			_textField.SelectedStart = 0;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (4, _textField.SelectedLength);
					Assert.Equal ("TAB ", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (7, _textField.SelectedLength);
					Assert.Equal ("TAB to ", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (12, _textField.SelectedLength);
					Assert.Equal ("TAB to jump ", _textField.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (20, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between ", _textField.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (25, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between text ", _textField.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (32, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void WordBackward_With_Selection ()
		{
			_textField.CursorPosition = _textField.Text.Length;
			_textField.SelectedStart = _textField.Text.Length;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (7, _textField.SelectedLength);
					Assert.Equal ("fields.", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (12, _textField.SelectedLength);
					Assert.Equal ("text fields.", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (20, _textField.SelectedLength);
					Assert.Equal ("between text fields.", _textField.SelectedText);
					break;
				case 3:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (25, _textField.SelectedLength);
					Assert.Equal ("jump between text fields.", _textField.SelectedText);
					break;
				case 4:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (28, _textField.SelectedLength);
					Assert.Equal ("to jump between text fields.", _textField.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (32, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void WordForward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textField.CursorPosition = 10;
			_textField.SelectedStart = 10;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (2, _textField.SelectedLength);
					Assert.Equal ("p ", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (10, _textField.SelectedLength);
					Assert.Equal ("p between ", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (15, _textField.SelectedLength);
					Assert.Equal ("p between text ", _textField.SelectedText);
					break;
				case 3:
					Assert.Equal (32, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (22, _textField.SelectedLength);
					Assert.Equal ("p between text fields.", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void WordBackward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textField.CursorPosition = 10;
			_textField.SelectedStart = 10;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (3, _textField.SelectedLength);
					Assert.Equal ("jum", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (6, _textField.SelectedLength);
					Assert.Equal ("to jum", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (10, _textField.SelectedLength);
					Assert.Equal ("TAB to jum", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void WordForward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                           1         2         3         4         5    
			//                 0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textField.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textField.CursorPosition = 0;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (6, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (9, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (28, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (38, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 6:
					Assert.Equal (40, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 7:
					Assert.Equal (46, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 8:
					Assert.Equal (48, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 9:
					Assert.Equal (55, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void WordBackward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                           1         2         3         4         5    
			//                 0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textField.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textField.CursorPosition = _textField.Text.Length;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (54, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (48, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (46, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (40, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (38, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (28, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 6:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 7:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 8:
					Assert.Equal (9, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 9:
					Assert.Equal (6, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 10:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[InitShutdown]
		public void Copy_Or_Cut_Null_If_No_Selection ()
		{
			_textField.SelectedStart = -1;
			_textField.Copy ();
			Assert.Null (_textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void Copy_Or_Cut_Not_Null_If_Has_Selection ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void Copy_Or_Cut_And_Paste_With_Selection ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			_textField.Paste ();
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			_textField.SelectedStart = 20;
			_textField.Cut ();
			_textField.Paste ();
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
		}

		[Fact]
		[InitShutdown]
		public void Copy_Or_Cut_And_Paste_With_No_Selection ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			_textField.SelectedStart = -1;
			_textField.Paste ();
			Assert.Equal ("TAB to jump between texttext fields.", _textField.Text);
			_textField.SelectedStart = 24;
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			_textField.SelectedStart = -1;
			_textField.Paste ();
			Assert.Equal ("TAB to jump between texttext fields.", _textField.Text);
		}

		[Fact]
		[InitShutdown]
		public void Copy_Or_Cut__Not_Allowed_If_Secret_Is_True ()
		{
			_textField.Secret = true;
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Null (_textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
			_textField.Secret = false;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void Paste_Always_Clear_The_SelectedText ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			_textField.Paste ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[InitShutdown]
		public void TextChanging_Event ()
		{
			bool cancel = true;

			_textField.TextChanging += (e) => {
				Assert.Equal ("changing", e.NewText);
				if (cancel) {
					e.Cancel = true;
				}
			};

			_textField.Text = "changing";
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			cancel = false;
			_textField.Text = "changing";
			Assert.Equal ("changing", _textField.Text);
		}

		[Fact]
		[InitShutdown]
		public void TextChanged_Event ()
		{
			_textField.TextChanged += (e) => {
				Assert.Equal ("TAB to jump between text fields.", e);
			};

			_textField.Text = "changed";
			Assert.Equal ("changed", _textField.Text);
		}

		[Fact]
		[InitShutdown]
		public void Used_Is_True_By_Default ()
		{
			_textField.CursorPosition = 10;
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumup between text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusp between text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumusep between text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedp between text fields.", _textField.Text);
		}

		[Fact]
		[InitShutdown]
		public void Used_Is_False ()
		{
			_textField.Used = false;
			_textField.CursorPosition = 10;
			Assert.Equal ("TAB to jump between text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumu between text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusbetween text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumuseetween text fields.", _textField.Text);
			_textField.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedtween text fields.", _textField.Text);
		}

		[Fact]
		public void ProcessKey_Backspace_From_End ()
		{
			var tf = new TextField ("ABC");
			tf.EnsureFocus ();
			Assert.Equal ("ABC", tf.Text);

			// now delete the C
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("AB", tf.Text);

			// then delete the B
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("A", tf.Text);

			// then delete the A
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("", tf.Text);
		}

		[Fact]
		public void ProcessKey_Backspace_From_Middle ()
		{
			var tf = new TextField ("ABC");
			tf.EnsureFocus ();
			tf.CursorPosition = 2;
			Assert.Equal ("ABC", tf.Text);

			// now delete the B
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("AC", tf.Text);

			// then delete the A
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("C", tf.Text);

			// then delete nothing
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("C", tf.Text);

			// now delete the C
			tf.CursorPosition = 1;
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("", tf.Text);
		}

		[Fact]
		public void Cancel_TextChanging_ThenBackspace ()
		{
			var tf = new TextField ();
			tf.EnsureFocus ();
			tf.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ()));
			Assert.Equal ("A", tf.Text);

			// cancel the next keystroke
			tf.TextChanging += (e) => e.Cancel = e.NewText == "AB";
			tf.ProcessKey (new KeyEvent (Key.B, new KeyModifiers ()));

			// B was canceled so should just be A
			Assert.Equal ("A", tf.Text);

			// now delete the A
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));

			Assert.Equal ("", tf.Text);
		}
	}
}
